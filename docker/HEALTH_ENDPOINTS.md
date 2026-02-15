# Health Check Endpoints Documentation

This document describes the health check endpoints available in the ExpenseTracker API.

## Available Endpoints

### 1. Simple Health Check (Root Level)

**Endpoint:** `GET /health`

**Description:** A lightweight health check endpoint designed for Docker and Kubernetes health probes. Returns a simple JSON response indicating the API is running.

**Authentication:** Not required

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Usage:**
- Docker HEALTHCHECK
- Kubernetes liveness probes
- Load balancer health checks
- Simple uptime monitoring

**Example:**
```bash
curl http://localhost:5000/health
```

---

### 2. Detailed Health Check (V1 API)

**Endpoint:** `GET /api/v1/health`

**Description:** A comprehensive health check endpoint that provides detailed information about the API status, including database connectivity.

**Authentication:** Not required

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0.0",
  "database": {
    "status": "Healthy",
    "message": "Database connection successful"
  }
}
```

**Response (503 Service Unavailable):**
```json
{
  "status": "Unhealthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0.0",
  "database": {
    "status": "Unhealthy",
    "message": "Cannot connect to database"
  }
}
```

**Response Fields:**
- `status`: Overall health status ("Healthy" or "Unhealthy")
- `timestamp`: Current UTC timestamp
- `version`: Application version
- `database.status`: Database health status
- `database.message`: Additional database information

**Usage:**
- Detailed health monitoring
- Troubleshooting database connectivity
- API readiness checks
- Monitoring dashboards

**Example:**
```bash
curl http://localhost:5000/api/v1/health
```

---

## Docker Integration

The Dockerfile includes a HEALTHCHECK instruction that uses the `/health` endpoint:

```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1
```

**Parameters:**
- `interval`: Check every 30 seconds
- `timeout`: 3 second timeout for each check
- `start-period`: 10 second grace period on startup
- `retries`: 3 consecutive failures before marking as unhealthy

**Check container health:**
```bash
docker inspect --format='{{.State.Health.Status}}' expense-tracker-api
```

---

## Kubernetes Integration

### Liveness Probe

Use the simple `/health` endpoint for liveness probes:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 5000
  initialDelaySeconds: 10
  periodSeconds: 30
  timeoutSeconds: 3
  failureThreshold: 3
```

### Readiness Probe

Use the detailed `/api/v1/health` endpoint for readiness probes:

```yaml
readinessProbe:
  httpGet:
    path: /api/v1/health
    port: 5000
  initialDelaySeconds: 5
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

---

## Monitoring and Alerting

### Prometheus

Example Prometheus configuration for health check monitoring:

```yaml
scrape_configs:
  - job_name: 'expense-tracker-health'
    metrics_path: '/api/v1/health'
    scrape_interval: 30s
    static_configs:
      - targets: ['expense-tracker-api:5000']
```

### Uptime Monitoring

Popular uptime monitoring services can use either endpoint:

**UptimeRobot / Pingdom:**
- URL: `http://your-domain.com/health`
- Expected response: 200 OK
- Check interval: 5 minutes

**Better Uptime / StatusCake:**
- URL: `http://your-domain.com/api/v1/health`
- Expected response: 200 OK with JSON containing `"status": "Healthy"`
- Check interval: 1 minute

---

## Load Balancer Configuration

### NGINX

```nginx
upstream expense_tracker_api {
    server api1:5000;
    server api2:5000;
    server api3:5000;
    
    # Health check
    check interval=10000 rise=2 fall=3 timeout=5000 type=http;
    check_http_send "GET /health HTTP/1.0\r\n\r\n";
    check_http_expect_alive http_2xx;
}
```

### AWS Application Load Balancer

```json
{
  "HealthCheckEnabled": true,
  "HealthCheckPath": "/health",
  "HealthCheckIntervalSeconds": 30,
  "HealthCheckTimeoutSeconds": 5,
  "HealthyThresholdCount": 2,
  "UnhealthyThresholdCount": 3,
  "Matcher": {
    "HttpCode": "200"
  }
}
```

---

## Testing

### Manual Testing

```bash
# Simple health check
curl -i http://localhost:5000/health

# Detailed health check
curl -i http://localhost:5000/api/v1/health

# Pretty print JSON
curl -s http://localhost:5000/api/v1/health | jq
```

### Automated Testing

```bash
#!/bin/bash
# health-check.sh

HEALTH_URL="http://localhost:5000/health"
MAX_RETRIES=10
RETRY_INTERVAL=5

for i in $(seq 1 $MAX_RETRIES); do
    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" $HEALTH_URL)
    
    if [ "$HTTP_CODE" = "200" ]; then
        echo "✅ API is healthy!"
        exit 0
    fi
    
    echo "⏳ Attempt $i/$MAX_RETRIES - API not ready (HTTP $HTTP_CODE)"
    sleep $RETRY_INTERVAL
done

echo "❌ API failed to become healthy after $MAX_RETRIES attempts"
exit 1
```

---

## Best Practices

1. **Use `/health` for Docker/K8s probes** - It's lightweight and fast
2. **Use `/api/v1/health` for monitoring dashboards** - Provides detailed information
3. **Don't authenticate health checks** - They should be publicly accessible
4. **Set appropriate timeouts** - Health checks should complete quickly (< 5 seconds)
5. **Monitor database connectivity** - The detailed endpoint checks database health
6. **Log health check failures** - The API logs database connection issues
7. **Use readiness vs liveness probes** - Readiness checks dependencies, liveness checks the process

---

## Troubleshooting

### Health Check Returns 503

**Possible causes:**
1. Database connection failure
2. Database credentials incorrect
3. Network issues between API and database
4. Database container not running

**Resolution:**
```bash
# Check database container
docker compose -f docker/docker-compose.yml ps postgres

# View API logs
docker compose -f docker/docker-compose.yml logs api

# Test database connectivity
docker exec -it expense-tracker-db psql -U postgres -d expense_tracker_db -c "SELECT 1;"
```

### Health Check Times Out

**Possible causes:**
1. API container not running
2. Firewall blocking port 5000
3. API startup taking longer than expected

**Resolution:**
```bash
# Check if API is running
docker ps | grep expense-tracker-api

# Check API logs
docker logs expense-tracker-api

# Check port mapping
docker port expense-tracker-api
```

### Docker Health Check Failing

**Check container health status:**
```bash
docker inspect expense-tracker-api | jq '.[0].State.Health'
```

**View health check logs:**
```bash
docker inspect expense-tracker-api | jq '.[0].State.Health.Log'
```

---

## Version History

- **v1.0** - Initial health check endpoints
  - Added `/health` for simple checks
  - Added `/api/v1/health` for detailed checks
  - Included database connectivity validation