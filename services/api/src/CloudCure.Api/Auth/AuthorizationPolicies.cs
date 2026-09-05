namespace CloudCure.Api.Auth;

public static class AuthorizationPolicies
{
    public const string RequireStaff = nameof(RequireStaff);
    public const string RequireDoctor = nameof(RequireDoctor);
    public const string RequireAdmin = nameof(RequireAdmin);
}
