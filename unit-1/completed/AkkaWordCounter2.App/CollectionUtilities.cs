using System.Collections.Immutable;

namespace AkkaWordCounter2.App;

public static class CollectionUtilities
{
    public static ImmutableDictionary<string, int> MergeWordCounts(IEnumerable<ImmutableDictionary<string, int>> counts)
    {
        var mergedCounts = counts.Aggregate(ImmutableDictionary<string, int>.Empty,
            (acc, next) =>
            {
                foreach (var (word, count) in next)
                {
                    acc = acc.SetItem(word, acc.GetValueOrDefault(word, 0) + count);
                }

                return acc;
            });
        return mergedCounts;
    }
}