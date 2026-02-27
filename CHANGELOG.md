# Changelog

All notable changes to **BetterBreadcrumbBar for WinUI 3** are documented here.

---

## [0.9.0]

### Added
- **Right-click context menu** on every interactive element and on any empty area of the bar:
  - Single centralised handler with hit-testing — no duplicate-menu issues
  - `Node` in event args reflects the actual element clicked (segment, chevron parent, or current node for empty areas)
  - Segment buttons, chevron/separator buttons, leading icon, overflow (`…`) button
- **Built-in menu items** (always present, always first):
  - *Copy path as text* — copies the node's `FullPath` to the clipboard automatically; also raises `ContextMenuItemClicked` with `IsCopyPath = true`
  - *Paste and go* — raises `ContextMenuItemClicked` with `IsPasteAndGo = true`; the host is responsible for reading the clipboard and calling `SetPath`
- **`CopyPathMenuText` DP** — localise the "Copy path as text" label (default: `"Copy path as text"`)
- **`PasteAndGoMenuText` DP** — localise the "Paste and go" label (default: `"Paste and go"`)
- **`ContextMenuItems` DP** — `ObservableCollection<BreadcrumbContextMenuItem>` for custom menu entries; each item exposes `Text`, `Tag`, `IsEnabled`, `HasSeparatorBefore`; custom items appear below a separator after the two built-in entries
- **`ContextMenuItemClicked` event** (`BreadcrumbContextMenuItemClickedEventArgs`) — fired for all menu clicks (built-in and custom); args carry `Node`, `Path`, `ItemTag`, `IsCopyPath`, `IsPasteAndGo`
- **`BreadcrumbContextMenuItem`** public class — used to populate `ContextMenuItems`
- **`BreadcrumbContextMenuItemClickedEventArgs`** public class — event args for `ContextMenuItemClicked`

### Fixed
- ESC key in inline address bar now reliably exits edit mode and restores the segment view

---

## [0.8.7]

### Fixed
- NuGet package `Project Website` and `Source Repository` URLs corrected to `https://github.com/zipgenius/BetterBreadcrumbBar_WinUI3`
- `FindDescendant` build error resolved (was caused by missing namespace in partial class context)

---

## [0.8.6]

### Fixed
- Demo app window title and header `TextBlock` were still showing `v0.7.3`; now correctly display the current version number

---

## [0.8.5]

### Added
- Chevron dropdown and overflow flyout now cap at **10 visible rows** (scrollable via `ScrollViewer`); implemented via `MenuFlyoutPresenterStyle` with `MaxHeight`

### Changed
- Demo app: removed hard-coded `FontFamily="Cascadia Code"` from both breadcrumb controls so they inherit the system font
