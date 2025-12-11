using ErrorOr;
using SampleCkWebApp.Domain.Errors;

namespace SampleCkWebApp.Application.Users;

public static class UserValidator
{
    public static ErrorOr<Success> ValidateCreateUserRequest(string name, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return UserErrors.InvalidName;
        }

        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
        {
            return UserErrors.InvalidEmail;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return UserErrors.InvalidPassword;
        }

        return new Success();
    }
    
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

