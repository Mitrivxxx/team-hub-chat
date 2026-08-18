using Microsoft.EntityFrameworkCore;
using team_hub_chat.Data;
using team_hub_chat.Dtos;
using team_hub_chat.Models;

namespace team_hub_chat.Services.Conversations;

public interface IConversationService
{
    Task<ConversationPageResponse> ListAsync(
        Guid organizationId, Guid actorUserId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ConversationResponse> CreateAsync(
        Guid organizationId, CreateConversationRequest request, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<ConversationResponse> GetAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<ConversationResponse> UpdateAsync(
        Guid organizationId, Guid conversationId, UpdateConversationRequest request, Guid actorUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationMemberResponse>> ListMembersAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<ConversationMemberResponse> AddMemberAsync(
        Guid organizationId, Guid conversationId, AddConversationMemberRequest request, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<ConversationMemberResponse> UpdateMemberAsync(
        Guid organizationId, Guid conversationId, Guid userId, UpdateConversationMemberRequest request, Guid actorUserId, CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(
        Guid organizationId, Guid conversationId, Guid userId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task LeaveAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<ConversationPinResponse> GetPinAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<ConversationPinResponse> SetPinAsync(
        Guid organizationId, Guid conversationId, SetConversationPinRequest request, Guid actorUserId, CancellationToken cancellationToken = default);

    Task DeletePinAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<ConversationSettingsResponse> GetSettingsAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<ConversationSettingsResponse> UpdateSettingsAsync(
        Guid organizationId, Guid conversationId, UpdateConversationSettingsRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class ConversationService(
    ChatDbContext db,
    IChatOrganizationAccess orgAccess) : IConversationService
{
    const int DefaultPageSize = 20;
    const int MaxPageSize = 100;

    public async Task<ConversationPageResponse> ListAsync(
        Guid organizationId, Guid actorUserId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await orgAccess.EnsureOrgMemberAsync(organizationId, actorUserId, cancellationToken);
        ClampPage(ref page, ref pageSize);

        var query = db.Conversations
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.DeletedAt == null)
            .Where(c => c.Members.Any(m => m.UserId == actorUserId && m.LeftAt == null));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.LastMessageAt)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConversationResponse
            {
                Id = c.Id,
                OrganizationId = c.OrganizationId,
                Type = c.Type,
                Name = c.Name,
                CreatedBy = c.CreatedBy,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                LastMessageAt = c.LastMessageAt,
                MyRole = c.Members.Where(m => m.UserId == actorUserId && m.LeftAt == null).Select(m => m.Role).First(),
                MemberCount = c.Members.Count(m => m.LeftAt == null)
            })
            .ToListAsync(cancellationToken);

        return new ConversationPageResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ConversationResponse> CreateAsync(
        Guid organizationId, CreateConversationRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await orgAccess.EnsureOrgMemberAsync(organizationId, actorUserId, cancellationToken);

        var type = request.Type.Trim().ToUpperInvariant();
        if (!ChatCodes.IsConversationType(type))
            throw new ChatValidationException("Conversation type must be DIRECT or GROUP.");

        var extraMemberIds = request.MemberUserIds
            .Where(id => id != actorUserId)
            .Distinct()
            .ToList();

        if (type == ChatCodes.ConversationTypes.Direct)
        {
            if (extraMemberIds.Count != 1)
                throw new ChatValidationException("DIRECT conversations require exactly one other member.");
            if (!string.IsNullOrWhiteSpace(request.Name))
                throw new ChatValidationException("DIRECT conversations cannot have a name.");

            await orgAccess.EnsureOrgMembersAsync(organizationId, actorUserId, extraMemberIds, cancellationToken);
            var otherId = extraMemberIds[0];
            var duplicate = await db.Conversations.AnyAsync(
                c => c.OrganizationId == organizationId
                    && c.Type == ChatCodes.ConversationTypes.Direct
                    && c.DeletedAt == null
                    && c.Members.Count(m => m.LeftAt == null) == 2
                    && c.Members.Any(m => m.LeftAt == null && m.UserId == actorUserId)
                    && c.Members.Any(m => m.LeftAt == null && m.UserId == otherId),
                cancellationToken);
            if (duplicate)
                throw new ChatConflictException("A DIRECT conversation with this member already exists.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ChatValidationException("GROUP conversations require a name.");
            if (request.Name.Trim().Length > 255)
                throw new ChatValidationException("Name is too long.");
            await orgAccess.EnsureOrgMembersAsync(organizationId, actorUserId, extraMemberIds, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Type = type,
            Name = type == ChatCodes.ConversationTypes.Group ? request.Name!.Trim() : null,
            CreatedBy = actorUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        conversation.Members.Add(new ConversationMember
        {
            ConversationId = conversation.Id,
            UserId = actorUserId,
            Role = ChatCodes.MemberRoles.Owner,
            JoinedAt = now
        });

        foreach (var userId in extraMemberIds)
        {
            conversation.Members.Add(new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = userId,
                Role = ChatCodes.MemberRoles.Member,
                JoinedAt = now
            });
        }

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(conversation, actorUserId);
    }

    public async Task<ConversationResponse> GetAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        EnsureActiveMember(conversation, actorUserId);
        return ToResponse(conversation, actorUserId);
    }

    public async Task<ConversationResponse> UpdateAsync(
        Guid organizationId, Guid conversationId, UpdateConversationRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var member = EnsureActiveMember(conversation, actorUserId);
        if (!ChatCodes.CanManageMembers(member.Role))
            throw new ChatAccessException("Only OWNER or ADMIN can update this conversation.");
        if (conversation.Type != ChatCodes.ConversationTypes.Group)
            throw new ChatValidationException("Only GROUP conversations can be renamed.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ChatValidationException("Name is required.");

        conversation.Name = request.Name.Trim();
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(conversation, actorUserId);
    }

    public async Task DeleteAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var member = EnsureActiveMember(conversation, actorUserId);
        if (member.Role != ChatCodes.MemberRoles.Owner)
            throw new ChatAccessException("Only OWNER can delete this conversation.");

        conversation.DeletedAt = DateTimeOffset.UtcNow;
        conversation.UpdatedAt = conversation.DeletedAt.Value;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationMemberResponse>> ListMembersAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        EnsureActiveMember(conversation, actorUserId);
        return conversation.Members
            .Where(m => m.LeftAt == null)
            .OrderBy(m => m.JoinedAt)
            .Select(ToMemberResponse)
            .ToList();
    }

    public async Task<ConversationMemberResponse> AddMemberAsync(
        Guid organizationId, Guid conversationId, AddConversationMemberRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var actor = EnsureActiveMember(conversation, actorUserId);
        if (conversation.Type != ChatCodes.ConversationTypes.Group)
            throw new ChatConflictException("DIRECT conversations cannot add members.");
        if (!ChatCodes.CanManageMembers(actor.Role))
            throw new ChatAccessException("Only OWNER or ADMIN can add members.");

        var role = request.Role.Trim().ToUpperInvariant();
        if (!ChatCodes.IsMemberRole(role))
            throw new ChatValidationException("Role must be OWNER, ADMIN, or MEMBER.");
        if (role == ChatCodes.MemberRoles.Owner && actor.Role != ChatCodes.MemberRoles.Owner)
            throw new ChatAccessException("Only OWNER can assign the OWNER role.");

        await orgAccess.EnsureOrgMembersAsync(organizationId, actorUserId, [request.UserId], cancellationToken);

        var existing = conversation.Members.FirstOrDefault(m => m.UserId == request.UserId);
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            existing = new ConversationMember
            {
                ConversationId = conversation.Id,
                UserId = request.UserId,
                Role = role,
                JoinedAt = now
            };
            conversation.Members.Add(existing);
        }
        else if (existing.LeftAt is null)
        {
            throw new ChatConflictException("User is already a member of this conversation.");
        }
        else
        {
            existing.LeftAt = null;
            existing.Role = role;
            existing.JoinedAt = now;
        }

        conversation.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToMemberResponse(existing);
    }

    public async Task<ConversationMemberResponse> UpdateMemberAsync(
        Guid organizationId, Guid conversationId, Guid userId, UpdateConversationMemberRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var actor = EnsureActiveMember(conversation, actorUserId);
        if (actor.Role != ChatCodes.MemberRoles.Owner)
            throw new ChatAccessException("Only OWNER can change member roles.");

        var role = request.Role.Trim().ToUpperInvariant();
        if (!ChatCodes.IsMemberRole(role))
            throw new ChatValidationException("Role must be OWNER, ADMIN, or MEMBER.");

        var target = conversation.Members.FirstOrDefault(m => m.UserId == userId && m.LeftAt == null)
            ?? throw new ChatNotFoundException("Member was not found.");

        if (target.Role == ChatCodes.MemberRoles.Owner
            && role != ChatCodes.MemberRoles.Owner
            && CountActiveOwners(conversation) == 1)
            throw new ChatConflictException("Cannot demote the last OWNER.");

        target.Role = role;
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToMemberResponse(target);
    }

    public async Task RemoveMemberAsync(
        Guid organizationId, Guid conversationId, Guid userId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var actor = EnsureActiveMember(conversation, actorUserId);
        if (!ChatCodes.CanManageMembers(actor.Role))
            throw new ChatAccessException("Only OWNER or ADMIN can remove members.");

        var target = conversation.Members.FirstOrDefault(m => m.UserId == userId && m.LeftAt == null)
            ?? throw new ChatNotFoundException("Member was not found.");

        if (target.Role == ChatCodes.MemberRoles.Owner && CountActiveOwners(conversation) == 1)
            throw new ChatConflictException("Cannot remove the last OWNER.");

        target.LeftAt = DateTimeOffset.UtcNow;
        conversation.UpdatedAt = target.LeftAt.Value;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task LeaveAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var actor = EnsureActiveMember(conversation, actorUserId);
        if (actor.Role == ChatCodes.MemberRoles.Owner && CountActiveOwners(conversation) == 1)
            throw new ChatConflictException("Last OWNER cannot leave.");

        actor.LeftAt = DateTimeOffset.UtcNow;
        conversation.UpdatedAt = actor.LeftAt.Value;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConversationPinResponse> GetPinAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        EnsureActiveMember(conversation, actorUserId);
        var pin = conversation.Pins.FirstOrDefault(p => p.UserId == actorUserId)
            ?? throw new ChatNotFoundException("Pin was not found.");
        return ToPinResponse(pin);
    }

    public async Task<ConversationPinResponse> SetPinAsync(
        Guid organizationId, Guid conversationId, SetConversationPinRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        EnsureActiveMember(conversation, actorUserId);

        var messageExists = await db.Messages.AnyAsync(
            m => m.Id == request.MessageId && m.ConversationId == conversationId, cancellationToken);
        if (!messageExists)
            throw new ChatValidationException("Message does not belong to this conversation.");

        var now = DateTimeOffset.UtcNow;
        var pin = conversation.Pins.FirstOrDefault(p => p.UserId == actorUserId);
        if (pin is null)
        {
            pin = new ConversationPin
            {
                ConversationId = conversation.Id,
                UserId = actorUserId,
                MessageId = request.MessageId,
                CreatedAt = now
            };
            conversation.Pins.Add(pin);
        }
        else
        {
            pin.MessageId = request.MessageId;
            pin.CreatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToPinResponse(pin);
    }

    public async Task DeletePinAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        EnsureActiveMember(conversation, actorUserId);
        var pin = conversation.Pins.FirstOrDefault(p => p.UserId == actorUserId)
            ?? throw new ChatNotFoundException("Pin was not found.");
        db.ConversationPins.Remove(pin);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConversationSettingsResponse> GetSettingsAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        EnsureActiveMember(conversation, actorUserId);
        var settings = conversation.Settings.FirstOrDefault(s => s.UserId == actorUserId);
        return settings is null
            ? new ConversationSettingsResponse
            {
                NotificationsEnabled = true,
                MutedUntil = null,
                UpdatedAt = conversation.CreatedAt
            }
            : ToSettingsResponse(settings);
    }

    public async Task<ConversationSettingsResponse> UpdateSettingsAsync(
        Guid organizationId, Guid conversationId, UpdateConversationSettingsRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await LoadLiveAsync(organizationId, conversationId, actorUserId, cancellationToken);
        EnsureActiveMember(conversation, actorUserId);

        var now = DateTimeOffset.UtcNow;
        var settings = conversation.Settings.FirstOrDefault(s => s.UserId == actorUserId);
        if (settings is null)
        {
            settings = new ConversationSetting
            {
                ConversationId = conversation.Id,
                UserId = actorUserId,
                NotificationsEnabled = request.NotificationsEnabled ?? true,
                MutedUntil = request.MutedUntil,
                UpdatedAt = now
            };
            conversation.Settings.Add(settings);
        }
        else
        {
            if (request.NotificationsEnabled is bool enabled)
                settings.NotificationsEnabled = enabled;
            settings.MutedUntil = request.MutedUntil;
            settings.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToSettingsResponse(settings);
    }

    async Task<Conversation> LoadLiveAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken)
    {
        await orgAccess.EnsureOrgMemberAsync(organizationId, actorUserId, cancellationToken);

        var conversation = await db.Conversations
            .Include(c => c.Members)
            .Include(c => c.Pins)
            .Include(c => c.Settings)
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.OrganizationId == organizationId && c.DeletedAt == null,
                cancellationToken);

        return conversation ?? throw new ChatNotFoundException("Conversation was not found.");
    }

    static void ClampPage(ref int page, ref int pageSize)
    {
        if (page < 1)
            page = 1;
        if (pageSize < 1)
            pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize)
            pageSize = MaxPageSize;
    }

    static ConversationMember EnsureActiveMember(Conversation conversation, Guid userId)
    {
        return conversation.Members.FirstOrDefault(m => m.UserId == userId && m.LeftAt == null)
            ?? throw new ChatAccessException("User is not a member of this conversation.");
    }

    static int CountActiveOwners(Conversation conversation) =>
        conversation.Members.Count(m => m.LeftAt == null && m.Role == ChatCodes.MemberRoles.Owner);

    static ConversationResponse ToResponse(Conversation conversation, Guid actorUserId)
    {
        var member = conversation.Members.First(m => m.UserId == actorUserId && m.LeftAt == null);
        return new ConversationResponse
        {
            Id = conversation.Id,
            OrganizationId = conversation.OrganizationId,
            Type = conversation.Type,
            Name = conversation.Name,
            CreatedBy = conversation.CreatedBy,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            LastMessageAt = conversation.LastMessageAt,
            MyRole = member.Role,
            MemberCount = conversation.Members.Count(m => m.LeftAt == null)
        };
    }

    static ConversationMemberResponse ToMemberResponse(ConversationMember member) => new()
    {
        UserId = member.UserId,
        Role = member.Role,
        JoinedAt = member.JoinedAt,
        LastReadMessageId = member.LastReadMessageId,
        LastReadAt = member.LastReadAt,
        MutedUntil = member.MutedUntil
    };

    static ConversationPinResponse ToPinResponse(ConversationPin pin) => new()
    {
        MessageId = pin.MessageId,
        CreatedAt = pin.CreatedAt
    };

    static ConversationSettingsResponse ToSettingsResponse(ConversationSetting settings) => new()
    {
        NotificationsEnabled = settings.NotificationsEnabled,
        MutedUntil = settings.MutedUntil,
        UpdatedAt = settings.UpdatedAt
    };
}
