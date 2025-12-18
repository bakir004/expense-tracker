FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files first for better layer caching
COPY SampleCkWebApp/src/SampleCkWebApp.WebApi/SampleCkWebApp.WebApi.csproj SampleCkWebApp/src/SampleCkWebApp.WebApi/
COPY SampleCkWebApp/src/SampleCkWebApp.Application/SampleCkWebApp.Application.csproj SampleCkWebApp/src/SampleCkWebApp.Application/
COPY SampleCkWebApp/src/SampleCkWebApp.Domain/SampleCkWebApp.Domain.csproj SampleCkWebApp/src/SampleCkWebApp.Domain/
COPY SampleCkWebApp/src/SampleCkWebApp.Contracts/SampleCkWebApp.Contracts.csproj SampleCkWebApp/src/SampleCkWebApp.Contracts/
COPY SampleCkWebApp/src/SampleCkWebApp.Infrastructure/SampleCkWebApp.Infrastructure.csproj SampleCkWebApp/src/SampleCkWebApp.Infrastructure/

# Restore dependencies
RUN dotnet restore SampleCkWebApp/src/SampleCkWebApp.WebApi/SampleCkWebApp.WebApi.csproj

# Copy all source code
COPY SampleCkWebApp/src/ SampleCkWebApp/src/

# Build and publish
WORKDIR /src/SampleCkWebApp/src/SampleCkWebApp.WebApi
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SampleCkWebApp.WebApi.dll"]
