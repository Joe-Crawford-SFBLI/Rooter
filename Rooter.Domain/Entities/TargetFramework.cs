namespace Rooter.Domain.Entities;

/// <summary>
/// Represents a target framework in the project assets.
/// </summary>
public class TargetFramework
{
    /// <summary>
    /// Direct dependencies for this target framework.
    /// </summary>
    public IReadOnlyDictionary<string, PackageReference> Dependencies { get; init; } = new Dictionary<string, PackageReference>();
}