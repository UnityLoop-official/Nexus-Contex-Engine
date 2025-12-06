using Microsoft.AspNetCore.Mvc;
using Nexus.Linker.Services;

namespace Nexus.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class CodeController : ControllerBase
{
    // TODO: Make this configurable via appsettings.json or environment variable
    // For now, we'll validate that paths don't escape using relative paths
    private static readonly ILogger<CodeController>? _logger;

    [HttpGet("fetch")]
    public async Task<IActionResult> FetchNode([FromQuery] string path, [FromQuery] int startLine, [FromQuery] int endLine)
    {
        // SECURITY: Validate path to prevent path traversal attacks
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("Path parameter is required");
        }

        // Reject paths with path traversal patterns
        if (path.Contains("..", StringComparison.Ordinal) ||
            path.Contains("~", StringComparison.Ordinal))
        {
            _logger?.LogWarning("Path traversal attempt detected: {Path}", path);
            return BadRequest("Invalid path: path traversal patterns not allowed");
        }

        // Get the full normalized path
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Invalid path format: {Path}", path);
            return BadRequest($"Invalid path format: {ex.Message}");
        }

        // TODO: Add basePath validation when solution context is available
        // For now, we validate that the resolved path exists and is a valid file
        // Future: Validate IsSubPathOf(basePath, fullPath) where basePath comes from request context

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound($"File not found: {path}");
        }

        var lines = await System.IO.File.ReadAllLinesAsync(fullPath);

        // clamp lines
        int start = Math.Max(0, startLine - 1);
        int end = Math.Min(lines.Length, endLine);

        var selectedLines = lines.Skip(start).Take(end - start);
        var content = string.Join(Environment.NewLine, selectedLines);

        return Ok(new
        {
            Path = path,
            Code = content
        });
    }

    /// <summary>
    /// Validates that targetPath is a subdirectory or file within basePath.
    /// Prevents path traversal attacks.
    /// </summary>
    /// <param name="basePath">The authorized root directory</param>
    /// <param name="targetPath">The path to validate</param>
    /// <returns>True if targetPath is within basePath, false otherwise</returns>
    private static bool IsSubPathOf(string basePath, string targetPath)
    {
        try
        {
            var baseFullPath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var targetFullPath = Path.GetFullPath(targetPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Case-insensitive comparison for Windows, case-sensitive for Unix-like systems
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return targetFullPath.StartsWith(baseFullPath + Path.DirectorySeparatorChar, comparison) ||
                   targetFullPath.Equals(baseFullPath, comparison);
        }
        catch
        {
            // If path normalization fails, reject as invalid
            return false;
        }
    }
}
