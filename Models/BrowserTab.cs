using Microsoft.Web.WebView2.Wpf;

namespace NorthstarBrowser.Models;

public sealed class BrowserTab
{
    public required WebView2 View { get; init; }
    public string Title { get; set; } = "New page";
    public override string ToString() => Title.Length > 24 ? Title[..24] + "…" : Title;
}
