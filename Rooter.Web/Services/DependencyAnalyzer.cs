using Rooter.Web.Models;

namespace Rooter.Web.Services;

public interface IDependencyAnalyzer
{
    DependencyGraph BuildDependencyGraph(List<PackageReference> packages, string projectName = "", string targetFramework = "");
    DependencyGraph MergeDependencyGraphs(List<DependencyGraph> graphs);
    List<string> FindDependencyPath(DependencyGraph graph, string fromPackage, string toPackage);
    Dictionary<string, List<string>> FindVersionConflicts(DependencyGraph graph);
    List<PackageReference> GetDirectDependencies(DependencyGraph graph);
    List<PackageReference> GetTransitiveDependencies(DependencyGraph graph);
    DependencyGraph FilterByPackage(DependencyGraph graph, string packageName);
}

public class DependencyAnalyzer : IDependencyAnalyzer
{
    public DependencyGraph BuildDependencyGraph(List<PackageReference> packages, string projectName = "", string targetFramework = "")
    {
        var graph = new DependencyGraph
        {
            ProjectName = projectName,
            TargetFramework = targetFramework
        };

        var processedPackages = new HashSet<string>();

        foreach (var package in packages)
        {
            AddPackageToGraph(graph, package, processedPackages, 0, true);
        }

        return graph;
    }

    public DependencyGraph MergeDependencyGraphs(List<DependencyGraph> graphs)
    {
        var mergedGraph = new DependencyGraph
        {
            ProjectName = "Merged Solution",
            TargetFramework = "Multiple"
        };

        foreach (var graph in graphs)
        {
            foreach (var node in graph.Nodes.Values)
            {
                var existingKey = mergedGraph.Nodes.Keys.FirstOrDefault(k =>
                    k.StartsWith($"{node.Name}/"));

                if (existingKey != null)
                {
                    var existingNode = mergedGraph.Nodes[existingKey];
                    // Keep the existing node but track that it appears in multiple projects
                    continue;
                }

                mergedGraph.Nodes[node.Id] = new PackageNode
                {
                    Id = node.Id,
                    Name = node.Name,
                    Version = node.Version,
                    Type = node.Type,
                    Level = node.Level,
                    IsDirectDependency = node.IsDirectDependency
                };
            }

            foreach (var edge in graph.Edges)
            {
                var existingEdge = mergedGraph.Edges.FirstOrDefault(e =>
                    e.Source == edge.Source && e.Target == edge.Target);

                if (existingEdge == null)
                {
                    mergedGraph.Edges.Add(new DependencyEdge
                    {
                        Source = edge.Source,
                        Target = edge.Target,
                        Type = edge.Type,
                        VersionConstraint = edge.VersionConstraint
                    });
                }
            }
        }

        return mergedGraph;
    }

    public List<string> FindDependencyPath(DependencyGraph graph, string fromPackage, string toPackage)
    {
        // Find packages by name (not exact ID)
        var fromNode = graph.Nodes.Values.FirstOrDefault(n =>
            n.Name.Equals(fromPackage, StringComparison.OrdinalIgnoreCase));
        var toNode = graph.Nodes.Values.FirstOrDefault(n =>
            n.Name.Equals(toPackage, StringComparison.OrdinalIgnoreCase));

        if (fromNode == null || toNode == null)
            return new List<string>();

        return graph.GetDependencyPath(fromNode.Id, toNode.Id);
    }

    public Dictionary<string, List<string>> FindVersionConflicts(DependencyGraph graph)
    {
        var conflicts = new Dictionary<string, List<string>>();

        var packageGroups = graph.Nodes.Values.GroupBy(n => n.Name);

        foreach (var group in packageGroups)
        {
            var versions = group.Select(n => n.Version).Distinct().ToList();
            if (versions.Count > 1)
            {
                conflicts[group.Key] = versions.OrderBy(v => v).ToList();
            }
        }

        return conflicts;
    }

    public List<PackageReference> GetDirectDependencies(DependencyGraph graph)
    {
        var directNodes = graph.Nodes.Values.Where(n => n.IsDirectDependency).ToList();

        return directNodes.Select(node => new PackageReference
        {
            Name = node.Name,
            Version = node.Version,
            Type = node.Type
        }).ToList();
    }

    public List<PackageReference> GetTransitiveDependencies(DependencyGraph graph)
    {
        var transitiveNodes = graph.Nodes.Values.Where(n => !n.IsDirectDependency).ToList();

        return transitiveNodes.Select(node => new PackageReference
        {
            Name = node.Name,
            Version = node.Version,
            Type = node.Type
        }).ToList();
    }

    public DependencyGraph FilterByPackage(DependencyGraph graph, string packageName)
    {
        var filteredGraph = new DependencyGraph
        {
            ProjectName = graph.ProjectName,
            TargetFramework = graph.TargetFramework
        };

        // Find the target package
        var targetNode = graph.Nodes.Values.FirstOrDefault(n =>
            n.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

        if (targetNode == null)
            return filteredGraph;

        // Find all packages that depend on the target package (ancestors)
        var ancestors = FindAncestors(graph, targetNode.Id);

        // Find all packages that the target package depends on (descendants)
        var descendants = FindDescendants(graph, targetNode.Id);

        // Include the target package itself
        var relevantNodeIds = new HashSet<string> { targetNode.Id };
        relevantNodeIds.UnionWith(ancestors);
        relevantNodeIds.UnionWith(descendants);

        // Add relevant nodes
        foreach (var nodeId in relevantNodeIds)
        {
            if (graph.Nodes.TryGetValue(nodeId, out var node))
            {
                filteredGraph.Nodes[nodeId] = node;
            }
        }

        // Add relevant edges
        filteredGraph.Edges = graph.Edges
            .Where(e => relevantNodeIds.Contains(e.Source) && relevantNodeIds.Contains(e.Target))
            .ToList();

        return filteredGraph;
    }

    private void AddPackageToGraph(DependencyGraph graph, PackageReference package,
        HashSet<string> processedPackages, int level, bool isDirect)
    {
        var packageId = package.Id;

        if (processedPackages.Contains(packageId))
            return;

        processedPackages.Add(packageId);

        if (!graph.Nodes.ContainsKey(packageId))
        {
            graph.Nodes[packageId] = new PackageNode
            {
                Id = packageId,
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

            if (!graph.Nodes.ContainsKey(dependencyId))
            {
                graph.Nodes[dependencyId] = new PackageNode
                {
                    Id = dependencyId,
                    Name = dependency.Name,
                    Version = dependency.Version,
                    Type = dependency.Type,
                    Level = level + 1,
                    IsDirectDependency = false
                };
            }

            var edge = new DependencyEdge
            {
                Source = packageId,
                Target = dependencyId,
                Type = "dependency"
            };

            if (!graph.Edges.Any(e => e.Source == edge.Source && e.Target == edge.Target))
            {
                graph.Edges.Add(edge);
            }

            // Recursively add sub-dependencies
            AddPackageToGraph(graph, dependency, processedPackages, level + 1, false);
        }
    }

    private HashSet<string> FindAncestors(DependencyGraph graph, string nodeId)
    {
        var ancestors = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(nodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var incomingEdges = graph.Edges.Where(e => e.Target == current);

            foreach (var edge in incomingEdges)
            {
                if (!ancestors.Contains(edge.Source))
                {
                    ancestors.Add(edge.Source);
                    queue.Enqueue(edge.Source);
                }
            }
        }

        return ancestors;
    }

    private HashSet<string> FindDescendants(DependencyGraph graph, string nodeId)
    {
        var descendants = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(nodeId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var outgoingEdges = graph.Edges.Where(e => e.Source == current);

            foreach (var edge in outgoingEdges)
            {
                if (!descendants.Contains(edge.Target))
                {
                    descendants.Add(edge.Target);
                    queue.Enqueue(edge.Target);
                }
            }
        }

        return descendants;
    }
}