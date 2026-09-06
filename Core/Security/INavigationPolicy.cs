namespace Newton.Core.Security;

public interface INavigationPolicy
{
    NavigationDecision Evaluate(Uri destination, bool hasUserGesture);
}

public readonly record struct NavigationDecision(bool IsAllowed, bool RequiresExternalLaunch, string? Reason = null);

public sealed class DefaultNavigationPolicy : INavigationPolicy
{
    public NavigationDecision Evaluate(Uri destination, bool hasUserGesture) => destination.Scheme.ToLowerInvariant() switch
    {
        "https" or "http" => new(true, false),
        "file" => new(hasUserGesture, false, hasUserGesture ? null : "Local files require an explicit user action."),
        "mailto" or "tel" => new(hasUserGesture, true, hasUserGesture ? null : "External protocols require an explicit user action."),
        "data" or "blob" => new(false, false, "Direct navigation to embedded-data schemes is blocked."),
        "javascript" => new(false, false, "JavaScript URLs are blocked."),
        "newton" => new(true, false),
        _ => new(false, false, "Unsupported address scheme.")
    };
}
