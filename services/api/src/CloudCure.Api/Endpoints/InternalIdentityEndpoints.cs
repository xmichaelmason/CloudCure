using CloudCure.Api.Contracts;
using CloudCure.Application.Identity;

namespace CloudCure.Api.Endpoints;

public static class InternalIdentityEndpoints
{
    /// <summary>
    /// Reachable only on the Docker-internal network in a real deployment (never published to
    /// the host — see docker-compose.yml). Still requires a validly-signed internal JWT
    /// (any subject/roles, since Node calls this before it knows the caller's personId) as
    /// defense in depth beyond network segmentation.
    /// </summary>
    public static void MapInternalIdentityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/internal/identity").RequireAuthorization();

        group.MapPost("/resolve", async (ResolveIdentityRequest request, IdentityResolutionService service) =>
        {
            var result = await service.ResolveOrProvisionAsync(request.Auth0Subject, request.Email, request.FirstName, request.LastName);
            return Results.Ok(new ResolveIdentityResponse(result.PersonId, result.Roles, result.Status));
        });
    }
}
