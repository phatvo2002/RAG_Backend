using Nexora.Domain.Common;
using Nexora.Domain.Common.Constants;
using Nexora.Domain.Common.Exceptions;

namespace Nexora.Domain.Common.Entities;

public abstract class UserProfileEntityBase
{
    protected UserProfileEntityBase()
    {
    }

    protected UserProfileEntityBase(Guid userId, DateTimeOffset now)
    {
        UserId = DomainGuard.RequiredId(userId, nameof(userId));
        var utcNow = DomainGuard.Utc(now, nameof(now));
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
        RowVersion = 1;
    }

    public Guid UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeleteBy { get; private set; }

    public long RowVersion { get; private set; }

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
