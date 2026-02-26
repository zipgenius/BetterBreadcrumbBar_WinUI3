// =============================================================================
// BetterBreadcrumbBar for WinUI 3
// Author:  Matteo Riso
// Version: 0.8.5
// Website: https://zipgenius.it
// Written with Claude AI
// License: MIT
// =============================================================================

using System.Collections.ObjectModel;

namespace BetterBreadcrumbBar.Control;

/// <summary>
/// Represents a single node in a path (real or virtual folder).
/// </summary>
public class PathNode
{
    /// <summary>
    /// Display name of the segment (e.g. "Documents", "C:\\", "archive.zip").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full path up to and including this node.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Direct children of this node (sub-folders at the next level).
    /// Populated by the <see cref="IPathProvider"/> when the user clicks the chevron.
    /// </summary>
    public ObservableCollection<PathNode> Children { get; } = new();

    /// <summary>
    /// Whether this node is a root node (e.g. "This PC", ZIP root, drive root).
    /// </summary>
    public bool IsRoot { get; set; }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
