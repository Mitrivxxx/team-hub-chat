using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using TeamHub.BlobStorage;
using team_hub_chat.Data;
using team_hub_chat.Grpc;

namespace team_hub_chat.Tests;

public sealed class TestChatWebApplicationFactory : WebApplicationFactory<Program>
{
    readonly string _databaseName = $"team-hub-chat-tests-{Guid.NewGuid():N}";

    public FakeOrganizationMemberGrpcProxyClient OrganizationMembers { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5433;Database=chat_test;Username=test");
        builder.UseSetting("Jwt:Key", TestJwtConfiguration.Key);
        builder.UseSetting("Jwt:Issuer", TestJwtConfiguration.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwtConfiguration.Audience);
        builder.UseSetting("Grpc:Organization", "http://127.0.0.1:9");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<ChatDbContext>>();
            services.RemoveAll<DbContextOptions<ChatDbContext>>();
            services.RemoveAll<ChatDbContext>();

            services.AddDbContext<ChatDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IOrganizationMemberGrpcProxyClient>();
            services.AddSingleton<IOrganizationMemberGrpcProxyClient>(OrganizationMembers);

            services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
            services.AddHealthChecks();
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, FakeBlobStorageService>();
        });
    }

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        OrganizationMembers.Members.Add(userId);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateAccessToken(userId));
        return client;
    }

    public static string CreateAccessToken(Guid userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtConfiguration.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestJwtConfiguration.Issuer,
            audience: TestJwtConfiguration.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, "test-user")
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
