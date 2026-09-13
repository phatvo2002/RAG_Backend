using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.ApiFunction.Command;

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
