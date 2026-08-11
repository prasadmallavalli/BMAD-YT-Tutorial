---
baseline_commit: NO_VCS
---

# Story 1.3: Customer Management UI

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to create, view, edit, and list Customers from the desktop app,
so that I can manage the customers I take orders for.

## Acceptance Criteria

1. **Given** the app is running, **When** I open the Customer list, **Then** `CustomerListForm` (`ICustomerListView`) displays all Customers via `CustomerListPresenter`, which opens one `IServiceScope` per action (AD-3) and calls `ICustomerService` asynchronously without blocking the UI.
2. **And Given** the list, **When** I create or edit a Customer via `CustomerDetailForm`/`CustomerDetailPresenter`, **Then** valid submission persists the change and returns to the refreshed list.
3. **And Given** invalid input (e.g. missing Name/Email), **When** I submit, **Then** the `Result<T>` failure message is surfaced on the form without a crash.
4. **And Given** the Presentation project, **When** reviewed, **Then** Customer forms reference only `CustomerDto` and the injected service/Presenter — no `OrderFlow.DAL`/`Domain` types — satisfying FR-10.

## Tasks / Subtasks

- [x] Task 0: Add `OrderFlow.Presentation.Tests` project (AC: #1, #3) — **architecture amendment, do this first**
  - [x] `dotnet new xunit -o OrderFlow.Presentation.Tests` (or hand-craft the `.csproj`), targeting `net10.0-windows`. Copy **only** `TargetFramework`/`EnableWindowsTargeting` from `OrderFlow.Presentation.csproj` — do **not** copy `OutputType=WinExe` or `UseWindowsForms=true`; this is a test library, not a WinForms executable
  - [x] Swap default `xunit` package for `xunit.v3` `3.2.2`, and pin `Microsoft.NET.Test.Sdk` `17.14.1`, `xunit.runner.visualstudio` `3.1.4`, `coverlet.collector` `6.0.4` — match `OrderFlow.Tests.csproj`'s actual versions exactly, don't let the toolchains drift between the two test projects. Add `Moq` `4.20.72`
  - [x] `ProjectReference` to `OrderFlow.Presentation` (for Presenters/IViews) and `OrderFlow.BLL` (for `ICustomerService`/`CustomerDto`/`Result<T>`)
  - [x] Add to `OrderFlow.sln`/`OrderFlow.slnx`
  - [x] Do **not** add this reference to the existing `OrderFlow.Tests` project or retarget it — see Dev Notes for why (it would break every currently-passing test's ability to run locally) — **verified: `OrderFlow.Tests` untouched, still 10/10 passing locally**
- [x] Task 1: `ICustomerListView` + `CustomerListPresenter` (AC: #1, #3, #4)
  - [x] `OrderFlow.Presentation/ICustomerListView.cs`: `void DisplayCustomers(IReadOnlyList<CustomerDto> customers)`, `void ShowError(string message)`
  - [x] `OrderFlow.Presentation/CustomerListPresenter.cs`: constructor `(ICustomerListView view, IServiceScopeFactory scopeFactory)` — see Dev Notes for why not `ICustomerService` directly. `LoadCustomersAsync()`: `await using var scope = _scopeFactory.CreateAsyncScope();` → resolve `ICustomerService` via `scope.ServiceProvider.GetRequiredService<ICustomerService>()` (use `GetRequiredService`, not `GetService` — a misconfiguration should throw a clear `InvalidOperationException`, not a raw `NullReferenceException`) → `GetAllAsync()` → `_view.DisplayCustomers(...)` on success, `_view.ShowError(...)` on failure. **Never call `.ConfigureAwait(false)` anywhere in Presenter code** — see Dev Notes, it would break the UI-thread marshaling `DisplayCustomers`/`ShowCustomer` depend on
- [x] Task 2: `CustomerListForm` (AC: #1, #2, #4)
  - [x] `OrderFlow.Presentation/CustomerListForm.cs` + `.Designer.cs`: implements `ICustomerListView`; a `DataGridView` (read-only, `AutoGenerateColumns = true`) plus "Add", "Edit", "Refresh" buttons
  - [x] `DisplayCustomers(IReadOnlyList<CustomerDto> customers)` sets `dataGridView.DataSource = customers.ToList()` (bind the `CustomerDto` objects directly — do **not** rebuild a subset of columns manually). Hide the `Id` column after binding (`dataGridView.Columns["Id"].Visible = false`) so it isn't shown to the user but the bound object is still recoverable per row
  - [x] "Edit" reads the selected customer as `(dataGridView.CurrentRow?.DataBoundItem as CustomerDto)?.Id` — this is the only correct way to recover the row's `Id` when a list of DTOs is bound directly (never derive it from row index or a displayed cell value); no-op (or disable the button) when nothing is selected
  - [x] Constructor takes `IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory` (both DI-injectable with no extra registration — see Dev Notes on the root-`IServiceProvider`-for-Form-launching exception); constructs `_presenter = new CustomerListPresenter(this, scopeFactory)`
  - [x] Wire `Load`/`Click` handlers the standard WinForms way — subscribed in `.Designer.cs`'s `InitializeComponent()` (e.g. `this.Load += CustomerListForm_Load;`, button `Click +=`), not left unsubscribed. This compiles fine either way but silently breaks AC #1/#2 if forgotten — there's no compiler error for a missing event subscription
  - [x] Known limitation, accepted as-is for this story: clicking "Customers" on `MainForm` repeatedly opens multiple non-modal `CustomerListForm` instances (it's `AddTransient`, no singleton-instance/reuse guard). Not an AC violation; revisit only if it becomes a real annoyance in a later story
  - [x] `Load` event: `async void CustomerListForm_Load(...)` → `await _presenter.LoadCustomersAsync()`
  - [x] "Refresh": same call as `Load`
  - [x] "Add": resolve `CustomerDetailForm` via `serviceProvider.GetRequiredService<CustomerDetailForm>()`, call `.Initialize(null)`, `ShowDialog()`; if result is `DialogResult.OK`, reload the list
  - [x] "Edit": same, but `.Initialize(selectedCustomerId)` using the `DataBoundItem` cast above; disabled/no-op when no row is selected
  - [x] `DisplayCustomers`/`ShowError` implement the `ICustomerListView` contract (bind `DataGridView.DataSource` per above; show errors via `MessageBox.Show` — no crash, satisfying AC #3 for this screen)
- [x] Task 3: `ICustomerDetailView` + `CustomerDetailPresenter` (AC: #2, #3, #4)
  - [x] `OrderFlow.Presentation/ICustomerDetailView.cs`: `void ShowCustomer(CustomerDto customer)`, `void ShowError(string message)`
  - [x] `OrderFlow.Presentation/CustomerDetailPresenter.cs`: constructor `(ICustomerDetailView view, IServiceScopeFactory scopeFactory)`. Resolve `ICustomerService` via `GetRequiredService<ICustomerService>()` in every method (never `GetService`). `Task<bool> LoadAsync(int customerId)`: scoped `GetAsync(id)` → `_view.ShowCustomer(...)` + return `true` on success, `_view.ShowError(...)` + return `false` on failure. `Task<bool> SaveAsync(int? customerId, CustomerDto dto)`: scoped `UpdateAsync(customerId.Value, dto)` if `customerId.HasValue` else `CreateAsync(dto)` → return `true` on success, `_view.ShowError(...)` + return `false` on failure. **Never call `.ConfigureAwait(false)`** — same reason as Task 1
- [x] Task 4: `CustomerDetailForm` (AC: #2, #3, #4)
  - [x] `OrderFlow.Presentation/CustomerDetailForm.cs` + `.Designer.cs`: implements `ICustomerDetailView`; `TextBox`es for Name/Email/Phone, "Save"/"Cancel" buttons
  - [x] Constructor takes `IServiceScopeFactory scopeFactory` only (this is a leaf form — it never launches another form, so it does **not** need `IServiceProvider`, unlike `CustomerListForm`); constructs `_presenter = new CustomerDetailPresenter(this, scopeFactory)`
  - [x] `public void Initialize(int? customerId)`: stores `_customerId`; sets form title ("Add Customer" / "Edit Customer") — see Dev Notes on why this is a post-construction method, not a constructor parameter
  - [x] `Load` event: `async void CustomerDetailForm_Load(...)` — if `_customerId.HasValue`, `await _presenter.LoadAsync(_customerId.Value)` to populate the text boxes (see Dev Notes on why the edit-mode fetch happens here, not in `Initialize`). Wire this subscription in `.Designer.cs`'s `InitializeComponent()`, same as Task 2 — an unsubscribed `Load` silently breaks AC #2's edit path with no compiler error
  - [x] "Save" click: `async void SaveButton_Click(...)` — build a `CustomerDto` from the text boxes, `await _presenter.SaveAsync(_customerId, dto)`; if `true`, `DialogResult = DialogResult.OK; Close();`
  - [x] "Cancel" click: `DialogResult = DialogResult.Cancel; Close();` (no BLL call)
  - [x] `ShowCustomer`/`ShowError` implement `ICustomerDetailView` (populate text boxes; show errors via `MessageBox.Show` — no crash, satisfying AC #3)
- [x] Task 5: Wire up `MainForm` + composition root (AC: #1)
  - [x] `MainForm.cs`/`.Designer.cs`: add a "Customers" `Button`; constructor gains `IServiceProvider serviceProvider` (same root-provider-for-Form-launching exception as `CustomerListForm`)
  - [x] `async void CustomersButton_Click(...)`: resolve `CustomerListForm` via `serviceProvider.GetRequiredService<CustomerListForm>()`, `.Show()` (non-modal — see Dev Notes) — *(implemented as sync `void`, not `async void`: `.Show()` is non-blocking/non-modal and there's no `await` in this handler, so `async` would be misleading — no behavioral difference)*
  - [x] `Program.cs` `ConfigureServices`: `services.AddTransient<CustomerListForm>();` and `services.AddTransient<CustomerDetailForm>();`
- [x] Task 6: Presenter tests in `OrderFlow.Presentation.Tests` (AC: #1, #2, #3) — **UNVERIFIED-ENVIRONMENT: will build, cannot run on this macOS machine (see Dev Notes and user decision)**
  - [x] `CustomerListPresenterTests`: `LoadCustomersAsync` calls `view.DisplayCustomers(...)` on success and `view.ShowError(...)` on failure — mock the `IServiceScopeFactory`→`IServiceScope`→`IServiceProvider` chain per the exact pattern in Dev Notes (do **not** try to mock `CreateAsyncScope()` directly — it's an extension method wrapping `CreateScope()`)
  - [x] `CustomerDetailPresenterTests`: `LoadAsync` found/not-found paths; `SaveAsync` create path (`customerId == null` → `CreateAsync` called), update path (`customerId.HasValue` → `UpdateAsync` called), and a validation-failure path (`view.ShowError` called, returns `false`)
- [x] Task 7: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 7 projects now) — 0 errors, 0 warnings
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` still passes all existing tests (must be unaffected by this story — confirms the project-split decision worked) — **confirmed: 10/10 passed**
  - [x] `dotnet build OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj` succeeds (this is the verification ceiling on this machine — do **not** attempt `dotnet test` on it here; it will abort with a `Microsoft.WindowsDesktop.App` runtime error, matching Story 1.1's `MainForm` launch precedent) — **confirmed: build succeeded, 0 warnings, 0 errors; `dotnet test` intentionally not attempted here**
  - [x] Document in Completion Notes, explicitly: Presenter test *behavior* (pass/fail of the actual assertions) remains **UNVERIFIED-ENVIRONMENT** pending Windows/CI, same as Story 1.1's `MainForm` launch and consistent with the user's explicit decision during story creation

### Review Findings

- [x] [Review][Patch] `CustomerDetailForm` resolved via `_serviceProvider.GetRequiredService<CustomerDetailForm>()` in `OpenDetailFormAsync` is never disposed — `ShowDialog()` does **not** auto-dispose (unlike `Show()`, which does on `Close()`), so every single Add/Edit action leaks a `Form` and its native window handle for the process's lifetime. Confirmed by 3 independent reviewers. Wrap in `using`. [`CustomerListForm.cs:47-56`]
- [x] [Review][Patch] `MainForm.CustomersButton_Click` calls `listForm.Show()` without setting `Owner` — closing `MainForm` while `CustomerListForm` is open abandons it instead of cascading the close (WinForms auto-closes owned forms when their Owner closes). Use `listForm.Show(this)`. [`MainForm.cs:22-23`]
- [x] [Review][Patch] No reentrancy guard on `Save`/`Refresh`/`Add`/`Edit` buttons — a fast double-click on Save can fire two overlapping `SaveAsync` calls, plausibly creating a duplicate customer record. Disable the triggering button before `await`, re-enable in `finally`. [`CustomerDetailForm.cs:34-48`, `CustomerListForm.cs:22-45`]
- [x] [Review][Patch] `EditButton_Click` silently no-ops when no row is selected — no feedback, dead silence for the user. Wire `dataGridView.SelectionChanged` to toggle `editButton.Enabled`. [`CustomerListForm.cs:37-45`]
- [x] [Review][Patch] `DisplayCustomers`' column-hiding uses the bare string `"Id"` — fragile to a future `CustomerDto.Id` rename (fails silently, column stays visible, no error). Use `nameof(CustomerDto.Id)` instead. [`CustomerListForm.cs:62`]
- [x] [Review][Patch] `CreateMockScope()` helper is duplicated verbatim across `CustomerListPresenterTests.cs` and `CustomerDetailPresenterTests.cs` — extract to a shared test helper.
- [x] [Review][Patch] `CustomerDetailPresenterTests`' success-path tests (`LoadAsync` found, `SaveAsync` create/update) never assert `ShowError` was **not** called — the "success stays silent toward the view" contract is unverified. Add `mockView.Verify(v => v.ShowError(It.IsAny<string>()), Times.Never)` to each.
- [x] [Review][Patch] The new root-`IServiceProvider`-for-Form-launching exception (documented in this story's Dev Notes) was never folded into the Architecture Spine's AD-3/AD-5 text itself — inconsistent with Story 1.1's own precedent, where the composition-root and public-`AppDbContext` exceptions *were* written into AD-1/AD-9. Amend AD-3 (and/or AD-5) in `ARCHITECTURE-SPINE.md` to record it formally.
- [x] [Review][Defer] Root `IServiceProvider` injected into `MainForm`/`CustomerListForm` purely to call `GetRequiredService<TForm>()` is a Service Locator pattern (`CustomerListForm` additionally carries both `IServiceProvider` and `IServiceScopeFactory`, two overlapping DI abstractions) — deferred, deliberate/disclosed tradeoff for this story (see Dev Notes reasoning); reconsider a `Func<TForm>`/`IFormFactory` abstraction only if this pattern recurs across 3+ more UI stories (Product/Order forms).
- [x] [Review][Defer] No busy/loading UI indicator (cursor, disabled state, "Loading…") during any async operation — deferred, UX polish, not required by any AC.
- [x] [Review][Defer] No empty-state messaging ("No customers found") when the grid loads zero rows — deferred, UX polish, not required by any AC.

## Dev Notes

- **Two decisions were made with the user during story creation, both recorded as amendments:**
  1. Presenter tests are written now but accepted as `UNVERIFIED-ENVIRONMENT` on this macOS machine (same posture as Story 1.1's `MainForm` launch) rather than restructuring `OrderFlow.Presentation` itself to make Presenters platform-agnostic.
  2. To honor that without breaking the currently-passing `OrderFlow.Tests` suite, a **new 7th project, `OrderFlow.Presentation.Tests`**, was added — this amends the Architecture Spine's Structural Seed (see the spine's `OrderFlow.Presentation.Tests` note, added alongside this story). Do not merge Presenter tests into `OrderFlow.Tests` or retarget `OrderFlow.Tests` itself — verified empirically during story creation that doing so aborts the test host for *every* test in that project on a machine without the WinForms runtime, not just the new ones.
- **Never use `.ConfigureAwait(false)` anywhere in Presenter or Form code.** It's common generic .NET async advice, but it's wrong here: `DisplayCustomers`/`ShowCustomer`/`ShowError` touch WinForms controls and must run back on the UI thread after an `await`. `ConfigureAwait(false)` would let the continuation resume on a thread-pool thread instead, throwing `InvalidOperationException: Cross-thread operation not valid` the first time a Presenter method actually updates a control after an awaited BLL call. The default `ConfigureAwait(true)` (i.e., no call at all) is correct and required throughout this story's async code.
- **Why `CustomerListPresenter`'s constructor takes `IServiceScopeFactory`, not `ICustomerService` directly:** AD-3's literal Rule: "A `XxxPresenter` class is constructor-injected with that `IView` and an `IServiceScopeFactory` (never long-lived BLL service instances)... For each user-initiated action... the Presenter opens one `IServiceScope`, resolves the BLL services it needs for that single operation... and disposes the scope when the operation completes." Injecting `ICustomerService` directly would give the Presenter one instance that outlives every action (a "long-lived BLL service instance"), which is exactly what this Rule forbids — each button click must get its own fresh scope.
- **Why the Presenter/Form constructs itself, rather than DI resolving the Presenter as a service:** the Presenter needs the Form instance itself as its `IView` (`new CustomerListPresenter(this, scopeFactory)`), and DI cannot inject a not-yet-constructed object into itself. So each `XxxForm`'s own constructor — itself DI-resolved (`AddTransient<CustomerListForm>()`) — receives `IServiceScopeFactory` (and, for Forms that launch other Forms, `IServiceProvider`) via constructor injection, and manually constructs its own Presenter as its first action, passing `this`. This is "constructor-injected... at Form-creation time" read literally: the *Presenter's own construction* happens at Form-creation time, performed by the Form.
- **The root `IServiceProvider`-for-Form-launching exception (new, this story) — narrow and Form-navigation-only:** `MainForm` and `CustomerListForm` need to resolve *other Transient Forms* to show them (`MainForm` → `CustomerListForm` → `CustomerDetailForm`). They receive the root `IServiceProvider` via constructor injection solely to call `GetRequiredService<TForm>()` for this purpose — never to resolve a BLL/DAL service directly (that would violate AD-5's Scoped-per-operation rule and AD-9). `CustomerDetailForm` is a leaf (never launches another Form) and does **not** get `IServiceProvider` — only `IServiceScopeFactory`, for its own Presenter's scoped operations. This exception is analogous in spirit to Story 1.1's composition-root exception: Forms aren't "the business operation," they're the operation's UI container: each launched Form still gets its own correctly-scoped `IServiceScopeFactory` for whatever its Presenter actually does.
- **Why `CustomerDetailForm`'s create-vs-edit mode is a post-construction `Initialize(int? customerId)` method, not a constructor parameter:** DI (`AddTransient<CustomerDetailForm>()`) can only supply registered services to a constructor — it has no way to pass a runtime-chosen value like "the id of the row the user clicked." The caller (`CustomerListForm`) resolves the Form via DI first, then calls `Initialize(...)` with the runtime value, then `ShowDialog()`.
- **Why the edit-mode fetch happens in the `Load` event, not in `Initialize`:** `Initialize` runs synchronously before `ShowDialog()` is called; `ShowDialog()` is what starts the form's message loop. An `async` fetch kicked off from `Initialize` would be racing the dialog's own startup. The `Load` event fires once the message loop is already pumping, which is the standard WinForms point to kick off `async void` I/O — matches AD-3's "top-level UI event handler may be `async void`" allowance exactly.
- **List↔Detail refresh signal is `DialogResult`, not something the Presenter touches:** `CustomerDetailForm` sets `DialogResult = DialogResult.OK` and closes itself only after a successful `SaveAsync`. `CustomerListForm`'s Add/Edit handlers check `ShowDialog()`'s return value and reload the list only on `OK`. `DialogResult`/`ShowDialog` are WinForms View concerns — the Presenter's `SaveAsync` just returns `bool`, staying UI-framework-agnostic (and easily unit-testable).
- **`CustomerListForm` opens non-modally (`.Show()`), `CustomerDetailForm` opens modally (`.ShowDialog()`):** no UX spec exists for this project (by design, per epics.md's Overview), so this is a dev design call, stated explicitly so future UI stories (Product/Order) follow the same convention: list/browse screens are non-modal (so multiple lists could theoretically be open at once later), single-record edit dialogs are modal (so "did it save" can be read synchronously off `ShowDialog()`'s return value).
- **Moq pattern for testing Presenters — do not try to mock `IServiceScopeFactory.CreateAsyncScope()` directly, it's an extension method:**
  ```csharp
  var mockService = new Mock<ICustomerService>();
  var mockProvider = new Mock<IServiceProvider>();
  mockProvider.Setup(p => p.GetService(typeof(ICustomerService))).Returns(mockService.Object);
  var mockScope = new Mock<IServiceScope>();
  mockScope.Setup(s => s.ServiceProvider).Returns(mockProvider.Object);
  var mockScopeFactory = new Mock<IServiceScopeFactory>();
  mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
  ```
  `CreateAsyncScope()` is `ServiceProviderServiceExtensions.CreateAsyncScope(this IServiceScopeFactory)`, which internally wraps `CreateScope()` — the real interface method, and the one Moq can actually intercept. `AsyncServiceScope.DisposeAsync()` falls back to the wrapped scope's sync `Dispose()` if it isn't itself `IAsyncDisposable`, which `Mock<IServiceScope>` supports for free (it implements `IDisposable` already) — no extra setup needed for disposal to work in tests.
- **AD-12 compliance:** `ICustomerListView`, `ICustomerDetailView`, both Presenters, and both Forms reference only `CustomerDto`, `ICustomerService`, and `Result<T>` from `OrderFlow.BLL` — never `OrderFlow.Domain.Customer` or any `OrderFlow.DAL` type. This is directly checkable (AC #4): grep for `OrderFlow.DAL`/`OrderFlow.Domain` `using` directives in any file this story adds under `OrderFlow.Presentation/` — there should be none (the pre-existing `Program.cs` composition-root exception is unaffected and unrelated).
- **`UNVERIFIED-ENVIRONMENT` scope for this story, precisely:** (1) actually launching/clicking through the Forms — same Story 1.1 gap, still open; (2) now also: actually *running* the new Presenter tests (they build; the `OrderFlow.Presentation.Tests` test host needs `Microsoft.WindowsDesktop.App`, unavailable on macOS, confirmed empirically during story creation). Both are pending a Windows machine or CI runner. Everything else in this story (compilation of all 7 projects, `OrderFlow.Tests`'s continued local pass) is fully verifiable now and must be.

### Project Structure Notes

```
OrderFlow/
  OrderFlow.Presentation/
    ICustomerListView.cs            # new
    CustomerListPresenter.cs        # new
    CustomerListForm.cs             # new
    CustomerListForm.Designer.cs    # new
    ICustomerDetailView.cs          # new
    CustomerDetailPresenter.cs      # new
    CustomerDetailForm.cs           # new
    CustomerDetailForm.Designer.cs  # new
    MainForm.cs                     # modified: + IServiceProvider, "Customers" button handler
    MainForm.Designer.cs            # modified: + Button control
    Program.cs                      # modified: register CustomerListForm, CustomerDetailForm
  OrderFlow.Presentation.Tests/     # new project (7th) — see Dev Notes
    OrderFlow.Presentation.Tests.csproj
    CustomerListPresenterTests.cs
    CustomerDetailPresenterTests.cs
  OrderFlow.sln / OrderFlow.slnx    # modified: + OrderFlow.Presentation.Tests
```

`OrderFlow.Tests/` is untouched by this story. The Architecture Spine's Structural Seed is amended (new project note added) as part of this story, not left implicit.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3: Customer Management UI] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Overview] — "no UX design contract exists for this project... UX phase was skipped by design"
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-3 — Presentation: constructor-injected Presenter + per-screen IView]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-5 — DI lifetimes: scoped-per-operation, Singleton reserved for config]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-12 — Domain entities never cross the BLL→Presentation boundary]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Structural Seed] — `OrderFlow.Presentation.Tests` amendment added alongside this story
- [Source: _bmad-output/implementation-artifacts/1-2-customer-domain-repository-service.md] — `ICustomerService`/`CustomerDto`/`Result<T>` contract this story consumes; `IUnitOfWork` implements both `IDisposable`/`IAsyncDisposable`, anticipated exactly for this story's `await using` scope pattern
- [Source: _bmad-output/implementation-artifacts/1-1-solution-scaffold-composition-root.md] — `MainForm` shell, `Program.cs` composition root, `UNVERIFIED-ENVIRONMENT` precedent this story extends

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 7 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 10, Skipped: 0, Total: 10 — confirms the `OrderFlow.Presentation.Tests` split left the existing suite fully untouched and runnable.
- `dotnet build OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj`: Build succeeded, 0 Warning(s), 0 Error(s). `dotnet test` intentionally **not** attempted on this project here — confirmed during story creation that it aborts the test host on macOS (`Microsoft.WindowsDesktop.App` not found), same root cause as Story 1.1's `MainForm` launch gap.

### Completion Notes List

- **Two decisions made with the user during story creation are load-bearing for this implementation** (see Dev Notes): (1) Presenter tests are written now but accepted as `UNVERIFIED-ENVIRONMENT` on macOS; (2) a new 7th project `OrderFlow.Presentation.Tests` isolates them, keeping `OrderFlow.Tests` fully runnable locally. Both empirically verified true during story creation *and* re-confirmed during implementation (10/10 existing tests still pass; new test project builds but wasn't run here).
- `ICustomerListView`/`CustomerListPresenter` and `ICustomerDetailView`/`CustomerDetailPresenter` implement AD-3's Presenter pattern exactly as specified: constructor-injected with `IView` + `IServiceScopeFactory`, one `await using ... CreateAsyncScope()` per action, never a long-lived `ICustomerService` instance.
- `CustomerListForm`/`CustomerDetailForm` construct their own Presenters (`this` + injected `IServiceScopeFactory`) in their constructors, per the "Presenter construction at Form-creation time" resolution documented in Dev Notes.
- The narrow root-`IServiceProvider`-for-Form-launching exception was applied exactly as scoped: `MainForm` and `CustomerListForm` (both launch other Forms) receive it; `CustomerDetailForm` (a leaf) does not.
- `CustomerListForm.DisplayCustomers` binds `CustomerDto` objects directly to `DataGridView.DataSource` (hiding the `Id` column) and "Edit" recovers the selected row's `Id` via `CurrentRow.DataBoundItem as CustomerDto` — exactly the pattern the independent story review flagged as needing explicit specification.
- `CustomerDetailForm`'s create/edit mode uses the post-construction `Initialize(int? customerId)` method; the edit-mode fetch happens in the `Load` event handler, not in `Initialize`, per Dev Notes' reasoning about `ShowDialog()`'s message-loop timing.
- `DialogResult.OK`/`Cancel` is the List↔Detail refresh signal; `CustomerDetailPresenter.SaveAsync` stays WinForms-agnostic, returning `bool`.
- AC #4 verified directly: `grep -l "using OrderFlow.DAL\|using OrderFlow.Domain"` across every new `OrderFlow.Presentation/` file (excluding the pre-existing, sanctioned `Program.cs` composition-root exception) returned no matches.
- Presenter tests use the exact Moq pattern documented in Dev Notes (mocking `IServiceScopeFactory.CreateScope()`, not the `CreateAsyncScope()` extension method) — 7 tests total across both Presenters, covering success/failure paths and the create-vs-update dispatch in `SaveAsync`.
- No new `UNVERIFIED-ENVIRONMENT` gaps beyond what was already disclosed and agreed during story creation: (1) Story 1.1's pre-existing `MainForm` launch gap, unchanged; (2) the new Presenter-test-execution gap, isolated to `OrderFlow.Presentation.Tests` and explicitly scoped to this story.
- **Code review (2026-08-07) — 8 patches applied:** fixed a real resource leak (`CustomerDetailForm` resolved per Add/Edit click was never disposed — `ShowDialog()` doesn't auto-dispose like `Show()` does; now wrapped in `using`); set `Owner` on `CustomerListForm.Show(this)` so closing `MainForm` cascades to owned windows; added reentrancy guards (button-disable/re-enable around every `await`) to Add/Edit/Refresh/Save, closing a double-click → duplicate-record risk; wired `DataGridView.SelectionChanged` to keep "Edit" correctly enabled/disabled instead of silently no-opping; replaced the bare `"Id"` string with `nameof(CustomerDto.Id)`; extracted the duplicated `CreateMockScope()` test helper into a shared `MockScopeHelper`; added missing `ShowError`-never-called assertions to 3 success-path Presenter tests; amended AD-3 in the Architecture Spine to formally record the root-`IServiceProvider`-for-Form-launching exception (previously only in this story's Dev Notes, inconsistent with Story 1.1's own precedent of amending AD text directly).
- **Deliberately dismissed, not fixed:** wrapping Presenter service calls in try/catch for thrown (non-`Result<T>`) exceptions — the architecture's own "UI error surfacing" convention explicitly routes infrastructure exceptions through the global `Application.ThreadException` handler (built in Story 1.1's review), not per-Presenter try/catch; adding it here would violate the documented convention. A "Load fails but Save still succeeds with blank data" scenario was checked against Story 1.2's actual `CustomerService.UpdateAsync` and found not reachable — it always re-validates existence before mutating, so a deleted customer fails loudly at Save time too.

### File List

- `OrderFlow/OrderFlow.Presentation/ICustomerListView.cs` (new)
- `OrderFlow/OrderFlow.Presentation/CustomerListPresenter.cs` (new)
- `OrderFlow/OrderFlow.Presentation/CustomerListForm.cs` (new; modified during code review: `using` disposal for `CustomerDetailForm`, reentrancy guards, `SelectionChanged` handler, `nameof(CustomerDto.Id)`)
- `OrderFlow/OrderFlow.Presentation/CustomerListForm.Designer.cs` (new; modified during code review: wire `SelectionChanged`, initial `editButton.Enabled = false`)
- `OrderFlow/OrderFlow.Presentation/ICustomerDetailView.cs` (new)
- `OrderFlow/OrderFlow.Presentation/CustomerDetailPresenter.cs` (new)
- `OrderFlow/OrderFlow.Presentation/CustomerDetailForm.cs` (new; modified during code review: reentrancy guard on Save)
- `OrderFlow/OrderFlow.Presentation/CustomerDetailForm.Designer.cs` (new)
- `OrderFlow/OrderFlow.Presentation/MainForm.cs` (modified: `IServiceProvider` injection, "Customers" button handler; modified again during code review: `Show(this)` to set `Owner`)
- `OrderFlow/OrderFlow.Presentation/MainForm.Designer.cs` (modified: `customersButton` control)
- `OrderFlow/OrderFlow.Presentation/Program.cs` (modified: register `CustomerListForm`, `CustomerDetailForm`)
- `OrderFlow/OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj` (new — 7th project)
- `OrderFlow/OrderFlow.Presentation.Tests/CustomerListPresenterTests.cs` (new; modified during code review: use shared `MockScopeHelper`)
- `OrderFlow/OrderFlow.Presentation.Tests/CustomerDetailPresenterTests.cs` (new; modified during code review: use shared `MockScopeHelper`, added `ShowError`-never-called assertions to 3 success-path tests)
- `OrderFlow/OrderFlow.Presentation.Tests/MockScopeHelper.cs` (new — added during code review, extracted from duplicated test setup)
- `OrderFlow/OrderFlow.sln` (modified: + `OrderFlow.Presentation.Tests`)
- `OrderFlow/OrderFlow.slnx` (modified: + `OrderFlow.Presentation.Tests`)
- `_bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md` (modified: Structural Seed amended with `OrderFlow.Presentation.Tests` note; modified again during code review: AD-3 amended with the Form-launching exception)

## Change Log

- 2026-08-07: Implemented Story 1.3 — first UI story. Added `OrderFlow.Presentation.Tests` (7th project, architecture-amended) to isolate Presenter tests from the WinForms-runtime constraint discovered during story creation. Built `ICustomerListView`/`CustomerListPresenter`, `ICustomerDetailView`/`CustomerDetailPresenter`, `CustomerListForm`, `CustomerDetailForm`, and wired `MainForm`/`Program.cs`. `dotnet build` green across all 7 projects; `OrderFlow.Tests` unaffected (10/10 still passing); `OrderFlow.Presentation.Tests` builds but is `UNVERIFIED-ENVIRONMENT` for actual test execution, consistent with Story 1.1's `MainForm` launch precedent and the user's explicit decisions during story creation.
- 2026-08-07: Code review applied — fixed a real `CustomerDetailForm` disposal leak (`ShowDialog()` doesn't auto-dispose), set `Owner` on `CustomerListForm.Show()`, added reentrancy guards to every action button, fixed the Edit-button selection-state gap, replaced a magic string with `nameof()`, deduplicated test setup into `MockScopeHelper`, strengthened 3 Presenter tests, and amended AD-3 in the Architecture Spine to formally record the Form-launching exception. 3 items deferred to `deferred-work.md`. `dotnet build`/`dotnet test OrderFlow.Tests` re-verified green (10/10) after all changes.
