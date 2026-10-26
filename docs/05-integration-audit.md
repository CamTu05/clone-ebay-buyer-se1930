# Architecture Integration Audit

## Purpose

Assess the initialized Buyer Module against the official requirements, architecture conventions, database design, API contract draft, and SQL Server schema without implementing business features or executing destructive database operations.

## Sources Reviewed

- `docs/01_Requirements_Task_Breakdown_Buyer_Clone_eBay.docx`
- `docs/02_Architecture_Conventions_Clone_eBay.docx`
- `docs/03_Database_Design_Clone_eBay.docx`
- `docs/04_API_Contract_Clone_eBay_Buyer.md`
- `database/clone_ebay_complete_schema_v2.sql`

The DOCX contents were read from their WordprocessingML document XML. The SQL script was inspected only and was not executed.

## 1. What Already Matches

### Solution and layers

- The solution has separate `Buyer.API`, `Buyer.Application`, `Buyer.Domain`, and `Buyer.Infrastructure` projects.
- Existing project references follow the required dependency direction:
  - API references Application and Infrastructure.
  - Application references Domain.
  - Infrastructure references Application and Domain.
  - Domain has no project dependency.
- Test projects are separated into unit and integration tests.
- Existing project names differ from the document examples but preserve the same four-layer responsibilities. Renaming is not required for architecture compliance.

### API baseline

- Controllers, middleware, Swagger, CORS, rate limiting, health checks, logging, and built-in dependency injection are configured in API.
- Global unhandled-exception handling does not leak exception details.
- The existing controller is thin and has no direct `DbContext` access.
- The documented unversioned `/api/<resource>` baseline is now used by the system sample route.

### Application and Domain boundaries

- Application currently depends only on Domain.
- Domain is independent from API and Infrastructure.
- No business logic or persistence implementation has leaked into controllers.
- Empty feature folders provide space for application services, DTOs, interfaces, domain entities, enums, and value objects.

### Infrastructure and persistence preparation

- EF Core 8 SQL Server packages are installed in Infrastructure.
- `BuyerDbContext` exists and scans the Infrastructure assembly for Fluent API configurations.
- Infrastructure registration is isolated in `DependencyInjection.cs`.
- No database is required for current health/sample endpoints.
- No migration is applied automatically and no destructive SQL is executed.

### Frontend

- React, TypeScript, Vite, React Router, Axios, and Tailwind CSS are configured.
- The frontend has shared API and component folders plus feature placeholders.
- Backend URLs are environment-driven rather than duplicated in feature code.
- Loading, error, navigation, not-found, and backend-status UI are present.

### Tests and delivery preparation

- Integration smoke tests use `WebApplicationFactory`.
- A unit test guards Domain project independence.
- Dockerfiles, Compose, Nginx, and GitHub Actions are prepared.
- CI restores, builds, and tests the backend and lints/builds the frontend.
- CI now covers the documented `develop` integration branch as well as `main`.
- Git workflow documentation consistently uses personal branches into `develop`, then stable promotion to `main`.

## 2. Conflicts

### Authentication persistence

The official SQL schema defines a custom `dbo.User` table with an `INT IDENTITY` key, `passwordHash`, verification flags, account status, and related `ExternalLogin`, `VerificationToken`, and `OtpChallenge` tables. Default ASP.NET Core Identity would create a separate `AspNetUsers` model.

This is the highest-risk architecture decision. Creating default Identity tables now would produce two user systems with different keys and lifecycle state. Authentication persistence must not be implemented until the team chooses one approved strategy.

Recommended strategy for the two-week scope: retain the official custom `User` schema as the single source of persistence and use ASP.NET Core's proven password-hashing and JWT components through application abstractions. If the team requires full Identity managers, implement and review a custom store/mapping against the official schema rather than creating default Identity tables.

### API route prefix

- Architecture and API contract: `/api/<resource>`.
- Initial scaffold: `/api/v1/...`.

The existing sample endpoint and its test/request file were aligned to `/api/system/status`. API versioning should only be introduced later through a team-approved contract change.

### Error representation

- The official convention requires the custom success/error envelope.
- Current unhandled exceptions use ASP.NET Core `ProblemDetails`.
- The sample status endpoint returns a bare response DTO.

No global response rewrite was added because the API contract is marked draft and detailed validation/error mapping remains unresolved. Before the first business endpoint, the team must choose whether every application error uses the custom envelope and whether `ProblemDetails` remains only for infrastructure/unhandled failures.

### Social login priority

- Requirements scope lists Google/Facebook login as P0.
- Requirements also allow social login to be deferred when time is insufficient.
- Database design classifies `ExternalLogin` as P2.

The team must set one sprint priority before assigning authentication work.

### Schema ownership and bootstrap

- The SQL file is a complete clean-bootstrap script and contains `SINGLE_USER`, `ROLLBACK IMMEDIATE`, `DROP DATABASE`, and `CREATE DATABASE`.
- Team conventions require EF Core migrations.
- The current EF model is empty.

The script was not executed. The team must approve whether it is the schema baseline used to create the first non-destructive EF migration or remains a reference checked against manually authored mappings. There must not be two independently evolving schema sources.

### Product and Listing API semantics

The database correctly separates Product from seller-specific Listing, but the draft API has both product and listing routes without final buyer-facing projection rules. Cart, shipping, inventory, and auctions must use `listingId`; implementation must wait for agreed response DTOs.

### Duplicate stock fields

Both `Listing.quantity` and `Inventory.quantity` exist in the SQL design. The authoritative stock source and synchronization rule are not defined. Checkout and cart stock behavior must not be implemented until this is clarified.

### Guest order access

Guest checkout is P0, but secure guest order retrieval is not contracted and the schema has no guest access-token persistence. Email alone must not authorize order access.

### Tax representation

`TaxRegion.taxRate` is `DECIMAL(7,4)` and constrained from 0 to 100, but documents do not state whether 5% is stored as `5.0000` or `0.0500`. Calculation and seed behavior require an explicit decision.

## 3. What Is Missing

### Domain

- Confirmed P0 entities, enums, and value objects are not mapped in C#.
- Auction, inventory, order, and payment invariants are not represented.
- No domain exceptions or status-transition policies exist.

This is currently intentional. Entities must be implemented from the approved schema, not inferred from endpoint names.

### Application

- No feature services or use cases.
- No repository interfaces.
- No request/response DTOs.
- No validation layer.
- No current-user, clock, transaction, or provider abstractions.
- No `AddApplication` registration method.

### Infrastructure

- No `DbSet` properties, entity configurations, migrations, repository implementations, or seed implementation.
- No payment, shipping, tax, import-charge, OTP, or email provider implementation.
- Readiness does not yet test SQL Server.

### API

- No authentication/JWT configuration.
- No authorization/ownership policies.
- No standardized application response/error mapping.
- No Buyer feature controllers.
- No SignalR Auction Hub.
- No validation integration.

### Frontend

- Feature folders are placeholders only.
- No authentication/session handling, catalog, cart, checkout, order, or auction integration.
- No SignalR client.
- No frontend test setup.

### DevOps

- Nginx has no `/hubs/` WebSocket proxy configuration yet.
- ASP.NET Core forwarded-header handling is not configured, so IP rate limiting behind Nginx requires review.
- Docker configurations have not been locally validated because Docker is unavailable.
- Kubernetes and JMeter remain intentionally deferred.

## 4. What Needs Refactoring

### Required before the first feature

1. Approve the single authentication persistence strategy.
2. Approve schema ownership and first-migration strategy.
3. Approve response envelope and application-error mapping.
4. Resolve Product/Listing projections and stock source of truth.
5. Establish Application service/repository/provider interfaces and `AddApplication` registration.
6. Map only the entities needed by the selected first P0 vertical slice, while preserving official key, precision, timestamp, relationship, and concurrency rules.

### Safe cleanup later

- Rename assembly-marker files from `Class1.cs` without changing their types.
- Remove unused WeatherForecast and Vite starter artifacts after confirming no references.
- Separate liveness from database-aware readiness after persistence is required.
- Add trusted forwarded-header configuration before relying on client-IP rate limiting behind Nginx.
- Add Nginx WebSocket forwarding when the Auction Hub is implemented.
- Expand architecture tests to assert all layer dependency rules.

### Project naming migration plan

The documents use `CloneEbay.*`, while the initialized solution uses `Buyer.*`. The current names are internally consistent and clearly identify the independently runnable Buyer Module. Renaming projects would create broad namespace, project-reference, Docker, CI, and test churn without improving layer boundaries.

Recommended plan:

1. Keep `Buyer.*` throughout the two-week delivery.
2. Document that `Buyer.API/Application/Domain/Infrastructure` implement the corresponding `CloneEbay.*` responsibilities.
3. Rename only if integration with other group modules later requires a shared solution namespace, using a dedicated reviewed refactor after feature stabilization.

## 5. Database Compatibility Assessment

### Confirmed compatibility targets

- Database name: `CloneEbayDB_V2`.
- Primary keys: mainly `INT IDENTITY`; transaction/history tables use `BIGINT IDENTITY`.
- Money: `DECIMAL(18,2)`.
- Timestamps: `DATETIME2(3)` and UTC application handling.
- `Product` and `Listing` are separate.
- `CartItem` references `Listing`.
- Guest checkout uses nullable buyer plus guest email and snapshots.
- One `CheckoutGroup` can create many seller-specific `OrderTable` records.
- Auction and Inventory use SQL Server `ROWVERSION`.
- Order and item records preserve price, tax, shipping, address, title, and image snapshots.
- Payment and Order statuses remain independent.

### Current compatibility status

The current EF model contains no entities, so it neither conflicts with nor implements the schema yet. This is safer than speculative mappings but blocks persistence-backed development.

The environment example now targets `CloneEbayDB_V2`, matching the official script. The script remains a destructive reference and is not wired into application startup or Compose initialization.

### Concerns requiring approval

- Custom User versus ASP.NET Core Identity.
- Listing quantity versus Inventory quantity.
- Tax-rate storage unit and rounding.
- Buyer/guest fields permit both values because constraints require at least one, not exactly one.
- Guest order access tokens are absent.
- Mutable counters and balances need explicit transaction/concurrency policies.
- The active-listings SQL view exposes `highestBidderId`; public API projections must apply privacy rules.

## 6. Auction Preparation Boundaries

The existing architecture can support Auction without implementing the engine now:

- API: future `AuctionController` and `AuctionHub` only handle transport, authentication, groups, and responses.
- Application: future `IAuctionService` or focused bid/winner use cases own eligibility, minimum bid, proxy calculation, tie-breaking, winner resolution, and post-commit event intent.
- Domain: future Auction/Bid entities and enums protect invariant state and private maximum bids.
- Infrastructure: repository/transaction implementation reloads Auction, applies `rowVersion` concurrency, persists Bid/Auction atomically, and publishes realtime notifications only after commit.

Required sequence:

1. Load Auction inside the chosen transaction/concurrency strategy.
2. Validate buyer eligibility, state, start/end time, and minimum bid.
3. Recalculate effective bid, public current price, and highest bidder without exposing competing maximum bids.
4. Insert/update persistence state.
5. Commit successfully.
6. Broadcast `AuctionUpdated` or `AuctionEnded` through SignalR.

The complete proxy-bidding algorithm, retry policy, Hub authorization, winner scheduler, and payload redaction must be agreed before implementation.

## 7. Recommended Implementation Order

1. **Team decisions:** authentication store, social-login priority, schema authority, response/error format, Product/Listing projection, inventory source, tax-rate unit, and guest order access.
2. **Shared foundation:** Application DI, result/error abstractions, pagination DTO, repository/unit-of-work boundary, clock/current-user interfaces, validation approach, and authorization conventions.
3. **Database baseline:** map approved P0 tables and constraints in small reviewed groups; verify against SQL without running its destructive header; create a reviewed baseline migration only after approval.
4. **Authentication vertical slice:** custom User-compatible registration/login/JWT/verification foundation, avoiding a second user system.
5. **Catalog/Listing read vertical slice:** public backend DTOs and frontend list/detail, establishing Product versus Listing conventions.
6. **Address and Cart vertical slices:** ownership, listing-based cart items, and local/server cart behavior.
7. **Shipping/Tax calculators and Checkout preview:** server-authoritative calculations before order creation.
8. **Payment mock and Order creation:** `CheckoutGroup`, seller-specific orders, snapshots, and independent statuses.
9. **Auction foundation:** entity/configuration, concurrency repository, bid service tests, then API and SignalR.
10. **P1 features:** coupon, cancellation, return/refund, review, watchlist, and notifications after P0 stabilization.

## Changes Applied During This Audit

- Changed the existing sample route from `/api/v1/system/status` to `/api/system/status` and updated its integration test and HTTP request file.
- Updated `.env.example` to use `CloneEbayDB_V2`.
- Updated CI triggers to include `develop` and `main`.
- Updated README wording for official documents, destructive SQL safety, database name, and the unresolved custom User/Identity strategy.
- No project was recreated or renamed.
- No entity, migration, authentication flow, API feature, auction engine, branch, commit, or database was created or executed.

## 8. Approved Architecture Decisions

The following directions are approved and supersede earlier unresolved recommendations in this audit:

- [x] Keep the existing `Buyer.API`, `Buyer.Application`, `Buyer.Domain`, and `Buyer.Infrastructure` projects and dependency direction.
- [x] Use `dbo.User` from SQL schema v2 as the only canonical user data model.
- [x] Reuse standard ASP.NET Core password hashing and authentication security mechanisms without creating default Identity tables.
- [x] Do not create a duplicate account system.
- [x] Use EF Core migrations for future schema creation and evolution.
- [x] Treat SQL schema v2 as the canonical design and mapping reference.
- [x] Keep Product and Listing separate and preserve their distinct IDs.
- [x] Access inventory through an Application-owned `IInventoryGateway` port.
- [x] Use the documented API success/error envelope for business endpoints.
- [x] Preserve every documented `INT`/`BIGINT` identity convention.

These decisions resolve the earlier User/Identity, schema ownership, response-envelope, Product/Listing separation, inventory boundary, and key-strategy gates. They do not authorize business-feature implementation or migration execution.

## 9. Canonical User and Authentication Strategy

### Single user system

`dbo.User` and its related `ExternalLogin`, `VerificationToken`, and `OtpChallenge` tables are the only authentication persistence model. Do not add `IdentityDbContext`, `IdentityUser`, `AddIdentity`, default Identity stores, or any `AspNetUsers`, `AspNetRoles`, `AspNetUserLogins`, or related tables.

Use standard ASP.NET Core security components through adapters:

- Infrastructure implements an Application-owned `IPasswordService` using `IPasswordHasher<User>` and `PasswordHasher<User>`.
- API uses ASP.NET Core JWT bearer authentication and authorization policies.
- Infrastructure implements JWT issuance behind an Application-owned token interface.
- Application owns registration, login, verification, eligibility, and account-state use cases.
- Domain remains free from ASP.NET Core, EF Core, and JWT dependencies.

The password adapter must support `PasswordVerificationResult.SuccessRehashNeeded` and persist an upgraded hash after successful verification. A null `passwordHash` means local-password login is unavailable, which supports a future external-login-only account. Invalid email and invalid password must produce the same public credentials error. Passwords, JWTs, OTPs, raw verification tokens, and private bid maximums must never be logged.

The narrow dependency choice is `Microsoft.Extensions.Identity.Core` 8.x in Infrastructure for `IPasswordHasher<TUser>`. The API should use `Microsoft.AspNetCore.Authentication.JwtBearer` 8.x if the shared framework does not provide the required compile-time reference. No Identity persistence package belongs in Domain or Application.

### Layer placement

- **Domain:** `User`, `ExternalLogin`, `VerificationToken`, `OtpChallenge`, confirmed enums, and state invariants.
- **Application:** request/response DTOs, auth services/use cases, `IUserRepository`, `IPasswordService`, token service abstraction, `IClock`, current-user abstraction, OTP/token generation interfaces, and stable errors.
- **Infrastructure:** EF configurations/repositories, password-hasher adapter, JWT issuer, cryptographic token/OTP hashing, provider implementations, and migrations.
- **API:** thin auth controllers, authentication/authorization registration, policies, Swagger bearer definition, response-envelope translation, and HTTP current-user adapter.

When implemented, `UseAuthentication()` must execute before `UseAuthorization()`. The current scaffold does not yet register authentication, intentionally.

### JWT limitations requiring a policy decision

The canonical schema has no refresh-token/session table, security stamp, credential version, or token revocation state. Therefore a first implementation may issue short-lived access tokens, but `/api/auth/logout` can only instruct the client to discard its token. It must not claim immediate server-side revocation.

Before implementing production-style logout or refresh, approve one schema extension through an EF migration:

1. hashed refresh-token/session records with expiry and revocation metadata; or
2. a user security-version value checked during authorization; or
3. a distributed `jti` denylist until access-token expiry.

Issuer, audience, signing algorithm/key management, access-token lifetime, clock skew, claims, and account-state revalidation must be approved before JWT implementation. Real signing keys belong in user-secrets or environment/deployment secrets, never repository settings.

## 10. Concrete EF Core Mapping Strategy

Use explicit `IEntityTypeConfiguration<T>` classes in `Buyer.Infrastructure/Persistence/Configurations`. `BuyerDbContext` already scans this assembly. Do not rely on naming conventions because SQL uses `dbo.[User]`, lower-camel-case columns, `OrderTable`, named constraints, filtered indexes, and mixed `INT`/`BIGINT` keys.

### User

Map `User` to `dbo.User` explicitly:

- `Id` → `id`, `INT IDENTITY(1,1)`, generated on add.
- `Username` → `username`, required, `NVARCHAR(100)`.
- `Email` → `email`, required, `NVARCHAR(255)`, unique.
- `PasswordHash` → `passwordHash`, nullable, `NVARCHAR(500)`.
- `Role` → `role`, required, `NVARCHAR(20)`, string enum conversion, SQL default `BUYER`, and `CK_User_Role`.
- `AvatarUrl` → `avatarURL`, nullable, `NVARCHAR(1000)`.
- `PhoneNumber` → `phoneNumber`, nullable, `NVARCHAR(30)`.
- verification flags → required `BIT` values with SQL defaults.
- `AccountStatus` → `accountStatus`, required, `NVARCHAR(20)`, string enum conversion, SQL default `ACTIVE`, and `CK_User_Status`.
- timestamps → exact lower-camel-case names and `DATETIME2(3)`; creation/update defaults use `SYSUTCDATETIME()`.
- preserve the unique email constraint, non-unique username index, and filtered phone index.

Configure all User relationships as `DeleteBehavior.NoAction`. The schema uses account status `DELETED`; physical deletion would conflict with order, bid, payment, and audit history.

### ExternalLogin

- `Id` and `UserId` remain `int`; `Id` is identity.
- Map provider to `NVARCHAR(50)` and provider user ID to `NVARCHAR(255)`.
- Preserve unique `(provider, providerUserId)` and required User FK with `NoAction` deletion.
- Do not store provider access/refresh tokens in this table without a separately approved encrypted-secret design.

### VerificationToken

- Preserve `int` identity and User FK.
- Map token type as the documented string enum in `NVARCHAR(30)`.
- Store only a deterministic cryptographic digest in `tokenHash`; send the raw random token once.
- Map all times to `DATETIME2(3)` and preserve the `(userId, tokenType, expiresAt)` index and type check.
- Consume a token atomically only when unused and unexpired.

A high-entropy token may use a SHA-256 digest. A low-entropy OTP must use a keyed HMAC or equivalent protected digest; plain SHA-256 is insufficient for six-digit codes.

### OtpChallenge

- Preserve `int` identity and nullable `int userId` for pre-account registration flows.
- Preserve exact lengths, purpose check, nonnegative attempt/resend checks, UTC `DATETIME2(3)` columns, defaults, and `NoAction` User relationship.
- Attempt and verification updates must be atomic. The current table has no concurrency token, so use a conditional update/transaction or add an approved concurrency column by migration.

### Common mapping rules

- Preserve all SQL names, schemas, identity generation, nullability, lengths, precision, defaults, checks, indexes, filters, foreign keys, and delete behavior.
- Map money as `decimal(18,2)`.
- Map timestamps as UTC `DateTime` to `datetime2(3)` and enforce UTC at API/Application boundaries because SQL Server does not store `DateTime.Kind`.
- Map `Auction.RowVersion` and `Inventory.RowVersion` as generated `byte[]` values using `IsRowVersion()`/`IsConcurrencyToken()`.
- Map `Order` explicitly to `dbo.OrderTable`.
- Never expose mapped entities directly from API responses.
- Preserve `Product.Id`, `Listing.Id`, and every FK as the documented `int`; preserve transaction/history IDs such as Bid, CheckoutGroup, OrderTable, OrderItem, and Payment as `long`/`BIGINT`.

### Migration baseline process

SQL v2 is the canonical specification; EF migrations are the only future deployment/evolution mechanism. The destructive database header must never be placed in application startup or an EF migration.

For a new database:

1. Map the reviewed canonical schema.
2. Generate an initial migration without applying it.
3. Review generated SQL against SQL v2 for names, keys, identity, precision, nullability, defaults, checks, filtered/unique indexes, foreign keys, delete behavior, and rowversion.
4. After separate approval, test it only against a disposable empty `CloneEbayDB_V2`.
5. Add CI coverage that applies all migrations from empty state.

For an existing database created from SQL v2, do not run a create-all migration. First prove schema equivalence, then use a separately reviewed adoption-baseline procedure to record migration history without recreating objects. Never insert a migration-history row without a schema comparison.

Do not use `EnsureCreated` and do not automatically call `Database.Migrate()` during normal API startup.

## 11. Inventory Boundary Decision

`IInventoryGateway` belongs in Application and isolates Buyer use cases from inventory persistence or a future real service. Infrastructure supplies the implementation. Controllers must never read `Inventory` or `BuyerDbContext` directly.

The contract must support concurrency-safe availability and reservation semantics rather than simple `GetQuantity`/`UpdateQuantity` methods. Expected capabilities are availability lookup, atomic reservation, reservation commit/consumption, release, idempotency, and explicit insufficient-stock/concurrency failures.

The SQL schema still contains both `Listing.quantity` and `Inventory.quantity`. The gateway boundary is approved, but the authoritative stock field and synchronization rule remain unresolved. Do not implement cart/checkout stock mutation until the team decides whether `Inventory.quantity` is on-hand stock, whether available stock is `quantity - reservedQuantity`, and whether `Listing.quantity` is legacy/denormalized display data.

## 12. API Envelope Decision

Business endpoints must use the documented envelope:

- success: `success`, `message`, `data`, `errors = null`;
- failure: `success = false`, `message`, `data = null`, and stable `code`/optional `field` errors;
- pagination belongs inside `data` with `items`, `page`, `pageSize`, `totalItems`, and `totalPages`;
- a successful HTTP 204 remains bodyless.

Before the first business endpoint, add shared Application/API response models and map validation, authentication, authorization, not-found, conflict, concurrency, rate-limit, malformed JSON, and unhandled errors consistently. Health and Swagger endpoints may remain outside the business envelope. `ProblemDetails` must not leak as an undocumented alternative shape for business API failures; either adapt it to the approved envelope at the boundary or reserve it for non-business infrastructure endpoints with an explicit exception in the contract.

## 13. Remaining Schema Incompatibilities and Blockers

These items are not overridden by the approved architecture and require decisions or reviewed migrations before their affected features:

1. **Login identity and normalization:** username is not unique; email behavior depends on SQL collation; normalized lookup columns and whitespace/Unicode/case rules are absent. Decide whether login is email-only and define canonicalization before registration/login.
2. **Role capability:** one scalar role cannot represent a user who is both buyer and seller. Decide whether roles are exclusive or whether capabilities/multiple roles require a schema change.
3. **Lockout and token invalidation:** no failed-attempt count, lockout expiry, security version/stamp, refresh-token/session table, or rowversion exists on User.
4. **Verification-token lookup:** `tokenHash` is neither unique nor indexed. Approve token format/lookup and whether an index or fixed-size digest column is required.
5. **OTP policy:** code length, expiry, maximum attempts/resends, cooldown, supersession, lookup index, and concurrency strategy remain undefined.
6. **Phone semantics:** normalization and uniqueness are undefined; the current filtered index is non-unique.
7. **Social login:** provider priority, account linking, trusted email behavior, callback security, and `(userId, provider)` uniqueness remain unresolved.
8. **Existing database state:** determine whether each target environment starts empty or already contains SQL-v2-created objects before baselining migrations.
9. **Schema ownership:** one team/context must own migrations for shared `dbo.User`; Buyer, Seller, and Admin modules must not independently migrate it.
10. **Inventory authority:** `Listing.quantity` versus `Inventory.quantity`, reservation lifecycle, and gateway semantics remain unresolved.
11. **Product/Listing projections:** separation is approved, but search/detail DTO composition and active-listing price presentation remain unagreed.
12. **Tax:** stored values are seeded as percentages such as `5.0000`; rounding rules and whether shipping is taxable still require confirmation.
13. **Guest order access:** the schema has no secure guest access-token persistence; email alone must not authorize reads.
14. **Buyer/guest integrity:** current checks permit both `buyerId` and `guestEmail`; decide whether this is intentional or should become exclusive.
15. **JWT policy:** issuer, audience, algorithm/key rotation, lifetime, claims, refresh/revocation, logout semantics, and account-status checks remain unresolved.
16. **Auction algorithm:** proxy calculation, retry policy, winner scheduling/idempotency, Hub authorization, and payload privacy remain unresolved.
17. **Physical deletion:** User and audit entities should be soft-deleted/retained, but anonymization and PII-retention policy are not defined.
18. **Cross-table integrity:** seller/listing/shipping-policy consistency, fixed-price/auction field requirements, total decomposition, and several business cardinalities rely on application rules rather than database constraints.

## 14. Updated Implementation Order

1. Resolve auth security blockers: email/login normalization, role capability, lockout, JWT lifecycle, token/OTP policy, and shared migration ownership.
2. Define shared Application abstractions and API envelope/error mapping.
3. Implement and review exact EF mappings against SQL v2 without generating or executing a migration yet.
4. Agree whether the first migration is a complete v2 baseline for empty databases or an incremental migration sequence; define existing-database adoption separately.
5. Implement the authentication foundation against `dbo.User` only after steps 1–4 are approved.
6. Agree Product/Listing response DTOs and Inventory gateway semantics.
7. Continue with one P0 vertical slice at a time.

## Approval Status

Approved:

- [x] Four-layer projects and existing names
- [x] Canonical custom `dbo.User`
- [x] Standard ASP.NET Core password/authentication security components
- [x] No duplicate user system
- [x] EF Core migrations for future changes
- [x] SQL v2 as canonical mapping reference
- [x] Separate Product and Listing entities
- [x] Application-owned `IInventoryGateway`
- [x] Documented API response envelope
- [x] Documented `INT`/`BIGINT` key conventions

Still blocked or pending:

- [ ] Login/email/phone normalization and uniqueness policy
- [ ] Single-role versus multi-capability decision
- [ ] Lockout, JWT refresh/revocation, and logout semantics
- [ ] Verification-token and OTP security/concurrency policy
- [ ] Migration ownership and existing-database adoption plan
- [ ] Inventory source of truth and reservation semantics
- [ ] Product/Listing API projections
- [ ] Tax unit/rounding and shipping-tax policy
- [ ] Guest-order access strategy
- [ ] Auction proxy algorithm and concurrency retry policy
- [ ] Social-login sprint priority and linking rules

## 15. Database First Decision

The database strategy is now **Database First**. `CloneEbayDB_V2` already exists and was created from the official SQL v2 script. The existing database and SQL v2 document are the canonical schema and mapping sources. EF Core reverse-engineered classes represent that schema in the application; they do not authorize EF to recreate it.

This supersedes earlier recommendations to generate an initial create-all migration from C# mappings. EF Core migrations may be used only for future, explicitly approved schema changes after the reverse-engineered model is accepted. Do not execute the original SQL script, `EnsureCreated`, `Database.Migrate()` at startup, `dotnet ef database update`, or a baseline create migration against the existing database.

### Scaffold result

The database was reverse engineered into `Buyer.Infrastructure`:

- Context: `backend/Buyer.Infrastructure/Data/CloneEbayDbContext.cs`
- Entity/view models: `backend/Buyer.Infrastructure/Data/Models/`
- Context namespace: `Buyer.Infrastructure.Data`
- Model namespace: `Buyer.Infrastructure.Data.Models`
- DI connection key: `ConnectionStrings:BuyerDatabase`

The scaffold generated 39 model classes: 38 concrete table entity classes plus the keyless `vw_ActiveListing` view model. SQL table `CouponListing` is represented as an EF shared-type many-to-many join in `OnModelCreating`, so it intentionally has no dedicated CLR class. The context has 39 `DbSet` properties: 38 table sets and one view set.

The previous empty handwritten `BuyerDbContext` is no longer registered. DI registers only `CloneEbayDbContext`, avoiding competing persistence models. No `IdentityDbContext`, Identity entities, or `AspNet*` tables were generated.

### Scaffold command pattern

Use a non-committed connection source whenever possible. The performed local scaffold used Windows integrated authentication and did not contain a password. The repeatable command shape is:

```powershell
$env:ConnectionStrings__BuyerDatabase = '<local secret connection string>'
dotnet ef dbcontext scaffold `
  'Name=ConnectionStrings:BuyerDatabase' `
  Microsoft.EntityFrameworkCore.SqlServer `
  --project 'backend/Buyer.Infrastructure/Buyer.Infrastructure.csproj' `
  --startup-project 'backend/Buyer.API/Buyer.API.csproj' `
  --context 'CloneEbayDbContext' `
  --context-dir 'Data' `
  --output-dir 'Data/Models' `
  --namespace 'Buyer.Infrastructure.Data.Models' `
  --context-namespace 'Buyer.Infrastructure.Data' `
  --schema 'dbo' `
  --no-onconfiguring `
  --use-database-names
```

Do not add `--force` casually. Re-scaffolding can overwrite generated context/model files and any manual edits. Review database changes and source diffs first. Prefer partial classes and separate application/domain adapters for custom behavior instead of editing generated files.

### Connection configuration

`CloneEbayDbContext` has only an options constructor and no `OnConfiguring`, so no database credentials are embedded in source. `Buyer.Infrastructure.DependencyInjection` reads `ConnectionStrings:BuyerDatabase` and configures SQL Server through `AddDbContext<CloneEbayDbContext>()`. Local secrets must be supplied through `ConnectionStrings__BuyerDatabase`, user-secrets, or an uncommitted environment file.

### Preserved mappings

Reverse engineering preserved:

- `int` for SQL `INT` keys and foreign keys;
- `long` for SQL `BIGINT` keys and foreign keys;
- identity-generated primary keys and the non-generated shared primary key on `SellerFeedbackSummary`;
- nullable CLR properties for nullable columns;
- one-to-one, one-to-many, self-referencing, and many-to-many relationships;
- unique and filtered indexes;
- `decimal(18,2)`, `decimal(7,4)`, `decimal(10,2)`, `decimal(10,3)`, and other SQL precision declarations;
- `datetime2(3)` precision and server defaults;
- `Auction.rowVersion` and `Inventory.rowVersion` as generated rowversion/concurrency tokens;
- the keyless `vw_ActiveListings` view mapping;
- exact table/column-oriented database names through `--use-database-names`.

### Database First limitations

EF Core scaffolding did not reproduce every database artifact in the C# model:

- SQL `CHECK` constraints are enforced by the database but are not represented in the scaffolded EF model.
- EF initially inferred `User`–`Address` as one-to-one from the filtered unique default-address index. The generated mapping was corrected to one-to-many while retaining `UX_Address_OneDefaultPerUser`; future re-scaffolds must reapply or verify this correction.
- Seeded `ShippingRegion` and `TaxRegion` rows are data, not scaffolded model configuration.
- The SQL script's `CREATE DATABASE`, `DROP DATABASE`, `GO`, and bootstrap behavior are not and must never be scaffolded or executed by the application.
- Database constraint names generated automatically by SQL Server, especially primary-key and unnamed unique-constraint names, are environment-specific and appear in the generated context. Avoid treating those generated names as portable contracts.
- View definitions are not generated; only the keyless view result shape is mapped.
- Triggers, stored procedures, functions, permissions, jobs, and operational database settings would require separate inventory if added later.
- Database defaults are represented, but SQL Server does not automatically maintain all `updatedAt` values on update unless application code or a database trigger does so.

Application validation must mirror important check constraints for useful errors, while SQL Server remains the final integrity boundary.

### Database ownership rules

1. SQL Server `CloneEbayDB_V2` is the canonical deployed schema; SQL v2 is its reviewed reference.
2. `CloneEbayDbContext` and generated models are Infrastructure persistence details, not API DTOs or Domain contracts.
3. Buyer controllers and Application services must not access `CloneEbayDbContext` directly; Infrastructure repositories/gateways own EF access.
4. `dbo.User` remains the single account model. Do not add default ASP.NET Identity tables.
5. Product and Listing remain separate; inventory access flows through `IInventoryGateway`.
6. One designated database owner must coordinate future schema changes shared by Buyer, Seller, and Admin modules.
7. Future schema changes require an approved database change, an EF migration generated from the reviewed change, SQL review, and documentation update.
8. Never hand-edit an already deployed migration or run automatic migrations at API startup.
9. Re-scaffold only after an approved database change, review all overwritten/generated diffs, and restore any intentional partial-class extensions.
10. Never commit connection strings, passwords, access tokens, or generated `OnConfiguring` credentials.
