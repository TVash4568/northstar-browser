using System.IO;

namespace Newton.Core.Browsing;

public interface IBrowserEngine : IAsyncDisposable
{
    BrowserCapabilities Capabilities { get; }
    Uri? Source { get; }
    bool CanGoBack { get; }
    bool CanGoForward { get; }

    event EventHandler<NavigationChangedEventArgs>? NavigationChanged;
    event EventHandler<DownloadRequestedEventArgs>? DownloadRequested;
    event EventHandler<PermissionRequestedEventArgs>? PermissionRequested;

    ValueTask NavigateAsync(Uri destination, CancellationToken cancellationToken = default);
    ValueTask GoBackAsync(CancellationToken cancellationToken = default);
    ValueTask GoForwardAsync(CancellationToken cancellationToken = default);
    ValueTask ReloadAsync(CancellationToken cancellationToken = default);
    ValueTask<string> ExecuteJavaScriptAsync(string script, CancellationToken cancellationToken = default);
    ValueTask SetUserAgentAsync(string? userAgent, CancellationToken cancellationToken = default);
    ValueTask ClearCookiesAsync(Uri? origin = null, CancellationToken cancellationToken = default);
    ValueTask CreateProfileAsync(BrowserProfileOptions options, CancellationToken cancellationToken = default);
    ValueTask CaptureScreenshotAsync(Stream destination, CancellationToken cancellationToken = default);
}

public sealed record BrowserCapabilities(
    bool Profiles,
    bool Screenshots,
    bool DeveloperTools,
    bool Extensions,
    bool HardwareAcceleration);

public sealed record BrowserProfileOptions(string Name, bool IsPrivate = false);

public sealed class NavigationChangedEventArgs(Uri? source, string? title) : EventArgs
{
    public Uri? Source { get; } = source;
    public string? Title { get; } = title;
}

public sealed class DownloadRequestedEventArgs(Uri source, string? suggestedFileName) : EventArgs
{
    public Uri Source { get; } = source;
    public string? SuggestedFileName { get; } = suggestedFileName;
    public bool Cancel { get; set; } = true;
    public string? DestinationPath { get; set; }
}

public sealed class PermissionRequestedEventArgs(Uri origin, BrowserPermission permission) : EventArgs
{
    public Uri Origin { get; } = origin;
    public BrowserPermission Permission { get; } = permission;
    public PermissionDecision Decision { get; set; } = PermissionDecision.Deny;
}

public enum BrowserPermission { Camera, Microphone, Location, Notifications, Clipboard, Other }
public enum PermissionDecision { Deny, AllowOnce }
