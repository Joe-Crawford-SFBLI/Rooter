namespace Rooter.Domain.Entities;

/// <summary>
/// Represents a NuGet package in the domain model.
/// This is a pure domain entity without infrastructure concerns.
/// </summary>
public class Package
{
    /// <summary>
    /// The unique identifier for this package (Name/Version combination).
    /// </summary>
    public string Id => $"{Name}/{Version}";

    /// <summary>
    /// The name of the package.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The version of the package.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The type of package (e.g., "package", "project", etc.).
    /// </summary>
    public string Type { get; init; } = "package";

    /// <summary>
    /// Whether this is a direct dependency of the project.
    /// </summary>
    public bool IsDirectDependency { get; init; }

    /// <summary>
    /// The level in the dependency tree (0 for direct dependencies).
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// The packages that this package depends on.
    /// </summary>
    public IReadOnlyList<Package> Dependencies { get; init; } = new List<Package>();
}