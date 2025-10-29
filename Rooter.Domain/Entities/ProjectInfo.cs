namespace Rooter.Domain.Entities;

/// <summary>
/// Represents project information from the assets file.
/// </summary>
public class ProjectInfo
{
    /// <summary>
    /// The version of the project.
    /// </summary>
    public string Version { get; init; } = "";

    /// <summary>
    /// Whether to restore the project.
    /// </summary>
    public bool Restore { get; init; }

    /// <summary>
    /// Project-specific properties.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}