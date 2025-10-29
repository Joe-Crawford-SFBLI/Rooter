namespace Rooter.Domain.Entities;

/// <summary>
/// Represents the structure of a project.assets.json file.
/// This is a domain representation independent of the actual file format.
/// </summary>
public class ProjectAssets
{
    /// <summary>
    /// The version of the project assets file format.
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// The target frameworks defined in the project.
    /// </summary>
    public IReadOnlyDictionary<string, TargetFramework> Targets { get; init; } = new Dictionary<string, TargetFramework>();

    /// <summary>
    /// The libraries (packages) referenced by the project.
    /// </summary>
    public IReadOnlyDictionary<string, Library> Libraries { get; init; } = new Dictionary<string, Library>();

    /// <summary>
    /// Information about the project itself.
    /// </summary>
    public ProjectInfo? Project { get; init; }

    /// <summary>
    /// Direct dependencies grouped by target framework.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ProjectFileDependencyGroups { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
}