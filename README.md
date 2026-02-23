# dotnet-health-kit

[![NuGet](https://img.shields.io/nuget/v/JG.HealthKit?logo=nuget)](https://www.nuget.org/packages/JG.HealthKit)
[![Downloads](https://img.shields.io/nuget/dt/JG.HealthKit?color=%230099ff&logo=nuget)](https://www.nuget.org/packages/JG.HealthKit)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](./LICENSE)
[![CI](https://github.com/jamesgober/dotnet-health-kit/actions/workflows/ci.yml/badge.svg)](https://github.com/jamesgober/dotnet-health-kit/actions)

---

Lightweight health check and readiness probe library for .NET services. Built-in checks for databases, HTTP dependencies, disk space, and memory — with Kubernetes-ready endpoints, degraded state detection, and custom check support.


## Features

- **Liveness & Readiness** — Separate `/health/live` and `/health/ready` endpoints for orchestrator probes
- **Built-in Checks** — Database connectivity, HTTP dependency, disk space, memory pressure, custom TCP
- **Degraded State** — Three-state model (Healthy, Degraded, Unhealthy) with configurable thresholds
- **Custom Checks** — Implement `IHealthCheck` for application-specific health indicators
- **Caching** — Configurable TTL to prevent health check storms under load
- **JSON Response** — Detailed check results with timing, status, and error context
- **Startup Probes** — Optional startup check that gates readiness until initialization completes
- **Single Registration** — `services.AddHealthKit()`

## Installation

```bash
dotnet add package JG.HealthKit
```

## Quick Start

```csharp
builder.Services.AddHealthKit(options =>
{
    options.AddSqlServer("Server=localhost;Database=mydb;...");
    options.AddHttpDependency("payment-api", "https://api.payments.com/health");
    options.AddDiskSpace(threshold: 500_000_000); // 500MB minimum
    options.CacheDuration = TimeSpan.FromSeconds(10);
});

app.MapHealthKit(); // Maps /health/live and /health/ready
```

## Documentation

- **[API Reference](./docs/API.md)** — Full API documentation and examples

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

Licensed under the Apache License 2.0. See [LICENSE](./LICENSE) for details.

---

**Ready to get started?** Install via NuGet and check out the [API reference](./docs/API.md).
