using HtmlAgilityPack;

namespace AkkaWordCounter2.App;

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
        foreach (var node in root.Descendants()
                     .Where(n => n.NodeType == HtmlNodeType.Text &&
                            n.ParentNode.Name != "script" &&
                            n.ParentNode.Name != "style"))
        {
            string text = node.InnerText.Trim();
            if (!string.IsNullOrEmpty(text))
                yield return text;
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