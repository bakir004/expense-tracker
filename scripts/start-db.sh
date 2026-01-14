#!/bin/bash
# Start PostgreSQL container with database initialization script

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Remove existing container if it exists
docker rm -f postgres-db 2>/dev/null || true

# Start PostgreSQL with volume mount for init script
docker run --name postgres-db \
  -e POSTGRES_USER=myuser \
  -e POSTGRES_PASSWORD=mypassword \
  -e POSTGRES_DB=mydatabase \
  -p 5432:5432 \
  -v "$PROJECT_ROOT/scripts/database.sql:/docker-entrypoint-initdb.d/01-init.sql:ro" \
  -d postgres:latest

echo "PostgreSQL container started. Waiting for initialization..."
sleep 3
docker logs postgres-db --tail 20

