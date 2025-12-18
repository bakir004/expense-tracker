using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Transactions.Data;
using SampleCkWebApp.Application.Transactions.Interfaces.Application;
using SampleCkWebApp.Application.Transactions.Interfaces.Infrastructure;
using SampleCkWebApp.Application.Users.Interfaces.Infrastructure;
using SampleCkWebApp.Application.Categories.Interfaces.Infrastructure;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.Transactions;

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
        // Verify the user exists first
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }
        
        // User exists, get their transactions (may be empty)
        var result = await _transactionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        
        return BuildResult(result.Value);
    }

    public async Task<ErrorOr<GetTransactionsResult>> GetByUserIdAndTypeAsync(int userId, TransactionType type, CancellationToken cancellationToken)
    {
        // Verify the user exists first
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }
        
        // User exists, get their transactions filtered by type (may be empty)
        var result = await _transactionRepository.GetByUserIdAndTypeAsync(userId, type, cancellationToken);
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
        // Validate
        var validationResult = TransactionValidator.ValidateCreateTransaction(transactionType, amount, date, subject, categoryId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        // Verify the user exists
        var userResult = await _userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return UserErrors.NotFound;
        }
        
        // Verify the category exists (if provided)
        if (categoryId.HasValue)
        {
            var categoryResult = await _categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
            if (categoryResult.IsError)
            {
                return CategoryErrors.NotFound;
            }
        }
        
        // Verify the transaction group exists (if provided)
        if (transactionGroupId.HasValue)
        {
            var transactionGroupResult = await _transactionGroupRepository.GetByIdAsync(transactionGroupId.Value, cancellationToken);
            if (transactionGroupResult.IsError)
            {
                return TransactionGroupErrors.NotFound;
            }
        }
        
        // Calculate signed amount
        var signedAmount = transactionType == TransactionType.Expense ? -amount : amount;
        
        // Create domain entity
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
        // Check if transaction exists
        var existingResult = await _transactionRepository.GetByIdAsync(id, cancellationToken);
        if (existingResult.IsError)
        {
            return existingResult.Errors;
        }
        
        // Validate
        var validationResult = TransactionValidator.ValidateCreateTransaction(transactionType, amount, date, subject, categoryId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        // Verify the category exists (if provided)
        if (categoryId.HasValue)
        {
            var categoryResult = await _categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
            if (categoryResult.IsError)
            {
                return CategoryErrors.NotFound;
            }
        }
        
        // Verify the transaction group exists (if provided)
        if (transactionGroupId.HasValue)
        {
            var transactionGroupResult = await _transactionGroupRepository.GetByIdAsync(transactionGroupId.Value, cancellationToken);
            if (transactionGroupResult.IsError)
            {
                return TransactionGroupErrors.NotFound;
            }
        }
        
        // Calculate signed amount
        var signedAmount = transactionType == TransactionType.Expense ? -amount : amount;
        
        // Update domain entity
        var transaction = new Transaction
        {
            Id = id,
            UserId = existingResult.Value.UserId,  // Preserve original user
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

