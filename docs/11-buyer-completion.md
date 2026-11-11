# Buyer completion implementation — 10/11/2026

Implemented against CloneEbayBuyer; Google Sheet unchanged.

## Delivered

- Registration and email verification, resend verification, forgot/reset/change password. Single-use expiring tokens, hashed passwords, authVersion-based JWT revocation after password change/reset/logout.
- Transactional email outbox with SMTP delivery and retry. Local-only Development mailbox at /dev-mailbox; production endpoint returns 404.
- Google OpenID Connect sign-in using authorization code, PKCE, verified email and one-time exchange code. Never automatically links a different identity to an existing email account.
- Guest cart, merge-on-login with idempotent merge operations, partial-failure retention; Saved for Later, restore and remove.
- Buyer and guest checkout, Buy It Now, shipping selection, configured tax snapshots, encrypted idempotency receipts, retry-safe stock/order creation.
- Guest order access and mock-payment retry using expiring bearer tokens stored hashed in GuestCheckoutAccess; receipt payloads containing credentials are encrypted.
- Proxy bidding, serialized concurrent bids and stable tie handling; SSE public auction updates; personal bid history; winner-only checkout within 48 hours, duplicate-order protection.
- Product review list, seller feedback submission/list, delivered-order and uniqueness checks.
- Username changes with persisted 30-day restriction. Payment, outbid, winner and shipping notifications. Cancellation/return request statuses visible in purchase history.
- Docker database bootstrap/upgrade, JWT configuration, Nginx SSE proxy; SQL/E2E CI job; JMeter plan, Node load smoke; rolling-deployment Kubernetes baseline.

## Database and runtime

Applied database/upgrades/001-buyer-completion.sql to CloneEbayBuyer without deleting existing rows. It adds authVersion/usernameChangedAt, tax columns, OperationReceipt and EmailOutbox, and updates money constraints to include tax. Readiness checks connectivity and the new schema.

Local API restarted at http://localhost:5127, FE at http://127.0.0.1:5173. Existing JWTs require sign-in again. Confirmed seed.buyer01@example.test / Demo@12345 login succeeds. Demo users keep existing passwords.

## External configuration

Use user-secrets or environment variables; do not commit credentials:

- SMTP: Smtp:Host, Port, EnableSsl, From, Username, Password; EmailOutbox:Enabled=true; Frontend:PublicUrl must match your frontend. Without SMTP, local verification/reset messages are read through /dev-mailbox.
- Google: OAuth:Google:ClientId and ClientSecret, authorized redirect URI `<API origin>/signin-google`. The button appears only when configured. Existing password accounts are not silently linked by matching email.
- Tax: Checkout:TaxRates:<COUNTRY_CODE> is a decimal from 0 to 1. VN defaults to 0 in local demo. SQL tests use 0.10 to exercise calculation; this is a test policy, not a statement of statutory tax.
- Behind a proxy: ReverseProxy:KnownProxies lists trusted proxy IPs. Set frontend public URL/CORS and HTTPS correctly. Replicated OAuth needs shared Data Protection keys or appropriate sticky routing, as documented in infrastructure/k8s/README.md.
- Keep JWT signing key stable across replicas; encrypted receipt recovery also uses this key. Coordinate key rotation with receipt retention/recovery.

PayPal remains mock as required by the backlog. External SMTP/Google cannot be verified end-to-end without service credentials. Docker, Java/JMeter and Kubernetes are unavailable on this machine: configuration/plans were added, but no container deployment, JMeter execution or zero-downtime claim is made. CI changes have not been pushed or run remotely. Seller/admin fulfilment, cancellation/return approval and real refunds remain outside Buyer scope.

## Verification actually completed

- 68 unit + 74 integration tests passed. Mock-only endpoint fixtures explicitly use Legacy mapping; new SQL smoke uses the current mapping.
- Fresh isolated database: CloneEbayBuyer_Test_finish_20261110.
- buyer-completion-smoke.mjs: 47 account/cart/checkout/guest assertions passed.
- buyer-auction-smoke.mjs: 29 proxy/concurrent bid/winner checkout assertions passed.
- buyer-feedback-smoke.mjs: 7 delivered review/feedback assertions passed.
- buyer-lifecycle-smoke.mjs: 10 concurrent checkout races passed, plus automatic expiry even after buyer lock and exactly-once stock restoration.
- Playwright on headless Brave: 2 end-to-end flows passed (registration/verification/merge/save/restore/address/COD, and guest Buy It Now/failed payment/retry/cart preservation).
- buyer-rate-limit-smoke.mjs: login/API 429, Retry-After and anonymous-order denial passed. Fixed endpoint metadata overriding the original login limiter.
- 100 simultaneous virtual clients, 12 seeded buyer accounts, 400 requests: 0 errors, elapsed 1778 ms, API p95 680 ms on this local machine. This smoke is not a production capacity guarantee.
- Frontend build/lint passed; npm audit reports 0 vulnerabilities.

All mutation tests target dedicated databases with CloneEbayBuyer_Test_ prefixes. Do not point these scripts at application data. Browser tests require E2E_ALLOW_MUTATIONS=isolated-database; on Windows E2E_BROWSER_PATH may point to Brave. Test setup and CI runner are in scripts/ci-buyer-integration.sh.
