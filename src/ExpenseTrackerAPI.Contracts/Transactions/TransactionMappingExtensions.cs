using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Extension methods for mapping Transaction domain entities to contract DTOs.
/// </summary>
public static class TransactionMappingExtensions
{
    /// <summary>
    /// Maps a Transaction domain entity to a TransactionResponse DTO.
    /// </summary>
    /// <param name="transaction">The transaction entity to map</param>
    /// <returns>A TransactionResponse with string representations of enums</returns>
    public static TransactionResponse ToResponse(this Transaction transaction)
    {
        return new TransactionResponse(
            Id: transaction.Id,
            UserId: transaction.UserId,
            TransactionType: transaction.TransactionType.ToString(),
            Amount: transaction.Amount,
            SignedAmount: transaction.SignedAmount,
            Date: transaction.Date,
            Subject: transaction.Subject,
            Notes: transaction.Notes,
            PaymentMethod: transaction.PaymentMethod.ToString(),
            CumulativeDelta: transaction.CumulativeDelta,
            CategoryId: transaction.CategoryId,
            TransactionGroupId: transaction.TransactionGroupId,
            CreatedAt: transaction.CreatedAt,
            UpdatedAt: transaction.UpdatedAt);
    }
}
