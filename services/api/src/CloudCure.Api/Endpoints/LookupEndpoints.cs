using CloudCure.Api.Auth;
using CloudCure.Api.Contracts;
using CloudCure.Domain.Enums;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCure.Api.Endpoints;

public static class LookupEndpoints
{
    public static void MapLookupEndpoints(this WebApplication app)
    {
        app.MapGet("/api/body-regions", async (CloudCureDbContext db) =>
        {
            var regions = await db.BodyRegions
                .OrderBy(r => r.RegionGroup).ThenBy(r => r.DisplayName)
                .Select(r => new BodyRegionResponse(r.Id, r.Code, r.DisplayName, r.RegionGroup))
                .ToListAsync();

            return Results.Ok(regions);
        }).RequireAuthorization(AuthorizationPolicies.RequireStaff);

        app.MapGet("/api/staff", async (CloudCureDbContext db, string? role, string? q) =>
        {
            var query = db.StaffMembers.Include(s => s.Person).AsQueryable();

            if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<RoleName>(role, ignoreCase: true, out var roleName))
            {
                query = query.Where(s => db.PersonRoles.Any(pr =>
                    pr.PersonId == s.PersonId && pr.RoleId == roleName && pr.Status == PersonRoleStatus.Active));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(s => EF.Functions.ILike(s.Person.FirstName, $"%{q}%") || EF.Functions.ILike(s.Person.LastName, $"%{q}%"));
            }

            var results = await query
                .OrderBy(s => s.Person.LastName)
                .Select(s => new StaffSearchResult(s.Id, s.Person.FirstName, s.Person.LastName, s.Specialization))
                .ToListAsync();

            return Results.Ok(results);
        }).RequireAuthorization(AuthorizationPolicies.RequireStaff);
    }
}
