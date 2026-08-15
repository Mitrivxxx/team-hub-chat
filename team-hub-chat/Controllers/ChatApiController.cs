using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using team_hub_chat.Configuration;

namespace team_hub_chat.Controllers;

/// <summary>
/// Versioned chat API base. Business controllers inherit this route.
/// </summary>
[ApiController]
[ApiVersion(ChatApiVersions.Current)]
[Route("api/chat/v{version:apiVersion}")]
[Authorize]
public abstract class ChatApiController : ControllerBase;
