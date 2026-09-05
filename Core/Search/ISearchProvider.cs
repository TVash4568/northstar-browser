namespace Newton.Core.Search;

public interface ISearchProvider
{
    string Id { get; }
    string DisplayName { get; }
    Uri CreateSearchUri(string query);
}

public interface ISuggestionProvider
{
    string Id { get; }
    bool UsesNetwork { get; }
    IAsyncEnumerable<SearchSuggestion> SuggestAsync(string input, CancellationToken cancellationToken = default);
}

public sealed record SearchSuggestion(string Text, SearchSuggestionKind Kind, Uri? Destination = null);
public enum SearchSuggestionKind { Search, LocalHistory, Bookmark, OpenTab }

public sealed class DisabledSuggestionProvider : ISuggestionProvider
{
    public string Id => "disabled";
    public bool UsesNetwork => false;
    public async IAsyncEnumerable<SearchSuggestion> SuggestAsync(string input, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }
}
