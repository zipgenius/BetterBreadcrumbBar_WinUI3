using BetterBreadcrumbBar.Control;
using BetterBreadcrumbBar.Control.Providers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BetterBreadcrumbBar.Demo;

/// <summary>Demo application main window.</summary>
public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    // ── Providers ─────────────────────────────────────────────────────────────
    public FileSystemPathProvider FsProvider  { get; } = new();
    public VirtualPathProvider    ZipProvider { get; private set; } = null!;

    // ── INotifyPropertyChanged ────────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(name);
    }

    // ── Filesystem feature flags ───────────────────────────────────────────────
    private bool _fsShowLastChevron = true;
    public bool FsShowLastChevron { get => _fsShowLastChevron; set => Set(ref _fsShowLastChevron, value); }

    private bool _fsShowBack = true;
    public bool FsShowBack { get => _fsShowBack; set => Set(ref _fsShowBack, value); }

    private bool _fsShowUp = true;
    public bool FsShowUp { get => _fsShowUp; set => Set(ref _fsShowUp, value); }

    private bool _fsShowHome = true;
    public bool FsShowHome { get => _fsShowHome; set => Set(ref _fsShowHome, value); }

    private bool _fsShowIcon = true;
    public bool FsShowIcon { get => _fsShowIcon; set => Set(ref _fsShowIcon, value); }

    private bool _fsRtl = false;
    public bool FsRtl { get => _fsRtl; set => Set(ref _fsRtl, value); }

    private bool _fsCanGoBack;
    public bool FsCanGoBack { get => _fsCanGoBack; private set => Set(ref _fsCanGoBack, value); }

    // ── ZIP feature flags ─────────────────────────────────────────────────────
    private bool _zipShowLastChevron = true;
    public bool ZipShowLastChevron { get => _zipShowLastChevron; set => Set(ref _zipShowLastChevron, value); }

    private bool _zipShowBack = true;
    public bool ZipShowBack { get => _zipShowBack; set => Set(ref _zipShowBack, value); }

    private bool _zipShowUp = true;
    public bool ZipShowUp { get => _zipShowUp; set => Set(ref _zipShowUp, value); }

    private bool _zipShowHome = true;
    public bool ZipShowHome { get => _zipShowHome; set => Set(ref _zipShowHome, value); }

    private bool _zipShowIcon = true;
    public bool ZipShowIcon { get => _zipShowIcon; set => Set(ref _zipShowIcon, value); }

    private bool _zipRtl = false;
    public bool ZipRtl { get => _zipRtl; set => Set(ref _zipRtl, value); }

    private bool _zipCanGoBack;
    public bool ZipCanGoBack { get => _zipCanGoBack; private set => Set(ref _zipCanGoBack, value); }

    // ── History stacks ────────────────────────────────────────────────────────
    private readonly Stack<List<PathNode>> _fsHistory  = new();
    private string _fsCurrentPath = @"C:\Users";

    private readonly Stack<List<PathNode>> _zipHistory = new();
    private string _zipCurrentPath = "";

    // ── ZIP structure ─────────────────────────────────────────────────────────
    private static readonly string[] ZipEntries =
    {
        "src/components/ui/Button.cs", "src/components/ui/TextBox.cs",
        "src/components/ui/ListView.cs", "src/components/layout/Grid.cs",
        "src/components/layout/StackPanel.cs", "src/services/AuthService.cs",
        "src/services/DataService.cs", "src/models/User.cs", "src/models/Product.cs",
        "docs/api/reference.md", "docs/api/changelog.md",
        "docs/guides/quickstart.md", "docs/guides/advanced.md",
        "tests/unit/AuthTests.cs", "tests/unit/DataTests.cs",
        "tests/integration/ApiTests.cs", "assets/images/logo.png",
        "assets/fonts/Inter.ttf", "README.md", "package.json",
    };

    private PathNode _zipRoot = null!;

    // ── Constructor ───────────────────────────────────────────────────────────
    public MainWindow()
    {
        var (prov, root) = VirtualPathProvider.FromPaths(ZipEntries, "archive.zip");
        ZipProvider = prov;
        _zipRoot    = root;

        this.InitializeComponent();
        SetWindowIcon();
        NavigateFilesystem(@"C:\Users", push: false);
        NavigateZip("", push: false);
        ZipStructureText.Text = BuildZipTreeText(_zipRoot, 0);
    }

    // ── Icon toggle handlers (needed because LeadingIcon = null can't bind to CheckBox directly) ─
    private void FsIconToggle_Checked(object sender, RoutedEventArgs e)
        => FsBreadcrumb.LeadingIcon = new SymbolIcon(Symbol.Folder);
    private void FsIconToggle_Unchecked(object sender, RoutedEventArgs e)
        => FsBreadcrumb.LeadingIcon = null;
    private void ZipIconToggle_Checked(object sender, RoutedEventArgs e)
        => ZipBreadcrumb.LeadingIcon = new FontIcon { Glyph = "\uE8B7", FontSize = 14 };
    private void ZipIconToggle_Unchecked(object sender, RoutedEventArgs e)
        => ZipBreadcrumb.LeadingIcon = null;

    private void FsRtlToggle_Checked(object sender, RoutedEventArgs e)
        => FsBreadcrumb.FlowDirection = Microsoft.UI.Xaml.FlowDirection.RightToLeft;
    private void FsRtlToggle_Unchecked(object sender, RoutedEventArgs e)
        => FsBreadcrumb.FlowDirection = Microsoft.UI.Xaml.FlowDirection.LeftToRight;
    private void ZipRtlToggle_Checked(object sender, RoutedEventArgs e)
        => ZipBreadcrumb.FlowDirection = Microsoft.UI.Xaml.FlowDirection.RightToLeft;
    private void ZipRtlToggle_Unchecked(object sender, RoutedEventArgs e)
        => ZipBreadcrumb.FlowDirection = Microsoft.UI.Xaml.FlowDirection.LeftToRight;

    // ── Filesystem navigation ─────────────────────────────────────────────────
    private void NavigateFilesystem(string path, bool push = true)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var nodes = FileSystemPathProvider.BuildPathNodes(path);
            if (push && _fsCurrentPath != path)
            {
                _fsHistory.Push(FileSystemPathProvider.BuildPathNodes(_fsCurrentPath));
                FsCanGoBack = true;
            }
            _fsCurrentPath   = path;
            FsPathInput.Text = path;
            FsBreadcrumb.SetPath(nodes);
            Log(FsEventLog, $"[Navigate] {path}");
        }
        catch (Exception ex) { Log(FsEventLog, $"[Error] {ex.Message}"); }
    }

    private void FsBreadcrumb_SegmentClicked(object s, PathNodeEventArgs e)
    {
        Log(FsEventLog, $"[Segment] {e.Node.FullPath}");
        NavigateFilesystem(e.Node.FullPath);
    }
    private void FsBreadcrumb_NodeSelected(object s, PathNodeEventArgs e)
    {
        Log(FsEventLog, $"[Menu] {e.Node.FullPath}");
        NavigateFilesystem(e.Node.FullPath);
    }
    private void FsBreadcrumb_PathSubmitted(object s, PathSubmittedEventArgs e)
    {
        Log(FsEventLog, $"[Address bar] {e.Path}");
        NavigateFilesystem(e.Path);
    }
    private void FsBreadcrumb_BackRequested(object s, EventArgs e)
    {
        if (_fsHistory.Count == 0) return;
        var prev = _fsHistory.Pop();
        FsCanGoBack      = _fsHistory.Count > 0;
        _fsCurrentPath   = prev[^1].FullPath;
        FsPathInput.Text = _fsCurrentPath;
        FsBreadcrumb.SetPath(prev);
        Log(FsEventLog, $"[Back] → {_fsCurrentPath}");
    }
    private void FsBreadcrumb_UpRequested(object s, PathNodeEventArgs e)
    {
        Log(FsEventLog, $"[Up] → {e.Node.FullPath}");
        NavigateFilesystem(e.Node.FullPath);
    }
    private void FsBreadcrumb_HomeRequested(object s, EventArgs e)
    {
        Log(FsEventLog, @"[Home] → C:\Users");
        NavigateFilesystem(@"C:\Users");
    }
    private void FsGoButton_Click(object s, RoutedEventArgs e) => NavigateFilesystem(FsPathInput.Text);
    private void FsPathInput_KeyDown(object s, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter) NavigateFilesystem(FsPathInput.Text);
    }
    private void FsQuickPath_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string p) NavigateFilesystem(p);
    }

    // ── ZIP navigation ────────────────────────────────────────────────────────
    private void NavigateZip(string vp, bool push = true)
    {
        if (push && _zipCurrentPath != vp)
        {
            _zipHistory.Push(ZipProvider.BuildPathNodes(_zipCurrentPath));
            ZipCanGoBack = true;
        }
        _zipCurrentPath = vp;
        ZipBreadcrumb.SetPath(ZipProvider.BuildPathNodes(vp));
        Log(ZipEventLog, $"[Navigate] {(vp == "" ? "(root)" : vp)}");
    }
    private void ZipBreadcrumb_SegmentClicked(object s, PathNodeEventArgs e)
    {
        Log(ZipEventLog, $"[Segment] {(e.Node.FullPath == "" ? "(root)" : e.Node.FullPath)}");
        NavigateZip(e.Node.FullPath);
    }
    private void ZipBreadcrumb_NodeSelected(object s, PathNodeEventArgs e)
    {
        Log(ZipEventLog, $"[Menu] {e.Node.FullPath}");
        NavigateZip(e.Node.FullPath);
    }
    private void ZipBreadcrumb_PathSubmitted(object s, PathSubmittedEventArgs e)
    {
        Log(ZipEventLog, $"[Address bar] {e.Path}");
        NavigateZip(e.Path.Trim('/'));
    }
    private void ZipBreadcrumb_BackRequested(object s, EventArgs e)
    {
        if (_zipHistory.Count == 0) return;
        var prev = _zipHistory.Pop();
        ZipCanGoBack    = _zipHistory.Count > 0;
        _zipCurrentPath = prev[^1].FullPath;
        ZipBreadcrumb.SetPath(prev);
        Log(ZipEventLog, $"[Back] → {(_zipCurrentPath == "" ? "(root)" : _zipCurrentPath)}");
    }
    private void ZipBreadcrumb_UpRequested(object s, PathNodeEventArgs e)
    {
        Log(ZipEventLog, $"[Up] → {(e.Node.FullPath == "" ? "(root)" : e.Node.FullPath)}");
        NavigateZip(e.Node.FullPath);
    }
    private void ZipBreadcrumb_HomeRequested(object s, EventArgs e)
    {
        Log(ZipEventLog, "[Home] → (root)");
        NavigateZip("");
    }
    private void ZipQuickPath_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string p) NavigateZip(p);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void Log(TextBlock tb, string msg)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss.fff");
        tb.Text = $"[{ts}] {msg}\n" + tb.Text;
        var lines = tb.Text.Split('\n');
        if (lines.Length > 20) tb.Text = string.Join('\n', lines.Take(20));
    }

    private static string BuildZipTreeText(PathNode node, int depth)
    {
        var sb     = new System.Text.StringBuilder();
        var indent = new string(' ', depth * 2);
        sb.AppendLine($"{indent}[+] {(depth == 0 ? node.Name : node.Name + "/")}");
        foreach (var child in node.Children) sb.Append(BuildZipTreeText(child, depth + 1));
        return sb.ToString();
    }

    private void SetWindowIcon()
    {
        var hwnd   = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var winId  = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWin = AppWindow.GetFromWindowId(winId);
        var ico    = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "main.ico");
        if (System.IO.File.Exists(ico)) appWin.SetIcon(ico);
    }
}
