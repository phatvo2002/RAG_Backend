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
    bool RequiresConfirmation,
    string? MenuParentCode = null,
    int DisplayOrder = 0,
    bool IsMenuVisible = true,
    bool IsMobile = false,
    string? MenuUrl = null) : ITransactionalCommand<Result<ApiFunctionResponse>>;

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
            request.Description,
            request.MenuParentCode,
            request.DisplayOrder,
            request.IsMenuVisible,
            request.IsMobile,
            request.MenuUrl);

        unitOfWork.ApiFunctionRepository.Insert(function);
        return Result<ApiFunctionResponse>.Success(ApiFunctionResponse.FromEntity(function));
    }
}
