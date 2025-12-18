# Expense Tracker API Documentation

## Overview

RESTful API built with ASP.NET Core for managing users, categories, transactions, and transaction groups. The API follows a clean architecture pattern with clear separation between Domain, Application, Infrastructure, and WebApi layers.

## API Versioning

All endpoints are versioned under `/api/v1`. Versioning is managed through route constants in `ApiRoutes.cs` for easy maintenance and future expansion.

- **Base URL**: `/api/v1`
- **Swagger UI**: `/swagger`

## Endpoints

### Users (`/api/v1/users`)

- `GET /users` - Get all users
- `GET /users/{id}` - Get user by ID
- `POST /users` - Create new user
- `GET /users/{id}/balance` - Get user's current balance (initial balance + cumulative delta from latest transaction)
- `PUT /users/{id}/initial-balance` - Set user's initial balance

### Categories (`/api/v1/categories`)

- `GET /categories` - Get all categories
- `GET /categories/{id}` - Get category by ID
- `POST /categories` - Create new category (name must be unique)
- `PUT /categories/{id}` - Update category
- `DELETE /categories/{id}` - Delete category

### Transactions (`/api/v1/transactions`)

- `GET /transactions` - Get all transactions
- `GET /transactions/{id}` - Get transaction by ID
- `GET /transactions/user/{userId}` - Get all transactions for a user (404 if user doesn't exist)
- `GET /transactions/user/{userId}/type/{type}` - Get transactions by user and type (404 if user doesn't exist)
- `POST /transactions` - Create new transaction (validates user, category, and transaction group existence)
- `PUT /transactions/{id}` - Update transaction (validates user, category, and transaction group existence)
- `DELETE /transactions/{id}` - Delete transaction

### Transaction Groups (`/api/v1/transaction-groups`)

- `GET /transaction-groups` - Get all transaction groups
- `GET /transaction-groups/{id}` - Get transaction group by ID
- `GET /transaction-groups/user/{userId}` - Get transaction groups for a user (404 if user doesn't exist)
- `POST /transaction-groups` - Create new transaction group (validates user existence)
- `PUT /transaction-groups/{id}` - Update transaction group
- `DELETE /transaction-groups/{id}` - Delete transaction group

### Health Check

- `GET /health` - Application health status

## Error Handling

All errors follow RFC 9110 Problem Details format. The API uses the `ErrorOr` pattern for functional error handling.

**Status Codes:**

- `400 Bad Request` - Validation errors
- `404 Not Found` - Resource not found
- `409 Conflict` - Duplicate resources (e.g., category name already exists)
- `422 Unprocessable Entity` - Semantic validation errors (e.g., user doesn't exist when creating transaction group)
- `500 Internal Server Error` - Unexpected errors

**Error Response Format:**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Error description",
  "status": 404,
  "detail": "Detailed error message",
  "instance": "/api/v1/transactions/123"
}
```

## Architecture

**Layers:**

- **Domain** - Entities and business rules
- **Application** - Business logic and use cases
- **Infrastructure** - Data access (PostgreSQL via Npgsql)
- **WebApi** - HTTP controllers and API configuration

**Key Patterns:**

- Repository pattern for data access
- Service layer for business logic
- ErrorOr for functional error handling
- Mapping extensions for DTO conversions

## Database

PostgreSQL database with the following main entities:

- `User` - Users with initial balance
- `Category` - Transaction categories (unique names)
- `Transaction` - Individual transactions with cumulative delta tracking
- `TransactionGroup` - Groups of related transactions

Balance calculation: `initial_balance + cumulative_delta` from the latest transaction.
