using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Application.Transactions.Data;
using ExpenseTrackerAPI.Contracts.Transactions;

namespace ExpenseTrackerAPI.Application.Transactions.Mappings;

public static class TransactionMappings
{
    public static GetTransactionsResponse ToResponse(this GetTransactionsResult result)
    {
        return new GetTransactionsResponse
        {
            Transactions = result.Transactions.Select(t => t.ToResponse()).ToList(),
            TotalCount = result.Transactions.Count,
            Summary = new TransactionSummary
            {
                TotalIncome = result.TotalIncome,
                TotalExpenses = result.TotalExpenses,
                NetChange = result.NetChange,
                IncomeCount = result.IncomeCount,
                ExpenseCount = result.ExpenseCount
            }
        };
    }

    public static TransactionResponse ToResponse(this Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            TransactionType = transaction.TransactionType.ToDatabaseString(),
            Amount = transaction.Amount,
            SignedAmount = transaction.SignedAmount,
            CumulativeDelta = transaction.CumulativeDelta,
            Date = transaction.Date,
            Subject = transaction.Subject,
            Notes = transaction.Notes,
            PaymentMethod = transaction.PaymentMethod,
            CategoryId = transaction.CategoryId,
            TransactionGroupId = transaction.TransactionGroupId,
            IncomeSource = transaction.IncomeSource,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }

    public static string ToDatabaseString(this TransactionType type)
    {
        return type switch
        {
            TransactionType.EXPENSE => "EXPENSE",
            TransactionType.INCOME => "INCOME",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    public static TransactionType FromDatabaseString(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "EXPENSE" => TransactionType.EXPENSE,
            "INCOME" => TransactionType.INCOME,
            _ => throw new ArgumentException($"Unknown transaction type: {value}")
        };
    }
}

