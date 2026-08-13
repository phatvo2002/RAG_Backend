using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;
using Nexora.Domain.Authorization.Enums;
using DomainApiFunction = Nexora.Domain.Authorization.ApiFunction;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Command;

public sealed record CreateApiFunctionCommand(
    string Code,
    string Module,
    string Name,
    string? Description,
    ApiHttpMethod HttpMethod,
    string RouteTemplate,
    ApiActionType ActionType,
    ApiRiskLevel RiskLevel,
    bool RequiresConfirmation) : ITransactionalCommand<Result<ApiFunctionResponse>>;

public sealed class CreateApiFunctionCommandHandler(IUnitOfWork unitOfWork, IClock clock) : IRequestHandler<CreateApiFunctionCommand, Result<ApiFunctionResponse>>
{
    public async Task<Result<ApiFunctionResponse>> Handle(CreateApiFunctionCommand request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.ApiFunctionRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
        {
            return Result<ApiFunctionResponse>.Failure(Error.Conflict(CommonMessages.ApiFunction.CodeAlreadyExists));
        }

        if (await unitOfWork.ApiFunctionRepository.GetActiveByHttpMethodAndRouteAsync(request.HttpMethod, request.RouteTemplate, null, cancellationToken) is not null)
        {
            return Result<ApiFunctionResponse>.Failure(Error.Conflict(CommonMessages.ApiFunction.RouteAlreadyExists));
        }

        var function = DomainApiFunction.Create(
            request.Code,
            request.Module,
            request.Name,
            request.HttpMethod,
            request.RouteTemplate,
            request.ActionType,
            request.RiskLevel,
            clock.UtcNow,
            request.RequiresConfirmation,
            request.Description);

        unitOfWork.ApiFunctionRepository.Insert(function);
        return Result<ApiFunctionResponse>.Success(ApiFunctionResponse.FromEntity(function));
    }
}

public sealed record UpdateApiFunctionCommand(
    Guid ApiFunctionId,
    string Module,
    string Name,
    string? Description,
    ApiHttpMethod HttpMethod,
    string RouteTemplate,
    ApiActionType ActionType,
    ApiRiskLevel RiskLevel,
    bool RequiresConfirmation) : ITransactionalCommand<Result<ApiFunctionResponse>>;

public sealed class UpdateApiFunctionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateApiFunctionCommand, Result<ApiFunctionResponse>>
{
    public async Task<Result<ApiFunctionResponse>> Handle(UpdateApiFunctionCommand request, CancellationToken cancellationToken)
    {
        var function = await unitOfWork.ApiFunctionRepository.GetAsync(
            item => item.Id == request.ApiFunctionId,
            cancellationToken: cancellationToken);
        if (function is null)
        {
            return Result<ApiFunctionResponse>.Failure(Error.NotFound(CommonMessages.ApiFunction.NotFound));
        }

        if (await unitOfWork.ApiFunctionRepository.GetActiveByHttpMethodAndRouteAsync(request.HttpMethod, request.RouteTemplate, request.ApiFunctionId, cancellationToken) is not null)
        {
            return Result<ApiFunctionResponse>.Failure(Error.Conflict(CommonMessages.ApiFunction.RouteAlreadyExists));
        }

        function.Update(
            request.Module,
            request.Name,
            request.HttpMethod,
            request.RouteTemplate,
            request.ActionType,
            request.RiskLevel,
            request.RequiresConfirmation,
            request.Description);

        return Result<ApiFunctionResponse>.Success(ApiFunctionResponse.FromEntity(function));
    }
}

public sealed record DeactivateApiFunctionCommand(Guid ApiFunctionId) : ITransactionalCommand<Result>;

public sealed class DeactivateApiFunctionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeactivateApiFunctionCommand, Result>
{
    public async Task<Result> Handle(DeactivateApiFunctionCommand request, CancellationToken cancellationToken)
    {
        var function = await unitOfWork.ApiFunctionRepository.GetAsync(
            item => item.Id == request.ApiFunctionId,
            cancellationToken: cancellationToken);
        if (function is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.ApiFunction.NotFound));
        }

        function.Deactivate();
        return Result.Success();
    }
}

public sealed record ReactivateApiFunctionCommand(Guid ApiFunctionId) : ITransactionalCommand<Result>;

public sealed class ReactivateApiFunctionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ReactivateApiFunctionCommand, Result>
{
    public async Task<Result> Handle(ReactivateApiFunctionCommand request, CancellationToken cancellationToken)
    {
        var function = await unitOfWork.ApiFunctionRepository.GetAsync(
            item => item.Id == request.ApiFunctionId,
            cancellationToken: cancellationToken);
        if (function is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.ApiFunction.NotFound));
        }

        if (await unitOfWork.ApiFunctionRepository.GetActiveByHttpMethodAndRouteAsync(function.HttpMethod, function.RouteTemplate.Value, request.ApiFunctionId, cancellationToken) is not null)
        {
            return Result.Failure(Error.Conflict(CommonMessages.ApiFunction.RouteAlreadyExists));
        }

        function.Activate();
        return Result.Success();
    }
}
