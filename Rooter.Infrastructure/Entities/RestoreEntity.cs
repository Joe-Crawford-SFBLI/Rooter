using System.Text.Json.Serialization;

namespace Rooter.Infrastructure.Entities;

/// <summary>
/// Infrastructure entity representing restore information.
/// </summary>
public class RestoreEntity
{
    [JsonPropertyName("projectUniqueName")]
    public string? ProjectUniqueName { get; set; }

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("projectPath")]
    public string? ProjectPath { get; set; }
}