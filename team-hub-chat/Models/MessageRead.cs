namespace team_hub_chat.Models;

public class MessageRead
{
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ReadAt { get; set; }

    public Message Message { get; set; } = null!;
}
