using System.Diagnostics;
using CloudCure.Infrastructure.Audit;
using Microsoft.AspNetCore.Http;

namespace CloudCure.Api.Auth;

/// <summary>
/// Attributes audit rows to whoever the validated internal JWT names as `sub`, and correlates
/// them to the request's OpenTelemetry trace — registered in DI after
/// <c>AddCloudCureData</c> so it overrides that call's <see cref="NullCurrentRequestContext"/>
/// default (see Program.cs).
/// </summary>
public class HttpContextCurrentRequestContext(IHttpContextAccessor httpContextAccessor) : ICurrentRequestContext
{
    public Guid? ActorPersonId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirst(InternalJwt.SubjectClaimType)?.Value;
            return Guid.TryParse(subject, out var personId) ? personId : null;
        }
    }

    public string? CorrelationId =>
        Activity.Current?.TraceId.ToString() ?? httpContextAccessor.HttpContext?.TraceIdentifier;
}
