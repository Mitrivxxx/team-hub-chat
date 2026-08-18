using Microsoft.AspNetCore.Mvc;
using team_hub_chat.Dtos;
using team_hub_chat.Services;
using team_hub_chat.Services.Attachments;

namespace team_hub_chat.Controllers.Messages;

[Route("api/chat/v{version:apiVersion}/organizations/{orgId:guid}/conversations/{conversationId:guid}/messages/{messageId:guid}/attachments")]
public sealed class MessageAttachmentsController(
    ICurrentUserService currentUserService,
    IMessageAttachmentService attachmentService) : ChatApiController
{
    /// <summary>Upload a message attachment.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(MessageAttachmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Upload(
        Guid orgId, Guid conversationId, Guid messageId, IFormFile file, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var attachment = await attachmentService.UploadAsync(
            orgId, conversationId, messageId, file, userId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, attachment);
    }

    /// <summary>Soft-delete a message attachment.</summary>
    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Delete(
        Guid orgId, Guid conversationId, Guid messageId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        await attachmentService.DeleteAsync(
            orgId, conversationId, messageId, attachmentId, userId, cancellationToken);
        return NoContent();
    }
}
