using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Rooter.Application.Interfaces;
using Rooter.Application.Services;
using Rooter.Domain.Entities;

namespace Rooter.Web.Pages;

/// <summary>
/// Code-behind for the Analyzer Blazor page.
/// Handles dependency analysis functionality including file uploads and processing.
/// </summary>
public partial class Analyzer : ComponentBase
{
    [Inject] public required IProjectAssetsParser ProjectAssetsParser { get; set; }
    [Inject] public required IDependencyAnalyzer DependencyAnalyzer { get; set; }
    [Inject] public required IJSRuntime JSRuntime { get; set; }

    private InputFile? _fileInput;
    private DependencyGraph? _dependencyGraph;
    private Dictionary<string, List<string>> _versionConflicts = new();
    private string _dependencyGraphJson = "";
    private bool _isLoading = false;
    private string _loadingMessage = "";
    private string _errorMessage = "";
    private bool _isDragOver = false;

    private async Task _HandleFileSelected(InputFileChangeEventArgs e)
    {
        await _ProcessFiles(e.GetMultipleFiles());
    }

    private async Task _HandleFileDrop(DragEventArgs e)
    {
        _isDragOver = false;

        // Note: Blazor Server doesn't support DataTransfer.Files in drag events
        // This is a limitation - we'll show an appropriate message
        _errorMessage = "Drag and drop is not supported in Blazor Server. Please use the 'Browse Files' button instead.";
        StateHasChanged();
    }

    private void _HandleDragOver(DragEventArgs e)
    {
        _isDragOver = true;
    }

    private void _HandleDragEnter(DragEventArgs e)
    {
        _isDragOver = true;
    }

    private void _HandleDragLeave(DragEventArgs e)
    {
        _isDragOver = false;
    }

    private async Task _OpenFileDialog()
    {
        if (_fileInput != null)
        {
            await JSRuntime.InvokeVoidAsync("document.querySelector('input[type=file]').click");
        }
    }

    private async Task _ProcessFiles(IReadOnlyList<IBrowserFile> files)
    {
        _isLoading = true;
        _errorMessage = "";
        _loadingMessage = "Processing files...";
        StateHasChanged();

        try
        {
            var projectsData = new List<(string ProjectName, string Content)>();

            foreach (var file in files)
            {
                if (file.Name.EndsWith(".json"))
                {
                    _loadingMessage = $"Reading {file.Name}...";
                    StateHasChanged();

                    using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    var projectName = Path.GetFileNameWithoutExtension(file.Name);
                    projectsData.Add((projectName, content));
                }
            }

            if (projectsData.Count > 0)
            {
                await _AnalyzeProjects(projectsData);
            }
            else
            {
                _errorMessage = "No valid JSON files found in the selected files.";
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error processing files: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

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

    private async Task _LoadExample()
    {
        _isLoading = true;
        _errorMessage = "";
        _loadingMessage = "Loading example...";
        StateHasChanged();

        try
        {
            // Use one of the example files
            var examplePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Examples", "Example1", "project.assets.json");

            if (!File.Exists(examplePath))
            {
                _errorMessage = "Example file not found";
                return;
            }

            var projectAssets = await ProjectAssetsParser.ParseAsync(examplePath);
            if (projectAssets == null)
            {
                _errorMessage = "Could not parse example file";
                return;
            }

            var packages = ProjectAssetsParser.ExtractPackages(projectAssets);
            _dependencyGraph = DependencyAnalyzer.BuildDependencyGraph(packages, "Example1");

            // Find version conflicts
            _versionConflicts = DependencyAnalyzer.FindVersionConflicts(_dependencyGraph);

            // Serialize to JSON for display
            _dependencyGraphJson = System.Text.Json.JsonSerializer.Serialize(_dependencyGraph, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading example: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}