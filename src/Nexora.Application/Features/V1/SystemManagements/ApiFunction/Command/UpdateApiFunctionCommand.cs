using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;
using Nexora.Application.Features.V1.SystemManagements.ApiFunction.Model;
using Nexora.Domain.Authorization.Enums;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Command;

public sealed record UpdateApiFunctionCommand(
    Guid ApiFunctionId,
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
            request.Description,
            request.MenuParentCode,
            request.DisplayOrder,
            request.IsMenuVisible,
            request.IsMobile,
            request.MenuUrl);

        return Result<ApiFunctionResponse>.Success(ApiFunctionResponse.FromEntity(function));
    }
}
