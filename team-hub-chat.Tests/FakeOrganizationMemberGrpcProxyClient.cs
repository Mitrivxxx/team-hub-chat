using Grpc.Core;
using TeamHub.GrpcContracts.Organization.V1;
using team_hub_chat.Grpc;

namespace team_hub_chat.Tests;

public sealed class FakeOrganizationMemberGrpcProxyClient : IOrganizationMemberGrpcProxyClient
{
    public HashSet<Guid> Members { get; } = [];
    public bool DenyAll { get; set; }

    public Task<ListMembersResponse> ListMembersAsync(
        ListMembersRequest request,
        CancellationToken cancellationToken = default)
    {
        if (DenyAll
            || !Guid.TryParse(request.ActorUserId, out var actorId)
            || (Members.Count > 0 && !Members.Contains(actorId)))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "not a member"));
        }

        var response = new ListMembersResponse();
        var ids = Members.Count > 0 ? Members : [actorId];
        foreach (var id in ids)
        {
            response.Members.Add(new Member
            {
                UserId = id.ToString(),
                JoinedAt = DateTimeOffset.UtcNow.ToString("O")
            });
        }

        return Task.FromResult(response);
    }
}
