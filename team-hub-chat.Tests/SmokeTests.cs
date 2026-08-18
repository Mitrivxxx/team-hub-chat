using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace team_hub_chat.Tests;

public class SmokeTests : IClassFixture<TestChatWebApplicationFactory>
{
    readonly TestChatWebApplicationFactory _factory;
    readonly HttpClient _client;

    public SmokeTests(TestChatWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Swagger_V1_Generates()
    {
        using var scope = _factory.Services.CreateScope();
        var swagger = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();
        var document = swagger.GetSwagger("v1");

        Assert.NotNull(document);
        Assert.NotEmpty(document.Paths);
    }
}
