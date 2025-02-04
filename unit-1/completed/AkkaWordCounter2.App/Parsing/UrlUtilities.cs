namespace AkkaWordCounter2.App.Parsing;

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

public static class UrlUtilities
{
    public static bool CanMakeAbsoluteHttpUri(AbsoluteUri baseUri, string rawUri)
    {
        // this will not return true for things like "mailto:" or "tel:" links
        if (IsAbsoluteUri(rawUri))
            return true;
        try
        {
            var absUri = new Uri(baseUri.Value, rawUri);
            var returnVal = absUri.Scheme.Equals(Uri.UriSchemeHttp) || absUri.Scheme.Equals(Uri.UriSchemeHttps);
            return returnVal;
        }
        catch
        {
            return false;
        }
    }

    public static bool AbsoluteUriIsInDomain(AbsoluteUri baseUrl, AbsoluteUri otherUri)
    {
        return AbsoluteUriIsInDomain(baseUrl, otherUri.Value);
    }

    public static bool AbsoluteUriIsInDomain(AbsoluteUri baseUrl, Uri otherUri)
    {
        return baseUrl.Value.Host == otherUri.Host;
    }
    
    public static bool IsAbsoluteUri(string rawUri)
    {
        return rawUri.StartsWith(Uri.UriSchemeHttp) || rawUri.StartsWith(Uri.UriSchemeHttps);
    }
    
    public static Uri ToAbsoluteUri(AbsoluteUri root, string rawUri)
    {
        return Uri.IsWellFormedUriString(rawUri, UriKind.Absolute)
            ? new Uri(rawUri, UriKind.Absolute)
            : new Uri(root.Value, rawUri);
    }
}