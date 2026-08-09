using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Chat;

public class Conversation : SoftDeletableEntityBase
{
    protected Conversation()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public byte[]? TitleCiphertext { get; set; }

    public string Status { get; set; } = string.Empty;

}
