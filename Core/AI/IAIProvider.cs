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
    AIContextEnvelope? Context = null);

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
