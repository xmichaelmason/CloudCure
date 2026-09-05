using CloudCure.Application.Patients;
using CloudCure.Infrastructure.Audit;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CloudCure.Application.Tests;

/// <summary>
/// The regression suite for the old app's "create patient, then fire off N unawaited POSTs
/// for conditions/allergies/etc with no rollback" bug: a patient could end up half-created,
/// with only some of its history saved. Here the whole intake is one transaction — it all
/// succeeds or none of it does.
/// </summary>
public class PatientIntakeServiceTests : IAsyncLifetime
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

    private static PatientIntakeRequest ValidRequest() => new(
        FirstName: "Ada",
        LastName: "Lovelace",
        DateOfBirth: new DateOnly(1990, 1, 1),
        Phone: "(415) 555-2671",
        Email: "ada@example.com",
        AddressLine1: "123 Main St",
        AddressLine2: null,
        City: "Springfield",
        StateProvince: "IL",
        PostalCode: "62704",
        CountryCode: "US",
        EmergencyContactName: "Bob Lovelace",
        EmergencyContactPhone: "415-555-9999",
        Allergies: ["Penicillin"],
        Conditions: ["Hypertension"],
        Medications: ["Lisinopril"],
        Surgeries: []);

    [Fact]
    public async Task CreateAsync_persists_the_patient_and_all_history_items_together()
    {
        await using var context = CreateContext();
        var service = new PatientIntakeService(context);

        var patientId = await service.CreateAsync(ValidRequest());

        var patient = await context.Patients
            .Include(p => p.Person)
            .Include(p => p.Allergies)
            .Include(p => p.Conditions)
            .Include(p => p.Medications)
            .SingleAsync(p => p.Id == patientId);

        Assert.Equal("Ada", patient.Person.FirstName);
        Assert.Single(patient.Allergies);
        Assert.Equal("Penicillin", patient.Allergies.Single().FreeTextName);
        Assert.Single(patient.Conditions);
        Assert.Single(patient.Medications);
    }

    [Fact]
    public async Task CreateAsync_canonicalizes_phone_numbers_to_E164_before_storage()
    {
        await using var context = CreateContext();
        var service = new PatientIntakeService(context);

        var patientId = await service.CreateAsync(ValidRequest());

        var patient = await context.Patients.Include(p => p.Person).SingleAsync(p => p.Id == patientId);
        Assert.Equal("+14155552671", patient.Person.PhoneE164);
        Assert.Equal("+14155559999", patient.EmergencyContactPhoneE164);
    }

    [Fact]
    public async Task CreateAsync_creates_the_decomposed_address_when_provided()
    {
        await using var context = CreateContext();
        var service = new PatientIntakeService(context);

        var patientId = await service.CreateAsync(ValidRequest());

        var patient = await context.Patients.Include(p => p.Person).ThenInclude(person => person.Address).SingleAsync(p => p.Id == patientId);
        Assert.NotNull(patient.Person.Address);
        Assert.Equal("Springfield", patient.Person.Address!.City);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_unparseable_phone_number_and_persists_nothing()
    {
        await using var context = CreateContext();
        var service = new PatientIntakeService(context);
        var request = ValidRequest() with { Phone = "not-a-phone-number" };

        await Assert.ThrowsAsync<FormatException>(() => service.CreateAsync(request));

        Assert.Equal(0, await context.People.CountAsync());
        Assert.Equal(0, await context.Patients.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_rolls_back_the_entire_intake_including_history_items_on_a_database_constraint_violation()
    {
        await using var context = CreateContext();
        var service = new PatientIntakeService(context);

        // country_code is varchar(2) — this is a realistic client bug (e.g. "USA" instead of
        // "US"), not a contrived one, and it's the *last* thing EF sends in the batch, so this
        // genuinely proves the earlier inserts (person, patient, allergies) are rolled back
        // too, rather than the old app's "patient created, then some children silently failed."
        var request = ValidRequest() with { CountryCode = "USA" };

        await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(request));

        Assert.Equal(0, await context.People.CountAsync());
        Assert.Equal(0, await context.Patients.CountAsync());
        Assert.Equal(0, await context.PatientAllergies.CountAsync());
    }
}
