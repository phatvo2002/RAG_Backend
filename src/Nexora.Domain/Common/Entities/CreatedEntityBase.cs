namespace Nexora.Domain.Common.Entities;

public abstract class CreatedEntityBase : EntityBase
{
    protected CreatedEntityBase()
    {
    }

    public DateTimeOffset CreatedAt { get; private set; }
}
