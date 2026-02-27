// =============================================================================
// BetterBreadcrumbBar for WinUI 3
// Author:  Matteo Riso
// Version: 0.9.0
// Website: https://zipgenius.it
// Written with Claude AI
// License: MIT
// =============================================================================

namespace BetterBreadcrumbBar.Control;

/// <summary>
/// Represents a custom item in the breadcrumb context menu.
/// Add instances to <see cref="BetterBreadcrumbBar.ContextMenuItems"/> to extend
/// the right-click menu with application-specific commands.
/// </summary>
public class BreadcrumbContextMenuItem
{
    /// <summary>
    /// Text displayed in the menu.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional arbitrary tag that will be forwarded to
    /// <see cref="BreadcrumbContextMenuItemClickedEventArgs.ItemTag"/> so the host
    /// can distinguish between multiple custom items without sub-classing.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// When <c>true</c> a separator line is drawn above this item.
    /// </summary>
    public bool HasSeparatorBefore { get; set; }

    /// <summary>
    /// When <c>true</c> the item is shown but cannot be clicked.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Event arguments raised when the user clicks an item in the breadcrumb context menu.
/// </summary>
public class BreadcrumbContextMenuItemClickedEventArgs : EventArgs
{
    /// <summary>
    /// The path node that was right-clicked.
    /// For a segment button this is the node of that segment.
    /// For a chevron this is the parent node whose children are listed.
    /// For the leading icon this is the first (root) node.
    /// </summary>
    public PathNode Node { get; }

    /// <summary>
    /// The full path string of <see cref="Node"/>.
    /// Convenience shortcut for <c>Node.FullPath</c>.
    /// </summary>
    public string Path => Node.FullPath;

    /// <summary>
    /// The <see cref="BreadcrumbContextMenuItem.Tag"/> of the item that was clicked,
    /// or <c>null</c> for the two built-in items (<em>Copy path</em> / <em>Paste and go</em>).
    /// </summary>
    public object? ItemTag { get; }

    /// <summary>
    /// <c>true</c> when the user clicked the built-in <em>Copy path as text</em> item.
    /// </summary>
    public bool IsCopyPath { get; }

    /// <summary>
    /// <c>true</c> when the user clicked the built-in <em>Paste and go</em> item.
    /// </summary>
    public bool IsPasteAndGo { get; }

    internal BreadcrumbContextMenuItemClickedEventArgs(
        PathNode node, object? itemTag, bool isCopyPath, bool isPasteAndGo)
    {
        Node        = node;
        ItemTag     = itemTag;
        IsCopyPath  = isCopyPath;
        IsPasteAndGo = isPasteAndGo;
    }
}
