using System.Collections.ObjectModel;

namespace NorthstarBrowser.Models;

public sealed class BrowserSession
{
    public required string Name { get; init; }
    public string Initial => Name[..1].ToUpperInvariant();
    public ObservableCollection<BrowserTab> Tabs { get; } = [];
}
