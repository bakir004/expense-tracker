FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SampleCkWebApp/src/SampleCkWebApp.WebApi/SampleCkWebApp.WebApi.csproj", "SampleCkWebApp/src/SampleCkWebApp.WebApi/"]
COPY ["SampleCkWebApp/src/SampleCkWebApp.Application/SampleCkWebApp.Application.csproj", "SampleCkWebApp/src/SampleCkWebApp.Application/"]
COPY ["SampleCkWebApp/src/SampleCkWebApp.Domain/SampleCkWebApp.Domain.csproj", "SampleCkWebApp/src/SampleCkWebApp.Domain/"]
COPY ["SampleCkWebApp/src/SampleCkWebApp.Contracts/SampleCkWebApp.Contracts.csproj", "SampleCkWebApp/src/SampleCkWebApp.Contracts/"]
COPY ["SampleCkWebApp/src/SampleCkWebApp.Infrastructure/SampleCkWebApp.Infrastructure.csproj", "SampleCkWebApp/src/SampleCkWebApp.Infrastructure/"]
RUN dotnet restore "SampleCkWebApp/src/SampleCkWebApp.WebApi/SampleCkWebApp.WebApi.csproj"
COPY . .
WORKDIR "/src/SampleCkWebApp/src/SampleCkWebApp.WebApi"
RUN dotnet build "./SampleCkWebApp.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SampleCkWebApp.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SampleCkWebApp.WebApi.dll"]
