// =============================================================================
// BetterBreadcrumbBar for WinUI 3
// Author:  Matteo Riso
// Version: 0.8.6
// Website: https://zipgenius.it
// Written with Claude AI
// License: MIT
// =============================================================================

namespace BetterBreadcrumbBar.Control.Providers;

/// <summary>
/// <see cref="IPathProvider"/> implementation that operates on a virtual path tree
/// (e.g. folders extracted from a ZIP archive, a database, or an FTP server).
/// The control does not read any archive; the caller is responsible for supplying
/// the structure as a tree of <see cref="PathNode"/> objects.
/// Also implements <see cref="IPathProvider.GetSuggestionsAsync"/> for inline address-bar autocomplete.
/// </summary>
public class VirtualPathProvider : IPathProvider
{
    private readonly PathNode _root;

    /// <summary>
    /// Initialises the provider with the root node of the virtual tree.
    /// </summary>
    /// <param name="root">Root of the virtual path tree.</param>
    public VirtualPathProvider(PathNode root)
    {
        _root = root;
    }

    /// <summary>
    /// Builds a <see cref="VirtualPathProvider"/> and a node tree
    /// from a flat sequence of slash-separated paths.
    /// </summary>
    /// <param name="paths">Paths in the format "folder/subfolder/file.txt".</param>
    /// <param name="rootName">Name of the root node (e.g. "archive.zip").</param>
    public static (VirtualPathProvider Provider, PathNode Root) FromPaths(
        IEnumerable<string> paths, string rootName = "Root")
    {
        var root    = new PathNode { Name = rootName, FullPath = string.Empty, IsRoot = true };
        var nodeMap = new Dictionary<string, PathNode>(StringComparer.OrdinalIgnoreCase)
        {
            [string.Empty] = root
        };

        foreach (var path in paths)
        {
            var parts       = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string cur      = string.Empty;

            for (int i = 0; i < parts.Length - 1; i++) // directories only, not files
            {
                string parent = cur;
                cur = cur.Length == 0 ? parts[i] : cur + "/" + parts[i];

                if (!nodeMap.TryGetValue(cur, out var node))
                {
                    node = new PathNode { Name = parts[i], FullPath = cur };
                    nodeMap[cur]    = node;
                    nodeMap[parent].Children.Add(node);
                }
            }
        }

        return (new VirtualPathProvider(root), root);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<PathNode>> GetChildrenAsync(
        PathNode node, CancellationToken cancellationToken = default)
    {
        var found    = FindNode(_root, node.FullPath);
        var children = found?.Children ?? Enumerable.Empty<PathNode>();
        return Task.FromResult(children);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Returns virtual paths that start with <paramref name="partialPath"/>.
    /// The comparison is case-insensitive.
    /// </remarks>
    public Task<IEnumerable<string>> GetSuggestionsAsync(
        string partialPath, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        CollectSuggestions(_root, partialPath.Trim('/'), results, 20);
        return Task.FromResult<IEnumerable<string>>(results);
    }

    private static void CollectSuggestions(PathNode node, string prefix, List<string> results, int max)
    {
        foreach (var child in node.Children)
        {
            if (results.Count >= max) return;
            if (child.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                results.Add(child.FullPath);
            CollectSuggestions(child, prefix, results, max);
        }
    }

    /// <summary>
    /// Builds an ordered list of <see cref="PathNode"/> objects from a virtual path string.
    /// </summary>
    /// <param name="virtualPath">Slash-separated virtual path, e.g. <c>src/components/ui</c>.</param>
    public List<PathNode> BuildPathNodes(string virtualPath)
    {
        var nodes = new List<PathNode> { _root };
        var parts = virtualPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string acc = string.Empty;

        foreach (var part in parts)
        {
            acc = acc.Length == 0 ? part : acc + "/" + part;
            var found = FindNode(_root, acc);
            nodes.Add(found ?? new PathNode { Name = part, FullPath = acc });
        }
        return nodes;
    }

    private static PathNode? FindNode(PathNode current, string fullPath)
    {
        if (current.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
            return current;
        foreach (var child in current.Children)
        {
            var found = FindNode(child, fullPath);
            if (found != null) return found;
        }
        return null;
    }
}
