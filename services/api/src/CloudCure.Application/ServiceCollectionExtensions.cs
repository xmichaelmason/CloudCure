using CloudCure.Application.Encounters;
using CloudCure.Application.Identity;
using CloudCure.Application.Patients;
using CloudCure.Application.Screenings;
using Microsoft.Extensions.DependencyInjection;

namespace CloudCure.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudCureApplication(this IServiceCollection services)
    {
        services.AddScoped<IdentityResolutionService>();
        services.AddScoped<StaffProvisioningService>();
        services.AddScoped<PatientIntakeService>();
        services.AddScoped<ScreeningService>();
        services.AddScoped<EncounterWorkflowService>();
        return services;
    }
}
