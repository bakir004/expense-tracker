# ExpenseTracker API - Endpoint Documentation

This document provides a quick reference guide to all available API endpoints in the ExpenseTracker API.

## Table of Contents

- [Base Information](#base-information)
- [Authentication](#authentication)
- [Health Check](#health-check)
- [Users](#users)
- [Categories](#categories)
- [Transaction Groups](#transaction-groups)
- [Transactions](#transactions)
- [Swagger Documentation](#swagger-documentation)

## Base Information

- **Base URL (Development)**: `http://localhost:5000`
- **Base URL (Docker)**: `http://localhost:8080`
- **API Version**: v1.0
- **Authentication**: JWT Bearer Token (except for public endpoints)

### API Versioning

All endpoints use URL-based versioning:
```
/api/v1/{resource}
```

## Authentication

All endpoints except authentication, health checks, and category listing require a valid JWT token in the Authorization header:

```http
Authorization: Bearer {your-jwt-token}
```

### 🔓 Register New User

**POST** `/api/v1/auth/register`

Creates a new user account.

**Request Body:**
```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "password": "Password123!",
  "initialBalance": 1000.00
}
```

**Password Requirements:**
- 8-100 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

**Response:** `201 Created`
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "balance": 1000.00,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Errors:**
- `400 Bad Request` - Invalid data or weak password
- `409 Conflict` - Email already exists

---

### 🔓 Login

**POST** `/api/v1/auth/login`

Authenticates a user and returns a JWT token.

**Request Body:**
```json
{
  "email": "john.doe@example.com",
  "password": "Password123!"
}
```

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "balance": 1000.00,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Seeded Test Users:**
- Email: `john.doe@email.com`, Password: `Password123!`
- Email: `jane.smith@email.com`, Password: `Password123!`
- Email: `mike.wilson@email.com`, Password: `Password123!`

**Errors:**
- `400 Bad Request` - Missing credentials
- `401 Unauthorized` - Invalid email or password

---

## Health Check

### 🔓 Get API Health Status

**GET** `/api/v1/health`

Returns comprehensive health information about the API and database.

**Response:** `200 OK`
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0.0",
  "database": {
    "status": "Healthy",
    "message": "Database connection successful"
  }
}
```

**Errors:**
- `503 Service Unavailable` - API or database is unhealthy

**Note:** This endpoint is public and used for monitoring/health checks.

---

## Users

### 🔒 Update User Profile

**PUT** `/api/v1/users/profile`

Partially updates the authenticated user's profile. Only provide fields you want to change.

**Request Body:**
```json
{
  "currentPassword": "Password123!",
  "name": "John Smith",
  "email": "john.smith@example.com",
  "newPassword": "NewPassword456!",
  "initialBalance": 1500.00
}
```

**Required Fields:**
- `currentPassword` - Always required for security verification

**Optional Fields (only updated if provided):**
- `name` - User's full name
- `email` - New email address (must be unique)
- `newPassword` - New password (must meet strength requirements)
- `initialBalance` - Updated account balance

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "John Smith",
  "email": "john.smith@example.com",
  "balance": 1500.00,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-20T14:45:00Z"
}
```

**Errors:**
- `400 Bad Request` - Validation errors or missing current password
- `401 Unauthorized` - Invalid current password or not authenticated
- `409 Conflict` - Email already in use

---

### 🔒 Delete User Account

**DELETE** `/api/v1/users/profile`

Permanently deletes the authenticated user's account and all associated data.

**⚠️ WARNING: This action is irreversible!**

**Request Body:**
```json
{
  "currentPassword": "Password123!",
  "confirmDeletion": true
}
```

**What Gets Deleted:**
- User account and profile
- All transactions
- All transaction groups
- All custom categories
- Authentication tokens become invalid

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "deletedAt": "2024-01-20T15:00:00Z",
  "message": "Account successfully deleted"
}
```

**Errors:**
- `400 Bad Request` - Missing password or confirmation flag
- `401 Unauthorized` - Invalid password or not authenticated

---

## Categories

### 🔓 Get All Categories

**GET** `/api/v1/categories`

Retrieves all available categories for expense and income transactions.

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "name": "Food & Dining",
    "description": "Groceries, restaurants, cafes",
    "icon": "utensils"
  },
  {
    "id": 2,
    "name": "Transportation",
    "description": "Gas, public transport, parking",
    "icon": "car"
  }
]
```

**Note:** This endpoint is public and doesn't require authentication.

**Use Cases:**
- Populating category dropdowns in transaction forms
- Category-based filtering and reporting
- Budget planning by category

---

## Transaction Groups

Transaction groups help organize related transactions together (e.g., "January Budget", "Vacation Expenses", "Home Renovation").

### 🔒 Get All Transaction Groups

**GET** `/api/v1/transaction-groups`

Retrieves all transaction groups for the authenticated user.

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "name": "January Budget",
    "description": "Monthly expenses for January 2024",
    "userId": 1,
    "createdAt": "2024-01-01T00:00:00Z"
  },
  {
    "id": 2,
    "name": "Vacation Trip",
    "description": "Summer vacation to Europe",
    "userId": 1,
    "createdAt": "2024-01-10T12:00:00Z"
  }
]
```

**Errors:**
- `401 Unauthorized` - Not authenticated

---

### 🔒 Get Transaction Group by ID

**GET** `/api/v1/transaction-groups/{id}`

Retrieves a specific transaction group by ID. Must belong to authenticated user.

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "January Budget",
  "description": "Monthly expenses for January 2024",
  "userId": 1,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Errors:**
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Group belongs to another user
- `404 Not Found` - Group doesn't exist

---

### 🔒 Create Transaction Group

**POST** `/api/v1/transaction-groups`

Creates a new transaction group for the authenticated user.

**Request Body:**
```json
{
  "name": "February Budget",
  "description": "Monthly expenses for February 2024"
}
```

**Response:** `201 Created`
```json
{
  "id": 3,
  "name": "February Budget",
  "description": "Monthly expenses for February 2024",
  "userId": 1,
  "createdAt": "2024-02-01T00:00:00Z"
}
```

**Errors:**
- `400 Bad Request` - Validation errors (e.g., missing name)
- `401 Unauthorized` - Not authenticated

---

### 🔒 Update Transaction Group

**PUT** `/api/v1/transaction-groups/{id}`

Updates an existing transaction group. Must belong to authenticated user.

**Request Body:**
```json
{
  "name": "Updated Budget Name",
  "description": "Updated description"
}
```

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "Updated Budget Name",
  "description": "Updated description",
  "userId": 1,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Errors:**
- `400 Bad Request` - Validation errors
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Group belongs to another user
- `404 Not Found` - Group doesn't exist

---

### 🔒 Delete Transaction Group

**DELETE** `/api/v1/transaction-groups/{id}`

Deletes a transaction group. Transactions in the group are not deleted, only ungrouped.

**Response:** `204 No Content`

**Errors:**
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Group belongs to another user
- `404 Not Found` - Group doesn't exist

---

## Transactions

### 🔒 Get Transactions (with Filtering & Pagination)

**GET** `/api/v1/transactions`

Retrieves transactions with powerful filtering, sorting, and pagination.

**Query Parameters:**

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `transactionType` | string | EXPENSE or INCOME | `EXPENSE` |
| `minAmount` | decimal | Minimum amount (inclusive) | `10.00` |
| `maxAmount` | decimal | Maximum amount (inclusive) | `1000.00` |
| `dateFrom` | date | Start date (yyyy-MM-dd) | `2024-01-01` |
| `dateTo` | date | End date (yyyy-MM-dd) | `2024-12-31` |
| `subjectContains` | string | Search in subjects | `grocery` |
| `notesContains` | string | Search in notes | `important` |
| `paymentMethods` | string | Comma-separated methods | `CASH,DEBIT_CARD` |
| `categoryIds` | string | Comma-separated IDs | `1,2,3` |
| `uncategorized` | boolean | Only uncategorized transactions | `true` |
| `transactionGroupIds` | string | Comma-separated group IDs | `1,2` |
| `ungrouped` | boolean | Only ungrouped transactions | `true` |
| `sortBy` | string | Sort field (date, amount, etc.) | `amount` |
| `sortDirection` | string | asc or desc | `desc` |
| `page` | integer | Page number (starts at 1) | `1` |
| `pageSize` | integer | Items per page (max 100) | `20` |

**Payment Methods:**
- `CASH`
- `DEBIT_CARD`
- `CREDIT_CARD`
- `BANK_TRANSFER`
- `MOBILE_PAYMENT`
- `PAYPAL`
- `CRYPTO`
- `OTHER`

**Example Request:**
```
GET /api/v1/transactions?transactionType=EXPENSE&dateFrom=2024-01-01&dateTo=2024-01-31&sortBy=date&sortDirection=desc&page=1&pageSize=20
```

**Response:** `200 OK`
```json
{
  "transactions": [
    {
      "id": 1,
      "userId": 1,
      "categoryId": 1,
      "categoryName": "Food & Dining",
      "transactionGroupId": 1,
      "transactionGroupName": "January Budget",
      "transactionType": "EXPENSE",
      "amount": 45.50,
      "date": "2024-01-15",
      "subject": "Grocery shopping",
      "notes": "Weekly groceries",
      "paymentMethod": "DEBIT_CARD",
      "createdAt": "2024-01-15T14:30:00Z",
      "updatedAt": "2024-01-15T14:30:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

**Errors:**
- `400 Bad Request` - Invalid parameters (e.g., invalid date format)
- `401 Unauthorized` - Not authenticated

---

### 🔒 Get Transaction by ID

**GET** `/api/v1/transactions/{id}`

Retrieves a specific transaction by ID. Must belong to authenticated user.

**Response:** `200 OK`
```json
{
  "id": 1,
  "userId": 1,
  "categoryId": 1,
  "categoryName": "Food & Dining",
  "transactionGroupId": 1,
  "transactionGroupName": "January Budget",
  "transactionType": "EXPENSE",
  "amount": 45.50,
  "date": "2024-01-15",
  "subject": "Grocery shopping",
  "notes": "Weekly groceries",
  "paymentMethod": "DEBIT_CARD",
  "createdAt": "2024-01-15T14:30:00Z",
  "updatedAt": "2024-01-15T14:30:00Z"
}
```

**Errors:**
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Transaction belongs to another user
- `404 Not Found` - Transaction doesn't exist

---

### 🔒 Create Transaction

**POST** `/api/v1/transactions`

Creates a new transaction for the authenticated user.

**Request Body:**
```json
{
  "categoryId": 1,
  "transactionGroupId": 1,
  "transactionType": "EXPENSE",
  "amount": 45.50,
  "date": "2024-01-15",
  "subject": "Grocery shopping",
  "notes": "Weekly groceries",
  "paymentMethod": "DEBIT_CARD"
}
```

**Required Fields:**
- `transactionType` - EXPENSE or INCOME
- `amount` - Positive decimal value
- `date` - Transaction date (yyyy-MM-dd)
- `subject` - Transaction description
- `paymentMethod` - Payment method enum

**Optional Fields:**
- `categoryId` - Category to assign
- `transactionGroupId` - Group to assign
- `notes` - Additional notes

**Response:** `201 Created`
```json
{
  "id": 1,
  "userId": 1,
  "categoryId": 1,
  "categoryName": "Food & Dining",
  "transactionGroupId": 1,
  "transactionGroupName": "January Budget",
  "transactionType": "EXPENSE",
  "amount": 45.50,
  "date": "2024-01-15",
  "subject": "Grocery shopping",
  "notes": "Weekly groceries",
  "paymentMethod": "DEBIT_CARD",
  "createdAt": "2024-01-15T14:30:00Z",
  "updatedAt": "2024-01-15T14:30:00Z"
}
```

**Errors:**
- `400 Bad Request` - Validation errors
- `401 Unauthorized` - Not authenticated
- `404 Not Found` - Referenced category or group doesn't exist

---

### 🔒 Update Transaction

**PUT** `/api/v1/transactions/{id}`

Updates an existing transaction. Must belong to authenticated user.

**Request Body:**
```json
{
  "categoryId": 2,
  "transactionGroupId": 1,
  "transactionType": "EXPENSE",
  "amount": 50.00,
  "date": "2024-01-15",
  "subject": "Updated subject",
  "notes": "Updated notes",
  "paymentMethod": "CREDIT_CARD"
}
```

**Response:** `200 OK`
```json
{
  "id": 1,
  "userId": 1,
  "categoryId": 2,
  "categoryName": "Transportation",
  "transactionGroupId": 1,
  "transactionGroupName": "January Budget",
  "transactionType": "EXPENSE",
  "amount": 50.00,
  "date": "2024-01-15",
  "subject": "Updated subject",
  "notes": "Updated notes",
  "paymentMethod": "CREDIT_CARD",
  "createdAt": "2024-01-15T14:30:00Z",
  "updatedAt": "2024-01-20T16:45:00Z"
}
```

**Errors:**
- `400 Bad Request` - Validation errors
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Transaction belongs to another user
- `404 Not Found` - Transaction, category, or group doesn't exist

---

### 🔒 Delete Transaction

**DELETE** `/api/v1/transactions/{id}`

Permanently deletes a transaction. Must belong to authenticated user.

**Response:** `204 No Content`

**Errors:**
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Transaction belongs to another user
- `404 Not Found` - Transaction doesn't exist

---

## Swagger Documentation

Interactive API documentation is available via Swagger UI:

**Development:**
- URL: `http://localhost:5000/swagger`

**Docker:**
- URL: `http://localhost:8080/swagger`

Swagger provides:
- Interactive API testing
- Detailed endpoint documentation
- Request/response schema definitions
- Authentication support (click "Authorize" button and enter token)

### Using Swagger with Authentication

1. Navigate to the Swagger UI
2. Click the **Authorize** button (top right)
3. Enter your token in the format: `Bearer {your-token}`
4. Click **Authorize** and **Close**
5. All authenticated endpoints will now include your token

---

## Quick Start Examples

### Example 1: Register and Login

```bash
# Register a new user
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "john@example.com",
    "password": "Password123!",
    "initialBalance": 1000.00
  }'

# Login to get token
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "Password123!"
  }'
```

### Example 2: Create and Query Transactions

```bash
# Set your token
TOKEN="your-jwt-token-here"

# Create a transaction
curl -X POST http://localhost:5000/api/v1/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "categoryId": 1,
    "transactionType": "EXPENSE",
    "amount": 45.50,
    "date": "2024-01-15",
    "subject": "Grocery shopping",
    "paymentMethod": "DEBIT_CARD"
  }'

# Get all expenses for January 2024
curl -X GET "http://localhost:5000/api/v1/transactions?transactionType=EXPENSE&dateFrom=2024-01-01&dateTo=2024-01-31" \
  -H "Authorization: Bearer $TOKEN"
```

### Example 3: Organize with Transaction Groups

```bash
TOKEN="your-jwt-token-here"

# Create a transaction group
curl -X POST http://localhost:5000/api/v1/transaction-groups \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "Vacation Trip",
    "description": "Summer vacation expenses"
  }'

# Create transaction in group (use group ID from previous response)
curl -X POST http://localhost:5000/api/v1/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "transactionGroupId": 1,
    "categoryId": 5,
    "transactionType": "EXPENSE",
    "amount": 150.00,
    "date": "2024-06-15",
    "subject": "Hotel booking",
    "paymentMethod": "CREDIT_CARD"
  }'
```

---

## Error Response Format

All errors follow the RFC 7807 Problem Details standard:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": [
    {
      "code": "Validation.Required",
      "description": "Email is required."
    }
  ]
}
```

**Common HTTP Status Codes:**
- `200 OK` - Request succeeded
- `201 Created` - Resource created successfully
- `204 No Content` - Request succeeded with no response body
- `400 Bad Request` - Invalid request data or validation errors
- `401 Unauthorized` - Authentication required or invalid credentials
- `403 Forbidden` - User doesn't have permission to access resource
- `404 Not Found` - Resource doesn't exist
- `409 Conflict` - Resource conflict (e.g., duplicate email)
- `503 Service Unavailable` - Service is temporarily unavailable

---

## Additional Resources

### Related Documentation
- **[Architecture Overview](../../docs/ARCHITECTURE.md)** - System design and layer responsibilities
- **[Domain Layer](../Domain/README.md)** - Business entities and validation rules
- **[Application Layer](../Application/README.md)** - Services implementing business logic
- **[Infrastructure Layer](../Infrastructure/README.md)** - Repository implementations and cumulative delta system
- **[Contracts Layer](../Contracts/README.md)** - Request/Response DTOs used by these endpoints

### Deployment & Configuration
- **[Main README](../../README.md)** - Quickstart guide and setup instructions
- **[Docker Setup](../../docker/README.md)** - Container configuration and deployment
- **API Configuration**: `appsettings.json`

### Testing
- **[WebApi Tests](../../tests/WebApi.Tests/README.md)** - E2E tests for these endpoints

---

## Support

For issues, questions, or contributions, please refer to the main repository documentation.

**Legend:**
- 🔓 = Public endpoint (no authentication required)
- 🔒 = Protected endpoint (JWT token required)