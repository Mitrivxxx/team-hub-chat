using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace team_hub_chat.Services;

public interface ICurrentUserService
{
    Guid GetRequiredUserId();
}

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid GetRequiredUserId()
    {
        var principal = httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var rawUserId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(rawUserId) || !Guid.TryParse(rawUserId, out var userId))
            throw new UnauthorizedAccessException("User is not authenticated.");

        return userId;
    }
}
