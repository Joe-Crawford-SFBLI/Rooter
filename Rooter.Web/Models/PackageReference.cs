namespace Rooter.Web.Models;

public record PackageReference
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string Type { get; init; } = "package";
    public List<PackageReference> Dependencies { get; init; } = new();

    public string Id => $"{Name}/{Version}";

    public override string ToString() => Id;
}