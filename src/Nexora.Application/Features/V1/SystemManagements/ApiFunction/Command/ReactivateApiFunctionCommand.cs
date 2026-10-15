using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Command;

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
