namespace Rooter.Domain.Entities;

/// <summary>
/// Represents a package reference in the dependency graph.
/// </summary>
public class PackageReference
{
    /// <summary>
    /// The unique identifier for this package reference.
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
    /// The type of package reference.
    /// </summary>
    public string Type { get; init; } = "package";

    /// <summary>
    /// The dependencies of this package.
    /// </summary>
    public IReadOnlyList<PackageReference> Dependencies { get; init; } = new List<PackageReference>();
}