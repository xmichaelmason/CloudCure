using System.Text;
using CloudCure.Infrastructure.Audit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CloudCure.Api.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudCureApiAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        // Overrides AddCloudCureData's NullCurrentRequestContext default now that we can read
        // the real caller off the validated token.
        services.AddScoped<ICurrentRequestContext, HttpContextCurrentRequestContext>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Resolved lazily (this callback runs at first-use, via the options system) —
                // not read eagerly at registration time — so test hosts (WebApplicationFactory)
                // that add configuration providers after Program.cs's top-level statements
                // still take effect.
                var sharedSecret = configuration["Internal:JwtSharedSecret"]
                    ?? throw new InvalidOperationException("Missing required configuration: Internal:JwtSharedSecret");

                // Keep claim types exactly as issued ("sub", "role") instead of the legacy
                // ClaimTypes.* URI remapping — simpler to reason about end to end.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = InternalJwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = InternalJwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sharedSecret)),
                    RoleClaimType = InternalJwt.RoleClaimType,
                    NameClaimType = InternalJwt.SubjectClaimType,
                    ClockSkew = TimeSpan.FromSeconds(10),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.RequireStaff, p => p.RequireRole("Nurse", "Doctor", "Admin"))
            .AddPolicy(AuthorizationPolicies.RequireDoctor, p => p.RequireRole("Doctor"))
            .AddPolicy(AuthorizationPolicies.RequireAdmin, p => p.RequireRole("Admin"));

        return services;
    }
}
