namespace Nexora.Domain.Common.Entities;

public abstract class CompositeUpdateTrackedEntityBase
{
    protected CompositeUpdateTrackedEntityBase()
    {
    }

    public Guid? UpdatedByUserId { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }

    public long RowVersion { get; protected set; }
}
