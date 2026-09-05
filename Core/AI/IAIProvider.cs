namespace Newton.Core.AI;

public interface IAIProvider
{
    string Id { get; }
    string DisplayName { get; }
    AIProviderCapabilities Capabilities { get; }

    IAsyncEnumerable<AIResponseChunk> CompleteAsync(
        AIRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AIRequest(
    string Prompt,
    AIPageContext? PageContext = null);

public sealed record AIPageContext
{
    internal AIPageContext(Uri origin, string text, DateTimeOffset grantedAtUtc)
        => (Origin, Text, GrantedAtUtc) = (origin, text, grantedAtUtc);

    public Uri Origin { get; }
    public string Text { get; }
    public DateTimeOffset GrantedAtUtc { get; }
}

public static class AIContextPolicy
{
    public static AIPageContext CreatePageContext(
        Uri origin,
        string pageText,
        bool isPrivateProfile,
        bool userConfirmedThisAction)
    {
        if (isPrivateProfile)
            throw new InvalidOperationException("Private-page content cannot be shared with an AI provider.");
        if (!userConfirmedThisAction)
            throw new InvalidOperationException("Explicit page-context consent is required for each AI action.");
        if (string.IsNullOrWhiteSpace(pageText))
            throw new ArgumentException("Page context cannot be empty.", nameof(pageText));
        return new AIPageContext(origin, pageText, DateTimeOffset.UtcNow);
    }
}

public sealed record AIResponseChunk(string Text, bool IsComplete = false);

public sealed record AIProviderCapabilities(
    bool IsLocal,
    bool SupportsStreaming,
    bool AcceptsPageContext);

public enum AIProviderSelection
{
    Disabled,
    OpenAI,
    Gemini,
    Claude,
    Local
}
