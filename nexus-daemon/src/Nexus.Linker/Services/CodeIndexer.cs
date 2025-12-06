using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nexus.Core.Models;

namespace Nexus.Linker.Services;

public class CodeIndexer : ICodeIndexer
{
    public async Task<List<Node>> IndexAsync(string path)
    {
        var nodes = new List<Node>();
        
        if (Directory.Exists(path))
        {
            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                nodes.AddRange(await ParseFileAsync(file, path));
            }
        }
        else if (File.Exists(path) && path.EndsWith(".cs"))
        {
            nodes.AddRange(await ParseFileAsync(path, Path.GetDirectoryName(path) ?? ""));
        }

        return nodes;
    }

    private async Task<List<Node>> ParseFileAsync(string filePath, string rootPath)
    {
        var nodes = new List<Node>();
        var content = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(content);
        var root = await tree.GetRootAsync();
        
        var relativePath = Path.GetRelativePath(rootPath, filePath).Replace("\\", "/");

        // Find Classes
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        foreach (var cls in classes)
        {
            // Simple heuristic for type
            var type = ExtractTypeFromClass(cls); 

            // Create Node for Class (optional, currently focused on functions per user prompt, but scanning classes helps context)
            // For now, let's index methods primarily as they are the actionable units in the prompt examples (FN:123)
            
            var methods = cls.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                nodes.Add(CreateMethodNode(method, cls, relativePath, type));
            }
        }

        return nodes;
    }

    private NodeType ExtractTypeFromClass(ClassDeclarationSyntax cls)
    {
        var name = cls.Identifier.Text;
        if (name.EndsWith("Service")) return NodeType.SRV;
        if (name.EndsWith("Repository")) return NodeType.REPO;
        if (name.EndsWith("Controller")) return NodeType.CTRL;
        if (name.EndsWith("Tests")) return NodeType.TEST;
        if (name.EndsWith("Model") || name.EndsWith("Dto")) return NodeType.DOM;
        
        // Check attributes/inheritance logic here later
        return NodeType.UNK;
    }

    private Node CreateMethodNode(MethodDeclarationSyntax method, ClassDeclarationSyntax cls, string relativePath, NodeType parentType)
    {
        // Stable ID generation (simplified for MVP: Hash of path + signature)
        var signature = $"{cls.Identifier.Text}.{method.Identifier.Text}";
        var nodeId = $"FN:{Math.Abs(signature.GetHashCode()) % 10000}"; // Fake ID generation like FN:123
        
        // Line numbers (0-indexed to 1-indexed)
        var span = method.GetLocation().GetLineSpan();
        var startLine = span.StartLinePosition.Line + 1;
        var endLine = span.EndLinePosition.Line + 1;

        return new Node
        {
            NodeId = nodeId,
            Type = parentType, // Inherit type from class for now, or refine
            Path = relativePath,
            LineStart = startLine,
            LineEnd = endLine,
            Summary = method.Identifier.Text // Placeholder summary
        };
    }
}
