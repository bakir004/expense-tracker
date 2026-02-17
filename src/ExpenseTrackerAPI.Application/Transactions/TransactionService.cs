using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Contracts.Transactions;

namespace ExpenseTrackerAPI.Application.Transactions;

/// <summary>
/// Application service for transaction operations.
/// Orchestrates domain logic and coordinates with the repository.
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IUserRepository userRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<ErrorOr<Transaction>> GetByIdAsync(int id, int userId, CancellationToken cancellationToken)
    {
        return await _transactionRepository.GetByIdAsync(id, userId, cancellationToken);
    }

    public async Task<ErrorOr<Transaction>> CreateAsync(
        int userId,
        TransactionType transactionType,
        decimal amount,
        DateOnly date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int? categoryId,
        int? transactionGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            var transaction = new Transaction(
                userId: userId,
                transactionType: transactionType,
                amount: amount,
                date: date,
                subject: subject,
                paymentMethod: paymentMethod,
                notes: notes,
                categoryId: categoryId,
                transactionGroupId: transactionGroupId);

            return await _transactionRepository.CreateAsync(transaction, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return TransactionErrors.ValidationError(ex.Message);
        }
    }

    public async Task<ErrorOr<Transaction>> UpdateAsync(
        int id,
        int userId,
        TransactionType transactionType,
        decimal amount,
        DateOnly date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int? categoryId,
        int? transactionGroupId,
        CancellationToken cancellationToken)
    {
        try
        {
            var oldTransaction = await _transactionRepository.GetByIdAsync(id, userId, cancellationToken);
            if (oldTransaction.IsError)
            {
                return oldTransaction.Errors;
            }

            var existingTransaction = oldTransaction.Value;

            if (userId != existingTransaction.UserId)
                return TransactionErrors.Unauthorized;

            var updatedTransaction = new Transaction(
                userId: existingTransaction.UserId,
                transactionType: transactionType,
                amount: amount,
                date: date,
                subject: subject,
                paymentMethod: paymentMethod,
                notes: notes,
                categoryId: categoryId,
                transactionGroupId: transactionGroupId);

            updatedTransaction.UpdateId(id);

            return await _transactionRepository.UpdateAsync(existingTransaction, updatedTransaction, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return TransactionErrors.ValidationError(ex.Message);
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, int userId, CancellationToken cancellationToken)
    {
        var existingTransaction = await _transactionRepository.GetByIdAsync(id, userId, cancellationToken);

        if (existingTransaction.IsError)
            return existingTransaction.Errors;

        var transactionOwnerId = existingTransaction.Value.UserId;

        if (transactionOwnerId != userId)
            return TransactionErrors.NotFound;

        return await _transactionRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task<ErrorOr<TransactionFilterResponse>> GetByUserIdWithFilterAsync(
        int userId,
        TransactionFilter filter,
        CancellationToken cancellationToken)
    {
        // Verify user exists
        var userResult = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }

        var result = await _transactionRepository.GetByUserIdWithFilterAsync(userId, filter, cancellationToken);

        if (result.IsError)
        {
            return result.Errors;
        }

        var (transactions, totalCount) = result.Value;

        return new TransactionFilterResponse
        {
            Transactions = transactions.Select(t => t.ToResponse()).ToList(),
            CurrentPage = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
        };
    }
}
