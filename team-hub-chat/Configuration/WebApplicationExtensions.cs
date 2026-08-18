using Asp.Versioning.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using TeamHub.Observability;
using team_hub_chat.Data;
using team_hub_chat.Grpc;

namespace team_hub_chat.Configuration;

public static class WebApplicationExtensions
{
    public static async Task ApplyStartupSchemaAsync(this WebApplication app)
    {
        if (app.Environment.IsEnvironment("Testing"))
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await db.Database.MigrateAsync();
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseTeamHubExceptionHandling();
        app.UseTeamHubCorrelationId();
        app.UseTeamHubSessionId();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseTeamHubUserIdLogging();
        app.UseSerilogRequestLoggingExcludingHealth();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(options =>
            {
                options.PreSerializeFilters.Add(static (_, request) =>
                    request.HttpContext.Response.Headers.CacheControl = "no-store");
            });
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"Team Hub Chat API {description.GroupName}");
                }
            });
        }

        app.MapHealthChecks("/health");
        app.MapGrpcService<ChatOrganizationMemberGrpcService>();
        app.MapControllers();

        return app;
    }
}
