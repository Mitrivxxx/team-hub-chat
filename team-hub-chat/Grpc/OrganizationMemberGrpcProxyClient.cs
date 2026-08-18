using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using TeamHub.GrpcContracts.Organization.V1;
using team_hub_chat.Configuration;

namespace team_hub_chat.Grpc;

public interface IOrganizationMemberGrpcProxyClient
{
    Task<ListMembersResponse> ListMembersAsync(
        ListMembersRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class OrganizationMemberGrpcProxyClient : IOrganizationMemberGrpcProxyClient, IDisposable
{
    readonly GrpcChannel _channel;
    readonly OrganizationMemberService.OrganizationMemberServiceClient _client;

    public OrganizationMemberGrpcProxyClient(IOptions<GrpcOptions> options)
    {
        _channel = GrpcChannel.ForAddress(options.Value.Organization);
        _client = new OrganizationMemberService.OrganizationMemberServiceClient(_channel);
    }

    public Task<ListMembersResponse> ListMembersAsync(
        ListMembersRequest request,
        CancellationToken cancellationToken = default) =>
        _client.ListMembersAsync(request, cancellationToken: cancellationToken).ResponseAsync;

    public void Dispose() => _channel.Dispose();
}

