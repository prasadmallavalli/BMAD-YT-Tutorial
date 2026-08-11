# Deferred Work

## Deferred from: code review of 1-1-solution-scaffold-composition-root (2026-08-07)

- No architecture-fitness test (e.g. NetArchTest) enforcing AD-1's layer-direction rule — blocked on resolving the composition-root decision item from this review first.
- ~~No CI pipeline configured — also the only realistic path to eventually verify AC2 (`MainForm` launches without throwing) since the dev machine is macOS.~~ **Resolved 2026-08-11**: `.github/workflows/tests.yml` (windows-latest) added, repo pushed to `github.com/prasadmallavalli/BMAD-YT-Tutorial`. CI runs `dotnet test` on the whole solution on every push/PR to `main`, **and** AC2 itself is now independently verified by a dedicated smoke-test step (see below) — nothing about this item remains open.
- ~~`OrderType` enum placeholder has no explicit int values pinned.~~ **Resolved in Story 2.1** (`{ Unspecified = 0, Standard = 1, Rush = 2 }`). `OrderStatus` still has only `Unspecified`/`Confirmed` pinned — the remaining lifecycle values (`Processing`/`Shipped`/`Delivered`/`Cancelled`) are pending Story 3.1.
- ~~No `IDesignTimeDbContextFactory<AppDbContext>` — not needed until EF migrations begin in Story 1.2.~~ **Resolved in Story 1.2** (`AppDbContextFactory` added).
- No `packages.lock.json` for reproducible restores — team-practice decision, not mandated by any architecture doc.
- ~~A real DI-resolution smoke test (beyond the placeholder) is blocked by the same already-accepted AC2 Windows-verification gap — revisit once Windows/CI access exists.~~ **Resolved 2026-08-11**: `.github/workflows/tests.yml` now launches the built `OrderFlow.Presentation.exe` on windows-latest and asserts the window title is "OrderFlow Desktop" (vs. `Program.ReportFatal`'s "OrderFlow — Fatal Error" dialog, an early exit, or a timeout) within 20s. This exercises the real composition root — `ConfigureServices`, `BuildServiceProvider(ValidateOnBuild: true, ValidateScopes: true)`, and `MainForm`'s constructor — not just a placeholder. CI run #4 (commit `574708c`) passed this step. AC2 is closed.

## Deferred from: code review of 1-2-customer-domain-repository-service (2026-08-07)

- No unique constraint/index on `Customer.Email` — not required by this story's AC or the PRD, a product-scope decision for a later story.
- `CustomerService.Validate()` doesn't check `Email` is a well-formed email address — AC only requires "validates required fields," not format.
- No DAL-level tests for `CustomerRepository`/`UnitOfWork`/`StampAuditableEntries` against an in-memory/SQLite provider — the mocked-`IUnitOfWork` BLL tests already satisfy AC #4 literally; this is a future testing-infrastructure investment. **(Story 1.4 update: also extends to `ProductRepository`/`InventoryRepository` and the `Product`↔`Inventory` 1:1 relationship/`.Include` behavior, and to `UnitOfWork.SaveChangesAsync()`'s real `DbUpdateConcurrencyException`→`ConcurrencyConflictException` translation — none of it verified beyond mocked-`IUnitOfWork` tests.)**
- No `CancellationToken` threaded through `ICustomerRepository`/`IUnitOfWork`/`ICustomerService` — not mandated by architecture/epics yet; adding it now touches every method signature across three layers.
- No index on `Email`/`Name` for lookups — no `GetByEmail`-style lookup method exists in spec yet to make the gap concrete.

## Deferred from: code review of 1-3-customer-management-ui (2026-08-07)

- Root `IServiceProvider` injected into `MainForm`/`CustomerListForm` purely to call `GetRequiredService<TForm>()` is a Service Locator pattern (deliberate/disclosed tradeoff for this story) — reconsider a `Func<TForm>`/`IFormFactory` abstraction only if it recurs across 3+ more UI stories (Product/Order forms).
- No busy/loading UI indicator (cursor, disabled state, "Loading…") during any async operation — UX polish, not required by any AC.
- No empty-state messaging ("No customers found") when the grid loads zero rows — UX polish, not required by any AC.

## Deferred from: code review of 1-4-product-inventory-domain-repository-service (2026-08-07)

- No unique constraint/index on `Product.SKU` — not required by this story's AC or the PRD, same pattern as `Customer.Email` (Story 1.2).
- No validation that `UnitPrice` fits `decimal(18,2)`'s precision/scale before hitting the DB — not a realistic data-entry scenario, low priority.
- `NotFoundError = "Product not found"` duplicated verbatim across `ProductService` and `InventoryService` — rule of three, revisit if a third service duplicates it.

## Deferred from: code review of 1-5-product-management-inventory-visibility-ui (2026-08-09)

- `ProductListForm_Load` doesn't disable Add/Refresh while its own initial `LoadProductsAsync()` await is in flight, allowing an overlapping second load to race the first — pre-existing pattern from Story 1.3's `CustomerListForm_Load`, already covered by Story 1.3's "no busy/loading UI indicator" entry above.
- `ProductDetailForm`'s Save/Cancel buttons aren't guarded against each other during in-flight async work — identical to `CustomerDetailForm` (Story 1.3), same already-deferred "no busy/loading indicator" gap.
- `editButton.Enabled` recomputation is triplicated across `DataGridView_SelectionChanged`, `DisplayProducts`, and `EditButton_Click`'s `finally` — pre-existing triplication copied verbatim from `CustomerListForm` (Story 1.3) per this story's explicit "mirror Story 1.3" instruction.
- `ProductDetailForm.Designer.cs` never assigns `AcceptButton`/`CancelButton` despite `FormBorderStyle = FixedDialog`, so Enter/Esc do nothing — identical gap in `CustomerDetailForm.Designer.cs` (Story 1.3).
- ~~`ProductDetailPresenterTests.SaveAsync_OnFailure_ShowsErrorAndReturnsFalse` only covers the `CreateAsync`-failure branch, never `UpdateAsync`-failure — low-risk (shared post-processing code), mirrors an identical gap already accepted in `CustomerDetailPresenterTests` (Story 1.3).~~ **Resolved 2026-08-11**: added `SaveAsync_OnUpdateFailure_ShowsErrorAndReturnsFalse` to both `ProductDetailPresenterTests` and `CustomerDetailPresenterTests` (existing test renamed to `SaveAsync_OnCreateFailure_ShowsErrorAndReturnsFalse` for symmetry); CI-verified passing on windows-latest.
- `MainForm`'s list-form buttons resolve a new Transient form on every click with no single-instance tracking, allowing unlimited duplicate non-modal windows — identical pre-existing pattern from Story 1.3 (disclosed Service Locator tradeoff).
- `ProductListForm.DisplayProducts` relies entirely on `AutoGenerateColumns = true` with no currency formatting or explicit column headers — same pre-existing pattern as `CustomerListForm` (Story 1.3), UX polish not required by any AC.
- `ProductDetailForm` has no unsaved-changes confirmation on Cancel and no client-side validation before calling `SaveAsync` — identical to `CustomerDetailForm` (Story 1.3), UX polish not required by any AC.

## Deferred from: code review of 2-1-order-orderitem-domain-repository (2026-08-09)

- `AppDbContext.SaveChangesAsync(CancellationToken)` (the async override) has no `DbUpdateConcurrencyException`→`ConcurrencyConflictException` translation, unlike the synchronous `SaveChanges()` override in the same file — pre-existing asymmetry since Story 1.4's code review (which only patched the sync path). Harmless today since nothing calls the async override for an `UPDATE` outside `UnitOfWork.SaveChangesAsync()`'s own translation, but should be closed before that changes.
- `IOrderRepository` has no `Update`/`Remove` method (matches `IProductRepository`'s precedent) — foreseeable gap once Epic 3's `OrderStatusService` needs an Order status-transition update path. Out of Story 2.1's explicit Domain+DAL-only scope.

## Deferred from: code review of 2-2-pricing-strategy-order-total-calculation (2026-08-10)

- No architecture-fitness test guards AD-11's "single `IPricingStrategy` registration, no keyed dispatch" rule — nothing stops a future story from quietly adding a second registration or keyed dispatch, and build/tests would stay green throughout. Same category as Story 1.1's already-deferred "no architecture-fitness test (e.g. NetArchTest) enforcing AD-1's layer-direction rule" — revisit together if that testing investment is ever made.

## Deferred from: code review of story-2-5-order-creation-confirmation-ui (2026-08-10)

- Near-total duplication between `StandardOrderProcessor`/`RushOrderProcessor`'s `ConfirmAsync` orchestration (stock-check/persist/decrement/transition steps copy-pasted, only total calculation differs) — rule-of-three not yet met, matches this codebase's established `NotFoundError`-duplication precedent (Story 1.4/1.5).
- Sequential per-item `await` loops for stock-check and inventory-fetch cause N round-trips per order instead of a batched query — performance nitpick, not correctness; out of scope for this non-production demo app.
- Create Order screen never displays the computed total (incl. rush surcharge) to the user before or after confirmation — UX polish not required by any AC.
- No client-side stock-level feedback on the quantity input — `quantityNumericUpDown` allows up to 100000 regardless of the selected product's actual stock — UX polish not required by any AC; server-side rejection (AC #3) already covers correctness.
- `RemoveItemButton_Click` silently no-ops when no grid row is selected, unlike every other empty/invalid-state path on the form which calls `ShowError` — UX polish not required by any AC.
- `quantityNumericUpDown` is not reset to 1 after "Add Item", so the next add silently reuses the previous quantity — UX polish not required by any AC.

## Deferred from: code review of story-3-1-orderstatus-full-transition-table (2026-08-10)

- `INotifier.Notify(...)` is called after `SaveChangesAsync()` succeeds with no try/catch — if `Notify` throws, the status change is already persisted but the caller gets an unhandled exception instead of a `Result<T>`. Pre-existing since Story 2.4, unmodified by Story 3.1.
- The pre-existing `TransitionTo_OnConcurrencyConflict_ReturnsFriendlyFailureAndDoesNotNotify` test (Story 2.4) never asserts `order.Status` after the failure, leaving the in-memory-mutation-before-save-attempt behavior unverified. Pre-existing test gap, not introduced by Story 3.1.
- `OrderStatus.Unspecified = 0` doubles as both a real pipeline state and the CLR default for an uninitialized `Order.Status`, so a mapping bug or missing default could silently look like a legitimate "new order" rather than a data-integrity error. Pre-existing sentinel-value characteristic from Story 2.1/2.4, not introduced by Story 3.1.

## Deferred from: code review of story-3-2-order-list-detail-view (2026-08-10)

- `order.Total.ToString("C")` uses the thread's current culture with no pinned format — on a non-USD-culture machine this renders in an unexpected currency symbol/format. UX polish not required by any AC; no established culture-pinning convention exists anywhere in this codebase to follow.
- `StandardOrderProcessor`/`RushOrderProcessor`'s Step 1 has no validation rejecting an empty `Items` list or a zero/negative `Quantity` line item. Pre-existing since Story 2.5 (unmodified by Story 3.2), and unreachable via the only existing caller (`OrderCreateForm` already guards both cases).

## Deferred from: code review of story-3-3-order-status-transition-ui (2026-08-11)

- Unhandled exceptions from `OrderStatusService.TransitionTo`'s `GetByIdAsync`/`SaveChangesAsync` calls, and from `OrderDetailForm`'s async-void handlers generally, rely entirely on `Program.cs`'s global `Application.ThreadException`/`AppDomain.UnhandledException` fatal-error handler rather than a graceful per-operation `Result` failure. Verified this is the identical try/finally-without-catch pattern already present in every other Form in this codebase (`CustomerDetailForm`, `ProductDetailForm`, `OrderCreateForm`, etc.) — not introduced by Story 3.3.
- ~~The new `TransitionToAsync_OnSuccess_ReturnsTrue`/`TransitionToAsync_OnFailure_ShowsErrorAndReturnsFalse`/updated `LoadAsync_*` tests in `OrderDetailPresenterTests.cs` compile but cannot execute on this macOS dev machine (missing `Microsoft.WindowsDesktop.App` runtime) — same already-accepted **UNVERIFIED-ENVIRONMENT** gap as Stories 2.5/3.1/3.2.~~ **Resolved 2026-08-11**: CI (`.github/workflows/tests.yml`, windows-latest) now executes the full `OrderFlow.Presentation.Tests` suite on every push/PR to `main`; first run passed, so these tests (and the rest of the suite) are no longer environment-unverified.
- Whether two users can concurrently issue two different *legal* transitions from the same starting status (e.g. both racing `Confirmed → Processing` and `Confirmed → Cancelled`) is safely resolved by a genuine EF concurrency token, or just last-writer-wins, isn't verifiable from Story 3.3's files alone — `TransitionTo`'s validate-then-save logic is unchanged from Story 2.4/3.1, out of this story's scope.
- If `OrderDetailForm` is closed while a `TransitionToAsync`/`LoadAsync` await is still pending, the `finally` block's control access (`transitionButton.Enabled = ...`) could throw `ObjectDisposedException`. Identical pre-existing pattern to every other async Form in this codebase (no disposal guard anywhere).

## Deferred from: code review of story-3-4-notification-visibility (2026-08-11)

- `MainForm`'s `GetLog()` snapshot and `Notified +=` subscription in its constructor aren't atomic, which could in principle drop or duplicate a notification if `Notify()` fired in that exact window. Verified unreachable given the app's actual call graph today — `Notify()` is only reachable via a user-clicked Transition button on an already-open `OrderDetailForm`, which requires `MainForm`'s constructor (and `Application.Run`'s message loop) to have already completed. Latent hazard only if a future story adds a `Notify()` path independent of UI action (e.g. a background sync).
- `InAppNotifier._log` (Singleton, lives the app's whole lifetime) and `MainForm._notifications` (the bound `BindingList`) have no cap or eviction — unbounded memory growth over a long session, and `GetLog()`'s O(n) lock-held copy compounds as the log grows. Demo-scale app, not required by any AC.
- `Notifier_Notified`'s `InvokeRequired` check can return a false negative before a Form's window handle exists — same underlying invariant as the snapshot/subscribe race above: unreachable today since `Notify()` can't fire before `MainForm`'s handle is created (no code path reaches `TransitionTo` before the message loop is pumping and a Form is on screen).
- A `Notify()` call already in-flight when `MainForm` closes could invoke `Notifier_Notified` after or concurrently with `OnFormClosed`'s unsubscribe/disposal. Narrow race requiring an in-flight order transition on an owned Form at the exact moment the owner (`MainForm`) closes; WinForms closes owned Forms with their owner, further narrowing the window. Same systemic "no disposal guard during an in-flight await" pattern already deferred from Story 3.3's review (`OrderDetailForm`).
- New notifications append to the bottom of `notificationDataGridView` with no auto-scroll or newest-first ordering — UX polish, not required by any AC, same category as this project's many already-deferred UX items (Story 1.3/1.5/2.5).

## Deferred from: code review of stories 4-1/4-2/4-3-exhibit-pairs (2026-08-11)

Reviewed together since all three share `OrderFlow.Exhibits/Program.cs`/`.csproj`. Acceptance Auditor found zero AC violations across all three stories (hand-verified SRP/OCP "equivalent output" claims and DIP's Before/After `CustomerLookupService` diff). Most adversarial/edge-case findings (missing null-guards, no validation on discount magnitudes, negative-quantity handling) were dismissed as unreachable — every exhibit `Runner.Run()` only ever passes fixed hardcoded sample data; there is no external caller or input path into this sandbox.

- `Program.cs`'s dispatch is case-sensitive with no trimming and no `--help`/alias handling — a typo (`Before-Srp`, trailing space) silently falls through to the generic usage block instead of a targeted hint. Minor interactive-UX polish, not required by any AC.
- `Program.cs` never sets a non-zero exit code on invalid/missing arguments — the `default` case prints usage and still exits 0, indistinguishable from success to any script/CI wrapper. No automation invokes this project today, per its own "interview purposes only" isolation (AD-8).
- The `Id=1, Name="Ada Lovelace", Email="ada@example.com"` customer fixture (Story 4.3) is hand-duplicated across `Before/Dip/SqlCustomerRepository.cs`, `After/Dip/SqlCustomerRepository.cs`, and `After/Dip/FakeCustomerRepository.cs` with no single source of truth. The Before/After duplication itself is an explicit, documented design choice; only the third file's copy is unaccounted for, and it's a one-line teaching fixture — low value to fix.
