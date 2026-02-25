# API Reference

## Quick Start

```csharp
// Program.cs
builder.Services.AddHealthKit(options =>
{
    options.AddSqlServer("Server=localhost;Database=mydb;Trusted_Connection=true;");
    options.AddHttpDependency("payment-api", "https://api.payments.com/health");
    options.AddDiskSpace(threshold: 500_000_000); // 500 MB minimum
    options.AddMemoryCheck(unhealthyThreshold: 1_073_741_824); // 1 GB
    options.CacheDuration = TimeSpan.FromSeconds(10);
});

app.MapHealthKit(); // Maps /health/live and /health/ready
```

---

## Registration

### `services.AddHealthKit(Action<HealthKitOptions> configure)`

Registers all HealthKit services in the DI container. Call this once in your service configuration.

```csharp
builder.Services.AddHealthKit(options =>
{
    // Register checks here
});
```

### `app.MapHealthKit()`

Maps the liveness and readiness HTTP endpoints. Call this after building the app.

```csharp
app.MapHealthKit();
```

Default paths:
- `GET /health/live` — Liveness probe (tagged `"live"`)
- `GET /health/ready` — Readiness probe (tagged `"ready"`)

---

## HealthKitOptions

| Property | Type | Default | Description |
|---|---|---|---|
| `CacheDuration` | `TimeSpan` | `TimeSpan.Zero` | How long to cache health results. Zero disables caching. |
| `DefaultTimeout` | `TimeSpan` | `30s` | Default timeout for individual checks without an explicit timeout. |
| `LivenessPath` | `string` | `/health/live` | URL path for the liveness endpoint. |
| `ReadinessPath` | `string` | `/health/ready` | URL path for the readiness endpoint. |

---

## Built-in Health Checks

### SQL Server

TCP connectivity check to a SQL Server endpoint, parsed from a standard connection string.

```csharp
options.AddSqlServer("Server=myserver,1434;Database=mydb;User=sa;Password=...;");
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `connectionString` | `string` | *required* | SQL Server connection string. |
| `name` | `string?` | `sqlserver:{host}:{port}` | Override the check name. |
| `tags` | `IEnumerable<string>?` | `["ready"]` | Endpoint tags. |
| `timeout` | `TimeSpan?` | `null` | Per-check timeout. |

> For a full query-level database check, use `AddDatabaseCheck` instead.

### HTTP Dependency

Sends an HTTP GET and verifies a success status code (2xx).

```csharp
options.AddHttpDependency("payment-api", "https://api.payments.com/health");
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `name` | `string` | *required* | Unique check name. |
| `url` | `string` | *required* | Absolute URL to probe. |
| `tags` | `IEnumerable<string>?` | `["ready"]` | Endpoint tags. |
| `timeout` | `TimeSpan?` | `null` | Per-check timeout. |

### Disk Space

Monitors available free space on a drive or mount point.

```csharp
options.AddDiskSpace(
    threshold: 500_000_000,           // 500 MB = unhealthy
    degradedThreshold: 1_000_000_000, // 1 GB = degraded
    driveName: @"D:\");
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `threshold` | `long` | *required* | Minimum free bytes before unhealthy. |
| `degradedThreshold` | `long?` | `= threshold` | Free bytes below which the check reports degraded. |
| `driveName` | `string?` | OS root | Drive or mount point (`C:\`, `/`). |
| `name` | `string?` | `disk-space` | Override the check name. |
| `tags` | `IEnumerable<string>?` | `["ready"]` | Endpoint tags. |

### Memory

Monitors managed heap allocation against thresholds.

```csharp
options.AddMemoryCheck(
    unhealthyThreshold: 2_147_483_648,  // 2 GB = unhealthy
    degradedThreshold: 1_073_741_824);  // 1 GB = degraded
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `unhealthyThreshold` | `long` | `1 GB` | Allocated bytes above which the check reports unhealthy. |
| `degradedThreshold` | `long?` | `= unhealthyThreshold` | Allocated bytes above which the check reports degraded. |
| `name` | `string?` | `memory` | Override the check name. |
| `tags` | `IEnumerable<string>?` | `["ready"]` | Endpoint tags. |

### TCP

Raw TCP connectivity check to any host and port.

```csharp
options.AddTcp("redis", "redis.internal", 6379);
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `name` | `string` | *required* | Unique check name. |
| `host` | `string` | *required* | Hostname or IP address. |
| `port` | `int` | *required* | TCP port (1–65535). |
| `tags` | `IEnumerable<string>?` | `["ready"]` | Endpoint tags. |
| `timeout` | `TimeSpan?` | `null` | Per-check timeout. |

### Database

Opens a `DbConnection` and optionally executes a test query. Works with any ADO.NET provider.

```csharp
options.AddDatabaseCheck("orders-db",
    () => new SqlConnection("Server=...;Database=orders;..."),
    testQuery: "SELECT 1");
```

| Parameter | Type | Default | Description |
|---|---|---|---|
| `name` | `string` | *required* | Unique check name. |
| `connectionFactory` | `Func<DbConnection>` | *required* | Factory that creates a new connection. |
| `testQuery` | `string?` | `null` | SQL to execute after opening. |
| `tags` | `IEnumerable<string>?` | `["ready"]` | Endpoint tags. |
| `timeout` | `TimeSpan?` | `null` | Per-check timeout. |

### Startup Probe

Gates readiness until your application signals that initialization is complete.

```csharp
// Registration
options.AddStartupCheck();

// In your startup logic
app.Services.GetRequiredService<IStartupHealthSignal>().MarkReady();
```

The readiness endpoint returns unhealthy until `MarkReady()` is called.

---

## Custom Health Checks

### Using a delegate

```csharp
options.AddCheck("cache-warmup", async ct =>
{
    bool ready = await CheckCacheAsync(ct);
    return ready
        ? HealthCheckResult.Healthy("Cache warmed")
        : HealthCheckResult.Degraded("Cache warming in progress");
});
```

### Implementing IHealthCheck

```csharp
public class QueueHealthCheck : IHealthCheck
{
    private readonly IQueueClient _client;

    public QueueHealthCheck(IQueueClient client) => _client = client;

    public async ValueTask<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        int depth = await _client.GetDepthAsync(cancellationToken);

        if (depth > 10_000)
            return HealthCheckResult.Unhealthy($"Queue depth critical: {depth}");

        if (depth > 1_000)
            return HealthCheckResult.Degraded($"Queue depth elevated: {depth}");

        return HealthCheckResult.Healthy($"Queue depth: {depth}");
    }
}

// Registration
options.AddCheck<QueueHealthCheck>("message-queue");
```

---

## Tags and Endpoints

Every check is assigned one or more tags that determine which endpoint includes it.

| Tag | Endpoint | Purpose |
|---|---|---|
| `"live"` | `/health/live` | Liveness — is the process alive? |
| `"ready"` | `/health/ready` | Readiness — can it serve traffic? |

The default tag is `"ready"`. A check can belong to both endpoints:

```csharp
options.AddCheck("critical", myCheck, tags: new[] { "live", "ready" });
```

If no checks are tagged `"live"`, the liveness endpoint returns a healthy response with no entries, which is the recommended Kubernetes pattern.

---

## Three-State Model

| Status | HTTP Code | Meaning |
|---|---|---|
| `Healthy` | 200 | Everything is working normally. |
| `Degraded` | 200 | Functional but with warnings (high memory, slow dependency). |
| `Unhealthy` | 503 | Not functioning. Kubernetes removes the pod from service. |

The aggregate report status is the worst status across all individual checks.

---

## Response Format

```json
{
  "status": "Degraded",
  "totalDuration": "00:00:00.0234567",
  "checks": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0050000",
      "description": "Database connection successful"
    },
    "disk-space": {
      "status": "Degraded",
      "duration": "00:00:00.0001200",
      "description": "Disk space warning on C:\\: 1.2 GB available",
      "data": {
        "availableBytes": 1200000000,
        "totalBytes": 512000000000
      }
    }
  }
}
```

---

## Caching

When `CacheDuration` is set, results are cached to prevent health check storms:

```csharp
options.CacheDuration = TimeSpan.FromSeconds(10);
```

- Cache is per-endpoint (liveness and readiness are cached independently).
- On cache miss, only one concurrent execution runs; other requests wait for the result.
- Set to `TimeSpan.Zero` to disable (the default).

---

## Kubernetes Configuration

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10

startupProbe:
  httpGet:
    path: /health/ready
    port: 8080
  failureThreshold: 30
  periodSeconds: 2
```
