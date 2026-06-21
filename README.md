# Jumbo Product Service

Assessment project for Jumbo solution engineer — a product catalogue service with a Blazor frontend, ASP.NET Core API, background worker, and layered backend architecture.

## Project structure

```text
├── backend/
│   ├── Jumbo.ProductCatalog.Api/            # ASP.NET Core Web API
│   ├── Jumbo.ProductCatalog.Worker/         # Background service host
│   ├── Jumbo.ProductCatalog.Core/           # Use cases and service interfaces
│   ├── Jumbo.ProductCatalog.Domain/         # Core entities and business rules
│   └── Jumbo.ProductCatalog.Infrastructure/ # Data access and external services
├── frontend/
│   └── Jumbo.ProductCatalog.UI/             # Blazor frontend
├── tests/
│   ├── Jumbo.ProductCatalog.Api.Tests/
│   └── Jumbo.ProductCatalog.Core.Tests/
└── docs/
    ├── architecture-decisions.md
```

## Prerequisites

- [.NET SDK 10.0.x](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for SQL Server + Azurite)

## Docker setup

`docker-compose.yml` spins up two services:

| Service | Image | Ports | Purpose |
|---|---|---|---|
| `sqlserver` | `mssql/server:2022-latest` | `1433` | SQL Server database |
| `azurite` | `azure-storage/azurite` | `10000–10002` | Local Azure Blob/Queue/Table emulator |

### 1. Configure environment

```bash
cp .env.example .env
```

Open `.env` and set a strong SQL Server SA password (must meet SQL Server complexity rules — min 8 chars, mixed case, digit, symbol):

```
MSSQL_SA_PASSWORD=Your_Strong_Password1!
```

### 2. Start containers

```bash
docker compose up -d
```

Verify both containers are healthy before continuing:

```bash
docker compose ps
```

`sqlserver` uses a healthcheck; wait until its status shows `healthy` (usually ~30 s).

### 3. Apply database migrations

Install the EF Core CLI tool if you don't have it:

```bash
dotnet tool install --global dotnet-ef
```

Run migrations (replace the password with the one you set in `.env`):

```bash
dotnet ef database update \
  --project backend/Jumbo.ProductCatalog.Infrastructure \
  --startup-project backend/Jumbo.ProductCatalog.Api \
  --connection "Server=localhost,1433;Database=JumboProductCatalog;User Id=sa;Password=Your_Strong_Password1!;TrustServerCertificate=True"
```

### 4. Set local app secrets

The `.env` file only configures Docker containers. The .NET apps read connection strings via `dotnet user-secrets`:

```bash
dotnet user-secrets set "Database:ConnectionString" \
  "Server=localhost,1433;Database=JumboProductCatalog;User Id=sa;Password=Your_Strong_Password1!;TrustServerCertificate=True" \
  --project backend/Jumbo.ProductCatalog.Api

dotnet user-secrets set "BlobStorage:ConnectionString" \
  "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tiqIFOGWWhbhwznHXVjmQT==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;" \
  --project backend/Jumbo.ProductCatalog.Api
```

Repeat both secrets for `Jumbo.ProductCatalog.Worker` if running the worker locally.

### Stopping / resetting

```bash
docker compose down          # stop containers, keep volumes
docker compose down -v       # stop and delete all data volumes
```

## Getting started

### 1. Start infrastructure

Complete the [Docker setup](#docker-setup) steps above first.

### 2. Run

```bash
dotnet restore
dotnet build
dotnet run --project backend/Jumbo.ProductCatalog.Api
```

## Notes

- Targeting `net10.0`.
- `NuGet.Config` pins the feed to `nuget.org` only so restore works without private company credentials.
