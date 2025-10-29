namespace Rooter.Web.Models;

public class PackageNode
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public string Type { get; set; } = "package";
    public int Level { get; set; } = 0; // Depth in dependency tree
    public bool IsDirectDependency { get; set; } = false;
}