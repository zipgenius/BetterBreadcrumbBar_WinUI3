// =============================================================================
// BetterBreadcrumbBar for WinUI 3
// Author:  Matteo Riso
// Version: 0.8.6
// Website: https://zipgenius.it
// Written with Claude AI
// License: MIT
// =============================================================================

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Microsoft.UI.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BetterBreadcrumbBar.Control;

/// <summary>
/// Internal view-model representing a single breadcrumb segment.
/// Not part of the public API.
/// </summary>
internal class PathSegment : INotifyPropertyChanged
{
    private bool       _isSeparatorLoading;
    private bool       _isLast;
    private bool       _showLastChevron;
    private bool       _lastChevronVerified;
    private bool       _isHiddenByOverflow;
    private string     _chevronGlyph    = ChevronLtr;

    // Typography fields — kept in sync with BetterBreadcrumbBar via ApplyTypography()
    private FontFamily _fontFamily      = new FontFamily("XamlAutoFontFamily");
    private double     _fontSize        = 13;
    private FontWeight _fontWeight      = FontWeights.Normal;
    private FontStyle  _fontStyle       = FontStyle.Normal;
    private FontStretch _fontStretch    = FontStretch.Normal;
    private Brush?     _foreground      = null;   // null = inherit from theme
    private int        _characterSpacing = 0;

    internal const string ChevronLtr = "\uE76C";   // ❯  right-pointing
    internal const string ChevronRtl = "\uE76B";   // ❮  left-pointing

    /// <summary>The path node this segment represents.</summary>
    public PathNode Node { get; }

    /// <summary>Text displayed on the segment button.</summary>
    public string Label => Node.Name;

    // ── Typography — bound directly to the Button in the DataTemplate ─────────
    // Setting values here bypasses the Button's default theme ControlTemplate
    // which would otherwise override font properties via local ThemeResource values.

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set { _fontFamily = value; Notify(); }
    }

    public double FontSize
    {
        get => _fontSize;
        set { _fontSize = value; Notify(); }
    }

    public FontWeight FontWeight
    {
        get => _fontWeight;
        set { _fontWeight = value; Notify(); }
    }

    public FontStyle FontStyle
    {
        get => _fontStyle;
        set { _fontStyle = value; Notify(); }
    }

    public FontStretch FontStretch
    {
        get => _fontStretch;
        set { _fontStretch = value; Notify(); }
    }

    /// <summary>
    /// Foreground brush for the segment text.
    /// <c>null</c> means "use the theme default" — in that case the binding falls back
    /// to the inherited value from the parent visual tree.
    /// </summary>
    public Brush? Foreground
    {
        get => _foreground;
        set { _foreground = value; Notify(); }
    }

    public int CharacterSpacing
    {
        get => _characterSpacing;
        set { _characterSpacing = value; Notify(); }
    }

    // ── Chevron glyph ─────────────────────────────────────────────────────────

    /// <summary>
    /// The Segoe Fluent Icons glyph used for the separator chevron.
    /// Set to <see cref="ChevronLtr"/> (&#xE76C; ❯) for LTR and
    /// <see cref="ChevronRtl"/> (&#xE76B; ❮) for RTL.
    /// </summary>
    public string ChevronGlyph
    {
        get => _chevronGlyph;
        set { _chevronGlyph = value; Notify(); }
    }

    // ── Overflow / visibility ─────────────────────────────────────────────────

    /// <summary>Whether the separator/chevron is currently loading children.</summary>
    public bool IsSeparatorLoading
    {
        get => _isSeparatorLoading;
        set { _isSeparatorLoading = value; Notify(); Notify(nameof(SeparatorVisibility)); }
    }

    /// <summary>Whether this is the last segment in the path.</summary>
    public bool IsLast
    {
        get => _isLast;
        set { _isLast = value; Notify(); Notify(nameof(SeparatorVisibility)); }
    }

    /// <summary>
    /// Set to <c>true</c> by the control after the provider confirms the node has children,
    /// causing the trailing chevron to become visible on the last segment.
    /// </summary>
    public bool ShowLastChevron
    {
        get => _showLastChevron;
        set { _showLastChevron = value; Notify(); Notify(nameof(SeparatorVisibility)); }
    }

    /// <summary>Becomes <c>true</c> once the provider has responded for the final node.</summary>
    public bool LastChevronVerified
    {
        get => _lastChevronVerified;
        set { _lastChevronVerified = value; Notify(); Notify(nameof(SeparatorVisibility)); }
    }

    /// <summary>
    /// Set to <c>true</c> by the overflow logic when this segment is among the leading
    /// ones that no longer fit in the available bar width.
    /// </summary>
    public bool IsHiddenByOverflow
    {
        get => _isHiddenByOverflow;
        set { _isHiddenByOverflow = value; Notify(); Notify(nameof(SegmentVisibility)); }
    }

    /// <summary>Visibility of the whole segment; Collapsed when hidden by overflow.</summary>
    public Visibility SegmentVisibility
        => _isHiddenByOverflow ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// Visibility of the separator/chevron button:
    /// always visible for intermediate segments; visible on the last segment
    /// only when <see cref="ShowLastChevron"/> is <c>true</c>.
    /// </summary>
    public Visibility SeparatorVisibility
        => IsLast
            ? (ShowLastChevron ? Visibility.Visible : Visibility.Collapsed)
            : Visibility.Visible;

    public PathSegment(PathNode node, bool isLast = false)
    {
        Node    = node;
        _isLast = isLast;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
