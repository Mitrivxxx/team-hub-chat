using Grpc.Core;
using TeamHub.GrpcContracts.Organization.V1;
using team_hub_chat.Grpc;

namespace team_hub_chat.Services;

public interface IChatOrganizationAccess
{
    Task EnsureOrgMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);

    Task EnsureOrgMembersAsync(
        Guid organizationId,
        Guid actorUserId,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}

public sealed class ChatOrganizationAccess(IOrganizationMemberGrpcProxyClient client) : IChatOrganizationAccess
{
    public async Task EnsureOrgMemberAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await ListMembersAsync(organizationId, userId, cancellationToken);
    }

    public async Task EnsureOrgMembersAsync(
        Guid organizationId,
        Guid actorUserId,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var response = await ListMembersAsync(organizationId, actorUserId, cancellationToken);
        var memberIds = response.Members
            .Select(m => Guid.TryParse(m.UserId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        foreach (var userId in userIds.Distinct())
        {
            if (userId == actorUserId)
                continue;
            if (!memberIds.Contains(userId))
                throw new ChatValidationException("User is not a member of this organization.");
        }
    }

    async Task<ListMembersResponse> ListMembersAsync(
        Guid organizationId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.ListMembersAsync(
                new ListMembersRequest
                {
                    OrganizationId = organizationId.ToString(),
                    ActorUserId = actorUserId.ToString()
                },
                cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            throw new ChatAccessException("User is not a member of this organization.");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new ChatNotFoundException("Organization was not found.");
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new ChatServiceUnavailableException("Organization service is unavailable.");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ChatValidationException(ex.Status.Detail);
        }
    }
}
