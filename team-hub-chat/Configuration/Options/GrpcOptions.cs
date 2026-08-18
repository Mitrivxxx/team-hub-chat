using System.ComponentModel.DataAnnotations;

namespace team_hub_chat.Configuration.Options;

public sealed class GrpcOptions
{
    public const string SectionName = "Grpc";

    [Required]
    public string Organization { get; set; } = "";
}
