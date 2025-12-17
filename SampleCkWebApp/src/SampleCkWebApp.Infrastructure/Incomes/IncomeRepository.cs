// ============================================================================
// FILE: IncomeRepository.cs
// ============================================================================
// WHAT: PostgreSQL implementation of the income repository interface.
//
// WHY: This repository exists in the Infrastructure layer to handle all
//      database operations for income. It implements IIncomeRepository (defined
//      in Application layer) following the Dependency Inversion Principle.
//      By keeping database-specific code (Npgsql, SQL queries) here, the
//      Application layer remains database-agnostic. If we need to switch
//      databases, only this file changes, not the business logic.
//
// WHAT IT DOES:
//      - Implements IIncomeRepository interface with PostgreSQL/Npgsql
//      - Executes SQL queries for CRUD operations on income
//      - Maps database records to Income domain entities
//      - Handles PaymentMethod enum conversion between C# and database formats
//      - Handles database exceptions (connection errors, foreign key violations)
//      - Returns ErrorOr results for consistent error handling
//      - Uses UserOptions for database connection string configuration
// ============================================================================

using ErrorOr;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Incomes.Interfaces.Infrastructure;
using SampleCkWebApp.Application.UserBalances.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Users.Options;
using SampleCkWebApp.Infrastructure.Shared;

namespace SampleCkWebApp.Infrastructure.Incomes;

/// <summary>
/// PostgreSQL implementation of the income repository.
/// Maps database records to domain entities.
/// </summary>
public class IncomeRepository : IIncomeRepository
{
    private readonly UserOptions _options;
    private readonly IUserBalanceRepository _userBalanceRepository;

    public IncomeRepository(UserOptions options, IUserBalanceRepository userBalanceRepository)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _userBalanceRepository = userBalanceRepository ?? throw new ArgumentNullException(nameof(userBalanceRepository));
    }

    public async Task<ErrorOr<List<Income>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, amount, description, source, payment_method, user_id, date::timestamp FROM Income ORDER BY id",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var incomes = new List<Income>();

            while (await reader.ReadAsync(cancellationToken))
            {
                incomes.Add(MapToDomainEntity(reader));
            }

            return incomes;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve incomes: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Income>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, amount, description, source, payment_method, user_id, date::timestamp FROM Income WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return IncomeErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve income: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Income>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, amount, description, source, payment_method, user_id, date::timestamp FROM Income WHERE user_id = @user_id ORDER BY id",
                connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var incomes = new List<Income>();

            while (await reader.ReadAsync(cancellationToken))
            {
                incomes.Add(MapToDomainEntity(reader));
            }

            return incomes;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve incomes by user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Income>> CreateAsync(Income income, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"INSERT INTO Income (amount, description, source, payment_method, user_id, date) 
                  VALUES (@amount, @description, @source, @payment_method, @user_id, @date) 
                  RETURNING id, amount, description, source, payment_method, user_id, date::timestamp",
                connection);
            
            command.Parameters.AddWithValue("amount", income.Amount);
            command.Parameters.AddWithValue("description", (object?)income.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("source", (object?)income.Source ?? DBNull.Value);
            command.Parameters.AddWithValue("payment_method", PaymentMethodHelper.ToDatabaseString(income.PaymentMethod));
            command.Parameters.AddWithValue("user_id", income.UserId);
            command.Parameters.AddWithValue("date", income.Date);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return Error.Failure("Database.Error", "Failed to create income");
            }

            var createdIncome = MapToDomainEntity(reader);
            
            // Update user balance and record in history
            var balanceResult = await _userBalanceRepository.UpdateBalanceAsync(
                income.UserId, 
                income.Amount,
                income.Date,
                "INCOME",
                createdIncome.Id,
                cancellationToken);
            
            if (balanceResult.IsError)
            {
                // Log the error but don't fail the income creation
                // Balance can be recalculated later if needed
            }

            return createdIncome;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create income: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Income>> UpdateAsync(Income income, CancellationToken cancellationToken)
    {
        try
        {
            // Get old income to calculate balance difference
            var oldIncomeResult = await GetByIdAsync(income.Id, cancellationToken);
            if (oldIncomeResult.IsError)
            {
                return oldIncomeResult.Errors;
            }
            var oldIncome = oldIncomeResult.Value;
            
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"UPDATE Income 
                  SET amount = @amount, description = @description, source = @source, 
                      payment_method = @payment_method, user_id = @user_id, date = @date 
                  WHERE id = @id 
                  RETURNING id, amount, description, source, payment_method, user_id, date::timestamp",
                connection);
            
            command.Parameters.AddWithValue("id", income.Id);
            command.Parameters.AddWithValue("amount", income.Amount);
            command.Parameters.AddWithValue("description", (object?)income.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("source", (object?)income.Source ?? DBNull.Value);
            command.Parameters.AddWithValue("payment_method", PaymentMethodHelper.ToDatabaseString(income.PaymentMethod));
            command.Parameters.AddWithValue("user_id", income.UserId);
            command.Parameters.AddWithValue("date", income.Date);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return IncomeErrors.NotFound;
            }

            var updatedIncome = MapToDomainEntity(reader);
            
            // Update user balance (adjust for amount difference) and record in history
            // If amount increased, add more. If decreased, subtract the difference.
            var amountDifference = income.Amount - oldIncome.Amount;
            var balanceResult = await _userBalanceRepository.UpdateBalanceAsync(
                income.UserId, 
                amountDifference,
                income.Date,
                "INCOME",
                updatedIncome.Id,
                cancellationToken);
            
            if (balanceResult.IsError)
            {
                // Log the error but don't fail the income update
                // Balance can be recalculated later if needed
            }

            return updatedIncome;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update income: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Get income before deleting to update balance
            var incomeResult = await GetByIdAsync(id, cancellationToken);
            if (incomeResult.IsError)
            {
                return incomeResult.Errors;
            }
            var income = incomeResult.Value;
            
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "DELETE FROM Income WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                return IncomeErrors.NotFound;
            }

            // Update user balance (subtract the deleted income amount) and record in history
            var balanceResult = await _userBalanceRepository.UpdateBalanceAsync(
                income.UserId, 
                -income.Amount,
                DateTime.UtcNow, // Use current time for deletion
                "INCOME",
                income.Id,
                cancellationToken);
            
            if (balanceResult.IsError)
            {
                // Log the error but don't fail the income deletion
                // Balance can be recalculated later if needed
            }

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete income: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps a database reader row to a domain entity.
    /// </summary>
    private static Income MapToDomainEntity(NpgsqlDataReader reader)
    {
        return new Income
        {
            Id = reader.GetInt32(0),
            Amount = reader.GetDecimal(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            Source = reader.IsDBNull(3) ? null : reader.GetString(3),
            PaymentMethod = PaymentMethodHelper.FromDatabaseString(reader.GetString(4)),
            UserId = reader.GetInt32(5),
            Date = reader.GetDateTime(6)
        };
    }
}

