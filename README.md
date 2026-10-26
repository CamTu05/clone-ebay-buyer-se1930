# eBay Clone – Buyer Module

Independent Buyer Module for the PRN232 Group 1 project. This repository is initialized as a modular monolith with simplified Clean Architecture and feature-based React organization.

## Tech Stack

- .NET 8, ASP.NET Core Web API, EF Core 8, SQL Server
- React, TypeScript, Vite, React Router, Axios, Tailwind CSS
- xUnit and ASP.NET Core integration testing
- Docker, Docker Compose, Nginx, and GitHub Actions preparation

## Structure

```text
backend/       API, Application, Domain, Infrastructure
tests/         Unit and integration tests
frontend/      Vite React buyer application
docs/          Official requirements, architecture, database, API contract, and audit
infrastructure/ Docker, Nginx, and Kubernetes preparation
performance-tests/ JMeter preparation
.github/       CI workflow
```

## Prerequisites

- .NET SDK 8.0
- Node.js 20 or newer and npm
- SQL Server only when persistence is enabled
- Docker Desktop only for container workflows

The target database name is `CloneEbayDB_V2`. The SQL file under `database/` is a destructive clean-bootstrap reference containing `DROP DATABASE`; never execute it against an existing database.

## Install

```bash
dotnet restore EbayClone.sln
npm ci --prefix frontend/buyer-web
```

Copy `.env.example` to `.env` for Docker Compose and `frontend/buyer-web/.env.example` to `frontend/buyer-web/.env.local` for local frontend development. Do not commit secrets.

## Run

Backend:

```bash
dotnet run --project backend/Buyer.API
```

Frontend in another terminal:

```bash
npm run dev --prefix frontend/buyer-web
```

Open the Vite URL shown by the frontend. Swagger is available at `http://localhost:5127/swagger` in Development. Health endpoints are `/health/live` and `/health/ready`.

## Environment

- `ConnectionStrings__BuyerDatabase`: optional SQL Server connection string. The API runs without it during initialization.
- `Cors__AllowedOrigins__0`: frontend origin.
- `RateLimiting__PermitLimit` and `RateLimiting__WindowSeconds`: fixed-window API limits.
- `VITE_API_BASE_URL`: public Buyer API base URL used by the frontend.

## Build and Test

```bash
dotnet build EbayClone.sln --configuration Release
dotnet test EbayClone.sln --configuration Release --no-build
npm run lint --prefix frontend/buyer-web
npm run build --prefix frontend/buyer-web
```

## Containers

```bash
docker compose up --build
```

The Compose configuration is intended for local development and requires Docker Desktop.

## Known Limitations

- No authentication, authorization, business workflows, database entities, migrations, or seed data exist yet.
- The custom `dbo.User` schema versus ASP.NET Core Identity strategy must be approved before authentication persistence is implemented; do not create a second user system.
- External module abstractions are documented but not implemented yet.
- Kubernetes and JMeter configurations await deployment and performance requirements.
- SQL Server is not required for basic API startup; readiness currently reports process health only.
