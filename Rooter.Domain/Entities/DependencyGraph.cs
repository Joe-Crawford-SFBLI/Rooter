namespace Rooter.Domain.Entities;

/// <summary>
/// Represents a complete dependency graph for a project or solution.
/// </summary>
public class DependencyGraph
{
    /// <summary>
    /// The name of the project this graph represents.
    /// </summary>
    public string ProjectName { get; init; } = "";

    /// <summary>
    /// The target framework for this dependency graph.
    /// </summary>
    public string TargetFramework { get; init; } = "";

    /// <summary>
    /// All packages in the dependency graph.
    /// </summary>
    public IReadOnlyDictionary<string, Package> Packages { get; init; } = new Dictionary<string, Package>();

    /// <summary>
    /// All dependency relationships in the graph.
    /// </summary>
    public IReadOnlyList<Dependency> Dependencies { get; init; } = new List<Dependency>();

    /// <summary>
    /// Finds a dependency path between two packages.
    /// </summary>
    /// <param name="fromPackageId">The starting package ID</param>
    /// <param name="toPackageId">The target package ID</param>
    /// <returns>A list of package IDs representing the path, or empty if no path exists</returns>
    public List<string> GetDependencyPath(
        string fromPackageId,
        string toPackageId)
    {
        var visited = new HashSet<string>();
        var path = new List<string>();

        if (_FindPath(fromPackageId, toPackageId, visited, path))
        {
            return path;
        }

        return new List<string>();
    }

    private bool _FindPath(
        string current,
        string target,
        HashSet<string> visited,
        List<string> path)
    {
        if (current == target)
        {
            path.Add(current);
            return true;
        }

        if (visited.Contains(current))
            return false;

        visited.Add(current);
        path.Add(current);

        var outgoingDependencies = Dependencies
            .Where(d => d.FromPackageId == current)
            .Select(d => d.ToPackageId);

        foreach (var dependency in outgoingDependencies)
        {
            if (_FindPath(dependency, target, visited, path))
                return true;
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }
}