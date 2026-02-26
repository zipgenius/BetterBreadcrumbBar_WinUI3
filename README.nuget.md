# BetterBreadcrumbBar for WinUI 3

**Author:** Matteo Riso — [zipgenius.it](https://zipgenius.it)
**Version:** 0.8.6 · Written with Claude AI · [MIT License](LICENSE)

A Windows-Explorer-style breadcrumb navigation bar for **WinUI 3 / Windows App SDK**, definitely better than the official WinApp SDK Breadcrumb bar.

## Features

| Feature | Property / Event |
|---|---|
| Clickable path segments | `SegmentClicked` |
| Chevron dropdowns (siblings / children) | `NodeSelected`, `IPathProvider` |
| Optional last-segment chevron | `ShowLastSegmentChevron` |
| Optional leading icon (`SymbolIcon`, `FontIcon`, …) | `LeadingIcon` |
| Optional Back / Up / Home buttons | `ShowBackButton`, `ShowUpButton`, `ShowHomeButton` |
| Customisable button tooltips | `BackButtonTooltip`, `UpButtonTooltip`, `HomeButtonTooltip` |
| Bar tooltip shows current path | `CurrentPath` (read-only DP) |
| Inline address bar with autocomplete | `PathSubmitted`, `IPathProvider.GetSuggestionsAsync` |
| Overflow management (narrow windows) | automatic "…" button |
| Right-to-left layout | `FlowDirection="RightToLeft"` |
| Full typography customisation | `FontFamily`, `FontSize`, `FontWeight`, `FontStyle`, `FontStretch`, `Foreground`, `CharacterSpacing` |
| PerMonitorV2 Hi-DPI | `app.manifest` |

## Requirements

- Windows App SDK **1.8+** runtime installed on the target machine
- .NET 9 / Windows 10 19041+

## Quick start

```xml
xmlns:ctrl="using:BetterBreadcrumbBar.Control"

<ctrl:BetterBreadcrumbBar
    x:Name="MyBreadcrumb"
    PathProvider="{x:Bind MyProvider}"
    ShowLastSegmentChevron="True"
    ShowBackButton="True"
    ShowUpButton="True"
    ShowHomeButton="True"
    CanGoBack="{x:Bind CanGoBack, Mode=OneWay}"
    SegmentClicked="OnSegmentClicked"
    NodeSelected="OnNodeSelected"
    PathSubmitted="OnPathSubmitted"
    BackRequested="OnBackRequested"
    UpRequested="OnUpRequested"
    HomeRequested="OnHomeRequested"/>
```

```csharp
// Navigate to a path
MyBreadcrumb.SetPath(@"C:\Users\Public\Documents");

// Or supply an ordered list of PathNode objects
MyBreadcrumb.SetPath(myProvider.BuildPathNodes(currentPath));
```

## IPathProvider

```csharp
public class MyProvider : IPathProvider
{
    public Task<IEnumerable<PathNode>> GetChildrenAsync(PathNode node, CancellationToken ct)
        => /* return child nodes */ Task.FromResult(Enumerable.Empty<PathNode>());

    // Optional: drives autocomplete in the inline address bar
    public Task<IEnumerable<string>> GetSuggestionsAsync(string partial, CancellationToken ct)
        => /* return matching paths */ Task.FromResult(Enumerable.Empty<string>());
}
```

Two ready-made providers are included:
- **`FileSystemPathProvider`** — Windows filesystem paths (`C:\Users\...`)
- **`VirtualPathProvider`** — virtual / ZIP-style paths (`src/components/...`)

## Typography

All standard WinUI 3 typography properties are fully supported and live-updatable:

```xml
<ctrl:BetterBreadcrumbBar
    FontFamily="Cascadia Code"
    FontSize="14"
    FontWeight="SemiBold"/>
```

## License

MIT — see [LICENSE](LICENSE).
