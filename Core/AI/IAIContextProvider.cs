namespace Newton.Core.AI;

public interface IAIContextProvider
{
    ValueTask<AIContextEnvelope> CreateContextAsync(
        AIContextRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AIContextRequest(
    AIContextKind Kind,
    string DestinationProviderId,
    IReadOnlyCollection<Guid> SelectedTabIds,
    bool IsPrivateProfile,
    bool UserConfirmedThisAction);

public sealed record AIContextEnvelope(
    AIContextKind Kind,
    string DestinationProviderId,
    IReadOnlyCollection<AIContextItem> Items,
    DateTimeOffset GrantedAtUtc);

public sealed record AIContextItem(Uri Origin, string Text);

public enum AIContextKind
{
    None,
    SelectedText,
    CurrentPage,
    CurrentTab,
    SelectedTabs,
    BrowsingHistory
}

public static class AIContextGuard
{
    public static void Validate(AIContextRequest request)
    {
        if (request.IsPrivateProfile && request.Kind != AIContextKind.None)
            throw new InvalidOperationException("Private-profile context cannot be shared with AI.");
        if (request.Kind != AIContextKind.None && !request.UserConfirmedThisAction)
            throw new InvalidOperationException("Every browser-context transfer requires explicit per-action consent.");
        if (request.Kind == AIContextKind.SelectedTabs && request.SelectedTabIds.Count == 0)
            throw new InvalidOperationException("Selected-tab context requires an explicit tab selection.");
    }
}
