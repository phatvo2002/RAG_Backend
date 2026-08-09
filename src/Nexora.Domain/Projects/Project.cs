using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Projects;

public class Project : SoftDeletableEntityBase
{
    protected Project()
    {
    }

    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public byte[]? DescriptionCiphertext { get; set; }

    public Guid OwnerUserId { get; private set; }

    public string Status { get; set; } = string.Empty;

}
