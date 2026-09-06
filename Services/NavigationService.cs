namespace NorthstarBrowser.Services;

using Newton.Core.Security;

public static class NavigationService
{
    private static readonly INavigationPolicy Policy = new DefaultNavigationPolicy();
    private static readonly IReadOnlyDictionary<string, string> SearchEndpoints =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DuckDuckGo"] = "https://duckduckgo.com/?q=",
            ["Bing"] = "https://www.bing.com/search?q=",
            ["Brave Search"] = "https://search.brave.com/search?q=",
            ["Google"] = "https://www.google.com/search?q="
        };

    public static IReadOnlyCollection<string> SearchProviders => SearchEndpoints.Keys.ToArray();

    public static bool TryResolveAddress(string input, out Uri? destination)
    {
        input = input.Trim();
        if (Uri.TryCreate(input, UriKind.Absolute, out var absolute) &&
            Policy.Evaluate(absolute, hasUserGesture: true).IsAllowed)
        {
            destination = absolute;
            return true;
        }

        if (!input.Contains(' ') &&
            (input.Contains('.') || input.StartsWith("localhost", StringComparison.OrdinalIgnoreCase)) &&
            Uri.TryCreate("https://" + input, UriKind.Absolute, out var host))
        {
            destination = host;
            return true;
        }

        destination = null;
        return false;
    }

    public static NavigationDecision Evaluate(Uri destination, bool hasUserGesture = true) =>
        Policy.Evaluate(destination, hasUserGesture);

    public static Uri CreateSearch(string provider, string query)
    {
        if (!SearchEndpoints.TryGetValue(provider, out var endpoint))
            endpoint = SearchEndpoints["DuckDuckGo"];
        return new Uri(endpoint + Uri.EscapeDataString(query.Trim()));
    }
}
