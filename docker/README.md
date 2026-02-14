# Docker Deployment Guide

This directory contains Docker configuration files for building and running the ExpenseTracker API.

## Files

- `Dockerfile` - Multi-stage Docker build configuration
- `docker-compose.yml` - Docker Compose orchestration for API + PostgreSQL

## Prerequisites

- Docker 20.10 or higher
- Docker Compose 2.0 or higher

## Quick Start

### 1. Build and Run with Docker Compose

From the project root directory:

```bash
# Build and start all services
docker compose -f docker/docker-compose.yml up --build

# Or run in detached mode
docker compose -f docker/docker-compose.yml up -d --build
```

The API will be available at:
- HTTP: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

### 2. View Logs

```bash
# View all logs
docker compose -f docker/docker-compose.yml logs

# Follow API logs
docker compose -f docker/docker-compose.yml logs -f api

# Follow database logs
docker compose -f docker/docker-compose.yml logs -f postgres
```

### 3. Stop Services

```bash
# Stop services
docker compose -f docker/docker-compose.yml down

# Stop and remove volumes (deletes database data)
docker compose -f docker/docker-compose.yml down -v
```

## Manual Docker Build

If you want to build and run the API container separately:

```bash
# Build the image
docker build -f docker/Dockerfile -t expense-tracker-api:latest .

# Run the container (requires a PostgreSQL instance)
docker run -d \
  --name expense-tracker-api \
  -p 5000:5000 \
  -e DB_HOST=your-db-host \
  -e DB_NAME=expense_tracker_db \
  -e DB_USER=postgres \
  -e DB_PASSWORD=your-password \
  -e JWT_SECRET_KEY=your-secret-key-at-least-32-characters \
  expense-tracker-api:latest
```

## Environment Variables

Configure the application using environment variables in `docker-compose.yml`:

### Database Configuration
- `DB_HOST` - Database hostname (default: `postgres`)
- `DB_NAME` - Database name (default: `expense_tracker_db`)
- `DB_USER` - Database username (default: `postgres`)
- `DB_PASSWORD` - Database password (required)
- `DB_PORT` - Database port (default: `5432`)
- `DB_SSL_MODE` - SSL mode for database connection (default: `Disable`)

### JWT Configuration
- `JWT_SECRET_KEY` - Secret key for JWT signing (minimum 32 characters, required)
- `JWT_ISSUER` - JWT issuer claim (default: `ExpenseTrackerAPI`)
- `JWT_AUDIENCE` - JWT audience claim (default: `ExpenseTrackerAPI`)
- `JWT_EXPIRY_HOURS` - Token expiration time in hours (default: `24`)

### Application Configuration
- `ASPNETCORE_ENVIRONMENT` - Environment name (`Development`, `Staging`, `Production`)
- `ASPNETCORE_URLS` - URLs the application listens on

## Production Deployment

For production deployments:

1. **Update JWT Secret**: Change `JWT_SECRET_KEY` to a strong, randomly generated secret
   ```bash
   # Generate a secure secret key
   openssl rand -base64 48
   ```

2. **Use Strong Database Password**: Update `POSTGRES_PASSWORD` and `DB_PASSWORD`

3. **Enable SSL**: Update `DB_SSL_MODE` to `Require` if your database supports it

4. **Use Environment File**: Create a `.env` file (not committed to git):
   ```bash
   # Copy from the root .env file
   cp ../.env docker/.env
   ```
   
   Then reference it in docker-compose.yml:
   ```yaml
   api:
     env_file:
       - .env
   ```

5. **Configure Volumes**: Ensure persistent volumes for database data

6. **Set Up Reverse Proxy**: Use nginx or Traefik for SSL termination and load balancing

## Health Check

The API includes a health check endpoint:

```bash
# Check API health
curl http://localhost:5000/health
```

Docker health check is configured to monitor this endpoint automatically.

## Database Migrations

Migrations are applied automatically when the application starts. The API will:
1. Check database connectivity
2. Apply any pending migrations
3. Seed initial data if the database is empty

To manually trigger migrations:

```bash
# Access the API container
docker exec -it expense-tracker-api bash

# Run migrations (if needed)
dotnet ef database update --project /app/ExpenseTrackerAPI.Infrastructure.dll
```

## Troubleshooting

### API Can't Connect to Database

1. Check that PostgreSQL is running:
   ```bash
   docker compose -f docker/docker-compose.yml ps postgres
   ```

2. Verify database credentials in `docker-compose.yml`

3. Check API logs:
   ```bash
   docker compose -f docker/docker-compose.yml logs api
   ```

### Port Already in Use

If port 5000 or 5432 is already in use:

```bash
# Change the port mapping in docker-compose.yml
ports:
  - "5050:5000"  # Maps host port 5050 to container port 5000
```

### Container Keeps Restarting

Check the logs for errors:
```bash
docker compose -f docker/docker-compose.yml logs api
```

Common issues:
- Missing or invalid JWT secret key
- Database connection failure
- Invalid environment variables

## Development vs Production

### Development
- Uses `appsettings.Development.json`
- No environment variables required
- Run with `dotnet run` (not Docker)

### Production (Docker)
- Uses environment variables (from `.env` or `docker-compose.yml`)
- Requires `ASPNETCORE_ENVIRONMENT=Production`
- All sensitive values must be provided via environment variables

## Docker Image Details

### Build Stage
- Base: `mcr.microsoft.com/dotnet/sdk:9.0`
- Restores NuGet packages
- Compiles the application
- Publishes release build

### Runtime Stage
- Base: `mcr.microsoft.com/dotnet/aspnet:9.0`
- Minimal runtime image (~200MB)
- Includes only published application files
- Runs as non-root user (best practice)

## Cleanup

Remove all containers, images, and volumes:

```bash
# Stop and remove containers
docker compose -f docker/docker-compose.yml down -v

# Remove the built image
docker rmi expense-tracker-api:latest

# Clean up unused Docker resources
docker system prune -a
```

## Support

For issues or questions:
- Check application logs
- Review environment variable configuration
- Ensure PostgreSQL is accessible
- Verify JWT secret key is at least 32 characters