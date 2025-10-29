namespace Rooter.Domain.Entities;

/// <summary>
/// Represents a library (package) in the project assets.
/// </summary>
public class Library
{
    /// <summary>
    /// The type of library (e.g., "package", "project").
    /// </summary>
    public string Type { get; init; } = "package";

    /// <summary>
    /// Whether this is a service-able library.
    /// </summary>
    public bool Serviceable { get; init; }

    /// <summary>
    /// SHA hash of the library.
    /// </summary>
    public string? Sha512 { get; init; }

    /// <summary>
    /// Path to the library.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Files included in the library.
    /// </summary>
    public IReadOnlyList<string> Files { get; init; } = new List<string>();
}