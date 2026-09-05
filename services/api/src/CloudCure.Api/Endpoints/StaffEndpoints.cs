using CloudCure.Api.Auth;
using CloudCure.Api.Contracts;
using CloudCure.Application.Identity;
using CloudCure.Domain.Enums;

namespace CloudCure.Api.Endpoints;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this WebApplication app)
    {
        var staff = app.MapGroup("/api/staff").RequireAuthorization();

        // Any authenticated (even still-Pending) caller may register their own staff profile —
        // the role/approval gate is separate from "does this profile exist."
        staff.MapPost("/", async (RegisterStaffRequest request, HttpContext http, StaffProvisioningService service) =>
        {
            var personId = http.User.GetPersonId();
            await service.RegisterAsync(personId, request.WorkEmail, request.Specialization, request.StartDate, request.RoomNumber, request.EducationDegree);
            return Results.Created();
        });

        var approvals = app.MapGroup("/api/staff/pending-approvals").RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        approvals.MapGet("/", async (StaffProvisioningService service) =>
        {
            var pending = await service.ListPendingApprovalsAsync();
            return Results.Ok(pending.Select(p => new PendingApprovalResponse(p.PersonId, p.FirstName, p.LastName, p.Email, p.RequestedAt)));
        });

        approvals.MapPost("/{personId:guid}/approve", async (Guid personId, ApproveStaffRequest request, HttpContext http, StaffProvisioningService service) =>
        {
            if (!Enum.TryParse<RoleName>(request.Role, ignoreCase: true, out var role) || role == RoleName.Pending)
            {
                return Results.BadRequest(new { error = $"'{request.Role}' is not a grantable role." });
            }

            var approverId = http.User.GetPersonId();

            try
            {
                await service.ApproveAsync(personId, approverId, role);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }

            return Results.NoContent();
        });
    }
}
