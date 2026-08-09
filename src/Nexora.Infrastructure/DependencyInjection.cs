using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using MediatR;
using Nexora.Application;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Behaviors;
using Nexora.Infrastructure.Authentication;
using Nexora.Infrastructure.Context;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Repositories;

namespace Nexora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Postgres' is required. Configure ConnectionStrings:Postgres or ConnectionStrings__Postgres.");
        }

        services.AddDbContext<NexoraDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SchemaNames.Identity)));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<NexoraDbContext>();
        services.AddScoped<SignInManager<ApplicationUser>>();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => { options.Validate(); return true; }, "JWT configuration is invalid.")
            .ValidateOnStart();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        jwtOptions.Validate();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
        services.AddAuthorization();

        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<IHttpRequestMetadata, HttpRequestMetadata>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<IOrganizationRoleAssignmentRepository, OrganizationRoleAssignmentRepository>();
        services.AddScoped<IApiFunctionRepository, ApiFunctionRepository>();
        services.AddScoped<IAccessTokenRepository, AccessTokenRepository>();

        services.AddScoped<IPermissionChecker, ApiPermissionService>();
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

        return services;
    }
}
