using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CloudCure.Api.Contracts;

namespace CloudCure.Api.Tests;

/// <summary>
/// The regression suite for the old app's core security gap: it had no authentication at all
/// (UseAuthentication() was never even called) and let a new user self-assign any role,
/// including "Doctor", at registration. Every scenario here is something the old app could
/// not have caught because it had no enforcement to test in the first place.
/// </summary>
public class AuthorizationTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task Protected_endpoint_without_a_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_with_a_garbage_token_returns_401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RequireAdmin_endpoint_with_a_pending_no_role_token_returns_403()
    {
        var client = factory.CreateClient();
        var token = TestJwt.Mint(Guid.NewGuid());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/staff/pending-approvals");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RequireAdmin_endpoint_with_a_nurse_token_returns_403()
    {
        var client = factory.CreateClient();
        var token = TestJwt.Mint(Guid.NewGuid(), "Nurse");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/staff/pending-approvals");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RequireAdmin_endpoint_with_an_admin_token_returns_200()
    {
        var client = factory.CreateClient();
        var token = TestJwt.Mint(Guid.NewGuid(), "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/staff/pending-approvals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Resolve_ignores_a_client_supplied_role_claim_and_still_provisions_as_pending()
    {
        var client = factory.CreateClient();
        // Even a malicious/buggy caller asserting "Admin" in its own token must not be able
        // to make identity/resolve grant that role — role is decided only by ApproveAsync.
        var token = TestJwt.Mint(roles: "Admin");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/internal/identity/resolve", new ResolveIdentityRequest(
            Auth0Subject: $"auth0|{Guid.NewGuid()}", Email: "nobody@example.com", FirstName: "Test", LastName: "User"));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ResolveIdentityResponse>();

        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Empty(result.Roles);
    }

    [Fact]
    public async Task Resolve_without_any_token_returns_401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/internal/identity/resolve", new ResolveIdentityRequest(
            Auth0Subject: $"auth0|{Guid.NewGuid()}", Email: null, FirstName: "Test", LastName: "User"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
