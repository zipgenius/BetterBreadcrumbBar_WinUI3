// =============================================================================
// BetterBreadcrumbBar for WinUI 3
// Author:  Matteo Riso
// Version: 0.8.7
// Website: https://zipgenius.it
// Written with Claude AI
// License: MIT
// =============================================================================

namespace BetterBreadcrumbBar.Control;

/// <summary>
/// Contract that consumers of the control must implement to supply child nodes
/// for a given path node. The control does not read the filesystem or ZIP archives
/// directly — that is the provider's responsibility.
/// </summary>
public interface IPathProvider
{
    /// <summary>
    /// Returns the direct children of the specified node.
    /// Called asynchronously when the user opens the chevron dropdown menu,
    /// or when the control needs to verify whether the last segment has children.
    /// </summary>
    /// <param name="node">The node whose children should be retrieved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A sequence of child nodes.</returns>
    Task<IEnumerable<PathNode>> GetChildrenAsync(
        PathNode node,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns path suggestions for the inline address bar's autocomplete.
    /// Called while the user types in the inline editing box.
    /// <para>
    /// The default implementation returns an empty list.
    /// Override this method to provide meaningful completions
    /// (e.g. filesystem directory enumeration, virtual path prefix matching).
    /// </para>
    /// </summary>
    /// <param name="partialPath">The text the user has typed so far.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A sequence of suggestion strings shown below the address box.</returns>
    Task<IEnumerable<string>> GetSuggestionsAsync(
        string partialPath,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<string>());
}
