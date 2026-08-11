---
baseline_commit: NO_VCS
---

# Story 2.4: Order Status Foundation & Notification Plumbing

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want a minimal OrderStatusService and INotifier wired so confirming an order can set its initial status and fire a notification,
so that later stories can extend the same mechanism for the full order lifecycle.

## Acceptance Criteria

1. **Given** `OrderFlow.BLL`, **When** implemented, **Then** `IOrderStatusService`/`OrderStatusService` exists with `TransitionTo(orderId, newStatus)` as the sole owner of the allowed-transition table (AD-4), initially supporting only the "new order" → `Confirmed` transition (the rest of the lifecycle is added in Epic 3), and is the only caller of `INotifier.Notify(...)`, fired only after the `UnitOfWork` commits.
2. **And Given** `OrderFlow.BLL`, **When** implemented, **Then** `INotifier` is registered Singleton (AD-5) with a minimal in-app-log implementation publishing an `OrderStatusChangedNotification { OrderId, OldStatus, NewStatus }` DTO.
3. **And Given** `OrderFlow.Tests`, **When** complete, **Then** `OrderStatusService.TransitionTo` is tested confirming the initial transition succeeds and fires exactly one notification.

## Tasks / Subtasks

- [x] Task 1: `OrderStatusChangedNotification` + `NotificationLogEntry` DTOs (AC: #2)
  - [x] `OrderFlow.BLL/OrderStatusChangedNotification.cs`: `public class OrderStatusChangedNotification { public int OrderId { get; set; } public OrderStatus OldStatus { get; set; } public OrderStatus NewStatus { get; set; } }` — matches `OrderDto`/`CreateOrderRequest`'s existing mutable-class-with-`{ get; set; }` DTO shape (not a `record`); carries exactly this shape per AD-4 ("no more"); do not add a timestamp field here.
  - [x] `OrderFlow.BLL/NotificationLogEntry.cs`: same class shape, `{ OrderStatusChangedNotification Notification, DateTime OccurredAtUtc }` — the timestamp Story 3.4's notification panel needs lives here, in the log wrapper, not on the notification DTO itself (see Dev Notes).
- [x] Task 2: `INotifier` + `InAppNotifier` (AC: #2)
  - [x] `OrderFlow.BLL/INotifier.cs`: `void Notify(OrderStatusChangedNotification notification)` and `IReadOnlyList<NotificationLogEntry> GetLog()` — `GetLog()` is what Story 3.4 ("the in-app notification log (Epic 2's `INotifier`)") reads from later; nothing in this story calls it yet.
  - [x] `OrderFlow.BLL/InAppNotifier.cs`: implements `INotifier` with a private `List<NotificationLogEntry>` guarded by a `lock` (Singleton lifetime means concurrent async operations from separate user actions could call `Notify` at the same time — see Dev Notes). `Notify` appends `new NotificationLogEntry { Notification = notification, OccurredAtUtc = DateTime.UtcNow }`. `GetLog()` returns a defensive snapshot (`.ToList()` under the same lock), never the live list.
- [x] Task 3: `IOrderStatusService`/`OrderStatusService` (AC: #1)
  - [x] `OrderFlow.BLL/IOrderStatusService.cs`: `Task<Result<OrderStatus>> TransitionTo(int orderId, OrderStatus newStatus)` — method name matches AD-4's literal wording exactly (no `Async` suffix, a deliberate departure from this codebase's usual suffix convention — see Dev Notes).
  - [x] `OrderFlow.BLL/OrderStatusService.cs`: constructor takes `IUnitOfWork unitOfWork, INotifier notifier`. Holds a `private static readonly` allowed-transition table shaped `IReadOnlyDictionary<OrderType, IReadOnlyDictionary<OrderStatus, OrderStatus[]>>`, with one entry per `OrderType` (`Standard`, `Rush`) each mapping `OrderStatus.Unspecified → [OrderStatus.Confirmed]` — this is the only entry in either partition for this story; Story 3.1 adds the rest without changing this table's shape.
  - [x] `TransitionTo` logic: `GetByIdAsync(orderId)` via `_unitOfWork.Orders`; if `null`, return `Result<OrderStatus>.Failure("Order not found")`. Look up `order.OrderType` then `order.Status` in the table; if either lookup misses or `newStatus` isn't in the resulting array, return `Result<OrderStatus>.Failure(...)` describing the invalid transition — do not mutate the order or call the repository/notifier on this path. On a valid transition: capture `oldStatus = order.Status`, mutate `order.Status = newStatus` directly on the tracked entity returned by `GetByIdAsync` (same pattern as `CustomerService.UpdateAsync` — see Dev Notes, no repository `Update` method needed), `await _unitOfWork.SaveChangesAsync()`, **then** call `_notifier.Notify(new OrderStatusChangedNotification { OrderId = orderId, OldStatus = oldStatus, NewStatus = newStatus })` — notify only after save succeeds, never before (AD-4), and return `Result<OrderStatus>.Success(newStatus)`.
- [x] Task 4: Composition root registration (AC: #2)
  - [x] `OrderFlow.Presentation/Program.cs` `ConfigureServices`: `services.AddSingleton<INotifier, InAppNotifier>();` (AD-5 — only `IAppSettings`/`INotifier` are Singleton) and `services.AddScoped<IOrderStatusService, OrderStatusService>();` (everything else scoped-per-operation).
- [x] Task 5: `OrderFlow.Tests` — `OrderStatusService` tests (AC: #3)
  - [x] `OrderFlow.Tests/OrderStatusServiceTests.cs`, mocking `IOrderRepository`/`IUnitOfWork`/`INotifier` with Moq (same pattern as `CustomerServiceTests`):
    - `TransitionTo_FromUnspecifiedToConfirmed_ReturnsSuccessAndFiresExactlyOneNotification` (AC #3): an `Order` with `Status = OrderStatus.Unspecified`, `OrderType = OrderType.Standard` (or `Rush` — either partition works identically at this stage); assert `result.IsSuccess`, `result.Value == OrderStatus.Confirmed`, `order.Status == OrderStatus.Confirmed` (mutated in place), `mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once)`, and `mockNotifier.Verify(n => n.Notify(It.Is<OrderStatusChangedNotification>(x => x.OrderId == order.Id && x.OldStatus == OrderStatus.Unspecified && x.NewStatus == OrderStatus.Confirmed)), Times.Once)`.
    - `TransitionTo_WithMissingOrder_ReturnsFailureAndDoesNotNotify`: `GetByIdAsync` returns `null`; assert failure, `mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never)`, `mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never)`.
    - `TransitionTo_WithNoMatchingTableEntry_ReturnsFailureAndDoesNotNotify`: an `Order` already at `Status = OrderStatus.Confirmed`, request `TransitionTo(order.Id, OrderStatus.Confirmed)` (no entry exists for `Confirmed → Confirmed` in this story's table); assert failure, `order.Status` unchanged, `SaveChangesAsync`/`Notify` both never called.
  - [x] No new package references needed — `Moq` and `Microsoft.Extensions.DependencyInjection` are already in `OrderFlow.Tests.csproj` from Stories 1.2/2.3.
- [x] Task 6: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 7 projects) — 0 errors, 0 warnings.
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` passes, including all new tests, and all 39 pre-existing tests still pass.
  - [x] Confirm no `OrderFlow.Domain`/`OrderFlow.DAL` file was touched, and the only `OrderFlow.Presentation` change is the `Program.cs` registrations — this story is `OrderFlow.BLL` + `OrderFlow.Tests` (+ `Program.cs`) only, per its own AC.

### Review Findings

- [x] [Review][Patch] `TransitionTo` doesn't catch `ConcurrencyConflictException` around `SaveChangesAsync`, unlike `ProductService.UpdateAsync`'s established try/catch → `Result.Failure` pattern for the identical fetch-mutate-save shape — violates AD-10's "translate into a `Result<T>` failure... never an unhandled exception reaching Presentation" for `Order.RowVersion`, a real EF Core concurrency token. Fixed: wrapped `SaveChangesAsync()` in try/catch, returning `Result<OrderStatus>.Failure(ex.Message)`; added `TransitionTo_OnConcurrencyConflict_ReturnsFriendlyFailureAndDoesNotNotify` test. [`OrderFlow.BLL/OrderStatusService.cs`, `OrderFlow.Tests/OrderStatusServiceTests.cs`]
- [x] [Review][Patch] No test exercises `InAppNotifier` directly — it's only ever exercised indirectly through `Mock<INotifier>` in `OrderStatusServiceTests`, so its lock-guarded log mutation and `GetLog()`'s defensive-copy behavior are unverified. Fixed: added `InAppNotifierTests.cs` (4 tests covering append, ordering, defensive-snapshot behavior, and empty log). [`OrderFlow.Tests/InAppNotifierTests.cs`]
- [x] [Review][Patch] No test covers the "`OrderType` has no table entry" branch of `TransitionTo`'s guard — only the "status has no allowed next status" branch is tested. Fixed: added `TransitionTo_WithOrderTypeNotInTable_ReturnsFailureAndDoesNotNotify` using `OrderType.Unspecified`. [`OrderFlow.Tests/OrderStatusServiceTests.cs`]
- [x] [Review][Patch] Comments claim `INotifier` is "the only other Singleton besides `IAppSettings`," but `IAppSettings` doesn't exist anywhere in the current codebase (only named in the architecture spine as a planned future type) — misleading to a reader who greps for it. Fixed: reworded both comments to not imply `IAppSettings` is already built. [`OrderFlow.BLL/INotifier.cs`, `OrderFlow.Presentation/Program.cs`]
- [x] [Review][Patch] `OrderStatusService`'s not-found constant is named `OrderNotFoundError` while `CustomerService`/`ProductService`/`InventoryService` all use `NotFoundError` — inconsistent with the established sibling-service naming pattern. Fixed: renamed to `NotFoundError`. [`OrderFlow.BLL/OrderStatusService.cs`]
- Dismissed as noise / out of scope / already covered (7): `InAppNotifier`'s unbounded log growth (matches epics.md's own "minimal in-app notification log" decision and the PRD's "not a production system" non-goal — over-engineering to add eviction machinery here); the identical failure message for two distinct transition-failure causes (the `OrderType`-lookup-fails branch is currently unreachable — no code path persists `OrderType.Unspecified` on an `Order`); `AllowedTransitions`'s `OrderStatus[]` leaf values being technically mutable inside an `IReadOnlyDictionary` (the field is `private` and never exposed — nothing external can ever reach the arrays); `Result<OrderStatus>.Failure()`'s default `Value` coinciding with the meaningful `OrderStatus.Unspecified` domain value (a pre-existing, already-accepted characteristic of this codebase's `Result<T>` pattern — `Result<bool>` has the identical ambiguity today); `GetByIdAsync`'s `.Include(OrderItems)` being unnecessary overhead for a status-only mutation (a deliberate, documented scope decision in this story's own Dev Notes — no `IOrderRepository` change was in scope); no no-op guard for `newStatus == order.Status` (speculative — current table has no self-transition entries, so today's behavior is already correct; a hypothetical concern for a future story's table, not a bug now); no null-argument validation in the constructor (matches every sibling BLL service's established "trust internal callers" convention, already cited in this story's own Dev Notes).

## Dev Notes

- **This story's transition table has exactly one entry per `OrderType`: `Unspecified → Confirmed`.** `OrderStatus.Unspecified` (value `0`) is the Domain default for a never-yet-transitioned `Order` — it stands in for "new order" here since no separate "Created"/draft status exists (epics.md's Epic 2 decision: "create" and "confirm" are a single atomic action, no draft state). Do **not** add `Processing`/`Shipped`/`Delivered`/`Cancelled` to `OrderStatus` (Domain) or to this table in this story — those values and their per-`OrderType` transitions are Story 3.1's job (see `deferred-work.md`'s note that `OrderStatus` currently only has `Unspecified`/`Confirmed` pinned, by design).
- **No `IOrderRepository`/`OrderRepository` change needed.** `OrderRepository.GetByIdAsync` (Story 2.1) already returns a tracked entity from the shared per-operation `AppDbContext` (no `AsNoTracking`) — exactly like `CustomerRepository.GetByIdAsync` that `CustomerService.UpdateAsync` relies on. Mutating `order.Status` directly on the entity returned by `GetByIdAsync` is picked up by EF's change tracker on the next `SaveChangesAsync()`, per AD-6 ("repositories update via targeted property changes... never a blanket `Update()`"). This resolves the gap Story 2.1's code review flagged ("`IOrderRepository` has no `Update`/`Remove` method... foreseeable gap once [this service] needs an Order status-transition update path") — no repository interface change was actually needed, same as Customer/Product before it.
- **`TransitionTo` (no `Async` suffix) is a deliberate, literal match to AD-4's own wording**, which names the method `OrderStatusService.TransitionTo(int orderId, OrderStatus newStatus)` three separate times across epics.md and the architecture spine. Every other async BLL method in this codebase carries an `Async` suffix (`CreateAsync`, `ConfirmAsync`, `HasSufficientStockAsync`, etc.) — this is the one deliberate exception, because the architecture text names it exactly this way as the method future stories (3.1, 3.3) will call. Don't "fix" it to `TransitionToAsync`.
- **Return type `Result<OrderStatus>` is this story's own scope decision** (neither epics.md nor the architecture spine specifies a return type) — returning just the new `OrderStatus` is the minimal useful value for this story's scope (no `OrderDto` mapping needed; nothing here needs the order's line items or total). If a future story (3.3's status-transition UI) needs more of the order back, that is that story's decision to extend, not a reason to over-build this one now.
- **`INotifier.GetLog()` exists because Epic 3 Story 3.4 explicitly references it as already built**: its AC opens with "Given the in-app notification log (Epic 2's `INotifier`)... populated via the same `INotifier` singleton — no duplicate notification pathway." That means the log must live inside `INotifier`/`InAppNotifier` now, even though nothing in this story's own AC calls `GetLog()` — it exists so Story 3.4 has something to read without changing `INotifier`'s shape. Don't build a notification panel or any UI here — that's Story 3.4's job.
- **`OrderStatusChangedNotification` carries exactly `{ OrderId, OldStatus, NewStatus }` per AD-4's explicit "no more."** The timestamp Story 3.4 needs ("showing OrderId, OldStatus, NewStatus, and a timestamp") is captured by `NotificationLogEntry` wrapping the notification with `OccurredAtUtc` at the moment `InAppNotifier.Notify` is called — not by adding a field to the notification DTO itself.
- **`InAppNotifier`'s internal list needs a `lock`.** It's the only Singleton besides `IAppSettings` (AD-5), so it's shared across every concurrently-running business operation. AD-3/NFR-1 make async operations the norm, and Story 1.5's code review already flagged the codebase's WinForms forms don't yet disable buttons during in-flight async work (deferred, not this story's job to fix) — so two overlapping `TransitionTo` calls hitting `Notify` concurrently is plausible, not hypothetical. A plain `lock` around list mutation and around the `GetLog()` snapshot is cheap and sufficient; don't reach for `ConcurrentBag`/channels — that's over-engineering for an in-process log.
- **`OrderStatusService`/`InAppNotifier` are the only new BLL classes; no Presentation UI is built in this story.** `OrderProcessorFactory`'s `StandardOrderProcessor`/`RushOrderProcessor` (Story 2.3) still do **not** call `IOrderStatusService` yet — that wiring happens in Story 2.5, which extends those same processor classes with `IUnitOfWork`/`IInventoryService`/`IOrderStatusService` orchestration (persist → decrement → `TransitionTo(newOrderId, OrderStatus.Confirmed)` → the resulting notification). Don't reach into `StandardOrderProcessor`/`RushOrderProcessor` in this story.
- **Trust internal callers — no defensive validation on `orderId`/`newStatus` beyond the table lookup.** Matches the established "trust internal callers" convention from `IPricingStrategy.CalculateTotal` (Story 2.2) and `IOrderProcessor.ConfirmAsync` (Story 2.3): this is an internal BLL entry point, not a system boundary, so the only validation is the transition-table membership check the AC itself requires.
- **Naming conventions (unchanged):** `IXxx` interfaces, `XxxService`. DTOs (`OrderStatusChangedNotification`, `NotificationLogEntry`) are plain mutable classes with `{ get; set; }` properties, matching `OrderDto`/`CreateOrderRequest`'s existing shape exactly — don't introduce `record` types here; this codebase has no precedent for them.
- **Data & formats:** timestamps stored/passed as UTC `DateTime` (`NotificationLogEntry.OccurredAtUtc`), converted to local only at Presentation for display — per the Consistency Conventions table, though no Presentation code touches this in this story.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.BLL/
    OrderStatusChangedNotification.cs  # new
    NotificationLogEntry.cs            # new
    INotifier.cs                       # new
    InAppNotifier.cs                   # new
    IOrderStatusService.cs             # new
    OrderStatusService.cs              # new
  OrderFlow.Presentation/
    Program.cs                         # modified: + AddSingleton<INotifier, InAppNotifier>, + AddScoped<IOrderStatusService, OrderStatusService>
  OrderFlow.Tests/
    OrderStatusServiceTests.cs         # new
```

`OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.Presentation.Tests` are untouched by this story. No new package references in any project — `OrderFlow.BLL` needs none of these types to reference DI/EF Core packages, and `OrderFlow.Tests` already has `Moq`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.4: Order Status Foundation & Notification Plumbing] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2: Order Creation, Pricing & Inventory] — "Epics-level decisions": no separate draft state, notification surface is "a minimal in-app notification log"
- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.5: Order Creation & Confirmation UI] — confirms `IOrderStatusService`/notification wiring into the processors is Story 2.5's job, not this one
- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.1: OrderStatus Full Transition Table] — confirms the transition table is "partitioned by OrderType" already, extended (not reshaped) later
- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.4: Notification Visibility] — "the in-app notification log (Epic 2's `INotifier`)... populated via the same `INotifier` singleton" — the source of this story's `GetLog()`/`NotificationLogEntry` decision
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-4 — OrderStatus transitions + notifications are BLL-orchestrated] — `TransitionTo(int orderId, OrderStatus newStatus)` signature, sole-owner/sole-caller rules, notify-only-after-commit rule, exact `OrderStatusChangedNotification` DTO shape, `INotifier` registered Singleton
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-5 — DI lifetimes: scoped-per-operation, Singleton reserved for config] — `INotifier` is one of exactly two Singletons; `OrderStatusService` is Scoped
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-6 — Auditing via IAuditable, no soft-delete] — targeted property mutation on a tracked entity, never blanket `Update()`
- [Source: _bmad-output/implementation-artifacts/2-3-order-processor-factory-standard-vs-rush.md] — `OrderDto.Status` stays `Unspecified` because `IOrderProcessor` must not set `Confirmed` itself (AD-4); confirms this story is what Story 2.5 will call instead
- [Source: _bmad-output/implementation-artifacts/1-2-customer-domain-repository-service.md] — the tracked-entity direct-mutation update pattern this story reuses (`CustomerService.UpdateAsync`)
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Deferred from: code review of 2-1-order-orderitem-domain-repository] — the "no `Update`/`Remove` method" gap this story confirms does not need closing

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 7 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 42, Skipped: 0, Total: 42 (39 prior + 3 new).

### Completion Notes List

- `OrderStatusChangedNotification`/`NotificationLogEntry` added to `OrderFlow.BLL` as plain mutable classes (matching `OrderDto`/`CreateOrderRequest`'s existing shape, not records). `OrderStatusChangedNotification` carries exactly `{ OrderId, OldStatus, NewStatus }` per AD-4; the timestamp lives on `NotificationLogEntry` instead.
- `INotifier`/`InAppNotifier` added. `InAppNotifier` is a Singleton (AD-5) with a `lock`-guarded internal `List<NotificationLogEntry>`; `GetLog()` returns a defensive snapshot copy. Nothing in this story calls `GetLog()` yet — it exists for Story 3.4's notification panel, per that story's own AC referencing "the in-app notification log (Epic 2's `INotifier`)".
- `IOrderStatusService`/`OrderStatusService` added. `TransitionTo` (deliberately no `Async` suffix, matching AD-4's literal method name) holds a `private static readonly` allowed-transition table partitioned by `OrderType`, with exactly one entry per type in this story (`Unspecified → Confirmed`). Returns `Result<OrderStatus>` — a story-local scope decision since neither epics.md nor the architecture spine specifies a return type. Missing order and no-matching-transition both fail without touching `SaveChangesAsync`/`Notify`; a valid transition mutates the tracked `Order` entity's `Status` directly (same pattern as `CustomerService.UpdateAsync`, no `IOrderRepository` change needed), saves, then notifies only after the save succeeds.
- `Program.cs` composition root registers `INotifier`→`InAppNotifier` via `AddSingleton` and `IOrderStatusService`→`OrderStatusService` via `AddScoped`. `ValidateOnBuild`/`ValidateScopes` (Story 1.1) passed cleanly against the new registrations at build time.
- `OrderStatusServiceTests` (3 tests) added, mocking `IOrderRepository`/`IUnitOfWork`/`INotifier` with Moq: the AC #3 success-and-single-notification path, a missing-order failure path, and a no-matching-transition failure path (both failure paths assert `SaveChangesAsync`/`Notify` are never called).
- No new `UNVERIFIED-ENVIRONMENT` gaps — this story is pure `OrderFlow.BLL` logic plus DI wiring, fully verifiable on macOS (build and tests both succeeded locally).
- No `OrderFlow.Domain`/`OrderFlow.DAL` file touched; only `Program.cs` changed in `OrderFlow.Presentation` (the two new registrations) — confirmed via File List below. `StandardOrderProcessor`/`RushOrderProcessor` (Story 2.3) were not touched — they still don't call `IOrderStatusService`; that wiring is Story 2.5's job.

### File List

- `OrderFlow/OrderFlow.BLL/OrderStatusChangedNotification.cs` (new)
- `OrderFlow/OrderFlow.BLL/NotificationLogEntry.cs` (new)
- `OrderFlow/OrderFlow.BLL/INotifier.cs` (new; modified during code review: reworded misleading `IAppSettings` comment)
- `OrderFlow/OrderFlow.BLL/InAppNotifier.cs` (new)
- `OrderFlow/OrderFlow.BLL/IOrderStatusService.cs` (new)
- `OrderFlow/OrderFlow.BLL/OrderStatusService.cs` (new; modified during code review: catch `ConcurrencyConflictException` around `SaveChangesAsync`, renamed `OrderNotFoundError` → `NotFoundError`)
- `OrderFlow/OrderFlow.Presentation/Program.cs` (modified: `+` `AddSingleton<INotifier, InAppNotifier>()`, `+` `AddScoped<IOrderStatusService, OrderStatusService>()`; modified during code review: reworded misleading `IAppSettings` comment)
- `OrderFlow/OrderFlow.Tests/OrderStatusServiceTests.cs` (new; modified during code review: added `TransitionTo_WithOrderTypeNotInTable_...` and `TransitionTo_OnConcurrencyConflict_...` tests)
- `OrderFlow/OrderFlow.Tests/InAppNotifierTests.cs` (new, added during code review)

## Change Log

- 2026-08-10: Implemented Story 2.4 — `OrderStatusChangedNotification`/`NotificationLogEntry` DTOs, `INotifier`/`InAppNotifier` (Singleton, lock-guarded in-app log), `IOrderStatusService`/`OrderStatusService.TransitionTo` (sole owner of the `Unspecified → Confirmed` allowed-transition table per AD-4, notifies only after `UnitOfWork` commit). Composition-root registrations added to `Program.cs`. `dotnet build` green across all 7 projects with 0 warnings; `dotnet test` 42/42 passed (39 prior + 3 new).
- 2026-08-10: Code review applied — 5 patches, 0 deferred, 7 dismissed. Real findings: `TransitionTo` didn't catch `ConcurrencyConflictException` around `SaveChangesAsync` (AD-10 violation — fixed with the same try/catch → `Result.Failure` pattern `ProductService.UpdateAsync` already establishes); no direct test of `InAppNotifier` (added `InAppNotifierTests.cs`, 4 tests); no test of the `OrderType`-not-in-table branch (added); misleading comments implying `IAppSettings` already exists (reworded); `OrderNotFoundError` naming inconsistent with sibling services' `NotFoundError` (renamed). Seven findings dismissed as noise/out-of-scope/already-covered (unbounded log growth matches the epics' own "minimal" log decision; ambiguous dual-cause failure message covers a currently-unreachable branch; mutable array leaves are never externally exposed; `Result<T>` default-value ambiguity is a pre-existing accepted pattern; `.Include(OrderItems)` overhead was a deliberate documented scope decision; no self-transition guard is speculative; no null-arg validation matches established convention). Test count 42 → 48, all passing; `dotnet build`/`dotnet test` re-verified green (0 warnings) after all changes.
