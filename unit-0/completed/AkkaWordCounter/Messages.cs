using Akka.Actor;

namespace AkkaWordCounter;

public static class DocumentCommands{
    public sealed record ProcessDocument(string RawText);
}

public enum IgnoreTokenBehavior{
    Replace = 0,
    Add = 1
}

// Counter Inputs
public static class CounterCommands{
    public sealed record CountTokens(IReadOnlyList<string> Tokens);

    // provides a set of words to exclude from counting, like prepositions or articles
    public sealed record SetIgnoreTokens(IReadOnlyList<string> IgnoreTokens, 
        IgnoreTokenBehavior Behavior = IgnoreTokenBehavior.Add);

    // parser reached the end of the document
    public sealed record ExpectNoMoreTokens();
}

// Counter Queries
public static class CounterQueries{
    // Send this actor a notification once counting is complete
    public sealed record FetchCounts(IActorRef Subscriber);
}