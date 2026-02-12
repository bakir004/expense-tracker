FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/ExpenseTrackerAPI.WebApi/ExpenseTrackerAPI.WebApi.csproj src/ExpenseTrackerAPI.WebApi/
COPY src/ExpenseTrackerAPI.Application/ExpenseTrackerAPI.Application.csproj src/ExpenseTrackerAPI.Application/
COPY src/ExpenseTrackerAPI.Domain/ExpenseTrackerAPI.Domain.csproj src/ExpenseTrackerAPI.Domain/
COPY src/ExpenseTrackerAPI.Contracts/ExpenseTrackerAPI.Contracts.csproj src/ExpenseTrackerAPI.Contracts/
COPY src/ExpenseTrackerAPI.Infrastructure/ExpenseTrackerAPI.Infrastructure.csproj src/ExpenseTrackerAPI.Infrastructure/

RUN dotnet restore src/ExpenseTrackerAPI.WebApi/ExpenseTrackerAPI.WebApi.csproj

COPY src/ src/

WORKDIR /src/ExpenseTrackerAPI.WebApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExpenseTrackerAPI.WebApi.dll"]
