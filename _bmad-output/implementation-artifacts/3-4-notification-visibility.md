---
baseline_commit: NO_VCS
---

# Story 3.4: Notification Visibility

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to see a visible confirmation whenever an Order's status changes,
so that I know the event fired.

## Acceptance Criteria

1. **Given** the in-app notification log (Epic 2's `INotifier`), **When** an `OrderStatusChangedNotification` is published, **Then** it becomes visible in the UI (a notification panel on `MainForm`) showing OrderId, OldStatus, NewStatus, and a timestamp.
2. **And Given** multiple status changes occur during a session, **When** I view the notification panel, **Then** all fired notifications for the session are listed in order — fulfilling FR-9.
3. **And Given** the notification panel, **When** reviewed, **Then** it is populated via the same `INotifier` singleton — no duplicate notification pathway, consistent with AD-4's single-caller rule.

## Tasks / Subtasks

- [x] Task 1: Add a live-subscription event to `INotifier`/`InAppNotifier` (AC: #1, #2, #3)
  - [x] **This is a new design decision this story must make** — `GetLog()` alone only returns a point-in-time snapshot; a `MainForm` that called it once at startup would never see notifications fired *after* the form loaded. AD-4's own text already anticipates the fix: *"INotifier is registered Singleton (UI-side subscribers must outlive any single Scoped operation)"* — and `Program.cs`'s `ConfigureServices` comment says outright: *"INotifier is registered Singleton — UI-side subscribers (Story 3.4) must outlive any single Scoped operation."* This story is that anticipated subscriber; it adds a plain .NET event, not a second log or a polling timer (which would violate AC #3's "no duplicate notification pathway").
  - [x] `OrderFlow.BLL/INotifier.cs`: add `event EventHandler<NotificationLogEntry>? Notified;` alongside the existing `Notify`/`GetLog` members.
  - [x] `OrderFlow.BLL/InAppNotifier.cs`: in `Notify`, build the `NotificationLogEntry` once, append it to `_log` **inside** the existing `lock (_lock)` block (unchanged thread-safety), then raise `Notified?.Invoke(this, entry)` **outside** the lock (after it exits) so a slow or reentrant subscriber can never block a concurrent `TransitionTo` call from acquiring `_lock`. `GetLog()` is untouched.
- [x] Task 2: Wire `MainForm` to `INotifier` (AC: #1, #2, #3)
  - [x] `MainForm(IServiceProvider serviceProvider, INotifier notifier)`: add the `INotifier` constructor parameter (resolves automatically — it's already `services.AddSingleton<INotifier, InAppNotifier>()` in `Program.cs` since Story 2.4; no DI registration changes needed). Store `_notifier`.
  - [x] **No Presenter is introduced for this.** `INotifier` is a Singleton cross-cutting service (like a logger), not a Scoped BLL business operation (AD-5) — `MainForm` passively rendering an already-published DTO's fields is not validation/pricing/workflow logic, so it doesn't fall under AD-3's "only the Presenter may call BLL services" rule the way a `CustomerService.Create` call would. This mirrors the Form-launching exception AD-3 already documents for `IServiceProvider` (Story 1.3) — a second, narrower exception for observing (not calling) a Singleton service.
  - [x] Seed + subscribe in the constructor, after `InitializeComponent()`: build the `BindingList<NotificationRow>` from `notifier.GetLog()` (already chronological — no re-sort needed) mapped via a private `MapToRow(NotificationLogEntry)` helper, assign it to `notificationDataGridView.DataSource`, then `_notifier.Notified += Notifier_Notified;`. Seeding from `GetLog()` at construction plus live updates via the same instance's `Notified` event is the one and only pathway into the panel — satisfying AC #3.
  - [x] `Notifier_Notified(object? sender, NotificationLogEntry entry)`: `InAppNotifier.Notify` can in principle be called from any thread (see its own thread-safety comment), so guard with `if (InvokeRequired) { BeginInvoke(() => Notifier_Notified(sender, entry)); return; }` before touching `_notifications`/any control. Then `_notifications.Add(MapToRow(entry));` — `BindingList<T>.Add` raises `ListChanged`, which `DataGridView` already listens for via its `DataSource` binding, so no manual refresh call is needed.
  - [x] `MapToRow(NotificationLogEntry entry)`: maps to `new NotificationRow { Timestamp = entry.OccurredAtUtc.ToLocalTime(), OrderId = entry.Notification.OrderId, OldStatus = entry.Notification.OldStatus, NewStatus = entry.Notification.NewStatus }` — `.ToLocalTime()` for display only, per the Architecture Spine's Consistency Conventions ("Dates: stored/passed as UTC DateTime, converted to local only at the Presentation layer for display").
  - [x] Override `protected override void OnFormClosed(FormClosedEventArgs e)`: `_notifier.Notified -= Notifier_Notified;` then `base.OnFormClosed(e);` — defensive unsubscribe from the Singleton (not strictly load-bearing since `Application.Run(mainForm)` exits when the root form closes, but cheap and avoids leaving a dangling subscription if that ever changes).
  - [x] Add a private nested `NotificationRow` class in `MainForm.cs` (no new file — matches Story 3.3's precedent of extending existing types) with plain `{ get; set; }` properties in this order: `Timestamp` (`DateTime`), `OrderId` (`int`), `OldStatus` (`OrderStatus`), `NewStatus` (`OrderStatus`) — property declaration order drives `DataGridView`'s `AutoGenerateColumns` column order.
- [x] Task 3: Extend `MainForm.Designer.cs` UI (AC: #1, #2)
  - [x] Add `notificationsLabel` (`Text = "Notifications"`, static `Label`, `Location = new Point(12, 54)`, `Size = new Size(200, 20)`) — placed below the existing button row (buttons occupy `y=12..42`), identifying the panel as the "visible confirmation" AC #1 asks for.
  - [x] Add `notificationDataGridView`: `Dock = DockStyle.Bottom`, `Height = 400`, `ReadOnly = true`, `AllowUserToAddRows = false`, `AllowUserToDeleteRows = false`, `AutoGenerateColumns = true`, `MultiSelect = false` — mirrors `OrderListForm.Designer.cs`'s established `DataGridView` conventions exactly (Story 3.2). No `SelectionChanged` handler needed — this panel is display-only, no button depends on its selection.
  - [x] Grow `MainForm.ClientSize` from `800x450` to `800x500` so the docked grid (bottom 400px) doesn't overlap the label/buttons (top 100px) — existing buttons keep their unchanged absolute `Location`s.
  - [x] `Controls.Add(notificationsLabel); Controls.Add(notificationDataGridView);` alongside the existing four `Controls.Add(...)` calls. No changes to any existing button or its click handler.
- [x] Task 4: `OrderFlow.Tests` — extend `InAppNotifierTests.cs` (AC: #1, #2, #3)
  - [x] `Notify_RaisesNotifiedEventWithTheAppendedEntry`: subscribe, call `Notify`, assert the raised `NotificationLogEntry` is `Assert.Same` as `notifier.GetLog()[0]` (same instance, not a copy) and wraps the same `notification`.
  - [x] `Notify_CalledMultipleTimes_RaisesNotifiedEventEachTimeInOrder`: subscribe once, call `Notify` twice, assert the event fired twice with each entry's `Notification` `Assert.Same` as the corresponding call, in call order.
  - [x] `Notify_WithNoSubscribers_StillAppendsToLog`: call `Notify` with zero subscribers (the `Notified?.Invoke(...)` null-conditional path); assert no exception (`Record.Exception` is `null`) and `GetLog()` still gained the entry — guards against a null-delegate crash regressing `Notify`'s core append behavior.
- [x] Task 5: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 8 projects) — 0 errors, 0 warnings.
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` passes, including the 3 new tests, and every pre-existing test (79 as of Story 3.3) still passes.
  - [x] Confirm `OrderFlow.Domain`/`OrderFlow.DAL`/`Program.cs` are untouched — `INotifier` is already registered Singleton (Story 2.4); `MainForm`'s new constructor parameter resolves automatically from that existing registration, no new DI line needed. Confirm via File List below.
  - [x] `OrderFlow.Presentation.Tests` is not touched — `MainForm` has no Presenter (same as it's always had; only its constructor and `OnFormClosed` change), matching this codebase's established pattern that only Presenters get `Presentation.Tests` coverage, not Forms directly.

### Review Findings

- [x] [Review][Decision→Patch] `Notified` could fire out of `_log` append order under concurrent `Notify()` calls, breaking AC #2's ordering guarantee — **Resolved: user chose Option A.** `Notified?.Invoke` moved inside `lock (_lock)` so append-and-notify are atomic together, guaranteeing event order matches log order; trades away the "never block a concurrent writer" property Task 1 originally optimized for, accepted since `MainForm`'s only subscriber stays cheap (immediate `BeginInvoke` marshal, no inline work) [OrderFlow.BLL/InAppNotifier.cs:14-28]
- [x] [Review][Patch] `Notified?.Invoke(this, entry)` is unguarded — a subscriber's exception would propagate back into `OrderStatusService.TransitionTo`, making an already-committed operation appear to fail [OrderFlow.BLL/InAppNotifier.cs:25]
- [x] [Review][Patch] `MainForm`'s direct `INotifier` constructor-injection is a second exception to AD-3's "only the Presenter may call BLL services" rule, but unlike the Story 1.3 Form-launching exception, it isn't recorded in `ARCHITECTURE-SPINE.md`'s AD-3 rule text itself [_bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md AD-3; OrderFlow.Presentation/MainForm.cs:14-18]
- [x] [Review][Defer] `MainForm`'s `GetLog()` snapshot and `Notified +=` subscription aren't atomic, which could in principle drop or duplicate a notification — verified unreachable given the app's actual call graph today (`Notify()` requires an already-shown Form to trigger a transition, which can't happen before `MainForm`'s constructor completes); latent hazard only if a future story adds a `Notify()` path independent of UI action [OrderFlow.Presentation/MainForm.cs:29-33] — deferred (unreachable today)
- [x] [Review][Defer] `InAppNotifier._log`/`MainForm._notifications` have no cap or eviction — unbounded growth over a long session, plus `GetLog()`'s O(n) lock-held copy compounds as the log grows [OrderFlow.BLL/InAppNotifier.cs] — deferred (demo-scale, not required by any AC)
- [x] [Review][Defer] `InvokeRequired` can return a false negative before a Form's window handle exists — same underlying invariant as the snapshot/subscribe race above: unreachable today since `Notify()` can't fire before `MainForm`'s handle is created [OrderFlow.Presentation/MainForm.cs:64-68] — deferred (unreachable today)
- [x] [Review][Defer] A `Notify()` call already in-flight when `MainForm` closes could invoke `Notifier_Notified` after/concurrently with `OnFormClosed`'s unsubscribe — narrow race requiring an in-flight transition on an owned form at the exact moment the owner closes; same systemic no-disposal-guard pattern already deferred from Story 3.3's review [OrderFlow.Presentation/MainForm.cs:81-85] — deferred (pre-existing systemic pattern)
- [x] [Review][Defer] New notifications append to the bottom of the grid with no auto-scroll or newest-first ordering [OrderFlow.Presentation/MainForm.cs] — deferred (UX polish, not required by any AC)

## Dev Notes

- **The core design decision: an event on `INotifier`, not a poll.** `Program.cs`'s `ConfigureServices` and Architecture's AD-4 both already flag that `INotifier`'s Singleton lifetime exists specifically so a "UI-side subscriber" can attach and outlive any one Scoped operation — this story is that subscriber. Adding a polling `Timer` on `MainForm` instead would technically satisfy AC #1/#2 but would create a second read pathway alongside the constructor-time `GetLog()` seed, which risks exactly the "duplicate notification pathway" AC #3 forbids (a poll could double-count or race the event). The event is the only mechanism this story introduces.
- **Thread-safety mirrors `InAppNotifier`'s own existing caution.** Its class comment already calls out that the Singleton is "shared across every concurrently-running business operation" and lock-guards the list. `Notify` raising `Notified` *outside* the lock (Task 1) means a subscriber's work (here, a WinForms `BeginInvoke` marshal) never happens while `_lock` is held — so a slow UI marshal can't stall a concurrent `OrderStatusService.TransitionTo` call elsewhere in the app.
- **No Presenter for `MainForm`, and this is a deliberate, narrow exception — not a drift from AD-3.** Every other Form's business logic goes through a Presenter calling Scoped BLL services from a per-operation `IServiceScope` (AD-3/AD-5). `INotifier` is one of exactly two Singletons in the entire app (the other is the not-yet-built `IAppSettings`, per AD-5) and this story only *observes* it to render already-computed fields — it never calls a BLL method that performs validation, pricing, or workflow logic. `MainForm` already has one documented AD-3 exception (holding root `IServiceProvider` for Form-launching, added Story 1.3); this is a second, similarly narrow one, for the same reason: a cross-cutting Singleton concern that doesn't fit the per-operation Presenter shape at all.
- **UTC→local conversion happens only at the display boundary**, per the Architecture Spine's Consistency Conventions table — `NotificationLogEntry.OccurredAtUtc` itself is never mutated; `MapToRow` converts only when building the row `DataGridView` renders.
- **`DataGridView` + `AutoGenerateColumns = true`, no explicit column headers** — matches every other list Form in this codebase (`CustomerListForm`, `ProductListForm`, `OrderListForm`); already-accepted UX-polish gap per `deferred-work.md`, not required by any AC here either. Property names (`Timestamp`, `OrderId`, `OldStatus`, `NewStatus`) were chosen to read cleanly as auto-generated headers without needing explicit `HeaderText` configuration.
- **`BindingList<T>`, not a plain `List<T>` + manual `DataSource` reset.** `BindingList<T>.Add` raises `ListChanged`, which the grid's data-binding already listens for — this is what makes live updates from `Notifier_Notified` show up without any manual grid-refresh call, and is the idiomatic WinForms mechanism for exactly this "append and the grid updates itself" scenario.
- **No new files.** `INotifier`/`InAppNotifier` (Story 2.4) and `MainForm`/`MainForm.Designer.cs` (Story 1.1) are all existing types this story extends. `NotificationRow` is a private nested class inside `MainForm.cs`, not a new file — consistent with Story 3.3's "no new files" precedent for a change that's entirely additive to already-existing types.
- **`OccurredAtUtc` ordering**: `InAppNotifierTests` already proves (`Notify_CalledMultipleTimes_AppendsEntriesInOrder`) that `_log` is append-ordered, i.e., chronological — `MapToRow`'s seed-time `GetLog()` iteration needs no separate sort for AC #2's "listed in order."

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.BLL/
    INotifier.cs                       # modified: + Notified event
    InAppNotifier.cs                   # modified: Notify raises Notified after the lock releases
  OrderFlow.Presentation/
    MainForm.cs                        # modified: + INotifier ctor param, seed+subscribe, Notifier_Notified, MapToRow, OnFormClosed, NotificationRow
    MainForm.Designer.cs               # modified: + notificationsLabel, + notificationDataGridView, ClientSize grown to 800x500
  OrderFlow.Tests/
    InAppNotifierTests.cs              # modified: +3 tests
```

`OrderFlow.Domain`/`OrderFlow.DAL`/`Program.cs`/`OrderFlow.Presentation.Tests` are untouched by this story — no new files at all, no new DI registrations.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.4: Notification Visibility] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-4 — OrderStatus transitions + notifications are BLL-orchestrated] — "INotifier is registered Singleton (UI-side subscribers must outlive any single Scoped operation)" — the architectural basis for this story's event-subscriber design
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-3 — Presentation: constructor-injected Presenter + per-screen IView] — the Form-launching exception precedent (Story 1.3) this story's second, narrower exception mirrors
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Consistency Conventions] — UTC-stored/local-at-display date convention `MapToRow` follows
- [Source: OrderFlow/OrderFlow.Presentation/Program.cs] — `ConfigureServices` comment naming this exact story ("UI-side subscribers (Story 3.4) must outlive any single Scoped operation") as the reason `INotifier` is Singleton
- [Source: OrderFlow/OrderFlow.BLL/InAppNotifier.cs] — existing `Notify`/`GetLog`/lock-guarded `_log` this story extends with the `Notified` event
- [Source: OrderFlow/OrderFlow.BLL/NotificationLogEntry.cs] — existing `{ Notification, OccurredAtUtc }` shape the `Notified` event payload reuses as-is
- [Source: OrderFlow/OrderFlow.Presentation/OrderListForm.Designer.cs] — the `DataGridView` property convention (`ReadOnly`, `AllowUserToAddRows = false`, `AutoGenerateColumns = true`, etc.) `notificationDataGridView` mirrors
- [Source: _bmad-output/implementation-artifacts/3-3-order-status-transition-ui.md] — precedent for "no new files, extend existing types" and for calling out a story's central design decision explicitly in Dev Notes
- [Source: _bmad-output/implementation-artifacts/deferred-work.md] — the already-accepted "no explicit column headers" UX-polish gap this story's `notificationDataGridView` follows rather than reopens

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.Tests/OrderFlow.Tests.csproj` (after Task 4's tests added, before Task 1's `Notified` event existed): failed as expected — `InAppNotifier` did not yet contain `Notified` (CS1061 x2). Resolved by Task 1.
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj --filter InAppNotifierTests` (after Task 1): Passed! 7/7 (4 prior + 3 new).
- `dotnet build OrderFlow.sln` (all 8 projects, after Tasks 2/3): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` (final): Passed! Failed: 0, Passed: 82, Skipped: 0, Total: 82 (79 prior + 3 new).
- `OrderFlow.Presentation.Tests` was not modified and was not run — same pre-existing, already-accepted **UNVERIFIED-ENVIRONMENT** gap as every prior UI story (2.5, 3.1, 3.2, 3.3): its test host cannot launch on this macOS dev machine (missing `Microsoft.WindowsDesktop.App` runtime). It did build cleanly as part of the full-solution `dotnet build` above.

### Completion Notes List

- `INotifier`/`InAppNotifier` (`OrderFlow.BLL`) gained a `Notified` event — the live-subscription counterpart to the existing `GetLog()` snapshot. `InAppNotifier.Notify` now builds the `NotificationLogEntry` once, appends it to `_log` inside the existing `lock`, then raises `Notified` after the lock releases so a slow/reentrant subscriber can never block a concurrent `TransitionTo` call. `GetLog()`'s behavior and the pre-existing lock-guarding are otherwise unchanged.
- `MainForm` (+ Designer, `OrderFlow.Presentation`) now constructor-injects `INotifier` directly — a second, narrow AD-3 exception alongside its existing `IServiceProvider` one (Story 1.3), justified because `INotifier` is a Singleton cross-cutting service being passively observed, not a Scoped BLL business operation. It seeds a `BindingList<NotificationRow>` from `notifier.GetLog()` at construction, binds it to a new `notificationDataGridView` (`Dock = Bottom`, mirrors `OrderListForm`'s `DataGridView` conventions), and subscribes `Notifier_Notified` to `INotifier.Notified` for live updates thereafter — the seed + the event are the only two pathways into the panel, satisfying AC #3's "no duplicate notification pathway." `Notifier_Notified` marshals to the UI thread via `InvokeRequired`/`BeginInvoke` before touching the `BindingList`. `OnFormClosed` unsubscribes defensively. `NotificationRow` is a private nested class (no new file) mapping `OccurredAtUtc.ToLocalTime()` + the notification's `OrderId`/`OldStatus`/`NewStatus` fields, per the Architecture Spine's UTC-stored/local-at-display convention.
- `MainForm.ClientSize` grew from `800x450` to `800x500`; a `notificationsLabel` ("Notifications") and the docked `notificationDataGridView` (height 400) were added below the unchanged, absolutely-positioned button row. No existing button or click handler changed.
- `InAppNotifierTests.cs` gained 3 new tests for the `Notified` event (fires with the same entry instance appended to the log, fires once per `Notify` call in order across multiple calls, and doesn't throw with zero subscribers while still appending to the log).
- `dotnet build` is green across all 8 projects with 0 warnings. `OrderFlow.Tests` is 82/82 passing. `OrderFlow.Presentation.Tests` builds clean but cannot execute on this macOS dev machine (missing Windows Desktop runtime) — consistent with every prior UI story.
- No `OrderFlow.Domain`/`OrderFlow.DAL`/`Program.cs`/`OrderFlow.Presentation.Tests` file touched — confirmed via File List below; `INotifier`'s existing Singleton DI registration (Story 2.4) already covers `MainForm`'s new constructor parameter, no new DI line needed. No new files at all.

### File List

- `OrderFlow/OrderFlow.BLL/INotifier.cs` (modified: + `Notified` event)
- `OrderFlow/OrderFlow.BLL/InAppNotifier.cs` (modified: `Notify` raises `Notified` after the lock releases)
- `OrderFlow/OrderFlow.Presentation/MainForm.cs` (modified: + `INotifier` ctor param, seed+subscribe, `Notifier_Notified`, `MapToRow`, `OnFormClosed`, `NotificationRow`)
- `OrderFlow/OrderFlow.Presentation/MainForm.Designer.cs` (modified: + `notificationsLabel`, + `notificationDataGridView`, `ClientSize` grown to 800x500)
- `OrderFlow/OrderFlow.Tests/InAppNotifierTests.cs` (modified: +3 tests)

## Change Log

- 2026-08-11: Implemented Story 3.4 — `INotifier`/`InAppNotifier` gained a `Notified` event (live-subscription counterpart to `GetLog()`); `MainForm` extended with a notification `DataGridView` seeded from `GetLog()` and kept live via the event, with no Presenter (a narrow, documented AD-3 exception for observing the Singleton `INotifier`). `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 82/82 passed (79 prior + 3 new).
- 2026-08-11: Code review (3 layers) — 1 decision-needed finding resolved (user chose to move `Notified?.Invoke` inside `InAppNotifier.Notify`'s lock, guaranteeing event order matches log-append order for AC #2, trading away the original non-blocking-writer design in favor of correctness); 2 patch findings applied (`Notified?.Invoke` now wrapped in try/catch so a subscriber's fault can't fail an already-committed `TransitionTo`; `ARCHITECTURE-SPINE.md`'s AD-3 rule text updated with a second documented exception for `MainForm`'s `INotifier` observation). 5 findings deferred as unreachable-given-current-call-graph or UX polish (see `deferred-work.md`); 4 dismissed as noise or matching established convention. `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 82/82 passed. Status → `done`.
