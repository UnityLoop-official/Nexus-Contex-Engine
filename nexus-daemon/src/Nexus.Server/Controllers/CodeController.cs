using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexus.Linker.Services;

namespace Nexus.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class CodeController : ControllerBase
{
    // TODO: Make this configurable via appsettings.json or environment variable
    // TODO: In the future, this should be loaded from appsettings.json or passed during daemon startup.
    // For now, we allow setting it via a static property or default to a safe restriction (e.g. user drive).
    // This represents the "Authorized Root".
    private static string _authorizedRoot = @"C:\"; 
    
    private readonly ILogger<CodeController> _logger;

    public CodeController(ILogger<CodeController> logger)
    {
        _logger = logger;
    }

    [HttpGet("fetch")]
    public async Task<IActionResult> FetchNode([FromQuery] string path, [FromQuery] int startLine, [FromQuery] int endLine)
    {
        // 1. Basic sanity check
        if (path.Contains("..", StringComparison.Ordinal) || path.Contains("~", StringComparison.Ordinal))
        {
             _logger.LogWarning("Security: Path traversal attempt blocked. Path: {Path}", path);
             return BadRequest("Invalid path: traversal patterns detected.");
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
             _logger.LogWarning(ex, "Security: Malformed path format. Path: {Path}", path);
             return BadRequest($"Invalid path format: {ex.Message}");
        }

        // 2. Validate against Authorized Root
        // TODO: Move authorization root location to configuration (Phase 1 req)
        if (!IsSubPathOf(_authorizedRoot, fullPath))
        {
            _logger.LogWarning("Security: Access denied. Path {Path} is not under authorized root {_authorizedRoot}", fullPath, _authorizedRoot);
            return BadRequest($"Access to path '{path}' is denied. It is outside the authorized scope.");
        }

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound($"File not found: {path}");
        }

        try 
        {
            var lines = await System.IO.File.ReadAllLinesAsync(fullPath);
            int start = Math.Max(0, startLine - 1);
            int end = Math.Min(lines.Length, endLine);
            
            // Handle edge case where file is smaller than startLine
            if (start >= lines.Length) 
            {
                 return Ok(new { Path = path, Code = string.Empty });
            }

            var selectedLines = lines.Skip(start).Take(end - start);
            var content = string.Join(Environment.NewLine, selectedLines);

            return Ok(new
            {
                Path = path,
                Code = content
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file {Path}", fullPath);
            return StatusCode(500, "Error reading file content");
        }
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
