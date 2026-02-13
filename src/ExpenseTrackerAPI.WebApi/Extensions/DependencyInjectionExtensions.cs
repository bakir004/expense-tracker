using ExpenseTrackerAPI.Application.Common.Interfaces;
using ExpenseTrackerAPI.Application.Users.Interfaces.Application;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Users;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Transactions;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Application;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Categories;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Application;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.TransactionGroups;
using ExpenseTrackerAPI.Infrastructure.Authentication;
using ExpenseTrackerAPI.Infrastructure.Users;
using ExpenseTrackerAPI.Infrastructure.Transactions;
using ExpenseTrackerAPI.Infrastructure.Categories;
using ExpenseTrackerAPI.Infrastructure.TransactionGroups;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Asp.Versioning;

namespace ExpenseTrackerAPI.WebApi.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Register application layer services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ITransactionGroupService, TransactionGroupService>();

        return services;
    }

    /// <summary>
    /// Register infrastructure layer services.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITransactionGroupRepository, TransactionGroupRepository>();

        return services;
    }

    /// <summary>
    /// Configure JWT authentication and authorization.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secretKey = jwtSection["SecretKey"];
        var issuer = jwtSection["Issuer"] ?? "ExpenseTrackerAPI";
        var audience = jwtSection["Audience"] ?? "ExpenseTrackerAPI-Users";

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException(
                "JWT secret key is not configured. Please set 'Jwt:SecretKey' in configuration.");
        }

        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT secret key must be at least 32 characters long for security.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero // Remove default 5 minute tolerance
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Configure API versioning with multiple versioning strategies.
    /// </summary>
    public static IServiceCollection AddApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Set default API version
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;

            // Support multiple versioning strategies
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),           // /api/v1/auth/login
                new QueryStringApiVersionReader("version"), // ?version=1.0
                new HeaderApiVersionReader("X-Version"),    // X-Version: 1.0
                new MediaTypeApiVersionReader("ver")        // Accept: application/json;ver=1.0
            );

            // Version format
            options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
        }).AddApiExplorer(options =>
        {
            // Format version as "'v'major[.minor][-status]"
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
