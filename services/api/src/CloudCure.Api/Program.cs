using CloudCure.Api.Auth;
using CloudCure.Api.Endpoints;
using CloudCure.Api.ErrorHandling;
using CloudCure.Application;
using CloudCure.Infrastructure;
using CloudCure.Infrastructure.Data;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCloudCureData();
builder.Services.AddCloudCureApplication();
builder.Services.AddCloudCureApiAuth(builder.Configuration);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: "cloudcure-api"))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddEntityFrameworkCoreInstrumentation();
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        }
    });

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<CloudCureDbContext>();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapInternalIdentityEndpoints();
app.MapMeEndpoints();
app.MapStaffEndpoints();
app.MapPatientEndpoints();
app.MapScreeningEndpoints();
app.MapEncounterEndpoints();
app.MapLookupEndpoints();

app.Run();

public partial class Program;
