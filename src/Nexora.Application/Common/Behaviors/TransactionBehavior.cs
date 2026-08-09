using MediatR;
using Nexora.Application.Common.Abstractions;
using Nexora.Application.Common.Messaging;
using Nexora.Application.Common.Results;

namespace Nexora.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ITransactionalCommand<TResponse>)
        {
            return next(cancellationToken);
        }

        return unitOfWork.ExecuteInTransactionAsync(async transactionCancellationToken =>
        {
            var response = await next(transactionCancellationToken);
            if (response is IApplicationResult { IsSuccess: false })
            {
                return response;
            }

            await unitOfWork.SaveChangesAsync(transactionCancellationToken);
            return response;
        }, cancellationToken);
    }
}
