using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Domain.Authorization.Enums;
using Nexora.Domain.Common.ValueObjects;
using DomainApiFunction = Nexora.Domain.Authorization.ApiFunction;

namespace Nexora.Infrastructure.Persistence.Seeding;

internal sealed class ApiFunctionSeeder(IUnitOfWork unitOfWork, IClock clock, ILogger<ApiFunctionSeeder> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "apifunction.json");
        if (!File.Exists(seedPath))
        {
            throw new FileNotFoundException("ApiFunction seed file was not found.", seedPath);
        }

        var json = await File.ReadAllTextAsync(seedPath, cancellationToken);
        var seeds = JsonSerializer.Deserialize<List<ApiFunctionSeed>>(json, JsonOptions)
            ?? throw new InvalidOperationException("ApiFunction seed file is empty or invalid.");

        ValidateSeeds(seeds);

        await unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var existingFunctions = await unitOfWork.ApiFunctionRepository.GetAllAsync(asNoTracking: false, cancellationToken: transactionCancellationToken);
            var existingByCode = existingFunctions.ToDictionary(function => function.Code.Value, StringComparer.OrdinalIgnoreCase);
            var addedCount = 0;
            var updatedCount = 0;

            foreach (var seed in seeds)
            {
                var code = ApiFunctionCode.Create(seed.Code).Value;
                if (existingByCode.TryGetValue(code, out var existing))
                {
                    if (existing.Id != seed.Id)
                    {
                        logger.LogWarning("ApiFunction {Code} has database Id {DatabaseId}, seed Id {SeedId}; existing Id is preserved.", code, existing.Id, seed.Id);
                    }

                    if (NeedsUpdate(existing, seed))
                    {
                        existing.Update(seed.Module, seed.Name, seed.HttpMethod, seed.RouteTemplate, seed.ActionType, seed.RiskLevel, seed.RequiresConfirmation, seed.Description);
                        updatedCount++;
                    }

                    if (existing.Status != ApiFunctionStatus.Active)
                    {
                        existing.Activate();
                        updatedCount++;
                    }

                    continue;
                }

                var created = DomainApiFunction.Create(seed.Id, code, seed.Module, seed.Name, seed.HttpMethod, seed.RouteTemplate, seed.ActionType, seed.RiskLevel, clock.UtcNow, seed.RequiresConfirmation, seed.Description);
                unitOfWork.ApiFunctionRepository.Insert(created);
                existingByCode[code] = created;
                addedCount++;
            }

            await unitOfWork.SaveChangesAsync(transactionCancellationToken);
            logger.LogInformation("ApiFunction seed completed. Added: {AddedCount}; updated: {UpdatedCount}; total: {TotalCount}.", addedCount, updatedCount, seeds.Count);
            return true;
        }, cancellationToken);
    }

    private static bool NeedsUpdate(DomainApiFunction existing, ApiFunctionSeed seed) =>
        existing.Module != seed.Module || existing.Name != seed.Name || existing.Description != seed.Description ||
        existing.HttpMethod != seed.HttpMethod || existing.RouteTemplate.Value != seed.RouteTemplate ||
        existing.ActionType != seed.ActionType || existing.RiskLevel != seed.RiskLevel ||
        existing.RequiresConfirmation != seed.RequiresConfirmation;

    private static void ValidateSeeds(IReadOnlyCollection<ApiFunctionSeed> seeds)
    {
        if (seeds.Count == 0)
        {
            throw new InvalidOperationException("ApiFunction seed file must contain at least one record.");
        }

        var duplicateCodes = seeds.GroupBy(seed => seed.Code, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        if (duplicateCodes.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate ApiFunction codes in seed file: {string.Join(", ", duplicateCodes)}.");
        }

        var duplicateIds = seeds.GroupBy(seed => seed.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        if (duplicateIds.Length > 0 || seeds.Any(seed => seed.Id == Guid.Empty))
        {
            throw new InvalidOperationException("ApiFunction seed file contains an empty or duplicate Id.");
        }
    }

    private sealed record ApiFunctionSeed(
        Guid Id,
        string Code,
        string Module,
        string Name,
        string? Description,
        ApiHttpMethod HttpMethod,
        string RouteTemplate,
        ApiActionType ActionType,
        ApiRiskLevel RiskLevel,
        bool RequiresConfirmation);
}

public static class ApiFunctionSeedingExtensions
{
    public static async Task SeedApiFunctionsAsync(this IHost host, CancellationToken cancellationToken = default)
    {
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApiFunctionSeeder>().SeedAsync(cancellationToken);
    }
}
