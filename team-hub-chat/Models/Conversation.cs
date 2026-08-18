namespace team_hub_chat.Models;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Type { get; set; } = "";
    public string? Name { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<ConversationMember> Members { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<ConversationPin> Pins { get; set; } = [];
    public ICollection<ConversationSetting> Settings { get; set; } = [];
}
