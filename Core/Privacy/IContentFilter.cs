namespace Newton.Core.Privacy;

public interface IContentFilter
{
    ContentFilterDecision Evaluate(Uri requestUri);
}

public readonly record struct ContentFilterDecision(bool ShouldBlock, string? Reason = null)
{
    public static ContentFilterDecision Allow => new(false);
    public static ContentFilterDecision Block(string reason) => new(true, reason);
}

public sealed class HostContentFilter : IContentFilter
{
    private static readonly string[] BlockedHostSuffixes =
    [
        "doubleclick.net", "googlesyndication.com", "googleadservices.com",
        "adnxs.com", "amazon-adsystem.com", "criteo.com", "criteo.net",
        "scorecardresearch.com", "taboola.com", "outbrain.com",
        "facebook.net", "connect.facebook.net", "hotjar.com", "segment.io",
        "mixpanel.com", "amplitude.com", "clarity.ms"
    ];

    public ContentFilterDecision Evaluate(Uri requestUri)
    {
        var host = requestUri.Host.TrimEnd('.');
        return BlockedHostSuffixes.Any(blocked =>
            host.Equals(blocked, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith('.' + blocked, StringComparison.OrdinalIgnoreCase))
            ? ContentFilterDecision.Block("Known advertising or tracking host")
            : ContentFilterDecision.Allow;
    }
}
