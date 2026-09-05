using CloudCure.Application.Common;
using CloudCure.Domain.Entities;
using CloudCure.Infrastructure.Data;

namespace CloudCure.Application.Patients;

/// <summary>
/// Creates a patient and all of their intake-time history (allergies/conditions/medications/
/// surgeries) as one atomic operation. The old app created the patient, then fired off N
/// independent, unawaited POSTs for each history item with no transaction and no rollback —
/// a patient could end up persisted with only some of its history saved. Here it's a single
/// SaveChangesAsync call, which CloudCureDbContext already wraps in a transaction (for the
/// audit trail) — so either all of it lands, or none of it does.
/// </summary>
public class PatientIntakeService(CloudCureDbContext db)
{
    private readonly PhoneNumberFormatter _phoneFormatter = new();

    public async Task<int> CreateAsync(PatientIntakeRequest request, CancellationToken cancellationToken = default)
    {
        var phoneE164 = _phoneFormatter.ToE164(request.Phone);
        var emergencyPhoneE164 = string.IsNullOrWhiteSpace(request.EmergencyContactPhone)
            ? null
            : _phoneFormatter.ToE164(request.EmergencyContactPhone);

        var person = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            PhoneE164 = phoneE164,
            Email = request.Email,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        db.People.Add(person);

        if (!string.IsNullOrWhiteSpace(request.AddressLine1))
        {
            db.Addresses.Add(new Address
            {
                PersonId = person.Id,
                Line1 = request.AddressLine1,
                Line2 = string.IsNullOrWhiteSpace(request.AddressLine2) ? null : request.AddressLine2,
                City = request.City ?? string.Empty,
                StateProvince = request.StateProvince ?? string.Empty,
                PostalCode = request.PostalCode ?? string.Empty,
                CountryCode = request.CountryCode ?? string.Empty,
            });
        }

        var patient = new Patient
        {
            PersonId = person.Id,
            EmergencyContactName = string.IsNullOrWhiteSpace(request.EmergencyContactName) ? null : request.EmergencyContactName,
            EmergencyContactPhoneE164 = emergencyPhoneE164,
        };
        db.Patients.Add(patient);

        var recordedAt = DateTimeOffset.UtcNow;

        foreach (var name in NonBlank(request.Allergies))
        {
            db.PatientAllergies.Add(new PatientAllergy { Patient = patient, FreeTextName = name, RecordedAt = recordedAt, IsActive = true });
        }
        foreach (var name in NonBlank(request.Conditions))
        {
            db.PatientConditions.Add(new PatientCondition { Patient = patient, FreeTextName = name, RecordedAt = recordedAt, IsActive = true });
        }
        foreach (var name in NonBlank(request.Medications))
        {
            db.PatientMedications.Add(new PatientMedication { Patient = patient, FreeTextName = name, RecordedAt = recordedAt, IsActive = true });
        }
        foreach (var name in NonBlank(request.Surgeries))
        {
            db.PatientSurgeries.Add(new PatientSurgery { Patient = patient, FreeTextName = name, RecordedAt = recordedAt, IsActive = true });
        }

        await db.SaveChangesAsync(cancellationToken);

        return patient.Id;
    }

    private static IEnumerable<string> NonBlank(IReadOnlyList<string> items) =>
        items.Where(item => !string.IsNullOrWhiteSpace(item));
}
