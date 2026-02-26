// =============================================================================
// BetterBreadcrumbBar for WinUI 3
// Author:  Matteo Riso
// Version: 0.8.5
// Website: https://zipgenius.it
// Written with Claude AI
// License: MIT
// =============================================================================

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;

namespace BetterBreadcrumbBar.Control;

/// <summary>
/// A Windows-Explorer-style breadcrumb bar for WinUI 3.
/// Displays a path as a sequence of clickable segments separated by chevron arrows.
/// Chevrons open a dropdown listing sibling (or child) folders.
/// Clicking the empty area of the bar switches to an inline address box with autocomplete.
/// When the bar is too narrow to show all segments, a "…" overflow button appears on the
/// left; clicking it opens a flyout with the hidden leading segments.
/// </summary>
public sealed partial class BetterBreadcrumbBar : UserControl
{
    #region Dependency Properties

    // ── PathProvider ─────────────────────────────────────────────────────────
    public static readonly DependencyProperty PathProviderProperty =
        DependencyProperty.Register(nameof(PathProvider), typeof(IPathProvider),
            typeof(BetterBreadcrumbBar), new PropertyMetadata(null));

    /// <summary>
    /// Provider that returns child nodes for a given path node.
    /// Implement <see cref="IPathProvider"/> to support filesystem, ZIP, or any virtual hierarchy.
    /// </summary>
    public IPathProvider? PathProvider
    {
        get => (IPathProvider?)GetValue(PathProviderProperty);
        set => SetValue(PathProviderProperty, value);
    }

    // ── ShowLastSegmentChevron ────────────────────────────────────────────────
    public static readonly DependencyProperty ShowLastSegmentChevronProperty =
        DependencyProperty.Register(nameof(ShowLastSegmentChevron), typeof(bool),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata(false, OnShowLastSegmentChevronChanged));

    /// <summary>
    /// When <c>true</c>, displays a chevron on the last segment so the user can browse
    /// into sub-folders of the current node.
    /// The chevron only appears after the provider confirms at least one child exists.
    /// Default: <c>false</c>.
    /// </summary>
    public bool ShowLastSegmentChevron
    {
        get => (bool)GetValue(ShowLastSegmentChevronProperty);
        set => SetValue(ShowLastSegmentChevronProperty, value);
    }

    private static void OnShowLastSegmentChevronChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BetterBreadcrumbBar bar && bar._segments.Count > 0)
            bar.VerifyLastSegmentChevronAsync(bar._segments[^1]);
    }

    // ── LeadingIcon ───────────────────────────────────────────────────────────
    public static readonly DependencyProperty LeadingIconProperty =
        DependencyProperty.Register(nameof(LeadingIcon), typeof(IconElement),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata(null, OnLeadingIconChanged));

    /// <summary>
    /// Optional icon displayed before the first path segment.
    /// Accepts any <see cref="IconElement"/> subclass:
    /// <see cref="SymbolIcon"/>, <see cref="FontIcon"/>, <see cref="BitmapIcon"/>,
    /// <see cref="PathIcon"/>, etc.
    /// Set to <c>null</c> to hide the icon area (default).
    /// </summary>
    public IconElement? LeadingIcon
    {
        get => (IconElement?)GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    private static void OnLeadingIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BetterBreadcrumbBar bar) bar.UpdateLeadingIconArea();
    }

    // ── ShowBackButton ────────────────────────────────────────────────────────
    public static readonly DependencyProperty ShowBackButtonProperty =
        DependencyProperty.Register(nameof(ShowBackButton), typeof(bool),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata(false, OnNavButtonVisibilityChanged));

    /// <summary>
    /// Shows the Back button (left arrow) to the left of the bar.
    /// The button is automatically disabled when <see cref="CanGoBack"/> is <c>false</c>.
    /// Raises <see cref="BackRequested"/> when clicked. Default: <c>false</c>.
    /// </summary>
    public bool ShowBackButton
    {
        get => (bool)GetValue(ShowBackButtonProperty);
        set => SetValue(ShowBackButtonProperty, value);
    }

    // ── ShowUpButton ──────────────────────────────────────────────────────────
    public static readonly DependencyProperty ShowUpButtonProperty =
        DependencyProperty.Register(nameof(ShowUpButton), typeof(bool),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata(false, OnNavButtonVisibilityChanged));

    /// <summary>
    /// Shows the Up button (navigate to parent folder) to the left of the bar.
    /// The button is automatically disabled when <see cref="CanGoUp"/> is <c>false</c>.
    /// Raises <see cref="UpRequested"/> when clicked. Default: <c>false</c>.
    /// </summary>
    public bool ShowUpButton
    {
        get => (bool)GetValue(ShowUpButtonProperty);
        set => SetValue(ShowUpButtonProperty, value);
    }

    // ── ShowHomeButton ────────────────────────────────────────────────────────
    public static readonly DependencyProperty ShowHomeButtonProperty =
        DependencyProperty.Register(nameof(ShowHomeButton), typeof(bool),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata(false, OnNavButtonVisibilityChanged));

    /// <summary>
    /// Shows the Home button to the left of the bar.
    /// Raises <see cref="HomeRequested"/> when clicked. Default: <c>false</c>.
    /// </summary>
    public bool ShowHomeButton
    {
        get => (bool)GetValue(ShowHomeButtonProperty);
        set => SetValue(ShowHomeButtonProperty, value);
    }

    private static void OnNavButtonVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BetterBreadcrumbBar bar) bar.UpdateNavButtonsArea();
    }

    // ── BackButtonTooltip ─────────────────────────────────────────────────────
    public static readonly DependencyProperty BackButtonTooltipProperty =
        DependencyProperty.Register(nameof(BackButtonTooltip), typeof(string),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata("Back", OnNavTooltipChanged));

    /// <summary>Tooltip text for the Back button. Default: <c>"Back"</c>.</summary>
    public string BackButtonTooltip
    {
        get => (string)GetValue(BackButtonTooltipProperty);
        set => SetValue(BackButtonTooltipProperty, value);
    }

    // ── UpButtonTooltip ───────────────────────────────────────────────────────
    public static readonly DependencyProperty UpButtonTooltipProperty =
        DependencyProperty.Register(nameof(UpButtonTooltip), typeof(string),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata("Up — parent folder", OnNavTooltipChanged));

    /// <summary>Tooltip text for the Up button. Default: <c>"Up — parent folder"</c>.</summary>
    public string UpButtonTooltip
    {
        get => (string)GetValue(UpButtonTooltipProperty);
        set => SetValue(UpButtonTooltipProperty, value);
    }

    // ── HomeButtonTooltip ─────────────────────────────────────────────────────
    public static readonly DependencyProperty HomeButtonTooltipProperty =
        DependencyProperty.Register(nameof(HomeButtonTooltip), typeof(string),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata("Home", OnNavTooltipChanged));

    /// <summary>Tooltip text for the Home button. Default: <c>"Home"</c>.</summary>
    public string HomeButtonTooltip
    {
        get => (string)GetValue(HomeButtonTooltipProperty);
        set => SetValue(HomeButtonTooltipProperty, value);
    }

    private static void OnNavTooltipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BetterBreadcrumbBar bar) bar.ApplyNavTooltips();
    }

    // ── CurrentPath ───────────────────────────────────────────────────────────
    // WinUI 3 has no RegisterReadOnly / DependencyPropertyKey (WPF-only).
    // The canonical WinUI 3 pattern for a "read-only from outside" DP is a
    // normal Register with a private CLR setter.
    public static readonly DependencyProperty CurrentPathProperty =
        DependencyProperty.Register(nameof(CurrentPath), typeof(string),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata(string.Empty, OnCurrentPathChanged));

    /// <summary>
    /// The full path string of the currently displayed node.
    /// Updated automatically by <see cref="SetPath(IEnumerable{PathNode})"/>.
    /// Also used as the bar's tooltip.
    /// </summary>
    public string CurrentPath
    {
        get => (string)GetValue(CurrentPathProperty);
        private set => SetValue(CurrentPathProperty, value);
    }

    private static void OnCurrentPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BetterBreadcrumbBar bar)
            ToolTipService.SetToolTip(bar.RootBorder, (string)e.NewValue);
    }

    // ── CanGoBack ──────────────────────────────────────────────────────────────
    public static readonly DependencyProperty CanGoBackProperty =
        DependencyProperty.Register(nameof(CanGoBack), typeof(bool),
            typeof(BetterBreadcrumbBar),
            new PropertyMetadata(false, OnCanGoBackChanged));

    /// <summary>
    /// Whether the Back button is enabled.
    /// The control does not manage history; set this from the host.
    /// </summary>
    public bool CanGoBack
    {
        get => (bool)GetValue(CanGoBackProperty);
        set => SetValue(CanGoBackProperty, value);
    }

    private static void OnCanGoBackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BetterBreadcrumbBar bar)
            bar.BackButton.IsEnabled = (bool)e.NewValue;
    }

    /// <summary>
    /// Whether the Up button should be enabled (path has at least two segments).
    /// Updated automatically by <see cref="SetPath(IEnumerable{PathNode})"/>.
    /// </summary>
    public bool CanGoUp => _segments.Count > 1;

    // ── Typography API ────────────────────────────────────────────────────────
    // In WinUI 3, Button's default ControlTemplate sets FontFamily (and other font
    // properties) via local ThemeResource values. This beats both property inheritance
    // and TemplateBinding — neither approach can override a local value.
    //
    // "public new" CLR shadowing doesn't help either: the XAML parser calls
    // SetValue(Control.FontFamilyProperty, ...) directly, completely bypassing any
    // CLR setter, so the callback inside it never fires.
    //
    // The correct WinUI 3 solution is RegisterPropertyChangedCallback, which hooks
    // into the DP system itself and fires whenever the DP value changes — regardless
    // of whether the change came from XAML, a Style, code, animation, or binding.
    // We register callbacks for each typography DP in the constructor (after
    // InitializeComponent) and call ApplyTypography() which explicitly pushes the
    // values to SegmentsControl and InlineAddressBox, bypassing Button's theme defaults.

    // -- FlowDirection ----------------------------------------------------------
    // We shadow the inherited FlowDirection so we can react to changes and update
    // elements that do not auto-mirror (chevron glyphs, flyout placement).

    /// <summary>
    /// Gets or sets the flow direction of the breadcrumb bar.
    /// Set to <see cref="FlowDirection.RightToLeft"/> for Arabic, Hebrew, and other RTL locales.
    /// The bar layout, text, and chevron glyphs all adapt automatically.
    /// Default: <see cref="FlowDirection.LeftToRight"/>.
    /// </summary>
    public new FlowDirection FlowDirection
    {
        get => base.FlowDirection;
        set
        {
            base.FlowDirection = value;
            ApplyFlowDirection(value);
        }
    }

    #endregion

    #region Events

    /// <summary>Raised when the user clicks a path segment.</summary>
    public event EventHandler<PathNodeEventArgs>? SegmentClicked;

    /// <summary>Raised when the user selects a node from the chevron dropdown menu.</summary>
    public event EventHandler<PathNodeEventArgs>? NodeSelected;

    /// <summary>
    /// Raised when the Back button is clicked.
    /// The host must manage the history stack and call <see cref="SetPath(IEnumerable{PathNode})"/>
    /// with the previous path, then update <see cref="CanGoBack"/> accordingly.
    /// </summary>
    public event EventHandler? BackRequested;

    /// <summary>
    /// Raised when the Up button is clicked.
    /// <see cref="PathNodeEventArgs.Node"/> contains the parent node, ready for <see cref="SetPath(IEnumerable{PathNode})"/>.
    /// </summary>
    public event EventHandler<PathNodeEventArgs>? UpRequested;

    /// <summary>Raised when the Home button is clicked.</summary>
    public event EventHandler? HomeRequested;

    /// <summary>
    /// Raised when the user confirms a path in the inline address bar (Enter or suggestion chosen).
    /// <see cref="PathSubmittedEventArgs.Path"/> contains the typed or chosen text.
    /// The host should validate the path and call <see cref="SetPath(IEnumerable{PathNode})"/> if valid.
    /// </summary>
    public event EventHandler<PathSubmittedEventArgs>? PathSubmitted;

    #endregion

    private readonly ObservableCollection<PathSegment> _segments = new();
    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _verifyCts;
    private CancellationTokenSource? _suggestCts;

    // Number of leading segments currently hidden in the overflow flyout.
    // 0 = all segments visible, no overflow button shown.
    private int _hiddenSegmentCount = 0;

    private static readonly BoolToVisibilityConverter        _boolToVis    = new();
    private static readonly BoolToInverseVisibilityConverter _boolToInvVis = new();

    /// <summary>Initialises the control.</summary>
    public BetterBreadcrumbBar()
    {
        this.InitializeComponent();
        Resources["BoolToVisibilityConverter"]        = _boolToVis;
        Resources["BoolToInverseVisibilityConverter"] = _boolToInvVis;
        SegmentsControl.ItemsSource = _segments;
        BackButton.IsEnabled = false;

        // Default font size matching the Windows Explorer address bar.
        // Set before RegisterPropertyChangedCallback so the initial ApplyTypography()
        // call in Loaded sees the correct value.
        FontSize = 13;

        // RegisterPropertyChangedCallback fires whenever the DP value changes via ANY
        // mechanism (XAML, Style, binding, code) — unlike a CLR setter which the XAML
        // parser bypasses. This is the only reliable way to intercept DP changes in WinUI 3
        // when OverrideMetadata (WPF-only) is not available.
        RegisterPropertyChangedCallback(FontFamilyProperty,       (_, _) => ApplyTypography());
        RegisterPropertyChangedCallback(FontSizeProperty,         (_, _) => ApplyTypography());
        RegisterPropertyChangedCallback(FontWeightProperty,       (_, _) => ApplyTypography());
        RegisterPropertyChangedCallback(FontStyleProperty,        (_, _) => ApplyTypography());
        RegisterPropertyChangedCallback(FontStretchProperty,      (_, _) => ApplyTypography());
        RegisterPropertyChangedCallback(ForegroundProperty,       (_, _) => ApplyTypography());
        RegisterPropertyChangedCallback(CharacterSpacingProperty, (_, _) => ApplyTypography());

        Loaded += (_, _) =>
        {
            ApplyNavTooltips();
            UpdateNavButtonsArea();
            UpdateLeadingIconArea();
            ApplyFlowDirection(FlowDirection); // apply initial direction (may be set before Loaded)
            ApplyTypography();                 // push font properties to segments and address box
        };
    }

    // ── Path API ──────────────────────────────────────────────────────────────

    /// <summary>Sets the displayed path from an ordered list of nodes (root → current).</summary>
    public void SetPath(IEnumerable<PathNode> pathNodes)
    {
        _verifyCts?.Cancel();
        _hiddenSegmentCount = 0;
        _segments.Clear();

        var nodes = pathNodes.ToList();
        string chevronGlyph = FlowDirection == FlowDirection.RightToLeft
            ? PathSegment.ChevronRtl : PathSegment.ChevronLtr;
        var ff  = base.FontFamily;
        var fs  = base.FontSize;
        var fw  = base.FontWeight;
        var fst = base.FontStyle;
        var fsc = base.FontStretch;
        var fg  = base.Foreground;
        var cs2 = base.CharacterSpacing;
        for (int i = 0; i < nodes.Count; i++)
        {
            var seg = new PathSegment(nodes[i], isLast: i == nodes.Count - 1)
            {
                ChevronGlyph     = chevronGlyph,
                FontFamily       = ff,
                FontSize         = fs,
                FontWeight       = fw,
                FontStyle        = fst,
                FontStretch      = fsc,
                Foreground       = fg,
                CharacterSpacing = cs2,
            };
            _segments.Add(seg);
        }

        CurrentPath        = nodes.Count > 0 ? nodes[^1].FullPath : string.Empty;
        UpButton.IsEnabled = CanGoUp;

        if (ShowLastSegmentChevron && _segments.Count > 0)
            VerifyLastSegmentChevronAsync(_segments[^1]);

        // Capture natural segment widths once layout has settled (all segments visible).
        _segmentNaturalWidths = Array.Empty<double>(); // invalidate stale cache
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            CaptureSegmentWidths);
    }

    /// <summary>Sets the displayed path from a Windows filesystem path string.</summary>
    public void SetPath(string fullPath) => SetPath(BuildNodesFromPath(fullPath));

    /// <summary>Returns the parent node of the current node, or <c>null</c> if at the root.</summary>
    public PathNode? GetParentNode()
        => _segments.Count > 1 ? _segments[^2].Node : null;

    /// <summary>Returns the current (last) node, or <c>null</c> if the path is empty.</summary>
    public PathNode? GetCurrentNode()
        => _segments.Count > 0 ? _segments[^1].Node : null;

    // ── Overflow management ───────────────────────────────────────────────────
    //
    // Strategy: _segments always contains ALL nodes. _hiddenSegmentCount tracks
    // how many leading ones are collapsed. Each PathSegment exposes IsHiddenByOverflow;
    // the DataTemplate binds the root StackPanel Visibility to SegmentVisibility.
    //
    // Root causes of the previous glitch loop:
    //   A) SegmentsControl_SizeChanged fired every time a segment was hidden/shown,
    //      posting another UpdateOverflow → infinite feedback loop.
    //   B) Reading container.ActualWidth for a HIDDEN container returns 0, so every
    //      hidden segment appeared to "fit", they were all revealed, they overflowed,
    //      got hidden again → oscillation at display frequency.
    //
    // Fix:
    //   1. Only UserControl_SizeChanged drives recalculation (no SegmentsControl handler).
    //   2. Natural widths are captured once into _segmentNaturalWidths when all segments
    //      are visible (immediately after SetPath), and reused on every resize — so hidden
    //      containers never corrupt the measurement.
    //   3. The DispatcherQueue post is used only to defer past the current layout pass;
    //      _overflowPending coalesces multiple posts per frame.

    // Width reserved for the overflow button (28) + trailing chevron (~14) + spacing.
    private const double OverflowButtonWidth = 46;

    private bool   _overflowPending      = false;
    private double _chromeWidthCache     = -1;   // invalidated by nav-button / icon changes
    private double[] _segmentNaturalWidths = Array.Empty<double>();

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        => ScheduleOverflowUpdate();

    // Called once after SetPath so natural widths are captured while all segments visible.
    private void CaptureSegmentWidths()
    {
        _segmentNaturalWidths = new double[_segments.Count];
        for (int i = 0; i < _segments.Count; i++)
        {
            if (SegmentsControl.ContainerFromIndex(i) is FrameworkElement fe && fe.ActualWidth > 0)
                _segmentNaturalWidths[i] = fe.ActualWidth;
            else
                _segmentNaturalWidths[i] = _segments[i].Label.Length * 8.5 + 22; // fallback
        }
        _overflowPending  = false;   // reset so the next resize triggers fresh
        _chromeWidthCache = -1;      // chrome may have changed with the new path
        UpdateOverflow();
    }

    private void ScheduleOverflowUpdate()
    {
        if (_overflowPending) return;
        _overflowPending = true;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            _overflowPending = false;
            UpdateOverflow();
        });
    }

    /// <summary>
    /// Computes the correct <see cref="_hiddenSegmentCount"/> from stable natural widths
    /// and the current UserControl width. Never reads hidden container widths.
    /// </summary>
    private void UpdateOverflow()
    {
        if (_segments.Count == 0) { ApplyOverflow(0); return; }

        double totalWidth = ActualWidth;
        if (totalWidth <= 0) return;

        // Cache chrome width — it only changes when nav buttons or icon are toggled,
        // not on every resize. Recompute when _chromeWidthCache == -1.
        if (_chromeWidthCache < 0)
        {
            _chromeWidthCache = 0;
            foreach (var child in NormalView.Children)
            {
                if (child == SegmentsControl) continue;
                if (child is FrameworkElement fe && fe.Visibility != Visibility.Collapsed)
                    _chromeWidthCache += fe.ActualWidth;
            }
        }

        // Ensure natural widths array is sized correctly (guard against stale state).
        if (_segmentNaturalWidths.Length != _segments.Count)
        {
            // Widths not yet captured or stale — schedule a re-capture after layout.
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                CaptureSegmentWidths);
            return;
        }

        const double borderPadding      = 8;
        double spaceWithoutOverflow = totalWidth - borderPadding - _chromeWidthCache;
        double spaceWithOverflow    = spaceWithoutOverflow - OverflowButtonWidth;

        double totalNatural = 0;
        foreach (var w in _segmentNaturalWidths) totalNatural += w;

        // All segments fit without overflow button.
        if (totalNatural <= spaceWithoutOverflow) { ApplyOverflow(0); return; }

        // Extreme narrow: keep only the last segment.
        if (spaceWithOverflow <= 0) { ApplyOverflow(_segments.Count - 1); return; }

        // Walk backwards accumulating widths to find the first visible segment.
        double acc       = 0;
        int firstVisible = _segments.Count; // assume all hidden, decrement below

        for (int i = _segments.Count - 1; i >= 0; i--)
        {
            double w = _segmentNaturalWidths[i];
            if (acc + w <= spaceWithOverflow) { acc += w; firstVisible = i; }
            else break;
        }

        ApplyOverflow(firstVisible);
    }

    /// <summary>
    /// Applies the computed overflow state. Idempotent — skips work if nothing changed.
    /// </summary>
    private void ApplyOverflow(int hiddenCount)
    {
        if (_hiddenSegmentCount == hiddenCount &&
            OverflowButton.Visibility == (hiddenCount > 0 ? Visibility.Visible : Visibility.Collapsed))
            return; // nothing changed — avoid triggering further layout events

        _hiddenSegmentCount = hiddenCount;

        for (int i = 0; i < _segments.Count; i++)
            _segments[i].IsHiddenByOverflow = (i < hiddenCount);

        bool hasOverflow = hiddenCount > 0;
        OverflowButton.Visibility  = hasOverflow ? Visibility.Visible : Visibility.Collapsed;
        OverflowChevron.Visibility = hasOverflow ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Opens a flyout listing all segments currently hidden by overflow.
    /// Clicking an item raises <see cref="SegmentClicked"/>.
    /// </summary>
    private void OverflowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_hiddenSegmentCount == 0) return;

        var placement = FlowDirection == FlowDirection.RightToLeft
            ? FlyoutPlacementMode.BottomEdgeAlignedRight
            : FlyoutPlacementMode.BottomEdgeAlignedLeft;
        var flyout = CreateBoundedFlyout(placement);

        for (int i = 0; i < _hiddenSegmentCount; i++)
        {
            var seg  = _segments[i];
            var item = new MenuFlyoutItem { Text = seg.Label, Tag = seg };
            item.Click += (s, _) =>
            {
                if (s is MenuFlyoutItem mi && mi.Tag is PathSegment clicked)
                    SegmentClicked?.Invoke(this, new PathNodeEventArgs(clicked.Node));
            };
            flyout.Items.Add(item);
        }

        flyout.ShowAt(OverflowButton);
    }

    // ── Inline address box ────────────────────────────────────────────────────
    //
    // Design: enter on PointerPressed on the bar's empty area; exit on Enter,
    // Escape, or click anywhere outside the RootBorder (captured via
    // Window.Content.PointerPressed with handledEventsToo=false at the root).
    // We deliberately avoid LostFocus / LosingFocus: both are unreliable on
    // AutoSuggestBox in WinUI 3 because the suggestion-list popup triggers them
    // even for intra-control focus changes.

    private bool _inEditMode;
    private bool _suppressExitOnce;   // set before QuerySubmitted triggers navigation
    private long _exitEditModeTickCount;

    private void EnterEditMode()
    {
        if (_inEditMode) return;
        _inEditMode = true;

        InlineAddressBox.Text       = CurrentPath;
        NormalView.Visibility       = Visibility.Collapsed;
        InlineAddressBox.Visibility = Visibility.Visible;
        InlineAddressBox.Focus(FocusState.Programmatic);

        // Place the caret at the end without selecting anything,
        // so the user can immediately refine the existing path.
        var tb = FindDescendant<TextBox>(InlineAddressBox);
        if (tb is not null)
            tb.Select(tb.Text.Length, 0);

        // Subscribe to pointer-pressed on the whole window content so we can
        // detect clicks outside the bar and dismiss the address box.
        var root = GetWindowContent();
        if (root != null)
            root.AddHandler(
                PointerPressedEvent,
                new PointerEventHandler(WindowContent_PointerPressed),
                handledEventsToo: true);
    }

    private void ExitEditMode()
    {
        if (!_inEditMode) return;
        _inEditMode = false;

        _suggestCts?.Cancel();
        _exitEditModeTickCount      = Environment.TickCount64;
        InlineAddressBox.Visibility = Visibility.Collapsed;
        NormalView.Visibility       = Visibility.Visible;

        var root = GetWindowContent();
        if (root != null)
            root.RemoveHandler(
                PointerPressedEvent,
                new PointerEventHandler(WindowContent_PointerPressed));
    }

    /// <summary>
    /// Returns the root <see cref="FrameworkElement"/> of the window that hosts this control,
    /// used to attach a window-wide pointer handler that detects clicks outside the bar.
    /// </summary>
    private FrameworkElement? GetWindowContent()
    {
        DependencyObject? current = this;
        while (current != null)
        {
            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            if (parent == null && current is FrameworkElement fe)
                return fe;
            current = parent;
        }
        return null;
    }

    /// <summary>
    /// Window-level pointer handler: dismiss the address box when the user
    /// clicks anywhere outside the <see cref="RootBorder"/>.
    /// </summary>
    private void WindowContent_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_inEditMode) return;

        var point = e.GetCurrentPoint(RootBorder);
        bool insideBar = point.Position.X >= 0
                      && point.Position.Y >= 0
                      && point.Position.X <= RootBorder.ActualWidth
                      && point.Position.Y <= RootBorder.ActualHeight;

        if (!insideBar)
            ExitEditMode();
    }

    private void RootBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_inEditMode)
        {
            e.Handled = true;
            return;
        }

        if (Environment.TickCount64 - _exitEditModeTickCount < 200)
            return;

        EnterEditMode();
        e.Handled = true;
    }

    private async void InlineAddressBox_TextChanged(
        AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput || PathProvider == null)
            return;

        _suggestCts?.Cancel();
        _suggestCts = new CancellationTokenSource();
        var token   = _suggestCts.Token;

        try
        {
            var suggestions = await PathProvider.GetSuggestionsAsync(sender.Text, token);
            if (!token.IsCancellationRequested)
                sender.ItemsSource = suggestions.ToList();
        }
        catch (OperationCanceledException) { }
    }

    private void InlineAddressBox_SuggestionChosen(
        AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string chosen)
            sender.Text = chosen;
    }

    private void InlineAddressBox_QuerySubmitted(
        AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        string path = args.ChosenSuggestion as string ?? args.QueryText;
        ExitEditMode();
        if (!string.IsNullOrWhiteSpace(path))
            PathSubmitted?.Invoke(this, new PathSubmittedEventArgs(path));
    }

    private void InlineAddressBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            ExitEditMode();
        }
    }

    // ── Last-chevron verification ─────────────────────────────────────────────

    private async void VerifyLastSegmentChevronAsync(PathSegment segment)
    {
        if (PathProvider == null || !segment.IsLast) return;

        _verifyCts?.Cancel();
        _verifyCts = new CancellationTokenSource();
        var token  = _verifyCts.Token;

        segment.IsSeparatorLoading = true;
        segment.ShowLastChevron    = false;

        try
        {
            IEnumerable<PathNode> children;
            if (segment.Node.Children.Count > 0)
            {
                children = segment.Node.Children;
            }
            else
            {
                children = await PathProvider.GetChildrenAsync(segment.Node, token);
                if (token.IsCancellationRequested) return;
                foreach (var child in children)
                    segment.Node.Children.Add(child);
            }
            segment.ShowLastChevron     = segment.Node.Children.Count > 0;
            segment.LastChevronVerified = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BreadcrumbBar verify: {ex.Message}");
            segment.ShowLastChevron = false;
        }
        finally
        {
            if (!token.IsCancellationRequested)
                segment.IsSeparatorLoading = false;
        }
    }

    // ── Visual area updaters ──────────────────────────────────────────────────

    private void UpdateNavButtonsArea()
    {
        bool any = ShowBackButton || ShowUpButton || ShowHomeButton;
        BackButton.Visibility      = ShowBackButton ? Visibility.Visible : Visibility.Collapsed;
        UpButton.Visibility        = ShowUpButton   ? Visibility.Visible : Visibility.Collapsed;
        HomeButton.Visibility      = ShowHomeButton ? Visibility.Visible : Visibility.Collapsed;
        NavButtonsPanel.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        NavSeparator.Visibility    = any ? Visibility.Visible : Visibility.Collapsed;
        _chromeWidthCache = -1; // chrome changed — recompute on next overflow pass
        ScheduleOverflowUpdate();
    }

    private void UpdateLeadingIconArea()
    {
        var icon = LeadingIcon;
        LeadingIconPresenter.Child      = icon;
        LeadingIconPresenter.Visibility = icon is not null ? Visibility.Visible : Visibility.Collapsed;
        IconSeparator.Visibility        = icon is not null ? Visibility.Visible : Visibility.Collapsed;
        _chromeWidthCache = -1; // chrome changed — recompute on next overflow pass
        ScheduleOverflowUpdate();
    }

    private void ApplyNavTooltips()
    {
        ToolTipService.SetToolTip(BackButton, BackButtonTooltip);
        ToolTipService.SetToolTip(UpButton,   UpButtonTooltip);
        ToolTipService.SetToolTip(HomeButton, HomeButtonTooltip);
    }

    /// <summary>
    /// Explicitly propagates all typography properties to <see cref="SegmentsControl"/>
    /// and <see cref="InlineAddressBox"/>, bypassing WinUI 3's inheritance chain which
    /// is blocked by Button's default theme ControlTemplate setting FontFamily locally.
    /// </summary>
    private void ApplyTypography()
    {
        var family  = base.FontFamily;
        var size    = base.FontSize;
        var weight  = base.FontWeight;
        var style   = base.FontStyle;
        var stretch = base.FontStretch;
        var fg      = base.Foreground;
        var spacing = base.CharacterSpacing;

        // Push values onto every PathSegment. The DataTemplate binds these properties
        // directly onto each Button with x:Bind (Mode=OneWay), so the Button receives
        // a LOCAL value that wins over the theme ControlTemplate's ThemeResource default.
        foreach (var seg in _segments)
        {
            seg.FontFamily        = family;
            seg.FontSize          = size;
            seg.FontWeight        = weight;
            seg.FontStyle         = style;
            seg.FontStretch       = stretch;
            seg.Foreground        = fg;
            seg.CharacterSpacing  = spacing;
        }

        // InlineAddressBox: explicit propagation (no DataTemplate involved).
        InlineAddressBox.FontFamily       = family;
        InlineAddressBox.FontSize         = size;
        InlineAddressBox.FontWeight       = weight;
        InlineAddressBox.FontStyle        = style;
        InlineAddressBox.FontStretch      = stretch;
        InlineAddressBox.Foreground       = fg;
        InlineAddressBox.CharacterSpacing = spacing;
    }

    /// <summary>
    /// Propagates a <see cref="FlowDirection"/> change to all elements that do not
    /// mirror automatically: segment chevron glyphs, the overflow chevron, and flyout placement.
    /// The <see cref="InlineAddressBox"/> is always kept LTR so file-path strings
    /// (e.g. C:\\Users\\...) always read left-to-right regardless of UI direction.
    /// </summary>
    private void ApplyFlowDirection(FlowDirection direction)
    {
        bool isRtl = direction == FlowDirection.RightToLeft;
        string chevron = isRtl ? PathSegment.ChevronRtl : PathSegment.ChevronLtr;

        // Update separator chevron glyph on every existing segment.
        foreach (var seg in _segments)
            seg.ChevronGlyph = chevron;

        // Update the overflow button trailing chevron.
        OverflowChevron.Glyph = chevron;

        // Keep address box LTR so file paths always display correctly.
        InlineAddressBox.FlowDirection = FlowDirection.LeftToRight;

        // Invalidate chrome cache and recompute overflow.
        _chromeWidthCache = -1;
        ScheduleOverflowUpdate();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Walks the visual tree downward and returns the first descendant of type <typeparamref name="T"/>,
    /// or <c>null</c> if none is found.
    /// </summary>
    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;
            var result = FindDescendant<T>(child);
            if (result is not null)
                return result;
        }
        return null;
    }

    private static List<PathNode> BuildNodesFromPath(string fullPath)
    {
        var parts = fullPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        var nodes = new List<PathNode>();
        string acc = string.Empty;
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (i == 0 && part.Length == 2 && part[1] == ':')
            {
                acc = part + "\\";
                nodes.Add(new PathNode { Name = part + "\\", FullPath = acc, IsRoot = true });
            }
            else
            {
                acc = acc.TrimEnd('/', '\\') + "\\" + part;
                nodes.Add(new PathNode { Name = part, FullPath = acc });
            }
        }
        return nodes;
    }

    // ── Segment event handlers ────────────────────────────────────────────────

    private void SegmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PathSegment seg)
            SegmentClicked?.Invoke(this, new PathNodeEventArgs(seg.Node));
    }

    private async void SeparatorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not PathSegment segment) return;
        if (PathProvider == null) return;

        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;

        segment.IsSeparatorLoading = true;
        try
        {
            var idx = _segments.IndexOf(segment);
            PathNode parentNode = (segment.IsLast && ShowLastSegmentChevron)
                ? segment.Node
                : (idx <= 0 ? segment.Node : _segments[idx - 1].Node);

            IEnumerable<PathNode> children;
            if (parentNode.Children.Count > 0)
            {
                children = parentNode.Children;
            }
            else
            {
                children = await PathProvider.GetChildrenAsync(parentNode, token);
                if (token.IsCancellationRequested) return;
                foreach (var child in children)
                    parentNode.Children.Add(child);
            }
            if (token.IsCancellationRequested) return;
            ShowChildrenFlyout(btn, children, segment);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BreadcrumbBar load: {ex.Message}");
        }
        finally
        {
            if (!token.IsCancellationRequested)
                segment.IsSeparatorLoading = false;
        }
    }

    private void ShowChildrenFlyout(Button anchor, IEnumerable<PathNode> children, PathSegment currentSeg)
    {
        var placement = FlowDirection == FlowDirection.RightToLeft
            ? FlyoutPlacementMode.BottomEdgeAlignedRight
            : FlyoutPlacementMode.BottomEdgeAlignedLeft;
        var flyout = CreateBoundedFlyout(placement);
        var list   = children.ToList();

        if (list.Count == 0)
        {
            flyout.Items.Add(new MenuFlyoutItem { Text = "(No subfolders)", IsEnabled = false });
        }
        else
        {
            foreach (var child in list)
            {
                var item = new MenuFlyoutItem { Text = child.Name, Tag = child };
                if (currentSeg.Node.Name == child.Name)
                    item.Icon = new FontIcon { Glyph = "\uE915" };
                item.Click += (s, _) =>
                {
                    if (s is MenuFlyoutItem mi && mi.Tag is PathNode node)
                        NodeSelected?.Invoke(this, new PathNodeEventArgs(node));
                };
                flyout.Items.Add(item);
            }
        }
        flyout.ShowAt(anchor);
    }

    /// <summary>
    /// Creates a <see cref="MenuFlyout"/> whose presenter is capped to 10 visible rows.
    /// A <see cref="ScrollViewer"/> is injected via <see cref="MenuFlyout.MenuFlyoutPresenterStyle"/>
    /// so longer lists scroll rather than growing off-screen.
    /// One row ≈ 36 px (WinUI default MenuFlyoutItem height); 10 rows = 360 px.
    /// </summary>
    private static MenuFlyout CreateBoundedFlyout(FlyoutPlacementMode placement)
    {
        const double MaxRows       = 10;
        const double RowHeight     = 36;   // default MenuFlyoutItem height in WinUI 3
        const double VerticalPadding = 8;  // top + bottom padding of the presenter
        double maxHeight = MaxRows * RowHeight + VerticalPadding;

        var style = new Style(typeof(MenuFlyoutPresenter));
        style.Setters.Add(new Setter(MaxHeightProperty, maxHeight));
        style.Setters.Add(new Setter(ScrollViewer.VerticalScrollBarVisibilityProperty,
                                     ScrollBarVisibility.Auto));
        style.Setters.Add(new Setter(ScrollViewer.VerticalScrollModeProperty,
                                     ScrollMode.Auto));

        return new MenuFlyout
        {
            Placement             = placement,
            MenuFlyoutPresenterStyle = style,
        };
    }

    // ── Nav button event handlers ─────────────────────────────────────────────

    private void BackButton_Click(object sender, RoutedEventArgs e)
        => BackRequested?.Invoke(this, EventArgs.Empty);

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        var parent = GetParentNode();
        if (parent is not null)
            UpRequested?.Invoke(this, new PathNodeEventArgs(parent));
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
        => HomeRequested?.Invoke(this, EventArgs.Empty);
}

/// <summary>Event arguments carrying a <see cref="PathNode"/>.</summary>
public class PathNodeEventArgs : EventArgs
{
    /// <summary>The path node associated with the event.</summary>
    public PathNode Node { get; }
    /// <param name="node">The associated path node.</param>
    public PathNodeEventArgs(PathNode node) => Node = node;
}

/// <summary>Event arguments for <see cref="BetterBreadcrumbBar.PathSubmitted"/>.</summary>
public class PathSubmittedEventArgs : EventArgs
{
    /// <summary>The path string typed or chosen by the user.</summary>
    public string Path { get; }
    /// <param name="path">The submitted path string.</param>
    public PathSubmittedEventArgs(string path) => Path = path;
}
