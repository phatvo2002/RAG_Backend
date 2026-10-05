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

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedByUserId { get; private set; }

    public static Message Create(Guid id, Guid organizationId, Guid conversationId, string senderType,
        Guid? senderUserId, byte[] content, string contentHash, int sequenceNo, DateTimeOffset now) => new(id, now)
    {
        OrganizationId = organizationId,
        ConversationId = conversationId,
        SenderType = senderType,
        SenderUserId = senderUserId,
        ContentCiphertext = content,
        ContentHash = contentHash,
        SequenceNo = sequenceNo
    };

    public void MarkDeleted(Guid userId, DateTimeOffset now)
    {
        DeletedAt ??= now;
        DeletedByUserId ??= userId;
    }

    private Message(Guid id, DateTimeOffset now) : base(id, now) { }

}
