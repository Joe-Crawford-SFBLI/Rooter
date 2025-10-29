using System.Text.Json.Serialization;

namespace Rooter.Infrastructure.Entities;

/// <summary>
/// Infrastructure entity representing a target framework in the assets file.
/// </summary>
public class TargetFrameworkEntity
{
    [JsonPropertyName("dependencies")]
    public Dictionary<string, PackageReferenceEntity>? Dependencies { get; set; }

    [JsonPropertyName("runtime")]
    public Dictionary<string, object>? Runtime { get; set; }

    [JsonPropertyName("compile")]
    public Dictionary<string, object>? Compile { get; set; }
}