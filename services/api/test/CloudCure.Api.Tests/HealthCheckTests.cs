using System.Net;

namespace CloudCure.Api.Tests;

public class HealthCheckTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [Fact]
    public async Task Healthz_ReturnsOk_WhenDatabaseIsReachable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
