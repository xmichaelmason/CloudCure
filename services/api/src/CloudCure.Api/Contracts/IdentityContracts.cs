namespace CloudCure.Api.Contracts;

public record ResolveIdentityRequest(string Auth0Subject, string? Email, string FirstName, string LastName);

public record ResolveIdentityResponse(Guid PersonId, IReadOnlyList<string> Roles, string Status);

public record MeResponse(Guid PersonId, string FirstName, string LastName, string? Email, IReadOnlyList<string> Roles, string Status);
