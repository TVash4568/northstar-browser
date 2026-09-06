using Newton.Core.Domain;
using WebViewControl = Microsoft.Web.WebView2.Wpf.WebView2;

namespace NorthstarBrowser.Windows.WebView2;

public sealed class WebView2RendererRegistry : IDisposable
{
    private readonly Dictionary<TabId, WebViewControl> _renderers = [];
    public WebViewControl Create(TabModel tab)
    {
        if (_renderers.ContainsKey(tab.Id)) throw new InvalidOperationException("Tab already has a renderer.");
        var renderer = new WebViewControl();
        _renderers.Add(tab.Id, renderer);
        tab.EngineInstanceId = new EngineInstanceId(Guid.NewGuid());
        return renderer;
    }
    public WebViewControl Get(TabModel tab) => _renderers[tab.Id];
    public bool TryGet(TabModel tab, out WebViewControl renderer) => _renderers.TryGetValue(tab.Id, out renderer!);
    public void Discard(TabModel tab)
    {
        if (_renderers.Remove(tab.Id, out var renderer)) renderer.Dispose();
        tab.EngineInstanceId = null;
        tab.State = TabState.Discarded;
    }
    public void Dispose() { foreach (var renderer in _renderers.Values) renderer.Dispose(); _renderers.Clear(); }
}
