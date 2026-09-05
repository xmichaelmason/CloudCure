using CloudCure.Application.Encounters;
using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using CloudCure.Domain.Workflow;
using CloudCure.Infrastructure.Audit;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CloudCure.Application.Tests;

/// <summary>
/// Wires the already-tested EncounterWorkflow state machine to real persistence. Each
/// "recording" method only succeeds when the encounter is in exactly the stage that step
/// expects — this is the explicit, testable workflow that replaces the old app's implicit
/// "compute next step from nested template conditionals on data shape."
/// </summary>
public class EncounterWorkflowServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("cloudcure_v2_test")
        .WithUsername("cloudcure")
        .WithPassword("cloudcure")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private CloudCureDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CloudCureDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CloudCureDbContext(options, NullCurrentRequestContext.Instance);
    }

    private async Task<Guid> CreatePersonAsync(CloudCureDbContext context, string firstName, string lastName)
    {
        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        context.People.Add(person);
        await context.SaveChangesAsync();
        return person.Id;
    }

    private async Task<int> CreatePatientAsync(CloudCureDbContext context)
    {
        var personId = await CreatePersonAsync(context, "Test", "Patient");
        var patient = new Patient { PersonId = personId };
        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return patient.Id;
    }

    private async Task<int> CreateStaffMemberAsync(CloudCureDbContext context, string firstName, string lastName)
    {
        var personId = await CreatePersonAsync(context, firstName, lastName);
        var staff = new StaffMember
        {
            PersonId = personId,
            WorkEmail = $"{firstName}.{lastName}.{Guid.NewGuid():N}@clinic.example",
            Specialization = "General",
            StartDate = new DateOnly(2020, 1, 1),
            EducationDegree = "MD",
        };
        context.StaffMembers.Add(staff);
        await context.SaveChangesAsync();
        return staff.Id;
    }

    private static VitalsInput ValidVitals() => new(
        SystolicMmHg: 120, DiastolicMmHg: 80, OxygenSaturationPct: 98.0m, HeartRateBpm: 72, RespiratoryRateBpm: 16,
        TemperatureValue: 98.6m, TemperatureUnit: TemperatureUnit.Fahrenheit,
        HeightValue: 170m, HeightUnit: HeightUnit.Centimeters, WeightValue: 70m, WeightUnit: WeightUnit.Kilograms);

    private static AssessmentInput ValidAssessment(IReadOnlyList<int>? painRegionIds = null) => new(
        ChiefComplaint: "Headache", HistoryOfPresentIllness: "Started this morning", PainScale: 4,
        PainBodyRegionIds: painRegionIds ?? []);

    [Fact]
    public async Task CreateEncounterAsync_starts_the_encounter_ready_for_vitals_entry()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var service = new EncounterWorkflowService(context);

        var encounterId = await service.CreateEncounterAsync(patientId);

        var encounter = await context.Encounters.SingleAsync(e => e.Id == encounterId);
        Assert.Equal(EncounterStage.VitalsPending, encounter.Stage);
    }

    [Fact]
    public async Task RecordVitalsAsync_advances_the_encounter_to_AssessmentPending()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var service = new EncounterWorkflowService(context);
        var encounterId = await service.CreateEncounterAsync(patientId);

        await service.RecordVitalsAsync(encounterId, ValidVitals());

        var encounter = await context.Encounters.Include(e => e.Vitals).SingleAsync(e => e.Id == encounterId);
        Assert.Equal(EncounterStage.AssessmentPending, encounter.Stage);
        Assert.NotNull(encounter.Vitals);
        Assert.Equal(120, encounter.Vitals!.SystolicMmHg);
    }

    [Fact]
    public async Task RecordVitalsAsync_is_rejected_when_the_encounter_is_not_awaiting_vitals()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var service = new EncounterWorkflowService(context);
        var encounterId = await service.CreateEncounterAsync(patientId);
        await service.RecordVitalsAsync(encounterId, ValidVitals()); // now AssessmentPending

        await Assert.ThrowsAsync<InvalidEncounterTransitionException>(
            () => service.RecordVitalsAsync(encounterId, ValidVitals()));
    }

    [Fact]
    public async Task RecordAssessmentAsync_advances_to_AwaitingDoctor_and_saves_pain_points()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var service = new EncounterWorkflowService(context);
        var encounterId = await service.CreateEncounterAsync(patientId);
        await service.RecordVitalsAsync(encounterId, ValidVitals());
        var headRegion = await context.BodyRegions.FirstAsync(r => r.Code == "head");

        await service.RecordAssessmentAsync(encounterId, ValidAssessment([headRegion.Id]));

        var encounter = await context.Encounters
            .Include(e => e.Assessment).ThenInclude(a => a!.PainPoints)
            .SingleAsync(e => e.Id == encounterId);
        Assert.Equal(EncounterStage.AwaitingDoctor, encounter.Stage);
        Assert.Single(encounter.Assessment!.PainPoints);
    }

    [Fact]
    public async Task RecordAssessmentAsync_is_rejected_before_vitals_are_recorded()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var service = new EncounterWorkflowService(context);
        var encounterId = await service.CreateEncounterAsync(patientId); // still VitalsPending

        await Assert.ThrowsAsync<InvalidEncounterTransitionException>(
            () => service.RecordAssessmentAsync(encounterId, ValidAssessment()));
    }

    [Fact]
    public async Task FinalizeDiagnosisAsync_advances_to_Finalized_and_records_who_finalized_it()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var doctorId = await CreateStaffMemberAsync(context, "Doctor", "House");
        var service = new EncounterWorkflowService(context);
        var encounterId = await service.CreateEncounterAsync(patientId);
        await service.RecordVitalsAsync(encounterId, ValidVitals());
        await service.RecordAssessmentAsync(encounterId, ValidAssessment());

        await service.FinalizeDiagnosisAsync(encounterId, doctorId, "Tension headache", "Rest and hydration");

        var encounter = await context.Encounters.Include(e => e.Diagnosis).SingleAsync(e => e.Id == encounterId);
        Assert.Equal(EncounterStage.Finalized, encounter.Stage);
        Assert.Equal(doctorId, encounter.Diagnosis!.FinalizedByStaffMemberId);
        Assert.NotNull(encounter.Diagnosis.FinalizedAt);
    }

    [Fact]
    public async Task FinalizeDiagnosisAsync_is_rejected_before_an_assessment_exists()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var doctorId = await CreateStaffMemberAsync(context, "Doctor", "Who");
        var service = new EncounterWorkflowService(context);
        var encounterId = await service.CreateEncounterAsync(patientId);
        await service.RecordVitalsAsync(encounterId, ValidVitals()); // AssessmentPending, not AwaitingDoctor yet

        await Assert.ThrowsAsync<InvalidEncounterTransitionException>(
            () => service.FinalizeDiagnosisAsync(encounterId, doctorId, "x", "y"));
    }

    [Fact]
    public async Task AssignDoctorAsync_sets_the_attending_staff_member()
    {
        await using var context = CreateContext();
        var patientId = await CreatePatientAsync(context);
        var doctorId = await CreateStaffMemberAsync(context, "Meredith", "Grey");
        var service = new EncounterWorkflowService(context);
        var encounterId = await service.CreateEncounterAsync(patientId);

        await service.AssignDoctorAsync(encounterId, doctorId);

        var encounter = await context.Encounters.SingleAsync(e => e.Id == encounterId);
        Assert.Equal(doctorId, encounter.AttendingStaffMemberId);
    }
}
