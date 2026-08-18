namespace team_hub_chat.Services;

public sealed class ChatNotFoundException : Exception
{
    public ChatNotFoundException(string message = "Resource was not found.") : base(message) { }
}

public sealed class ChatAccessException : Exception
{
    public ChatAccessException(string message) : base(message) { }
}

public sealed class ChatConflictException : Exception
{
    public ChatConflictException(string message) : base(message) { }
}

public sealed class ChatValidationException : Exception
{
    public ChatValidationException(string message) : base(message) { }
}

public sealed class ChatStorageUnavailableException : Exception
{
    public ChatStorageUnavailableException(string message = "Blob storage is not configured.") : base(message) { }
}

public sealed class ChatServiceUnavailableException : Exception
{
    public ChatServiceUnavailableException(string message) : base(message) { }
}
