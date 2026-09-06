using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Newton.Core.Domain;
using Newton.Core.Privacy;
using NorthstarBrowser.Services;
using NorthstarBrowser.Windows.WebView2;
using WebViewControl = Microsoft.Web.WebView2.Wpf.WebView2;

namespace NorthstarBrowser;

public partial class MainWindow : Window
{
    private static readonly ProfileId DefaultProfileId = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
    private readonly ProfileModel _profile = new(DefaultProfileId, "Default");
    private readonly WebView2RendererRegistry _renderers = new();
    private readonly WebView2ContentFilterAdapter _privacy = new(new HostContentFilter());
    private readonly TabLifecycleManager _lifecycle = new();
    private readonly NewtonDataStore _dataStore = new();
    private readonly DispatcherTimer _recoveryTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private Task<CoreWebView2Environment>? _environmentTask;
    private TabModel? _previousTab;
    private TabModel? _secondaryTab;
    private static readonly string[] TabGroups = ["General", "Research", "Work", "Later"];
    private int _layoutMode;
    private bool _darkTheme;
    private WorkspaceModel? CurrentSession => SessionList.SelectedItem as WorkspaceModel;
    private TabModel? CurrentTab => TabStrip.SelectedItem as TabModel;
    private WebViewControl? CurrentView => CurrentTab is { } tab && _renderers.TryGet(tab, out var view) ? view : null;

    public MainWindow()
    {
        InitializeComponent();
        SessionList.ItemsSource = _profile.Workspaces;
        SearchProviderBox.ItemsSource = NavigationService.SearchProviders;
        SearchProviderBox.SelectedItem = "DuckDuckGo";
        _dataStore.Initialise();
        Loaded += async (_, _) => await RestoreOrStartAsync();
        Closing += OnClosing;
        _recoveryTimer.Tick += (_, _) => SaveRecoverySnapshot(false);
        _recoveryTimer.Start();
        PreviewKeyDown += HandleShortcuts;
    }

    private async Task RestoreOrStartAsync()
    {
        IReadOnlyList<RecoveryTab> recovery = _dataStore.WasPreviousShutdownClean ? [] : _dataStore.LoadRecoveryTabs();
        if (recovery.Count == 0) { await CreateSessionAsync("Start"); return; }
        foreach (var savedWorkspace in recovery.GroupBy(x => new { x.WorkspacePosition, x.WorkspaceName }).OrderBy(x => x.Key.WorkspacePosition))
        {
            var workspace = AddWorkspace(savedWorkspace.Key.WorkspaceName);
            foreach (var saved in savedWorkspace.OrderBy(x => x.TabPosition))
            {
                if (!Uri.TryCreate(saved.Url, UriKind.Absolute, out var uri)) continue;
                var tab = await CreateTabAsync(workspace, uri);
                tab.Title = saved.Title;
                tab.GroupId = new(saved.Group);
            }
        }
    }

    private WorkspaceModel AddWorkspace(string name)
    {
        var workspace = new WorkspaceModel(WorkspaceId.New(), _profile.Id, name);
        _profile.AddWorkspace(workspace);
        SessionList.Items.Refresh();
        SessionList.SelectedItem = workspace;
        return workspace;
    }

    private void SaveRecoverySnapshot(bool cleanShutdown)
    {
        var snapshot = _profile.Workspaces.SelectMany((workspace, wi) => workspace.Tabs.Select((tab, ti) =>
            new RecoveryTab(wi, workspace.Name, ti,
                _renderers.TryGet(tab, out var view) ? view.CoreWebView2?.Source ?? view.Source?.AbsoluteUri ?? tab.Url.AbsoluteUri : tab.Url.AbsoluteUri,
                tab.Title, tab.GroupId.Value)));
        _dataStore.SaveRecoverySnapshot(snapshot, cleanShutdown);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _recoveryTimer.Stop();
        SaveRecoverySnapshot(true);
        _renderers.Dispose();
        _dataStore.Dispose();
    }

    private async Task CreateSessionAsync(string name) => await CreateTabAsync(AddWorkspace(name), new Uri("https://duckduckgo.com"));

    private async Task<TabModel> CreateTabAsync(WorkspaceModel workspace, Uri uri)
    {
        var tab = new TabModel(TabId.New(), workspace.Id, uri);
        workspace.AddTab(tab);
        var view = _renderers.Create(tab);
        RefreshTabs(workspace);
        TabStrip.SelectedItem = tab;
        ShowPrimary(view);
        await view.EnsureCoreWebView2Async(await GetEnvironmentAsync());
        Harden(view.CoreWebView2);
        await _privacy.ApplyAsync(view.CoreWebView2);
        WireEvents(tab, view);
        view.Source = uri;
        return tab;
    }

    private static void Harden(CoreWebView2 core)
    {
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDevToolsEnabled = true;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.IsWebMessageEnabled = false;
        core.Settings.IsReputationCheckingRequired = true;
        core.PermissionRequested += HandlePermissionRequest;
        core.ServerCertificateErrorDetected += (_, e) => e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
    }

    private static void HandlePermissionRequest(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        var origin = Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : "This page";
        var result = MessageBox.Show($"{origin} is requesting access to {e.PermissionKind}.\n\nAllow this request once?", "Newton site permission", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        e.State = result == MessageBoxResult.Yes ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
        e.SavesInProfile = false;
    }

    private Task<CoreWebView2Environment> GetEnvironmentAsync() => _environmentTask ??= CoreWebView2Environment.CreateAsync(null,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Newton", "Profiles", _profile.Id.ToString(), "WebView2"),
        new CoreWebView2EnvironmentOptions { AreBrowserExtensionsEnabled = false });

    private void WireEvents(TabModel tab, WebViewControl view)
    {
        var core = view.CoreWebView2;
        core.DocumentTitleChanged += (_, _) => Dispatcher.Invoke(() => { tab.Title = string.IsNullOrWhiteSpace(core.DocumentTitle) ? "New page" : core.DocumentTitle; TabStrip.Items.Refresh(); Title = $"{tab.Title} — Newton Alpha"; });
        core.SourceChanged += (_, _) => Dispatcher.Invoke(() => { if (Uri.TryCreate(core.Source, UriKind.Absolute, out var source)) tab.Url = source; UpdateChrome(); });
        core.HistoryChanged += (_, _) => Dispatcher.Invoke(UpdateChrome);
        core.ProcessFailed += (_, _) => Dispatcher.Invoke(() => tab.State = TabState.Crashed);
        core.NewWindowRequested += (_, e) => Dispatcher.InvokeAsync(async () => { e.Handled = true; if (CurrentSession is not null && Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri)) await CreateTabAsync(CurrentSession, uri); });
    }

    private void UpdateChrome()
    {
        if (CurrentView?.CoreWebView2 is not { } core) return;
        AddressBox.Text = core.Source;
        BackButton.IsEnabled = core.CanGoBack;
        ForwardButton.IsEnabled = core.CanGoForward;
        var secure = core.Source.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        SecurityGlyph.Text = secure ? "●" : "!";
        SecurityGlyph.Foreground = secure ? System.Windows.Media.Brushes.SeaGreen : System.Windows.Media.Brushes.DarkOrange;
    }

    private void RefreshTabs(WorkspaceModel workspace) { TabStrip.ItemsSource = workspace.Tabs; TabStrip.Items.Refresh(); WorkspaceTitle.Text = workspace.Name.ToUpperInvariant(); }
    private void ShowPrimary(WebViewControl view) { PrimaryHost.Children.Clear(); PrimaryHost.Children.Add(view); }
    private async void NewSession_Click(object sender, RoutedEventArgs e) => await CreateSessionAsync($"Session {_profile.Workspaces.Count + 1}");
    private async void NewTab_Click(object sender, RoutedEventArgs e) { if (CurrentSession is { } workspace) await CreateTabAsync(workspace, new Uri("https://duckduckgo.com")); }
    private void Back_Click(object sender, RoutedEventArgs e) { if (CurrentView?.CanGoBack == true) CurrentView.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (CurrentView?.CanGoForward == true) CurrentView.GoForward(); }
    private void Reload_Click(object sender, RoutedEventArgs e) => CurrentView?.Reload();
    private void Go_Click(object sender, RoutedEventArgs e) => Navigate();
    private void AddressBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Navigate(); }
    private void Search_Click(object sender, RoutedEventArgs e) => Search();
    private void SearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Search(); }

    private void Search()
    {
        var query = SearchBox.Text.Trim();
        if (query.Length == 0 || CurrentView is not { } view) return;
        view.Source = NavigationService.CreateSearch(SearchProviderBox.SelectedItem as string ?? "DuckDuckGo", query);
    }

    private void Navigate()
    {
        if (CurrentView is not { } view) return;
        if (NavigationService.TryResolveAddress(AddressBox.Text, out var destination) && destination is not null) { view.Source = destination; return; }
        MessageBox.Show("Enter a valid website address here. Use the separate search box to search the web.", "Invalid web address", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SessionList_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (CurrentSession is { } workspace) { RefreshTabs(workspace); TabStrip.SelectedItem = workspace.Tabs.FirstOrDefault(); } }

    private async void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentTab is not { } tab || !_renderers.TryGet(tab, out var view)) return;
        var suspend = _lifecycle.Activate(tab, _previousTab, _secondaryTab);
        if (suspend is not null && _renderers.TryGet(suspend, out var oldView) && oldView.CoreWebView2 is { } oldCore) await oldCore.TrySuspendAsync();
        if (view.CoreWebView2 is { IsSuspended: true } core) core.Resume();
        if (_secondaryTab == tab && SplitToggle.IsChecked == true) { UpdateChrome(); return; }
        _previousTab = tab;
        ShowPrimary(view);
        UpdateChrome();
    }

    private void GroupTab_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentTab is not { } tab) return;
        var index = Array.IndexOf(TabGroups, tab.GroupId.Value);
        tab.GroupId = new(TabGroups[(index + 1) % TabGroups.Length]);
        TabStrip.Items.Refresh();
    }

    private void SplitToggle_Click(object sender, RoutedEventArgs e)
    {
        if (SplitToggle.IsChecked == true && CurrentSession is { } workspace)
        {
            _secondaryTab = workspace.Tabs.FirstOrDefault(t => t != CurrentTab);
            if (_secondaryTab is null || !_renderers.TryGet(_secondaryTab, out var view)) { SplitToggle.IsChecked = false; MessageBox.Show("Open at least two pages before using split view.", "Split view"); return; }
            BrowserHost.ColumnDefinitions[0].Width = new(1, GridUnitType.Star); BrowserHost.ColumnDefinitions[1].Width = new(5); BrowserHost.ColumnDefinitions[2].Width = new(1, GridUnitType.Star);
            SplitDivider.Visibility = Visibility.Visible; SecondaryHost.Visibility = Visibility.Visible; SecondaryHost.Children.Clear();
            if (view.CoreWebView2 is { IsSuspended: true } core) core.Resume();
            SecondaryHost.Children.Add(view);
        }
        else
        {
            SecondaryHost.Children.Clear(); SecondaryHost.Visibility = Visibility.Collapsed; SplitDivider.Visibility = Visibility.Collapsed;
            BrowserHost.ColumnDefinitions[1].Width = new(0); BrowserHost.ColumnDefinitions[2].Width = new(0); _secondaryTab = null;
        }
    }

    private void Privacy_Click(object sender, RoutedEventArgs e) => MessageBox.Show("HIGH protection is active by default.\n\n• No Newton telemetry\n• Strict WebView2 tracking prevention\n• Known advertising and analytics hosts blocked\n• Microsoft reputation checking enabled\n• Chromium process sandbox inherited from WebView2\n• Browser extensions disabled\n• High-entropy fingerprint values reduced\n• Website permissions require one-time approval\n• Invalid certificates rejected\n• Password saving and autofill disabled\n• Evergreen engine security updates\n\nThis is mitigation, not anonymity. Some sites may break, and WebView2 cannot provide Tor-level fingerprint resistance.", "Privacy status", MessageBoxButton.OK, MessageBoxImage.Information);

    private void OpenPdf_Click(object sender, RoutedEventArgs e) { var dialog = new OpenFileDialog { Filter = "PDF documents (*.pdf)|*.pdf", CheckFileExists = true }; if (dialog.ShowDialog(this) == true && CurrentView is { } view) view.Source = new Uri(dialog.FileName); }
    private async void PictureInPicture_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentView?.CoreWebView2 is not { } core) return;
        var result = await core.ExecuteScriptAsync("""(() => { const video = [...document.querySelectorAll('video')].find(v => !v.paused) || document.querySelector('video'); if (!video || !document.pictureInPictureEnabled || video.disablePictureInPicture) return 'unavailable'; video.requestPictureInPicture(); return 'requested'; })();""");
        if (result.Contains("unavailable", StringComparison.OrdinalIgnoreCase)) MessageBox.Show("No compatible video was found on this page.", "Picture-in-Picture");
    }
    private async void Screenshot_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentView?.CoreWebView2 is not { } core) return;
        var dialog = new SaveFileDialog { Filter = "PNG image (*.png)|*.png", FileName = $"Newton-{DateTime.Now:yyyyMMdd-HHmmss}.png", AddExtension = true };
        if (dialog.ShowDialog(this) != true) return;
        await using var stream = File.Create(dialog.FileName); await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
    }
    private void DeveloperTools_Click(object sender, RoutedEventArgs e) => CurrentView?.CoreWebView2?.OpenDevToolsWindow();

    private void GeneratePassword_Click(object sender, RoutedEventArgs e)
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz", upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", digits = "23456789", symbols = "!@#$%&*+-=?";
        var all = lower + upper + digits + symbols; var password = new char[20];
        password[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)]; password[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)]; password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)]; password[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var i = 4; i < password.Length; i++) password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        RandomNumberGenerator.Shuffle(password.AsSpan()); Clipboard.SetText(new(password));
        MessageBox.Show("A 20-character password has been copied to the clipboard. Paste it now; Newton has not stored it.", "Strong password generated", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Customise_Click(object sender, RoutedEventArgs e)
    {
        _layoutMode = (_layoutMode + 1) % 3; TabColumn.Width = _layoutMode switch { 0 => new(230), 1 => new(150), _ => new(0) };
        GroupButton.Visibility = _layoutMode == 2 ? Visibility.Collapsed : Visibility.Visible;
        WorkspaceTitle.Text = $"{CurrentSession?.Name.ToUpperInvariant()} · {(_layoutMode == 0 ? "SPACIOUS" : _layoutMode == 1 ? "COMPACT" : "FOCUS")}";
    }
    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _darkTheme = !_darkTheme;
        var surface = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_darkTheme ? "#111827" : "#FFFFFF"));
        var raised = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_darkTheme ? "#1F2937" : "#F8FAFC"));
        ChromeBar.Background = surface; WorkspaceBar.Background = raised; VerticalTabsPanel.Background = raised; PrimaryHost.Background = surface; SecondaryHost.Background = surface;
        AddressBox.Foreground = _darkTheme ? System.Windows.Media.Brushes.WhiteSmoke : System.Windows.Media.Brushes.Black;
    }
    private async void HandleShortcuts(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F12) { DeveloperTools_Click(this, new()); e.Handled = true; return; }
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        if (e.Key == Key.L) { AddressBox.Focus(); AddressBox.SelectAll(); e.Handled = true; }
        if (e.Key is Key.E or Key.K) { SearchBox.Focus(); SearchBox.SelectAll(); e.Handled = true; }
        if (e.Key == Key.T && CurrentSession is not null) { await CreateTabAsync(CurrentSession, new("https://duckduckgo.com")); e.Handled = true; }
        if (e.Key == Key.G && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { GroupTab_Click(this, new()); e.Handled = true; }
        if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { SplitToggle.IsChecked = !(SplitToggle.IsChecked ?? false); SplitToggle_Click(this, new()); e.Handled = true; }
        if (e.Key == Key.D && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { Theme_Click(this, new()); e.Handled = true; }
        if (e.Key == Key.I && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { DeveloperTools_Click(this, new()); e.Handled = true; }
    }
}
