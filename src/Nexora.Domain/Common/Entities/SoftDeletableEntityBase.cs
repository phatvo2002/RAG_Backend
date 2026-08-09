using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common.Entities;

public abstract class SoftDeletableEntityBase : AuditableEntityBase
{
    protected SoftDeletableEntityBase()
    {
    }

    protected SoftDeletableEntityBase(Guid id, DateTimeOffset now)
        : base(id, now)
    {
    }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeleteBy { get; private set; }

    public void MarkDeleted(Guid deletedByUserId, DateTimeOffset deletedAt)
    {
        DomainGuard.RequiredId(deletedByUserId, nameof(deletedByUserId));

        if (DeletedAt is not null)
        {
            throw new DomainException("entity.already_deleted", DomainMessages.AlreadyDeleted);
        }

        DeletedAt = DomainGuard.Utc(deletedAt, nameof(deletedAt));
        DeleteBy = deletedByUserId;
    }
}
