// ============================================================================
// FILE: CategoryRepository.cs
// ============================================================================
// WHAT: PostgreSQL implementation of the category repository interface.
//
// WHY: This repository exists in the Infrastructure layer to handle all
//      database operations for categories. It implements ICategoryRepository (defined
//      in Application layer) following the Dependency Inversion Principle.
//      By keeping database-specific code (Npgsql, SQL queries) here, the
//      Application layer remains database-agnostic. If we need to switch
//      databases, only this file changes, not the business logic.
//
// WHAT IT DOES:
//      - Implements ICategoryRepository interface with PostgreSQL/Npgsql
//      - Executes SQL queries for CRUD operations on categories
//      - Maps database records to Category domain entities
//      - Handles database exceptions (connection errors, foreign key violations)
//      - Returns ErrorOr results for consistent error handling
//      - Uses UserOptions for database connection string configuration
// ============================================================================

using ErrorOr;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Categories.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Users.Options;

namespace SampleCkWebApp.Infrastructure.Categories;

/// <summary>
/// PostgreSQL implementation of the category repository.
/// Maps database records to domain entities.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly UserOptions _options;

    public CategoryRepository(UserOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<List<Category>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, description, icon FROM Category ORDER BY id",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var categories = new List<Category>();

            while (await reader.ReadAsync(cancellationToken))
            {
                categories.Add(MapToDomainEntity(reader));
            }

            return categories;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve categories: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Category>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, description, icon FROM Category WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return CategoryErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve category: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Category>> CreateAsync(Category category, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"INSERT INTO Category (name, description, icon) 
                  VALUES (@name, @description, @icon) 
                  RETURNING id, name, description, icon",
                connection);
            
            command.Parameters.AddWithValue("name", category.Name);
            command.Parameters.AddWithValue("description", (object?)category.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("icon", (object?)category.Icon ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return Error.Failure("Database.Error", "Failed to create category");
            }

            return MapToDomainEntity(reader);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // Unique violation
        {
            return CategoryErrors.DuplicateName;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create category: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Category>> UpdateAsync(Category category, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"UPDATE Category 
                  SET name = @name, description = @description, icon = @icon 
                  WHERE id = @id 
                  RETURNING id, name, description, icon",
                connection);
            
            command.Parameters.AddWithValue("id", category.Id);
            command.Parameters.AddWithValue("name", category.Name);
            command.Parameters.AddWithValue("description", (object?)category.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("icon", (object?)category.Icon ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return CategoryErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // Unique violation
        {
            return CategoryErrors.DuplicateName;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update category: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "DELETE FROM Category WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                return CategoryErrors.NotFound;
            }

            return Result.Deleted;
        }
        catch (PostgresException ex) when (ex.SqlState == "23503") // Foreign key violation
        {
            return Error.Conflict("Database.Error", "Cannot delete category because it is referenced by expenses.");
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete category: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps a database reader row to a domain entity.
    /// </summary>
    private static Category MapToDomainEntity(NpgsqlDataReader reader)
    {
        return new Category
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            Icon = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }
}

