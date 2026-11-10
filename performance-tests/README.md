# Performance checks

Use an isolated API/database and configure test-specific rate limits deliberately. Do not disable production rate limits for benchmarking. Neither a smoke run nor this plan establishes production capacity.

Node authenticated smoke (100 concurrent clients, 12 demo buyer accounts, 400 requests):

```powershell
$env:BUYER_TEST_URL='http://localhost:5139'
$env:BUYER_TEST_PASSWORD='Demo@12345'
$env:BUYER_LOAD_ALLOW='isolated-database'
node scripts/buyer-load-smoke.mjs
```

Apache JMeter (requires Java and JMeter installed):

```text
jmeter -n -t performance-tests/buyer-smoke.jmx -Jhost=localhost -Jport=5139 -Jusers=100 -Jloops=3 -Jramp=10 -l performance-tests/results.jtl -e -o performance-tests/report
```

The JMeter plan asserts HTTP 200 for catalog reads. Inspect error rate, p95 and server/DB resource usage before acceptance. To test rate limiting separately, use default limits and verify excess requests produce 429 and Retry-After; do not count intentional 429s as capacity results.
