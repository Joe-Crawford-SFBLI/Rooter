namespace Rooter.Web.Models;

public class DependencyEdge
{
    public required string Source { get; set; }
    public required string Target { get; set; }
    public string Type { get; set; } = "dependency";
    public string? VersionConstraint { get; set; }
}