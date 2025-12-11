using ErrorOr;
using Npgsql;
using SampleCkWebApp.Application.Users.Data;
using SampleCkWebApp.Application.Users.Interfaces.Infrastructure;
using SampleCkWebApp.Domain.Errors;
using SampleCkWebApp.Infrastructure.Users.Options;

namespace SampleCkWebApp.Infrastructure.Users;

public class UserRepository : IUserRepository
{
    private readonly UserOptions _options;

    public UserRepository(UserOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<List<UserRecord>>> GetUsersAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, email, password_hash, created_at, updated_at FROM users ORDER BY id",
                connection);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var users = new List<UserRecord>();

            while (await reader.ReadAsync(cancellationToken))
            {
                users.Add(new UserRecord
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4),
                    UpdatedAt = reader.GetDateTime(5)
                });
            }

            return users;
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve users: {ex.Message}");
        }
    }

    public async Task<ErrorOr<UserRecord>> GetUserByIdAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, email, password_hash, created_at, updated_at FROM users WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return UserErrors.NotFound;
            }

            return new UserRecord
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5)
            };
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user: {ex.Message}");
        }
    }

    public async Task<ErrorOr<UserRecord>> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                "SELECT id, name, email, password_hash, created_at, updated_at FROM users WHERE email = @email",
                connection);
            command.Parameters.AddWithValue("email", email);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return UserErrors.NotFound;
            }

            return new UserRecord
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5)
            };
        }
        catch (Exception ex)
        {
            return Error.Failure("Database.Error", $"Failed to retrieve user by email: {ex.Message}");
        }
    }

    public async Task<ErrorOr<UserRecord>> CreateUserAsync(string name, string email, string passwordHash, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new NpgsqlCommand(
                @"INSERT INTO users (name, email, password_hash, created_at, updated_at) 
                  VALUES (@name, @email, @password_hash, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
                  RETURNING id, name, email, password_hash, created_at, updated_at",
                connection);
            
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("email", email);
            command.Parameters.AddWithValue("password_hash", passwordHash);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return Error.Failure("Database.Error", "Failed to create user");
            }

            return new UserRecord
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Email = reader.GetString(2),
                PasswordHash = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                UpdatedAt = reader.GetDateTime(5)
            };
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
}

