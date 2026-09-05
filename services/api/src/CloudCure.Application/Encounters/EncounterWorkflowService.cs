using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using CloudCure.Domain.Workflow;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Application.Encounters;

/// <summary>
/// Wires the <see cref="EncounterWorkflow"/> state machine to real persistence — each
/// recording step only succeeds when the encounter is in exactly the stage that step expects,
/// which is what replaces the old app's implicit "compute next step from nested template
/// conditionals on data shape" logic with something explicit and enforced server-side.
/// </summary>
public class EncounterWorkflowService(CloudCureDbContext db)
{
    public async Task<int> CreateEncounterAsync(int patientId, CancellationToken cancellationToken = default)
    {
        var encounter = new Encounter
        {
            PatientId = patientId,
            Stage = EncounterStage.Registered,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // A newly created encounter is immediately ready for vitals entry — "Registered" is a
        // transient predecessor, not a state a nurse ever has to separately advance out of.
        EncounterWorkflow.EnsureCanTransition(encounter.Stage, EncounterStage.VitalsPending);
        encounter.Stage = EncounterStage.VitalsPending;
        encounter.StartedAt = DateTimeOffset.UtcNow;

        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(cancellationToken);

        return encounter.Id;
    }

    public async Task RecordVitalsAsync(int encounterId, VitalsInput input, CancellationToken cancellationToken = default)
    {
        var encounter = await GetEncounterOrThrowAsync(encounterId, cancellationToken);
        EncounterWorkflow.EnsureCanTransition(encounter.Stage, EncounterStage.AssessmentPending);

        db.Vitals.Add(new Vitals
        {
            EncounterId = encounterId,
            SystolicMmHg = input.SystolicMmHg,
            DiastolicMmHg = input.DiastolicMmHg,
            OxygenSaturationPct = input.OxygenSaturationPct,
            HeartRateBpm = input.HeartRateBpm,
            RespiratoryRateBpm = input.RespiratoryRateBpm,
            TemperatureValue = input.TemperatureValue,
            TemperatureUnit = input.TemperatureUnit,
            HeightValue = input.HeightValue,
            HeightUnit = input.HeightUnit,
            WeightValue = input.WeightValue,
            WeightUnit = input.WeightUnit,
        });

        encounter.Stage = EncounterStage.AssessmentPending;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordAssessmentAsync(int encounterId, AssessmentInput input, CancellationToken cancellationToken = default)
    {
        var encounter = await GetEncounterOrThrowAsync(encounterId, cancellationToken);
        EncounterWorkflow.EnsureCanTransition(encounter.Stage, EncounterStage.AwaitingDoctor);

        var assessment = new Assessment
        {
            EncounterId = encounterId,
            ChiefComplaint = input.ChiefComplaint,
            HistoryOfPresentIllness = input.HistoryOfPresentIllness,
            PainScale = input.PainScale,
        };
        db.Assessments.Add(assessment);

        foreach (var bodyRegionId in input.PainBodyRegionIds)
        {
            db.AssessmentPainPoints.Add(new AssessmentPainPoint { Assessment = assessment, BodyRegionId = bodyRegionId });
        }

        encounter.Stage = EncounterStage.AwaitingDoctor;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FinalizeDiagnosisAsync(
        int encounterId, int finalizedByStaffMemberId, string doctorDiagnosis, string recommendedTreatment,
        CancellationToken cancellationToken = default)
    {
        var encounter = await GetEncounterOrThrowAsync(encounterId, cancellationToken);
        EncounterWorkflow.EnsureCanTransition(encounter.Stage, EncounterStage.Finalized);

        db.Diagnoses.Add(new Diagnosis
        {
            EncounterId = encounterId,
            DoctorDiagnosisText = doctorDiagnosis,
            RecommendedTreatment = recommendedTreatment,
            FinalizedByStaffMemberId = finalizedByStaffMemberId,
            FinalizedAt = DateTimeOffset.UtcNow,
        });

        encounter.Stage = EncounterStage.Finalized;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignDoctorAsync(int encounterId, int staffMemberId, CancellationToken cancellationToken = default)
    {
        var encounter = await GetEncounterOrThrowAsync(encounterId, cancellationToken);
        encounter.AttendingStaffMemberId = staffMemberId;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Encounter> GetEncounterOrThrowAsync(int encounterId, CancellationToken cancellationToken)
    {
        return await db.Encounters.SingleOrDefaultAsync(e => e.Id == encounterId, cancellationToken)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");
    }
}

public record VitalsInput(
    int SystolicMmHg,
    int DiastolicMmHg,
    decimal OxygenSaturationPct,
    int HeartRateBpm,
    int RespiratoryRateBpm,
    decimal TemperatureValue,
    TemperatureUnit TemperatureUnit,
    decimal HeightValue,
    HeightUnit HeightUnit,
    decimal WeightValue,
    WeightUnit WeightUnit);

public record AssessmentInput(string ChiefComplaint, string HistoryOfPresentIllness, int PainScale, IReadOnlyList<int> PainBodyRegionIds);
