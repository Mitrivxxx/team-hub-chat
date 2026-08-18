using DotNetEnv;
using TeamHub.Observability;
using team_hub_chat.Configuration;

if (!string.Equals(
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        Environments.Production,
        StringComparison.OrdinalIgnoreCase))
{
    // NoClobber: Aspire-injected ConnectionStrings/Jwt/OTEL win over local .env.
    Env.NoClobber().TraversePath().Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.AddTeamHubSerilog();

builder.Services.AddTeamHubOpenTelemetry(builder.Configuration, "team-hub-chat", includeEntityFrameworkCore: true);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtConfiguration(builder.Configuration);
builder.Services.AddChatHealthChecks(builder.Configuration);
builder.Services.AddChatGrpc(builder.Configuration);
builder.Services.AddApiInfrastructure();

var app = builder.Build();

await app.ApplyStartupSchemaAsync();

app.UseApiPipeline();
app.MapTeamHubObservabilityEndpoints();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;
