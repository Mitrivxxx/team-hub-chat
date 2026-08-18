using System.ComponentModel.DataAnnotations;

namespace team_hub_chat.Dtos;

public sealed class CreateConversationRequest
{
    [Required]
    [MaxLength(30)]
    public string Type { get; set; } = "";

    [MaxLength(255)]
    public string? Name { get; set; }

    public List<Guid> MemberUserIds { get; set; } = [];
}

public sealed class UpdateConversationRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }
}

public sealed class ConversationResponse
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Type { get; set; } = "";
    public string? Name { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public string MyRole { get; set; } = "";
    public int MemberCount { get; set; }
}

public sealed class ConversationPageResponse
{
    public IReadOnlyList<ConversationResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class AddConversationMemberRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(30)]
    public string Role { get; set; } = "";
}

public sealed class UpdateConversationMemberRequest
{
    [Required]
    [MaxLength(30)]
    public string Role { get; set; } = "";
}

public sealed class ConversationMemberResponse
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = "";
    public DateTimeOffset JoinedAt { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }
    public DateTimeOffset? MutedUntil { get; set; }
}

public sealed class ConversationPinResponse
{
    public Guid MessageId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class SetConversationPinRequest
{
    [Required]
    public Guid MessageId { get; set; }
}

public sealed class ConversationSettingsResponse
{
    public bool NotificationsEnabled { get; set; }
    public DateTimeOffset? MutedUntil { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class UpdateConversationSettingsRequest
{
    public bool? NotificationsEnabled { get; set; }
    public DateTimeOffset? MutedUntil { get; set; }
}
