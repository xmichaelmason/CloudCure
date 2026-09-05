using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudCure.Api.Contracts;

namespace CloudCure.Api.Tests;

public class ScreeningEndpointsTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private HttpClient StaffClient(params string[] roles)
    {
        var client = factory.CreateClient();
        var token = TestJwt.Mint(Guid.NewGuid(), roles.Length > 0 ? roles : ["Nurse"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GET_screening_template_returns_the_seeded_covid_questions_in_order()
    {
        var client = StaffClient();

        var response = await client.GetAsync("/api/screening-templates/covid19_v1");

        response.EnsureSuccessStatusCode();
        var template = await response.Content.ReadFromJsonAsync<ScreeningTemplateResponse>();
        Assert.NotNull(template);
        Assert.Equal(5, template.Questions.Count);
        Assert.True(template.Questions.SequenceEqual(template.Questions.OrderBy(q => q.DisplayOrder)));
    }

    [Fact]
    public async Task GET_screening_template_returns_404_for_an_unknown_code()
    {
        var client = StaffClient();

        var response = await client.GetAsync("/api/screening-templates/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GET_screening_template_without_staff_role_is_forbidden()
    {
        var client = factory.CreateClient();
        var token = TestJwt.Mint(Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/screening-templates/covid19_v1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// screenings.completed_by_person_id is a real FK to people — mints a token for an
    /// arbitrary personId with no backing row, which is fine for endpoints that never touch
    /// that FK, but submitting a screening needs an actual Person to attribute it to.
    /// </summary>
    private async Task<HttpClient> StaffClientWithRealPersonAsync(params string[] roles)
    {
        var serviceClient = factory.CreateClient();
        serviceClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Mint());
        var resolveResponse = await serviceClient.PostAsJsonAsync("/internal/identity/resolve", new ResolveIdentityRequest(
            Auth0Subject: $"auth0|{Guid.NewGuid()}", Email: null, FirstName: "Test", LastName: "Staff"));
        resolveResponse.EnsureSuccessStatusCode();
        var identity = await resolveResponse.Content.ReadFromJsonAsync<ResolveIdentityResponse>();

        var client = factory.CreateClient();
        var token = TestJwt.Mint(identity!.PersonId, roles.Length > 0 ? roles : ["Nurse"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<int> CreatePatientAsync(HttpClient client)
    {
        var request = new PatientIntakeApiRequest(
            FirstName: "Screening", LastName: $"Test{Guid.NewGuid():N}", DateOfBirth: new DateOnly(1990, 1, 1),
            Phone: "415-555-2671", Email: $"screening-{Guid.NewGuid():N}@example.com",
            AddressLine1: null, AddressLine2: null, City: null, StateProvince: null, PostalCode: null, CountryCode: null,
            EmergencyContactName: null, EmergencyContactPhone: null,
            Allergies: [], Conditions: [], Medications: [], Surgeries: []);

        var response = await client.PostAsJsonAsync("/api/patients", request);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<PatientCreatedResponse>();
        return created!.PatientId;
    }

    [Fact]
    public async Task POST_screening_submits_answers_for_every_question_and_returns_201()
    {
        var client = await StaffClientWithRealPersonAsync();
        var patientId = await CreatePatientAsync(client);

        var templateResponse = await client.GetAsync("/api/screening-templates/covid19_v1");
        var template = await templateResponse.Content.ReadFromJsonAsync<ScreeningTemplateResponse>();

        var request = new SubmitScreeningRequest(
            "covid19_v1",
            EncounterId: null,
            Answers: template!.Questions.Select(q => new ScreeningAnswerApiInput(q.Id, false, null, null)).ToList());

        var response = await client.PostAsJsonAsync($"/api/patients/{patientId}/screenings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ScreeningCreatedResponse>();
        Assert.NotNull(created);
        Assert.True(created.ScreeningId > 0);
    }

    [Fact]
    public async Task POST_screening_with_an_unknown_template_code_returns_400()
    {
        var client = StaffClient();
        var patientId = await CreatePatientAsync(client);

        var request = new SubmitScreeningRequest("not-a-template", null, []);

        var response = await client.PostAsJsonAsync($"/api/patients/{patientId}/screenings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
