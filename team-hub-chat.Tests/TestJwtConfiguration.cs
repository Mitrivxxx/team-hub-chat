namespace team_hub_chat.Tests;

internal static class TestJwtConfiguration
{
    public const string Key = "test-secret-key-at-least-32-characters-long";
    public const string Issuer = "AuthService";
    public const string Audience = "AuthServiceUsers";
}
