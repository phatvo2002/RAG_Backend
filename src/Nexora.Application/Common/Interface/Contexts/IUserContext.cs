namespace Nexora.Application.Common.Abstractions;

public interface IUserContext
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
