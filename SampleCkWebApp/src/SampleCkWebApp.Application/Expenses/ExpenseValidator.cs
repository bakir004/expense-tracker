using ErrorOr;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Domain.Entities;

namespace SampleCkWebApp.Application.Expenses;

public static class ExpenseValidator
{
    public static ErrorOr<Success> ValidateExpenseRequest(decimal amount, DateTime date, int categoryId, int userId)
    {
        if (amount <= 0)
        {
            return ExpenseErrors.InvalidAmount;
        }

        if (date == default)
        {
            return ExpenseErrors.InvalidDate;
        }

        if (categoryId <= 0)
        {
            return ExpenseErrors.InvalidCategoryId;
        }

        if (userId <= 0)
        {
            return ExpenseErrors.InvalidUserId;
        }

        return Result.Success;
    }
}

