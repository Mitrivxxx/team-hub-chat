namespace team_hub_chat.Models;

public class ConversationPin
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public Guid MessageId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Message Message { get; set; } = null!;
}
