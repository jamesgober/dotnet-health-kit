<div align="center">
    <img width="120px" height="auto" src="https://raw.githubusercontent.com/jamesgober/jamesgober/main/media/icons/hexagon-3.svg" alt="Triple Hexagon">
    <h1>
        <strong>dotnet-health-kit</strong>
        <sup><br><sub>HEALTH CHECKS &amp; READINESS</sub></sup>
    </h1>
    <div>
        <a href="https://www.nuget.org/packages/dotnet-health-kit"><img alt="NuGet" src="https://img.shields.io/nuget/v/dotnet-health-kit"></a>
        <span>&nbsp;</span>
        <a href="https://www.nuget.org/packages/dotnet-health-kit"><img alt="NuGet Downloads" src="https://img.shields.io/nuget/dt/dotnet-health-kit?color=%230099ff"></a>
        <span>&nbsp;</span>
        <a href="./LICENSE" title="License"><img alt="License" src="https://img.shields.io/badge/license-Apache--2.0-blue.svg"></a>
        <span>&nbsp;</span>
        <a href="https://github.com/jamesgober/dotnet-health-kit/actions"><img alt="GitHub CI" src="https://github.com/jamesgober/dotnet-health-kit/actions/workflows/ci.yml/badge.svg"></a>
    </div>
</div>
<br>
<p>
    Lightweight health check and readiness probe library for .NET services. Built-in checks for databases, HTTP dependencies, disk space, and memory — with Kubernetes-ready endpoints, degraded state detection, and custom check support.
</p>

## Features

- **Liveness & Readiness** — Separate `/health/live` and `/health/ready` endpoints for orchestrator probes
- **Built-in Checks** — Database connectivity, HTTP dependency, disk space, memory pressure, custom TCP
- **Degraded State** — Three-state model (Healthy, Degraded, Unhealthy) with configurable thresholds
- **Custom Checks** — Implement `IHealthCheck` for application-specific health indicators
- **Caching** — Configurable TTL to prevent health check storms under load
- **JSON Response** — Detailed check results with timing, status, and error context
- **Startup Probes** — Optional startup check that gates readiness until initialization completes
- **Single Registration** — `services.AddHealthKit()`

<br>

## Installation

```bash
dotnet add package dotnet-health-kit
```

<br>

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

<br>

## Documentation

- **[API Reference](./docs/API.md)** — Full API documentation and examples

<br>

## Contributing

Contributions welcome. Please:
1. Ensure all tests pass before submitting
2. Follow existing code style and patterns
3. Update documentation as needed

<br>

## Testing

```bash
dotnet test
```

<br>
<hr>
<br>

<div id="license">
    <h2>⚖️ License</h2>
    <p>Licensed under the <b>Apache License</b>, version 2.0 (the <b>"License"</b>); you may not use this software, including, but not limited to the source code, media files, ideas, techniques, or any other associated property or concept belonging to, associated with, or otherwise packaged with this software except in compliance with the <b>License</b>.</p>
    <p>You may obtain a copy of the <b>License</b> at: <a href="http://www.apache.org/licenses/LICENSE-2.0" title="Apache-2.0 License" target="_blank">http://www.apache.org/licenses/LICENSE-2.0</a>.</p>
    <p>Unless required by applicable law or agreed to in writing, software distributed under the <b>License</b> is distributed on an "<b>AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND</b>, either express or implied.</p>
    <p>See the <a href="./LICENSE" title="Software License file">LICENSE</a> file included with this project for the specific language governing permissions and limitations under the <b>License</b>.</p>
    <br>
</div>

<div align="center">
    <h2></h2>
    <sup>COPYRIGHT <small>&copy;</small> 2025 <strong>JAMES GOBER.</strong></sup>
</div>
