using TeamHub.BlobStorage;
using team_hub_chat.Data;
using team_hub_chat.Dtos;
using team_hub_chat.Models;
using Microsoft.EntityFrameworkCore;

namespace team_hub_chat.Services.Attachments;

public interface IMessageAttachmentService
{
    Task<MessageAttachmentResponse> UploadAsync(
        Guid organizationId,
        Guid conversationId,
        Guid messageId,
        IFormFile file,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid organizationId,
        Guid conversationId,
        Guid messageId,
        Guid attachmentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class MessageAttachmentService(
    ChatDbContext db,
    IChatOrganizationAccess orgAccess,
    IServiceProvider serviceProvider) : IMessageAttachmentService
{
    const long MaxFileSizeBytes = 10 * 1024 * 1024;

    static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["application/pdf"] = ".pdf",
        ["text/plain"] = ".txt",
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx",
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = ".xlsx"
    };

    IBlobStorageService? BlobStorage => serviceProvider.GetService<IBlobStorageService>();

    public async Task<MessageAttachmentResponse> UploadAsync(
        Guid organizationId,
        Guid conversationId,
        Guid messageId,
        IFormFile file,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var blobStorage = BlobStorage ?? throw new ChatStorageUnavailableException();
        ValidateFile(file);

        var conversation = await LoadConversationAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await db.Messages
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken)
            ?? throw new ChatNotFoundException("Message was not found.");

        if (message.DeletedAt is not null)
            throw new ChatNotFoundException("Message was not found.");

        var attachmentId = Guid.NewGuid();
        var extension = AllowedContentTypes[file.ContentType];
        var blobName = $"chat/{organizationId:D}/{conversationId:D}/{messageId:D}/{attachmentId:D}{extension}";

        await using var stream = file.OpenReadStream();
        await blobStorage.UploadAsync(blobName, stream, file.ContentType, cancellationToken, file.FileName);

        var now = DateTimeOffset.UtcNow;
        var attachment = new MessageAttachment
        {
            Id = attachmentId,
            MessageId = message.Id,
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            FileSize = file.Length,
            StorageKey = blobName,
            CreatedAt = now
        };
        message.Attachments.Add(attachment);
        conversation.LastMessageAt = now;
        conversation.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(attachment, blobStorage);
    }

    public async Task DeleteAsync(
        Guid organizationId,
        Guid conversationId,
        Guid messageId,
        Guid attachmentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await LoadConversationAsync(organizationId, conversationId, actorUserId, cancellationToken);
        var message = await db.Messages
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken)
            ?? throw new ChatNotFoundException("Message was not found.");

        var attachment = message.Attachments.FirstOrDefault(a => a.Id == attachmentId && a.DeletedAt == null)
            ?? throw new ChatNotFoundException("Attachment was not found.");

        var actor = conversation.Members.First(m => m.UserId == actorUserId && m.LeftAt == null);
        if (message.SenderId != actorUserId && !ChatCodes.CanManageMembers(actor.Role))
            throw new ChatAccessException("Only the sender, OWNER, or ADMIN can delete this attachment.");

        if (BlobStorage is { } blobStorage && !string.IsNullOrWhiteSpace(attachment.StorageKey))
            await blobStorage.DeleteIfExistsAsync(attachment.StorageKey, cancellationToken);

        attachment.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    async Task<Conversation> LoadConversationAsync(
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

    static void ValidateFile(IFormFile file)
    {
        if (file.Length <= 0)
            throw new ChatValidationException("File is empty.");
        if (file.Length > MaxFileSizeBytes)
            throw new ChatValidationException("File exceeds 10 MB.");
        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.ContainsKey(file.ContentType))
            throw new ChatValidationException("Unsupported file type.");
        if (string.IsNullOrWhiteSpace(file.FileName))
            throw new ChatValidationException("File name is required.");
    }

    static MessageAttachmentResponse ToResponse(MessageAttachment attachment, IBlobStorageService blobStorage) => new()
    {
        Id = attachment.Id,
        FileName = attachment.FileName,
        ContentType = attachment.ContentType,
        FileSize = attachment.FileSize,
        Url = blobStorage.GetReadSasUri(attachment.StorageKey)?.ToString(),
        CreatedAt = attachment.CreatedAt,
        DeletedAt = attachment.DeletedAt
    };
}
