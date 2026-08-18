using Microsoft.AspNetCore.Mvc;
using team_hub_chat.Dtos;
using team_hub_chat.Services;
using team_hub_chat.Services.Conversations;

namespace team_hub_chat.Controllers.Conversations;

[Route("api/chat/v{version:apiVersion}/organizations/{orgId:guid}/conversations")]
public sealed class ConversationsController(
    ICurrentUserService currentUserService,
    IConversationService conversationService) : ChatApiController
{
    /// <summary>List conversations for the current user in an organization.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ConversationPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        Guid orgId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.GetRequiredUserId();
        var result = await conversationService.ListAsync(orgId, userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Create a DIRECT or GROUP conversation.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid orgId, CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var conversation = await conversationService.CreateAsync(orgId, request, userId, cancellationToken);
        return CreatedAtVersionedAction(
            nameof(GetById),
            new { orgId, conversationId = conversation.Id },
            conversation);
    }

    /// <summary>Get a conversation.</summary>
    [HttpGet("{conversationId:guid}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid orgId, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var conversation = await conversationService.GetAsync(orgId, conversationId, userId, cancellationToken);
        return Ok(conversation);
    }

    /// <summary>Rename a GROUP conversation.</summary>
    [HttpPatch("{conversationId:guid}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid orgId, Guid conversationId, UpdateConversationRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var conversation = await conversationService.UpdateAsync(orgId, conversationId, request, userId, cancellationToken);
        return Ok(conversation);
    }

    /// <summary>Soft-delete a conversation.</summary>
    [HttpDelete("{conversationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid orgId, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        await conversationService.DeleteAsync(orgId, conversationId, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>Get the current user's pin in a conversation.</summary>
    [HttpGet("{conversationId:guid}/pin")]
    [ProducesResponseType(typeof(ConversationPinResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPin(Guid orgId, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var pin = await conversationService.GetPinAsync(orgId, conversationId, userId, cancellationToken);
        return Ok(pin);
    }

    /// <summary>Pin a message for the current user.</summary>
    [HttpPut("{conversationId:guid}/pin")]
    [ProducesResponseType(typeof(ConversationPinResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPin(
        Guid orgId, Guid conversationId, SetConversationPinRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var pin = await conversationService.SetPinAsync(orgId, conversationId, request, userId, cancellationToken);
        return Ok(pin);
    }

    /// <summary>Remove the current user's pin.</summary>
    [HttpDelete("{conversationId:guid}/pin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePin(Guid orgId, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        await conversationService.DeletePinAsync(orgId, conversationId, userId, cancellationToken);
        return NoContent();
    }

    /// <summary>Get notification settings for the current user.</summary>
    [HttpGet("{conversationId:guid}/settings")]
    [ProducesResponseType(typeof(ConversationSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSettings(Guid orgId, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var settings = await conversationService.GetSettingsAsync(orgId, conversationId, userId, cancellationToken);
        return Ok(settings);
    }

    /// <summary>Update notification settings for the current user.</summary>
    [HttpPatch("{conversationId:guid}/settings")]
    [ProducesResponseType(typeof(ConversationSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings(
        Guid orgId, Guid conversationId, UpdateConversationSettingsRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var settings = await conversationService.UpdateSettingsAsync(orgId, conversationId, request, userId, cancellationToken);
        return Ok(settings);
    }
}
