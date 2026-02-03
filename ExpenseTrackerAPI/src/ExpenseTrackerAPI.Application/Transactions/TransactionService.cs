using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;
using ExpenseTrackerAPI.Application.Transactions.Data;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Application;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Users.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.Categories.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Application.TransactionGroups.Interfaces.Infrastructure;

namespace ExpenseTrackerAPI.Application.Transactions;

/// <summary>
/// Application service for transaction operations.
/// Orchestrates domain logic and coordinates with the repository.
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionGroupRepository _transactionGroupRepository;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IUserRepository userRepository,
        ICategoryRepository categoryRepository,
        ITransactionGroupRepository transactionGroupRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _transactionGroupRepository = transactionGroupRepository ?? throw new ArgumentNullException(nameof(transactionGroupRepository));
    }

    public async Task<ErrorOr<GetTransactionsResult>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _transactionRepository.GetAllAsync(cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildResult(result.Value);
    }

    public async Task<ErrorOr<Transaction>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _transactionRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<ErrorOr<GetTransactionsResult>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }

        var result = await _transactionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildResult(result.Value);
    }

    public async Task<ErrorOr<GetTransactionsResult>> GetByUserIdWithFiltersAsync(int userId, TransactionQueryOptions options, CancellationToken cancellationToken)
    {
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }

        var result = await _transactionRepository.GetByUserIdWithFiltersAsync(userId, options, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildResult(result.Value);
    }

    public async Task<ErrorOr<GetTransactionsResult>> GetByUserIdAndTypeAsync(int userId, TransactionType type, CancellationToken cancellationToken)
    {
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }

        var result = await _transactionRepository.GetByUserIdAndTypeAsync(userId, type, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildResult(result.Value);
    }

    public async Task<ErrorOr<GetTransactionsResult>> GetByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }

        var result = await _transactionRepository.GetByUserIdAndDateRangeAsync(userId, startDate, endDate, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }

        return BuildResult(result.Value);
    }

    public async Task<ErrorOr<Transaction>> CreateAsync(
        int userId,
        TransactionType transactionType,
        decimal amount,
        DateTime date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int? categoryId,
        int? transactionGroupId,
        string? incomeSource,
        CancellationToken cancellationToken)
    {
        var validationResult = TransactionValidator.ValidateCreateTransaction(transactionType, amount, date, subject, categoryId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }

        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }

        if (categoryId.HasValue)
        {
            var categoryResult = await _categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
            if (categoryResult.IsError)
            {
                return CategoryErrors.NotFound;
            }
        }

        if (transactionGroupId.HasValue)
        {
            var transactionGroupResult = await _transactionGroupRepository.GetByIdAsync(transactionGroupId.Value, cancellationToken);
            if (transactionGroupResult.IsError)
            {
                return TransactionGroupErrors.NotFound;
            }
        }

        var signedAmount = transactionType == TransactionType.Expense ? -amount : amount;

        var transaction = new Transaction
        {
            UserId = userId,
            TransactionType = transactionType,
            Amount = amount,
            SignedAmount = signedAmount,
            Date = date,
            Subject = subject,
            Notes = notes,
            PaymentMethod = paymentMethod,
            CategoryId = categoryId,
            TransactionGroupId = transactionGroupId,
            IncomeSource = transactionType == TransactionType.Income ? incomeSource : null
        };

        return await _transactionRepository.CreateAsync(transaction, cancellationToken);
    }

    public async Task<ErrorOr<Transaction>> UpdateAsync(
        int id,
        TransactionType transactionType,
        decimal amount,
        DateTime date,
        string subject,
        string? notes,
        PaymentMethod paymentMethod,
        int? categoryId,
        int? transactionGroupId,
        string? incomeSource,
        CancellationToken cancellationToken)
    {
        var existingResult = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }

        var validationResult = TransactionValidator.ValidateCreateTransaction(transactionType, amount, date, subject, categoryId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }

        if (categoryId.HasValue)
        {
            var categoryResult = await _categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
            if (categoryResult.IsError)
            {
                return CategoryErrors.NotFound;
            }
        }

        if (transactionGroupId.HasValue)
        {
            var transactionGroupResult = await _transactionGroupRepository.GetByIdAsync(transactionGroupId.Value, cancellationToken);
            if (transactionGroupResult.IsError)
            {
                return TransactionGroupErrors.NotFound;
            }
        }

        var signedAmount = transactionType == TransactionType.Expense ? -amount : amount;

        var transaction = new Transaction
        {
            Id = id,
            UserId = existingResult.Value.UserId,
            TransactionType = transactionType,
            Amount = amount,
            SignedAmount = signedAmount,
            Date = date,
            Subject = subject,
            Notes = notes,
            PaymentMethod = paymentMethod,
            CategoryId = categoryId,
            TransactionGroupId = transactionGroupId,
            IncomeSource = transactionType == TransactionType.Income ? incomeSource : null
        };

        return await _transactionRepository.UpdateAsync(transaction, cancellationToken);
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return await _transactionRepository.DeleteAsync(id, cancellationToken);
    }

    private static GetTransactionsResult BuildResult(List<Transaction> transactions)
    {
        var totalIncome = transactions
            .Where(t => t.TransactionType == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpenses = transactions
            .Where(t => t.TransactionType == TransactionType.Expense)
            .Sum(t => t.Amount);

        return new GetTransactionsResult
        {
            Transactions = transactions,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            NetChange = totalIncome - totalExpenses,
            IncomeCount = transactions.Count(t => t.TransactionType == TransactionType.Income),
            ExpenseCount = transactions.Count(t => t.TransactionType == TransactionType.Expense)
        };
    }
}

