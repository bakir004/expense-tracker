using System.Net.Http.Headers;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;

namespace ExpenseTrackerAPI.WebApi.Tests.Common;

/// <summary>
/// Base class for E2E tests that require real authentication (not mocked).
/// Provides helpers for user registration, login, and authenticated requests.
/// </summary>
public abstract class BaseE2ETest : IClassFixture<ExpenseTrackerApiFactory>
{
    protected readonly HttpClient Client;
    protected readonly ExpenseTrackerApiFactory Factory;

    protected BaseE2ETest(ExpenseTrackerApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Creates a new HttpClient without the default test auth handler.
    /// Use this for tests that need to test real authentication flows.
    /// </summary>
    protected HttpClient CreateUnauthenticatedClient()
    {
        return Factory.CreateClient();
    }

    /// <summary>
    /// Register a new user and return the response.
    /// </summary>
    protected async Task<HttpResponseMessage> RegisterUserAsync(
        string name,
        string email,
        string password,
        decimal? initialBalance = null)
    {
        var request = new RegisterRequest(name, email, password, initialBalance);
        return await Client.PostAsJsonAsync(TestConstants.Routes.AuthRegister, request);
    }

    /// <summary>
    /// Login with credentials and return the response.
    /// </summary>
    protected async Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        var request = new LoginRequest(email, password);
        return await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, request);
    }

    /// <summary>
    /// Login and return the JWT token.
    /// </summary>
    protected async Task<string?> GetAuthTokenAsync(string email, string password)
    {
        var response = await LoginAsync(email, password);
        if (!response.IsSuccessStatusCode)
            return null;

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResponse?.Token;
    }

    /// <summary>
    /// Creates an HttpClient with the Bearer token set for authenticated requests.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Register a user, login, and return an authenticated client.
    /// Useful for tests that need a fresh user with real auth.
    /// </summary>
    protected async Task<(HttpClient Client, LoginResponse User)?> RegisterAndLoginAsync(
        string name,
        string email,
        string password,
        decimal? initialBalance = null)
    {
        // Register
        var registerResponse = await RegisterUserAsync(name, email, password, initialBalance);
        if (!registerResponse.IsSuccessStatusCode)
            return null;

        // Login
        var loginResponse = await LoginAsync(email, password);
        if (!loginResponse.IsSuccessStatusCode)
            return null;

        var user = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        if (user == null)
            return null;

        // Create authenticated client
        var client = CreateAuthenticatedClient(user.Token);
        return (client, user);
    }

    /// <summary>
    /// Generates a unique email for test isolation.
    /// </summary>
    protected static string GenerateUniqueEmail(string prefix = "test")
    {
        return $"{prefix}.{Guid.NewGuid():N}@expense-tracker-test.local";
    }

    /// <summary>
    /// Generates a unique username for test isolation.
    /// </summary>
    protected static string GenerateUniqueName(string prefix = "Test User")
    {
        return $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";
    }
}
