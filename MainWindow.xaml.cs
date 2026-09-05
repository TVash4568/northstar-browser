using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using NorthstarBrowser.Models;
using NorthstarBrowser.Services;

namespace NorthstarBrowser;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<BrowserSession> _sessions = [];
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
        Loaded += async (_, _) => await CreateSessionAsync("Start");
        PreviewKeyDown += HandleShortcuts;
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
        settings.AreDevToolsEnabled = false;
        settings.AreDefaultContextMenusEnabled = true;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsWebMessageEnabled = false;
        settings.IsReputationCheckingRequired = true;
        core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
        core.ServerCertificateErrorDetected += (_, e) => e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
    }

    private Task<CoreWebView2Environment> GetEnvironmentAsync() =>
        _environmentTask ??= CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: null,
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
            Title = $"{tab.Title} — Northstar Alpha";
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
    private void Navigate()
    {
        if (RunCommand(AddressBox.Text)) return;
        if (CurrentTab is not null) CurrentTab.View.Source = NavigationService.Resolve(AddressBox.Text);
    }

    private bool RunCommand(string input)
    {
        if (!input.TrimStart().StartsWith('>')) return false;
        switch (input.Trim().ToLowerInvariant())
        {
            case ">split": SplitToggle.IsChecked = !(SplitToggle.IsChecked ?? false); SplitToggle_Click(this, new RoutedEventArgs()); break;
            case ">group": GroupTab_Click(this, new RoutedEventArgs()); break;
            case ">theme": Theme_Click(this, new RoutedEventArgs()); break;
            case ">layout": Customise_Click(this, new RoutedEventArgs()); break;
            default: MessageBox.Show("Commands: >split, >group, >theme, >layout", "Northstar commands"); break;
        }
        return true;
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

        if (_previousTab is { } previous && previous != tab && previous != _secondaryTab && previous.View.CoreWebView2 is not null)
            await previous.View.CoreWebView2.TrySuspendAsync();

        if (tab.View.CoreWebView2 is { IsSuspended: true } activeCore)
            activeCore.Resume();

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
        "HIGH protection is active by default.\n\n• No Northstar telemetry\n• Strict WebView2 tracking prevention\n• Known advertising and analytics hosts blocked\n• Microsoft reputation checking enabled\n• Chromium process sandbox inherited from WebView2\n• Browser extensions disabled\n• High-entropy fingerprint values reduced\n• Website permissions denied\n• Invalid certificates rejected\n• Password saving and autofill disabled\n• Evergreen engine security updates\n\nThis is mitigation, not anonymity. Some sites may break, and WebView2 cannot provide Tor-level fingerprint resistance.",
        "Privacy status", MessageBoxButton.OK, MessageBoxImage.Information);

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
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        if (e.Key == Key.L) { AddressBox.Focus(); AddressBox.SelectAll(); e.Handled = true; }
        if (e.Key == Key.T && CurrentSession is not null) { await CreateTabAsync(CurrentSession, new Uri("https://duckduckgo.com")); e.Handled = true; }
        if (e.Key == Key.K) { AddressBox.Text = ">"; AddressBox.Focus(); AddressBox.CaretIndex = 1; e.Handled = true; }
        if (e.Key == Key.G && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { GroupTab_Click(this, new RoutedEventArgs()); e.Handled = true; }
        if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { SplitToggle.IsChecked = !(SplitToggle.IsChecked ?? false); SplitToggle_Click(this, new RoutedEventArgs()); e.Handled = true; }
        if (e.Key == Key.D && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) { Theme_Click(this, new RoutedEventArgs()); e.Handled = true; }
    }
}
