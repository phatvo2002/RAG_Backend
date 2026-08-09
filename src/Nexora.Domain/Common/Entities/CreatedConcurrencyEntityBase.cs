namespace Nexora.Domain.Common.Entities;

public abstract class CreatedConcurrencyEntityBase : CreatedEntityBase
{
    protected CreatedConcurrencyEntityBase()
    {
    }

    public long RowVersion { get; private set; }
}
