using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nexus.Server.Controllers;

namespace Nexus.Server.Tests;

public class CodeControllerTests
{
    private readonly Mock<ILogger<CodeController>> _loggerMock;
    private readonly CodeController _controller;

    public CodeControllerTests()
    {
        _loggerMock = new Mock<ILogger<CodeController>>();
        _controller = new CodeController(_loggerMock.Object);
    }

    [Fact]
    public async Task FetchNode_TraveralAttempt_ReturnsBadRequest()
    {
        // Arrange
        string invalidPath = @"../../Windows/System32/drivers/etc/hosts";

        // Act
        var result = await _controller.FetchNode(invalidPath, 1, 10);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("traversal", badRequest.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FetchNode_OutsideRoot_ReturnsBadRequest()
    {
        // Arrange
        // Current root provided in CodeController is typically C:\
        // To test "outside root", we ideally need to change the root or use a path that is definitely not under C:\ (e.g. D:\ if C:\ is hardcoded)
        // However, since IsSubPathOf is private and authorizedRoot is private static hardcoded to C:\, 
        // passing a path like "Z:\fake.txt" should fail if Z drive doesn't exist or is different.
        // Or using a UNC path if not allowed.
        // Actually, CodeController hardcodes "C:\" as root.
        // So checking "D:\test.txt" should return BadRequest.
        
        if (OperatingSystem.IsWindows())
        {
             // D:\ is likely not C:\
             string outsidePath = @"D:\secret.txt";
             
             // Note: Path.GetFullPath might throw if D: doesn't exist on some environments, 
             // but usually strictly string parsing works.
             // However, the controller calls Path.GetFullPath.
             
             // Let's rely on the IsSubPathOf logic.
             var result = await _controller.FetchNode(outsidePath, 1, 10);
             
             // If valid path format but outside root:
             var badRequest = Assert.IsType<BadRequestObjectResult>(result);
             Assert.Contains("denied", badRequest.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
