// ============================================================================
// FILE: UserBalanceRepository.cs
// ============================================================================
// WHAT: PostgreSQL implementation of the user balance repository interface.
//
// WHY: This repository exists in the Infrastructure layer to handle all
//      database operations for user balances. It implements IUserBalanceRepository (defined
//      in Application layer) following the Dependency Inversion Principle.
//      By keeping database-specific code (Npgsql, SQL queries) here, the
//      Application layer remains database-agnostic. If we need to switch
//      databases, only this file changes, not the business logic.
//
// WHAT IT DOES:
//      - Implements IUserBalanceRepository interface with PostgreSQL/Npgsql
//      - Executes SQL queries for balance operations
//      - Maps database records to UserBalance domain entities
//      - Handles incremental balance updates and full recalculations
//      - Handles database exceptions (connection errors)
//      - Returns ErrorOr results for consistent error handling
//      - Uses UserOptions for database connection string configuration
// ============================================================================

using ErrorOr;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.UserBalances.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Users.Options;

namespace SampleCkWebApp.Infrastructure.UserBalances;

/// <summary>
/// PostgreSQL implementation of the user balance repository.
/// Maps database records to domain entities.
/// </summary>
public class UserBalanceRepository : IUserBalanceRepository
{
    private readonly UserOptions _options;

    public UserBalanceRepository(UserOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<UserBalance>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, user_id, current_balance, initial_balance, last_updated FROM UserBalance WHERE user_id = @user_id",
                connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return UserBalanceErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user balance: {ex.Message}");
        }
    }

    public async Task<ErrorOr<UserBalance>> CreateAsync(UserBalance userBalance, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                UserBalance createdBalance;
                
                // Create the user balance
                await using (var command = new NpgsqlCommand(
                    @"INSERT INTO UserBalance (user_id, current_balance, initial_balance, last_updated) 
                      VALUES (@user_id, @current_balance, @initial_balance, CURRENT_TIMESTAMP) 
                      RETURNING id, user_id, current_balance, initial_balance, last_updated",
                    connection, transaction))
                {
                    command.Parameters.AddWithValue("user_id", userBalance.UserId);
                    command.Parameters.AddWithValue("current_balance", userBalance.CurrentBalance);
                    command.Parameters.AddWithValue("initial_balance", userBalance.InitialBalance);

                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Error.Failure("Database.Error", "Failed to create user balance");
                    }

                    createdBalance = MapToDomainEntity(reader);
                    
                    // Ensure all rows are consumed (should only be one, but this ensures reader is fully consumed)
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        // Should not happen with RETURNING, but ensures reader is fully consumed
                    }
                } // Reader and command are now fully disposed
                
                // Insert initial balance into history (reader is now fully disposed)
                await using (var historyCommand = new NpgsqlCommand(
                    @"INSERT INTO UserBalanceHistory (user_id, balance, transaction_date, transaction_type, transaction_id) 
                      VALUES (@user_id, @balance, @transaction_date, @transaction_type::transaction_type_enum, NULL)",
                    connection, transaction))
                {
                    historyCommand.Parameters.AddWithValue("user_id", userBalance.UserId);
                    historyCommand.Parameters.AddWithValue("balance", createdBalance.CurrentBalance);
                    historyCommand.Parameters.AddWithValue("transaction_date", createdBalance.LastUpdated);
                    historyCommand.Parameters.AddWithValue("transaction_type", "INITIAL");
                    
                    await historyCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                await transaction.CommitAsync(cancellationToken);
                return createdBalance;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // Unique violation
        {
            return Error.Conflict("Database.Error", "User balance already exists for this user.");
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create user balance: {ex.Message}");
        }
    }

    public async Task<ErrorOr<UserBalance>> UpdateBalanceAsync(int userId, decimal amountChange, DateTime transactionDate, string transactionType, int? transactionId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                UserBalance userBalance;
                
                // Update the current balance
                await using (var updateCommand = new NpgsqlCommand(
                    @"UPDATE UserBalance 
                      SET current_balance = current_balance + @amount_change, last_updated = CURRENT_TIMESTAMP 
                      WHERE user_id = @user_id 
                      RETURNING id, user_id, current_balance, initial_balance, last_updated",
                    connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("user_id", userId);
                    updateCommand.Parameters.AddWithValue("amount_change", amountChange);

                    await using var reader = await updateCommand.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return UserBalanceErrors.NotFound;
                    }

                    userBalance = MapToDomainEntity(reader);
                    
                    // Ensure all rows are consumed (should only be one, but this ensures reader is fully consumed)
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        // Should not happen with RETURNING, but ensures reader is fully consumed
                    }
                } // Reader and command are now fully disposed
                
                // Insert into history table (reader is now fully disposed)
                await using (var historyCommand = new NpgsqlCommand(
                    @"INSERT INTO UserBalanceHistory (user_id, balance, transaction_date, transaction_type, transaction_id) 
                      VALUES (@user_id, @balance, @transaction_date, @transaction_type::transaction_type_enum, @transaction_id)",
                    connection, transaction))
                {
                    historyCommand.Parameters.AddWithValue("user_id", userId);
                    historyCommand.Parameters.AddWithValue("balance", userBalance.CurrentBalance);
                    historyCommand.Parameters.AddWithValue("transaction_date", transactionDate);
                    historyCommand.Parameters.AddWithValue("transaction_type", transactionType);
                    historyCommand.Parameters.AddWithValue("transaction_id", transactionId ?? (object)DBNull.Value);
                    
                    await historyCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                await transaction.CommitAsync(cancellationToken);
                return userBalance;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update user balance: {ex.Message}");
        }
    }

    public async Task<ErrorOr<UserBalance>> RecalculateBalanceAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Use a transaction to ensure consistency
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            
            try
            {
                decimal initialBalance;
                
                // Get initial balance using ExecuteScalar
                await using (var initialBalanceCommand = new NpgsqlCommand(
                    "SELECT initial_balance FROM UserBalance WHERE user_id = @user_id",
                    connection, transaction))
                {
                    initialBalanceCommand.Parameters.AddWithValue("user_id", userId);
                    var initialBalanceResult = await initialBalanceCommand.ExecuteScalarAsync(cancellationToken);
                    if (initialBalanceResult == null || initialBalanceResult == DBNull.Value)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return UserBalanceErrors.NotFound;
                    }
                    initialBalance = (decimal)initialBalanceResult;
                }
                
                // Sum all income
                decimal totalIncome;
                await using (var incomeCommand = new NpgsqlCommand(
                    "SELECT COALESCE(SUM(amount), 0) FROM Income WHERE user_id = @user_id",
                    connection, transaction))
                {
                    incomeCommand.Parameters.AddWithValue("user_id", userId);
                    totalIncome = (decimal)(await incomeCommand.ExecuteScalarAsync(cancellationToken) ?? 0m);
                }
                
                // Sum all expenses
                decimal totalExpenses;
                await using (var expenseCommand = new NpgsqlCommand(
                    "SELECT COALESCE(SUM(amount), 0) FROM Expense WHERE user_id = @user_id",
                    connection, transaction))
                {
                    expenseCommand.Parameters.AddWithValue("user_id", userId);
                    totalExpenses = (decimal)(await expenseCommand.ExecuteScalarAsync(cancellationToken) ?? 0m);
                }
                
                // Calculate new balance
                var newBalance = initialBalance + totalIncome - totalExpenses;
                
                // Update UserBalance
                UserBalance userBalance;
                await using (var updateCommand = new NpgsqlCommand(
                    @"UPDATE UserBalance 
                      SET current_balance = @current_balance, last_updated = CURRENT_TIMESTAMP 
                      WHERE user_id = @user_id 
                      RETURNING id, user_id, current_balance, initial_balance, last_updated",
                    connection, transaction))
                {
                    updateCommand.Parameters.AddWithValue("current_balance", newBalance);
                    updateCommand.Parameters.AddWithValue("user_id", userId);
                    
                    await using var updateReader = await updateCommand.ExecuteReaderAsync(cancellationToken);
                    if (!await updateReader.ReadAsync(cancellationToken))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return UserBalanceErrors.NotFound;
                    }
                    
                    userBalance = MapToDomainEntity(updateReader);
                    
                    // Ensure all rows are consumed (should only be one, but this ensures reader is fully consumed)
                    while (await updateReader.ReadAsync(cancellationToken))
                    {
                        // Should not happen with RETURNING, but ensures reader is fully consumed
                    }
                } // Reader and command are now fully disposed
                
                // Insert into history table for recalculation (reader is now fully disposed)
                await using (var historyCommand = new NpgsqlCommand(
                    @"INSERT INTO UserBalanceHistory (user_id, balance, transaction_date, transaction_type, transaction_id) 
                      VALUES (@user_id, @balance, CURRENT_TIMESTAMP, @transaction_type::transaction_type_enum, NULL)",
                    connection, transaction))
                {
                    historyCommand.Parameters.AddWithValue("user_id", userId);
                    historyCommand.Parameters.AddWithValue("balance", userBalance.CurrentBalance);
                    historyCommand.Parameters.AddWithValue("transaction_type", "RECALCULATE");
                    
                    await historyCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                await transaction.CommitAsync(cancellationToken);
                
                return userBalance;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to recalculate balance: {ex.Message}");
        }
    }

    public async Task<ErrorOr<decimal>> CalculateBalanceAtDateAsync(int userId, DateTime targetDate, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Query the most recent balance history record at or before the target date
            await using (var command = new NpgsqlCommand(
                @"SELECT balance FROM UserBalanceHistory 
                  WHERE user_id = @user_id AND transaction_date <= @target_date 
                  ORDER BY transaction_date DESC, id DESC 
                  LIMIT 1",
                connection))
            {
                command.Parameters.AddWithValue("user_id", userId);
                command.Parameters.AddWithValue("target_date", targetDate);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                
                if (result == null || result == DBNull.Value)
                {
                    // If no history found, check if user balance exists and return initial balance
                    await using (var fallbackCommand = new NpgsqlCommand(
                        "SELECT initial_balance FROM UserBalance WHERE user_id = @user_id",
                        connection))
                    {
                        fallbackCommand.Parameters.AddWithValue("user_id", userId);
                        var fallbackResult = await fallbackCommand.ExecuteScalarAsync(cancellationToken);
                        
                        if (fallbackResult == null || fallbackResult == DBNull.Value)
                        {
                            return UserBalanceErrors.NotFound;
                        }
                        
                        return (decimal)fallbackResult;
                    }
                }

                return (decimal)result;
            }
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to get balance at date: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps a database reader row to a domain entity.
    /// </summary>
    private static UserBalance MapToDomainEntity(NpgsqlDataReader reader)
    {
        return new UserBalance
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            CurrentBalance = reader.GetDecimal(2),
            InitialBalance = reader.GetDecimal(3),
            LastUpdated = reader.GetDateTime(4)
        };
    }
}

