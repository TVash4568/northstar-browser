using Microsoft.Web.WebView2.Core;
using Newton.Core.Privacy;

namespace NorthstarBrowser.Windows.WebView2;

public sealed class WebView2ContentFilterAdapter(IContentFilter filter, PrivacyLevel level = PrivacyLevel.Strict)
{
    public PrivacyLevel Level => level;
    public PrivacyRulesetMetadata Ruleset => filter.Metadata;
    public async Task ApplyAsync(CoreWebView2 core)
    {
        core.Profile.PreferredTrackingPreventionLevel = level switch
        {
            PrivacyLevel.Standard => CoreWebView2TrackingPreventionLevel.Basic,
            PrivacyLevel.Balanced => CoreWebView2TrackingPreventionLevel.Balanced,
            _ => CoreWebView2TrackingPreventionLevel.Strict
        };
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += (_, e) =>
        {
            if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri) || !filter.Evaluate(uri).ShouldBlock) return;
            e.Response = core.Environment.CreateWebResourceResponse(null, 403, "Blocked by Newton", "Content-Type: text/plain");
        };

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
}
