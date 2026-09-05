using NorthstarBrowser.Models;

namespace NorthstarBrowser.Services;

public sealed class TabLifecycleManager
{
    public async ValueTask ActivateAsync(BrowserTab active, BrowserTab? previous, BrowserTab? protectedTab)
    {
        if (previous is not null && previous != active && previous != protectedTab && previous.View.CoreWebView2 is not null)
        {
            await previous.View.CoreWebView2.TrySuspendAsync();
            previous.LifecycleState = TabLifecycleState.Suspended;
        }

        if (active.View.CoreWebView2 is { IsSuspended: true } core) core.Resume();
        active.LifecycleState = TabLifecycleState.Active;
        active.LastActivatedUtc = DateTimeOffset.UtcNow;
    }
}
