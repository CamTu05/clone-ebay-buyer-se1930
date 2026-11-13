#!/usr/bin/env bash
set -euo pipefail
: "${SQL_CONTAINER_ID:?SQL_CONTAINER_ID required}"
: "${SQLCMDPASSWORD:?SQLCMDPASSWORD required}"
export BUYER_TEST_SQL_USER=sa BUYER_TEST_SQL_SERVER=localhost BUYER_TEST_DATABASE=CloneEbayBuyer_Test_ci
export BUYER_TEST_PASSWORD='Demo@12345' BUYER_TEST_URL=http://localhost:5139
mkdir -p "$RUNNER_TEMP/buyer-tools"
cat > "$RUNNER_TEMP/buyer-tools/sqlcmd" <<'WRAPPER'
#!/usr/bin/env bash
exec docker exec -e SQLCMDPASSWORD "$SQL_CONTAINER_ID" /opt/mssql-tools18/bin/sqlcmd "$@"
WRAPPER
chmod +x "$RUNNER_TEMP/buyer-tools/sqlcmd"
export PATH="$RUNNER_TEMP/buyer-tools:$PATH"
for file in CloneEbayBuyer.sql CloneEbayBuyer.Seed.sql; do
 sed 's/CloneEbayBuyer/CloneEbayBuyer_Test_ci/g' "database/$file" > "$RUNNER_TEMP/$file"
 docker cp "$RUNNER_TEMP/$file" "$SQL_CONTAINER_ID:/tmp/$file"
 sqlcmd -S localhost -U sa -C -I -b -f 65001 -i "/tmp/$file"
done
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__BuyerDatabase="Server=localhost,1433;Database=$BUYER_TEST_DATABASE;User Id=sa;Password=$SQLCMDPASSWORD;TrustServerCertificate=True"
export Jwt__SigningKey='ci-only-ephemeral-signing-key-at-least-32-characters'
export BuyerLifecycle__Enabled=true BuyerLifecycle__IntervalSeconds=5 RateLimiting__PermitLimit=10000 RateLimiting__LoginPermitLimit=1000 Checkout__TaxRates__VN=0.10
export Cors__AllowedOrigins__2=http://127.0.0.1:5174
(dotnet run --project backend/Buyer.API -c Release --no-build --no-launch-profile --urls http://localhost:5139 > "$RUNNER_TEMP/buyer-api.log" 2>&1) &
api_pid=$!
(cd frontend/buyer-web && VITE_API_BASE_URL=http://localhost:5139 npm run dev -- --host 127.0.0.1 --port 5174 --strictPort > "$RUNNER_TEMP/buyer-web.log" 2>&1) &
web_pid=$!
trap 'kill "$api_pid" "$web_pid" 2>/dev/null || true' EXIT
for attempt in $(seq 1 60); do if curl -sf http://localhost:5139/health/ready > /dev/null; then break; fi; sleep 1; done
curl -f http://localhost:5139/health/ready
node scripts/buyer-completion-smoke.mjs
node scripts/buyer-feedback-smoke.mjs
node scripts/buyer-auction-smoke.mjs
node scripts/buyer-lifecycle-smoke.mjs
(cd frontend/buyer-web && E2E_ALLOW_MUTATIONS=isolated-database npx playwright test)
BUYER_LOAD_ALLOW=isolated-database node scripts/buyer-load-smoke.mjs
