namespace Nexora.Domain.Common.Entities;

public abstract class CreatedConcurrencyEntityBase : CreatedEntityBase
{
    protected CreatedConcurrencyEntityBase()
    {
    }

    protected CreatedConcurrencyEntityBase(Guid id, DateTimeOffset now)
        : base(id, now)
    {
        RowVersion = 1;
    }

    public long RowVersion { get; private set; }
}
