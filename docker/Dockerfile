# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files for restore
COPY src/ExpenseTrackerAPI.Domain/ExpenseTrackerAPI.Domain.csproj ./src/ExpenseTrackerAPI.Domain/
COPY src/ExpenseTrackerAPI.Contracts/ExpenseTrackerAPI.Contracts.csproj ./src/ExpenseTrackerAPI.Contracts/
COPY src/ExpenseTrackerAPI.Application/ExpenseTrackerAPI.Application.csproj ./src/ExpenseTrackerAPI.Application/
COPY src/ExpenseTrackerAPI.Infrastructure/ExpenseTrackerAPI.Infrastructure.csproj ./src/ExpenseTrackerAPI.Infrastructure/
COPY src/ExpenseTrackerAPI.WebApi/ExpenseTrackerAPI.WebApi.csproj ./src/ExpenseTrackerAPI.WebApi/

# Restore dependencies for WebApi project (will restore all dependencies)
WORKDIR /src/src/ExpenseTrackerAPI.WebApi
RUN dotnet restore

# Copy remaining source code
WORKDIR /src
COPY src/ ./src/

# Build and publish
WORKDIR /src/src/ExpenseTrackerAPI.WebApi
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose ports
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "ExpenseTrackerAPI.WebApi.dll"]
