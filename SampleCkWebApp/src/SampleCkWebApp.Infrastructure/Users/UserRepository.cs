// ============================================================================
// FILE: UserRepository.cs
// ============================================================================
// WHAT: PostgreSQL implementation of the user repository interface.
//
// WHY: This repository exists in the Infrastructure layer to handle all
//      database operations for users. It implements IUserRepository (defined
//      in Application layer) following the Dependency Inversion Principle.
//      By keeping database-specific code (Npgsql, SQL queries) here, the
//      Application layer remains database-agnostic. If we need to switch
//      databases, only this file changes, not the business logic.
//
// WHAT IT DOES:
//      - Implements IUserRepository interface with PostgreSQL/Npgsql
//      - Executes SQL queries to: get all users, get user by ID, get user by
//        email, and create new users
//      - Maps database records to User domain entities
//      - Handles database exceptions (unique violations, connection errors)
//      - Returns ErrorOr results for consistent error handling
//      - Uses UserOptions for database connection string configuration
// ============================================================================

using ErrorOr;
using Npgsql;
using SampleCkWebApp.Domain.Entities;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Application.Users.Interfaces.Infrastructure;
using SampleCkWebApp.Infrastructure.Users.Options;

namespace SampleCkWebApp.Infrastructure.Users;

/// <summary>
/// PostgreSQL implementation of the user repository.
/// Maps database records to domain entities.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UserOptions _options;

    public UserRepository(UserOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<List<User>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, email, password_hash, initial_balance, created_at, updated_at FROM \"Users\" ORDER BY id",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var users = new List<User>();

            while (await reader.ReadAsync(cancellationToken))
            {
                users.Add(MapToDomainEntity(reader));
            }

            return users;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve users: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, email, password_hash, initial_balance, created_at, updated_at FROM \"Users\" WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return UserErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, email, password_hash, initial_balance, created_at, updated_at FROM \"Users\" WHERE email = @email",
                connection);
            command.Parameters.AddWithValue("email", email);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return UserErrors.NotFound;
            }

            return MapToDomainEntity(reader);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user by email: {ex.Message}");
        }
    }

    public async Task<ErrorOr<User>> CreateUserAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"INSERT INTO ""Users"" (name, email, password_hash, initial_balance, created_at, updated_at) 
                  VALUES (@name, @email, @password_hash, @initial_balance, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
                  RETURNING id, name, email, password_hash, initial_balance, created_at, updated_at",
                connection);
            
            command.Parameters.AddWithValue("name", user.Name);
            command.Parameters.AddWithValue("email", user.Email);
            command.Parameters.AddWithValue("password_hash", user.PasswordHash);
            command.Parameters.AddWithValue("initial_balance", user.InitialBalance);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return Error.Failure("Database.Error", "Failed to create user");
            }

            return MapToDomainEntity(reader);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") // Unique violation
        {
            return UserErrors.DuplicateEmail;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to create user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<(decimal InitialBalance, decimal CumulativeDelta, decimal CurrentBalance)>> GetUserBalanceAsync(int userId, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Get user's initial_balance and the cumulative_delta from their latest transaction
            var command = new NpgsqlCommand(
                @"SELECT 
                    u.initial_balance,
                    COALESCE(t.cumulative_delta, 0) AS cumulative_delta
                  FROM ""Users"" u
                  LEFT JOIN LATERAL (
                      SELECT cumulative_delta 
                      FROM Transaction 
                      WHERE user_id = u.id 
                      ORDER BY seq DESC 
                      LIMIT 1
                  ) t ON true
                  WHERE u.id = @user_id",
                connection);
            command.Parameters.AddWithValue("user_id", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return UserErrors.NotFound;
            }

            var initialBalance = reader.GetDecimal(0);
            var cumulativeDelta = reader.GetDecimal(1);
            var currentBalance = initialBalance + cumulativeDelta;

            return (initialBalance, cumulativeDelta, currentBalance);
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user balance: {ex.Message}");
        }
    }

    /// <summary>
    /// Maps a database reader row to a domain entity.
    /// Column order: id, name, email, password_hash, initial_balance, created_at, updated_at
    /// </summary>
    private static User MapToDomainEntity(NpgsqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Email = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            InitialBalance = reader.GetDecimal(4),
            CreatedAt = reader.GetDateTime(5),
            UpdatedAt = reader.GetDateTime(6)
        };
    }
}

