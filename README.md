# eBay Clone — Buyer

ASP.NET Core 8 / SQL Server backend and React/TypeScript frontend. Current database: **CloneEbayBuyer**.

Implementation and test evidence: [Buyer completion](docs/11-buyer-completion.md). Earlier architecture/research documents describe historical states and may predate the implemented workflows.

## Local setup

1. Configure ConnectionStrings:BuyerDatabase for SQL Server. Development defaults to .\SQLEXPRESS and CloneEbayBuyer. Environment variables and User Secrets override JSON settings.
2. For an existing current-schema database, run database/upgrades/001-buyer-completion.sql using sqlcmd -I -b. For a fresh database, run database/CloneEbayBuyer.sql, optionally the demo seed/password scripts, then the upgrade. The CREATE script aborts if the database exists.
3. Configure Jwt:SigningKey (32+ random characters) using User Secrets or environment variables.
4. Run the API and frontend:

```powershell
dotnet run --project backend/Buyer.API --launch-profile http
# Separate terminal
cd frontend/buyer-web
npm ci
npm run dev -- --host 127.0.0.1
```

Frontend http://127.0.0.1:5173; API http://localhost:5127; Swagger /swagger in Development; readiness /health/ready.

Demo buyer: seed.buyer01@example.test / Demo@12345. Registration requires verification. Read local test mail at /dev-mailbox when SMTP is not configured. Login is kept in memory; a full reload requires signing in again.

See docs/11-buyer-completion.md for SMTP, Google OAuth, tax configuration and actual verification limits. PayPal is simulated.

## Checks and delivery

```powershell
dotnet test EbayClone.sln -c Release
cd frontend/buyer-web
npm run build
npm run lint
npm run test:e2e
```

E2E and SQL smoke scripts must use an isolated seeded test database/API. Required environment variables and results are documented in docs/11-buyer-completion.md. Performance plans are in performance-tests; rolling deployment instructions are in infrastructure/k8s.

For Docker, copy .env.example to .env, replace the JWT key/database credentials, and run docker compose up --build. The one-shot db-init service bootstraps a fresh CloneEbayBuyer and applies the upgrade; existing rows are preserved. Remote CI/container/Kubernetes execution is separate from local build verification.
