using Nexora.Domain.Common;

namespace Nexora.Domain.Common.Entities;

public abstract class CreatedEntityBase : EntityBase
{
    protected CreatedEntityBase()
    {
    }

    protected CreatedEntityBase(Guid id, DateTimeOffset now)
        : base(id)
    {
        CreatedAt = DomainGuard.Utc(now, nameof(now));
    }

    public DateTimeOffset CreatedAt { get; private set; }
}
