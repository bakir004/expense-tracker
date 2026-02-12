using ErrorOr;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Transactions;

/// <summary>
/// Service implementation for transaction business operations.
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    public async Task<ErrorOr<GetAllTransactionsResponse>> GetAllTransactionsAsync(
        int userId,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // Validate input parameters
        if (userId <= 0)
            return UserErrors.InvalidUserId;

        if (pageNumber <= 0)
            return TransactionErrors.InvalidPageNumber;

        if (pageSize <= 0 || pageSize > 100)
            return TransactionErrors.InvalidPageSize;

        try
        {
            var result = await _transactionRepository.GetAllByUserIdAsync(userId, pageNumber, pageSize, cancellationToken);

            if (result.IsError)
                return result.Errors;

            var (transactions, totalCount) = result.Value;

            // Map domain entities to response DTOs
            var transactionResponses = transactions.Select(t => new TransactionResponse(
                Id: t.Id,
                UserId: t.UserId,
                TransactionType: t.TransactionType,
                Amount: t.Amount,
                SignedAmount: t.SignedAmount,
                Date: t.Date,
                Subject: t.Subject,
                Notes: t.Notes,
                PaymentMethod: t.PaymentMethod,
                CumulativeDelta: t.CumulativeDelta,
                CategoryId: t.CategoryId,
                TransactionGroupId: t.TransactionGroupId,
                CreatedAt: t.CreatedAt,
                UpdatedAt: t.UpdatedAt
            ));

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var hasNextPage = pageNumber < totalPages;
            var hasPreviousPage = pageNumber > 1;

            var response = new GetAllTransactionsResponse(
                Transactions: transactionResponses,
                TotalCount: totalCount,
                PageNumber: pageNumber,
                PageSize: pageSize,
                HasNextPage: hasNextPage,
                HasPreviousPage: hasPreviousPage
            );

            return response;
        }
        catch (Exception ex)
        {
            return Error.Failure("Transaction.GetAll.UnexpectedError", $"Failed to retrieve transactions: {ex.Message}");
        }
    }
}
