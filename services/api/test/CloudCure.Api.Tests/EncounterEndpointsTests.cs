using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudCure.Api.Contracts;
using CloudCure.Domain.Entities;
using CloudCure.Domain.Enums;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CloudCure.Api.Tests;

public class EncounterEndpointsTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    private async Task<(HttpClient Client, Guid PersonId)> RealStaffClientAsync(params string[] roles)
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
        return (client, identity.PersonId);
    }

    /// <summary>Registers a StaffMember profile for the given client's own personId (needed for RequireDoctor's diagnosis endpoint, which finalizes as "the current user's staff profile").</summary>
    private static async Task RegisterAsStaffAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/staff", new RegisterStaffRequest(
            $"doc-{Guid.NewGuid():N}@clinic.example", "Emergency Medicine", new DateOnly(2020, 1, 1), null, "MD"));
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Seeds an Active role grant directly (bypassing the admin-approval API, which is
    /// exercised on its own in StaffProvisioningServiceTests/other Api tests) — these tests
    /// are about the encounter workflow, and need an already-approved doctor as a fixture.
    /// </summary>
    private async Task GrantActiveRoleAsync(Guid personId, RoleName role)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudCureDbContext>();
        var pending = await db.PersonRoles.SingleAsync(pr => pr.PersonId == personId && pr.Status == PersonRoleStatus.Pending);
        db.PersonRoles.Remove(pending);
        db.PersonRoles.Add(new PersonRole
        {
            PersonId = personId,
            RoleId = role,
            Status = PersonRoleStatus.Active,
            GrantedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task<int> CreatePatientAsync(HttpClient client)
    {
        var request = new PatientIntakeApiRequest(
            "Encounter", $"Test{Guid.NewGuid():N}", new DateOnly(1990, 1, 1), "415-555-2671",
            $"enc-{Guid.NewGuid():N}@example.com", null, null, null, null, null, null, null, null, [], [], [], []);
        var response = await client.PostAsJsonAsync("/api/patients", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PatientCreatedResponse>())!.PatientId;
    }

    private static VitalsApiInput ValidVitals() => new(120, 80, 98.0m, 72, 16, 98.6m, "Fahrenheit", 170m, "Centimeters", 70m, "Kilograms");

    [Fact]
    public async Task Full_encounter_workflow_happy_path_reaches_Finalized()
    {
        var (nurseClient, _) = await RealStaffClientAsync("Nurse");
        var (doctorClient, doctorPersonId) = await RealStaffClientAsync("Doctor");
        await RegisterAsStaffAsync(doctorClient);

        var patientId = await CreatePatientAsync(nurseClient);
        var createResponse = await nurseClient.PostAsJsonAsync($"/api/patients/{patientId}/encounters", new { });
        createResponse.EnsureSuccessStatusCode();
        var encounterId = (await createResponse.Content.ReadFromJsonAsync<EncounterCreatedResponse>())!.EncounterId;

        var afterCreate = await nurseClient.GetFromJsonAsync<EncounterDetailResponse>($"/api/encounters/{encounterId}");
        Assert.Equal("VitalsPending", afterCreate!.Stage);

        var vitalsResponse = await nurseClient.PostAsJsonAsync($"/api/encounters/{encounterId}/vitals", ValidVitals());
        Assert.Equal(HttpStatusCode.NoContent, vitalsResponse.StatusCode);

        var assessmentResponse = await nurseClient.PostAsJsonAsync($"/api/encounters/{encounterId}/assessment",
            new AssessmentApiInput("Headache", "Since this morning", 4, []));
        Assert.Equal(HttpStatusCode.NoContent, assessmentResponse.StatusCode);

        var afterAssessment = await nurseClient.GetFromJsonAsync<EncounterDetailResponse>($"/api/encounters/{encounterId}");
        Assert.Equal("AwaitingDoctor", afterAssessment!.Stage);
        Assert.NotNull(afterAssessment.Vitals);
        Assert.Equal(120, afterAssessment.Vitals!.SystolicMmHg);

        var diagnosisResponse = await doctorClient.PostAsJsonAsync($"/api/encounters/{encounterId}/diagnosis",
            new FinalizeDiagnosisRequest("Tension headache", "Rest and hydration"));
        Assert.Equal(HttpStatusCode.NoContent, diagnosisResponse.StatusCode);

        var final = await nurseClient.GetFromJsonAsync<EncounterDetailResponse>($"/api/encounters/{encounterId}");
        Assert.Equal("Finalized", final!.Stage);
        Assert.Equal("Tension headache", final.Diagnosis!.DoctorDiagnosis);
    }

    [Fact]
    public async Task Recording_vitals_twice_is_rejected_with_409()
    {
        var (client, _) = await RealStaffClientAsync("Nurse");
        var patientId = await CreatePatientAsync(client);
        var encounterId = (await (await client.PostAsJsonAsync($"/api/patients/{patientId}/encounters", new { }))
            .Content.ReadFromJsonAsync<EncounterCreatedResponse>())!.EncounterId;
        await client.PostAsJsonAsync($"/api/encounters/{encounterId}/vitals", ValidVitals());

        var response = await client.PostAsJsonAsync($"/api/encounters/{encounterId}/vitals", ValidVitals());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task A_nurse_cannot_finalize_a_diagnosis()
    {
        var (nurseClient, _) = await RealStaffClientAsync("Nurse");
        var patientId = await CreatePatientAsync(nurseClient);
        var encounterId = (await (await nurseClient.PostAsJsonAsync($"/api/patients/{patientId}/encounters", new { }))
            .Content.ReadFromJsonAsync<EncounterCreatedResponse>())!.EncounterId;
        await nurseClient.PostAsJsonAsync($"/api/encounters/{encounterId}/vitals", ValidVitals());
        await nurseClient.PostAsJsonAsync($"/api/encounters/{encounterId}/assessment", new AssessmentApiInput("x", "y", 0, []));

        var response = await nurseClient.PostAsJsonAsync($"/api/encounters/{encounterId}/diagnosis",
            new FinalizeDiagnosisRequest("x", "y"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GET_body_regions_returns_the_seeded_categorized_list()
    {
        var (client, _) = await RealStaffClientAsync();

        var response = await client.GetFromJsonAsync<List<BodyRegionResponse>>("/api/body-regions");

        Assert.NotNull(response);
        Assert.Equal(20, response!.Count);
        Assert.Contains(response, r => r.Code == "head");
    }

    [Fact]
    public async Task GET_staff_filters_by_role_and_only_returns_active_doctors()
    {
        var (nurseClient, _) = await RealStaffClientAsync("Nurse");
        var (doctorClient, doctorPersonId) = await RealStaffClientAsync("Doctor");
        await RegisterAsStaffAsync(nurseClient);
        await RegisterAsStaffAsync(doctorClient);
        await GrantActiveRoleAsync(doctorPersonId, RoleName.Doctor);

        var doctors = await nurseClient.GetFromJsonAsync<List<StaffSearchResult>>("/api/staff?role=Doctor");

        Assert.NotNull(doctors);
        Assert.NotEmpty(doctors);
    }

    [Fact]
    public async Task Assign_doctor_sets_the_attending_staff_member_on_the_encounter()
    {
        var (nurseClient, _) = await RealStaffClientAsync("Nurse");
        var (doctorClient, doctorPersonId) = await RealStaffClientAsync("Doctor");
        await RegisterAsStaffAsync(doctorClient);
        await GrantActiveRoleAsync(doctorPersonId, RoleName.Doctor);
        var doctors = await nurseClient.GetFromJsonAsync<List<StaffSearchResult>>("/api/staff?role=Doctor");
        var doctor = doctors!.Last();

        var patientId = await CreatePatientAsync(nurseClient);
        var encounterId = (await (await nurseClient.PostAsJsonAsync($"/api/patients/{patientId}/encounters", new { }))
            .Content.ReadFromJsonAsync<EncounterCreatedResponse>())!.EncounterId;

        var assignResponse = await nurseClient.PostAsJsonAsync($"/api/encounters/{encounterId}/assign-doctor",
            new AssignDoctorRequest(doctor.StaffMemberId));
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

        var detail = await nurseClient.GetFromJsonAsync<EncounterDetailResponse>($"/api/encounters/{encounterId}");
        Assert.Equal($"{doctor.FirstName} {doctor.LastName}", detail!.AttendingDoctorName);
    }
}
