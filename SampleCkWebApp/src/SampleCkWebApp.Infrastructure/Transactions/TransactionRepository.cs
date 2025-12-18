using ErrorOr;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Transactions.Interfaces.Infrastructure;
using SampleCkWebApp.Application.Transactions.Mappings;
using SampleCkWebApp.Infrastructure.Users.Options;
using SampleCkWebApp.Infrastructure.Shared;

namespace SampleCkWebApp.Infrastructure.Transactions;

/// <summary>
/// PostgreSQL implementation of the transaction repository.
/// 
/// SIMPLIFIED DESIGN:
/// - Each transaction stores cumulative_delta (running sum of signed_amounts)
/// - User.initial_balance + cumulative_delta = actual balance
/// - No separate UserBalance or UserBalanceHistory tables needed
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly UserOptions _options;

    public TransactionRepository(UserOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<List<Transaction>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"SELECT t.id, t.user_id, t.transaction_type, t.amount, t.signed_amount, t.cumulative_delta,
                         t.date::timestamp, t.subject, t.notes, t.payment_method, t.seq, t.category_id, 
                         t.transaction_group_id, t.income_source, t.created_at, t.updated_at,
                         u.initial_balance
                  FROM Transaction t
                  JOIN ""Users"" u ON t.user_id = u.id
                  ORDER BY t.date DESC, t.seq DESC",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var transactions = new List<Transaction>();

            while (await reader.ReadAsync(cancellationToken))
            {
                transactions.Add(MapToDomainEntity(reader));
            }

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
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"SELECT t.id, t.user_id, t.transaction_type, t.amount, t.signed_amount, t.cumulative_delta,
                         t.date::timestamp, t.subject, t.notes, t.payment_method, t.seq, t.category_id, 
                         t.transaction_group_id, t.income_source, t.created_at, t.updated_at,
                         u.initial_balance
                  FROM Transaction t
                  JOIN ""Users"" u ON t.user_id = u.id
                  WHERE t.id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return TransactionErrors.NotFound;
            }

            return MapToDomainEntity(reader);
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
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"SELECT t.id, t.user_id, t.transaction_type, t.amount, t.signed_amount, t.cumulative_delta,
                         t.date::timestamp, t.subject, t.notes, t.payment_method, t.seq, t.category_id, 
                         t.transaction_group_id, t.income_source, t.created_at, t.updated_at,
                         u.initial_balance
                  FROM Transaction t
                  JOIN ""Users"" u ON t.user_id = u.id
                  WHERE t.user_id = @user_id 
                  ORDER BY t.date DESC, t.seq DESC",
                connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var transactions = new List<Transaction>();

            while (await reader.ReadAsync(cancellationToken))
            {
                transactions.Add(MapToDomainEntity(reader));
            }

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
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"SELECT t.id, t.user_id, t.transaction_type, t.amount, t.signed_amount, t.cumulative_delta,
                         t.date::timestamp, t.subject, t.notes, t.payment_method, t.seq, t.category_id, 
                         t.transaction_group_id, t.income_source, t.created_at, t.updated_at,
                         u.initial_balance
                  FROM Transaction t
                  JOIN ""Users"" u ON t.user_id = u.id
                  WHERE t.user_id = @user_id AND t.transaction_type = @type 
                  ORDER BY t.date DESC, t.seq DESC",
                connection);
            command.Parameters.AddWithValue("user_id", userId);
            command.Parameters.AddWithValue("type", type.ToDatabaseString());

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var transactions = new List<Transaction>();

            while (await reader.ReadAsync(cancellationToken))
            {
                transactions.Add(MapToDomainEntity(reader));
            }

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
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"SELECT t.id, t.user_id, t.transaction_type, t.amount, t.signed_amount, t.cumulative_delta,
                         t.date::timestamp, t.subject, t.notes, t.payment_method, t.seq, t.category_id, 
                         t.transaction_group_id, t.income_source, t.created_at, t.updated_at,
                         u.initial_balance
                  FROM Transaction t
                  JOIN ""Users"" u ON t.user_id = u.id
                  WHERE t.user_id = @user_id AND t.date >= @start_date AND t.date <= @end_date 
                  ORDER BY t.date DESC, t.seq DESC",
                connection);
            command.Parameters.AddWithValue("user_id", userId);
            command.Parameters.AddWithValue("start_date", startDate);
            command.Parameters.AddWithValue("end_date", endDate);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var transactions = new List<Transaction>();

            while (await reader.ReadAsync(cancellationToken))
            {
                transactions.Add(MapToDomainEntity(reader));
            }

            return transactions;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transactions: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Transaction>> CreateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Get the last cumulative_delta for this user (or 0 if no transactions)
                decimal previousCumulativeDelta = 0;
                await using (var getLastCommand = new NpgsqlCommand(
                    @"SELECT cumulative_delta FROM Transaction 
                      WHERE user_id = @user_id ORDER BY seq DESC LIMIT 1",
                    connection, dbTransaction))
                {
                    getLastCommand.Parameters.AddWithValue("user_id", transaction.UserId);
                    var result = await getLastCommand.ExecuteScalarAsync(cancellationToken);
                    if (result != null && result != DBNull.Value)
                    {
                        previousCumulativeDelta = (decimal)result;
                    }
                }
                
                // Calculate new cumulative_delta
                var newCumulativeDelta = previousCumulativeDelta + transaction.SignedAmount;
                
                // Insert the transaction
                var command = new NpgsqlCommand(
                    @"INSERT INTO Transaction (user_id, transaction_type, amount, signed_amount, cumulative_delta,
                                               date, subject, notes, payment_method, category_id, transaction_group_id, 
                                               income_source, created_at, updated_at) 
                      VALUES (@user_id, @transaction_type, @amount, @signed_amount, @cumulative_delta,
                              @date, @subject, @notes, @payment_method, @category_id, @transaction_group_id, 
                              @income_source, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
                      RETURNING id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
                                date::timestamp, subject, notes, payment_method, seq, category_id, 
                                transaction_group_id, income_source, created_at, updated_at",
                    connection, dbTransaction);
                
                command.Parameters.AddWithValue("user_id", transaction.UserId);
                command.Parameters.AddWithValue("transaction_type", transaction.TransactionType.ToDatabaseString());
                command.Parameters.AddWithValue("amount", transaction.Amount);
                command.Parameters.AddWithValue("signed_amount", transaction.SignedAmount);
                command.Parameters.AddWithValue("cumulative_delta", newCumulativeDelta);
                command.Parameters.AddWithValue("date", transaction.Date);
                command.Parameters.AddWithValue("subject", transaction.Subject);
                command.Parameters.AddWithValue("notes", (object?)transaction.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("payment_method", PaymentMethodHelper.ToDatabaseString(transaction.PaymentMethod));
                command.Parameters.AddWithValue("category_id", (object?)transaction.CategoryId ?? DBNull.Value);
                command.Parameters.AddWithValue("transaction_group_id", (object?)transaction.TransactionGroupId ?? DBNull.Value);
                command.Parameters.AddWithValue("income_source", (object?)transaction.IncomeSource ?? DBNull.Value);

                Transaction createdTransaction;
                decimal initialBalance = 0;
                
                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return Error.Failure("Database.Error", "Failed to create transaction");
                    }
                    createdTransaction = MapToDomainEntityWithoutJoin(reader);
                }
                
                // Get initial_balance to compute BalanceAfter
                await using (var getInitialCommand = new NpgsqlCommand(
                    @"SELECT initial_balance FROM ""Users"" WHERE id = @user_id",
                    connection, dbTransaction))
                {
                    getInitialCommand.Parameters.AddWithValue("user_id", transaction.UserId);
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
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // Get the old transaction
                Transaction oldTransaction;
                await using (var getCommand = new NpgsqlCommand(
                    @"SELECT id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
                             date::timestamp, subject, notes, payment_method, seq, category_id, 
                             transaction_group_id, income_source, created_at, updated_at
                      FROM Transaction WHERE id = @id FOR UPDATE",
                    connection, dbTransaction))
                {
                    getCommand.Parameters.AddWithValue("id", transaction.Id);
                    await using var reader = await getCommand.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return TransactionErrors.NotFound;
                    }
                    oldTransaction = MapToDomainEntityWithoutJoin(reader);
                }
                
                // Calculate the difference in signed_amount
                var signedAmountDiff = transaction.SignedAmount - oldTransaction.SignedAmount;
                var newCumulativeDelta = oldTransaction.CumulativeDelta + signedAmountDiff;
                
                // Update the transaction
                await using var updateCommand = new NpgsqlCommand(
                    @"UPDATE Transaction 
                      SET transaction_type = @transaction_type, amount = @amount, signed_amount = @signed_amount,
                          cumulative_delta = @cumulative_delta, date = @date, subject = @subject, notes = @notes, 
                          payment_method = @payment_method, category_id = @category_id, 
                          transaction_group_id = @transaction_group_id, income_source = @income_source,
                          updated_at = CURRENT_TIMESTAMP
                      WHERE id = @id 
                      RETURNING id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
                                date::timestamp, subject, notes, payment_method, seq, category_id, 
                                transaction_group_id, income_source, created_at, updated_at",
                    connection, dbTransaction);
                
                updateCommand.Parameters.AddWithValue("id", transaction.Id);
                updateCommand.Parameters.AddWithValue("transaction_type", transaction.TransactionType.ToDatabaseString());
                updateCommand.Parameters.AddWithValue("amount", transaction.Amount);
                updateCommand.Parameters.AddWithValue("signed_amount", transaction.SignedAmount);
                updateCommand.Parameters.AddWithValue("cumulative_delta", newCumulativeDelta);
                updateCommand.Parameters.AddWithValue("date", transaction.Date);
                updateCommand.Parameters.AddWithValue("subject", transaction.Subject);
                updateCommand.Parameters.AddWithValue("notes", (object?)transaction.Notes ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("payment_method", PaymentMethodHelper.ToDatabaseString(transaction.PaymentMethod));
                updateCommand.Parameters.AddWithValue("category_id", (object?)transaction.CategoryId ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("transaction_group_id", (object?)transaction.TransactionGroupId ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("income_source", (object?)transaction.IncomeSource ?? DBNull.Value);

                Transaction updatedTransaction;
                await using (var reader = await updateCommand.ExecuteReaderAsync(cancellationToken))
                {
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return TransactionErrors.NotFound;
                    }
                    updatedTransaction = MapToDomainEntityWithoutJoin(reader);
                }
                
                // Update all subsequent transactions' cumulative_delta (single statement!)
                if (signedAmountDiff != 0)
                {
                    await using var updateSubsequentCommand = new NpgsqlCommand(
                        @"UPDATE Transaction 
                          SET cumulative_delta = cumulative_delta + @diff, updated_at = CURRENT_TIMESTAMP
                          WHERE user_id = @user_id AND seq > @seq",
                        connection, dbTransaction);
                    updateSubsequentCommand.Parameters.AddWithValue("diff", signedAmountDiff);
                    updateSubsequentCommand.Parameters.AddWithValue("user_id", oldTransaction.UserId);
                    updateSubsequentCommand.Parameters.AddWithValue("seq", oldTransaction.Seq);
                    await updateSubsequentCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                // Get initial_balance to compute BalanceAfter
                decimal initialBalance = 0;
                await using (var getInitialCommand = new NpgsqlCommand(
                    @"SELECT initial_balance FROM ""Users"" WHERE id = @user_id",
                    connection, dbTransaction))
                {
                    getInitialCommand.Parameters.AddWithValue("user_id", oldTransaction.UserId);
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
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                // First, check if transaction exists and get the data we need
                // Use separate scalar queries to avoid reader disposal issues
                int userId;
                long seq;
                decimal signedAmount;
                
                // Check existence and get user_id
                await using (var checkCommand = new NpgsqlCommand(
                    @"SELECT user_id FROM Transaction WHERE id = @id",
                    connection, dbTransaction))
                {
                    checkCommand.Parameters.AddWithValue("id", id);
                    var userIdResult = await checkCommand.ExecuteScalarAsync(cancellationToken);
                    if (userIdResult == null || userIdResult == DBNull.Value)
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return TransactionErrors.NotFound;
                    }
                    userId = (int)userIdResult;
                }
                
                // Get seq
                await using (var seqCommand = new NpgsqlCommand(
                    @"SELECT seq FROM Transaction WHERE id = @id",
                    connection, dbTransaction))
                {
                    seqCommand.Parameters.AddWithValue("id", id);
                    var seqResult = await seqCommand.ExecuteScalarAsync(cancellationToken);
                    if (seqResult == null || seqResult == DBNull.Value)
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return TransactionErrors.NotFound;
                    }
                    seq = (long)seqResult;
                }
                
                // Get signed_amount
                await using (var amountCommand = new NpgsqlCommand(
                    @"SELECT signed_amount FROM Transaction WHERE id = @id",
                    connection, dbTransaction))
                {
                    amountCommand.Parameters.AddWithValue("id", id);
                    var amountResult = await amountCommand.ExecuteScalarAsync(cancellationToken);
                    if (amountResult == null || amountResult == DBNull.Value)
                    {
                        await dbTransaction.RollbackAsync(cancellationToken);
                        return TransactionErrors.NotFound;
                    }
                    signedAmount = (decimal)amountResult;
                }
                
                // Update all subsequent transactions' cumulative_delta (subtract the deleted amount)
                await using var updateSubsequentCommand = new NpgsqlCommand(
                    @"UPDATE Transaction 
                      SET cumulative_delta = cumulative_delta - @signed_amount, updated_at = CURRENT_TIMESTAMP
                      WHERE user_id = @user_id AND seq > @seq",
                    connection, dbTransaction);
                updateSubsequentCommand.Parameters.AddWithValue("signed_amount", signedAmount);
                updateSubsequentCommand.Parameters.AddWithValue("user_id", userId);
                updateSubsequentCommand.Parameters.AddWithValue("seq", seq);
                await updateSubsequentCommand.ExecuteNonQueryAsync(cancellationToken);
                
                // Delete the transaction
                await using var deleteCommand = new NpgsqlCommand(
                    "DELETE FROM Transaction WHERE id = @id",
                    connection, dbTransaction);
                deleteCommand.Parameters.AddWithValue("id", id);
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
                
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

    /// <summary>
    /// Maps a database reader row to a domain entity (with JOIN to Users for initial_balance).
    /// Column order: id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
    ///               date, subject, notes, payment_method, seq, category_id, transaction_group_id, 
    ///               income_source, created_at, updated_at, initial_balance
    /// </summary>
    private static Transaction MapToDomainEntity(NpgsqlDataReader reader)
    {
        var cumulativeDelta = reader.GetDecimal(5);
        var initialBalance = reader.GetDecimal(16);
        
        return new Transaction
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            TransactionType = TransactionMappings.FromDatabaseString(reader.GetString(2)),
            Amount = reader.GetDecimal(3),
            SignedAmount = reader.GetDecimal(4),
            CumulativeDelta = cumulativeDelta,
            Date = reader.GetDateTime(6),
            Subject = reader.GetString(7),
            Notes = reader.IsDBNull(8) ? null : reader.GetString(8),
            PaymentMethod = PaymentMethodHelper.FromDatabaseString(reader.GetString(9)),
            Seq = reader.GetInt64(10),
            CategoryId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            TransactionGroupId = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            IncomeSource = reader.IsDBNull(13) ? null : reader.GetString(13),
            CreatedAt = reader.GetDateTime(14),
            UpdatedAt = reader.GetDateTime(15),
            BalanceAfter = initialBalance + cumulativeDelta  // Computed!
        };
    }
    
    /// <summary>
    /// Maps a database reader row to a domain entity (without JOIN - BalanceAfter will be null).
    /// Column order: id, user_id, transaction_type, amount, signed_amount, cumulative_delta,
    ///               date, subject, notes, payment_method, seq, category_id, transaction_group_id, 
    ///               income_source, created_at, updated_at
    /// </summary>
    private static Transaction MapToDomainEntityWithoutJoin(NpgsqlDataReader reader)
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
            Seq = reader.GetInt64(10),
            CategoryId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            TransactionGroupId = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            IncomeSource = reader.IsDBNull(13) ? null : reader.GetString(13),
            CreatedAt = reader.GetDateTime(14),
            UpdatedAt = reader.GetDateTime(15),
            BalanceAfter = null  // Will be computed later
        };
    }
}
