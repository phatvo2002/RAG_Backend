using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Nexora.Application.Common.Constants;

namespace Nexora.Infrastructure.Persistence;

public sealed class DesignTimeNexoraDbContextFactory
    : IDesignTimeDbContextFactory<NexoraDbContext>
{
    public NexoraDbContext CreateDbContext(string[] args)
    {
        var webApiPath = ResolveWebApiPath();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webApiPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Postgres");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                CommonMessages.Infrastructure.DesignTimeConnectionStringRequired);
        }

        var options = new DbContextOptionsBuilder<NexoraDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SchemaNames.Identity))
            .Options;

        return new NexoraDbContext(options, userContext: null, tenantContext: null, clock: null);
    }

    private static string ResolveWebApiPath()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Nexora.WebApi"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Nexora.WebApi"),
            Directory.GetCurrentDirectory()
        };

        return candidates.FirstOrDefault(Directory.Exists)
            ?? throw new DirectoryNotFoundException(CommonMessages.Infrastructure.WebApiConfigurationDirectoryNotFound);
    }
}
