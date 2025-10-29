namespace Rooter.Domain.Entities;

/// <summary>
/// Represents a dependency relationship between two packages.
/// </summary>
public class Dependency
{
    /// <summary>
    /// The package that has the dependency.
    /// </summary>
    public required string FromPackageId { get; init; }

    /// <summary>
    /// The package that is being depended upon.
    /// </summary>
    public required string ToPackageId { get; init; }

    /// <summary>
    /// The type of dependency relationship.
    /// </summary>
    public string Type { get; init; } = "dependency";

    /// <summary>
    /// The version constraint for this dependency.
    /// </summary>
    public string? VersionConstraint { get; init; }
}