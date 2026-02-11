> [!IMPORTANT]
> VVS INFO!!!
> Verzija Expense Tracker API-ja koja je koristena u Tasku 3 je na branchu `VVS`

# Expense Tracker

Simple .NET expense tracker for managing your finances.

### Installation

Make sure you have `docker` installed. From the repo root, run:

```bash
docker compose -f ExpenseTrackerAPI/docker/compose/docker-compose.yml up -d
```

This will spin up a database and the backend API on port 8080.

On startup, the API applies Entity Framework Core migrations automatically, so the database schema is created or updated without any manual SQL. No init script is required.

To see what the API has to offer, Swagger is enabled at route `/swagger`.

## Development (from repo root)

**Run the API:**
```bash
dotnet run --project ExpenseTrackerAPI/src/ExpenseTrackerAPI.WebApi/ExpenseTrackerAPI.WebApi.csproj
# or
./run.sh
```

**Build:**
```bash
dotnet build ExpenseTrackerAPI.sln
```

**Run tests:**
```bash
dotnet test ExpenseTrackerAPI.sln
```

## Database Setup

The database schema is created and updated by **EF Core migrations** when the API starts. Ensure the API can connect to PostgreSQL; migrations run automatically on startup.

### Using Neon PostgreSQL (Cloud)

1. Set the `DATABASE_URL` environment variable:

   ```bash
   export DATABASE_URL="postgresql://user:password@host:port/database?sslmode=require"
   ```

2. Run the application; it will apply pending migrations and use `DATABASE_URL`.

### Using Local PostgreSQL

For local development, set the connection string in `appsettings.json` (or `appsettings.Development.json`) under `Database:ConnectionString`, or set the `DATABASE_URL` environment variable. You can start a local Postgres with:

```bash
./ExpenseTrackerAPI/scripts/start-db.sh
```

(Start-db.sh may mount an optional init script; the API will still apply migrations and own the schema.)

## Documentation

For detailed API documentation, see [ExpenseTrackerAPI/docs/API.md](ExpenseTrackerAPI/docs/API.md).
