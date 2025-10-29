using System.Text.Json.Serialization;

namespace Rooter.Infrastructure.Entities;

/// <summary>
/// Infrastructure entity representing a library in the assets file.
/// </summary>
public class LibraryEntity
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "package";

    [JsonPropertyName("serviceable")]
    public bool Serviceable { get; set; }

    [JsonPropertyName("sha512")]
    public string? Sha512 { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("files")]
    public List<string>? Files { get; set; }

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string>? Dependencies { get; set; }
}