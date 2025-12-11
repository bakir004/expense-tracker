using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace SampleCkWebApp.Infrastructure.MessageHistory.Options;

public sealed class MessageHistoryOptions
{
    public const string SectionName = "MessageHistory";

    public string? DatabaseUrl { get; init; }
    
    public static ValidateOptionsResult Validate(MessageHistoryOptions? options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail(
                $"Configuration section '{SectionName}' is null.");
        }
        
        // Ensure DatabaseUrl not blank
        if (string.IsNullOrWhiteSpace(options.DatabaseUrl))
        {
            return ValidateOptionsResult.Fail(
                $"Property '{nameof(options.DatabaseUrl)}' is required.");
        }
        
        // Ensure DatabaseUrl is a valid URL
        if (!Uri.TryCreate(options.DatabaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail(
                $"Property '{nameof(options.DatabaseUrl)}' is not a valid URL.");
        }
        
        // Ensure DatabaseUrl always ends with '/'
        if (!options.DatabaseUrl.EndsWith('/'))
        {
            return ValidateOptionsResult.Fail(
                $"Property '{nameof(options.DatabaseUrl)}' must end with '/'.");
        }
        
        return ValidateOptionsResult.Success;
    }
}

public static class MessageHistoryOptionsExtensions
{
    public static IServiceCollection TryAddMessageHistoryOptions(this IServiceCollection services, MessageHistoryOptions? options = null)
    {
        var validationResult = MessageHistoryOptions.Validate(options);
        if (!validationResult.Succeeded)
        {
            throw new OptionsValidationException(MessageHistoryOptions.SectionName, typeof(MessageHistoryOptions), validationResult.Failures);
        }
        
        services.TryAddSingleton(options!);
        return services;
    }
    
    public static MessageHistoryOptions? GetMessageHistoryOptions(this IConfiguration configuration)
    {
        var section = configuration.GetSection(MessageHistoryOptions.SectionName);
        if (!section.Exists())
        {
            return null;
        }
        
        MessageHistoryOptions options = new();
        section.Bind(options);
        return options;
    }
}