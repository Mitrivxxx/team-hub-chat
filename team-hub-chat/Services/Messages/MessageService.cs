using Microsoft.EntityFrameworkCore;
using team_hub_chat.Data;
using team_hub_chat.Dtos;
using team_hub_chat.Models;
using TeamHub.BlobStorage;

namespace team_hub_chat.Services.Messages;

public interface IMessageService
{
    Task<MessagePageResponse> ListAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, Guid? before, Guid? after, int pageSize, CancellationToken cancellationToken = default);

    Task<MessageResponse> GetAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<MessageResponse> CreateAsync(
        Guid organizationId, Guid conversationId, CreateMessageRequest request, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<MessageResponse> UpdateAsync(
        Guid organizationId, Guid conversationId, Guid messageId, UpdateMessageRequest request, Guid actorUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<MessageReactionResponse> SetReactionAsync(
        Guid organizationId, Guid conversationId, Guid messageId, SetMessageReactionRequest request, Guid actorUserId, CancellationToken cancellationToken = default);

    Task DeleteReactionAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task MarkReadAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageReadResponse>> ListReadsAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class MessageService(
    ChatDbContext db,
    IChatOrganizationAccess orgAccess,
    IServiceProvider serviceProvider) : IMessageService
{
    const int DefaultPageSize = 50;
    const int MaxPageSize = 100;

    IBlobStorageService? BlobStorage => serviceProvider.GetService<IBlobStorageService>();

    public async Task<MessagePageResponse> ListAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, Guid? before, Guid? after, int pageSize, CancellationToken cancellationToken = default)
    {
        if (before is not null && after is not null)
            throw new ChatValidationException("Use either before or after, not both.");

        ClampPageSize(ref pageSize);
        await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);

        IQueryable<Message> query = db.Messages
            .AsNoTracking()
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .Where(m => m.ConversationId == conversationId);

        if (before is Guid beforeId)
        {
            var cursor = await db.Messages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == beforeId && m.ConversationId == conversationId, cancellationToken)
                ?? throw new ChatValidationException("Cursor message was not found.");
            query = query.Where(m => m.CreatedAt < cursor.CreatedAt)
                .OrderByDescending(m => m.CreatedAt)
                .ThenByDescending(m => m.Id);
        }
        else if (after is Guid afterId)
        {
            var cursor = await db.Messages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == afterId && m.ConversationId == conversationId, cancellationToken)
                ?? throw new ChatValidationException("Cursor message was not found.");
            query = query.Where(m => m.CreatedAt > cursor.CreatedAt)
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.Id);
        }
        else
        {
            query = query.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id);
        }

        var rows = await query.Take(pageSize).ToListAsync(cancellationToken);

        if (after is null)
            rows.Reverse();

        return new MessagePageResponse
        {
            Items = rows.Select(ToResponse).ToList(),
            PageSize = pageSize
        };
    }

    public async Task<MessageResponse> GetAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await LoadMessageAsync(conversationId, messageId, cancellationToken);
        return ToResponse(message);
    }

    public async Task<MessageResponse> CreateAsync(
        Guid organizationId, Guid conversationId, CreateMessageRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var messageType = request.MessageType.Trim().ToUpperInvariant();
        if (!ChatCodes.IsMessageType(messageType))
            throw new ChatValidationException("Message type must be TEXT or SYSTEM.");
        if (messageType == ChatCodes.MessageTypes.Text && string.IsNullOrWhiteSpace(request.Content))
            throw new ChatValidationException("TEXT messages require content.");

        if (request.ReplyToMessageId is Guid replyId)
        {
            var replyExists = await db.Messages.AnyAsync(
                m => m.Id == replyId && m.ConversationId == conversationId, cancellationToken);
            if (!replyExists)
                throw new ChatValidationException("Reply target was not found in this conversation.");
        }

        var mentionIds = request.MentionUserIds.Distinct().ToList();
        if (mentionIds.Count > 0)
        {
            var activeMemberIds = conversation.Members
                .Where(m => m.LeftAt == null)
                .Select(m => m.UserId)
                .ToHashSet();
            if (mentionIds.Any(id => !activeMemberIds.Contains(id)))
                throw new ChatValidationException("Mentions must be conversation members.");
        }

        var now = DateTimeOffset.UtcNow;
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = actorUserId,
            ReplyToMessageId = request.ReplyToMessageId,
            Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim(),
            MessageType = messageType,
            CreatedAt = now
        };

        foreach (var userId in mentionIds)
        {
            message.Mentions.Add(new MessageMention
            {
                MessageId = message.Id,
                UserId = userId,
                CreatedAt = now
            });
        }

        conversation.LastMessageAt = now;
        conversation.UpdatedAt = now;
        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(message);
    }

    public async Task<MessageResponse> UpdateAsync(
        Guid organizationId, Guid conversationId, Guid messageId, UpdateMessageRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await LoadMessageAsync(conversationId, messageId, cancellationToken);
        if (message.DeletedAt is not null)
            throw new ChatNotFoundException("Message was not found.");
        if (message.SenderId != actorUserId)
            throw new ChatAccessException("Only the sender can edit this message.");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ChatValidationException("Content is required.");

        var now = DateTimeOffset.UtcNow;
        message.Content = request.Content.Trim();
        message.EditedAt = now;
        message.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(message);
    }

    public async Task DeleteAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await LoadMessageAsync(conversationId, messageId, cancellationToken);
        if (message.DeletedAt is not null)
            throw new ChatNotFoundException("Message was not found.");

        var actor = conversation.Members.First(m => m.UserId == actorUserId && m.LeftAt == null);
        if (message.SenderId != actorUserId && !ChatCodes.CanManageMembers(actor.Role))
            throw new ChatAccessException("Only the sender, OWNER, or ADMIN can delete this message.");

        var now = DateTimeOffset.UtcNow;
        message.DeletedAt = now;
        message.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MessageReactionResponse> SetReactionAsync(
        Guid organizationId, Guid conversationId, Guid messageId, SetMessageReactionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await LoadMessageAsync(conversationId, messageId, cancellationToken);
        if (message.DeletedAt is not null)
            throw new ChatNotFoundException("Message was not found.");
        if (string.IsNullOrWhiteSpace(request.Reaction))
            throw new ChatValidationException("Reaction is required.");

        var now = DateTimeOffset.UtcNow;
        var reaction = message.Reactions.FirstOrDefault(r => r.UserId == actorUserId);
        if (reaction is null)
        {
            reaction = new MessageReaction
            {
                MessageId = message.Id,
                UserId = actorUserId,
                Reaction = request.Reaction.Trim(),
                CreatedAt = now
            };
            message.Reactions.Add(reaction);
        }
        else
        {
            reaction.Reaction = request.Reaction.Trim();
            reaction.CreatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new MessageReactionResponse
        {
            UserId = reaction.UserId,
            Reaction = reaction.Reaction,
            CreatedAt = reaction.CreatedAt
        };
    }

    public async Task DeleteReactionAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await LoadMessageAsync(conversationId, messageId, cancellationToken);
        var reaction = message.Reactions.FirstOrDefault(r => r.UserId == actorUserId)
            ?? throw new ChatNotFoundException("Reaction was not found.");
        db.MessageReactions.Remove(reaction);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkReadAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var conversation = await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await LoadMessageAsync(conversationId, messageId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var read = message.Reads.FirstOrDefault(r => r.UserId == actorUserId);
        if (read is null)
        {
            message.Reads.Add(new MessageRead
            {
                MessageId = message.Id,
                UserId = actorUserId,
                ReadAt = now
            });
        }
        else
        {
            read.ReadAt = now;
        }

        var member = conversation.Members.First(m => m.UserId == actorUserId && m.LeftAt == null);
        member.LastReadMessageId = message.Id;
        member.LastReadAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MessageReadResponse>> ListReadsAsync(
        Guid organizationId, Guid conversationId, Guid messageId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        await EnsureConversationMemberAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await LoadMessageAsync(conversationId, messageId, cancellationToken);
        return message.Reads
            .OrderBy(r => r.ReadAt)
            .Select(r => new MessageReadResponse { UserId = r.UserId, ReadAt = r.ReadAt })
            .ToList();
    }

    async Task<Conversation> EnsureConversationMemberAsync(
        Guid organizationId, Guid conversationId, Guid actorUserId, CancellationToken cancellationToken)
    {
        await orgAccess.EnsureOrgMemberAsync(organizationId, actorUserId, cancellationToken);
        var conversation = await db.Conversations
            .Include(c => c.Members)
            .FirstOrDefaultAsync(
                c => c.Id == conversationId && c.OrganizationId == organizationId && c.DeletedAt == null,
                cancellationToken)
            ?? throw new ChatNotFoundException("Conversation was not found.");

        if (!conversation.Members.Any(m => m.UserId == actorUserId && m.LeftAt == null))
            throw new ChatAccessException("User is not a member of this conversation.");

        return conversation;
    }

    async Task<Message> LoadMessageAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        return await db.Messages
            .Include(m => m.Mentions)
            .Include(m => m.Reactions)
            .Include(m => m.Attachments)
            .Include(m => m.Reads)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken)
            ?? throw new ChatNotFoundException("Message was not found.");
    }

    MessageResponse ToResponse(Message message)
    {
        var blob = BlobStorage;
        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            ReplyToMessageId = message.ReplyToMessageId,
            Content = message.Content,
            MessageType = message.MessageType,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            EditedAt = message.EditedAt,
            DeletedAt = message.DeletedAt,
            Mentions = message.Mentions
                .Select(m => new MessageMentionResponse { UserId = m.UserId })
                .ToList(),
            Reactions = message.Reactions
                .Select(r => new MessageReactionResponse
                {
                    UserId = r.UserId,
                    Reaction = r.Reaction,
                    CreatedAt = r.CreatedAt
                })
                .ToList(),
            Attachments = message.Attachments
                .Where(a => a.DeletedAt == null)
                .Select(a => new MessageAttachmentResponse
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    Url = blob?.GetReadSasUri(a.StorageKey)?.ToString(),
                    CreatedAt = a.CreatedAt,
                    DeletedAt = a.DeletedAt
                })
                .ToList()
        };
    }

    static void ClampPageSize(ref int pageSize)
    {
        if (pageSize < 1)
            pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize)
            pageSize = MaxPageSize;
    }
}
