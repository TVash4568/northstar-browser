namespace Newton.Core.Privacy;

public interface IContentFilter
{
    PrivacyRulesetMetadata Metadata { get; }
    ContentFilterDecision Evaluate(Uri requestUri);
}

public enum PrivacyLevel { Standard, Balanced, Strict }
public sealed record PrivacyRulesetMetadata(string Version, DateOnly PublishedOn, string IntegritySha256);

public readonly record struct ContentFilterDecision(bool ShouldBlock, string? Reason = null)
{
    public static ContentFilterDecision Allow => new(false);
    public static ContentFilterDecision Block(string reason) => new(true, reason);
}

public sealed class HostContentFilter(PrivacyLevel level = PrivacyLevel.Strict) : IContentFilter
{
    public PrivacyRulesetMetadata Metadata { get; } = new(
        "newton-hosts-2026.09.1",
        new DateOnly(2026, 9, 6),
        "90457002acdeb7b56c3beabfee9b55b2bb720b798606a890575e222f23a7cbb1");
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
        if (level is PrivacyLevel.Standard) return ContentFilterDecision.Allow;
        var host = requestUri.Host.TrimEnd('.');
        return BlockedHostSuffixes.Any(blocked =>
            host.Equals(blocked, StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith('.' + blocked, StringComparison.OrdinalIgnoreCase))
            ? ContentFilterDecision.Block("Known advertising or tracking host")
            : ContentFilterDecision.Allow;
    }
}
