# RFC 9110 Error Format Tests

This directory contains comprehensive tests for validating that all API error responses conform to RFC 9110 (HTTP Semantics) standards and follow consistent error formatting conventions.

## Overview

These tests ensure that:

1. **RFC 9110 Compliance**: All error responses follow the ProblemDetails format as specified in RFC 7807/9110
2. **Consistent Status Codes**: HTTP status codes correctly represent the error type (400, 401, 404, 409, etc.)
3. **No Dots in Error Keys**: Validation error keys do not contain dots (e.g., use `Email` not `User.Email`)
4. **Required Fields**: All error responses include required fields (status, title/detail, traceId)
5. **Structured Validation Errors**: ValidationProblemDetails format is used for input validation errors

## Test Structure

### Error Response Models

**`ErrorResponseModels.cs`**
- `ProblemDetailsResponse`: Standard RFC 9110 error format
- `ValidationProblemDetailsResponse`: Validation-specific error format (extends ProblemDetailsResponse)
- `ErrorResponseValidator`: Helper methods for validating error response structure

### Test Files by Endpoint

1. **`AuthErrorFormatTests.cs`**
   - Registration endpoint errors (empty fields, weak passwords, duplicates)
   - Login endpoint errors (invalid credentials, missing fields)
   - RFC 9110 compliance validation

2. **`TransactionErrorFormatTests.cs`**
   - Create/Update/Delete transaction errors
   - Invalid transaction types, amounts, payment methods
   - Query parameter validation errors
   - Pagination errors

3. **`UserErrorFormatTests.cs`**
   - Profile update errors (invalid email, weak password, duplicate email)
   - Profile deletion errors (wrong password, missing confirmation)
   - Authentication and authorization errors

4. **`TransactionGroupErrorFormatTests.cs`**
   - Create/Update/Delete transaction group errors
   - Name and description validation
   - ID validation errors

## Error Format Standards

### ProblemDetails Format (RFC 9110)

All error responses must include:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-1234567890abcdef-1234567890abcdef-00",
  "errors": {
    "Email": ["Email format is invalid."],
    "Password": ["Password must be at least 8 characters long."]
  }
}
```

**Required Fields:**
- `status`: HTTP status code (must match response status)
- `title` or `detail`: Human-readable error description
- `traceId`: Request correlation ID for debugging

**Optional Fields:**
- `type`: URI reference identifying the error type
- `instance`: URI reference identifying the specific occurrence
- `errors`: Dictionary of field-specific validation errors (ValidationProblemDetails only)

### Error Key Naming Convention

**✅ CORRECT** - No dots in error keys:
```json
{
  "errors": {
    "Email": ["Email is required"],
    "Password": ["Password must be strong"],
    "CategoryId": ["Category not found"]
  }
}
```

**❌ INCORRECT** - Contains dots (nested paths):
```json
{
  "errors": {
    "User.Email": ["Email is required"],
    "Request.Password": ["Password must be strong"],
    "Transaction.CategoryId": ["Category not found"]
  }
}
```

### HTTP Status Code Mapping

| Status Code | Error Type | When to Use |
|-------------|------------|-------------|
| 400 Bad Request | Validation | Invalid input data, malformed requests |
| 401 Unauthorized | Authentication | Invalid credentials, missing/invalid token |
| 403 Forbidden | Authorization | Authenticated but not authorized |
| 404 Not Found | Not Found | Resource doesn't exist |
| 409 Conflict | Conflict | Duplicate resource (e.g., email already exists) |
| 422 Unprocessable Entity | Failure | Semantic validation errors |
| 500 Internal Server Error | Unexpected | Unhandled exceptions |

## Running the Tests

### Run all error format tests:
```bash
dotnet test --filter "FullyQualifiedName~ErrorFormatTests"
```

### Run tests for specific endpoint:
```bash
dotnet test --filter "FullyQualifiedName~AuthErrorFormatTests"
dotnet test --filter "FullyQualifiedName~TransactionErrorFormatTests"
dotnet test --filter "FullyQualifiedName~UserErrorFormatTests"
dotnet test --filter "FullyQualifiedName~TransactionGroupErrorFormatTests"
```

### Run specific test category:
```bash
# Test only validation error formats
dotnet test --filter "FullyQualifiedName~ValidationErrors_ShouldNeverContainDotsInErrorKeys"

# Test only RFC 9110 compliance
dotnet test --filter "FullyQualifiedName~RFC9110Compliance"
```

## Test Coverage

### Authentication Endpoints
- ✅ POST /api/v1/auth/register - All validation errors
- ✅ POST /api/v1/auth/login - All authentication errors
- ✅ GET /api/v1/auth/health - Error handling (if applicable)

### Transaction Endpoints
- ✅ POST /api/v1/transactions - Create validation errors
- ✅ GET /api/v1/transactions/{id} - Not found errors
- ✅ GET /api/v1/transactions - Query parameter validation
- ✅ PUT /api/v1/transactions/{id} - Update validation errors
- ✅ DELETE /api/v1/transactions/{id} - Delete errors

### User Endpoints
- ✅ PUT /api/v1/users/profile - Profile update errors
- ✅ DELETE /api/v1/users/profile - Profile deletion errors

### Transaction Group Endpoints
- ✅ POST /api/v1/transaction-groups - Create validation errors
- ✅ GET /api/v1/transaction-groups/{id} - Not found errors
- ✅ PUT /api/v1/transaction-groups/{id} - Update validation errors
- ✅ DELETE /api/v1/transaction-groups/{id} - Delete errors

### Category Endpoints
- ✅ Category error format testing (if applicable)

## Example Test Cases

### Validation Error Test
```csharp
[Fact]
public async Task Register_WithEmptyName_ShouldReturnValidationProblemDetailsWithoutDotsInKeys()
{
    // Arrange
    var request = new RegisterRequest("", "valid@email.com", "Password123!", null);

    // Act
    var response = await Client.PostAsJsonAsync("/api/v1/auth/register", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    
    var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsResponse>();
    
    // Validate RFC 9110 compliance
    ErrorResponseValidator.IsValidValidationProblemDetails(problemDetails).Should().BeTrue();
    
    // Validate no dots in error keys
    ErrorResponseValidator.AllKeysAreValid(problemDetails!.Errors).Should().BeTrue();
    
    problemDetails.Errors.Should().ContainKey("Name");
}
```

### Not Found Error Test
```csharp
[Fact]
public async Task GetTransaction_WithInvalidId_ShouldReturnNotFoundProblemDetailsWithoutDotsInKeys()
{
    // Arrange
    var nonExistentId = 999999;

    // Act
    var response = await Client.GetAsync($"/api/v1/transactions/{nonExistentId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    
    var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
    
    ErrorResponseValidator.IsValidProblemDetails(problemDetails).Should().BeTrue();
    
    problemDetails!.Status.Should().Be(404);
}
```

## Why These Tests Matter

1. **API Consistency**: Ensures all endpoints return errors in the same format
2. **Client Integration**: Makes it easier for frontend/mobile apps to handle errors uniformly
3. **Standards Compliance**: Follows RFC 9110 for HTTP semantics and error responses
4. **Debugging**: TraceId and proper status codes help with troubleshooting
5. **Developer Experience**: Clear, consistent error messages improve DX

## References

- [RFC 9110 - HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110.html)
- [RFC 7807 - Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc7807.html)
- [ASP.NET Core ProblemDetails](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails)
- [ASP.NET Core ValidationProblemDetails](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.validationproblemdetails)

## Maintenance

When adding new endpoints or modifying existing error responses:

1. Add corresponding error format tests in this directory
2. Ensure error keys don't contain dots
3. Use appropriate HTTP status codes
4. Include all required ProblemDetails fields
5. Add examples to this README if introducing new error patterns