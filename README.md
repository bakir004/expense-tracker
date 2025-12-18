# Expense Tracker

Simple .NET expense tracker for managing your finances.

### Installation

Make sure you have `docker` and `docker compose` installed. Navigate to `/docker/compose` and run:

```bash
docker compose up -d
```

this will spin up a database and the backend API.

On startup, the database will be populated with the necessary tables and seeded with some dummy data. To see what exactly gets created, the database initialization script is at `/scripts/database.sql`.
