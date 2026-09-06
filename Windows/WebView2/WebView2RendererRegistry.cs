using Microsoft.Web.WebView2.Wpf;
using Newton.Core.Domain;

namespace NorthstarBrowser.Windows.WebView2;

public sealed class WebView2RendererRegistry : IDisposable
{
    private readonly Dictionary<TabId, WebView2> _renderers = [];
    public WebView2 Create(TabModel tab)
    {
        if (_renderers.ContainsKey(tab.Id)) throw new InvalidOperationException("Tab already has a renderer.");
        var renderer = new WebView2();
        _renderers.Add(tab.Id, renderer);
        tab.EngineInstanceId = new EngineInstanceId(Guid.NewGuid());
        return renderer;
    }
    public WebView2 Get(TabModel tab) => _renderers[tab.Id];
    public bool TryGet(TabModel tab, out WebView2 renderer) => _renderers.TryGetValue(tab.Id, out renderer!);
    public void Discard(TabModel tab)
    {
        if (_renderers.Remove(tab.Id, out var renderer)) renderer.Dispose();
        tab.EngineInstanceId = null;
        tab.State = TabState.Discarded;
    }
    public void Dispose() { foreach (var renderer in _renderers.Values) renderer.Dispose(); _renderers.Clear(); }
}
