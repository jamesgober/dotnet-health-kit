# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-25

### Added
- Core health check abstractions: `IHealthCheck`, `HealthCheckResult`, `HealthReport`, `HealthStatus` three-state enum
- `HealthKitOptions` for fluent registration of health checks with tag-based endpoint filtering
- Built-in SQL Server TCP connectivity check via `AddSqlServer()`
- Built-in HTTP dependency check via `AddHttpDependency()`
- Built-in disk space monitor via `AddDiskSpace()` with degraded threshold support
- Built-in managed memory monitor via `AddMemoryCheck()` with degraded threshold support
- Built-in raw TCP connectivity check via `AddTcp()`
- Built-in database connection check via `AddDatabaseCheck()` with optional test query
- Startup health probe via `AddStartupCheck()` and `IStartupHealthSignal`
- Inline delegate health checks via `AddCheck(name, Func<...>)`
- Custom check registration via `AddCheck<T>()` with automatic DI registration
- Kubernetes-ready endpoints: `GET /health/live` and `GET /health/ready`
- Configurable endpoint paths via `HealthKitOptions.LivenessPath` and `ReadinessPath`
- TTL-based result caching to prevent health check storms under load
- Concurrent check execution with per-check timeout support
- JSON response formatting using `Utf8JsonWriter` for zero-allocation serialization
- No-cache response headers on health endpoints
- Single registration entry point: `services.AddHealthKit()` and `app.MapHealthKit()`
- Full XML documentation on all public API surfaces
- `InternalsVisibleTo` for test project access to internal types

### Performance
- `HealthCheckResult` implemented as `readonly struct` to minimize allocations
- `ValueTask<T>` used throughout for synchronous completion fast paths
- Lock-free cache hit path using `ConcurrentDictionary` with `Environment.TickCount64`
- `SemaphoreSlim`-based cache miss path to prevent thundering herd
- `Utf8JsonWriter` for direct UTF-8 JSON output without intermediate serialization
- Manual loops in hot paths; no LINQ overhead in check execution or filtering
- `ConfigureAwait(false)` on all internal async calls
- Pre-filtered registration lists cached after first access

[1.0.0]: https://github.com/jamesgober/dotnet-health-kit/releases/tag/v1.0.0
