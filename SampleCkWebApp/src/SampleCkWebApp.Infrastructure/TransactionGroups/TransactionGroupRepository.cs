using ErrorOr;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.TransactionGroups.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Users.Options;

namespace SampleCkWebApp.Infrastructure.TransactionGroups;

/// <summary>
/// PostgreSQL implementation of the transaction group repository.
/// Maps database records to domain entities.
/// </summary>
public class TransactionGroupRepository : ITransactionGroupRepository
{
    private readonly UserOptions _options;

    public TransactionGroupRepository(UserOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<List<TransactionGroup>>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, description, user_id, created_at FROM TransactionGroup ORDER BY id",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var transactionGroups = new List<TransactionGroup>();

            while (await reader.ReadAsync(cancellationToken))
            {
                transactionGroups.Add(MapToDomainEntity(reader));
            }

            return transactionGroups;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction groups: {ex.Message}");
        }
    }

    public async Task<ErrorOr<TransactionGroup>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, description, user_id, created_at FROM TransactionGroup WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return TransactionGroupErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction group: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<TransactionGroup>>> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, description, user_id, created_at FROM TransactionGroup WHERE user_id = @user_id ORDER BY id",
                connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var transactionGroups = new List<TransactionGroup>();

            while (await reader.ReadAsync(cancellationToken))
            {
                transactionGroups.Add(MapToDomainEntity(reader));
            }

            return transactionGroups;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve transaction groups by user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<TransactionGroup>> CreateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"INSERT INTO TransactionGroup (name, description, user_id) 
                  VALUES (@name, @description, @user_id) 
                  RETURNING id, name, description, user_id, created_at",
                connection);
            
            command.Parameters.AddWithValue("name", transactionGroup.Name);
            command.Parameters.AddWithValue("description", (object?)transactionGroup.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("user_id", transactionGroup.UserId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return Error.Failure("Database.Error", "Failed to create transaction group");
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create transaction group: {ex.Message}");
        }
    }

    public async Task<ErrorOr<TransactionGroup>> UpdateAsync(TransactionGroup transactionGroup, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"UPDATE TransactionGroup 
                  SET name = @name, description = @description 
                  WHERE id = @id 
                  RETURNING id, name, description, user_id, created_at",
                connection);
            
            command.Parameters.AddWithValue("id", transactionGroup.Id);
            command.Parameters.AddWithValue("name", transactionGroup.Name);
            command.Parameters.AddWithValue("description", (object?)transactionGroup.Description ?? DBNull.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return TransactionGroupErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to update transaction group: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Deleted>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "DELETE FROM TransactionGroup WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                return TransactionGroupErrors.NotFound;
            }

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to delete transaction group: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps a database reader row to a domain entity.
    /// Column order: id, name, description, user_id, created_at
    /// </summary>
    private static TransactionGroup MapToDomainEntity(NpgsqlDataReader reader)
    {
        return new TransactionGroup
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            UserId = reader.GetInt32(3),
            CreatedAt = reader.GetDateTime(4)
        };
    }
}

