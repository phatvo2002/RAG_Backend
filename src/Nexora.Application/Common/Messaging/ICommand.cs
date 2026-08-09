using MediatR;

namespace Nexora.Application.Common.Messaging;

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

public interface ITransactionalCommand<out TResponse> : ICommand<TResponse>
{
}

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
