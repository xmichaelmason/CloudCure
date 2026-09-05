namespace CloudCure.Application.Patients;

public record PatientIntakeRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Phone,
    string? Email,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? CountryCode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    IReadOnlyList<string> Allergies,
    IReadOnlyList<string> Conditions,
    IReadOnlyList<string> Medications,
    IReadOnlyList<string> Surgeries);
