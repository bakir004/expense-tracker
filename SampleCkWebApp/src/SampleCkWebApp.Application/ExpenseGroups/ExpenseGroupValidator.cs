using ErrorOr;
using SampleCkWebApp.Domain.Errors;

namespace SampleCkWebApp.Application.ExpenseGroups;

public static class ExpenseGroupValidator
{
    public static ErrorOr<Success> ValidateExpenseGroupRequest(string name, string? description, int userId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
        {
            return ExpenseGroupErrors.InvalidName;
        }

        if (userId <= 0)
        {
            return ExpenseGroupErrors.InvalidUserId;
        }

        return Result.Success;
    }
}

