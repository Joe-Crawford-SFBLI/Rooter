using System.Text.Json.Serialization;

namespace Rooter.Infrastructure.Entities;

/// <summary>
/// Infrastructure entity representing project information.
/// </summary>
public class ProjectInfoEntity
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("restore")]
    public RestoreEntity? Restore { get; set; }

    [JsonPropertyName("frameworks")]
    public Dictionary<string, object>? Frameworks { get; set; }
}