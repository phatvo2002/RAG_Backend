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

    public static Conversation Create(Guid id, Guid organizationId, Guid ownerUserId, byte[] title, DateTimeOffset now) => new(id, now)
    {
        OrganizationId = organizationId,
        OwnerUserId = ownerUserId,
        TitleCiphertext = title,
        Status = "ACTIVE"
    };

    private Conversation(Guid id, DateTimeOffset now) : base(id, now) { }

}
