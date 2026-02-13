using System.Net;
using System.Net.Http.Json;
using ExpenseTrackerAPI.Contracts.Users;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using ExpenseTrackerAPI.WebApi.Tests.Common;
using ExpenseTrackerAPI.WebApi.Tests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseTrackerAPI.WebApi.Tests.E2E.Users;

/// <summary>
/// E2E tests for user profile management API endpoints.
/// Tests profile update and deletion endpoints.
///
/// NOTE: These tests use the TestAuthHandler which authenticates all requests as user ID 1
/// (the first seeded user "John Doe" with email "john.doe@email.com").
/// </summary>
public class UserApiTests : BaseE2ETest
{
    public UserApiTests(ExpenseTrackerApiFactory factory) : base(factory) { }

    #region Update Profile Tests

    [Fact]
    public async Task UpdateProfile_WithValidData_ShouldUpdateUserSuccessfully()
    {
        // Arrange - Use seeded user (test auth handler authenticates as user ID 1)
        // Get original user data first
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var originalUser = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        originalUser.Should().NotBeNull();

        var newName = $"Updated Name {Guid.NewGuid().ToString("N")[..8]}";

        // Act - Update profile (we keep the same email since it's the seeded user)
        var updateRequest = new UpdateUserRequest(
            Name: newName,
            Email: originalUser!.Email, // Keep same email
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 1000.00m);

        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateResponse = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        updateResponse.Should().NotBeNull();
        updateResponse!.Name.Should().Be(newName);
        updateResponse.Email.Should().Be(originalUser.Email);
        updateResponse.InitialBalance.Should().Be(1000.00m);

        // Restore original name for other tests
        var restoreRequest = new UpdateUserRequest(
            Name: originalUser.Name,
            Email: originalUser.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: originalUser.InitialBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidCurrentPassword_ShouldReturnUnauthorized()
    {
        // Arrange - Use seeded user
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);

        var updateRequest = new UpdateUserRequest(
            Name: "New Name",
            Email: user!.Email,
            NewPassword: null,
            CurrentPassword: "WrongP@ssword123!", // Wrong password
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithNewPassword_ShouldUpdatePasswordSuccessfully()
    {
        // Arrange - Use seeded user
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var oldPassword = TestConstants.TestUsers.SeededUserPassword;
        var newPassword = "NewP@ssw0rd!123";

        // Act - Update password
        var updateRequest = new UpdateUserRequest(
            Name: user!.Name,
            Email: user.Email,
            NewPassword: newPassword,
            CurrentPassword: oldPassword,
            InitialBalance: user.InitialBalance);

        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify login with new password works
        var loginRequest = new LoginRequest(user.Email, newPassword);
        var loginResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore original password for other tests
        var restoreRequest = new UpdateUserRequest(
            Name: user.Name,
            Email: user.Email,
            NewPassword: oldPassword,
            CurrentPassword: newPassword,
            InitialBalance: user.InitialBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithDuplicateEmail_ShouldReturnConflict()
    {
        // Arrange - Try to change to another seeded user's email
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var currentUser = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var otherUserEmail = TestConstants.TestUsers.SeededUser2Email;

        var updateRequest = new UpdateUserRequest(
            Name: currentUser!.Name,
            Email: otherUserEmail, // Another user's email
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: currentUser.InitialBalance);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "email", false)] // Empty name
    [InlineData("name", "", false)] // Empty email
    [InlineData("name", "invalid-email", false)] // Invalid email format
    public async Task UpdateProfile_WithInvalidData_ShouldReturnBadRequest(
        string name, string email, bool useValidEmail)
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);

        var updateRequest = new UpdateUserRequest(
            Name: name,
            Email: useValidEmail ? user!.Email : email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyCurrentPassword_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);

        var updateRequest = new UpdateUserRequest(
            Name: user!.Name,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: "", // Empty current password
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithWeakNewPassword_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);

        var updateRequest = new UpdateUserRequest(
            Name: user!.Name,
            Email: user.Email,
            NewPassword: "weak", // Too weak
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: 0m);

        // Act
        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_OnlyName_ShouldKeepOtherFieldsUnchanged()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var originalName = user!.Name;
        var newName = $"Only Name Changed {Guid.NewGuid().ToString("N")[..8]}";

        // Act - Update only name
        var updateRequest = new UpdateUserRequest(
            Name: newName,
            Email: user.Email,
            NewPassword: null, // No password change
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);

        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateResponse = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        updateResponse!.Name.Should().Be(newName);
        updateResponse.Email.Should().Be(user.Email);

        // Verify login still works with same password
        var loginRequest = new LoginRequest(user.Email, TestConstants.TestUsers.SeededUserPassword);
        var loginResponse = await Client.PostAsJsonAsync(TestConstants.Routes.AuthLogin, loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Restore original name
        var restoreRequest = new UpdateUserRequest(
            Name: originalName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    #endregion

    #region Delete Profile Tests

    [Fact]
    public async Task DeleteProfile_WithoutConfirmation_ShouldReturnBadRequest()
    {
        // Arrange
        var deleteRequest = new DeleteUserRequest(
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            ConfirmDeletion: false); // No confirmation

        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.Routes.UserProfile)
        {
            Content = JsonContent.Create(deleteRequest)
        };

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteProfile_WithWrongPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var deleteRequest = new DeleteUserRequest(
            CurrentPassword: "WrongP@ssw0rd!",
            ConfirmDeletion: true);

        var request = new HttpRequestMessage(HttpMethod.Delete, TestConstants.Routes.UserProfile)
        {
            Content = JsonContent.Create(deleteRequest)
        };

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Note: We don't test successful deletion of the seeded user as it would affect other tests
    // E2E tests in UserWorkflows cover the full deletion flow with freshly registered users

    #endregion

    #region Edge Cases

    [Fact]
    public async Task UpdateProfile_WithSameEmail_ShouldSucceed()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var originalName = user!.Name;
        var newName = $"Same Email User {Guid.NewGuid().ToString("N")[..8]}";

        // Act - Update with same email (just changing name)
        var updateRequest = new UpdateUserRequest(
            Name: newName,
            Email: user.Email, // Same email
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);

        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateResponse = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        updateResponse!.Name.Should().Be(newName);
        updateResponse.Email.Should().Be(user.Email);

        // Restore original name
        var restoreRequest = new UpdateUserRequest(
            Name: originalName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithSpecialCharactersInName_ShouldSucceed()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var originalName = user!.Name;

        // Act
        var specialName = "José María O'Brien-Smith Ñoño";
        var updateRequest = new UpdateUserRequest(
            Name: specialName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);

        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateResponse = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        updateResponse!.Name.Should().Be(specialName);

        // Restore original name
        var restoreRequest = new UpdateUserRequest(
            Name: originalName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithMaxLengthName_ShouldSucceed()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var originalName = user!.Name;

        // Act
        var maxLengthName = new string('A', 100);
        var updateRequest = new UpdateUserRequest(
            Name: maxLengthName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);

        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateResponse = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        updateResponse!.Name.Should().Be(maxLengthName);

        // Restore original name
        var restoreRequest = new UpdateUserRequest(
            Name: originalName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithExceedingMaxLengthName_ShouldReturnBadRequest()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);

        // Act
        var tooLongName = new string('A', 101);
        var updateRequest = new UpdateUserRequest(
            Name: tooLongName,
            Email: user!.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: user.InitialBalance);

        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_MultipleTimesSequentially_ShouldAllSucceed()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var originalName = user!.Name;
        var originalBalance = user.InitialBalance;

        // Act & Assert - Update multiple times
        for (int i = 1; i <= 5; i++)
        {
            var updateRequest = new UpdateUserRequest(
                Name: $"Update #{i}",
                Email: user.Email,
                NewPassword: null,
                CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
                InitialBalance: i * 100.00m);

            var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updateResponse = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
            updateResponse!.Name.Should().Be($"Update #{i}");
            updateResponse.InitialBalance.Should().Be(i * 100.00m);
        }

        // Restore original values
        var restoreRequest = new UpdateUserRequest(
            Name: originalName,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: originalBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    [Fact]
    public async Task UpdateProfile_UpdateInitialBalance_ShouldPersist()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == TestAuthHandler.DefaultUserId);
        var originalBalance = user!.InitialBalance;
        var newBalance = 9999.99m;

        // Act
        var updateRequest = new UpdateUserRequest(
            Name: user.Name,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: newBalance);

        var response = await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updateResponse = await response.Content.ReadFromJsonAsync<UpdateUserResponse>();
        updateResponse!.InitialBalance.Should().Be(newBalance);

        // Restore original balance
        var restoreRequest = new UpdateUserRequest(
            Name: user.Name,
            Email: user.Email,
            NewPassword: null,
            CurrentPassword: TestConstants.TestUsers.SeededUserPassword,
            InitialBalance: originalBalance);
        await Client.PutAsJsonAsync(TestConstants.Routes.UserProfile, restoreRequest);
    }

    #endregion
}
