namespace Rooter.Web.Models;

public class DependencyGraph
{
    public Dictionary<string, PackageNode> Nodes { get; set; } = new();
    public List<DependencyEdge> Edges { get; set; } = new();
    public string ProjectName { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;

    public void AddPackage(PackageReference package)
    {
        var nodeKey = package.Id;

        if (!Nodes.ContainsKey(nodeKey))
        {
            Nodes[nodeKey] = new PackageNode
            {
                Id = nodeKey,
                Name = package.Name,
                Version = package.Version,
                Type = package.Type
            };
        }

        // Add dependencies as edges
        foreach (var dependency in package.Dependencies)
        {
            var dependencyKey = dependency.Id;

            if (!Nodes.ContainsKey(dependencyKey))
            {
                Nodes[dependencyKey] = new PackageNode
                {
                    Id = dependencyKey,
                    Name = dependency.Name,
                    Version = dependency.Version,
                    Type = dependency.Type
                };
            }

            var edge = new DependencyEdge
            {
                Source = nodeKey,
                Target = dependencyKey,
                Type = "dependency"
            };

            if (!Edges.Any(e => e.Source == edge.Source && e.Target == edge.Target))
            {
                Edges.Add(edge);
            }

            // Recursively add sub-dependencies
            AddPackage(dependency);
        }
    }

    public List<string> GetDependencyPath(string fromPackage, string toPackage)
    {
        var visited = new HashSet<string>();
        var path = new List<string>();

        if (FindPath(fromPackage, toPackage, visited, path))
        {
            return path;
        }

        return new List<string>();
    }

    private bool FindPath(string current, string target, HashSet<string> visited, List<string> path)
    {
        if (visited.Contains(current))
            return false;

        visited.Add(current);
        path.Add(current);

        if (current == target)
            return true;

        var dependencyEdges = Edges.Where(e => e.Source == current);
        foreach (var edge in dependencyEdges)
        {
            if (FindPath(edge.Target, target, visited, path))
                return true;
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }

    public Dictionary<string, List<string>> GetAllPackageVersions()
    {
        return Nodes.Values
            .GroupBy(n => n.Name)
            .ToDictionary(g => g.Key, g => g.Select(n => n.Version).Distinct().OrderBy(v => v).ToList());
    }
}