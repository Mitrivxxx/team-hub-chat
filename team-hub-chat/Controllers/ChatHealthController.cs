using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace team_hub_chat.Controllers;

public sealed class ChatHealthController(HealthCheckService healthChecks) : ChatApiController
{
    /// <summary>REST health (versioned).</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var report = await healthChecks.CheckHealthAsync(cancellationToken);
        return report.Status == HealthStatus.Healthy
            ? Ok()
            : StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}
