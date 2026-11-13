# Buyer load testing, rate limiting and load balancing

This directory provides repeatable evidence for:

- sustained, stress, spike and soak load through JMeter;
- Nginx request-rate limiting with HTTP 429;
- Nginx `least_conn` balancing across two .NET API instances.

Only run load tests against the isolated stack or an explicitly authorized test environment.

## Architecture

```text
JMeter on the host
       |
       +--> localhost:8080  Nginx rate-limited entry
       +--> localhost:8081  Nginx capacity entry (no request-rate limit)
                              /                 \
                          api-1                 api-2
                              \                 /
                                  SQL Server
```

Both ports use the same Nginx upstream and two API replicas. Port 8081 exists only in
`docker-compose.loadtest.yml`; it separates backend capacity results from intentional
HTTP 429 responses. The ASP.NET limiter remains enabled with a high, finite test-only
limit. Production limits are not disabled or changed.

## 1. Prepare the isolated stack

Start Docker Desktop, then run:

```powershell
./scripts/start-loadtest-stack.ps1
docker compose -f docker-compose.loadtest.yml ps
curl.exe -f http://localhost:8080/health/ready
curl.exe -f http://localhost:8081/health/ready
```

The isolated Compose file has local-only default credentials. Override `SA_PASSWORD`
and `JWT_SIGNING_KEY` through environment variables when needed; never reuse these test
defaults in a shared or production environment. The start script builds the API image
once and then starts both replicas from that image.

The load-test database uses its own named volume. To start the optional single background
worker, add `--profile workers`. API replicas intentionally do not run duplicate jobs.

## 2. Prove load balancing

```powershell
./scripts/test-nginx-load-balance.ps1
```

The command calls `/api/system/status` repeatedly and requires both `api-1` and `api-2`.
Responses expose `X-Api-Instance`; Nginx also adds `X-Upstream-Addr` for test evidence.

## 3. Prove rate limiting

```powershell
./scripts/test-nginx-rate-limit.ps1
```

The test sends a catalog burst and an invalid-login abuse simulation through port 8080.
It requires normal catalog HTTP 200, invalid-credential HTTP 401, intentional HTTP 429,
and a `Retry-After` header, with no unexpected response codes. JMeter marks expected 401
and 429 samples as errors in its generic chart; the verification script classifies them
by sampler label and response code. The generated HTML report is written beneath
`performance-tests/results/`.

## 4. Run load profiles

JMeter 5.6.3 and Java 17 or newer are recommended. The script finds JMeter from PATH,
`JMETER_HOME`, or the local `D:\Tools\JMeter\apache-jmeter-5.6.3` installation.

```powershell
./scripts/run-jmeter-load.ps1 -Profile smoke
./scripts/run-jmeter-load.ps1 -Profile load
./scripts/run-jmeter-load.ps1 -Profile stress
./scripts/run-jmeter-load.ps1 -Profile spike
./scripts/run-jmeter-load.ps1 -Profile soak
```

Default profiles:

| Profile | Users | Ramp | Duration | Purpose |
|---|---:|---:|---:|---|
| smoke | 5 | 5s | 30s | wiring and data check |
| load | 50 | 60s | 5m | expected sustained load |
| stress | 200 | 60s | 5m | find degradation |
| spike | 200 | 10s | 3m | sudden traffic increase |
| soak | 100 | 120s | 60m | leaks and pool exhaustion |

Override any value when required:

```powershell
./scripts/run-jmeter-load.ps1 -Profile load -Users 75 -RampSeconds 90 `
  -DurationSeconds 600 -MaxP95Milliseconds 500 -MaxErrorPercent 1
```

The script runs JMeter in CLI mode, generates JTL and HTML evidence, calculates p95,
p99, throughput and error rate, then returns a non-zero exit code when the gate fails.
The current workload is read-only and exercises products, listings, categories and the
system endpoint. Write-heavy checkout and bidding need isolated per-user inventory and
are deliberately not mixed into the first capacity baseline.

## 5. Inspect and stop

```powershell
docker compose -f docker-compose.loadtest.yml logs nginx --tail 100
docker compose -f docker-compose.loadtest.yml logs api1 api2 --tail 100
docker stats
docker compose -f docker-compose.loadtest.yml down
```

Keep the named database volume for repeatable runs. Remove it only when a fresh seeded
database is explicitly required:

```powershell
docker compose -f docker-compose.loadtest.yml down --volumes
```
