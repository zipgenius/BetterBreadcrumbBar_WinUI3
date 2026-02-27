# BetterBreadcrumbBar for WinUI 3

A Windows-Explorer-style breadcrumb navigation bar for **WinUI 3 / Windows App SDK**.

![Version](https://img.shields.io/badge/version-0.9.0-blue)
![NuGet](https://img.shields.io/nuget/v/BetterBreadcrumbBar.WinUI3)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-lightgrey)

---

https://github.com/user-attachments/assets/b79f1ed0-f39f-47b1-888b-8c86d8ebfbe2

## Features

- **Clickable path segments** — each segment is a button that raises `SegmentClicked`
- **Chevron dropdowns** — each separator opens a `MenuFlyout` listing sibling folders; scrollable, capped at 10 visible rows
- **Inline address bar** — clicking the empty area of the bar switches to an `AutoSuggestBox` with provider-driven autocomplete; confirmed with Enter, dismissed with Escape or a click outside
- **Overflow management** — when the bar is too narrow to display all segments, a `…` button appears on the left and opens a flyout with the hidden leading segments
- **Right-click context menu** — right-clicking anywhere on the bar (segments, chevrons, leading icon, overflow button, nav buttons, or any empty area between elements) opens a context menu; the `Node` in the event args reflects the specific element clicked — the segment node, the parent node for chevrons, or the current (last) node for empty areas and nav buttons; two built-in items (*Copy path as text* and *Paste and go*) are always present and label texts are fully localisable
- **Optional navigation buttons** — Back, Up, and Home buttons can be shown individually, each with a customisable tooltip
- **Optional leading icon** — accepts any `IconElement` subclass (`SymbolIcon`, `FontIcon`, `BitmapIcon`, `PathIcon`, …)
- **Full typography support** — `FontFamily`, `FontSize`, `FontWeight`, `FontStyle`, `FontStretch`, `Foreground`, and `CharacterSpacing` are all live-updatable and propagated correctly to every segment button, bypassing WinUI 3's theme override mechanism
- **Right-to-left layout** — set `FlowDirection="RightToLeft"` and the bar layout, chevron glyphs, and flyout placement all adapt automatically; the inline address box stays LTR so file paths always read correctly
- **PerMonitorV2 Hi-DPI** — declared in `app.manifest`
- **Provider pattern** — implement `IPathProvider` to connect any data source: local filesystem, ZIP archives, FTP servers, databases, or any virtual hierarchy
- **Requires** Windows App SDK 1.8+ runtime, .NET 9, Windows 10 1809 (build 17763)+

---

## Installation

```
dotnet add package BetterBreadcrumbBar.WinUI3
```

or via the NuGet Package Manager in Visual Studio — search for `BetterBreadcrumbBar.WinUI3`.

---

## Quick start

### 1. Add the namespace to your XAML

```xml
xmlns:bbb="using:BetterBreadcrumbBar.Control"
```

### 2. Declare the control

```xml
<bbb:BetterBreadcrumbBar
    x:Name="Breadcrumb"
    PathProvider="{x:Bind MyProvider}"
    ShowLastSegmentChevron="True"
    ShowBackButton="True"
    ShowUpButton="True"
    ShowHomeButton="True"
    CanGoBack="{x:Bind CanGoBack, Mode=OneWay}"
    SegmentClicked="Breadcrumb_SegmentClicked"
    NodeSelected="Breadcrumb_NodeSelected"
    PathSubmitted="Breadcrumb_PathSubmitted"
    BackRequested="Breadcrumb_BackRequested"
    UpRequested="Breadcrumb_UpRequested"
    HomeRequested="Breadcrumb_HomeRequested"
    ContextMenuItemClicked="Breadcrumb_ContextMenuItemClicked">
    <bbb:BetterBreadcrumbBar.LeadingIcon>
        <SymbolIcon Symbol="Folder"/>
    </bbb:BetterBreadcrumbBar.LeadingIcon>
</bbb:BetterBreadcrumbBar>
```

### 3. Navigate to a path

```csharp
// From a Windows filesystem path string
Breadcrumb.SetPath(@"C:\Users\Public\Documents");

// From an explicit list of PathNode objects (useful for virtual paths)
Breadcrumb.SetPath(new[]
{
    new PathNode { Name = "archive.zip", FullPath = "",             IsRoot = true },
    new PathNode { Name = "src",         FullPath = "src"                         },
    new PathNode { Name = "components",  FullPath = "src/components"              },
});
```

---

## IPathProvider

The control never reads the filesystem or any archive directly. All data comes through an
`IPathProvider` that you implement and assign to the `PathProvider` property.

```csharp
public interface IPathProvider
{
    // Called when the user opens a chevron dropdown or the control
    // needs to verify whether the last segment has children.
    Task<IEnumerable<PathNode>> GetChildrenAsync(
        PathNode node,
        CancellationToken cancellationToken = default);

    // Called while the user types in the inline address bar.
    // The default implementation returns an empty list — override
    // to provide meaningful autocomplete suggestions.
    Task<IEnumerable<string>> GetSuggestionsAsync(
        string partialPath,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<string>());
}
```

### Built-in providers

Two ready-to-use providers are included in `BetterBreadcrumbBar.Control.Providers`:

| Class | Description |
|---|---|
| `FileSystemPathProvider` | Reads real folders from the Windows filesystem. Skips hidden+system directories. Supports autocomplete (prefix-match on sibling folders). |
| `VirtualPathProvider` | Operates on a virtual path tree built from a flat list of slash-separated strings (e.g. entries extracted from a ZIP). Supports autocomplete. |

---

## Usage examples

### Example 1 — Real filesystem with history stack

```xml
<bbb:BetterBreadcrumbBar
    x:Name="FsBreadcrumb"
    PathProvider="{x:Bind FsProvider}"
    ShowLastSegmentChevron="True"
    ShowBackButton="True"
    ShowUpButton="True"
    ShowHomeButton="True"
    CanGoBack="{x:Bind CanGoBack, Mode=OneWay}"
    BackButtonTooltip="Go back"
    UpButtonTooltip="Parent folder"
    HomeButtonTooltip="Home"
    SegmentClicked="FsBreadcrumb_SegmentClicked"
    NodeSelected="FsBreadcrumb_NodeSelected"
    PathSubmitted="FsBreadcrumb_PathSubmitted"
    BackRequested="FsBreadcrumb_BackRequested"
    UpRequested="FsBreadcrumb_UpRequested"
    HomeRequested="FsBreadcrumb_HomeRequested"
    ContextMenuItemClicked="FsBreadcrumb_ContextMenuItemClicked">
    <bbb:BetterBreadcrumbBar.LeadingIcon>
        <SymbolIcon Symbol="Folder"/>
    </bbb:BetterBreadcrumbBar.LeadingIcon>
</bbb:BetterBreadcrumbBar>
```

```csharp
using BetterBreadcrumbBar.Control;
using BetterBreadcrumbBar.Control.Providers;

public sealed partial class MainWindow : Window
{
    public FileSystemPathProvider FsProvider { get; } = new();

    private readonly Stack<List<PathNode>> _history = new();
    private string _currentPath = @"C:\Users";

    private bool _canGoBack;
    public bool CanGoBack
    {
        get => _canGoBack;
        private set { _canGoBack = value; /* raise PropertyChanged */ }
    }

    public MainWindow()
    {
        InitializeComponent();
        Navigate(@"C:\Users", push: false);
    }

    private void Navigate(string path, bool push = true)
    {
        if (!Directory.Exists(path)) return;

        if (push && _currentPath != path)
        {
            _history.Push(FileSystemPathProvider.BuildPathNodes(_currentPath));
            CanGoBack = true;
        }
        _currentPath = path;
        FsBreadcrumb.SetPath(FileSystemPathProvider.BuildPathNodes(path));
    }

    private void FsBreadcrumb_SegmentClicked(object s, PathNodeEventArgs e)
        => Navigate(e.Node.FullPath);

    private void FsBreadcrumb_NodeSelected(object s, PathNodeEventArgs e)
        => Navigate(e.Node.FullPath);

    private void FsBreadcrumb_PathSubmitted(object s, PathSubmittedEventArgs e)
    {
        if (Directory.Exists(e.Path))
            Navigate(e.Path);
    }

    private void FsBreadcrumb_BackRequested(object s, EventArgs e)
    {
        if (_history.Count == 0) return;
        var prev = _history.Pop();
        CanGoBack    = _history.Count > 0;
        _currentPath = prev[^1].FullPath;
        FsBreadcrumb.SetPath(prev);
    }

    private void FsBreadcrumb_UpRequested(object s, PathNodeEventArgs e)
        => Navigate(e.Node.FullPath);

    private void FsBreadcrumb_HomeRequested(object s, EventArgs e)
        => Navigate(@"C:\Users");

    private async void FsBreadcrumb_ContextMenuItemClicked(
        object s, BreadcrumbContextMenuItemClickedEventArgs e)
    {
        if (e.IsPasteAndGo)
        {
            var text = await Clipboard.GetContent().GetTextAsync();
            if (Directory.Exists(text)) Navigate(text);
        }
        // IsCopyPath: clipboard already written by the control — react only if needed
        // Custom items: inspect e.ItemTag
    }
}
```

---

### Example 2 — Virtual path tree (e.g. ZIP archive contents)

```csharp
using BetterBreadcrumbBar.Control;
using BetterBreadcrumbBar.Control.Providers;

string[] entries =
{
    "src/components/ui/Button.cs",
    "src/components/ui/TextBox.cs",
    "src/services/AuthService.cs",
    "docs/api/reference.md",
    "docs/guides/quickstart.md",
};

var (provider, root) = VirtualPathProvider.FromPaths(entries, rootName: "archive.zip");
ZipBreadcrumb.PathProvider = provider;

ZipBreadcrumb.SetPath(provider.BuildPathNodes(""));
ZipBreadcrumb.SetPath(provider.BuildPathNodes("src/components"));
```

```xml
<bbb:BetterBreadcrumbBar
    x:Name="ZipBreadcrumb"
    ShowLastSegmentChevron="True"
    ShowBackButton="True"
    ShowUpButton="True"
    ShowHomeButton="True"
    CanGoBack="{x:Bind ZipCanGoBack, Mode=OneWay}"
    SegmentClicked="ZipBreadcrumb_SegmentClicked"
    NodeSelected="ZipBreadcrumb_NodeSelected"
    PathSubmitted="ZipBreadcrumb_PathSubmitted"
    BackRequested="ZipBreadcrumb_BackRequested"
    UpRequested="ZipBreadcrumb_UpRequested"
    HomeRequested="ZipBreadcrumb_HomeRequested"
    ContextMenuItemClicked="ZipBreadcrumb_ContextMenuItemClicked">
    <bbb:BetterBreadcrumbBar.LeadingIcon>
        <FontIcon Glyph="&#xE8B7;" FontSize="14"/>
    </bbb:BetterBreadcrumbBar.LeadingIcon>
</bbb:BetterBreadcrumbBar>
```

---

### Example 3 — Custom IPathProvider (FTP, database, …)

```csharp
public class FtpPathProvider : IPathProvider
{
    private readonly FtpClient _client;

    public FtpPathProvider(FtpClient client) => _client = client;

    public async Task<IEnumerable<PathNode>> GetChildrenAsync(
        PathNode node, CancellationToken ct = default)
    {
        var listing = await _client.GetListingAsync(node.FullPath, ct);
        return listing
            .Where(e => e.Type == FtpObjectType.Directory)
            .Select(e => new PathNode { Name = e.Name, FullPath = e.FullName });
    }

    public async Task<IEnumerable<string>> GetSuggestionsAsync(
        string partial, CancellationToken ct = default)
    {
        string parent = partial.Contains('/')
            ? partial[..partial.LastIndexOf('/')]
            : "/";

        var listing = await _client.GetListingAsync(parent, ct);
        string prefix = partial.Split('/')[^1];
        return listing
            .Where(e => e.Type == FtpObjectType.Directory
                     && e.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.FullName)
            .Take(20);
    }
}
```

---

### Example 4 — Right-click context menu with custom items

Right-clicking **anywhere on the bar** — a segment, a chevron, the leading icon, the overflow button, a nav button, or any empty area — opens the context menu. The `Node` in the event args reflects what was clicked: the specific segment node, the parent node for a chevron, or the current (last) node for empty areas and nav buttons.

```xml
<bbb:BetterBreadcrumbBar
    x:Name="Breadcrumb"
    PathProvider="{x:Bind Provider}"
    CopyPathMenuText="Copy path"
    PasteAndGoMenuText="Paste and navigate"
    ContextMenuItemClicked="Breadcrumb_ContextMenuItemClicked">
    <bbb:BetterBreadcrumbBar.ContextMenuItems>
        <bbb:BreadcrumbContextMenuItem Text="Open in Terminal"  Tag="terminal"/>
        <bbb:BreadcrumbContextMenuItem Text="Open in Explorer"  Tag="explorer"/>
        <bbb:BreadcrumbContextMenuItem Text="Add to Favourites" Tag="fav" HasSeparatorBefore="True"/>
    </bbb:BetterBreadcrumbBar.ContextMenuItems>
</bbb:BetterBreadcrumbBar>
```

```csharp
private async void Breadcrumb_ContextMenuItemClicked(
    object s, BreadcrumbContextMenuItemClickedEventArgs e)
{
    // e.Node   — PathNode that was right-clicked
    // e.Path   — shortcut for e.Node.FullPath
    // e.ItemTag — Tag set on BreadcrumbContextMenuItem

    if (e.IsPasteAndGo)
    {
        var text = await Clipboard.GetContent().GetTextAsync();
        if (Directory.Exists(text)) Navigate(text);
        return;
    }

    if (e.IsCopyPath) return; // already written to clipboard by the control

    switch (e.ItemTag as string)
    {
        case "terminal":
            Process.Start("wt.exe", $"-d \"{e.Path}\"");
            break;
        case "explorer":
            Process.Start("explorer.exe", $"\"{e.Path}\"");
            break;
        case "fav":
            AddToFavourites(e.Node);
            break;
    }
}
```

---

### Example 5 — Typography and RTL

```xml
<!-- Custom font applied to all segments -->
<bbb:BetterBreadcrumbBar
    PathProvider="{x:Bind Provider}"
    FontSize="13"
    FontWeight="SemiBold"
    Foreground="{ThemeResource AccentTextFillColorPrimaryBrush}"
    SegmentClicked="OnSegmentClicked"
    NodeSelected="OnNodeSelected"/>

<!-- Right-to-left layout for Arabic / Hebrew UIs -->
<bbb:BetterBreadcrumbBar
    PathProvider="{x:Bind Provider}"
    FlowDirection="RightToLeft"
    ShowBackButton="True"
    ShowUpButton="True"
    BackButtonTooltip="رجوع"
    UpButtonTooltip="المجلد الأصل"
    CopyPathMenuText="نسخ المسار"
    PasteAndGoMenuText="لصق والانتقال"
    SegmentClicked="OnSegmentClicked"
    NodeSelected="OnNodeSelected"/>
```

---

## API reference

### Dependency properties

| Property | Type | Default | Description |
|---|---|---|---|
| `PathProvider` | `IPathProvider?` | `null` | Supplies child nodes and autocomplete suggestions |
| `ShowLastSegmentChevron` | `bool` | `false` | Shows a chevron on the last segment (only if the provider confirms children exist) |
| `LeadingIcon` | `IconElement?` | `null` | Optional icon displayed before the first segment |
| `ShowBackButton` | `bool` | `false` | Shows the Back (←) navigation button |
| `ShowUpButton` | `bool` | `false` | Shows the Up (↑) navigation button |
| `ShowHomeButton` | `bool` | `false` | Shows the Home button |
| `BackButtonTooltip` | `string` | `"Back"` | Tooltip for the Back button |
| `UpButtonTooltip` | `string` | `"Up — parent folder"` | Tooltip for the Up button |
| `HomeButtonTooltip` | `string` | `"Home"` | Tooltip for the Home button |
| `CurrentPath` | `string` | `""` | Read-only. Full path of the current node. Also shown as bar tooltip. |
| `CanGoBack` | `bool` | `false` | Set by the host to enable or disable the Back button |
| `CanGoUp` | `bool` | computed | Read-only. `true` when the path has at least two segments |
| `FlowDirection` | `FlowDirection` | `LeftToRight` | LTR or RTL layout |
| `ContextMenuItems` | `ObservableCollection<BreadcrumbContextMenuItem>` | `null` | Custom items appended to the right-click menu |
| `CopyPathMenuText` | `string` | `"Copy path as text"` | Localisable label for the built-in Copy item |
| `PasteAndGoMenuText` | `string` | `"Paste and go"` | Localisable label for the built-in Paste item |

All standard `Control` typography properties (`FontFamily`, `FontSize`, `FontWeight`, `FontStyle`, `FontStretch`, `Foreground`, `CharacterSpacing`) are fully supported and propagated to every segment.

### Events

| Event | Args | Description |
|---|---|---|
| `SegmentClicked` | `PathNodeEventArgs` | User clicked a segment label |
| `NodeSelected` | `PathNodeEventArgs` | User chose an item from a chevron dropdown |
| `PathSubmitted` | `PathSubmittedEventArgs` | User confirmed a path in the inline address bar |
| `BackRequested` | `EventArgs` | Back button clicked |
| `UpRequested` | `PathNodeEventArgs` | Up button clicked; `Node` is the parent |
| `HomeRequested` | `EventArgs` | Home button clicked |
| `ContextMenuItemClicked` | `BreadcrumbContextMenuItemClickedEventArgs` | Any context menu item clicked (built-in or custom) |

### Methods

| Method | Description |
|---|---|
| `SetPath(IEnumerable<PathNode>)` | Displays an ordered list of nodes (root → current) |
| `SetPath(string)` | Convenience overload that parses a Windows filesystem path string |
| `GetCurrentNode()` | Returns the last (current) `PathNode`, or `null` if empty |
| `GetParentNode()` | Returns the second-to-last `PathNode`, or `null` if at root |

### BreadcrumbContextMenuItem

```csharp
public class BreadcrumbContextMenuItem
{
    public string  Text               { get; set; }  // Menu item label
    public object? Tag                { get; set; }  // Forwarded to ContextMenuItemClicked args
    public bool    IsEnabled          { get; set; }  // Default: true
    public bool    HasSeparatorBefore { get; set; }  // Draws a separator line above this item
}
```

### BreadcrumbContextMenuItemClickedEventArgs

```csharp
public class BreadcrumbContextMenuItemClickedEventArgs : EventArgs
{
    public PathNode Node         { get; }  // Node that was right-clicked
    public string   Path         { get; }  // Shortcut for Node.FullPath
    public object?  ItemTag      { get; }  // Tag of the custom item; null for built-in items
    public bool     IsCopyPath   { get; }  // true for the built-in Copy item
    public bool     IsPasteAndGo { get; }  // true for the built-in Paste item
}
```

---

## PathNode

```csharp
public class PathNode
{
    public string Name     { get; set; }  // Display label, e.g. "Documents"
    public string FullPath { get; set; }  // Full path to this node, e.g. "C:\Users\Public\Documents"
    public bool   IsRoot   { get; set; }  // True for drive roots, ZIP roots, etc.
    public ObservableCollection<PathNode> Children { get; }  // Populated by IPathProvider
}
```

---

## Requirements

| Requirement | Value |
|---|---|
| .NET | 9.0 |
| Windows App SDK | 1.8+ |
| Minimum OS | Windows 10 1809 (build 17763) |
| Deployment | Unpackaged (`WindowsPackageType=None`) |
| Architecture | x86 · x64 · ARM64 |

---

## License

MIT — see [LICENSE](LICENSE) for details.

---

## Author

**Matteo Riso** — [zipgenius.it](https://zipgenius.it)
