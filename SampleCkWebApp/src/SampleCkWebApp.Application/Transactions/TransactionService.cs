using ErrorOr;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Application.Transactions.Data;
using SampleCkWebApp.Application.Transactions.Interfaces.Application;
using SampleCkWebApp.Application.Transactions.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.Transactions;

/// <summary>
/// Application service for transaction operations.
/// Orchestrates domain logic and coordinates with the repository.
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionService(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
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
        var result = await _transactionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (result.IsError)
        {
            return result.Errors;
        }
        
        return BuildResult(result.Value);
    }

    public async Task<ErrorOr<GetTransactionsResult>> GetByUserIdAndTypeAsync(int userId, TransactionType type, CancellationToken cancellationToken)
    {
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
        var validationResult = TransactionValidator.ValidateCreateTransaction(transactionType, amount, date, categoryId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
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
        var validationResult = TransactionValidator.ValidateCreateTransaction(transactionType, amount, date, categoryId);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
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

