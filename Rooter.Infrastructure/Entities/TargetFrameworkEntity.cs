using System.Text.Json.Serialization;

namespace Rooter.Infrastructure.Entities;

/// <summary>
/// Infrastructure entity representing a target framework in the assets file.
/// The actual structure contains package references directly as properties.
/// </summary>
public class TargetFrameworkEntity : Dictionary<string, PackageReferenceEntity>
{
    // This entity inherits from Dictionary to capture the dynamic package structure
    // where each package/version combination is a key with PackageReferenceEntity as value
}