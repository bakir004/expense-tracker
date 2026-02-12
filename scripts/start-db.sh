#!/bin/bash
# Start PostgreSQL container for local development.
#
# The database is left empty. When you run the API, it will:
#   1. Apply EF Core migrations (create/update tables)
#   2. Seed demo data if the database is empty (users, categories, groups, sample transactions)
#
# Demo users have password: password123

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Remove existing container if it exists
docker rm -f postgres-db 2>/dev/null || true

# Start PostgreSQL (no init script; API handles schema + seed via EF Core)
docker run --name postgres-db \
  -e POSTGRES_USER=postgres\
  -e POSTGRES_PASSWORD=postgres\
  -e POSTGRES_DB=expense-tracker-db\
  -p 5432:5432 \
  -d postgres:latest

echo "PostgreSQL container started. Run the API to apply migrations and seed the database."
sleep 3
docker logs postgres-db --tail 20
