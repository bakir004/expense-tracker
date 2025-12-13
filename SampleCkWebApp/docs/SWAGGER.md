# Swagger API Documentation Guide

This document explains how to access and use the Swagger UI for the Expense Tracker API.

## Accessing Swagger UI

Once the application is running, you can access the Swagger UI at:

```
http://localhost:5000/swagger
```

## Features

The Swagger documentation includes:

### 1. **API Information**
   - Title: Expense Tracker API
   - Version: v1
   - Description: A RESTful API for managing expenses, users, and categories

### 2. **Endpoint Documentation**
   Each endpoint includes:
   - **Summary**: Brief description of what the endpoint does
   - **Parameters**: Detailed parameter descriptions with examples
   - **Request Body**: Schema and examples for POST requests
   - **Response Codes**: All possible HTTP status codes
   - **Response Schema**: Detailed response models with examples

### 3. **Interactive Testing**
   - **Try it out**: Test endpoints directly from the browser
   - **Execute**: Send requests and see real responses
   - **Request/Response Examples**: Pre-filled examples for quick testing

## Available Endpoints

### Users API

#### GET /users
- Retrieves all users from the system
- Returns: List of users with total count

#### GET /users/{id}
- Retrieves a specific user by ID
- Parameters: `id` (integer, required)
- Returns: User information

#### POST /users
- Creates a new user
- Request Body:
  ```json
  {
    "name": "John Doe",
    "email": "john.doe@example.com",
    "password": "securepassword123"
  }
  ```
- Returns: Created user information

### Health Check

#### GET /health
- Returns application health status
- Returns: Status, timestamp, and version

## Using Swagger UI

### 1. **Viewing Endpoints**
   - Expand any endpoint to see its details
   - Click "Try it out" to enable interactive testing

### 2. **Testing Endpoints**

   **For GET requests:**
   1. Click "Try it out"
   2. Fill in any required parameters
   3. Click "Execute"
   4. View the response below

   **For POST requests:**
   1. Click "Try it out"
   2. Modify the request body JSON (examples are provided)
   3. Click "Execute"
   4. View the response and status code

### 3. **Response Information**
   - **Status Code**: HTTP status code (200, 201, 400, etc.)
   - **Response Body**: JSON response data
   - **Response Headers**: HTTP headers returned

### 4. **Schema Documentation**
   - Click on any model name to see its structure
   - View property types, descriptions, and examples
   - Understand required vs optional fields

## Example: Creating a User

1. Navigate to `POST /users` endpoint
2. Click "Try it out"
3. Modify the request body:
   ```json
   {
     "name": "Jane Smith",
     "email": "jane.smith@example.com",
     "password": "mypassword123"
   }
   ```
4. Click "Execute"
5. Review the response:
   - Status: `201 Created`
   - Response body contains the created user (without password)

## Response Codes

The API uses standard HTTP status codes:

- **200 OK**: Successful GET request
- **201 Created**: Successful POST request (resource created)
- **400 Bad Request**: Validation error
- **404 Not Found**: Resource not found
- **409 Conflict**: Duplicate resource (e.g., email already exists)
- **500 Internal Server Error**: Server error

## Tips

1. **Hide Schemas**: Schemas are hidden by default. Click "Models" to view them.
2. **Filter Endpoints**: Use the search box to filter endpoints
3. **Download OpenAPI Spec**: The OpenAPI JSON specification is available at `/swagger/v1/swagger.json`
4. **Request Duration**: The UI shows request duration for each call

## OpenAPI Specification

The OpenAPI 3.0 specification is available at:
```
http://localhost:5000/swagger/v1/swagger.json
```

You can use this JSON file with:
- Postman (import as OpenAPI)
- Other API testing tools
- Code generation tools
- API documentation generators

## Troubleshooting

### Swagger UI Not Loading
- Ensure the application is running
- Check that port 5000 is not blocked
- Verify `appsettings.json` configuration

### XML Comments Not Showing
- Ensure the project builds successfully
- XML documentation files should be generated in `bin/Debug/net8.0/`
- Check that `GenerateDocumentationFile` is set to `true` in `.csproj`

### Endpoints Not Appearing
- Verify controllers inherit from `ApiControllerBase` or `ControllerBase`
- Check that routes are properly configured
- Ensure `[ApiController]` attribute is present

## Configuration

Swagger configuration is located in `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Expense Tracker API",
        Version = "v1",
        Description = "A RESTful API for managing expenses, users, and categories..."
    });
    
    // Includes XML comments from assemblies
    c.IncludeXmlComments(xmlPath);
});
```

Swagger UI configuration:
```csharp
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Tracker API v1");
    c.RoutePrefix = "swagger"; // Available at /swagger
    c.DocumentTitle = "Expense Tracker API Documentation";
});
```

