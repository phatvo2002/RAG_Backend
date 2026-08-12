using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Permissions;
using Nexora.Infrastructure.Authentication;
using Nexora.Infrastructure.Context;
using Nexora.Infrastructure.Authorization;
using Nexora.Infrastructure.Identity;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Repositories;
using Nexora.Infrastructure.Options;

namespace Nexora.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(CommonMessages.Infrastructure.ConnectionStringRequired);
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
        services.AddScoped<IUserStore<ApplicationUser>>(serviceProvider =>
        {
            var store = new UserStore<ApplicationUser, ApplicationRole, NexoraDbContext, Guid>(serviceProvider.GetRequiredService<NexoraDbContext>(), serviceProvider.GetRequiredService<IdentityErrorDescriber>());
            store.AutoSaveChanges = false;
            return store;
        });
        services.AddScoped<IRoleStore<ApplicationRole>>(serviceProvider =>
        {
            var store = new RoleStore<ApplicationRole, NexoraDbContext, Guid>(serviceProvider.GetRequiredService<NexoraDbContext>(), serviceProvider.GetRequiredService<IdentityErrorDescriber>());
            store.AutoSaveChanges = false;
            return store;
        });
        services.AddScoped<SignInManager<ApplicationUser>>();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => { options.Validate(); return true; }, CommonMessages.Infrastructure.JwtConfigurationInvalid)
            .ValidateOnStart();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        jwtOptions.Validate();

        services.AddOptions<CompanyOptions>().Bind(configuration.GetSection(CompanyOptions.SectionName));

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
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<IDefaultOrganizationProvider, DefaultOrganizationProvider>();
        services.AddScoped<IHttpRequestMetadata, HttpRequestMetadata>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<IOrganizationRoleAssignmentRepository, OrganizationRoleAssignmentRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDepartmentMembershipRepository, DepartmentMembershipRepository>();
        services.AddScoped<IApiFunctionRepository, ApiFunctionRepository>();
        services.AddScoped<IRoleApiFunctionRepository, RoleApiFunctionRepository>();
        services.AddScoped<IAccessTokenRepository, AccessTokenRepository>();

        services.AddScoped<IApiRequestPermissionChecker, ApiPermissionService>();
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

        return services;
    }
}
