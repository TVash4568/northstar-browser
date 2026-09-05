using Microsoft.Web.WebView2.Wpf;

namespace NorthstarBrowser.Models;

public sealed class BrowserTab
{
    public required WebView2 View { get; init; }
    public string Title { get; set; } = "New page";
    public string Group { get; set; } = "General";
    public DateTimeOffset LastActivatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public TabLifecycleState LifecycleState { get; set; } = TabLifecycleState.Active;
    public string DisplayTitle => $"{Group}  ·  {(Title.Length > 22 ? Title[..22] + "…" : Title)}";
    public override string ToString() => DisplayTitle;
}

public enum TabLifecycleState { Active, Background, Suspended, Discarded }
