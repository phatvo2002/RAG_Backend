using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Abstractions.Repositories;
using Nexora.Application.Common.Constants;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Features.V1.SystemManagements.Organization.Command;

public sealed record SuspendOrganizationCommand(Guid OrganizationId) : ITransactionalCommand<Result>;

public sealed class SuspendOrganizationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<SuspendOrganizationCommand, Result>
{
    public async Task<Result> Handle(SuspendOrganizationCommand request, CancellationToken cancellationToken)
    {
        var organization = await unitOfWork.OrganizationRepository.GetByIdAsync(request.OrganizationId, cancellationToken, asTracking: true);
        if (organization is null)
        {
            return Result.Failure(Error.NotFound(CommonMessages.Organization.NotFound));
        }

        organization.Suspend();
        return Result.Success();
    }
}
