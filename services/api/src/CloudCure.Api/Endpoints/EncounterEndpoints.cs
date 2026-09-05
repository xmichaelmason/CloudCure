using CloudCure.Api.Auth;
using CloudCure.Api.Contracts;
using CloudCure.Application.Encounters;
using CloudCure.Domain.Enums;
using CloudCure.Domain.Workflow;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Api.Endpoints;

public static class EncounterEndpoints
{
    public static void MapEncounterEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/encounters").RequireAuthorization(AuthorizationPolicies.RequireStaff);

        app.MapPost("/api/patients/{patientId:int}/encounters", async (int patientId, EncounterWorkflowService service) =>
        {
            var encounterId = await service.CreateEncounterAsync(patientId);
            return Results.Created($"/api/encounters/{encounterId}", new EncounterCreatedResponse(encounterId));
        }).RequireAuthorization(AuthorizationPolicies.RequireStaff);

        group.MapGet("/{encounterId:int}", async (int encounterId, CloudCureDbContext db) =>
        {
            var encounter = await db.Encounters
                .Include(e => e.Patient).ThenInclude(p => p.Person)
                .Include(e => e.AttendingStaffMember).ThenInclude(s => s!.Person)
                .Include(e => e.Vitals)
                .Include(e => e.Assessment).ThenInclude(a => a!.PainPoints).ThenInclude(p => p.BodyRegion)
                .Include(e => e.Diagnosis).ThenInclude(d => d!.FinalizedByStaffMember).ThenInclude(s => s!.Person)
                .SingleOrDefaultAsync(e => e.Id == encounterId);

            if (encounter is null)
            {
                return Results.NotFound();
            }

            var vitals = encounter.Vitals is { } v
                ? new VitalsResponse(v.SystolicMmHg, v.DiastolicMmHg, v.OxygenSaturationPct, v.HeartRateBpm, v.RespiratoryRateBpm,
                    v.TemperatureValue, v.TemperatureUnit.ToString(), v.HeightValue, v.HeightUnit.ToString(), v.WeightValue, v.WeightUnit.ToString())
                : null;

            var assessment = encounter.Assessment is { } a
                ? new AssessmentResponse(a.ChiefComplaint, a.HistoryOfPresentIllness, a.PainScale,
                    a.PainPoints.Select(p => p.BodyRegion.DisplayName).ToList())
                : null;

            var diagnosis = encounter.Diagnosis is { } d
                ? new DiagnosisResponse(d.DoctorDiagnosisText, d.RecommendedTreatment,
                    d.FinalizedByStaffMember is { } f ? $"{f.Person.FirstName} {f.Person.LastName}" : null, d.FinalizedAt)
                : null;

            return Results.Ok(new EncounterDetailResponse(
                encounter.Id,
                encounter.PatientId,
                $"{encounter.Patient.Person.FirstName} {encounter.Patient.Person.LastName}",
                encounter.Stage.ToString(),
                encounter.AttendingStaffMember is { } doc ? $"{doc.Person.FirstName} {doc.Person.LastName}" : null,
                vitals,
                assessment,
                diagnosis));
        });

        group.MapPost("/{encounterId:int}/vitals", async (int encounterId, VitalsApiInput input, EncounterWorkflowService service) =>
        {
            if (!Enum.TryParse<TemperatureUnit>(input.TemperatureUnit, ignoreCase: true, out var tempUnit) ||
                !Enum.TryParse<HeightUnit>(input.HeightUnit, ignoreCase: true, out var heightUnit) ||
                !Enum.TryParse<WeightUnit>(input.WeightUnit, ignoreCase: true, out var weightUnit))
            {
                return Results.BadRequest(new { error = "Invalid unit of measure." });
            }

            try
            {
                await service.RecordVitalsAsync(encounterId, new VitalsInput(
                    input.SystolicMmHg, input.DiastolicMmHg, input.OxygenSaturationPct, input.HeartRateBpm, input.RespiratoryRateBpm,
                    input.TemperatureValue, tempUnit, input.HeightValue, heightUnit, input.WeightValue, weightUnit));
            }
            catch (InvalidEncounterTransitionException)
            {
                // Let this propagate to the global handler for a 409 — it's a subtype of
                // InvalidOperationException, so it must be caught (and rethrown) ahead of the
                // broader catch below, or it would be mismapped to 404.
                throw;
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }

            return Results.NoContent();
        });

        group.MapPost("/{encounterId:int}/assessment", async (int encounterId, AssessmentApiInput input, EncounterWorkflowService service) =>
        {
            try
            {
                await service.RecordAssessmentAsync(encounterId, new AssessmentInput(
                    input.ChiefComplaint, input.HistoryOfPresentIllness, input.PainScale, input.PainBodyRegionIds));
            }
            catch (InvalidEncounterTransitionException)
            {
                // Let this propagate to the global handler for a 409 — it's a subtype of
                // InvalidOperationException, so it must be caught (and rethrown) ahead of the
                // broader catch below, or it would be mismapped to 404.
                throw;
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }

            return Results.NoContent();
        });

        group.MapPost("/{encounterId:int}/diagnosis", async (int encounterId, FinalizeDiagnosisRequest request, HttpContext http, CloudCureDbContext db, EncounterWorkflowService service) =>
        {
            var personId = http.User.GetPersonId();
            var staffMember = await db.StaffMembers.SingleOrDefaultAsync(s => s.PersonId == personId);
            if (staffMember is null)
            {
                return Results.Problem("The current user has no staff profile.", statusCode: StatusCodes.Status409Conflict);
            }

            try
            {
                await service.FinalizeDiagnosisAsync(encounterId, staffMember.Id, request.DoctorDiagnosis, request.RecommendedTreatment);
            }
            catch (InvalidEncounterTransitionException)
            {
                // Let this propagate to the global handler for a 409 — it's a subtype of
                // InvalidOperationException, so it must be caught (and rethrown) ahead of the
                // broader catch below, or it would be mismapped to 404.
                throw;
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }

            return Results.NoContent();
        }).RequireAuthorization(AuthorizationPolicies.RequireDoctor);

        group.MapPost("/{encounterId:int}/assign-doctor", async (int encounterId, AssignDoctorRequest request, EncounterWorkflowService service) =>
        {
            try
            {
                await service.AssignDoctorAsync(encounterId, request.StaffMemberId);
            }
            catch (InvalidEncounterTransitionException)
            {
                // Let this propagate to the global handler for a 409 — it's a subtype of
                // InvalidOperationException, so it must be caught (and rethrown) ahead of the
                // broader catch below, or it would be mismapped to 404.
                throw;
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }

            return Results.NoContent();
        });
    }
}
