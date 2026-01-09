using ErrorOr;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.TransactionGroups;

public static class TransactionGroupValidator
{
    public static ErrorOr<Success> ValidateTransactionGroupRequest(string name, string? description, int userId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255)
        {
            return TransactionGroupErrors.InvalidName;
        }

        if (userId <= 0)
        {
            return TransactionGroupErrors.InvalidUserId;
        }

        return Result.Success;
    }
}

