using System.Text.Json;

namespace Rooter.Web.Models;

public class ProjectAssets
{
    public int Version { get; set; }
    public Dictionary<string, Dictionary<string, PackageTarget>> Targets { get; set; } = new();
    public Dictionary<string, Library> Libraries { get; set; } = new();
    public string ProjectFilePath { get; set; } = string.Empty;
    public ProjectSpec? Project { get; set; }
}

public class PackageTarget
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Dependencies { get; set; } = new();
    public Dictionary<string, object> Compile { get; set; } = new();
    public Dictionary<string, object> Runtime { get; set; } = new();
    public List<string>? FrameworkReferences { get; set; }
}

public class Library
{
    public string Sha512 { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string> Files { get; set; } = new();
}

public class ProjectSpec
{
    public Dictionary<string, TargetFramework> Frameworks { get; set; } = new();
}

public class TargetFramework
{
    public Dictionary<string, DependencySpec> Dependencies { get; set; } = new();
}

public class DependencySpec
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}