// ============================================================================
// FILE: UserOptions.cs
// ============================================================================
// WHAT: Configuration options class for user repository database settings.
//
// WHY: This options class exists in the Infrastructure layer to provide
//      strongly-typed configuration for database connections. It follows
//      the Options pattern from Microsoft.Extensions.Options, which is
//      the standard way to handle configuration in .NET. By validating
//      configuration at startup, we fail fast if the connection string
//      is missing or invalid, rather than discovering it at runtime.
//
// WHAT IT DOES:
//      - Defines UserOptions class with ConnectionString property
//      - Validates that ConnectionString is not null or empty
//      - Provides extension methods to bind from IConfiguration
//      - Registers options in DI container with validation
//      - Used by UserRepository to get database connection string
//      - Configuration is read from appsettings.json "Database" section
// ============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ExpenseTrackerAPI.Infrastructure.Users.Options;

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

