using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using NorthstarBrowser.Models;
using NorthstarBrowser.Services;

namespace NorthstarBrowser;

public partial class MainWindow : Window
{
    private readonly BrowserProfile _profile = new() { Id = "default", Name = "Default" };
    private ObservableCollection<BrowserSession> _sessions => _profile.Workspaces;
    private readonly TabLifecycleManager _tabLifecycle = new();
    private readonly NewtonDataStore _dataStore = new();
    private readonly DispatcherTimer _recoveryTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private Task<CoreWebView2Environment>? _environmentTask;
    private BrowserTab? _previousTab;
    private BrowserTab? _secondaryTab;
    private static readonly string[] TabGroups = ["General", "Research", "Work", "Later"];
    private int _layoutMode;
    private bool _darkTheme;
    private BrowserSession? CurrentSession => SessionList.SelectedItem as BrowserSession;
    private BrowserTab? CurrentTab => TabStrip.SelectedItem as BrowserTab;

    public MainWindow()
    {
        InitializeComponent();
        SessionList.ItemsSource = _sessions;
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
        if (recovery.Count == 0)
        {
            await CreateSessionAsync("Start");
            return;
        }

        foreach (var workspace in recovery.GroupBy(x => new { x.WorkspacePosition, x.WorkspaceName }).OrderBy(x => x.Key.WorkspacePosition))
        {
            var session = new BrowserSession { Name = workspace.Key.WorkspaceName };
            _sessions.Add(session);
            SessionList.SelectedItem = session;
            foreach (var saved in workspace.OrderBy(x => x.TabPosition))
            {
                if (!Uri.TryCreate(saved.Url, UriKind.Absolute, out var uri)) continue;
                await CreateTabAsync(session, uri);
                if (CurrentTab is { } tab) { tab.Title = saved.Title; tab.Group = saved.Group; }
            }
        }
    }

    private void SaveRecoverySnapshot(bool cleanShutdown)
    {
        var snapshot = _sessions.SelectMany((workspace, workspaceIndex) =>
            workspace.Tabs.Select((tab, tabIndex) => new RecoveryTab(
                workspaceIndex, workspace.Name, tabIndex,
                tab.View.CoreWebView2?.Source ?? tab.View.Source?.AbsoluteUri ?? "https://duckduckgo.com",
                tab.Title, tab.Group)));
        _dataStore.SaveRecoverySnapshot(snapshot, cleanShutdown);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _recoveryTimer.Stop();
        SaveRecoverySnapshot(true);
        _dataStore.Dispose();
    }

    private async Task CreateSessionAsync(string name)
    {
        var session = new BrowserSession { Name = name };
        _sessions.Add(session);
        SessionList.SelectedItem = session;
        await CreateTabAsync(session, new Uri("https://duckduckgo.com"));
    }

    private async Task CreateTabAsync(BrowserSession session, Uri uri)
    {
        var view = new WebView2();
        var tab = new BrowserTab { View = view };
        session.Tabs.Add(tab);
        RefreshTabs(session);
        TabStrip.SelectedItem = tab;
        PrimaryHost.Children.Clear();
        PrimaryHost.Children.Add(view);

        await view.EnsureCoreWebView2Async(await GetEnvironmentAsync());
        Harden(view.CoreWebView2);
        await PrivacyGuard.ApplyAsync(view.CoreWebView2);
        WireEvents(tab);
        view.Source = uri;
    }

    private static void Harden(CoreWebView2 core)
    {
        var settings = core.Settings;
        settings.IsStatusBarEnabled = false;
        settings.AreDevToolsEnabled = true;
        settings.AreDefaultContextMenusEnabled = true;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsWebMessageEnabled = false;
        settings.IsReputationCheckingRequired = true;
        core.PermissionRequested += HandlePermissionRequest;
        core.ServerCertificateErrorDetected += (_, e) => e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
    }

    private static void HandlePermissionRequest(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        var origin = Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority)
            : "This page";
        var result = MessageBox.Show(
            $"{origin} is requesting access to {e.PermissionKind}.\n\nAllow this request once?",
            "Newton site permission", MessageBoxButton.YesNo,
            MessageBoxImage.Warning, MessageBoxResult.No);
        e.State = result == MessageBoxResult.Yes
            ? CoreWebView2PermissionState.Allow
            : CoreWebView2PermissionState.Deny;
        e.SavesInProfile = false;
    }

    private Task<CoreWebView2Environment> GetEnvironmentAsync() =>
        _environmentTask ??= CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Newton", "Profiles", _profile.Id, "WebView2"),
            options: new CoreWebView2EnvironmentOptions
            {
                AreBrowserExtensionsEnabled = false
            });

    private void WireEvents(BrowserTab tab)
    {
        var core = tab.View.CoreWebView2;
        core.DocumentTitleChanged += (_, _) => Dispatcher.Invoke(() =>
        {
            tab.Title = string.IsNullOrWhiteSpace(core.DocumentTitle) ? "New page" : core.DocumentTitle;
            TabStrip.Items.Refresh();
            Title = $"{tab.Title} — Newton Alpha";
        });
        core.SourceChanged += (_, _) => Dispatcher.Invoke(UpdateChrome);
        core.HistoryChanged += (_, _) => Dispatcher.Invoke(UpdateChrome);
        core.NewWindowRequested += (_, e) => Dispatcher.InvokeAsync(async () =>
        {
            e.Handled = true;
            if (CurrentSession is not null && Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
                await CreateTabAsync(CurrentSession, uri);
        });
    }

    private void UpdateChrome()
    {
        if (CurrentTab?.View.CoreWebView2 is not { } core) return;
        AddressBox.Text = core.Source;
        BackButton.IsEnabled = core.CanGoBack;
        ForwardButton.IsEnabled = core.CanGoForward;
        SecurityGlyph.Text = core.Source.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "●" : "!";
        SecurityGlyph.Foreground = core.Source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? System.Windows.Media.Brushes.SeaGreen : System.Windows.Media.Brushes.DarkOrange;
    }

    private void RefreshTabs(BrowserSession session)
    {
        TabStrip.ItemsSource = session.Tabs;
        WorkspaceTitle.Text = session.Name.ToUpperInvariant();
    }

    private async void NewSession_Click(object sender, RoutedEventArgs e) =>
        await CreateSessionAsync($"Session {_sessions.Count + 1}");
    private async void NewTab_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentSession is not null) await CreateTabAsync(CurrentSession, new Uri("https://duckduckgo.com"));
    }
    private void Back_Click(object sender, RoutedEventArgs e) { if (CurrentTab?.View.CanGoBack == true) CurrentTab.View.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (CurrentTab?.View.CanGoForward == true) CurrentTab.View.GoForward(); }
    private void Reload_Click(object sender, RoutedEventArgs e) => CurrentTab?.View.Reload();
    private void Go_Click(object sender, RoutedEventArgs e) => Navigate();
    private void AddressBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Navigate(); }
    private void Search_Click(object sender, RoutedEventArgs e) => Search();
    private void SearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) Search(); }

    private void Search()
    {
        var query = SearchBox.Text.Trim();
        if (query.Length == 0 || CurrentTab is null) return;
        var provider = SearchProviderBox.SelectedItem as string ?? "DuckDuckGo";
        CurrentTab.View.Source = NavigationService.CreateSearch(provider, query);
    }

    private void Navigate()
    {
        if (CurrentTab is null) return;
        if (NavigationService.TryResolveAddress(AddressBox.Text, out var destination) && destination is not null)
        {
            CurrentTab.View.Source = destination;
            return;
        }
        MessageBox.Show(
            "Enter a valid website address here. Use the separate search box to search the web.",
            "Invalid web address", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SessionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentSession is not { } session) return;
        RefreshTabs(session);
        TabStrip.SelectedItem = session.Tabs.FirstOrDefault();
    }

    private async void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentTab is not { } tab) return;

        await _tabLifecycle.ActivateAsync(tab, _previousTab, _secondaryTab);

        if (_secondaryTab == tab && SplitToggle.IsChecked == true)
        {
            UpdateChrome();
            return;
        }

        _previousTab = tab;
        PrimaryHost.Children.Clear();
        PrimaryHost.Children.Add(tab.View);
        UpdateChrome();
    }

    private void GroupTab_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentTab is not { } tab) return;
        var index = Array.IndexOf(TabGroups, tab.Group);
        tab.Group = TabGroups[(index + 1) % TabGroups.Length];
        TabStrip.Items.Refresh();
    }

    private void SplitToggle_Click(object sender, RoutedEventArgs e)
    {
        if (SplitToggle.IsChecked == true && CurrentSession is { } session)
        {
            _secondaryTab = session.Tabs.FirstOrDefault(t => t != CurrentTab);
            if (_secondaryTab is null)
            {
                SplitToggle.IsChecked = false;
                MessageBox.Show("Open at least two pages before using split view.", "Split view");
                return;
            }

            BrowserHost.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            BrowserHost.ColumnDefinitions[1].Width = new GridLength(5);
            BrowserHost.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            SplitDivider.Visibility = Visibility.Visible;
            SecondaryHost.Visibility = Visibility.Visible;
            SecondaryHost.Children.Clear();
            if (_secondaryTab.View.CoreWebView2 is { IsSuspended: true } secondaryCore) secondaryCore.Resume();
            SecondaryHost.Children.Add(_secondaryTab.View);
        }
        else
        {
            SecondaryHost.Children.Clear();
            SecondaryHost.Visibility = Visibility.Collapsed;
            SplitDivider.Visibility = Visibility.Collapsed;
            BrowserHost.ColumnDefinitions[1].Width = new GridLength(0);
            BrowserHost.ColumnDefinitions[2].Width = new GridLength(0);
            _secondaryTab = null;
        }
    }

    private void Privacy_Click(object sender, RoutedEventArgs e) => MessageBox.Show(
        "HIGH protection is active by default.\n\n• No Newton telemetry\n• Strict WebView2 tracking prevention\n• Known advertising and analytics hosts blocked\n• Microsoft reputation checking enabled\n• Chromium process sandbox inherited from WebView2\n• Browser extensions disabled\n• High-entropy fingerprint values reduced\n• Website permissions require one-time approval\n• Invalid certificates rejected\n• Password saving and autofill disabled\n• Evergreen engine security updates\n\nThis is mitigation, not anonymity. Some sites may break, and WebView2 cannot provide Tor-level fingerprint resistance.",
        "Privacy status", MessageBoxButton.OK, MessageBoxImage.Information);

    private void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PDF documents (*.pdf)|*.pdf", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true && CurrentTab is not null)
            CurrentTab.View.Source = new Uri(dialog.FileName);
    }

    private async void PictureInPicture_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentTab?.View.CoreWebView2 is not { } core) return;
        var result = await core.ExecuteScriptAsync("""
            (() => {
              const video = [...document.querySelectorAll('video')].find(v => !v.paused) || document.querySelector('video');
              if (!video || !document.pictureInPictureEnabled || video.disablePictureInPicture) return 'unavailable';
              video.requestPictureInPicture(); return 'requested';
            })();
            """);
        if (result.Contains("unavailable", StringComparison.OrdinalIgnoreCase))
            MessageBox.Show("No compatible video was found on this page.", "Picture-in-Picture");
    }

    private async void Screenshot_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentTab?.View.CoreWebView2 is not { } core) return;
        var dialog = new SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png",
            FileName = $"Newton-{DateTime.Now:yyyyMMdd-HHmmss}.png",
            AddExtension = true
        };
        if (dialog.ShowDialog(this) != true) return;
        await using var stream = File.Create(dialog.FileName);
        await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
    }

    private void DeveloperTools_Click(object sender, RoutedEventArgs e) =>
        CurrentTab?.View.CoreWebView2?.OpenDevToolsWindow();

    private void GeneratePassword_Click(object sender, RoutedEventArgs e)
    {
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string digits = "23456789";
        const string symbols = "!@#$%&*+-=?";
        const string all = lower + upper + digits + symbols;
        var password = new char[20];
        password[0] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        password[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        password[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];
        for (var i = 4; i < password.Length; i++)
            password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        RandomNumberGenerator.Shuffle(password.AsSpan());
        Clipboard.SetText(new string(password));
        MessageBox.Show(
            "A 20-character password has been copied to the clipboard. Paste it now; Newton has not stored it.",
            "Strong password generated", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Customise_Click(object sender, RoutedEventArgs e)
    {
        _layoutMode = (_layoutMode + 1) % 3;
        TabColumn.Width = _layoutMode switch
        {
            0 => new GridLength(230),
            1 => new GridLength(150),
            _ => new GridLength(0)
        };
        GroupButton.Visibility = _layoutMode == 2 ? Visibility.Collapsed : Visibility.Visible;
        WorkspaceTitle.Text = $"{CurrentSession?.Name.ToUpperInvariant()} · {(_layoutMode == 0 ? "SPACIOUS" : _layoutMode == 1 ? "COMPACT" : "FOCUS")}";
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        _darkTheme = !_darkTheme;
        var surface = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_darkTheme ? "#111827" : "#FFFFFF"));
        var raised = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_darkTheme ? "#1F2937" : "#F8FAFC"));
        ChromeBar.Background = surface;
        WorkspaceBar.Background = raised;
        VerticalTabsPanel.Background = raised;
        PrimaryHost.Background = surface;
        SecondaryHost.Background = surface;
        AddressBox.Foreground = _darkTheme ? System.Windows.Media.Brushes.WhiteSmoke : System.Windows.Media.Brushes.Black;
    }

    private async void HandleShortcuts(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F12)
        {
            DeveloperTools_Click(this, new RoutedEventArgs());
            e.Handled = true;
            return;
        }
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        if (e.Key == Key.L) { AddressBox.Focus(); AddressBox.SelectAll(); e.Handled = true; }
        if (e.Key == Key.E) { SearchBox.Focus(); SearchBox.SelectAll(); e.Handled = true; }
        if (e.Key == Key.T && CurrentSession is not null) { await CreateTabAsync(CurrentSession, new Uri("https://duckduckgo.com")); e.Handled = true; }
        if (e.Key == Key.K) { SearchBox.Focus(); SearchBox.SelectAll(); e.Handled = true; }
        if (e.Key == Key.G && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { GroupTab_Click(this, new RoutedEventArgs()); e.Handled = true; }
        if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { SplitToggle.IsChecked = !(SplitToggle.IsChecked ?? false); SplitToggle_Click(this, new RoutedEventArgs()); e.Handled = true; }
        if (e.Key == Key.D && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { Theme_Click(this, new RoutedEventArgs()); e.Handled = true; }
        if (e.Key == Key.I && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { DeveloperTools_Click(this, new RoutedEventArgs()); e.Handled = true; }
    }
}
