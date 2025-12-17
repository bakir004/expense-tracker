using ErrorOr;
using SampleCkWebApp.Domain.Errors;

namespace SampleCkWebApp.Application.Incomes;

public static class IncomeValidator
{
    public static ErrorOr<Success> ValidateIncomeRequest(decimal amount, DateTime date, int userId)
    {
        if (amount <= 0)
        {
            return IncomeErrors.InvalidAmount;
        }

        if (date == default)
        {
            return IncomeErrors.InvalidDate;
        }

        if (userId <= 0)
        {
            return IncomeErrors.InvalidUserId;
        }

        return Result.Success;
    }
}

