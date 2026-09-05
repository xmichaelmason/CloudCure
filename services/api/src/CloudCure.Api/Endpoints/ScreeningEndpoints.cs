using CloudCure.Api.Auth;
using CloudCure.Api.Contracts;
using CloudCure.Application.Screenings;

namespace CloudCure.Api.Endpoints;

public static class ScreeningEndpoints
{
    public static void MapScreeningEndpoints(this WebApplication app)
    {
        app.MapGet("/api/screening-templates/{code}", async (string code, ScreeningService service) =>
        {
            var template = await service.GetTemplateAsync(code);
            if (template is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new ScreeningTemplateResponse(
                template.Id,
                template.Code,
                template.Title,
                template.Questions.Select(q => new ScreeningQuestionResponse(q.Id, q.DisplayOrder, q.QuestionText, q.AnswerType.ToString())).ToList()));
        }).RequireAuthorization(AuthorizationPolicies.RequireStaff);

        app.MapPost("/api/patients/{patientId:int}/screenings", async (int patientId, SubmitScreeningRequest request, HttpContext http, ScreeningService service) =>
        {
            if (await service.GetTemplateAsync(request.TemplateCode) is null)
            {
                return Results.BadRequest(new { error = $"'{request.TemplateCode}' is not a known screening template." });
            }

            var completedBy = http.User.GetPersonId();

            var answers = request.Answers
                .Select(a => new ScreeningAnswerInput(a.QuestionId, a.AnswerBool, a.AnswerText, a.AnswerNumber))
                .ToList();

            var screeningId = await service.SubmitAsync(patientId, request.TemplateCode, completedBy, request.EncounterId, answers);

            return Results.Created($"/api/screenings/{screeningId}", new ScreeningCreatedResponse(screeningId));
        }).RequireAuthorization(AuthorizationPolicies.RequireStaff);
    }
}
