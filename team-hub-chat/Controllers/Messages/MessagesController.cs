using Microsoft.AspNetCore.Mvc;
using team_hub_chat.Dtos;
using team_hub_chat.Services;
using team_hub_chat.Services.Messages;

namespace team_hub_chat.Controllers.Messages;

[Route("api/chat/v{version:apiVersion}/organizations/{orgId:guid}/conversations/{conversationId:guid}/messages")]
public sealed class MessagesController(
    ICurrentUserService currentUserService,
    IMessageService messageService) : ChatApiController
{
    /// <summary>List messages with a createdAt cursor.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MessagePageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        Guid orgId,
        Guid conversationId,
        [FromQuery] Guid? before,
        [FromQuery] Guid? after,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetRequiredUserId();
        var result = await messageService.ListAsync(
            orgId, conversationId, userId, before, after, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a message.</summary>
    [HttpGet("{messageId:guid}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid orgId, Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var message = await messageService.GetAsync(orgId, conversationId, messageId, userId, cancellationToken);
        return Ok(message);
    }

    /// <summary>Send a message.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid orgId, Guid conversationId, CreateMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var message = await messageService.CreateAsync(orgId, conversationId, request, userId, cancellationToken);
        return CreatedAtVersionedAction(
            nameof(GetById),
            new { orgId, conversationId, messageId = message.Id },
            message);
    }

    /// <summary>Edit a message.</summary>
    [HttpPatch("{messageId:guid}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid orgId, Guid conversationId, Guid messageId, UpdateMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var message = await messageService.UpdateAsync(
            orgId, conversationId, messageId, request, userId, cancellationToken);
        return Ok(message);
    }

    /// <summary>Soft-delete a message.</summary>
    [HttpDelete("{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid orgId, Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        await messageService.DeleteAsync(orgId, conversationId, messageId, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>Set the current user's reaction.</summary>
    [HttpPut("{messageId:guid}/reactions")]
    [ProducesResponseType(typeof(MessageReactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetReaction(
        Guid orgId, Guid conversationId, Guid messageId, SetMessageReactionRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var reaction = await messageService.SetReactionAsync(
            orgId, conversationId, messageId, request, userId, cancellationToken);
        return Ok(reaction);
    }

    /// <summary>Remove the current user's reaction.</summary>
    [HttpDelete("{messageId:guid}/reactions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReaction(
        Guid orgId, Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        await messageService.DeleteReactionAsync(orgId, conversationId, messageId, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>Mark a message as read.</summary>
    [HttpPost("{messageId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(
        Guid orgId, Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        await messageService.MarkReadAsync(orgId, conversationId, messageId, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>List read receipts for a message.</summary>
    [HttpGet("{messageId:guid}/reads")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageReadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListReads(
        Guid orgId, Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var reads = await messageService.ListReadsAsync(orgId, conversationId, messageId, userId, cancellationToken);
        return Ok(reads);
    }
}
