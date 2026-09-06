using Newton.Core.Domain;

namespace NorthstarBrowser.Services;

public sealed class TabLifecycleManager
{
    public TabModel? Activate(TabModel active, TabModel? previous, TabModel? protectedTab)
    {
        TabModel? suspend = null;
        if (previous is not null && previous != active && previous != protectedTab)
        {
            previous.State = TabState.Sleeping;
            suspend = previous;
        }
        active.State = TabState.Active;
        active.LastActive = DateTimeOffset.UtcNow;
        return suspend;
    }
}
