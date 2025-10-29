using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Rooter.Application.Interfaces;
using Rooter.Application.Services;
using Rooter.Domain.Entities;

namespace Rooter.Web.Pages;

/// <summary>
/// Represents a package option for the dropdown selector
/// </summary>
public class PackageOption
{
    /// <summary>
    /// The package name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The package version
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// The display name for the dropdown (Name v Version)
    /// </summary>
    public string DisplayName => $"{Name} v{Version}";

    /// <summary>
    /// The package ID (Name/Version)
    /// </summary>
    public string Id => $"{Name}/{Version}";
}

/// <summary>
/// Code-behind for the Analyzer Blazor page.
/// Handles dependency analysis functionality for repository auto-detection.
/// </summary>
public partial class Analyzer : ComponentBase
{
    [Inject] public required IProjectAssetsParser ProjectAssetsParser { get; set; }
    [Inject] public required IDependencyAnalyzer DependencyAnalyzer { get; set; }
    [Inject] public required IJSRuntime JSRuntime { get; set; }

    private string _repositoryPath = "";
    private DependencyGraph? _dependencyGraph;
    private Dictionary<string, List<string>> _versionConflicts = new();
    private string _dependencyGraphJson = "";
    private bool _isLoading = false;
    private string _loadingMessage = "";
    private string _errorMessage = "";

    // Package visualization fields
    private string _searchTerm = "";
    private bool _showAllVersions = false;
    private string _selectedPackageInfo = "";
    private string _mermaidChart = "";
    private List<PackageOption> _allPackageOptions = new();
    private List<PackageOption> _filteredPackageOptions = new();
    private Package? _selectedPackage;

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

            // Update package options for the new visualization
            _UpdatePackageOptions();

            // Serialize to JSON for display (keeping for potential future use)
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

    /// <summary>
    /// Updates the package options list when dependency graph changes
    /// </summary>
    private void _UpdatePackageOptions()
    {
        if (_dependencyGraph == null)
        {
            _allPackageOptions.Clear();
            _filteredPackageOptions.Clear();
            return;
        }

        var options = new List<PackageOption>();

        if (_showAllVersions)
        {
            // Show all package versions
            options = _dependencyGraph.Packages.Values
                .Select(p => new PackageOption
                {
                    Name = p.Name,
                    Version = p.Version
                })
                .OrderBy(o => o.Name)
                .ThenBy(o => o.Version)
                .ToList();
        }
        else
        {
            // Show only the latest version of each package
            options = _dependencyGraph.Packages.Values
                .GroupBy(p => p.Name)
                .Select(g => g.OrderByDescending(p => p.Version).First())
                .Select(p => new PackageOption
                {
                    Name = p.Name,
                    Version = p.Version
                })
                .OrderBy(o => o.Name)
                .ToList();
        }

        _allPackageOptions = options;
        _FilterPackageOptions();
    }

    /// <summary>
    /// Filters package options based on search term
    /// </summary>
    private void _FilterPackageOptions()
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            _filteredPackageOptions = _allPackageOptions.ToList();
        }
        else
        {
            _filteredPackageOptions = _allPackageOptions
                .Where(p => p.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <summary>
    /// Handles search input changes
    /// </summary>
    private void _OnSearchInputChanged(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? "";
        _FilterPackageOptions();
        StateHasChanged();
    }

    /// <summary>
    /// Handles show all versions checkbox changes
    /// </summary>
    private void _OnShowAllVersionsChanged()
    {
        _UpdatePackageOptions();
        StateHasChanged();
    }

    /// <summary>
    /// Generates the Mermaid chart for the selected package
    /// </summary>
    private void _GenerateChart()
    {
        if (_dependencyGraph == null || string.IsNullOrWhiteSpace(_searchTerm))
        {
            _mermaidChart = "";
            _selectedPackageInfo = "Please enter a search term.";
            return;
        }

        PackageOption? selectedOption = null;

        // Try to find exact match by DisplayName first
        selectedOption = _allPackageOptions
            .FirstOrDefault(p => p.DisplayName.Equals(_searchTerm, StringComparison.OrdinalIgnoreCase));

        // If not found, try to find by Name only
        if (selectedOption == null)
        {
            selectedOption = _allPackageOptions
                .FirstOrDefault(p => p.Name.Equals(_searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // If still not found, try partial matching on DisplayName
        if (selectedOption == null)
        {
            selectedOption = _allPackageOptions
                .FirstOrDefault(p => p.DisplayName.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // If still not found, try partial matching on Name
        if (selectedOption == null)
        {
            selectedOption = _allPackageOptions
                .FirstOrDefault(p => p.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (selectedOption == null)
        {
            var availablePackages = string.Join(", ", _allPackageOptions.Take(5).Select(p => p.DisplayName));
            _selectedPackageInfo = $"Package not found. Available packages include: {availablePackages}...";
            _mermaidChart = "";
            return;
        }

        _selectedPackage = _dependencyGraph.Packages.Values
            .FirstOrDefault(p => p.Name == selectedOption.Name && p.Version == selectedOption.Version);

        if (_selectedPackage == null)
        {
            _selectedPackageInfo = $"Package '{selectedOption.DisplayName}' not found in dependency graph.";
            _mermaidChart = "";
            return;
        }

        _selectedPackageInfo = $"{_selectedPackage.Name} v{_selectedPackage.Version}";

        // Filter the graph to show only related packages
        var filteredGraph = DependencyAnalyzer.FilterByPackage(_dependencyGraph, _selectedPackage.Name);

        _mermaidChart = _GenerateMermaidChart(filteredGraph, _selectedPackage);
        StateHasChanged();

        // Enhanced chart rendering with proper timing and error handling
        _ = Task.Run(async () =>
        {
            // Prepare container for new chart to prevent zoom-related sizing issues
            try
            {
                await JSRuntime.InvokeVoidAsync("prepareForNewChart");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not prepare chart container: {ex.Message}");
            }

            // Wait for Blazor to complete rendering the DOM
            await Task.Delay(100);

            try
            {
                // Ensure the chart container exists before rendering
                var retryCount = 0;
                const int maxRetries = 10;

                while (retryCount < maxRetries)
                {
                    await Task.Delay(50);
                    await JSRuntime.InvokeVoidAsync("renderMermaidChart");
                    retryCount++;

                    // Break out of retry loop if successful (no exception thrown)
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering Mermaid chart: {ex.Message}");
                // Try once more after a longer delay
                await Task.Delay(500);
                try
                {
                    await JSRuntime.InvokeVoidAsync("renderMermaidChart");
                }
                catch (Exception retryEx)
                {
                    Console.WriteLine($"Retry failed for Mermaid chart: {retryEx.Message}");
                }
            }
        });
    }

    /// <summary>
    /// Generates a Mermaid flowchart for the dependency graph
    /// </summary>
    /// <param name="graph">The filtered dependency graph</param>
    /// <param name="selectedPackage">The selected package to highlight</param>
    /// <returns>Mermaid chart markup</returns>
    private string _GenerateMermaidChart(
        DependencyGraph graph,
        Package selectedPackage)
    {
        var mermaid = new System.Text.StringBuilder();
        mermaid.AppendLine("<div class=\"mermaid\">");
        mermaid.AppendLine("%%{init: {'flowchart': {'nodeSpacing': 80, 'rankSpacing': 120, 'curve': 'basis', 'padding': 50, 'useMaxWidth': true}, 'theme': 'base', 'themeVariables': {'fontSize': '14px', 'fontFamily': 'Segoe UI, sans-serif', 'primaryTextColor': '#2c3e50', 'lineColor': '#6b7280'}}}%%");
        mermaid.AppendLine("graph TD");

        // Add nodes with better sizing
        foreach (var package in graph.Packages.Values)
        {
            var nodeId = _GetNodeId(package);
            var displayName = _GetDisplayName(package);

            if (package.Id == selectedPackage.Id)
            {
                // Highlight the selected package
                mermaid.AppendLine($"    {nodeId}[\"{displayName}\"]:::selected");
            }
            else if (package.IsDirectDependency)
            {
                // Style direct dependencies
                mermaid.AppendLine($"    {nodeId}[\"{displayName}\"]:::direct");
            }
            else
            {
                // Style transitive dependencies
                mermaid.AppendLine($"    {nodeId}[\"{displayName}\"]:::transitive");
            }
        }

        // Add edges
        foreach (var dependency in graph.Dependencies)
        {
            var fromId = _GetNodeIdFromPackageId(dependency.FromPackageId);
            var toId = _GetNodeIdFromPackageId(dependency.ToPackageId);
            mermaid.AppendLine($"    {fromId} --> {toId}");
        }

        // Add enhanced styles with consistent sizing
        mermaid.AppendLine("    classDef selected fill:#ff6b6b,stroke:#ee5a52,stroke-width:4px,color:#fff");
        mermaid.AppendLine("    classDef direct fill:#4ecdc4,stroke:#45b7aa,stroke-width:3px,color:#fff");
        mermaid.AppendLine("    classDef transitive fill:#95e1d3,stroke:#6bcf7f,stroke-width:2px,color:#333");

        mermaid.AppendLine("</div>");

        return mermaid.ToString();
    }

    /// <summary>
    /// Gets a valid node ID for Mermaid from a package
    /// </summary>
    private string _GetNodeId(Package package)
    {
        return _GetNodeIdFromPackageId(package.Id);
    }

    /// <summary>
    /// Gets a valid node ID for Mermaid from a package ID
    /// </summary>
    private string _GetNodeIdFromPackageId(string packageId)
    {
        // Replace invalid characters for Mermaid node IDs
        return packageId
            .Replace("/", "_")
            .Replace(".", "_")
            .Replace("-", "_")
            .Replace(" ", "_");
    }

    /// <summary>
    /// Gets a display name for the package node
    /// </summary>
    private string _GetDisplayName(Package package)
    {
        // Break long package names into multiple lines for better readability
        var packageName = package.Name;

        // More aggressive line breaking for better fit
        if (packageName.Length > 18)
        {
            // Try to break at dots or other separators
            var parts = packageName.Split(new[] { '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                var result = new List<string>();
                var currentLine = "";

                foreach (var part in parts)
                {
                    // More conservative line length to ensure text fits
                    if (currentLine.Length + part.Length + 1 > 12 && !string.IsNullOrEmpty(currentLine))
                    {
                        result.Add(currentLine);
                        currentLine = part;
                    }
                    else
                    {
                        currentLine = string.IsNullOrEmpty(currentLine) ? part : $"{currentLine}.{part}";
                    }
                }

                if (!string.IsNullOrEmpty(currentLine))
                {
                    result.Add(currentLine);
                }

                packageName = string.Join("<br/>", result);
            }
            else
            {
                // Force break very long single words
                if (packageName.Length > 25)
                {
                    var mid = packageName.Length / 2;
                    packageName = $"{packageName.Substring(0, mid)}<br/>{packageName.Substring(mid)}";
                }
            }
        }

        // Shorten version display if it's very long
        var version = package.Version;
        if (version.Length > 15)
        {
            version = version.Length > 18 ? version.Substring(0, 15) + "..." : version;
        }

        return $"{packageName}<br/>v{version}";
    }
}