using Microsoft.AspNetCore.Mvc;
using team_hub_chat.Dtos;
using team_hub_chat.Services;
using team_hub_chat.Services.Conversations;

namespace team_hub_chat.Controllers.Conversations;

[Route("api/chat/v{version:apiVersion}/organizations/{orgId:guid}/conversations/{conversationId:guid}")]
public sealed class ConversationMembersController(
    ICurrentUserService currentUserService,
    IConversationService conversationService) : ChatApiController
{
    /// <summary>List active conversation members.</summary>
    [HttpGet("members")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(Guid orgId, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var members = await conversationService.ListMembersAsync(orgId, conversationId, userId, cancellationToken);
        return Ok(members);
    }

    /// <summary>Add or re-join a conversation member.</summary>
    [HttpPost("members")]
    [ProducesResponseType(typeof(ConversationMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(
        Guid orgId, Guid conversationId, AddConversationMemberRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var member = await conversationService.AddMemberAsync(orgId, conversationId, request, userId, cancellationToken);
        return CreatedAtVersionedAction(nameof(List), new { orgId, conversationId }, member);
    }

    /// <summary>Change a member role.</summary>
    [HttpPatch("members/{userId:guid}")]
    [ProducesResponseType(typeof(ConversationMemberResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid orgId, Guid conversationId, Guid userId, UpdateConversationMemberRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = currentUserService.GetRequiredUserId();
        var member = await conversationService.UpdateMemberAsync(
            orgId, conversationId, userId, request, actorUserId, cancellationToken);
        return Ok(member);
    }

    /// <summary>Remove a conversation member.</summary>
    [HttpDelete("members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(
        Guid orgId, Guid conversationId, Guid userId, CancellationToken cancellationToken)
    {
        var actorUserId = currentUserService.GetRequiredUserId();
        await conversationService.RemoveMemberAsync(orgId, conversationId, userId, actorUserId, cancellationToken);
        return NoContent();
    }

    /// <summary>Leave a conversation.</summary>
    [HttpPost("leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Leave(Guid orgId, Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        await conversationService.LeaveAsync(orgId, conversationId, userId, cancellationToken);
        return NoContent();
    }
}
