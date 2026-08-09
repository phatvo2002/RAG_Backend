namespace Nexora.Domain.Common.Entities;

public abstract class CompositeUpdateTrackedEntityBase
{
    protected CompositeUpdateTrackedEntityBase()
    {
    }

    public Guid? UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public long RowVersion { get; private set; }
}
