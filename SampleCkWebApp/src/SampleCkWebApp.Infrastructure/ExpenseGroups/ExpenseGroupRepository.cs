// ============================================================================
// FILE: ExpenseGroupRepository.cs
// ============================================================================
// WHAT: PostgreSQL implementation of the expense group repository interface.
//
// WHY: This repository exists in the Infrastructure layer to handle all
//      database operations for expense groups. It implements IExpenseGroupRepository (defined
//      in Application layer) following the Dependency Inversion Principle.
//      By keeping database-specific code (Npgsql, SQL queries) here, the
//      Application layer remains database-agnostic. If we need to switch
//      databases, only this file changes, not the business logic.
//
// WHAT IT DOES:
//      - Implements IExpenseGroupRepository interface with PostgreSQL/Npgsql
//      - Executes SQL queries for CRUD operations on expense groups
//      - Maps database records to ExpenseGroup domain entities
//      - Handles database exceptions (connection errors, foreign key violations)
//      - Returns ErrorOr results for consistent error handling
//      - Uses UserOptions for database connection string configuration
// ============================================================================

using ErrorOr;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.ExpenseGroups.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Users.Options;

namespace SampleCkWebApp.Infrastructure.ExpenseGroups;

/// <summary>
/// PostgreSQL implementation of the expense group repository.
/// Maps database records to domain entities.
/// </summary>
public class ExpenseGroupRepository : IExpenseGroupRepository
{
    private readonly UserOptions _options;

    public ExpenseGroupRepository(UserOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<List<ExpenseGroup>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, description, user_id FROM ExpenseGroup ORDER BY id",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var expenseGroups = new List<ExpenseGroup>();

            while (await reader.ReadAsync(cancellationToken))
            {
                expenseGroups.Add(MapToDomainEntity(reader));
            }

            return expenseGroups;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve expense groups: {ex.Message}");
        }
    }

    public async Task<ErrorOr<ExpenseGroup>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, description, user_id FROM ExpenseGroup WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return ExpenseGroupErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve expense group: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<ExpenseGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, description, user_id FROM ExpenseGroup WHERE user_id = @user_id ORDER BY id",
                connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var expenseGroups = new List<ExpenseGroup>();

            while (await reader.ReadAsync(cancellationToken))
            {
                expenseGroups.Add(MapToDomainEntity(reader));
            }

            return expenseGroups;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve expense groups by user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<ExpenseGroup>> CreateAsync(ExpenseGroup expenseGroup, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"INSERT INTO ExpenseGroup (name, description, user_id) 
                  VALUES (@name, @description, @user_id) 
                  RETURNING id, name, description, user_id",
                connection);
            
            command.Parameters.AddWithValue("name", expenseGroup.Name);
            command.Parameters.AddWithValue("description", (object?)expenseGroup.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("user_id", expenseGroup.UserId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return Error.Failure("Database.Error", "Failed to create expense group");
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create expense group: {ex.Message}");
        }
    }

    public async Task<ErrorOr<ExpenseGroup>> UpdateAsync(ExpenseGroup expenseGroup, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"UPDATE ExpenseGroup 
                  SET name = @name, description = @description, user_id = @user_id 
                  WHERE id = @id 
                  RETURNING id, name, description, user_id",
                connection);
            
            command.Parameters.AddWithValue("id", expenseGroup.Id);
            command.Parameters.AddWithValue("name", expenseGroup.Name);
            command.Parameters.AddWithValue("description", (object?)expenseGroup.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("user_id", expenseGroup.UserId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return ExpenseGroupErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update expense group: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "DELETE FROM ExpenseGroup WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                return ExpenseGroupErrors.NotFound;
            }

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete expense group: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps a database reader row to a domain entity.
    /// </summary>
    private static ExpenseGroup MapToDomainEntity(NpgsqlDataReader reader)
    {
        return new ExpenseGroup
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            UserId = reader.GetInt32(3)
        };
    }
}

