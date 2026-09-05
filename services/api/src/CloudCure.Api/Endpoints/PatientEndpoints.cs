using CloudCure.Api.Auth;
using CloudCure.Api.Contracts;
using CloudCure.Application.Common;
using CloudCure.Application.Patients;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Api.Endpoints;

public static class PatientEndpoints
{
    public static void MapPatientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/patients").RequireAuthorization(AuthorizationPolicies.RequireStaff);
        var phoneFormatter = new PhoneNumberFormatter();

        group.MapPost("/", async (PatientIntakeApiRequest request, PatientIntakeService service) =>
        {
            var patientId = await service.CreateAsync(new PatientIntakeRequest(
                request.FirstName,
                request.LastName,
                request.DateOfBirth,
                request.Phone,
                request.Email,
                request.AddressLine1,
                request.AddressLine2,
                request.City,
                request.StateProvince,
                request.PostalCode,
                request.CountryCode,
                request.EmergencyContactName,
                request.EmergencyContactPhone,
                request.Allergies ?? [],
                request.Conditions ?? [],
                request.Medications ?? [],
                request.Surgeries ?? []));

            return Results.Created($"/api/patients/{patientId}", new PatientCreatedResponse(patientId));
        });

        // Deliberately a lightweight projection, not the old app's GetAllWithNav() that
        // .Include()s the entire patient graph (conditions/allergies/surgeries/medications/
        // diagnoses) just to render a plain list.
        group.MapGet("/", async (CloudCureDbContext db, string? q, int page = 1, int pageSize = 25) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize is <= 0 or > 100 ? 25 : pageSize;

            var query = db.Patients.Include(p => p.Person).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p => EF.Functions.ILike(p.Person.FirstName, $"%{q}%") || EF.Functions.ILike(p.Person.LastName, $"%{q}%"));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Person.LastName).ThenBy(p => p.Person.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new { p.Id, p.Person.FirstName, p.Person.LastName, p.Person.DateOfBirth, p.Person.PhoneE164 })
                .ToListAsync();

            var mapped = items
                .Select(p => new PatientListItem(p.Id, p.FirstName, p.LastName, p.DateOfBirth, FormatDisplay(phoneFormatter, p.PhoneE164)))
                .ToList();

            return Results.Ok(new PatientListResponse(mapped, page, pageSize, totalCount));
        });

        group.MapGet("/{patientId:int}", async (int patientId, CloudCureDbContext db) =>
        {
            var patient = await db.Patients
                .Include(p => p.Person)
                .Include(p => p.Allergies)
                .Include(p => p.Conditions)
                .Include(p => p.Medications)
                .Include(p => p.Surgeries)
                .SingleOrDefaultAsync(p => p.Id == patientId);

            if (patient is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new PatientDetailResponse(
                patient.Id,
                patient.Person.FirstName,
                patient.Person.LastName,
                patient.Person.DateOfBirth,
                FormatDisplay(phoneFormatter, patient.Person.PhoneE164),
                patient.Person.Email,
                patient.EmergencyContactName,
                FormatDisplay(phoneFormatter, patient.EmergencyContactPhoneE164),
                patient.Allergies.Select(HistoryItemName).ToList(),
                patient.Conditions.Select(HistoryItemName).ToList(),
                patient.Medications.Select(HistoryItemName).ToList(),
                patient.Surgeries.Select(HistoryItemName).ToList()));
        });
    }

    private static string? FormatDisplay(PhoneNumberFormatter formatter, string? e164) =>
        e164 is null ? null : formatter.ToDisplay(e164);

    private static string HistoryItemName(Domain.Entities.PatientAllergy item) => item.FreeTextName ?? item.Reference?.CanonicalName ?? "";
    private static string HistoryItemName(Domain.Entities.PatientCondition item) => item.FreeTextName ?? item.Reference?.CanonicalName ?? "";
    private static string HistoryItemName(Domain.Entities.PatientMedication item) => item.FreeTextName ?? item.Reference?.CanonicalName ?? "";
    private static string HistoryItemName(Domain.Entities.PatientSurgery item) => item.FreeTextName ?? item.Reference?.CanonicalName ?? "";
}
