---
baseline_commit: NO_VCS
---

# Story 3.3: Order Status Transition UI

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to transition an Order through its status from the Order detail view,
so that I can track and advance its lifecycle.

## Acceptance Criteria

1. **Given** an open Order detail, **When** I view available actions, **Then** only statuses valid per the current OrderStatus/OrderType are offered; invalid transitions are not presented as options.
2. **And Given** a valid transition, **When** I select it, **Then** `OrderDetailPresenter` calls `IOrderStatusService.TransitionTo` asynchronously, and on success the detail view refreshes to show the new status.
3. **And Given** an attempted invalid transition (e.g. stale UI state), **When** `TransitionTo` rejects it, **Then** the rejection message is surfaced without a crash and the displayed status is unchanged — fulfilling FR-8.

## Tasks / Subtasks

- [x] Task 1: Extend `IOrderStatusService` with a query for allowed next statuses (AC: #1)
  - [x] **This is a new design decision this story must make** — `OrderStatusService.AllowedTransitions` (Story 2.4/3.1) is a `private static readonly` table; Presentation has no way to know "which statuses are valid to offer" without either duplicating the table (violates AD-4's sole-owner rule) or this story exposing a read-only query on the same service that already owns it.
  - [x] `OrderFlow.BLL/IOrderStatusService.cs`: add `IReadOnlyList<OrderStatus> GetAllowedNextStatuses(OrderType orderType, OrderStatus currentStatus);` — synchronous (pure in-memory table lookup, no DB access, unlike `TransitionTo`).
  - [x] `OrderFlow.BLL/OrderStatusService.cs`: implement by reusing the exact same lookup chain `TransitionTo` already uses: `return AllowedTransitions.TryGetValue(orderType, out var transitionsForType) && transitionsForType.TryGetValue(currentStatus, out var allowed) ? allowed : [];` — returns an empty list for an unknown `OrderType` or a terminal/unmapped `currentStatus` (e.g. `Delivered`, `Cancelled`), matching `TransitionTo`'s existing miss-path semantics exactly. `AllowedTransitions` itself is untouched — this story only adds a new read accessor, no table changes (the table was already completed in Story 3.1).
- [x] Task 2: Extend `IOrderDetailView`/`OrderDetailPresenter` (Story 3.2) for transitions (AC: #1, #2, #3)
  - [x] `OrderFlow.Presentation/IOrderDetailView.cs`: add `void DisplayAvailableTransitions(IReadOnlyList<OrderStatus> allowedStatuses);` alongside the existing `ShowOrder`/`ShowError`.
  - [x] `OrderFlow.Presentation/OrderDetailPresenter.cs`: `LoadAsync(int orderId)` — **within its existing single `IServiceScope`** (AD-3 already permits resolving every BLL service one operation needs from one scope), after a successful `IOrderService.GetAsync` call, also resolve `IOrderStatusService` and call `GetAllowedNextStatuses(order.OrderType, order.Status)`, then `_view.DisplayAvailableTransitions(allowed)` — call this **after** `_view.ShowOrder(order)`. On the existing not-found-failure path, do not call `DisplayAvailableTransitions` (nothing to show).
  - [x] Add `Task<bool> TransitionToAsync(int orderId, OrderStatus newStatus)`: own `IServiceScope`, resolve `IOrderStatusService`, call `TransitionTo(orderId, newStatus)`; on success return `true` (do **not** call `LoadAsync` from inside this method — see Dev Notes on why the reload is Form-orchestrated, not Presenter-internal); on failure, `_view.ShowError(result.Error!)` and return `false`.
- [x] Task 3: Extend `OrderDetailForm` UI (AC: #1, #2, #3)
  - [x] Controls (add to the existing Designer, below `statusValueLabel`, above `itemsDataGridView` — shift `itemsDataGridView`/`totalLabel`/`totalValueLabel`/`closeButton` down to make room): a `statusComboBox` (`DropDownStyle = ComboBoxStyle.DropDownList`) and a `transitionButton` (Text = "Transition").
  - [x] `DisplayAvailableTransitions(IReadOnlyList<OrderStatus> allowedStatuses)`: `statusComboBox.DataSource = allowedStatuses.ToList(); statusComboBox.Enabled = allowedStatuses.Count > 0; transitionButton.Enabled = allowedStatuses.Count > 0;` — an empty list (terminal state, e.g. viewing a `Delivered` order) disables both, with no separate "no actions available" message needed (disabled controls already communicate this, matching this codebase's established minimal-polish convention).
  - [x] `TransitionButton_Click`: `if (statusComboBox.SelectedItem is not OrderStatus newStatus) return;` guard; disable `transitionButton` for the duration; `if (await _presenter.TransitionToAsync(_orderId, newStatus)) { await _presenter.LoadAsync(_orderId); }` — **the Form, not the Presenter, chains the reload-on-success**, mirroring `OrderListForm`/`ProductListForm`'s established "reload after a successful child operation" pattern (Story 1.3/3.2) rather than one Presenter method internally calling another. `ShowError` on failure is already handled by the Presenter — no duplicate handling in the Form. Re-enable `transitionButton` in a `finally` (the subsequent `LoadAsync`, if it ran, will set the correct final enabled state via `DisplayAvailableTransitions` anyway).
- [x] Task 4: `OrderFlow.Tests` — extend `OrderStatusServiceTests.cs` (AC: #1)
  - [x] `GetAllowedNextStatuses_ForStandardConfirmed_ReturnsProcessingAndCancelled`.
  - [x] `GetAllowedNextStatuses_ForRushProcessing_ReturnsShippedOnly` — the Rush-specific restriction (no `Cancelled`) from Story 3.1, now visible through this new read accessor too.
  - [x] `GetAllowedNextStatuses_ForTerminalStatus_ReturnsEmpty` (e.g. `Delivered`).
  - [x] `GetAllowedNextStatuses_ForUnknownOrderType_ReturnsEmpty` (e.g. `OrderType.Unspecified`).
- [x] Task 5: `OrderFlow.Presentation.Tests` — extend `OrderDetailPresenterTests.cs` (AC: #1, #2, #3)
  - [x] Update the existing `LoadAsync_WithExistingOrder_ShowsOrder` test's mock setup to also mock `IOrderStatusService.GetAllowedNextStatuses`, and assert `DisplayAvailableTransitions` is called with the expected list.
  - [x] `TransitionToAsync_OnSuccess_ReturnsTrue`: mock `IOrderStatusService.TransitionTo` returning `Result<OrderStatus>.Success(...)`; assert `true` returned, `ShowError` never called.
  - [x] `TransitionToAsync_OnFailure_ShowsErrorAndReturnsFalse`: mock `TransitionTo` returning `Result<OrderStatus>.Failure("...")` (simulating AC #3's stale-UI rejection); assert `false` returned, `ShowError` called with that message.
- [x] Task 6: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 8 projects) — 0 errors, 0 warnings.
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` passes, including all new/updated tests, and every pre-existing test still passes.
  - [x] Confirm `OrderFlow.Domain`/`OrderFlow.DAL`/`Program.cs` are untouched — this story only extends already-registered types (`IOrderStatusService`/`OrderDetailForm` are both already wired into the composition root, Story 2.4/3.2); no new DI registrations are needed.

### Review Findings

- [x] [Review][Patch] `GetAllowedNextStatuses` leaks the mutable array instance from the static `AllowedTransitions` table instead of returning a defensive copy [OrderFlow.BLL/OrderStatusService.cs:91-95]
- [x] [Review][Patch] `TransitionButton_Click`'s `finally` re-enables controls from stale pre-transition state when a successful transition's follow-up reload fails [OrderFlow.Presentation/OrderDetailForm.cs:37-60]
- [x] [Review][Patch] `statusComboBox` stays enabled/interactive during an in-flight transition, unlike `transitionButton` [OrderFlow.Presentation/OrderDetailForm.cs:44]
- [x] [Review][Patch] `TransitionTo` and `GetAllowedNextStatuses` duplicate the same `AllowedTransitions` lookup chain instead of a shared private helper [OrderFlow.BLL/OrderStatusService.cs:53-58,91-95]
- [x] [Review][Patch] No presenter test asserts `LoadAsync` forwards an empty allowed-transitions list to `DisplayAvailableTransitions` (terminal-order path) [OrderFlow.Presentation.Tests/OrderDetailPresenterTests.cs]
- [x] [Review][Defer] Unhandled exceptions from `GetByIdAsync`/`SaveChangesAsync`/async-void handlers rely entirely on `Program.cs`'s global `Application.ThreadException`/`UnhandledException` fatal-error handler rather than graceful per-operation `Result` failures [OrderFlow.BLL/OrderStatusService.cs, OrderFlow.Presentation/OrderDetailForm.cs] — deferred, pre-existing
- [x] [Review][Defer] New `TransitionToAsync`/`LoadAsync` tests compile but cannot execute on this macOS dev machine (missing `Microsoft.WindowsDesktop.App` runtime) [OrderFlow.Presentation.Tests] — deferred, pre-existing
- [x] [Review][Defer] Optimistic-concurrency handling for two racing legal-but-conflicting transitions is unverifiable from this story's files alone [OrderFlow.BLL/OrderStatusService.cs:45-86] — deferred, pre-existing
- [x] [Review][Defer] Form disposed while an await is pending could throw `ObjectDisposedException` on subsequent control access in the `finally` block [OrderFlow.Presentation/OrderDetailForm.cs:54-59] — deferred, pre-existing

## Dev Notes

- **`GetAllowedNextStatuses` is a new method, not a new class.** `OrderStatusService` remains the sole owner of `AllowedTransitions` (AD-4) — this story adds a read accessor to the same table `TransitionTo` already consults, not a second source of truth. Do not duplicate the transition table anywhere in Presentation.
- **The reload-after-transition is Form-orchestrated, not Presenter-internal.** `TransitionToAsync` returns `Task<bool>` and does **not** call `LoadAsync` itself — `OrderDetailForm.TransitionButton_Click` calls `LoadAsync` again only if `TransitionToAsync` returned `true`. This mirrors `ProductListForm.OpenDetailFormAsync`/`OrderListForm` calling their list-reload method again after a successful child dialog (Story 1.3/3.2), and keeps each Presenter method exactly one business operation (AD-5) rather than one method silently spanning two scoped operations.
- **UI choice: a `ComboBox` + one "Transition" button, not one button per allowed status.** This codebase has no precedent for dynamically generating WinForms controls at runtime — every existing form uses Designer-declared static controls (`OrderCreateForm`'s `orderTypeComboBox`/`productComboBox` are the closest analog). A `ComboBox` populated from `DisplayAvailableTransitions` fits that convention directly.
- **AC #3's "stale UI state" is genuinely reachable, not just theoretical, even in this single-user desktop app**: `OrderListForm` has no duplicate-window guard (a pre-existing, already-accepted gap — see `deferred-work.md`), so a user can open two `OrderDetailForm` instances on the same Order, transition it from one, then attempt a now-stale transition from the other. `TransitionTo`'s existing table-membership check (Story 2.4/3.1) already rejects this correctly — this story's job is only to surface that rejection via `ShowError` without corrupting the displayed status, not to change `TransitionTo`'s validation.
- **No new files** — every change in this story extends an already-existing type from Story 2.4 (`IOrderStatusService`/`OrderStatusService`) or Story 3.2 (`IOrderDetailView`/`OrderDetailPresenter`/`OrderDetailForm`). No new DI registrations are needed; both are already wired into `Program.cs`.
- **Naming/DTO conventions unchanged**: `OrderStatus` values pass through Presentation the same way `OrderType` already does in `OrderCreateForm`'s `orderTypeComboBox` (Story 2.5) — a Domain enum used directly in UI binding is explicitly permitted by AD-1/AD-12 (Presentation may reference Domain only for enums/lightweight value types, never entity instances).
- **Trust internal callers convention continues**: `GetAllowedNextStatuses` performs no validation beyond the table lookup itself — matches `TransitionTo`'s own established "internal BLL entry point" reasoning.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.BLL/
    IOrderStatusService.cs             # modified: + GetAllowedNextStatuses
    OrderStatusService.cs              # modified: implement GetAllowedNextStatuses (reuses AllowedTransitions, unchanged)
  OrderFlow.Presentation/
    IOrderDetailView.cs                # modified: + DisplayAvailableTransitions
    OrderDetailPresenter.cs            # modified: LoadAsync also displays allowed transitions; + TransitionToAsync
    OrderDetailForm.cs                 # modified: + TransitionButton_Click, DisplayAvailableTransitions
    OrderDetailForm.Designer.cs        # modified: + statusComboBox, + transitionButton
  OrderFlow.Tests/
    OrderStatusServiceTests.cs         # modified: +4 tests
  OrderFlow.Presentation.Tests/
    OrderDetailPresenterTests.cs       # modified: existing test updated, +2 new tests
```

`OrderFlow.Domain`/`OrderFlow.DAL`/`Program.cs` are untouched by this story — no new files at all.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.3: Order Status Transition UI] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-4 — OrderStatus transitions + notifications are BLL-orchestrated] — sole-owner-of-the-table rule this story's `GetAllowedNextStatuses` preserves
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-5 — DI lifetimes: scoped-per-operation] — "one Presenter-method invocation" definition behind the Form-orchestrated-reload decision
- [Source: _bmad-output/implementation-artifacts/3-2-order-list-detail-view.md] — `OrderDetailForm`/`OrderDetailPresenter`/`IOrderDetailView`'s existing shape this story extends (leaf form, `LoadAsync` returning `Task`)
- [Source: _bmad-output/implementation-artifacts/3-1-orderstatus-full-transition-table.md] — the exact per-`OrderType` `AllowedTransitions` table (including the Rush `Processing`-has-no-`Cancelled` restriction) this story's `GetAllowedNextStatuses` reads from
- [Source: _bmad-output/implementation-artifacts/2-4-order-status-foundation-notification-plumbing.md] — `TransitionTo`'s existing signature/notify-after-save/`ConcurrencyConflictException` handling, unchanged by this story
- [Source: _bmad-output/implementation-artifacts/deferred-work.md] — `OrderListForm`'s no-duplicate-window-guard gap that makes AC #3's stale-UI scenario concretely reachable, not just theoretical

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.BLL/OrderFlow.BLL.csproj`: Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet build OrderFlow.Presentation/OrderFlow.Presentation.csproj` (after Task 2, before Task 3): failed as expected — `OrderDetailForm` did not yet implement the new `IOrderDetailView.DisplayAvailableTransitions` member. Resolved by Task 3.
- `dotnet build OrderFlow.Presentation/OrderFlow.Presentation.csproj` (after Task 3): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet build OrderFlow.Tests/OrderFlow.Tests.csproj` + `dotnet test --filter OrderStatusServiceTests`: Passed! 21/21.
- `dotnet build OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj`: Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet build OrderFlow.sln` (all 8 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` (final): Passed! Failed: 0, Passed: 79, Skipped: 0, Total: 79 (75 prior + 4 new).
- `dotnet test OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj`: build succeeded but test host failed to launch on macOS (missing `Microsoft.WindowsDesktop.App` runtime) — same pre-existing, already-accepted **UNVERIFIED-ENVIRONMENT** gap as every prior UI story (2.5, 3.1, 3.2). The 2 new `TransitionToAsync` tests and the 2 updated `LoadAsync` tests compile and are structurally correct but have not executed on this dev machine.

### Completion Notes List

- `IOrderStatusService`/`OrderStatusService` (`OrderFlow.BLL`) gained `GetAllowedNextStatuses(OrderType, OrderStatus)` — a synchronous read accessor reusing the exact same `AllowedTransitions.TryGetValue` chain `TransitionTo` already uses. `AllowedTransitions` itself and `TransitionTo`'s logic are byte-for-byte unchanged.
- `IOrderDetailView` gained `DisplayAvailableTransitions(IReadOnlyList<OrderStatus>)`. `OrderDetailPresenter.LoadAsync` now also resolves `IOrderStatusService` from its existing scope (after a successful `IOrderService.GetAsync`) and calls it. Added `TransitionToAsync(int, OrderStatus)` returning `Task<bool>` — deliberately does **not** call `LoadAsync` internally; the reload-on-success is orchestrated by `OrderDetailForm` instead, keeping each Presenter method one scoped business operation (AD-5), mirroring `OrderListForm`/`ProductListForm`'s existing reload-after-child-operation pattern.
- `MockScopeHelper` (`OrderFlow.Presentation.Tests`) gained a two-service `CreateMockScope<TService1, TService2>()` overload to support `OrderDetailPresenterTests`' `LoadAsync` tests, which now resolve both `IOrderService` and `IOrderStatusService` from one mocked scope.
- `OrderDetailForm` (+ Designer, `OrderFlow.Presentation`) gained a `statusComboBox` + `transitionButton` row between the Status label and the line-items grid (existing controls shifted down 30px, `ClientSize` grew to `480x335`). `DisplayAvailableTransitions` populates/enables the combo and button; an empty list (terminal status) disables both with no separate message, matching this codebase's minimal-polish convention. `TransitionButton_Click` calls `TransitionToAsync`, then `LoadAsync` again only on success.
- `OrderStatusServiceTests.cs` gained 4 new tests for `GetAllowedNextStatuses` (Standard/Rush valid cases including the Rush-specific no-`Cancelled`-after-`Processing` restriction, a terminal status, and an unknown `OrderType`). `OrderDetailPresenterTests.cs`'s existing `LoadAsync` tests were updated for the two-service resolution and to assert `DisplayAvailableTransitions`; 2 new `TransitionToAsync` tests added (success, and a failure simulating AC #3's stale-UI rejection).
- `dotnet build` is green across all 8 projects with 0 warnings. `OrderFlow.Tests` is 79/79 passing. `OrderFlow.Presentation.Tests` builds clean but cannot execute on this macOS dev machine (missing Windows Desktop runtime).
- No `OrderFlow.Domain`/`OrderFlow.DAL`/`Program.cs` file touched — confirmed via File List below; every change extends an already-registered type from Story 2.4 or 3.2, no new DI registrations needed. No new files at all.

### File List

- `OrderFlow/OrderFlow.BLL/IOrderStatusService.cs` (modified: + `GetAllowedNextStatuses`)
- `OrderFlow/OrderFlow.BLL/OrderStatusService.cs` (modified: implement `GetAllowedNextStatuses`)
- `OrderFlow/OrderFlow.Presentation/IOrderDetailView.cs` (modified: + `DisplayAvailableTransitions`)
- `OrderFlow/OrderFlow.Presentation/OrderDetailPresenter.cs` (modified: `LoadAsync` also displays allowed transitions; + `TransitionToAsync`)
- `OrderFlow/OrderFlow.Presentation/OrderDetailForm.cs` (modified: + `TransitionButton_Click`, `DisplayAvailableTransitions`)
- `OrderFlow/OrderFlow.Presentation/OrderDetailForm.Designer.cs` (modified: + `statusComboBox`, + `transitionButton`, layout shifted)
- `OrderFlow/OrderFlow.Tests/OrderStatusServiceTests.cs` (modified: +4 tests)
- `OrderFlow/OrderFlow.Presentation.Tests/MockScopeHelper.cs` (modified: + two-service `CreateMockScope` overload)
- `OrderFlow/OrderFlow.Presentation.Tests/OrderDetailPresenterTests.cs` (modified: existing tests updated, +2 new tests)

## Change Log

- 2026-08-10: Implemented Story 3.3 — `IOrderStatusService.GetAllowedNextStatuses` added (reuses the existing transition table, AD-4's sole-owner rule preserved); `OrderDetailForm` extended with a status-transition ComboBox+button whose reload-on-success is Form-orchestrated (AD-5); `MockScopeHelper` gained a two-service overload. `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 79/79 passed (75 prior + 4 new).
- 2026-08-11: Code review (3 layers: Blind Hunter, Edge Case Hunter, Acceptance Auditor) — 5 patch findings applied: `OrderStatusService` now shares one `TryGetAllowedTransitions` helper between `TransitionTo`/`GetAllowedNextStatuses` and returns a defensive copy from the latter; `OrderDetailForm.TransitionButton_Click` now disables `statusComboBox` for the duration of a transition and no longer re-enables either control from stale pre-transition data when a post-success reload fails; added `LoadAsync_WithTerminalStatusOrder_DisplaysEmptyAllowedTransitions` to `OrderDetailPresenterTests`. 4 findings deferred as pre-existing/out-of-scope (see `deferred-work.md`); 5 dismissed as noise or already matching spec/convention. `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 82/82 passed. Status → `done`.
