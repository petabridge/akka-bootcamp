using HtmlAgilityPack;

namespace AkkaWordCounter2.App.Tests;

public class TextExtractorSpecs
{
    public const string HtmlForTextEtraction = """
                                               <html>
                                                <head><title>This text should will appear</title></head>
                                                <body>
                                                    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
                                                    <script>
                                                        // this should not appear
                                                        $(document).ready(function() {
                                                            console.log("Hello, world!");
                                                        });
                                                    </script>
                                                    <div>
                                                        <p>This is a test</p>
                                                        <ol>
                                                            <li><a href="https://petabridge.com/">One</a></li>
                                                            <li>Two</li>
                                                            <li>Three</li>
                                                        </ol>
                                                    </div>
                                                </body>
                                               </html>
                                               """;
    
    [Fact]
    public void TextExtractor_should_extract_text_from_html()
    {
        // arrange
        var html = new HtmlDocument();
        html.LoadHtml(HtmlForTextEtraction);
        
        // act
        var text = TextExtractor.ExtractText(html).ToList();
        
        // assert
        Assert.Equal(5, text.Count); // 5 text elements
    }
}