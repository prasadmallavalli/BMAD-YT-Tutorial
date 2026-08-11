---
baseline_commit: NO_VCS
---

# Story 3.1: OrderStatus Full Transition Table

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want the full per-OrderType OrderStatus transition table implemented in OrderStatusService,
so that Orders can progress through their complete lifecycle with invalid transitions blocked.

## Acceptance Criteria

1. **Given** `OrderStatusService` (AD-4), **When** extended, **Then** its allowed-transition table is partitioned by `OrderType` per the locked sequences (see Dev Notes).
2. **And Given** a requested transition not in the table for the Order's `OrderType`, **When** `TransitionTo` is called, **Then** it returns a `Result<T>` failure without persisting a status change or firing a notification.
3. **And Given** a valid transition, **When** `TransitionTo` succeeds, **Then** it fires `INotifier.Notify(OrderStatusChangedNotification)` only after the `UnitOfWork` commits (unchanged from Epic 2's foundation).
4. **And Given** `OrderFlow.Tests`, **When** complete, **Then** `TransitionTo` is tested for at least one valid and one invalid transition per `OrderType`, including the Rush-specific `Cancelled` restriction.

## Tasks / Subtasks

- [x] Task 1: Extend `OrderStatus` enum with the remaining lifecycle values (AC: #1)
  - [x] `OrderFlow.Domain/OrderStatus.cs`: add `Processing = 2`, `Shipped = 3`, `Delivered = 4`, `Cancelled = 5` alongside the existing `Unspecified = 0`/`Confirmed = 1`. Update the file's stale comment ("`OrderStatus`'s remaining lifecycle values... are pending Story 3.1") since this story is what fills it in.
  - [x] **No migration needed.** `OrderConfiguration.cs` has no explicit conversion for `Order.Status` — EF Core's default enum→`int` column mapping already covers every value; adding enum members changes no schema. Confirm `OrderFlow.DAL`/`Migrations/` is untouched.
- [x] Task 2: Extend `OrderStatusService.AllowedTransitions` with the full per-`OrderType` table (AC: #1, #2, #3)
  - [x] `OrderFlow.BLL/OrderStatusService.cs`: extend the existing `private static readonly IReadOnlyDictionary<OrderType, IReadOnlyDictionary<OrderStatus, OrderStatus[]>> AllowedTransitions` — **do not reshape it**, only add entries to each `OrderType` partition, keeping the Story 2.4 `Unspecified → [Confirmed]` entry exactly as-is for both types:
    - `OrderType.Standard`: `Unspecified → [Confirmed]` (unchanged), `Confirmed → [Processing, Cancelled]`, `Processing → [Shipped, Cancelled]`, `Shipped → [Delivered]`.
    - `OrderType.Rush`: `Unspecified → [Confirmed]` (unchanged), `Confirmed → [Processing, Cancelled]`, `Processing → [Shipped]` (no `Cancelled` — this is the Rush-specific restriction AC #4 requires a test for; Rush orders begin processing immediately, so once `Processing` starts, cancellation is no longer offered), `Shipped → [Delivered]`.
    - **`Delivered` and `Cancelled` are terminal — do not add dictionary entries for them.** A status with no entry in its `OrderType` partition already fails correctly via the existing `!transitionsForType.TryGetValue(order.Status, out var allowedNextStatuses)` check (same mechanism the `OrderType`-not-in-table case already uses) — adding an empty-array entry would be redundant with what "no entry" already does.
  - [x] No other change to `TransitionTo`'s logic, `OrderStatusChangedNotification`, `NotificationLogEntry`, or `INotifier`/`InAppNotifier` — the method signature, the fetch-mutate-save-then-notify flow, and the `ConcurrencyConflictException` → `Result` translation are all unchanged from Story 2.4 (AC #3 is satisfied by code that already exists; this story only widens the transition table it consults).
- [x] Task 3: `OrderFlow.Tests` — extend `OrderStatusServiceTests.cs` with per-`OrderType` valid/invalid transition coverage (AC: #4)
  - [x] Add tests reusing the exact `Mock<IOrderRepository>`/`Mock<IUnitOfWork>`/`Mock<INotifier>` setup pattern already established in this file's 5 existing tests (construct an `Order` at a given `Status`/`OrderType`, call `TransitionTo`, assert on `result`/`order.Status`/`SaveChangesAsync`/`Notify` call counts):
    - `TransitionTo_Standard_ConfirmedToProcessing_Succeeds` (valid).
    - `TransitionTo_Standard_ProcessingToCancelled_Succeeds` (valid — exercises the "`Cancelled` reachable from `Confirmed` **or** `Processing`" half of Standard's rule not covered by the Confirmed→Cancelled case alone).
    - `TransitionTo_Standard_ShippedToCancelled_ReturnsFailureAndDoesNotNotify` (invalid — "not after `Shipped`" applies to both `OrderType`s).
    - `TransitionTo_Rush_ConfirmedToProcessing_Succeeds` (valid).
    - `TransitionTo_Rush_ProcessingToCancelled_ReturnsFailureAndDoesNotNotify` (**invalid — this is the Rush-specific restriction AC #4 explicitly names**: unlike Standard, Rush's `Processing` state has no `Cancelled` entry at all).
    - `TransitionTo_Rush_ConfirmedToCancelled_Succeeds` (valid — confirms Rush's `Cancelled` path still works from `Confirmed`, only `Processing→Cancelled` is blocked).
  - [x] Every failure-path test asserts `order.Status` unchanged, `mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never)`, `mockNotifier.Verify(n => n.Notify(It.IsAny<OrderStatusChangedNotification>()), Times.Never)` — matches the existing `TransitionTo_WithNoMatchingTableEntry_...` test's assertion shape exactly.
- [x] Task 4: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 8 projects) — 0 errors, 0 warnings.
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` passes, including all new tests, and every pre-existing test still passes.
  - [x] Confirm no `OrderFlow.DAL`/`OrderFlow.Presentation` file was touched — this story is `OrderFlow.Domain` (one enum) + `OrderFlow.BLL` (one dictionary) + `OrderFlow.Tests` only.

### Review Findings

- [x] [Review][Patch] Test coverage doesn't exercise every new transition-table entry this story added — no test for `Processing→Shipped` or `Shipped→Delivered` (either `OrderType`), Standard's `Confirmed→Cancelled` path is never asserted (only `Processing→Cancelled` is), and no test proves a terminal state (`Delivered`) actually rejects further transitions. AC #4's literal "at least one valid/invalid per OrderType" is satisfied, but the file's own header comment claims the "full lifecycle" is locked down. Fix: add `TransitionTo_Standard_ProcessingToShipped_Succeeds`, `TransitionTo_Standard_ShippedToDelivered_Succeeds`, `TransitionTo_Standard_ConfirmedToCancelled_Succeeds`, `TransitionTo_Rush_ProcessingToShipped_Succeeds`, `TransitionTo_Rush_ShippedToDelivered_Succeeds`, and one terminal-state rejection test. [`OrderFlow/OrderFlow.Tests/OrderStatusServiceTests.cs`]
- [x] [Review][Defer] `INotifier.Notify(...)` is called after `SaveChangesAsync()` succeeds with no try/catch — if `Notify` throws, the status change is already persisted but the caller gets an unhandled exception instead of a `Result<T>` [`OrderFlow/OrderFlow.BLL/OrderStatusService.cs`] — deferred, pre-existing since Story 2.4, unmodified by this story.
- [x] [Review][Defer] The pre-existing `TransitionTo_OnConcurrencyConflict_ReturnsFriendlyFailureAndDoesNotNotify` test (Story 2.4) never asserts `order.Status` after the failure, leaving the in-memory-mutation-before-save-attempt behavior unverified [`OrderFlow/OrderFlow.Tests/OrderStatusServiceTests.cs`] — deferred, pre-existing test gap, not introduced by this story.
- [x] [Review][Defer] `OrderStatus.Unspecified = 0` doubles as both a real pipeline state and the CLR default for an uninitialized `Order.Status`, so a mapping bug or missing default could silently look like a legitimate "new order" rather than a data-integrity error [`OrderFlow/OrderFlow.Domain/OrderStatus.cs`] — deferred, pre-existing sentinel-value characteristic from Story 2.1/2.4, not introduced by this story.
- Dismissed as noise / already adjudicated in Story 2.4's own review / matches documented convention / unreachable / speculative (12): only `ConcurrencyConflictException` is caught around `SaveChangesAsync`/`GetByIdAsync`, other infra exceptions propagate unhandled (matches the architecture's Consistency Convention that infrastructure exceptions surface through the global handler, not per-call try/catch — and the `ConcurrencyConflictException`-specific catch was itself a deliberate, reviewed addition during Story 2.4's own code review); the tracked entity's `Status` mutation isn't reverted in-memory on a concurrency-save failure (unreachable under AD-5's enforced per-operation `IServiceScope` discipline — no code path reuses a `UnitOfWork` instance beyond one business operation); the failure message exposes raw enum names/order IDs with no localization (matches the PRD's explicit "not a production system" non-goal framing); the failure message conflates three distinct validation-failure causes into one string (Story 2.4's own code review already dismissed this exact concern as noise); `AllowedTransitions`'s `OrderStatus[]` leaf values being technically mutable inside an `IReadOnlyDictionary` (Story 2.4's own code review already dismissed this exact concern — the field is private and never exposed); optimistic concurrency untested against a real `DbContext`/RowVersion (already tracked in `deferred-work.md` since Story 1.2 as a broader DAL-testing-infrastructure gap); no `CancellationToken` anywhere in the BLL (already tracked in `deferred-work.md` since Story 1.2, codebase-wide); a future new `OrderType` with no matching `AllowedTransitions` partition would fail silently (speculative — `OrderType`'s full set is explicitly "pinned" per Story 2.1, no new type is anticipated); heavy per-test mock boilerplate with no shared fixture/builder (matches this codebase's established, already-accepted test-authoring convention — every BLL test file uses the identical pattern); trusting `ConcurrencyConflictException.Message` as the user-facing string (by design — `DefaultMessage` is `ConcurrencyConflictException`'s own documented single source of truth); no constructor null-argument validation (Story 2.4's own code review already dismissed this exact concern as matching the established "trust internal callers" convention); `SaveChangesAsync` returning 0 rows affected without throwing (unreachable — no delete/remove feature exists anywhere in this app for `Order`, per `IOrderRepository`'s interface).

## Dev Notes

- **The full transition table is locked verbatim by epics.md's own Epic 3 "Epics-level decision"** (not left to this story to design): *Standard:* `Confirmed → Processing → Shipped → Delivered`, with `Cancelled` reachable from `Confirmed` or `Processing` (not after `Shipped`). *Rush:* same forward sequence, but `Cancelled` is reachable only from `Confirmed` — "Rush orders begin processing immediately, so once `Processing` starts it's committed." Implement exactly this; there is no design decision left for this story to make.
- **`AllowedTransitions` is widened, not rebuilt.** Story 2.4 already created this dictionary with exactly one entry per `OrderType` (`Unspecified → Confirmed`) specifically so this story could extend it without reshaping — see Story 2.4's own Dev Notes: *"Do **not** add `Processing`/`Shipped`/`Delivered`/`Cancelled`... in this story — those values and their per-`OrderType` transitions are Story 3.1's job."* Keep the existing entries untouched; only add new `OrderStatus` keys to each `OrderType`'s inner dictionary.
- **No Domain/DAL migration needed.** `Order.Status` has no explicit EF conversion (`OrderConfiguration.cs` doesn't configure one) — it already maps to a plain `int` column via EF Core's default enum handling, so adding four new enum members is a zero-schema-impact change. Do not generate a migration for this story.
- **No Presentation change in this story.** `OrderStatusService.TransitionTo` has no caller yet outside `StandardOrderProcessor`/`RushOrderProcessor`'s `Unspecified → Confirmed` call (Story 2.5) — a UI to actually *invoke* a `Processing`/`Shipped`/`Delivered`/`Cancelled` transition is Story 3.3's job. This story only makes the service capable of accepting those transitions when called.
- **`TransitionTo` itself needs zero logic changes.** Its guard (`!transitionsForType.TryGetValue(...) || !transitionsForType[...].TryGetValue(...) || !allowedNextStatuses.Contains(newStatus)`), its notify-only-after-save ordering, and its `ConcurrencyConflictException` → `Result` translation (added during Story 2.4's own code review) already handle an arbitrarily-sized transition table correctly — this story is purely a data change to the table it reads.
- **Naming/DTO shapes are unchanged**: `OrderStatusChangedNotification`, `NotificationLogEntry`, `INotifier`/`InAppNotifier` are untouched — Story 3.4 (Notification Visibility) is what eventually reads `InAppNotifier.GetLog()`, not this story.
- **Trust internal callers convention continues**: no new validation is added to `TransitionTo` beyond the table-membership check the AC already requires — matches every prior BLL story's established "internal BLL entry point, not a system boundary" reasoning.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.Domain/
    OrderStatus.cs                     # modified: + Processing/Shipped/Delivered/Cancelled
  OrderFlow.BLL/
    OrderStatusService.cs              # modified: AllowedTransitions widened per OrderType
  OrderFlow.Tests/
    OrderStatusServiceTests.cs         # modified: +6 tests (valid/invalid per OrderType, Rush restriction)
```

`OrderFlow.DAL`/`OrderFlow.Presentation`/`OrderFlow.Presentation.Tests` are untouched by this story.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.1: OrderStatus Full Transition Table] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3: Order Lifecycle Visibility & Notifications] — "Epics-level decision" locking the exact per-OrderType transition sequences, including the Rush-specific Cancelled restriction
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-4 — OrderStatus transitions + notifications are BLL-orchestrated] — `OrderStatusService` as sole owner of the transition table, partitioned by OrderType, sole caller of `INotifier.Notify`
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Deferred] — "Full OrderStatus sequence, incl. exact Cancelled reachability, per OrderType (PRD §8 Q2) — Epics fills OrderStatusService's OrderType-partitioned transition table within AD-4's contract" — confirms this is an Epics-level (not Architecture-level) decision, already made in epics.md
- [Source: _bmad-output/implementation-artifacts/2-4-order-status-foundation-notification-plumbing.md#Dev Notes] — the exact `Unspecified → Confirmed`-only table this story extends, and the explicit instruction that Story 3.1 "extends it, not changing this table's shape"
- [Source: _bmad-output/implementation-artifacts/2-4-order-status-foundation-notification-plumbing.md#Dev Agent Record] — `TransitionTo`'s established `ConcurrencyConflictException` → `Result` translation (added during 2.4's own code review) that this story's wider table inherits for free
- [Source: _bmad-output/implementation-artifacts/deferred-work.md] — confirms `OrderStatus` currently has only `Unspecified`/`Confirmed` pinned "by design," pending this story

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.BLL/OrderFlow.BLL.csproj`: Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet build OrderFlow.sln` (all 8 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 64, Skipped: 0, Total: 64 (58 prior + 6 new).

### Completion Notes List

- `OrderStatus` (`OrderFlow.Domain`) extended with `Processing = 2`, `Shipped = 3`, `Delivered = 4`, `Cancelled = 5` alongside the existing `Unspecified`/`Confirmed`. No migration generated — `OrderConfiguration.cs` has no explicit conversion for `Order.Status`, so it already maps to a plain `int` column via EF Core's default enum handling; confirmed `OrderFlow.DAL/Migrations/` is untouched.
- `OrderStatusService.AllowedTransitions` (`OrderFlow.BLL`) widened per epics.md's locked Epic 3 decision, keeping Story 2.4's `Unspecified → Confirmed` entries unchanged for both `OrderType`s: Standard gained `Confirmed → [Processing, Cancelled]`, `Processing → [Shipped, Cancelled]`, `Shipped → [Delivered]`; Rush gained the same except `Processing → [Shipped]` only (no `Cancelled` — the Rush-specific restriction). `Delivered`/`Cancelled` are terminal — no dictionary entries added for them, relying on the existing `TryGetValue` miss path. `TransitionTo`'s logic, notify-after-save ordering, and `ConcurrencyConflictException` handling were not touched.
- `OrderStatusServiceTests.cs` gained 6 new tests: one valid + one invalid transition per `OrderType`, plus the Rush-specific restriction explicitly required by AC #4 (`TransitionTo_Rush_ProcessingToCancelled_ReturnsFailureAndDoesNotNotify`) and its Standard counterpart proving `Processing→Cancelled` *is* valid for Standard (`TransitionTo_Standard_ProcessingToCancelled_Succeeds`), isolating the asymmetry to exactly the one table cell that differs.
- `dotnet build` is green across all 8 projects with 0 warnings. `OrderFlow.Tests` is 64/64 passing.
- No `OrderFlow.DAL`/`OrderFlow.Presentation` file touched — confirmed via File List below; this story is `OrderFlow.Domain` (one enum) + `OrderFlow.BLL` (one dictionary) + `OrderFlow.Tests` only.

### File List

- `OrderFlow/OrderFlow.Domain/OrderStatus.cs` (modified: + `Processing`/`Shipped`/`Delivered`/`Cancelled`)
- `OrderFlow/OrderFlow.BLL/OrderStatusService.cs` (modified: `AllowedTransitions` widened per `OrderType`)
- `OrderFlow/OrderFlow.Tests/OrderStatusServiceTests.cs` (modified: +6 tests; modified during code review: +6 more tests closing the full-lifecycle/terminal-state coverage gap)
- `OrderFlow/OrderFlow.DAL/AppDbContext.cs` (modified during code review — **not part of this story's own scope**: found missing the `Orders`/`OrderItems` `DbSet`s and `OrderConfiguration`/`OrderItemConfiguration` registrations, an unrelated pre-existing regression discovered while re-verifying the build after the review patch; restored with user confirmation)

## Change Log

- 2026-08-10: Implemented Story 3.1 — `OrderStatus` enum extended with the full lifecycle; `OrderStatusService.AllowedTransitions` widened per `OrderType` per epics.md's locked decision, including the Rush-specific "no `Cancelled` after `Processing`" restriction. No migration needed (no schema change). `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 64/64 passed (58 prior + 6 new).
- 2026-08-10: Code review applied — 1 patch, 3 deferred, 12 dismissed. Real finding: test coverage only satisfied AC #4's literal "at least one valid/invalid per OrderType" — no test exercised `Processing→Shipped`, `Shipped→Delivered` (either type), Standard's `Confirmed→Cancelled` path, or a terminal-state rejection — fixed by adding 6 more tests closing every remaining transition-table cell. Three findings deferred to `deferred-work.md` (unguarded `Notify` call, an existing 2.4 test not asserting post-failure state, the `Unspecified=0` sentinel/CLR-default ambiguity) — all pre-existing, not introduced by this story. Twelve findings dismissed: several were already explicitly adjudicated during Story 2.4's own code review on this same unmodified code (mutable array leaves, message-cause conflation, no constructor null-arg validation); others matched documented architecture convention (infrastructure exceptions surface via the global handler, not per-call try/catch) or already-tracked `deferred-work.md` entries (no `CancellationToken`, concurrency untested against a real `DbContext`); one was unreachable (no delete feature exists for `Order`) and one speculative (`OrderType`'s set is explicitly pinned, no new type anticipated). Separately, `dotnet build` failed on re-verification due to `OrderFlow.DAL/AppDbContext.cs` missing the `Orders`/`OrderItems` `DbSet`s and their `OnModelCreating` registrations — an unrelated pre-existing regression (not caused by this story) discovered mid-review; restored with user confirmation. `dotnet build`/`dotnet test` re-verified green (0 warnings) after all changes — 70/70 passing (58 → 64 → 70).
