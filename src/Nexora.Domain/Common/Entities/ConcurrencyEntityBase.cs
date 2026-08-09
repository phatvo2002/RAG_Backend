namespace Nexora.Domain.Common.Entities;

public abstract class ConcurrencyEntityBase : EntityBase
{
    protected ConcurrencyEntityBase()
    {
    }

    protected ConcurrencyEntityBase(Guid id)
        : base(id)
    {
    }

    public long RowVersion { get; private set; }
}
