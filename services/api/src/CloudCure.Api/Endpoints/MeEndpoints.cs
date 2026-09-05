using CloudCure.Api.Auth;
using CloudCure.Api.Contracts;
using CloudCure.Application.Identity;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/me").RequireAuthorization();

        group.MapGet("/", async (HttpContext http, CloudCureDbContext db) =>
        {
            var personId = http.User.GetPersonId();

            var person = await db.People
                .Include(p => p.PersonRoles)
                .SingleOrDefaultAsync(p => p.Id == personId);

            if (person is null)
            {
                return Results.NotFound();
            }

            var summary = PersonRoleSummary.From(person.PersonRoles);
            return Results.Ok(new MeResponse(person.Id, person.FirstName, person.LastName, person.Email, summary.Roles, summary.Status));
        });
    }
}
