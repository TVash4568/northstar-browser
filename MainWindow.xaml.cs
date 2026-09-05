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
        BrowserHost.Children.Clear();
        BrowserHost.Children.Add(view);

        await view.EnsureCoreWebView2Async();
        Harden(view.CoreWebView2);
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
        core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
        core.ServerCertificateErrorDetected += (_, e) => e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
    }

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
        TabStrip.DisplayMemberPath = nameof(BrowserTab.Title);
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
    private void Navigate() { if (CurrentTab is not null) CurrentTab.View.Source = NavigationService.Resolve(AddressBox.Text); }

    private void SessionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentSession is not { } session) return;
        RefreshTabs(session);
        TabStrip.SelectedItem = session.Tabs.FirstOrDefault();
    }

    private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentTab is not { } tab) return;
        BrowserHost.Children.Clear();
        BrowserHost.Children.Add(tab.View);
        UpdateChrome();
    }

    private void Privacy_Click(object sender, RoutedEventArgs e) => MessageBox.Show(
        "Northstar blocks all website permission requests by default, rejects invalid certificates, disables password saving and sends no browser telemetry.\n\nAlpha limitation: tracker blocking and per-site permission prompts are not implemented yet.",
        "Privacy status", MessageBoxButton.OK, MessageBoxImage.Information);

    private async void HandleShortcuts(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
        if (e.Key == Key.L) { AddressBox.Focus(); AddressBox.SelectAll(); e.Handled = true; }
        if (e.Key == Key.T && CurrentSession is not null) { await CreateTabAsync(CurrentSession, new Uri("https://duckduckgo.com")); e.Handled = true; }
    }
}
