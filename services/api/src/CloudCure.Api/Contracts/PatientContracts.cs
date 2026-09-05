namespace CloudCure.Api.Contracts;

public record PatientIntakeApiRequest(
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
    IReadOnlyList<string>? Allergies,
    IReadOnlyList<string>? Conditions,
    IReadOnlyList<string>? Medications,
    IReadOnlyList<string>? Surgeries);

public record PatientCreatedResponse(int PatientId);

public record PatientListItem(int PatientId, string FirstName, string LastName, DateOnly? DateOfBirth, string? PhoneDisplay);

public record PatientListResponse(IReadOnlyList<PatientListItem> Items, int Page, int PageSize, int TotalCount);

public record PatientDetailResponse(
    int PatientId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PhoneDisplay,
    string? Email,
    string? EmergencyContactName,
    string? EmergencyContactPhoneDisplay,
    IReadOnlyList<string> Allergies,
    IReadOnlyList<string> Conditions,
    IReadOnlyList<string> Medications,
    IReadOnlyList<string> Surgeries);
