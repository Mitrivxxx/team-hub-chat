namespace team_hub_chat.Models;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public string? Content { get; set; }
    public string MessageType { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Message? ReplyTo { get; set; }
    public ICollection<Message> Replies { get; set; } = [];
    public ICollection<MessageAttachment> Attachments { get; set; } = [];
    public ICollection<MessageReaction> Reactions { get; set; } = [];
    public ICollection<MessageMention> Mentions { get; set; } = [];
    public ICollection<MessageRead> Reads { get; set; } = [];
    public ICollection<ConversationPin> Pins { get; set; } = [];
}
