using Microsoft.AspNetCore.Mvc;
using Rooter.Web.Models;
using Rooter.Web.Services;
using System.Text.Json;

namespace Rooter.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DependencyController : ControllerBase
{
    private readonly IProjectAssetsParser _parser;
    private readonly IDependencyAnalyzer _analyzer;
    private readonly ILogger<DependencyController> _logger;

    public DependencyController(
        IProjectAssetsParser parser,
        IDependencyAnalyzer analyzer,
        ILogger<DependencyController> logger)
    {
        _parser = parser;
        _analyzer = analyzer;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<DependencyGraph>> AnalyzeDependencies([FromBody] AnalyzeRequest request)
    {
        try
        {
            var projectAssets = await _parser.ParseFromJsonAsync(request.ProjectAssetsJson);
            if (projectAssets == null)
                return BadRequest("Invalid project.assets.json content");

            var packages = _parser.ExtractPackages(projectAssets, request.TargetFramework);
            var graph = _analyzer.BuildDependencyGraph(packages, request.ProjectName, request.TargetFramework ?? "");

            return Ok(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing dependencies");
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error analyzing dependencies: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error analyzing dependencies: {ex.Message}");
        }
    }

    [HttpPost("analyze-multiple")]
    public async Task<ActionResult<DependencyGraph>> AnalyzeMultipleProjects([FromBody] AnalyzeMultipleRequest request)
    {
        try
        {
            var graphs = new List<DependencyGraph>();

            foreach (var projectRequest in request.Projects)
            {
                var projectAssets = await _parser.ParseFromJsonAsync(projectRequest.ProjectAssetsJson);
                if (projectAssets != null)
                {
                    var packages = _parser.ExtractPackages(projectAssets, projectRequest.TargetFramework);
                    var graph = _analyzer.BuildDependencyGraph(packages, projectRequest.ProjectName, projectRequest.TargetFramework ?? "");
                    graphs.Add(graph);
                }
            }

            var mergedGraph = _analyzer.MergeDependencyGraphs(graphs);
            return Ok(mergedGraph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing multiple projects");
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error analyzing multiple projects: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error analyzing multiple projects: {ex.Message}");
        }
    }

    [HttpPost("path")]
    public async Task<ActionResult<List<string>>> FindDependencyPath([FromBody] FindPathRequest request)
    {
        try
        {
            var projectAssets = await _parser.ParseFromJsonAsync(request.ProjectAssetsJson);
            if (projectAssets == null)
                return BadRequest("Invalid project.assets.json content");

            var packages = _parser.ExtractPackages(projectAssets);
            var graph = _analyzer.BuildDependencyGraph(packages);

            var path = _analyzer.FindDependencyPath(graph, request.FromPackage, request.ToPackage);
            return Ok(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding dependency path");
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error finding dependency path: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error finding dependency path: {ex.Message}");
        }
    }

    [HttpPost("conflicts")]
    public async Task<ActionResult<Dictionary<string, List<string>>>> FindVersionConflicts([FromBody] AnalyzeRequest request)
    {
        try
        {
            var projectAssets = await _parser.ParseFromJsonAsync(request.ProjectAssetsJson);
            if (projectAssets == null)
                return BadRequest("Invalid project.assets.json content");

            var packages = _parser.ExtractPackages(projectAssets, request.TargetFramework);
            var graph = _analyzer.BuildDependencyGraph(packages);

            var conflicts = _analyzer.FindVersionConflicts(graph);
            return Ok(conflicts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding version conflicts");
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error finding version conflicts: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error finding version conflicts: {ex.Message}");
        }
    }

    [HttpPost("filter")]
    public async Task<ActionResult<DependencyGraph>> FilterByPackage([FromBody] FilterRequest request)
    {
        try
        {
            var projectAssets = await _parser.ParseFromJsonAsync(request.ProjectAssetsJson);
            if (projectAssets == null)
                return BadRequest("Invalid project.assets.json content");

            var packages = _parser.ExtractPackages(projectAssets, request.TargetFramework);
            var graph = _analyzer.BuildDependencyGraph(packages);

            var filteredGraph = _analyzer.FilterByPackage(graph, request.PackageName);
            return Ok(filteredGraph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering by package");
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error filtering by package: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error filtering by package: {ex.Message}");
        }
    }

    [HttpPost("analyze-path")]
    public async Task<ActionResult<DependencyGraph>> AnalyzeFromPath([FromBody] AnalyzePathRequest request)
    {
        try
        {
            if (!System.IO.File.Exists(request.FilePath))
                return BadRequest($"File not found: {request.FilePath}");

            var projectAssets = await _parser.ParseAsync(request.FilePath);
            if (projectAssets == null)
                return BadRequest("Invalid project.assets.json file");

            var packages = _parser.ExtractPackages(projectAssets, request.TargetFramework);
            var projectName = request.ProjectName ?? Path.GetFileName(Path.GetDirectoryName(request.FilePath)) ?? "Unknown";
            var graph = _analyzer.BuildDependencyGraph(packages, projectName, request.TargetFramework ?? "");

            return Ok(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing from path: {FilePath}", request.FilePath);
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error analyzing from path: {request.FilePath} - {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error analyzing file: {ex.Message}");
        }
    }

    [HttpPost("analyze-multiple-paths")]
    public async Task<ActionResult<DependencyGraph>> AnalyzeMultiplePaths([FromBody] AnalyzeMultiplePathsRequest request)
    {
        try
        {
            var graphs = new List<DependencyGraph>();

            foreach (var pathRequest in request.FilePaths)
            {
                if (System.IO.File.Exists(pathRequest.FilePath))
                {
                    var projectAssets = await _parser.ParseAsync(pathRequest.FilePath);
                    if (projectAssets != null)
                    {
                        var packages = _parser.ExtractPackages(projectAssets, pathRequest.TargetFramework);
                        var projectName = pathRequest.ProjectName ?? Path.GetFileName(Path.GetDirectoryName(pathRequest.FilePath)) ?? "Unknown";
                        var graph = _analyzer.BuildDependencyGraph(packages, projectName, pathRequest.TargetFramework ?? "");
                        graphs.Add(graph);
                    }
                }
            }

            if (graphs.Count == 0)
                return BadRequest("No valid project.assets.json files found");

            var mergedGraph = _analyzer.MergeDependencyGraphs(graphs);
            return Ok(mergedGraph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing multiple paths");
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error analyzing multiple paths: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error analyzing files: {ex.Message}");
        }
    }

    [HttpPost("discover-projects")]
    public ActionResult<List<ProjectInfo>> DiscoverProjects([FromBody] DiscoverProjectsRequest request)
    {
        try
        {
            var projects = new List<ProjectInfo>();
            var searchPath = request.SearchPath ?? Directory.GetCurrentDirectory();

            if (!Directory.Exists(searchPath))
                return BadRequest($"Directory not found: {searchPath}");

            // Search for project.assets.json files
            var assetFiles = Directory.GetFiles(searchPath, "project.assets.json", SearchOption.AllDirectories)
                .Where(f => !f.Contains("node_modules") && !f.Contains(".git"))
                .ToList();

            foreach (var file in assetFiles)
            {
                var projectDir = Path.GetDirectoryName(file);
                var projectName = Path.GetFileName(projectDir);

                // Try to find associated .csproj file
                var csprojFiles = Directory.GetFiles(projectDir!, "*.csproj");
                if (csprojFiles.Length > 0)
                {
                    projectName = Path.GetFileNameWithoutExtension(csprojFiles[0]);
                }

                projects.Add(new ProjectInfo
                {
                    ProjectName = projectName ?? "Unknown",
                    ProjectAssetsPath = file,
                    ProjectDirectory = projectDir!
                });
            }

            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering projects");
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error discovering projects: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error discovering projects: {ex.Message}");
        }
    }

    [HttpGet("example")]
    public async Task<ActionResult<DependencyGraph>> GetExampleAnalysis()
    {
        try
        {
            // Use one of the example files
            var examplePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Examples", "Example1", "project.assets.json");

            if (!System.IO.File.Exists(examplePath))
                return NotFound("Example file not found");

            var projectAssets = await _parser.ParseAsync(examplePath);
            if (projectAssets == null)
                return BadRequest("Could not parse example file");

            var packages = _parser.ExtractPackages(projectAssets);
            var graph = _analyzer.BuildDependencyGraph(packages, "Example1");

            return Ok(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting example analysis");
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} Error getting example analysis: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return BadRequest($"Error getting example analysis: {ex.Message}");
        }
    }
}

public record AnalyzeRequest
{
    public required string ProjectAssetsJson { get; init; }
    public string ProjectName { get; init; } = "";
    public string? TargetFramework { get; init; }
}

public record AnalyzeMultipleRequest
{
    public required List<AnalyzeRequest> Projects { get; init; }
}

public record FindPathRequest
{
    public required string ProjectAssetsJson { get; init; }
    public required string FromPackage { get; init; }
    public required string ToPackage { get; init; }
}

public record FilterRequest
{
    public required string ProjectAssetsJson { get; init; }
    public required string PackageName { get; init; }
    public string? TargetFramework { get; init; }
}

public record AnalyzePathRequest
{
    public required string FilePath { get; init; }
    public string? ProjectName { get; init; }
    public string? TargetFramework { get; init; }
}

public record AnalyzeMultiplePathsRequest
{
    public required List<AnalyzePathRequest> FilePaths { get; init; }
}

public record DiscoverProjectsRequest
{
    public string? SearchPath { get; init; }
}

public record ProjectInfo
{
    public required string ProjectName { get; init; }
    public required string ProjectAssetsPath { get; init; }
    public required string ProjectDirectory { get; init; }
}