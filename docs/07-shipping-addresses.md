# Buyer shipping addresses

Implemented account address book and the authenticated delivery-address step at `/checkout`. Full checkout, shipping quotations, order creation and payment do not exist in this checkout yet. No migration, schema alteration or production seed data is introduced.

## eBay behavior and project decisions

Official references reviewed on 3 November 2026:

- [Changing the address on your eBay account](https://www.ebay.com/help/Account/Settings/Changing_the_address_on_your_eBay_account?id=4201): manage several shipping addresses, add/edit/delete, designate a primary address, start checkout with the primary address, choose a different saved address or add one at checkout. New checkout addresses are saved for future purchases. Selecting a different address does not itself request a new primary address.
- [Changing shipping details after purchase](https://www.ebay.com/help/buying/shipping-delivery/changing-shipping-details-purchase?id=4028): an account-address edit does not rewrite an existing purchase destination; generally the buyer must contact the seller and may need cancellation/repurchase.

The clone follows those behaviors for saved shipping addresses. Registration/billing/return addresses are different concepts and are outside this task.

These are explicit project choices, not claims about undocumented eBay internals:

- The first saved address becomes primary automatically. Later addresses are not primary unless explicitly requested.
- Deleting the primary address promotes the remaining address with the lowest ID (oldest saved entry). Deleting the last address is allowed; checkout then requires a new address.
- A primary address cannot be unset without a replacement; choose another primary or delete it.
- Address fields are trimmed and length-checked against SQL v2. Country codes use the runtime ISO region catalog, normalized to uppercase; country names are derived server-side. Country can be changed on a shipping address. Optional fields can be blank, accommodating destinations without postal codes/states. No postal/geocoding/carrier deliverability verification is claimed. Legacy rows without countryCode remain editable through partial updates without losing their stored country.
- Existing order shipping snapshots are immutable. On address deletion, only the nullable `OrderTable.addressId` reference is cleared before deleting the Address row; all order snapshot fields, totals and statuses are unchanged. The current schema's FK otherwise prevents deletion. This behavior requires review by the order owner when integrating checkout.
- Account status is checked in SQL on every address read/write, so a suspended account cannot continue using an older valid token.

## API contract

All endpoints require a Bearer JWT. The server derives userId from the token. Body/query userId never chooses an owner. Responses use the existing `ApiEnvelope<T>` from the authentication module. A foreign ID returns the same 404 as a missing address.

| Method | Endpoint | Success | Description |
|---|---|---|---|
| GET | `/api/addresses` | 200 | Owned addresses, primary first, then ID |
| GET | `/api/addresses/countries` | 200 | Supported country code/name options |
| GET | `/api/addresses/{addressId}` | 200 | Resolve an owned destination, including checkout selection |
| POST | `/api/addresses` | 201 + Location | Save an address |
| PATCH | `/api/addresses/{addressId}` | 200 | Partial edit |
| PATCH | `/api/addresses/{addressId}/default` | 200 | Make primary, idempotent |
| DELETE | `/api/addresses/{addressId}` | 204 with no body | Delete from address book |

Create example:

```json
{
  "fullName": "Nguyen Van A",
  "phone": "+84 901234567",
  "street": "123 Main Street",
  "addressLine2": "Apartment 2",
  "city": "Hanoi",
  "state": "",
  "postalCode": "10000",
  "countryCode": "VN",
  "isDefault": false
}
```

fullName, phone, street, city and countryCode are required for create. Limits: fullName 150, phone 30, street/addressLine2 255, city/state 100, postalCode 30, countryCode two letters from the supported list. Text control characters are rejected. Phone must contain digits; international punctuation is allowed. `country` and ownership fields are not client-controlled.

PATCH supports the same fields. Omitted/null fields retain their current values; `""` clears an optional field. This is ordinary application/json partial-update semantics, not RFC JSON Merge Patch. Example: `{ "street": "456 New Street", "addressLine2": "" }`.

An address response includes `id`, `fullName`, `phone`, `street`, nullable `addressLine2`, `city`, nullable `state`, nullable `postalCode`, `country`, nullable `countryCode`, and `isDefault`. It does not expose user navigation properties or order graphs.

| Status | Code | Meaning |
|---|---|---|
| 400 | `ADDRESS_INVALID` | Invalid field; errors include field name |
| 400 | `ADDRESS_ID_INVALID` | Non-positive, malformed or overflowing ID |
| 400 | `VALIDATION_ERROR` | Malformed JSON/model binding |
| 401 | `AUTH_UNAUTHORIZED` | Missing/invalid/expired JWT or unusable subject |
| 403 | `AUTH_ACCOUNT_UNAVAILABLE` | User missing or not ACTIVE |
| 404 | `ADDRESS_NOT_FOUND` | Missing or foreign address |
| 409 | `ADDRESS_DEFAULT_REQUIRED` | Attempt to unset primary without replacement |
| 409 | `ADDRESS_CONFLICT` | Concurrent mutation failure; reload before retrying |
| 503 | `ADDRESS_UNAVAILABLE` | Database configuration absent |
| 429 | `RATE_LIMIT_EXCEEDED` | Existing API rate limit |

Other unexpected errors use the existing global 500 envelope.

## Ownership and atomicity

Controller → AddressService → IAddressRepository → AddressRepository → CloneEbayDbContext. Application operates on Domain ShippingAddress values, never generated EF models. The repository maps and persists changes; the service owns primary/deletion/validation policies.

SQL Server mutations use a serializable transaction and lock the parent User using `UPDLOCK, HOLDLOCK` before reading its addresses. This also protects an initially empty book. All address mutations must use this boundary. Demotions are flushed before promotions to avoid a transient violation of `UX_Address_OneDefaultPerUser`. FK unlinking, deletion and replacement-primary writes commit or roll back together. There is no automatic retry that might duplicate a create after an uncertain response. Existing schema has no address rowversion, so concurrent edits of different fields follow serialized last-write semantics; no ETag guarantee is claimed.

## Checkout handoff

`/checkout` is currently only the Ship to step. It reuses the same address editor/list, initially selects primary, lets buyers choose another address, and revalidates ownership through GET before displaying the chosen destination. New addresses are persisted and selected. Selection stays in the mounted checkout page; navigating away/reloading restarts with primary and it does not change the account's primary address.

The checkout owner should embed the address selection in the eventual checkout flow, retain its `addressId` in checkout state and call `AddressService.SelectForCheckoutAsync` at preview/confirmation. Do not trust an address snapshot posted by the browser. At order creation, resolve the current owned address inside the checkout transaction, recalculate shipping/tax, and copy its fields into existing `OrderTable.shipping*` columns. Order creation must coordinate against concurrent address mutation, for example by taking the same user lock with the same lock ordering. Do not hold a transaction while waiting for payment-provider network calls. Guest checkout uses its own validated shipping input, never another user's saved addresses.

No address selection means no order may be created. A deleted destination must cause reselection rather than silently sending an order elsewhere. Shipping-country eligibility comes from listing/shipping policy in the future shipping feature, not the list of address-book countries.

## Verification

`dotnet test EbayClone.sln --configuration Release` covers authenticated CRUD, multiple addresses, identity spoofing, missing JWT/subject, foreign-resource checks, malformed IDs/JSON, field validation, default switching, deletion promotion, rollback, checkout selection, inactive accounts, and historical snapshots. Repository tests use a fresh in-memory SQLite relational database with the existing model and a test-only `sysutcdatetime` function. They do not create or modify the existing SQL Server database.

SQLite verifies persistence, the unique default index, transactions and order FK handling. It cannot establish SQL Server lock behavior. For database-specific concurrency acceptance, use a team-approved disposable SQL-v2 database: issue parallel first-address creates and parallel primary changes for the same user and verify exactly one primary remains; also race delete versus checkout using the shared lock boundary. Do not run the destructive bootstrap against existing data.

Manual UI checks:

1. Start backend/frontend with existing auth configuration, sign in, open `/addresses`.
2. Save the first address, verify primary badge; add a second and verify both persist on return to the page.
3. Edit one field and clear apartment information; cancel another edit and verify no write occurred.
4. Make the second address primary; delete it with confirmation and verify the first becomes primary.
5. Open `/checkout`, choose a non-primary address, click Use this address and verify the account primary remains unchanged.
6. Add/edit an address from checkout, select it and verify it is also in the address book.
7. Delete the selected address in another tab, then click Use this address: show an error/reload instead of accepting a stale destination.
8. Verify empty state, last-address deletion, invalid input, signed-out redirect and API/network failure handling.

Automated build/lint do not substitute for browser accessibility/visual QA or SQL Server concurrency testing.
