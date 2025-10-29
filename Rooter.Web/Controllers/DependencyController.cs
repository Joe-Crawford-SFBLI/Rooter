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
            return BadRequest($"Error filtering by package: {ex.Message}");
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