using CloudCure.Infrastructure.Audit;
using CloudCure.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudCure.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Reads ConnectionStrings:Default from <see cref="IConfiguration"/> lazily, at first
    /// DbContext resolution rather than at startup — so test hosts (e.g. WebApplicationFactory)
    /// that override configuration after Program.cs's top-level statements still take effect.
    /// </summary>
    public static IServiceCollection AddCloudCureData(this IServiceCollection services)
    {
        // Placeholder until phase 3 (auth foundation) registers a request-scoped implementation
        // that reads the internal JWT's subject claim — see plan section 3.
        services.AddSingleton<ICurrentRequestContext>(NullCurrentRequestContext.Instance);

        services.AddDbContext<CloudCureDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Missing required configuration: ConnectionStrings:Default");

            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        return services;
    }
}
