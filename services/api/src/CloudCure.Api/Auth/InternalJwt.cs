namespace CloudCure.Api.Auth;

/// <summary>
/// Constants for the internal, service-to-service HS256 JWT the Node BFF mints on every
/// outgoing request (see plan section 3). Never Auth0's own tokens — Auth0 is identity-only
/// here, and Node is the only thing that ever talks to it.
/// </summary>
public static class InternalJwt
{
    public const string Issuer = "cloudcure-web";
    public const string Audience = "cloudcure-api";
    public const string RoleClaimType = "role";
    public const string SubjectClaimType = "sub";
}
