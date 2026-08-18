namespace team_hub_chat.Models;

public class ConversationSetting
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public bool NotificationsEnabled { get; set; }
    public DateTimeOffset? MutedUntil { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
