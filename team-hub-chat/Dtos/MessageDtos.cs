using System.ComponentModel.DataAnnotations;

namespace team_hub_chat.Dtos;

public sealed class CreateMessageRequest
{
    [MaxLength(8000)]
    public string? Content { get; set; }

    [Required]
    [MaxLength(30)]
    public string MessageType { get; set; } = "";

    public Guid? ReplyToMessageId { get; set; }

    public List<Guid> MentionUserIds { get; set; } = [];
}

public sealed class UpdateMessageRequest
{
    [Required]
    [MaxLength(8000)]
    public string Content { get; set; } = "";
}

public sealed class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public Guid? ReplyToMessageId { get; set; }
    public string? Content { get; set; }
    public string MessageType { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public IReadOnlyList<MessageMentionResponse> Mentions { get; set; } = [];
    public IReadOnlyList<MessageReactionResponse> Reactions { get; set; } = [];
    public IReadOnlyList<MessageAttachmentResponse> Attachments { get; set; } = [];
}

public sealed class MessagePageResponse
{
    public IReadOnlyList<MessageResponse> Items { get; set; } = [];
    public int PageSize { get; set; }
}

public sealed class MessageMentionResponse
{
    public Guid UserId { get; set; }
}

public sealed class MessageReactionResponse
{
    public Guid UserId { get; set; }
    public string Reaction { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SetMessageReactionRequest
{
    [Required]
    [MaxLength(50)]
    public string Reaction { get; set; } = "";
}

public sealed class MessageReadResponse
{
    public Guid UserId { get; set; }
    public DateTimeOffset ReadAt { get; set; }
}

public sealed class MessageAttachmentResponse
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public string? Url { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
