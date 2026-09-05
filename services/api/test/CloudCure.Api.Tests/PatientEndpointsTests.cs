using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudCure.Api.Contracts;

namespace CloudCure.Api.Tests;

public class PatientEndpointsTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private static PatientIntakeApiRequest ValidRequest(string firstName = "Ada") => new(
        FirstName: firstName,
        LastName: "Lovelace",
        DateOfBirth: new DateOnly(1990, 1, 1),
        Phone: "(415) 555-2671",
        // Unique per call: people.email is uniquely constrained, and these tests share one
        // Postgres container across the whole class.
        Email: $"{firstName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com",
        AddressLine1: "123 Main St",
        AddressLine2: null,
        City: "Springfield",
        StateProvince: "IL",
        PostalCode: "62704",
        CountryCode: "US",
        EmergencyContactName: "Bob Lovelace",
        EmergencyContactPhone: "415-555-9999",
        Allergies: ["Penicillin"],
        Conditions: [],
        Medications: [],
        Surgeries: []);

    private HttpClient StaffClient(params string[] roles)
    {
        var client = factory.CreateClient();
        var token = TestJwt.Mint(Guid.NewGuid(), roles.Length > 0 ? roles : ["Nurse"]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task POST_patients_without_staff_role_is_forbidden()
    {
        var client = factory.CreateClient();
        var token = TestJwt.Mint(Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/patients", ValidRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_patients_as_nurse_creates_the_patient_and_returns_201()
    {
        var client = StaffClient("Nurse");

        var response = await client.PostAsJsonAsync("/api/patients", ValidRequest("Grace"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<PatientCreatedResponse>();
        Assert.NotNull(created);
        Assert.True(created.PatientId > 0);
    }

    [Fact]
    public async Task POST_patients_with_an_invalid_phone_number_returns_a_400_problem_response()
    {
        var client = StaffClient("Nurse");
        var request = ValidRequest("Bad") with { Phone = "not-a-phone-number" };

        var response = await client.PostAsJsonAsync("/api/patients", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GET_patients_returns_a_lightweight_projection_not_the_full_history_graph()
    {
        var client = StaffClient("Nurse");
        await client.PostAsJsonAsync("/api/patients", ValidRequest("Katherine"));

        var response = await client.GetAsync("/api/patients?q=Katherine");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PatientListResponse>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, i => i.FirstName == "Katherine");
    }

    [Fact]
    public async Task GET_patient_by_id_returns_full_detail_including_history()
    {
        var client = StaffClient("Nurse");
        var createResponse = await client.PostAsJsonAsync("/api/patients", ValidRequest("Rosalind"));
        var created = await createResponse.Content.ReadFromJsonAsync<PatientCreatedResponse>();

        var response = await client.GetAsync($"/api/patients/{created!.PatientId}");

        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<PatientDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal("Rosalind", detail.FirstName);
        Assert.Contains("Penicillin", detail.Allergies);
        Assert.Equal("(415) 555-2671", detail.PhoneDisplay);
    }

    [Fact]
    public async Task GET_patient_by_id_returns_404_for_an_unknown_id()
    {
        var client = StaffClient("Nurse");

        var response = await client.GetAsync("/api/patients/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
