using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;

namespace SampleCkWebApp.Application.Transactions;

/// <summary>
/// Validates transaction-related requests.
/// </summary>
public static class TransactionValidator
{
    public static ErrorOr<Success> ValidateCreateTransaction(
        TransactionType transactionType,
        decimal amount,
        DateTime date,
        string subject,
        int? categoryId)
    {
        var errors = new List<Error>();
        
        // Subject is required and cannot be empty
        if (string.IsNullOrWhiteSpace(subject))
        {
            errors.Add(TransactionErrors.InvalidSubject);
        }
        
        // Amount must be positive
        if (amount <= 0)
        {
            errors.Add(TransactionErrors.InvalidAmount);
        }
        
        // Date cannot be too far in the future (allow 1 day for timezone differences)
        if (date.Date > DateTime.UtcNow.Date.AddDays(1))
        {
            errors.Add(TransactionErrors.InvalidDate);
        }
        
        // Expenses must have a category
        if (transactionType == TransactionType.Expense && categoryId == null)
        {
            errors.Add(TransactionErrors.ExpenseMissingCategory);
        }
        
        if (errors.Count > 0)
        {
            return errors;
        }
        
        return Result.Success;
    }
    
    public static ErrorOr<TransactionType> ParseTransactionType(string typeString)
    {
        return typeString.ToUpperInvariant() switch
        {
            "EXPENSE" => TransactionType.Expense,
            "INCOME" => TransactionType.Income,
            _ => TransactionErrors.InvalidTransactionType
        };
    }
}

