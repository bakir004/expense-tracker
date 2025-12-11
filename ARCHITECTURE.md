# Project Architecture Documentation

This document explains the folder structure and what should be contained in each directory of the SampleCkWebApp solution.

## Solution Overview

This project follows **Clean Architecture** (also known as Onion Architecture) principles, which separates the application into distinct layers with clear dependencies and responsibilities.

## Folder Structure

### `/SampleCkWebApp/src/` - Source Code Directory

This directory contains all the main application projects organized by layer.

---

## 1. `SampleCkWebApp.Domain` - Domain Layer

**Purpose**: Contains the core business logic and domain entities. This is the innermost layer with no dependencies on other application layers.

**What should be here:**
- **Domain Entities**: Core business objects and value objects
- **Domain Errors**: Error definitions specific to domain rules (e.g., `MessageHistoryErrors.cs`)
- **Domain Services**: Business logic that doesn't naturally fit into entities
- **Domain Events**: Events that represent something that happened in the domain
- **Value Objects**: Immutable objects that represent domain concepts
- **Domain Exceptions**: Custom exceptions for domain-specific errors

**What should NOT be here:**
- Infrastructure concerns (database, HTTP, file system)
- Application services
- DTOs or ViewModels
- Framework-specific code

**Example from this project:**
- `Errors/MessageHistoryErrors.cs` - Domain-specific error definitions

---

## 2. `SampleCkWebApp.Contracts` - Contracts Layer

**Purpose**: Defines the API contracts (request/response models) that are shared between the API layer and external consumers.

**What should be here:**
- **Request Models**: DTOs for incoming API requests (e.g., `GetMessageHistoryRequest.cs`)
- **Response Models**: DTOs for API responses (e.g., `GetMessageHistoryResponse.cs`, `HistoricalMessage.cs`)
- **API Contracts**: Interfaces or models that define the shape of data exchanged via the API
- **Shared Models**: Data transfer objects used across API boundaries

**What should NOT be here:**
- Business logic
- Domain entities
- Infrastructure implementations
- Internal application models

**Example from this project:**
- `MessageHistory/GetMessageHistoryRequest.cs` - Request DTO
- `MessageHistory/GetMessageHistoryResponse.cs` - Response DTO
- `MessageHistory/HistoricalMessage.cs` - Response data model

---

## 3. `SampleCkWebApp.Application` - Application Layer

**Purpose**: Contains application-specific business logic, use cases, and orchestrates domain objects. This layer depends on the Domain layer but not on Infrastructure or WebApi.

**What should be here:**
- **Application Services**: Services that implement use cases and orchestrate domain logic (e.g., `MessageHistoryService.cs`)
- **Interfaces/Application**: Application service interfaces (e.g., `IMessageHistoryService.cs`)
- **Interfaces/Infrastructure**: Interfaces for infrastructure dependencies (e.g., `IMessageHistoryRepository.cs`)
- **Data Models**: Application-specific data models and DTOs (e.g., `GetMessageHistoryResult.cs`, `MessageHistoryRecord.cs`)
- **Validators**: Application-level validation logic (e.g., `MessageHistoryValidator.cs`)
- **DependencyInjection.cs**: Registration of application services
- **Mappers**: Mapping logic between domain and application models
- **Use Cases**: Individual use case implementations

**What should NOT be here:**
- Infrastructure implementations (database access, external API calls)
- Web/HTTP specific code
- Framework-specific dependencies (except for abstractions)

**Example from this project:**
- `MessageHistory/MessageHistoryService.cs` - Application service
- `MessageHistory/Interfaces/Application/IMessageHistoryService.cs` - Service interface
- `MessageHistory/Interfaces/Infrastructure/IMessageHistoryRepository.cs` - Repository interface
- `MessageHistory/Data/` - Application data models
- `MessageHistory/MessageHistoryValidator.cs` - Validation logic

---

## 4. `SampleCkWebApp.Infrastructure` - Infrastructure Layer

**Purpose**: Implements technical concerns and external integrations. This layer depends on Application and Domain layers.

**What should be here:**
- **Repository Implementations**: Concrete implementations of repository interfaces (e.g., `MessageHistoryRepository.cs`)
- **Database Contexts**: Entity Framework DbContext or similar
- **External Service Clients**: HTTP clients, third-party API integrations
- **Options/Configuration**: Configuration classes for infrastructure (e.g., `MessageHistoryOptions.cs`)
- **DependencyInjection.cs**: Registration of infrastructure services
- **Caching Implementations**: Redis, in-memory cache implementations
- **Message Queue Implementations**: RabbitMQ, Azure Service Bus, etc.
- **File System Access**: File I/O implementations
- **Email/SMS Services**: External communication service implementations

**What should NOT be here:**
- Business logic
- Domain entities (only references)
- Web/HTTP controllers
- Application services

**Example from this project:**
- `MessageHistory/MessageHistoryRepository.cs` - Repository implementation
- `MessageHistory/Options/MessageHistoryOptions.cs` - Configuration options
- `MessageHistory/DependencyInjection.cs` - Infrastructure service registration

---

## 5. `SampleCkWebApp.WebApi` - Presentation/API Layer

**Purpose**: Handles HTTP requests, routing, and API-specific concerns. This is the outermost layer.

**What should be here:**
- **Controllers**: API endpoints and request handling (e.g., `MessageHistoryController.cs`, `HealthController.cs`)
- **Base Controllers**: Shared controller functionality (e.g., `AppControllerBase.cs` / `ApiControllerBase.cs`)
- **Mappings**: Mapping between contracts and application models (e.g., `DeviceHistoryMappings.cs`)
- **Middleware**: Custom middleware for cross-cutting concerns
- **DependencyInjection.cs**: Registration of API-specific services
- **Options/**: API-specific configuration classes
- **Program.cs**: Application entry point and startup configuration
- **appsettings.json**: Application configuration files
- **Dockerfile**: Containerization configuration
- **Extensions**: Helper extension methods for HTTP context, diagnostics, etc.

**What should NOT be here:**
- Business logic (delegate to Application layer)
- Database access (delegate to Infrastructure layer)
- Domain entities (use DTOs/Contracts instead)

**Example from this project:**
- `Controllers/MessageHistory/MessageHistoryController.cs` - API controller
- `Controllers/HealthController.cs` - Health check endpoint
- `Controllers/AppControllerBase.cs` - Base controller with error handling
- `Mappings/DeviceHistoryMappings.cs` - DTO mapping logic
- `Program.cs` - Application startup
- `DependencyInjection.cs` - API service registration

---

## Additional Directories

### `/docker/` - Docker Configuration

**Purpose**: Contains Docker-related configuration files.

**What should be here:**
- **build/**: Dockerfiles for building application images
- **compose/**: Docker Compose files for orchestrating services

---

### `/scripts/` - Build and Deployment Scripts

**Purpose**: Contains utility scripts for building, testing, and deploying the application.

**What should be here:**
- Build scripts (e.g., `build.sh`)
- Deployment scripts
- Database migration scripts
- Test execution scripts

---

## Dependency Flow

The dependency flow follows Clean Architecture principles:

```
WebApi (outermost)
    ↓ depends on
Application
    ↓ depends on
Domain (innermost)

Infrastructure
    ↓ depends on
Application
    ↓ depends on
Domain

Contracts
    ↑ used by
WebApi
```

**Key Rules:**
1. **Domain** has no dependencies on other layers
2. **Application** depends only on **Domain**
3. **Infrastructure** depends on **Application** and **Domain**
4. **WebApi** depends on **Application**, **Domain**, and **Contracts**
5. **Contracts** is used by **WebApi** but has minimal dependencies

---

## Health Endpoint

A `/health` endpoint has been added to the application at `Controllers/HealthController.cs`. This endpoint:

- Returns a simple health status
- Includes a timestamp
- Includes version information
- Is excluded from request logging (as configured in `Program.cs`)

**Testing the Health Endpoint:**

Once the application is running, you can test it using:

```bash
# Using curl
curl http://localhost:5000/health

# Using PowerShell
Invoke-WebRequest -Uri http://localhost:5000/health

# Expected response:
{
  "status": "healthy",
  "timestamp": "2024-01-01T12:00:00Z",
  "version": "1.0.0"
}
```

**Note**: The application may require a database connection to start fully. Ensure your database is configured in `appsettings.json` before running the application.

---

## Best Practices

1. **Separation of Concerns**: Each layer should only contain code relevant to its responsibility
2. **Dependency Inversion**: Depend on abstractions (interfaces), not concrete implementations
3. **Single Responsibility**: Each class should have one reason to change
4. **Interface Segregation**: Keep interfaces focused and specific
5. **Don't Repeat Yourself (DRY)**: Share common code through base classes or shared projects

---

## Summary

This architecture provides:
- **Maintainability**: Clear separation makes code easier to understand and modify
- **Testability**: Layers can be tested independently with mocked dependencies
- **Flexibility**: Infrastructure can be swapped without affecting business logic
- **Scalability**: Clear boundaries make it easier to scale different parts of the system

