using Microsoft.Web.WebView2.Core;

namespace NorthstarBrowser.Services;

public static class PrivacyGuard
{
    private static readonly string[] BlockedHostSuffixes =
    [
        "doubleclick.net", "googlesyndication.com", "googleadservices.com",
        "adnxs.com", "amazon-adsystem.com", "criteo.com", "criteo.net",
        "scorecardresearch.com", "taboola.com", "outbrain.com",
        "facebook.net", "connect.facebook.net", "hotjar.com", "segment.io",
        "mixpanel.com", "amplitude.com", "clarity.ms"
    ];

    public static async Task ApplyAsync(CoreWebView2 core)
    {
        core.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Strict;
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += BlockKnownTrackingRequests;

        // Reduce high-entropy values exposed to ordinary page scripts. This is
        // mitigation, not anonymity: WebView2 cannot provide Tor-style uniformity.
        await core.AddScriptToExecuteOnDocumentCreatedAsync("""
            (() => {
              const fixed = (target, key, value) => {
                try { Object.defineProperty(target, key, { get: () => value, configurable: false }); } catch {}
              };
              fixed(Navigator.prototype, 'hardwareConcurrency', 4);
              fixed(Navigator.prototype, 'deviceMemory', 8);
              fixed(Navigator.prototype, 'webdriver', undefined);
              fixed(Screen.prototype, 'colorDepth', 24);
              fixed(Screen.prototype, 'pixelDepth', 24);
            })();
            """);
    }

    private static void BlockKnownTrackingRequests(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri)) return;
        var host = uri.Host.TrimEnd('.');
        if (!BlockedHostSuffixes.Any(blocked =>
                host.Equals(blocked, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith('.' + blocked, StringComparison.OrdinalIgnoreCase))) return;

        if (sender is CoreWebView2 core)
            e.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blocked by Newton", "Content-Type: text/plain");
    }
}
