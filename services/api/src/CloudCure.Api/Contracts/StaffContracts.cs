namespace CloudCure.Api.Contracts;

public record RegisterStaffRequest(string WorkEmail, string Specialization, DateOnly StartDate, string? RoomNumber, string EducationDegree);

public record PendingApprovalResponse(Guid PersonId, string FirstName, string LastName, string? Email, DateTimeOffset RequestedAt);

// Role is a closed set the admin picks from — never free text echoed back from a client's
// self-description, which is exactly how the old app let a new user become "Doctor".
public record ApproveStaffRequest(string Role);
