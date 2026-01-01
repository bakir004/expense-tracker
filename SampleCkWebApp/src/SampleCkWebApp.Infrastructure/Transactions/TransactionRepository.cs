using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Transactions.Interfaces.Infrastructure;
using SampleCkWebApp.Application.Transactions.Mappings;
using SampleCkWebApp.Infrastructure.Shared;

namespace SampleCkWebApp.Infrastructure.Transactions;

/// <summary>
/// Hybrid implementation of the transaction repository using both EF Core and raw SQL.
/// 
/// SIMPLIFIED DESIGN:
/// - Each transaction stores cumulative_delta (running sum of signed_amounts)
/// - User.initial_balance + cumulative_delta = actual balance
/// - No separate UserBalance or UserBalanceHistory tables needed
/// 
/// IMPLEMENTATION APPROACH:
/// - Uses EF Core for simple read queries (GetAll, GetById, etc.)
/// - Uses raw SQL for complex write operations with cumulative balance calculations
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly ExpenseTrackerDbContext _context;

    public TransactionRepository(ExpenseTrackerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<List<Transaction>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            if (transaction == null)
            {
                return TransactionErrors.NotFound;
            }

            return transaction;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdAndTypeAsync(int userId, TransactionType type, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.TransactionType == type)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Transaction>>> GetByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync(cancellationToken);

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    // ========================================================================
    // COMPLEX OPERATIONS USING RAW SQL
    // These operations require precise cumulative balance calculations
    // and atomic updates of multiple transactions
    // ========================================================================

    public async Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Get the last cumulative_delta for this user (or 0 if no transactions)
                decimal previousCumulativeDelta = 0;
                await using (var getLastCommand = connection.CreateCommand())
                {
                    getLastCommand.Transaction = dbTransaction;
                    getLastCommand.CommandText = @"SELECT cumulative_delta FROM Transaction 
                                                   WHERE user_id = @user_id ORDER BY date DESC, id DESC LIMIT 1";
                    var param = getLastCommand.CreateParameter();
                    param.ParameterName = "@user_id";
                    param.Value = transaction.UserId;
                    getLastCommand.Parameters.Add(param);

                    var result = await getLastCommand.ExecuteScalarAsync(cancellationToken);
                    if (result != null && result != DBNull.Value)
                    {
                        previousCumulativeDelta = (decimal)result;
                    }
                }
                
                // Calculate new cumulative_delta
                var newCumulativeDelta = previousCumulativeDelta + transaction.SignedAmount;
                
                // Insert the transaction
                await using var command = connection.CreateCommand();
                command.Transaction = dbTransaction;
                command.CommandText = @"
                    INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta,
                                           date, subject, notes, payment_method, category_id, transaction_group_id, 
                                           income_source, created_at, updated_at) 
                    VALUES (@user_id, @transaction_type, @amount, @signed_amount, @cumulative_delta,
                            @date, @subject, @notes, @payment_method, @category_id, @transaction_group_id, 
                            @income_source, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
                    RETURNING id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
                              date::timestamp, subject, notes, payment_method, category_id, 
                              transaction_group_id, income_source, created_at, updated_at";

                AddParameter(command, "@user_id", transaction.UserId);
                AddParameter(command, "@transaction_type", transaction.TransactionType.ToDatabaseString());
                AddParameter(command, "@amount", transaction.Amount);
                AddParameter(command, "@signed_amount", transaction.SignedAmount);
                AddParameter(command, "@cumulative_delta", newCumulativeDelta);
                AddParameter(command, "@date", transaction.Date);
                AddParameter(command, "@subject", transaction.Subject);
                AddParameter(command, "@notes", (object?)transaction.Notes ?? DBNull.Value);
                AddParameter(command, "@payment_method", PaymentMethodHelper.ToDatabaseString(transaction.PaymentMethod));
                AddParameter(command, "@category_id", (object?)transaction.CategoryId ?? DBNull.Value);
                AddParameter(command, "@transaction_group_id", (object?)transaction.TransactionGroupId ?? DBNull.Value);
                AddParameter(command, "@income_source", (object?)transaction.IncomeSource ?? DBNull.Value);

                Transaction createdTransaction;
                decimal initialBalance = 0;
                
                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return Error.Failure("Database.Error", "Failed to create transaction");
                    }
                    createdTransaction = MapFromReader(reader);
                }
                
                // Get initial_balance to compute BalanceAfter
                await using (var getInitialCommand = connection.CreateCommand())
                {
                    getInitialCommand.Transaction = dbTransaction;
                    getInitialCommand.CommandText = @"SELECT initial_balance FROM ""Users"" WHERE id = @user_id";
                    var param = getInitialCommand.CreateParameter();
                    param.ParameterName = "@user_id";
                    param.Value = transaction.UserId;
                    getInitialCommand.Parameters.Add(param);

                    var result = await getInitialCommand.ExecuteScalarAsync(cancellationToken);
                    if (result != null && result != DBNull.Value)
                    {
                        initialBalance = (decimal)result;
                    }
                }
                
                createdTransaction.BalanceAfter = initialBalance + createdTransaction.CumulativeDelta;
                
                await dbTransaction.CommitAsync(cancellationToken);
                return createdTransaction;
            }
            catch
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create transaction: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> UpdateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Get the old transaction
                Transaction oldTransaction;
                await using (var getCommand = connection.CreateCommand())
                {
                    getCommand.Transaction = dbTransaction;
                    getCommand.CommandText = @"
                        SELECT id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
                               date::timestamp, subject, notes, payment_method, category_id, 
                               transaction_group_id, income_source, created_at, updated_at
                        FROM Transaction WHERE id = @id FOR UPDATE";
                    var param = getCommand.CreateParameter();
                    param.ParameterName = "@id";
                    param.Value = transaction.Id;
                    getCommand.Parameters.Add(param);

                    await using var reader = await getCommand.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return TransactionErrors.NotFound;
                    }
                    oldTransaction = MapFromReader(reader);
                }
                
                // Calculate the difference in signed_amount
                var signedAmountDiff = transaction.SignedAmount - oldTransaction.SignedAmount;
                var newCumulativeDelta = oldTransaction.CumulativeDelta + signedAmountDiff;
                
                // Update the transaction
                Transaction updatedTransaction;
                await using (var updateCommand = connection.CreateCommand())
                {
                    updateCommand.Transaction = dbTransaction;
                    updateCommand.CommandText = @"
                        UPDATE Transaction 
                        SET transaction_type = @transaction_type, amount = @amount, signed_amount = @signed_amount,
                            cumulative_delta = @cumulative_delta, date = @date, subject = @subject, notes = @notes, 
                            payment_method = @payment_method, category_id = @category_id, 
                            transaction_group_id = @transaction_group_id, income_source = @income_source,
                            updated_at = CURRENT_TIMESTAMP
                        WHERE id = @id 
                        RETURNING id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
                                  date::timestamp, subject, notes, payment_method, category_id, 
                                  transaction_group_id, income_source, created_at, updated_at";

                    AddParameter(updateCommand, "@id", transaction.Id);
                    AddParameter(updateCommand, "@transaction_type", transaction.TransactionType.ToDatabaseString());
                    AddParameter(updateCommand, "@amount", transaction.Amount);
                    AddParameter(updateCommand, "@signed_amount", transaction.SignedAmount);
                    AddParameter(updateCommand, "@cumulative_delta", newCumulativeDelta);
                    AddParameter(updateCommand, "@date", transaction.Date);
                    AddParameter(updateCommand, "@subject", transaction.Subject);
                    AddParameter(updateCommand, "@notes", (object?)transaction.Notes ?? DBNull.Value);
                    AddParameter(updateCommand, "@payment_method", PaymentMethodHelper.ToDatabaseString(transaction.PaymentMethod));
                    AddParameter(updateCommand, "@category_id", (object?)transaction.CategoryId ?? DBNull.Value);
                    AddParameter(updateCommand, "@transaction_group_id", (object?)transaction.TransactionGroupId ?? DBNull.Value);
                    AddParameter(updateCommand, "@income_source", (object?)transaction.IncomeSource ?? DBNull.Value);

                    await using var reader = await updateCommand.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return TransactionErrors.NotFound;
                    }
                    updatedTransaction = MapFromReader(reader);
                }
                
                // Update all subsequent transactions' cumulative_delta (single statement!)
                if (signedAmountDiff != 0)
                {
                    await using var updateSubsequentCommand = connection.CreateCommand();
                    updateSubsequentCommand.Transaction = dbTransaction;
                    updateSubsequentCommand.CommandText = @"
                        UPDATE Transaction 
                        SET cumulative_delta = cumulative_delta + @diff, updated_at = CURRENT_TIMESTAMP
                        WHERE user_id = @user_id AND (date > @date OR (date = @date AND id > @id))";

                    AddParameter(updateSubsequentCommand, "@diff", signedAmountDiff);
                    AddParameter(updateSubsequentCommand, "@user_id", oldTransaction.UserId);
                    AddParameter(updateSubsequentCommand, "@date", oldTransaction.Date);
                    AddParameter(updateSubsequentCommand, "@id", oldTransaction.Id);

                    await updateSubsequentCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                // Get initial_balance to compute BalanceAfter
                decimal initialBalance = 0;
                await using (var getInitialCommand = connection.CreateCommand())
                {
                    getInitialCommand.Transaction = dbTransaction;
                    getInitialCommand.CommandText = @"SELECT initial_balance FROM ""Users"" WHERE id = @user_id";
                    var param = getInitialCommand.CreateParameter();
                    param.ParameterName = "@user_id";
                    param.Value = oldTransaction.UserId;
                    getInitialCommand.Parameters.Add(param);

                    var result = await getInitialCommand.ExecuteScalarAsync(cancellationToken);
                    if (result != null && result != DBNull.Value)
                    {
                        initialBalance = (decimal)result;
                    }
                }
                
                updatedTransaction.BalanceAfter = initialBalance + updatedTransaction.CumulativeDelta;
                
                await dbTransaction.CommitAsync(cancellationToken);
                return updatedTransaction;
            }
            catch
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update transaction: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // First, check if transaction exists and get the data we need
                int userId;
                DateTime date;
                decimal signedAmount;
                
                await using (var checkCommand = connection.CreateCommand())
                {
                    checkCommand.Transaction = dbTransaction;
                    checkCommand.CommandText = @"SELECT user_id, date, signed_amount FROM Transaction WHERE id = @id";
                    var param = checkCommand.CreateParameter();
                    param.ParameterName = "@id";
                    param.Value = id;
                    checkCommand.Parameters.Add(param);

                    await using var reader = await checkCommand.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return TransactionErrors.NotFound;
                    }
                    userId = reader.GetInt32(0);
                    date = reader.GetDateTime(1);
                    signedAmount = reader.GetDecimal(2);
                }
                
                // Update all subsequent transactions' cumulative_delta (subtract the deleted amount)
                await using (var updateSubsequentCommand = connection.CreateCommand())
                {
                    updateSubsequentCommand.Transaction = dbTransaction;
                    updateSubsequentCommand.CommandText = @"
                        UPDATE Transaction 
                        SET cumulative_delta = cumulative_delta - @signed_amount, updated_at = CURRENT_TIMESTAMP
                        WHERE user_id = @user_id AND (date > @date OR (date = @date AND id > @id))";

                    AddParameter(updateSubsequentCommand, "@signed_amount", signedAmount);
                    AddParameter(updateSubsequentCommand, "@user_id", userId);
                    AddParameter(updateSubsequentCommand, "@date", date);
                    AddParameter(updateSubsequentCommand, "@id", id);

                    await updateSubsequentCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                // Delete the transaction
                await using (var deleteCommand = connection.CreateCommand())
                {
                    deleteCommand.Transaction = dbTransaction;
                    deleteCommand.CommandText = "DELETE FROM Transaction WHERE id = @id";
                    var param = deleteCommand.CreateParameter();
                    param.ParameterName = "@id";
                    param.Value = id;
                    deleteCommand.Parameters.Add(param);

                    await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                await dbTransaction.CommitAsync(cancellationToken);
                return Result.Deleted;
            }
            catch
            {
                await dbTransaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete transaction: {ex.Message}");
        }
    }

    // ========================================================================
    // HELPER METHODS
    // ========================================================================

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Maps a database reader row to a domain entity (without initial_balance - BalanceAfter will be null).
    /// Column order: id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
    ///               date, subject, notes, payment_method, category_id, transaction_group_id, 
    ///               income_source, created_at, updated_at
    /// </summary>
    private static Transaction MapFromReader(System.Data.Common.DbDataReader reader)
    {
        return new Transaction
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            TransactionType = TransactionMappings.FromDatabaseString(reader.GetString(2)),
            Amount = reader.GetDecimal(3),
            SignedAmount = reader.GetDecimal(4),
            CumulativeDelta = reader.GetDecimal(5),
            Date = reader.GetDateTime(6),
            Subject = reader.GetString(7),
            Notes = reader.IsDBNull(8) ? null : reader.GetString(8),
            PaymentMethod = PaymentMethodHelper.FromDatabaseString(reader.GetString(9)),
            CategoryId = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            TransactionGroupId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            IncomeSource = reader.IsDBNull(12) ? null : reader.GetString(12),
            CreatedAt = reader.GetDateTime(13),
            UpdatedAt = reader.GetDateTime(14),
            BalanceAfter = null  // Will be computed later
        };
    }
}
