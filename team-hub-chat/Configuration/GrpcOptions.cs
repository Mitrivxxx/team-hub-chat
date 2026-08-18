namespace team_hub_chat.Configuration;

public sealed class GrpcOptions
{
    public const string SectionName = "Grpc";

    public string Organization { get; set; } = "http://localhost:5102";
}

