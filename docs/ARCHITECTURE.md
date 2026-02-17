# ExpenseTracker API - Architecture Documentation

## Documentation Navigation

```
📚 Documentation Structure
│
├── 📖 Main README.md
│                                          
├── 📋 docs/ARCHITECTURE.md (You are here) 
│   └── Complete system overview           
│                                          
├── 🔌 API Layer Documentation             
│   └── src/WebApi/README.md              
│       └── All endpoints with examples    
│                                          
├── 📦 Core Layer Documentation            
│   ├── src/Domain/README.md              
│   │   └── Validation rules              
│   ├── src/Application/README.md         
│   │   └── Business logic                
│   ├── src/Infrastructure/README.md      
│   │   └── Cumulative delta system       
│   └── src/Contracts/README.md           
│       └── DTOs and mappings             
│                                          
└── 🧪 Test Documentation                 
    ├── tests/Domain.Tests/README.md      
    ├── tests/Application.Tests/README.md 
    ├── tests/Infrastructure.Tests/README.md
    └── tests/WebApi.Tests/README.md      
```

**Quick Links:**
- 🚀 [Getting Started](../README.md#quickstart-guide)
- 🔌 [API Endpoints](../src/ExpenseTrackerAPI.WebApi/README.md)
- 💡 [Cumulative Delta Explained](../src/ExpenseTrackerAPI.Infrastructure/README.md#cumulative-delta-system)

---

## Overview

ExpenseTracker API is a RESTful web service built with .NET 9 following **Clean Architecture** principles. The application helps users track their expenses and income with features like categorization, grouping, and comprehensive filtering.

## Architecture Layers

The solution follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      WebApi Layer                           │
│              (Controllers, Middleware, Config)              │
├─────────────────────────────────────────────────────────────┤
│                    Application Layer                        │
│                (Services, Interfaces, DTOs)                 │
├─────────────────────────────────────────────────────────────┤
│                     Contracts Layer                         │
│           (Request/Response Models, Validators)             │
├─────────────────────────────────────────────────────────────┤
│                   Infrastructure Layer                      │
│         (Repositories, Database Context, External)          │
├─────────────────────────────────────────────────────────────┤
│                      Domain Layer                           │
│              (Entities, Enums, Domain Errors)               │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

```
expense-tracker/
├── src/
│   ├── ExpenseTrackerAPI.Domain/           # Core business entities
│   ├── ExpenseTrackerAPI.Contracts/        # API contracts (DTOs)
│   ├── ExpenseTrackerAPI.Application/      # Business logic & services
│   ├── ExpenseTrackerAPI.Infrastructure/   # Data access & external services
│   └── ExpenseTrackerAPI.WebApi/           # API endpoints & configuration
├── tests/
│   ├── ExpenseTrackerAPI.Application.Tests/
│   ├── ExpenseTrackerAPI.Infrastructure.Tests/
│   ├── ExpenseTrackerAPI.WebApi.Tests/
│   └── ExpenseTrackerAPI.Domain.Tests/
├── docker/
│   ├── Dockerfile
│   ├── docker-compose.yml                  # Development
│   └── docker-compose.prod.yml             # Production
└── docs/
```

## Layer Details

### 1. Domain Layer (`ExpenseTrackerAPI.Domain`)

The innermost layer containing enterprise business rules. Has no dependencies on other layers.

**Contains:**
- **Entities**: `User`, `Transaction`, `Category`, `TransactionGroup`
- **Enums**: `TransactionType`, `PaymentMethod`
- **Domain Errors**: Strongly-typed error definitions

**Key Principles:**
- No external dependencies
- Pure C# classes
- Business rules and validation logic

### 2. Contracts Layer (`ExpenseTrackerAPI.Contracts`)

Defines the API contract between layers and external consumers.

**Contains:**
- **Request Models**: `CreateTransactionRequest`, `RegisterRequest`, etc.
- **Response Models**: `TransactionResponse`, `LoginResponse`, etc.
- **Mapping Extensions**: Entity-to-Response mappers
- **Validators**: Request validation logic (e.g., `TransactionFilterParser`)

**Key Principles:**
- DTOs only - no business logic
- Validation attributes for input validation
- Mapping utilities

### 3. Application Layer (`ExpenseTrackerAPI.Application`)

Contains application business logic and orchestrates the flow between layers.

**Contains:**
- **Services**: `UserService`, `TransactionService`, `CategoryService`, `TransactionGroupService`
- **Interfaces**: Repository and service contracts
- **Use Cases**: Application-specific business rules

**Key Principles:**
- Depends only on Domain and Contracts
- Defines interfaces that Infrastructure implements
- Contains no framework-specific code

### 4. Infrastructure Layer (`ExpenseTrackerAPI.Infrastructure`)

Implements external concerns like database access and third-party integrations.

**Contains:**
- **Repositories**: `UserRepository`, `TransactionRepository`, etc.
- **Database Context**: `ApplicationDbContext` (Entity Framework Core)
- **Configurations**: EF Core entity configurations
- **Persistence**: Database seeding and migrations

**Key Principles:**
- Implements Application layer interfaces
- Contains EF Core DbContext and migrations
- Handles data persistence

### 5. WebApi Layer (`ExpenseTrackerAPI.WebApi`)

The entry point of the application, handling HTTP requests and responses.

**Contains:**
- **Controllers**: Versioned API endpoints (V1, V2)
- **Middleware**: Custom middleware (e.g., UserContext)
- **Configuration**: `ApiSettings`, JWT settings
- **Extensions**: Dependency injection setup

**Key Principles:**
- Handles HTTP concerns only
- Configures dependency injection
- API versioning support

## Dependency Flow

- **WebApi** depends on Application, Contracts, Infrastructure
- **Application** depends on Domain, Contracts
- **Infrastructure** depends on Domain, Application (for interfaces)
- **Contracts** depends on Domain
- **Domain** has no dependencies

## Key Technologies

| Technology | Purpose |
|------------|---------|
| .NET 9 | Runtime framework |
| ASP.NET Core | Web API framework |
| Entity Framework Core 9 | ORM / Data access |
| PostgreSQL | Database |
| Serilog | Structured logging |
| JWT Bearer | Authentication |
| Swashbuckle | OpenAPI/Swagger |
| xUnit | Testing framework |
| FluentAssertions | Test assertions |
| Testcontainers | Integration testing |
| ErrorOr | Result pattern for error handling |

## API Versioning

The API supports versioning through URL path:
- `/api/v1/...` - Version 1 (current)
- `/api/v2/...` - Version 2 (placeholder)

## Authentication

- **JWT Bearer tokens** for authentication
- Tokens issued on login with configurable expiry
- Protected endpoints require `Authorization: Bearer <token>` header

## Configuration

### Environment-based Configuration

| Environment | Config Source |
|-------------|---------------|
| Development | `appsettings.Development.json` |
| Production | Environment variables (from `.env`) |

### Key Configuration Sections

- **Database**: Connection string, host, credentials
- **JWT**: Secret key, issuer, audience, expiry
- **ApiSettings**: Page size limits

## Docker Deployment

### Development (`docker-compose.yml`)
- Builds from source
- Uses `appsettings.Development.json`
- Hardcoded credentials for local dev

### Production (`docker-compose.prod.yml`)
- Uses pre-built image
- Reads from `.env` file
- Environment variable configuration

### Environment Variables

| Variable | Description |
|----------|-------------|
| `DB_HOST` | Database host (use `postgres` for Docker) |
| `DB_NAME` | Database name |
| `DB_USER` | Database username |
| `DB_PASSWORD` | Database password |
| `DB_PORT` | Database port |
| `DB_SSL_MODE` | SSL mode for connection |
| `JWT_SECRET_KEY` | JWT signing key (min 32 chars) |
| `JWT_ISSUER` | JWT issuer claim |
| `JWT_AUDIENCE` | JWT audience claim |
| `JWT_EXPIRY_HOURS` | Token expiration time |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` |

## Error Handling

The API uses the **ErrorOr** pattern for consistent error handling:
All error responses follow RFC 9110 ProblemDetails format.

## Testing Strategy

| Test Type | Project | Description |
|-----------|---------|-------------|
| Unit Tests | `Application.Tests` | Service layer testing with mocks |
| Repository Tests | `Infrastructure.Tests` | Database integration tests |
| E2E Tests | `WebApi.Tests` | Full API endpoint testing |
| Domain Tests | `Domain.Tests` | Domain logic testing |

Integration tests use **Testcontainers** to spin up real PostgreSQL instances.

## Health Checks

- `GET /health` - Simple health check (for Docker/K8s)
- `GET /api/v1/health` - Detailed health with database status

## Database Schema

```
┌──────────────┐     ┌──────────────────┐     ┌─────────────┐
│    Users     │────<│   Transactions   │>────│  Categories │
└──────────────┘     └──────────────────┘     └─────────────┘
                            │
                            │
                     ┌──────────────────┐
                     │TransactionGroups │
                     └──────────────────┘
```

- **Users**: User accounts with authentication
- **Transactions**: Expenses and income entries
- **Categories**: Transaction categorization
- **TransactionGroups**: Logical grouping of transactions

## Getting Started

### Local Development

```bash
# Run with .NET
dotnet run --project src/ExpenseTrackerAPI.WebApi

# Run with Docker (Development)
docker compose -f docker/docker-compose.yml up --build -d
```

### Production Deployment

```bash
# Configure .env file
cp .env.example .env
# Edit .env with production values

# Run with Docker (Production)
docker compose -f docker/docker-compose.prod.yml up -d
```

### Reset the API and database 

```bash
docker compose -f docker/docker-compose.prod.yml down -v
docker compose -f docker/docker-compose.prod.yml up -d
```

## Documentation Index

Comprehensive documentation is available for each layer of the application:

### 📚 Getting Started
- **[Main README](../README.md)** - Quickstart guide and setup instructions

### 🔌 API Documentation
- **[WebApi Layer](../src/ExpenseTrackerAPI.WebApi/README.md)** - Complete API endpoint reference
  - All endpoints with request/response examples
  - Authentication (register, login, JWT)
  - User management (profile, delete)
  - Transactions (CRUD, filtering, pagination)
  - Transaction Groups (organization)
  - Categories (classification)
  - Health checks
  - Error response formats
  - Quick start examples with curl

### 📦 Layer Documentation

- **[Domain Layer](../src/ExpenseTrackerAPI.Domain/README.md)** - Business entities and validation rules
  - User validation (name, email, password)
  - Transaction validation (amount, date, subject)
  - Business rule enforcement
  - Domain errors
  - Validation philosophy
  
- **[Application Layer](../src/ExpenseTrackerAPI.Application/README.md)** - Business logic and orchestration
  - User Service (registration, authentication, profile management)
  - Transaction Service (CRUD operations, filtering)
  - Security (BCrypt, JWT tokens)
  - Error handling patterns (ErrorOr)
  - Service patterns and best practices
  
- **[Infrastructure Layer](../src/ExpenseTrackerAPI.Infrastructure/README.md)** - Data access and persistence
  - Repository implementations
  - **Cumulative delta system** (detailed explanation with examples)
  - Transaction balance calculations (create, update, delete flows)
  - Database schema and indexes
  - Performance considerations
  - Bulk update operations
  
- **[Contracts Layer](../src/ExpenseTrackerAPI.Contracts/README.md)** - API contracts and DTOs
  - Request/Response models for all endpoints
  - Validation attributes
  - Mapping extensions
  - Partial update patterns
  - Best practices

### 🧪 Testing Documentation

- **[Domain Tests](../tests/ExpenseTrackerAPI.Domain.Tests/README.md)** - Entity validation tests
  - User and Transaction entity tests
  - Pure unit tests (no dependencies)
  - Business rule validation
  
- **[Application Tests](../tests/ExpenseTrackerAPI.Application.Tests/README.md)** - Service layer tests
  - Mocked repository tests
  - Password hashing and JWT validation
  - Filtering and pagination logic
  
- **[Infrastructure Tests](../tests/ExpenseTrackerAPI.Infrastructure.Tests/README.md)** - Repository tests
  - Real PostgreSQL database tests
  - Cumulative delta calculations
  - Testcontainers setup
  - Foreign key constraints
  
- **[WebApi Tests](../tests/ExpenseTrackerAPI.WebApi.Tests/README.md)** - E2E API tests
  - Full HTTP request/response cycle
  - Authentication with JWT tokens
  - Error format validation (RFC 7807)
  - Complete user journey workflows

---

**Quick Links:**
- [Swagger UI](http://localhost:5000/swagger) - Interactive API documentation (when running)
- [API Endpoints Reference](../src/ExpenseTrackerAPI.WebApi/README.md) - Complete endpoint documentation
- [Cumulative Delta Explained](../src/ExpenseTrackerAPI.Infrastructure/README.md#cumulative-delta-system) - Core balance tracking system
