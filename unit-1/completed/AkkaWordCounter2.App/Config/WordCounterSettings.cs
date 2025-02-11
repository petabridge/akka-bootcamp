using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AkkaWordCounter2.App.Config;

public class WordCounterSettings
{
    public string[] DocumentUris { get; set; } = [];
}

public sealed class WordCounterSettingsValidator : IValidateOptions<WordCounterSettings>
{
    public ValidateOptionsResult Validate(string? name, WordCounterSettings options)
    {
        var errors = new List<string>();
        
        if (options.DocumentUris.Length == 0)
        {
            errors.Add("DocumentUris must contain at least one URI");
        }
        
        if(options.DocumentUris.Any(uri => !Uri.IsWellFormedUriString(uri, UriKind.Absolute)))
        {
            errors.Add("DocumentUris must contain only absolute URIs");
        }
        
        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}

public static class WordCounterSettingsExtensions
{
    public static IServiceCollection AddWordCounterSettings(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<WordCounterSettings>, WordCounterSettingsValidator>();
        services.AddOptionsWithValidateOnStart<WordCounterSettings>()
            .BindConfiguration("WordCounter");
        
        return services;
    }
}