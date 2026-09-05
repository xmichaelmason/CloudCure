using CloudCure.Infrastructure.Audit;
using CloudCure.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace CloudCure.Api.Tests;

/// <summary>
/// Runs the API against a real, ephemeral Postgres container — not a mocked/in-memory
/// provider — so tests exercise the same database engine production uses. Deliberately
/// avoids the old app's pattern of testing against SQLite with foreign keys disabled.
/// </summary>
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSharedSecret = "test-only-shared-secret-not-for-production-use-0123456789";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("cloudcure_v2_test")
        .WithUsername("cloudcure")
        .WithPassword("cloudcure")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<CloudCureDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        await using var context = new CloudCureDbContext(options, NullCurrentRequestContext.Instance);
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Internal:JwtSharedSecret"] = JwtSharedSecret,
            });
        });
    }
}
