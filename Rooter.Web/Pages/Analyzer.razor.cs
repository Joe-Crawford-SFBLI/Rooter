using Microsoft.AspNetCore.Components;
using Rooter.Application.Interfaces;
using Rooter.Application.Services;
using Rooter.Domain.Entities;

namespace Rooter.Web.Pages;

/// <summary>
/// Code-behind for the Analyzer Blazor page.
/// Handles dependency analysis functionality for repository auto-detection.
/// </summary>
public partial class Analyzer : ComponentBase
{
    [Inject] public required IProjectAssetsParser ProjectAssetsParser { get; set; }
    [Inject] public required IDependencyAnalyzer DependencyAnalyzer { get; set; }

    private string _repositoryPath = "";
    private DependencyGraph? _dependencyGraph;
    private Dictionary<string, List<string>> _versionConflicts = new();
    private string _dependencyGraphJson = "";
    private bool _isLoading = false;
    private string _loadingMessage = "";
    private string _errorMessage = "";

    /// <summary>
    /// Analyzes a repository by auto-detecting all project.assets.json files
    /// </summary>
    private async Task _AnalyzeRepository()
    {
        if (string.IsNullOrWhiteSpace(_repositoryPath))
        {
            _errorMessage = "Please enter a repository path.";
            StateHasChanged();
            return;
        }

        if (!Directory.Exists(_repositoryPath))
        {
            _errorMessage = "The specified directory does not exist.";
            StateHasChanged();
            return;
        }

        _isLoading = true;
        _errorMessage = "";
        _loadingMessage = "Scanning repository for project.assets.json files...";
        StateHasChanged();

        try
        {
            // Find all project.assets.json files in the repository
            var projectAssetsFiles = Directory.GetFiles(
                _repositoryPath,
                "project.assets.json",
                SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("/bin/")) // Exclude build output directories
                .ToList();

            if (projectAssetsFiles.Count == 0)
            {
                _errorMessage = "No project.assets.json files found in the specified repository. Make sure to restore NuGet packages first (dotnet restore).";
                return;
            }

            _loadingMessage = $"Found {projectAssetsFiles.Count} project.assets.json file(s). Processing...";
            StateHasChanged();

            var projectsData = new List<(string ProjectName, string Content)>();

            foreach (var filePath in projectAssetsFiles)
            {
                try
                {
                    var projectName = _GetProjectNameFromPath(filePath);
                    _loadingMessage = $"Reading {projectName}...";
                    StateHasChanged();

                    var content = await File.ReadAllTextAsync(filePath);
                    projectsData.Add((projectName, content));
                }
                catch (Exception ex)
                {
                    // Log but continue with other files
                    Console.WriteLine($"Error reading {filePath}: {ex.Message}");
                }
            }

            if (projectsData.Count > 0)
            {
                await _AnalyzeProjects(projectsData);
            }
            else
            {
                _errorMessage = "Could not read any project.assets.json files.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error scanning repository: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Extracts a meaningful project name from the project.assets.json file path
    /// </summary>
    /// <param name="filePath">The full path to the project.assets.json file</param>
    /// <returns>A descriptive project name</returns>
    private string _GetProjectNameFromPath(string filePath)
    {
        // Get the directory containing the project.assets.json file (usually obj folder)
        var objDirectory = Path.GetDirectoryName(filePath);
        if (objDirectory != null)
        {
            // Get the parent directory (usually the project directory)
            var projectDirectory = Path.GetDirectoryName(objDirectory);
            if (projectDirectory != null)
            {
                return Path.GetFileName(projectDirectory);
            }
        }

        // Fallback to a generic name based on the path
        return Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(filePath))) ?? "Unknown Project";
    }

    /// <summary>
    /// Analyzes projects from the collected project data
    /// </summary>
    /// <param name="projectsData">List of project data containing project names and JSON content</param>
    private async Task _AnalyzeProjects(List<(string ProjectName, string Content)> projectsData)
    {
        try
        {
            _loadingMessage = "Analyzing dependencies...";
            StateHasChanged();

            if (projectsData.Count == 1)
            {
                await _AnalyzeSingleProject(projectsData[0]);
            }
            else
            {
                await _AnalyzeMultipleProjects(projectsData);
            }

            // Find version conflicts
            _versionConflicts = DependencyAnalyzer.FindVersionConflicts(_dependencyGraph!);

            // Serialize to JSON for display
            _dependencyGraphJson = System.Text.Json.JsonSerializer.Serialize(_dependencyGraph, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error analyzing dependencies: {ex.Message}";
        }
    }

    private async Task _AnalyzeSingleProject((string ProjectName, string Content) projectData)
    {
        var projectAssets = await ProjectAssetsParser.ParseFromJsonAsync(projectData.Content);
        if (projectAssets == null)
        {
            _errorMessage = "Invalid project.assets.json content";
            return;
        }

        var packages = ProjectAssetsParser.ExtractPackages(projectAssets);
        _dependencyGraph = DependencyAnalyzer.BuildDependencyGraph(packages, projectData.ProjectName);
    }

    private async Task _AnalyzeMultipleProjects(List<(string ProjectName, string Content)> projectsData)
    {
        var graphs = new List<DependencyGraph>();

        foreach (var project in projectsData)
        {
            var projectAssets = await ProjectAssetsParser.ParseFromJsonAsync(project.Content);
            if (projectAssets != null)
            {
                var packages = ProjectAssetsParser.ExtractPackages(projectAssets);
                var graph = DependencyAnalyzer.BuildDependencyGraph(packages, project.ProjectName);
                graphs.Add(graph);
            }
        }

        _dependencyGraph = DependencyAnalyzer.MergeDependencyGraphs(graphs);
    }

    /// <summary>
    /// Loads example data from the Examples directory
    /// </summary>
    private async Task _LoadExample()
    {
        _isLoading = true;
        _errorMessage = "";
        _loadingMessage = "Loading example...";
        StateHasChanged();

        try
        {
            // Use the Examples directory as a repository
            var currentDir = Directory.GetCurrentDirectory();
            var examplesPath = Path.Combine(currentDir, "..", "Examples");
            var fullExamplesPath = Path.GetFullPath(examplesPath);

            if (!Directory.Exists(fullExamplesPath))
            {
                _errorMessage = $"Examples directory not found at: {fullExamplesPath}";
                return;
            }

            // Set the repository path and analyze
            _repositoryPath = fullExamplesPath;
            await _AnalyzeRepository();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading example: {ex.Message}";
            _isLoading = false;
            StateHasChanged();
        }
    }
}