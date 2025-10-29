using Rooter.Domain.Entities;

namespace Rooter.Application.Services;

/// <summary>
/// Interface for dependency analysis operations.
/// Contains the business logic for building and analyzing dependency graphs.
/// </summary>
public interface IDependencyAnalyzer
{
    /// <summary>
    /// Builds a dependency graph from a list of package references.
    /// </summary>
    /// <param name="packages">The package references to analyze</param>
    /// <param name="projectName">The name of the project</param>
    /// <param name="targetFramework">The target framework</param>
    /// <returns>A complete dependency graph</returns>
    DependencyGraph BuildDependencyGraph(
        List<PackageReference> packages,
        string projectName = "",
        string targetFramework = "");

    /// <summary>
    /// Merges multiple dependency graphs into a single consolidated graph.
    /// </summary>
    /// <param name="graphs">The dependency graphs to merge</param>
    /// <returns>A merged dependency graph</returns>
    DependencyGraph MergeDependencyGraphs(List<DependencyGraph> graphs);

    /// <summary>
    /// Finds a dependency path between two packages in a graph.
    /// </summary>
    /// <param name="graph">The dependency graph to search</param>
    /// <param name="fromPackage">The source package name</param>
    /// <param name="toPackage">The target package name</param>
    /// <returns>A list of package names representing the path</returns>
    List<string> FindDependencyPath(
        DependencyGraph graph,
        string fromPackage,
        string toPackage);

    /// <summary>
    /// Finds version conflicts in a dependency graph.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze</param>
    /// <returns>A dictionary of package names to conflicting versions</returns>
    Dictionary<string, List<string>> FindVersionConflicts(DependencyGraph graph);

    /// <summary>
    /// Gets all direct dependencies from a dependency graph.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze</param>
    /// <returns>A list of direct dependencies</returns>
    List<PackageReference> GetDirectDependencies(DependencyGraph graph);

    /// <summary>
    /// Gets all transitive dependencies from a dependency graph.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze</param>
    /// <returns>A list of transitive dependencies</returns>
    List<PackageReference> GetTransitiveDependencies(DependencyGraph graph);

    /// <summary>
    /// Filters a dependency graph to show only packages related to a specific package.
    /// </summary>
    /// <param name="graph">The dependency graph to filter</param>
    /// <param name="packageName">The package name to filter by</param>
    /// <returns>A filtered dependency graph</returns>
    DependencyGraph FilterByPackage(
        DependencyGraph graph,
        string packageName);
}

/// <summary>
/// Application service for dependency analysis operations.
/// Contains the core business logic for building and analyzing dependency graphs.
/// </summary>
public class DependencyAnalyzerService : IDependencyAnalyzer
{
    /// <summary>
    /// Builds a dependency graph from a list of package references.
    /// </summary>
    /// <param name="packages">The package references to analyze</param>
    /// <param name="projectName">The name of the project</param>
    /// <param name="targetFramework">The target framework</param>
    /// <returns>A complete dependency graph</returns>
    public DependencyGraph BuildDependencyGraph(
        List<PackageReference> packages,
        string projectName = "",
        string targetFramework = "")
    {
        var graphPackages = new Dictionary<string, Package>();
        var dependencies = new List<Dependency>();
        var processedPackages = new HashSet<string>();

        foreach (var package in packages)
        {
            _AddPackageToGraph(
                package,
                graphPackages,
                dependencies,
                processedPackages,
                0,
                true);
        }

        return new DependencyGraph
        {
            ProjectName = projectName,
            TargetFramework = targetFramework,
            Packages = graphPackages,
            Dependencies = dependencies
        };
    }

    /// <summary>
    /// Merges multiple dependency graphs into a single consolidated graph.
    /// </summary>
    /// <param name="graphs">The dependency graphs to merge</param>
    /// <returns>A merged dependency graph</returns>
    public DependencyGraph MergeDependencyGraphs(List<DependencyGraph> graphs)
    {
        var mergedPackages = new Dictionary<string, Package>();
        var mergedDependencies = new List<Dependency>();

        foreach (var graph in graphs)
        {
            foreach (var package in graph.Packages.Values)
            {
                if (!mergedPackages.ContainsKey(package.Id))
                {
                    mergedPackages[package.Id] = package;
                }
            }

            foreach (var dependency in graph.Dependencies)
            {
                var existingDependency = mergedDependencies
                    .FirstOrDefault(d => d.FromPackageId == dependency.FromPackageId &&
                                        d.ToPackageId == dependency.ToPackageId);

                if (existingDependency == null)
                {
                    mergedDependencies.Add(dependency);
                }
            }
        }

        return new DependencyGraph
        {
            ProjectName = "Merged Solution",
            TargetFramework = "Multiple",
            Packages = mergedPackages,
            Dependencies = mergedDependencies
        };
    }

    /// <summary>
    /// Finds a dependency path between two packages in a graph.
    /// </summary>
    /// <param name="graph">The dependency graph to search</param>
    /// <param name="fromPackage">The source package name</param>
    /// <param name="toPackage">The target package name</param>
    /// <returns>A list of package names representing the path</returns>
    public List<string> FindDependencyPath(
        DependencyGraph graph,
        string fromPackage,
        string toPackage)
    {
        var fromNode = graph.Packages.Values
            .FirstOrDefault(p => p.Name.Equals(fromPackage, StringComparison.OrdinalIgnoreCase));
        var toNode = graph.Packages.Values
            .FirstOrDefault(p => p.Name.Equals(toPackage, StringComparison.OrdinalIgnoreCase));

        if (fromNode == null || toNode == null)
            return new List<string>();

        return graph.GetDependencyPath(fromNode.Id, toNode.Id);
    }

    /// <summary>
    /// Finds version conflicts in a dependency graph.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze</param>
    /// <returns>A dictionary of package names to conflicting versions</returns>
    public Dictionary<string, List<string>> FindVersionConflicts(DependencyGraph graph)
    {
        var conflicts = new Dictionary<string, List<string>>();

        var packageGroups = graph.Packages.Values
            .GroupBy(p => p.Name);

        foreach (var group in packageGroups)
        {
            var versions = group
                .Select(p => p.Version)
                .Distinct()
                .ToList();

            if (versions.Count > 1)
            {
                conflicts[group.Key] = versions
                    .OrderBy(v => v)
                    .ToList();
            }
        }

        return conflicts;
    }

    /// <summary>
    /// Gets all direct dependencies from a dependency graph.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze</param>
    /// <returns>A list of direct dependencies</returns>
    public List<PackageReference> GetDirectDependencies(DependencyGraph graph)
    {
        var directPackages = graph.Packages.Values
            .Where(p => p.IsDirectDependency)
            .ToList();

        return directPackages
            .Select(package => new PackageReference
            {
                Name = package.Name,
                Version = package.Version,
                Type = package.Type
            })
            .ToList();
    }

    /// <summary>
    /// Gets all transitive dependencies from a dependency graph.
    /// </summary>
    /// <param name="graph">The dependency graph to analyze</param>
    /// <returns>A list of transitive dependencies</returns>
    public List<PackageReference> GetTransitiveDependencies(DependencyGraph graph)
    {
        var transitivePackages = graph.Packages.Values
            .Where(p => !p.IsDirectDependency)
            .ToList();

        return transitivePackages
            .Select(package => new PackageReference
            {
                Name = package.Name,
                Version = package.Version,
                Type = package.Type
            })
            .ToList();
    }

    /// <summary>
    /// Filters a dependency graph to show only packages related to a specific package.
    /// </summary>
    /// <param name="graph">The dependency graph to filter</param>
    /// <param name="packageName">The package name to filter by</param>
    /// <returns>A filtered dependency graph</returns>
    public DependencyGraph FilterByPackage(
        DependencyGraph graph,
        string packageName)
    {
        var targetPackage = graph.Packages.Values
            .FirstOrDefault(p => p.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

        if (targetPackage == null)
        {
            return new DependencyGraph
            {
                ProjectName = graph.ProjectName,
                TargetFramework = graph.TargetFramework
            };
        }

        var ancestors = _FindAncestors(graph, targetPackage.Id);
        var descendants = _FindDescendants(graph, targetPackage.Id);

        var relevantPackageIds = new HashSet<string> { targetPackage.Id };
        relevantPackageIds.UnionWith(ancestors);
        relevantPackageIds.UnionWith(descendants);

        var filteredPackages = graph.Packages
            .Where(kvp => relevantPackageIds.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var filteredDependencies = graph.Dependencies
            .Where(d => relevantPackageIds.Contains(d.FromPackageId) &&
                       relevantPackageIds.Contains(d.ToPackageId))
            .ToList();

        return new DependencyGraph
        {
            ProjectName = graph.ProjectName,
            TargetFramework = graph.TargetFramework,
            Packages = filteredPackages,
            Dependencies = filteredDependencies
        };
    }

    private void _AddPackageToGraph(
        PackageReference package,
        Dictionary<string, Package> packages,
        List<Dependency> dependencies,
        HashSet<string> processedPackages,
        int level,
        bool isDirect)
    {
        var packageId = package.Id;

        if (processedPackages.Contains(packageId))
            return;

        processedPackages.Add(packageId);

        if (!packages.ContainsKey(packageId))
        {
            packages[packageId] = new Package
            {
                Name = package.Name,
                Version = package.Version,
                Type = package.Type,
                Level = level,
                IsDirectDependency = isDirect
            };
        }

        foreach (var dependency in package.Dependencies)
        {
            var dependencyId = dependency.Id;

            if (!packages.ContainsKey(dependencyId))
            {
                packages[dependencyId] = new Package
                {
                    Name = dependency.Name,
                    Version = dependency.Version,
                    Type = dependency.Type,
                    Level = level + 1,
                    IsDirectDependency = false
                };
            }

            var depRelation = new Dependency
            {
                FromPackageId = packageId,
                ToPackageId = dependencyId,
                Type = "dependency"
            };

            if (!dependencies.Any(d => d.FromPackageId == depRelation.FromPackageId &&
                                      d.ToPackageId == depRelation.ToPackageId))
            {
                dependencies.Add(depRelation);
            }

            _AddPackageToGraph(
                dependency,
                packages,
                dependencies,
                processedPackages,
                level + 1,
                false);
        }
    }

    private HashSet<string> _FindAncestors(
        DependencyGraph graph,
        string packageId)
    {
        var ancestors = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(packageId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var incomingDependencies = graph.Dependencies
                .Where(d => d.ToPackageId == current);

            foreach (var dependency in incomingDependencies)
            {
                if (!ancestors.Contains(dependency.FromPackageId))
                {
                    ancestors.Add(dependency.FromPackageId);
                    queue.Enqueue(dependency.FromPackageId);
                }
            }
        }

        return ancestors;
    }

    private HashSet<string> _FindDescendants(
        DependencyGraph graph,
        string packageId)
    {
        var descendants = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(packageId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var outgoingDependencies = graph.Dependencies
                .Where(d => d.FromPackageId == current);

            foreach (var dependency in outgoingDependencies)
            {
                if (!descendants.Contains(dependency.ToPackageId))
                {
                    descendants.Add(dependency.ToPackageId);
                    queue.Enqueue(dependency.ToPackageId);
                }
            }
        }

        return descendants;
    }
}