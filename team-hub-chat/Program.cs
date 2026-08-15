using DotNetEnv;
using TeamHub.Observability;
using team_hub_chat.Configuration;

if (!string.Equals(
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        Environments.Production,
        StringComparison.OrdinalIgnoreCase))
{
    // NoClobber: Aspire-injected Jwt/OTEL win over local .env.
    Env.NoClobber().TraversePath().Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.AddTeamHubSerilog();

builder.Services.AddTeamHubOpenTelemetry(builder.Configuration, "team-hub-chat", includeEntityFrameworkCore: false);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.AddChatHealthChecks();
builder.Services.AddApiInfrastructure();

var app = builder.Build();

app.UseApiPipeline();
app.MapTeamHubObservabilityEndpoints();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
