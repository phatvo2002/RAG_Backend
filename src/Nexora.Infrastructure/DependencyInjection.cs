using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Minio;
using FluentValidation;
using MediatR;
using Nexora.Application;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Auth;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Behaviors;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Permissions;
using Nexora.Infrastructure.Services.Authentication;
using Nexora.Infrastructure.Services.Identity.Context;
using Nexora.Infrastructure.Services.Common;
using Nexora.Infrastructure.Services.Audit;
using Nexora.Infrastructure.Services.Authorization;
using Nexora.Infrastructure.Services.Identity;
using Nexora.Infrastructure.Persistence;
using Nexora.Infrastructure.Persistence.Repositories;
using Nexora.Infrastructure.Persistence.Repositories.Documents;
using Nexora.Infrastructure.Persistence.Repositories.Identity;
using Nexora.Infrastructure.Services.DocumentProcessing;
using Nexora.Infrastructure.Services.Integrations.Embeddings;
using Nexora.Infrastructure.Services.Integrations.Storage;
using Nexora.Infrastructure.Services.Integrations.VectorStores;
using Nexora.Infrastructure.Services.Rag;
using Nexora.Application.Common.Abstractions.Rag;
using Nexora.Application.Common.Abstractions.Chat;
using Nexora.Application.Features.V1.Chat;
using Nexora.Application.Common.Abstractions.Account;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Logging;
using Nexora.Infrastructure.BackgroundProcessing;
using Nexora.Application.Common.Abstractions.Meetings;
using Nexora.Infrastructure.Persistence.Repositories.Meetings;
using Nexora.Infrastructure.Persistence.Repositories.Chat;
using Nexora.Infrastructure.Services.Integrations.LiveKit;
using Nexora.Infrastructure.Persistence.Seeding;

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
        services.RemoveAll<IUserValidator<ApplicationUser>>();
        services.AddScoped<IUserValidator<ApplicationUser>, UnicodeUserNameValidator>();
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
        services.AddOptions<StorageOptions>().Bind(configuration.GetSection(StorageOptions.SectionName));
        services.AddOptions<QdrantOptions>().Bind(configuration.GetSection(QdrantOptions.SectionName));
        services.AddOptions<EmbeddingOptions>().Bind(configuration.GetSection(EmbeddingOptions.SectionName));
        services.AddOptions<DocumentProcessingOptions>().Bind(configuration.GetSection(DocumentProcessingOptions.SectionName));
        services.AddOptions<ChatOptions>()
            .Bind(configuration.GetSection(ChatOptions.SectionName))
            .Validate(options => float.IsFinite(options.DepartmentMatchThreshold) &&
                options.DepartmentMatchThreshold is >= 0f and <= 1f &&
                options.MaxOutputTokens is >= 256 and <= 2048,
                "Chat:DepartmentMatchThreshold must be between 0 and 1 and MaxOutputTokens between 256 and 2048.")
            .ValidateOnStart();
        services.AddSingleton<IDepartmentScopeOptions>(serviceProvider =>
            serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChatOptions>>().Value);
        services.AddOptions<ProviderKeyOptions>().Bind(configuration.GetSection(ProviderKeyOptions.SectionName));

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
                    NameClaimType = JwtRegisteredClaimNames.Sub,
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
        services.AddScoped<IAuthTokenService, AuthTokenService>();

        services.AddScoped<IUserRepository, IdentityUserRepository>();
        services.AddScoped<IRoleRepository, IdentityRoleRepository>();

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<IOrganizationRoleAssignmentRepository, OrganizationRoleAssignmentRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IDepartmentMembershipRepository, DepartmentMembershipRepository>();
        services.AddScoped<IApiFunctionRepository, ApiFunctionRepository>();
        services.AddScoped<IRoleApiFunctionRepository, RoleApiFunctionRepository>();
        services.AddScoped<IAccessTokenRepository, AccessTokenRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IDocumentFolderRepository, DocumentFolderRepository>();
        services.AddScoped<IDocumentFolderAccessGrantRepository, DocumentFolderAccessGrantRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentChunkReadStore, DocumentRepository>();
        services.AddScoped<IDocumentRetrievalStore, DocumentRepository>();
        services.AddScoped<IFolderOverviewRepository, DocumentRepository>();
        services.AddScoped<IActivityLogWriter, ActivityLogWriter>();
        services.AddScoped<IMeetingStore, MeetingStore>();
        services.AddScoped<IMeetingTranscriptStore, MeetingTranscriptStore>();
        services.AddHttpClient<IMeetingRoomProvider, LiveKitRoomProvider>();
        services.AddScoped<ApiFunctionSeeder>();
        services.AddScoped<IDocumentProcessingStore, DocumentProcessingStore>();
        services.AddScoped<IPdfOcrEngine, TesseractOcrEngine>();
        services.AddScoped<IDocumentStorageSettings>(serviceProvider => serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value);
        services.AddScoped<IDocumentIndexSettings, DocumentIndexSettings>();
        services.AddScoped<SemanticDocumentTextChunker>();
        services.AddSingleton<IDocumentChunkingProgressStore, DocumentChunkingProgressStore>();
        services.AddScoped<IDocumentTextChunker>(provider =>
        {
            var mode = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DocumentProcessingOptions>>().Value.ChunkingMode;
            return mode switch
            {
                "Semantic" => provider.GetRequiredService<SemanticDocumentTextChunker>(),
                "Fixed" => new DocumentTextChunker(),
                _ => throw new InvalidOperationException($"DocumentProcessing:ChunkingMode không hợp lệ: {mode}.")
            };
        });
        services.AddScoped<ChatCipher>();
        services.AddScoped<IChatCipher>(serviceProvider => serviceProvider.GetRequiredService<ChatCipher>());
        services.AddScoped<ProviderKeyCipher>();
        services.AddScoped<IProviderKeyCipher>(serviceProvider => serviceProvider.GetRequiredService<ProviderKeyCipher>());
        services.AddScoped<IProviderEndpointSettings>(serviceProvider => serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProviderKeyOptions>>().Value);
        services.AddScoped<IAccountStore, AccountSettingsStore>();
        services.AddScoped<IChatStore, ChatStore>();
        services.AddScoped<IChatWorkflow, ChatWorkflow>();
        services.AddScoped<IChatProviderService, ChatProviderService>();
        services.AddHttpClient<ChatModelStreamer>();
        services.AddScoped<IChatModelResolver>(serviceProvider => serviceProvider.GetRequiredService<ChatModelStreamer>());
        services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        var storageEndpoint = new Uri(storageOptions.Endpoint);
        var minioBuilder = new MinioClient().WithEndpoint(storageEndpoint.Authority).WithCredentials(storageOptions.AccessKey, storageOptions.SecretKey);
        if (storageEndpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            minioBuilder.WithSSL();
        }

        services.AddSingleton<IMinioClient>(minioBuilder.Build());
        services.AddScoped<IObjectStorage, MinioObjectStorage>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<EmbeddingRateLimiter>();
        services.AddSingleton<IEmbeddingRateLimiter>(provider => provider.GetRequiredService<EmbeddingRateLimiter>());
        services.AddSingleton<IEmbeddingBatchScheduler>(provider => provider.GetRequiredService<EmbeddingRateLimiter>());
        services.AddHttpClient<IEmbeddingProvider, GeminiEmbeddingProvider>();
        services.AddScoped<GeminiEmbeddingGeneratorAdapter>();
        services.AddScoped<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>(
            provider => provider.GetRequiredService<GeminiEmbeddingGeneratorAdapter>());
        services.AddHttpClient<IVectorStore, QdrantVectorStore>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<QdrantOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("api-key", options.ApiKey);
            }
        });
        services.AddSingleton<DocumentProcessingQueue>();
        services.AddSingleton<IDocumentProcessingQueue>(serviceProvider => serviceProvider.GetRequiredService<DocumentProcessingQueue>());
        services.AddHostedService<DocumentProcessingWorker>();

        services.AddScoped<IApiRequestPermissionChecker, ApiPermissionService>();
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
            config.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

        return services;
    }
}
