using System.Text.Json.Serialization;

namespace Rooter.Infrastructure.Entities;

/// <summary>
/// Infrastructure entity representing the raw structure of a project.assets.json file.
/// This maps 1:1 to the actual JSON file format from NuGet.
/// </summary>
public class ProjectAssetsEntity
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("targets")]
    public Dictionary<string, TargetFrameworkEntity> Targets { get; set; } = new();

    [JsonPropertyName("libraries")]
    public Dictionary<string, LibraryEntity> Libraries { get; set; } = new();

    [JsonPropertyName("project")]
    public ProjectInfoEntity? Project { get; set; }

    [JsonPropertyName("projectFileDependencyGroups")]
    public Dictionary<string, List<string>>? ProjectFileDependencyGroups { get; set; }
}