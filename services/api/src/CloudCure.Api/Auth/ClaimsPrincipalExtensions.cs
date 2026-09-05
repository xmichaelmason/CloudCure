using System.Security.Claims;

namespace CloudCure.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetPersonId(this ClaimsPrincipal user)
    {
        var subject = user.FindFirst(InternalJwt.SubjectClaimType)?.Value;
        return Guid.TryParse(subject, out var personId)
            ? personId
            : throw new InvalidOperationException("The current principal has no valid person id (sub claim).");
    }
}
