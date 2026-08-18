namespace team_hub_chat.Models;

public class MessageReaction
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string Reaction { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public Message Message { get; set; } = null!;
}
