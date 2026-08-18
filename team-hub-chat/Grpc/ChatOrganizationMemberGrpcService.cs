using Grpc.Core;
using TeamHub.GrpcContracts.Organization.V1;

namespace team_hub_chat.Grpc;

/// <summary>
/// Internal gRPC proxy that exposes organization members for chat consumers.
/// </summary>
public sealed class ChatOrganizationMemberGrpcService(
    IOrganizationMemberGrpcProxyClient organizationMemberClient)
    : OrganizationMemberService.OrganizationMemberServiceBase
{
    /// <summary>List organization members for an authenticated actor.</summary>
    public override Task<ListMembersResponse> ListMembers(
        ListMembersRequest request,
        ServerCallContext context)
    {
        // Pass through to the organization service; validation/errors are handled there.
        return organizationMemberClient.ListMembersAsync(request, context.CancellationToken);
    }
}

