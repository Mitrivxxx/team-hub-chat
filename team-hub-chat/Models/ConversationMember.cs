namespace team_hub_chat.Models;

public class ConversationMember
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "";
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public DateTimeOffset? MutedUntil { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Message? LastReadMessage { get; set; }
}
