using Nexora.Domain.Common.Entities;

namespace Nexora.Domain.Chat;

public class Message : CreatedEntityBase
{
    protected Message()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid ConversationId { get; private set; }

    public string SenderType { get; set; } = string.Empty;

    public Guid? SenderUserId { get; private set; }

    public byte[] ContentCiphertext { get; private set; } = Array.Empty<byte>();

    public string ContentHash { get; private set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public int SequenceNo { get; set; }

}
