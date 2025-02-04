using AkkaWordCounter2.App.Parsing;

namespace AkkaWordCounter2.App.Tests;

public class UrlUtilitySpecs
{
    public static readonly AbsoluteUri RootUri = new(new Uri("http://localhost:8080"));
    
    [Theory]
    [InlineData("/some/path")]
    [InlineData("path")]
    [InlineData("path/dodad")]
    public void Should_make_absolute_Uri(string uri)
    {
        // Arrange

        // Act
        var combined = UrlUtilities.ToAbsoluteUri(RootUri, uri);

        // Assert
        UrlUtilities.IsAbsoluteUri(combined.AbsoluteUri);
    }
}