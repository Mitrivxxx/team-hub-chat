using TeamHub.Observability;
using TeamHub.Observability.Middleware;
using team_hub_chat.Services;

namespace team_hub_chat.Configuration;

public sealed class ChatExceptionMapper : IExceptionProblemDetailsMapper
{
    public bool TryMap(Exception exception, out ExceptionMapping mapping)
    {
        switch (exception)
        {
            case ChatNotFoundException ex:
                mapping = new ExceptionMapping(
                    StatusCodes.Status404NotFound,
                    "Not found.",
                    ex.Message,
                    ProblemTypes.For("chat-not-found"),
                    PreferMappedDetail: true);
                return true;

            case ChatAccessException ex:
                mapping = new ExceptionMapping(
                    StatusCodes.Status403Forbidden,
                    "Forbidden.",
                    ex.Message,
                    ProblemTypes.Forbidden,
                    PreferMappedDetail: true);
                return true;

            case ChatConflictException ex:
                mapping = new ExceptionMapping(
                    StatusCodes.Status409Conflict,
                    "Conflict.",
                    ex.Message,
                    ProblemTypes.Conflict,
                    PreferMappedDetail: true);
                return true;

            case ChatValidationException ex:
                mapping = new ExceptionMapping(
                    StatusCodes.Status400BadRequest,
                    "Bad request.",
                    ex.Message,
                    ProblemTypes.ValidationFailed,
                    PreferMappedDetail: true);
                return true;

            case ChatStorageUnavailableException or ChatServiceUnavailableException:
                mapping = new ExceptionMapping(
                    StatusCodes.Status503ServiceUnavailable,
                    "Service unavailable.",
                    exception.Message,
                    ProblemTypes.ServiceUnavailable,
                    PreferMappedDetail: true);
                return true;

            case UnauthorizedAccessException unauthorized:
                mapping = new ExceptionMapping(
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized.",
                    unauthorized.Message,
                    ProblemTypes.Unauthorized,
                    PreferMappedDetail: true);
                return true;

            default:
                mapping = default;
                return false;
        }
    }
}
