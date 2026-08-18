using System.Net;
using System.Net.Http.Json;
using team_hub_chat.Dtos;

namespace team_hub_chat.Tests;

public class ChatApiTests : IClassFixture<TestChatWebApplicationFactory>
{
    readonly TestChatWebApplicationFactory _factory;

    public ChatApiTests(TestChatWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListConversations_WithoutToken_ReturnsUnauthorized()
    {
        var orgId = Guid.NewGuid();
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/chat/v1/organizations/{orgId}/conversations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListConversations_WhenNotOrgMember_ReturnsForbidden()
    {
        _factory.OrganizationMembers.DenyAll = true;
        try
        {
            var orgId = Guid.NewGuid();
            var client = _factory.CreateAuthenticatedClient(Guid.NewGuid());
            var response = await client.GetAsync($"/api/chat/v1/organizations/{orgId}/conversations");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            _factory.OrganizationMembers.DenyAll = false;
        }
    }

    [Fact]
    public async Task CreateGroup_ThenList_ReturnsConversation()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        _factory.OrganizationMembers.Members.Add(userId);
        _factory.OrganizationMembers.Members.Add(otherId);

        var client = _factory.CreateAuthenticatedClient(userId);
        var create = await client.PostAsJsonAsync(
            $"/api/chat/v1/organizations/{orgId}/conversations",
            new CreateConversationRequest
            {
                Type = "GROUP",
                Name = "Engineering",
                MemberUserIds = [otherId]
            });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var conversation = await create.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);
        Assert.Equal("GROUP", conversation.Type);
        Assert.Equal("Engineering", conversation.Name);
        Assert.Equal("OWNER", conversation.MyRole);

        var list = await client.GetFromJsonAsync<ConversationPageResponse>(
            $"/api/chat/v1/organizations/{orgId}/conversations");
        Assert.NotNull(list);
        Assert.Contains(list.Items, c => c.Id == conversation.Id);
    }

    [Fact]
    public async Task SendMessage_AndReact_Succeeds()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(userId);

        var created = await client.PostAsJsonAsync(
            $"/api/chat/v1/organizations/{orgId}/conversations",
            new CreateConversationRequest { Type = "GROUP", Name = "Chat" });
        var conversation = await created.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);

        var send = await client.PostAsJsonAsync(
            $"/api/chat/v1/organizations/{orgId}/conversations/{conversation.Id}/messages",
            new CreateMessageRequest { Content = "hello", MessageType = "TEXT" });
        Assert.Equal(HttpStatusCode.Created, send.StatusCode);
        var message = await send.Content.ReadFromJsonAsync<MessageResponse>();
        Assert.NotNull(message);
        Assert.Equal("hello", message.Content);

        var react = await client.PutAsJsonAsync(
            $"/api/chat/v1/organizations/{orgId}/conversations/{conversation.Id}/messages/{message.Id}/reactions",
            new SetMessageReactionRequest { Reaction = "👍" });
        Assert.Equal(HttpStatusCode.OK, react.StatusCode);
    }

    [Fact]
    public async Task Leave_LastOwner_ReturnsConflict()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(userId);

        var created = await client.PostAsJsonAsync(
            $"/api/chat/v1/organizations/{orgId}/conversations",
            new CreateConversationRequest { Type = "GROUP", Name = "Solo" });
        var conversation = await created.Content.ReadFromJsonAsync<ConversationResponse>();
        Assert.NotNull(conversation);

        var leave = await client.PostAsync(
            $"/api/chat/v1/organizations/{orgId}/conversations/{conversation.Id}/leave",
            null);
        Assert.Equal(HttpStatusCode.Conflict, leave.StatusCode);
    }
}
