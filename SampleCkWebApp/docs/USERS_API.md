# Users API Documentation

This document describes the Users API endpoints for the Expense Tracker application.

## Overview

The Users API provides endpoints to manage users in the expense tracker system. It follows Clean Architecture principles and is fully integrated with the PostgreSQL database.

## Endpoints

### GET /users

Retrieves all users from the database.

**Request:**
```
GET /users
```

**Response:**
```json
{
  "users": [
    {
      "id": 1,
      "name": "John Doe",
      "email": "john.doe@example.com",
      "createdAt": "2025-01-01T10:00:00Z",
      "updatedAt": "2025-01-01T10:00:00Z"
    }
  ],
  "totalCount": 1
}
```

**Status Codes:**
- `200 OK` - Successfully retrieved users
- `500 Internal Server Error` - Database error

---

### GET /users/{id}

Retrieves a specific user by ID.

**Request:**
```
GET /users/1
```

**Response:**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "createdAt": "2025-01-01T10:00:00Z",
  "updatedAt": "2025-01-01T10:00:00Z"
}
```

**Status Codes:**
- `200 OK` - User found
- `404 Not Found` - User not found
- `500 Internal Server Error` - Database error

---

### POST /users

Creates a new user.

**Request:**
```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "password": "securepassword123"
}
```

**Response:**
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "createdAt": "2025-01-01T10:00:00Z",
  "updatedAt": "2025-01-01T10:00:00Z"
}
```

**Status Codes:**
- `201 Created` - User successfully created
- `400 Bad Request` - Validation error (invalid name, email, or password)
- `409 Conflict` - User with this email already exists
- `500 Internal Server Error` - Database error

**Validation Rules:**
- `name`: Required, 1-100 characters
- `email`: Required, must be a valid email format
- `password`: Required, minimum 6 characters

**Note:** Passwords are automatically hashed using BCrypt before storage. The password hash is never returned in responses.

---

## Database Setup

Before using the API, ensure your PostgreSQL database is set up with the following schema:

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Configuration

Update `appsettings.json` with your database connection string:

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;Database=expense_tracker;Username=postgres;Password=password"
  }
}
```

## Architecture

The Users feature follows Clean Architecture:

- **Domain Layer**: `UserErrors.cs` - Domain-specific error definitions
- **Contracts Layer**: Request/Response DTOs (`CreateUserRequest`, `UserResponse`, `GetUsersResponse`)
- **Application Layer**: 
  - `IUserService` - Application service interface
  - `UserService` - Business logic and orchestration
  - `UserValidator` - Validation logic
  - `UserRecord` - Application data models
- **Infrastructure Layer**:
  - `IUserRepository` - Repository interface
  - `UserRepository` - PostgreSQL implementation
  - `UserOptions` - Configuration options
- **WebApi Layer**:
  - `UsersController` - HTTP endpoints
  - `UserMappings` - DTO mapping logic

## Testing

### Using curl

```bash
# Get all users
curl http://localhost:5000/users

# Get user by ID
curl http://localhost:5000/users/1

# Create a new user
curl -X POST http://localhost:5000/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "john.doe@example.com",
    "password": "securepassword123"
  }'
```

### Using PowerShell

```powershell
# Get all users
Invoke-WebRequest -Uri http://localhost:5000/users | Select-Object -ExpandProperty Content

# Get user by ID
Invoke-WebRequest -Uri http://localhost:5000/users/1 | Select-Object -ExpandProperty Content

# Create a new user
$body = @{
    name = "John Doe"
    email = "john.doe@example.com"
    password = "securepassword123"
} | ConvertTo-Json

Invoke-WebRequest -Uri http://localhost:5000/users `
    -Method POST `
    -ContentType "application/json" `
    -Body $body
```

## Error Handling

All endpoints use the `ErrorOr` pattern for error handling. Errors are automatically converted to appropriate HTTP status codes:

- Validation errors → `400 Bad Request`
- Not found errors → `404 Not Found`
- Conflict errors (duplicate email) → `409 Conflict`
- Database errors → `500 Internal Server Error`

Error responses follow the standard ASP.NET Core Problem Details format.

