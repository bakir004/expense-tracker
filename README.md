# Expense Tracker

Simple .NET expense tracker for managing your finances.

### Installation

Make sure you have `docker` installed. Navigate to `/docker/compose` and run:

```bash
docker compose up -d
```

this will spin up a database and the backend API on port 8080.

On startup, the database will be populated with the necessary tables and seeded with some dummy data. To see what exactly gets created, the database initialization script is at `/scripts/database.sql`.

To see what the API has to offer, Swagger is enabled at route `/swagger`.

## Database Setup

### Using Neon PostgreSQL (Cloud)

1. Set the `DATABASE_URL` environment variable:

   ```bash
   export DATABASE_URL="postgresql://user:password@host:port/database?sslmode=require"
   ```

2. Initialize the database schema:

   ```bash
   # Linux/Mac
   ./scripts/init-db.sh

   # Windows PowerShell
   .\scripts\init-db.ps1
   ```

3. Run the application - it will automatically use the `DATABASE_URL` environment variable.

### Using Local PostgreSQL

For local development, update `appsettings.development.json` with your local database connection string, or set the `DATABASE_URL` environment variable.

## Documentation

For detailed API documentation, see [SampleCkWebApp/docs/API.md](SampleCkWebApp/docs/API.md).
