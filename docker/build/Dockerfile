FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files first for better layer caching
COPY ExpenseTrackerAPI/src/ExpenseTrackerAPI.WebApi/ExpenseTrackerAPI.WebApi.csproj ExpenseTrackerAPI/src/ExpenseTrackerAPI.WebApi/
COPY ExpenseTrackerAPI/src/ExpenseTrackerAPI.Application/ExpenseTrackerAPI.Application.csproj ExpenseTrackerAPI/src/ExpenseTrackerAPI.Application/
COPY ExpenseTrackerAPI/src/ExpenseTrackerAPI.Domain/ExpenseTrackerAPI.Domain.csproj ExpenseTrackerAPI/src/ExpenseTrackerAPI.Domain/
COPY ExpenseTrackerAPI/src/ExpenseTrackerAPI.Contracts/ExpenseTrackerAPI.Contracts.csproj ExpenseTrackerAPI/src/ExpenseTrackerAPI.Contracts/
COPY ExpenseTrackerAPI/src/ExpenseTrackerAPI.Infrastructure/ExpenseTrackerAPI.Infrastructure.csproj ExpenseTrackerAPI/src/ExpenseTrackerAPI.Infrastructure/

# Restore dependencies
RUN dotnet restore ExpenseTrackerAPI/src/ExpenseTrackerAPI.WebApi/ExpenseTrackerAPI.WebApi.csproj

# Copy all source code
COPY ExpenseTrackerAPI/src/ ExpenseTrackerAPI/src/

# Build and publish
WORKDIR /src/ExpenseTrackerAPI/src/ExpenseTrackerAPI.WebApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpenseTrackerAPI.WebApi.dll"]

