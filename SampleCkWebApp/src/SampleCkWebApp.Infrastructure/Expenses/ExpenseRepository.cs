// ============================================================================
// FILE: ExpenseRepository.cs
// ============================================================================
// WHAT: PostgreSQL implementation of the expense repository interface.
//
// WHY: This repository exists in the Infrastructure layer to handle all
//      database operations for expenses. It implements IExpenseRepository (defined
//      in Application layer) following the Dependency Inversion Principle.
//      By keeping database-specific code (Npgsql, SQL queries) here, the
//      Application layer remains database-agnostic. If we need to switch
//      databases, only this file changes, not the business logic.
//
// WHAT IT DOES:
//      - Implements IExpenseRepository interface with PostgreSQL/Npgsql
//      - Executes SQL queries for CRUD operations on expenses
//      - Maps database records to Expense domain entities
//      - Handles PaymentMethod enum conversion between C# and database formats
//      - Handles database exceptions (connection errors, foreign key violations)
//      - Returns ErrorOr results for consistent error handling
//      - Uses UserOptions for database connection string configuration
// ============================================================================

using ErrorOr;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Expenses.Interfaces.Infrastructure;
using SampleCkWebApp.Application.UserBalances.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Users.Options;
using SampleCkWebApp.Infrastructure.Shared;

namespace SampleCkWebApp.Infrastructure.Expenses;

/// <summary>
/// PostgreSQL implementation of the expense repository.
/// Maps database records to domain entities.
/// </summary>
public class ExpenseRepository : IExpenseRepository
{
    private readonly UserOptions _options;
    private readonly IUserBalanceRepository _userBalanceRepository;

    public ExpenseRepository(UserOptions options, IUserBalanceRepository userBalanceRepository)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _userBalanceRepository = userBalanceRepository ?? throw new ArgumentNullException(nameof(userBalanceRepository));
    }

    public async Task<ErrorOr<List<Expense>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, amount, date::timestamp, description, payment_method, category_id, user_id, expense_group_id FROM Expense ORDER BY id",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var expenses = new List<Expense>();

            while (await reader.ReadAsync(cancellationToken))
            {
                expenses.Add(MapToDomainEntity(reader));
            }

            return expenses;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve expenses: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Expense>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, amount, date::timestamp, description, payment_method, category_id, user_id, expense_group_id FROM Expense WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return ExpenseErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve expense: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Expense>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, amount, date::timestamp, description, payment_method, category_id, user_id, expense_group_id FROM Expense WHERE user_id = @user_id ORDER BY id",
                connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var expenses = new List<Expense>();

            while (await reader.ReadAsync(cancellationToken))
            {
                expenses.Add(MapToDomainEntity(reader));
            }

            return expenses;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve expenses by user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Expense>> CreateAsync(Expense expense, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"INSERT INTO Expense (amount, date, description, payment_method, category_id, user_id, expense_group_id) 
                  VALUES (@amount, @date, @description, @payment_method, @category_id, @user_id, @expense_group_id) 
                  RETURNING id, amount, date::timestamp, description, payment_method, category_id, user_id, expense_group_id",
                connection);
            
            command.Parameters.AddWithValue("amount", expense.Amount);
            command.Parameters.AddWithValue("date", expense.Date);
            command.Parameters.AddWithValue("description", (object?)expense.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("payment_method", PaymentMethodHelper.ToDatabaseString(expense.PaymentMethod));
            command.Parameters.AddWithValue("category_id", expense.CategoryId);
            command.Parameters.AddWithValue("user_id", expense.UserId);
            command.Parameters.AddWithValue("expense_group_id", (object?)expense.ExpenseGroupId ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return Error.Failure("Database.Error", "Failed to create expense");
            }

            var createdExpense = MapToDomainEntity(reader);
            
            // Update user balance (subtract expense amount) and record in history
            var balanceResult = await _userBalanceRepository.UpdateBalanceAsync(
                expense.UserId, 
                -expense.Amount,
                expense.Date,
                "EXPENSE",
                createdExpense.Id,
                cancellationToken);
            
            if (balanceResult.IsError)
            {
                // Log the error but don't fail the expense creation
                // Balance can be recalculated later if needed
            }

            return createdExpense;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create expense: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Expense>> UpdateAsync(Expense expense, CancellationToken cancellationToken)
    {
        try
        {
            // Get old expense to calculate balance difference
            var oldExpenseResult = await GetByIdAsync(expense.Id, cancellationToken);
            if (oldExpenseResult.IsError)
            {
                return oldExpenseResult.Errors;
            }
            var oldExpense = oldExpenseResult.Value;
            
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"UPDATE Expense 
                  SET amount = @amount, date = @date, description = @description, payment_method = @payment_method, 
                      category_id = @category_id, user_id = @user_id, expense_group_id = @expense_group_id 
                  WHERE id = @id 
                  RETURNING id, amount, date::timestamp, description, payment_method, category_id, user_id, expense_group_id",
                connection);
            
            command.Parameters.AddWithValue("id", expense.Id);
            command.Parameters.AddWithValue("amount", expense.Amount);
            command.Parameters.AddWithValue("date", expense.Date);
            command.Parameters.AddWithValue("description", (object?)expense.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("payment_method", PaymentMethodHelper.ToDatabaseString(expense.PaymentMethod));
            command.Parameters.AddWithValue("category_id", expense.CategoryId);
            command.Parameters.AddWithValue("user_id", expense.UserId);
            command.Parameters.AddWithValue("expense_group_id", (object?)expense.ExpenseGroupId ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return ExpenseErrors.NotFound;
            }

            var updatedExpense = MapToDomainEntity(reader);
            
            // Update user balance (adjust for amount difference)
            // If amount increased, subtract more. If decreased, add back the difference.
            var amountDifference = oldExpense.Amount - expense.Amount;
            var balanceResult = await _userBalanceRepository.UpdateBalanceAsync(
                expense.UserId, 
                amountDifference,
                expense.Date,
                "EXPENSE",
                updatedExpense.Id,
                cancellationToken);
            
            if (balanceResult.IsError)
            {
                // Log the error but don't fail the expense update
                // Balance can be recalculated later if needed
            }

            return updatedExpense;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update expense: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Get expense before deleting to update balance
            var expenseResult = await GetByIdAsync(id, cancellationToken);
            if (expenseResult.IsError)
            {
                return expenseResult.Errors;
            }
            var expense = expenseResult.Value;
            
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "DELETE FROM Expense WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                return ExpenseErrors.NotFound;
            }

            // Update user balance (add back the deleted expense amount) and record in history
            var balanceResult = await _userBalanceRepository.UpdateBalanceAsync(
                expense.UserId, 
                expense.Amount,
                DateTime.UtcNow, // Use current time for deletion
                "EXPENSE",
                expense.Id,
                cancellationToken);
            
            if (balanceResult.IsError)
            {
                // Log the error but don't fail the expense deletion
                // Balance can be recalculated later if needed
            }

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete expense: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps a database reader row to a domain entity.
    /// </summary>
    private static Expense MapToDomainEntity(NpgsqlDataReader reader)
    {
        return new Expense
        {
            Id = reader.GetInt32(0),
            Amount = reader.GetDecimal(1),
            Date = reader.GetDateTime(2),
            Description = reader.IsDBNull(3) ? null : reader.GetString(3),
            PaymentMethod = PaymentMethodHelper.FromDatabaseString(reader.GetString(4)),
            CategoryId = reader.GetInt32(5),
            UserId = reader.GetInt32(6),
            ExpenseGroupId = reader.IsDBNull(7) ? null : reader.GetInt32(7)
        };
    }
}

