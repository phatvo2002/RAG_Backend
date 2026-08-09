using Nexora.Domain.Common;

namespace Nexora.Domain.Common.Entities;

public abstract class AuditableEntityBase : EntityBase
{
    protected AuditableEntityBase()
    {
    }

    protected AuditableEntityBase(Guid id, DateTimeOffset now)
        : base(id)
    {
        var utcNow = DomainGuard.Utc(now, nameof(now));
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
        RowVersion = 1;
    }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public long RowVersion { get; private set; }
}
