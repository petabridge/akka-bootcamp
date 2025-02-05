using System.Collections.Immutable;

namespace AkkaWordCounter2.App.Tests;

public class CollectionUtilitiesSpecs
{
    [Fact]
    public void ShouldMergeWordCounts()
    {
        // arrange
        var wordCounts1 = new Dictionary<string, int>
        {
            { "hello", 1 },
            { "world", 2 }
        };
        
        var wordCounts2 = new Dictionary<string, int>
        {
            { "hello", 3 },
            { "world", 4 },
            { "foo", 1 }
        };
        
        // act
        var mergedCounts = CollectionUtilities.MergeWordCounts([wordCounts1, wordCounts2]);
        
        // assert
        var expected = ImmutableDictionary<string, int>.Empty
            .Add("hello", 4)
            .Add("world", 6)
            .Add("foo", 1);
        
        Assert.Equivalent(expected, mergedCounts);
    }
}