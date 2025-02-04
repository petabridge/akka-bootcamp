using HtmlAgilityPack;

namespace AkkaWordCounter2.App.Parsing;

public static class TextExtractor
{
    /// <summary>
    /// Extracts raw text from a HtmlDocument
    /// </summary>
    /// <remarks>
    /// Shouldn't pick up stuff from script / style tags etc
    /// </remarks>
    public static IEnumerable<string> ExtractText(HtmlDocument htmlDocument)
    {
        var root = htmlDocument.DocumentNode;
        foreach (var node in root.DescendantsAndSelf())
        {
            if (!node.HasChildNodes)
            {
                string text = node.InnerText;
                if (!string.IsNullOrEmpty(text))
                    yield return text.Trim();
            }
        }
    }
    
    public static IEnumerable<string> ExtractTokens(string text)
    {
        var tokens = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            yield return token.Trim();
        }
    }
}