using System.Collections.ObjectModel;

namespace NorthstarBrowser.Models;

public sealed class BrowserProfile
{
    public required string Id { get; init; }
    public required string Name { get; set; }
    public bool IsPrivate { get; init; }
    public ObservableCollection<BrowserSession> Workspaces { get; } = [];
}
