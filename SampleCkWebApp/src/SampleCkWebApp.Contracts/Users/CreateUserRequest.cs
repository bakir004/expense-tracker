// ============================================================================
// FILE: CreateUserRequest.cs
// ============================================================================
// WHAT: API contract DTO (Data Transfer Object) for user creation requests.
//
// WHY: This DTO exists in the Contracts layer to define the structure of
//      incoming API requests for creating users. Contracts are shared between
//      the API layer (WebApi) and external consumers. By placing request/response
//      models in a separate Contracts project, we can version APIs independently
//      and share these contracts with API clients (e.g., for code generation).
//
// WHAT IT DOES:
//      - Defines the shape of POST /users request body
//      - Contains three required properties: Name, Email, and Password
//      - Includes XML documentation for Swagger/OpenAPI generation
//      - Used by UsersController.CreateUser endpoint
//      - Validated by UserValidator in Application layer
//      - Acts as the contract between API consumers and the application
// ============================================================================

namespace SampleCkWebApp.Users;

/// <summary>
/// Request model for creating a new user
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// User's full name (1-100 characters)
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// User's email address (must be unique and valid email format)
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User's password (minimum 6 characters, will be hashed before storage)
    /// </summary>
    /// <example>securepassword123</example>
    public string Password { get; set; } = string.Empty;
}

