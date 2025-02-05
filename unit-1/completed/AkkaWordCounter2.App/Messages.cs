using System.Collections.Immutable;

namespace AkkaWordCounter2.App;

/// <summary>
/// Value type for enforcing absolute uris
/// </summary>
public record struct AbsoluteUri
{
    public AbsoluteUri(Uri value)
    {
        Value = value;
        if (!value.IsAbsoluteUri)
            throw new ArgumentException("Value must be an absolute URL", nameof(value));
    }

    public Uri Value { get; }

    public override string ToString() => Value.ToString();
}

public interface IWithDocumentId
{
    AbsoluteUri DocumentId { get; }
}

public static class DocumentCommands
{
    public sealed record ScanDocument(AbsoluteUri DocumentId) : IWithDocumentId;

    public sealed record ScanDocuments(IReadOnlyList<AbsoluteUri> DocumentIds);
}

public static class DocumentEvents
{
    public sealed record DocumentScanFailed(AbsoluteUri DocumentId, string Reason) : IWithDocumentId;
    
    public sealed record WordsFound(AbsoluteUri DocumentId, IReadOnlyList<string> Tokens) : IWithDocumentId;
    
    public sealed record EndOfDocumentReached(AbsoluteUri DocumentId) : IWithDocumentId;

    public sealed record CountsTabulatedForDocument(AbsoluteUri DocumentId, ImmutableDictionary<string, int> WordFrequencies)
        : IWithDocumentId;

    public sealed record CountsTabulatedForDocuments(
        IReadOnlyList<AbsoluteUri> Documents,
        IImmutableDictionary<string, int> WordFrequencies);
}

public static class DocumentQueries
{
    public sealed record FetchCounts(AbsoluteUri DocumentId) : IWithDocumentId;

    public sealed class SubscribeToAllCounts
    {
        public static readonly SubscribeToAllCounts Instance = new();
        private SubscribeToAllCounts(){}
    }
}