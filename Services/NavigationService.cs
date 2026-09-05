namespace NorthstarBrowser.Services;

public static class NavigationService
{
    private static readonly Uri SearchEndpoint = new("https://duckduckgo.com/?q=");

    public static Uri Resolve(string input)
    {
        input = input.Trim();
        if (Uri.TryCreate(input, UriKind.Absolute, out var absolute) &&
            absolute.Scheme is "https" or "http") return absolute;

        if (!input.Contains(' ') && input.Contains('.') &&
            Uri.TryCreate("https://" + input, UriKind.Absolute, out var host)) return host;

        return new Uri(SearchEndpoint + Uri.EscapeDataString(input));
    }
}
