using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace SampleCkWebApp.Infrastructure.Users.Options;

public sealed class UserOptions
{
    public const string SectionName = "Database";

    public string? ConnectionString { get; init; }
    
    public static ValidateOptionsResult Validate(UserOptions? options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail(
                $"Configuration section '{SectionName}' is null.");
        }
        
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail(
                $"Property '{nameof(options.ConnectionString)}' is required.");
        }
        
        return ValidateOptionsResult.Success;
    }
}

public static class UserOptionsExtensions
{
    public static IServiceCollection TryAddUserOptions(this IServiceCollection services, UserOptions? options = null)
    {
        var validationResult = UserOptions.Validate(options);
        if (!validationResult.Succeeded)
        {
            throw new OptionsValidationException(UserOptions.SectionName, typeof(UserOptions), validationResult.Failures);
        }
        
        services.TryAddSingleton(options!);
        return services;
    }
    
    public static UserOptions? GetUserOptions(this IConfiguration configuration)
    {
        var section = configuration.GetSection(UserOptions.SectionName);
        if (!section.Exists())
        {
            return null;
        }
        
        UserOptions options = new();
        section.Bind(options);
        return options;
    }
}

