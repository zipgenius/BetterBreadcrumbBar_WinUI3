// =============================================================================
// BetterBreadcrumbBar for WinUI 3
// Author:  Matteo Riso
// Version: 0.8.7
// Website: https://zipgenius.it
// Written with Claude AI
// License: MIT
// =============================================================================

namespace BetterBreadcrumbBar.Control.Providers;

/// <summary>
/// <see cref="IPathProvider"/> implementation that reads real folders from the Windows file system.
/// Also implements <see cref="IPathProvider.GetSuggestionsAsync"/> for inline address-bar autocomplete.
/// </summary>
public class FileSystemPathProvider : IPathProvider
{
    /// <inheritdoc/>
    public Task<IEnumerable<PathNode>> GetChildrenAsync(
        PathNode node, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = new List<PathNode>();

            try
            {
                // Special case: root node with no path → list drives
                if (node.IsRoot && string.IsNullOrEmpty(node.FullPath))
                {
                    foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                    {
                        results.Add(new PathNode
                        {
                            Name     = drive.Name,
                            FullPath = drive.RootDirectory.FullName,
                            IsRoot   = true
                        });
                    }
                    return results.AsEnumerable();
                }

                if (!Directory.Exists(node.FullPath)) return results;

                foreach (var dir in Directory.EnumerateDirectories(node.FullPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var info = new DirectoryInfo(dir);
                        // Skip hidden + system directories
                        if ((info.Attributes & FileAttributes.Hidden) != 0 &&
                            (info.Attributes & FileAttributes.System) != 0)
                            continue;

                        results.Add(new PathNode { Name = info.Name, FullPath = info.FullName });
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException)   { }

            return results.AsEnumerable();
        }, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// For filesystem paths, suggestions are the sub-directories of the parent directory
    /// that match the typed prefix. For example, typing "C:\Pro" suggests "C:\Program Files"
    /// and "C:\Program Files (x86)".
    /// </remarks>
    public Task<IEnumerable<string>> GetSuggestionsAsync(
        string partialPath, CancellationToken cancellationToken = default)
    {
        return Task.Run<IEnumerable<string>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var suggestions = new List<string>();
            if (string.IsNullOrEmpty(partialPath)) return suggestions;

            try
            {
                // If partialPath is a complete directory, list its children
                if (Directory.Exists(partialPath))
                {
                    foreach (var dir in Directory.EnumerateDirectories(partialPath))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var info = new DirectoryInfo(dir);
                            if ((info.Attributes & FileAttributes.Hidden) != 0 &&
                                (info.Attributes & FileAttributes.System) != 0)
                                continue;
                            suggestions.Add(info.FullName);
                            if (suggestions.Count >= 20) break;
                        }
                        catch (UnauthorizedAccessException) { }
                    }
                }
                else
                {
                    // List siblings that start with the typed text
                    string? parent = Path.GetDirectoryName(partialPath);
                    if (parent != null && Directory.Exists(parent))
                    {
                        string prefix = Path.GetFileName(partialPath);
                        foreach (var dir in Directory.EnumerateDirectories(parent))
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                var info = new DirectoryInfo(dir);
                                if ((info.Attributes & FileAttributes.Hidden) != 0 &&
                                    (info.Attributes & FileAttributes.System) != 0)
                                    continue;
                                if (info.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                    suggestions.Add(info.FullName);
                                if (suggestions.Count >= 20) break;
                            }
                            catch (UnauthorizedAccessException) { }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException)  { }
            catch (IOException)                 { }

            return suggestions;
        }, cancellationToken);
    }

    /// <summary>
    /// Builds an ordered list of <see cref="PathNode"/> objects from a full filesystem path.
    /// </summary>
    /// <param name="fullPath">Absolute Windows path, e.g. <c>C:\Users\Public</c>.</param>
    public static List<PathNode> BuildPathNodes(string fullPath)
    {
        var nodes = new List<PathNode>();
        var parts = fullPath.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        string acc = string.Empty;
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (i == 0 && part.Length == 2 && part[1] == ':')
            {
                acc = part + Path.DirectorySeparatorChar;
                nodes.Add(new PathNode { Name = part + "\\", FullPath = acc, IsRoot = true });
            }
            else
            {
                acc = Path.Combine(acc, part);
                nodes.Add(new PathNode { Name = part, FullPath = acc });
            }
        }
        return nodes;
    }
}
