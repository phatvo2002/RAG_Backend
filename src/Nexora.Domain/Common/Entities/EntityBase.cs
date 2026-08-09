using Nexora.Domain.Common;

namespace Nexora.Domain.Common.Entities;

public abstract class EntityBase
{
    protected EntityBase()
    {
    }

    protected EntityBase(Guid id)
    {
        Id = DomainGuard.RequiredId(id, nameof(id));
    }

    public Guid Id { get; private set; }
}
