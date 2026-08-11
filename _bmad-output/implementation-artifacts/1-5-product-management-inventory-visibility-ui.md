---
baseline_commit: NO_VCS
---

# Story 1.5: Product Management & Inventory Visibility UI

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to create, view, edit, and list Products, and see current stock levels,
so that I can manage the catalog and know what's available to sell.

## Acceptance Criteria

1. **Given** the app is running, **When** I open the Product list, **Then** `ProductListForm`/`ProductListPresenter` displays all Products (`Name`, `SKU`, `UnitPrice`) alongside each Product's current `StockQuantity`, loaded asynchronously.
2. **And Given** the list, **When** I create or edit a Product, **Then** valid submission persists and refreshes the list; invalid input (e.g. missing `SKU`) surfaces the `Result<T>` failure without crashing.
3. **And Given** the Product list, **When** I view stock levels, **Then** `StockQuantity` reflects the latest committed value — fulfilling FR-6.

## Tasks / Subtasks

- [x] Task 1: Generalize `MockScopeHelper` for reuse (prerequisite — do this first)
  - [x] `OrderFlow.Presentation.Tests/MockScopeHelper.cs`: change `CreateMockScope()` to a generic `CreateMockScope<TService>() where TService : class`, returning `(Mock<IServiceScopeFactory> scopeFactory, Mock<TService> service)` — same mocking mechanics (mock `CreateScope()`, not `CreateAsyncScope()`), just parameterized by service type instead of hardcoded to `ICustomerService`
  - [x] Update `CustomerListPresenterTests`/`CustomerDetailPresenterTests`' local `CreateMockScope()` wrapper methods to call `MockScopeHelper.CreateMockScope<ICustomerService>()` — confirm they still compile and pass unchanged otherwise
  - [x] This story's own `ProductListPresenterTests`/`ProductDetailPresenterTests` (Task 7) use `MockScopeHelper.CreateMockScope<IProductService>()` directly — see Dev Notes on why this was worth doing now rather than copy-pasting a second near-identical helper (exactly the DRY finding Story 1.3's code review caught once already for the Customer pair)
- [x] Task 2: `IProductListView` + `ProductListPresenter` (AC: #1, #2)
  - [x] `OrderFlow.Presentation/IProductListView.cs`: `void DisplayProducts(IReadOnlyList<ProductDto> products)`, `void ShowError(string message)`
  - [x] `OrderFlow.Presentation/ProductListPresenter.cs`: constructor `(IProductListView view, IServiceScopeFactory scopeFactory)`. `LoadProductsAsync()`: `await using var scope = _scopeFactory.CreateAsyncScope();` → `scope.ServiceProvider.GetRequiredService<IProductService>()` → `GetAllAsync()` → `_view.DisplayProducts(...)` on success, `_view.ShowError(...)` on failure. Never `.ConfigureAwait(false)` (same reason as Story 1.3: breaks UI-thread marshaling for `DisplayProducts`)
- [x] Task 3: `ProductListForm` (AC: #1, #2, #3)
  - [x] `OrderFlow.Presentation/ProductListForm.cs` + `.Designer.cs`: implements `IProductListView`; a `DataGridView` (read-only, `AutoGenerateColumns = true`) plus "Add", "Edit", "Refresh" buttons — identical structural shape to `CustomerListForm`
  - [x] `DisplayProducts(IReadOnlyList<ProductDto> products)` sets `dataGridView.DataSource = products.ToList()`, hides the `Id` column via `nameof(ProductDto.Id)` (never a bare string — Story 1.3 code review fix, apply it from the start this time), and sets `editButton.Enabled = dataGridView.CurrentRow is not null`. `ProductDto` already carries `Name`/`SKU`/`UnitPrice`/`StockQuantity` (Story 1.4) — binding it directly satisfies AC #1's "alongside StockQuantity" requirement with no extra column wiring
  - [x] "Edit" reads the selected product via `(dataGridView.CurrentRow?.DataBoundItem as ProductDto)?.Id`; no-op when nothing is selected; `dataGridView.SelectionChanged` wired to keep `editButton.Enabled` correct (Story 1.3 code review fix — bake in from the start)
  - [x] Constructor takes `IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory` (root-provider-for-Form-launching exception, AD-3 — see Story 1.3's amendment); constructs `_presenter = new ProductListPresenter(this, scopeFactory)`
  - [x] `OpenDetailFormAsync`: `using var detailForm = _serviceProvider.GetRequiredService<ProductDetailForm>();` (Story 1.3 code review fix — `ShowDialog()`-shown forms don't auto-dispose, bake the `using` in from the start, don't reintroduce the leak); `.Initialize(productId)`; `ShowDialog(this) == DialogResult.OK` → reload
  - [x] Add/Edit/Refresh buttons all disable themselves before their `await` and re-enable in `finally` (Story 1.3 code review fix — reentrancy guard, bake in from the start). **Re-enable is not identical across buttons:** `Add`/`Refresh` unconditionally re-enable to `true`; `Edit` re-enables to `dataGridView.CurrentRow is not null` (matching `CustomerListForm.EditButton_Click` exactly) — if `Edit` re-enabled unconditionally instead, it would stay enabled after a save even with nothing selected, a milder version of the exact no-op-with-no-feedback bug Story 1.3's `SelectionChanged` fix addressed
  - [x] `editButton.Enabled = false` set in `.Designer.cs`'s `InitializeComponent()` (initial state, before the async `Load` handler populates the grid) — matches `CustomerListForm.Designer.cs` exactly; without it, `Edit` is clickable (WinForms' default) during the brief window before data loads
  - [x] Wire `Load`/`Click`/`SelectionChanged` handlers in `.Designer.cs`'s `InitializeComponent()` — an unsubscribed handler compiles fine but silently breaks the AC
- [x] Task 4: `IProductDetailView` + `ProductDetailPresenter` (AC: #2)
  - [x] `OrderFlow.Presentation/IProductDetailView.cs`: `void ShowProduct(ProductDto product)`, `void ShowError(string message)`
  - [x] `OrderFlow.Presentation/ProductDetailPresenter.cs`: constructor `(IProductDetailView view, IServiceScopeFactory scopeFactory)`. `Task<bool> LoadAsync(int productId)` and `Task<bool> SaveAsync(int? productId, ProductDto dto)` — identical shape to `CustomerDetailPresenter` (`GetAsync`/`CreateAsync`-or-`UpdateAsync` dispatch on `productId.HasValue`), using `GetRequiredService<IProductService>()`
- [x] Task 5: `ProductDetailForm` (AC: #2)
  - [x] `OrderFlow.Presentation/ProductDetailForm.cs` + `.Designer.cs`: implements `IProductDetailView`; `TextBox`es for `Name`/`SKU`, a `NumericUpDown` for `UnitPrice` (`DecimalPlaces = 2`, `Minimum = 0`, `Maximum = 999999.99` — constrains input range at the UI level, which also narrows the decimal-overflow edge case Story 1.4's code review deferred), a `NumericUpDown` for `StockQuantity` (`Minimum = 0`, `Maximum = 1000000`, `DecimalPlaces = 0`), "Save"/"Cancel" buttons. **Note:** the `NumericUpDown`'s `Minimum = 0` for `UnitPrice` is more permissive than `ProductService.Validate`'s `UnitPrice <= 0` rejection (which requires strictly greater than zero) — this is fine, not a bug to fix here: a `0` entry still reaches the BLL, still gets rejected, and `ShowError` still surfaces it per AC #2. Don't tighten the `NumericUpDown`'s `Minimum` to try to pre-empt this; the BLL is the source of truth for the validation rule
  - [x] Constructor takes `IServiceScopeFactory scopeFactory` only (leaf form, same reasoning as `CustomerDetailForm`); constructs `_presenter = new ProductDetailPresenter(this, scopeFactory)`
  - [x] `public void Initialize(int? productId)`: stores `_productId`; sets form title ("Add Product" / "Edit Product")
  - [x] `Load` event: if `_productId.HasValue`, `await _presenter.LoadAsync(_productId.Value)` to populate the fields (same "fetch in `Load`, not `Initialize`" reasoning as Story 1.3)
  - [x] "Save" click: reentrancy-guarded (disable/re-enable, same as `CustomerDetailForm`); build a `ProductDto` from `nameTextBox.Text`/`skuTextBox.Text`/`unitPriceNumericUpDown.Value`/`stockQuantityNumericUpDown.Value` (cast `decimal`→`int` for `StockQuantity`); `await _presenter.SaveAsync(_productId, dto)` → `DialogResult.OK` + `Close()` on `true`
  - [x] "Cancel" click: `DialogResult.Cancel` + `Close()`, no BLL call
  - [x] `ShowProduct`/`ShowError` implement `IProductDetailView` (populate fields; `MessageBox.Show` for errors — this already correctly surfaces a `ConcurrencyConflictException`-driven `Result<T>.Failure` from `ProductService.UpdateAsync`, no special-case handling needed here, `Result<T>` is already opaque to *why* it failed)
- [x] Task 6: Wire up `MainForm` + composition root (AC: #1)
  - [x] `MainForm.cs`/`.Designer.cs`: add a second "Products" `Button` — the existing `customersButton` is at `Location = (12, 12)`, `Size = (120, 30)`; place `productsButton` at `Location = (144, 12)` (same row, no overlap), same `Size`; `ProductsButton_Click` resolves `ProductListForm` via the already-injected `IServiceProvider`, `.Show(this)` (non-modal, `Owner` set — same convention as `CustomersButton_Click`)
  - [x] `Program.cs` `ConfigureServices`: `services.AddTransient<ProductListForm>();` and `services.AddTransient<ProductDetailForm>();`
- [x] Task 7: Presenter tests in `OrderFlow.Presentation.Tests` (AC: #1, #2) — **UNVERIFIED-ENVIRONMENT for actual execution, same as Story 1.3's Presenter tests: builds here, runs on Windows/CI**
  - [x] `ProductListPresenterTests`: `LoadProductsAsync` success (`view.DisplayProducts` called) and failure (`view.ShowError` called) paths, using `MockScopeHelper.CreateMockScope<IProductService>()`
  - [x] `ProductDetailPresenterTests`: `LoadAsync` found/not-found; `SaveAsync` create path (`productId == null` → `CreateAsync`), update path (`productId.HasValue` → `UpdateAsync`), validation/concurrency-failure path (`view.ShowError` called, returns `false`) — mirrors `CustomerDetailPresenterTests` exactly
- [x] Task 8: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 7 projects) — 0 errors, 0 warnings
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` still passes all existing tests (confirms Task 1's `MockScopeHelper` generalization didn't break anything) — **confirmed: 28/28 passed**
  - [x] `dotnet build OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj` succeeds — do **not** attempt `dotnet test` on it here (same `Microsoft.WindowsDesktop.App` runtime gap as Story 1.3) — **confirmed: build succeeded, 0 warnings, 0 errors; `dotnet test` intentionally not attempted**

### Review Findings

- [x] [Review][Patch] `ProductDetailForm.ShowProduct` assigns `product.UnitPrice`/`product.StockQuantity` directly to `NumericUpDown.Value` with no bounds check — a pre-existing Product outside the UI's hardcoded range (`UnitPrice` > 999999.99, or `StockQuantity` > 1,000,000; neither bound is enforced by `ProductService.Validate` or the DB) throws an unhandled `ArgumentOutOfRangeException` from the `async void` `ProductDetailForm_Load`, surfacing as a global "Fatal Error" dialog instead of opening the edit form. Clamp both values to `[Minimum, Maximum]` before assignment. **Fixed 2026-08-09:** both assignments now go through `Math.Clamp(...)` against the control's `Minimum`/`Maximum`. [`OrderFlow.Presentation/ProductDetailForm.cs:69-72`]
- [x] [Review][Defer] `ProductListForm_Load` doesn't disable Add/Refresh while its own initial `LoadProductsAsync()` await is in flight, allowing an overlapping second load to race the first — deferred, pre-existing pattern from Story 1.3's `CustomerListForm_Load` (identical shape), already covered by Story 1.3's deferred "no busy/loading UI indicator during any async operation." [`OrderFlow.Presentation/ProductListForm.cs:22-24`]
- [x] [Review][Defer] `ProductDetailForm`'s Save/Cancel buttons aren't guarded against each other during in-flight async work (`Cancel` can close the form while `SaveButton_Click`'s `await` is still pending; `Save` isn't disabled while the initial `LoadAsync` in `ProductDetailForm_Load` is in flight) — deferred, identical to `CustomerDetailForm` (Story 1.3), same already-deferred "no busy/loading indicator" gap. [`OrderFlow.Presentation/ProductDetailForm.cs:26-32,34-57,59-63`]
- [x] [Review][Defer] `editButton.Enabled = dataGridView.CurrentRow is not null` is independently recomputed in three places (`DataGridView_SelectionChanged`, `DisplayProducts`, `EditButton_Click`'s `finally`) — deferred, pre-existing triplication copied verbatim from `CustomerListForm` (Story 1.3) per this story's explicit "mirror Story 1.3" instruction; not introduced by this diff. [`OrderFlow.Presentation/ProductListForm.cs:67,73,96`]
- [x] [Review][Defer] `ProductDetailForm.Designer.cs` sets `FormBorderStyle = FixedDialog` but never assigns `AcceptButton`/`CancelButton`, so Enter/Esc do nothing in the dialog — deferred, identical gap in `CustomerDetailForm.Designer.cs` (Story 1.3), not new. [`OrderFlow.Presentation/ProductDetailForm.Designer.cs`]
- [x] [Review][Defer] `ProductDetailPresenterTests.SaveAsync_OnFailure_ShowsErrorAndReturnsFalse` only exercises the `CreateAsync`-failure branch, never `UpdateAsync`-failure — deferred, low-risk (both branches share identical post-processing code) and mirrors an identical, already-accepted gap in `CustomerDetailPresenterTests` (Story 1.3). [`OrderFlow.Presentation.Tests/ProductDetailPresenterTests.cs:77`]
- [x] [Review][Defer] `MainForm`'s `ProductsButton_Click`/`CustomersButton_Click` resolve a new Transient list-form on every click with no single-instance tracking, so repeated clicks open unlimited duplicate non-modal windows — deferred, identical pre-existing pattern from Story 1.3, already flagged there as the disclosed Service Locator tradeoff. [`OrderFlow.Presentation/MainForm.cs:20-30`]
- [x] [Review][Defer] `ProductListForm.DisplayProducts` relies entirely on `AutoGenerateColumns = true` with no explicit column config — `UnitPrice` renders as a raw decimal (no currency formatting) and headers show raw property names — deferred, same pre-existing pattern as `CustomerListForm` (Story 1.3), UX polish not required by any AC. [`OrderFlow.Presentation/ProductListForm.cs:87-97`]
- [x] [Review][Defer] `ProductDetailForm` has no unsaved-changes confirmation on Cancel and no client-side validation before calling `SaveAsync` (relies entirely on the BLL round trip) — deferred, identical to `CustomerDetailForm` (Story 1.3), UX polish not required by any AC. [`OrderFlow.Presentation/ProductDetailForm.cs:34-63`]

## Dev Notes

- **This story is structurally identical to Story 1.3 (Customer Management UI) — same Presenter/IView/Form shape, same DI patterns, same root-`IServiceProvider`-for-Form-launching exception (AD-3).** Every design question Story 1.3 had to work out from scratch (Presenter construction timing, `Initialize`-then-`Load`-event for edit-mode fetch, `DialogResult`-based refresh signal, the Moq `CreateScope()`-not-`CreateAsyncScope()` test pattern) is already settled — just mirror `Customer*`/`ICustomer*View` files with `Product*`/`IProduct*View` names and `ProductDto`/`IProductService` types. Don't re-derive any of it.
- **Every code-review fix from Story 1.3 is baked into this story's tasks from the start, not left to be rediscovered:** `using var detailForm = ...` (dialog disposal — `ShowDialog()` doesn't auto-dispose, `Show()` does), `.Show(this)` to set `Owner`, reentrancy guards on every action button, `SelectionChanged` wiring for `editButton.Enabled`, `nameof(ProductDto.Id)` instead of a bare `"Id"` string. If the implementation doesn't include these, it's missing something this story explicitly asked for — not a "nice to have" a future review might catch.
- **`MockScopeHelper` is generalized in Task 1 specifically to avoid recreating the exact duplication Story 1.3's own code review flagged** (`CreateMockScope()` copy-pasted across `CustomerListPresenterTests`/`CustomerDetailPresenterTests`). Generalizing it once now means this story's `ProductListPresenterTests`/`ProductDetailPresenterTests` — and every future UI story's presenter tests (Order, etc.) — reuse the same helper instead of each adding their own copy.
- **AC #3 ("StockQuantity reflects the latest committed value") needs no extra implementation** — it's satisfied by the same refresh-after-save pattern already built for the Customer list (`OpenDetailFormAsync` reloads via the Presenter on `DialogResult.OK`) plus `ProductRepository.GetByIdAsync`/`GetAllAsync`'s `.Include(p => p.Inventory)` (Story 1.4) always reading the current row. Don't add polling, caching, or any other mechanism — the existing data flow already guarantees this.
- **`ProductService.UpdateAsync`'s `ConcurrencyConflictException` handling (Story 1.4) needs no special UI-layer handling either.** It already returns `Result<ProductDto>.Failure(ConcurrencyConflictException.DefaultMessage)` — from `ProductDetailPresenter.SaveAsync`'s perspective this is indistinguishable from any other validation failure, and `ShowError` already handles it generically. Do not add a `catch` for `ConcurrencyConflictException` anywhere in `OrderFlow.Presentation` — it never crosses the BLL boundary as a thrown exception, only as a `Result<T>.Failure`.
- **`NumericUpDown` for `UnitPrice`/`StockQuantity`, not `TextBox` + manual parsing:** avoids an entire class of "user typed non-numeric text" edge cases at the UI level for free, and the `Maximum` bound on `UnitPrice` narrows (though doesn't eliminate at the DB level) the `decimal(18,2)` overflow scenario Story 1.4's code review deferred. `NumericUpDown.Value` is `decimal`; cast to `int` for `StockQuantity` (`(int)stockQuantityNumericUpDown.Value`).
- **`OrderFlow.Presentation.csproj` already references `OrderFlow.BLL`** (Story 1.1) — `ProductDto`/`IProductService` need no new project reference.

### Project Structure Notes

```
OrderFlow/
  OrderFlow.Presentation/
    IProductListView.cs             # new
    ProductListPresenter.cs         # new
    ProductListForm.cs              # new
    ProductListForm.Designer.cs     # new
    IProductDetailView.cs           # new
    ProductDetailPresenter.cs       # new
    ProductDetailForm.cs            # new
    ProductDetailForm.Designer.cs   # new
    MainForm.cs                     # modified: "Products" button handler
    MainForm.Designer.cs            # modified: + productsButton control
    Program.cs                      # modified: register ProductListForm, ProductDetailForm
  OrderFlow.Presentation.Tests/
    MockScopeHelper.cs              # modified: generalized to CreateMockScope<TService>()
    CustomerListPresenterTests.cs   # modified: call CreateMockScope<ICustomerService>()
    CustomerDetailPresenterTests.cs # modified: call CreateMockScope<ICustomerService>()
    ProductListPresenterTests.cs    # new
    ProductDetailPresenterTests.cs  # new
```

`OrderFlow.Tests/` is untouched by this story (all changes are Presentation-layer, mirroring Story 1.3).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.5: Product Management & Inventory Visibility UI] — acceptance criteria origin
- [Source: _bmad-output/implementation-artifacts/1-3-customer-management-ui.md] — the Presenter/IView/Form pattern this story replicates verbatim, including every code-review fix baked in from the start this time
- [Source: _bmad-output/implementation-artifacts/1-4-product-inventory-domain-repository-service.md] — `ProductDto`/`IProductService`/`ConcurrencyConflictException` contract this story consumes
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-3 — Presentation: constructor-injected Presenter + per-screen IView, incl. Story 1.3's Form-launching exception amendment]

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 7 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 28, Skipped: 0, Total: 28 — confirms the `MockScopeHelper` generalization (Task 1) and this story's Presentation-only changes left `OrderFlow.Tests` fully untouched and runnable.
- `dotnet build OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj`: Build succeeded, 0 Warning(s), 0 Error(s). `dotnet test` intentionally **not** attempted on this project here — same `Microsoft.WindowsDesktop.App` runtime gap as Story 1.3.
- `grep -l "using OrderFlow.DAL\|using OrderFlow.Domain"` across every new `OrderFlow.Presentation/` file (excluding `Program.cs`'s pre-existing, sanctioned composition-root exception): no matches — AD-12 compliance confirmed directly.

### Completion Notes List

- This story mirrored Story 1.3's `Customer*` Presenter/IView/Form pattern into `Product*` files with no new design decisions required — every question (Presenter construction timing, `Initialize`-then-`Load`-event edit-mode fetch, `DialogResult`-based refresh signal, root-`IServiceProvider`-for-Form-launching exception) was already settled by Story 1.3 and its code review.
- Every Story 1.3 code-review fix was baked in from the start, not left to be rediscovered: `using var detailForm = ...` (dialog disposal), `.Show(this)`/`ShowDialog(this)` for `Owner`, reentrancy guards on every action button (with `Edit`'s conditional re-enable correctly distinguished from `Add`/`Refresh`'s unconditional re-enable), `SelectionChanged` wiring plus the initial `editButton.Enabled = false` in `.Designer.cs`, and `nameof(ProductDto.Id)` instead of a bare string.
- `MockScopeHelper.CreateMockScope()` was generalized to `CreateMockScope<TService>()` (Task 1, done first) — the two existing `CustomerListPresenterTests`/`CustomerDetailPresenterTests` wrapper methods now call `CreateMockScope<ICustomerService>()` and were otherwise unchanged; this story's own Presenter tests call `CreateMockScope<IProductService>()` directly with no local wrapper needed. Avoids recreating the exact test-helper duplication Story 1.3's own code review flagged once already.
- `ProductDetailForm` uses `NumericUpDown` (not `TextBox` + manual parsing) for `UnitPrice` (`DecimalPlaces=2`, `Maximum=999999.99`) and `StockQuantity` (`Minimum=0`, `Maximum=1000000`) — eliminates non-numeric-input edge cases at the UI level and narrows (though doesn't eliminate at the DB level) the `decimal(18,2)` overflow scenario Story 1.4's code review deferred. `StockQuantity` cast `decimal`→`int` when building the `ProductDto` in `SaveButton_Click`.
- `ProductDto` binds directly to `DataGridView.DataSource` in `DisplayProducts` — its declared property order (`Id, Name, SKU, UnitPrice, StockQuantity`) naturally produces AC #1's stated column order under `AutoGenerateColumns=true`, no extra column configuration needed.
- AC #3 ("`StockQuantity` reflects the latest committed value") required no additional implementation — satisfied by the existing refresh-after-save pattern (`OpenDetailFormAsync` reloads on `DialogResult.OK`) plus `ProductRepository`'s `.Include(p => p.Inventory)` (Story 1.4) always reading the current row.
- `ProductService.UpdateAsync`'s `ConcurrencyConflictException`-driven `Result<T>.Failure` needed no special UI-layer handling — `ShowError` already surfaces it generically, exactly as anticipated in Dev Notes; no `catch` for `ConcurrencyConflictException` exists anywhere in `OrderFlow.Presentation`.
- `MainForm` gained a "Products" button at `(144, 12)`, matching the existing "Customers" button's size and row, `Owner`-launching `ProductListForm` the same way.
- No new `UNVERIFIED-ENVIRONMENT` gaps beyond what Story 1.3 already established (Presenter-test execution, `MainForm` launch) — everything else (build, `OrderFlow.Tests`) is fully verifiable on macOS and was verified.

### File List

- `OrderFlow/OrderFlow.Presentation.Tests/MockScopeHelper.cs` (modified: generalized `CreateMockScope()` → `CreateMockScope<TService>()`)
- `OrderFlow/OrderFlow.Presentation.Tests/CustomerListPresenterTests.cs` (modified: `CreateMockScope<ICustomerService>()`)
- `OrderFlow/OrderFlow.Presentation.Tests/CustomerDetailPresenterTests.cs` (modified: `CreateMockScope<ICustomerService>()`)
- `OrderFlow/OrderFlow.Presentation/IProductListView.cs` (new)
- `OrderFlow/OrderFlow.Presentation/ProductListPresenter.cs` (new)
- `OrderFlow/OrderFlow.Presentation/ProductListForm.cs` (new)
- `OrderFlow/OrderFlow.Presentation/ProductListForm.Designer.cs` (new)
- `OrderFlow/OrderFlow.Presentation/IProductDetailView.cs` (new)
- `OrderFlow/OrderFlow.Presentation/ProductDetailPresenter.cs` (new)
- `OrderFlow/OrderFlow.Presentation/ProductDetailForm.cs` (new)
- `OrderFlow/OrderFlow.Presentation/ProductDetailForm.Designer.cs` (new)
- `OrderFlow/OrderFlow.Presentation/MainForm.cs` (modified: "Products" button handler)
- `OrderFlow/OrderFlow.Presentation/MainForm.Designer.cs` (modified: `productsButton` control)
- `OrderFlow/OrderFlow.Presentation/Program.cs` (modified: register `ProductListForm`, `ProductDetailForm`)
- `OrderFlow/OrderFlow.Presentation.Tests/ProductListPresenterTests.cs` (new)
- `OrderFlow/OrderFlow.Presentation.Tests/ProductDetailPresenterTests.cs` (new)

## Change Log

- 2026-08-09: Implemented Story 1.5 — second UI story, structurally identical to Story 1.3. Generalized `MockScopeHelper` to `CreateMockScope<TService>()` (prerequisite, avoids re-duplicating the test helper). Built `IProductListView`/`ProductListPresenter`/`ProductListForm`, `IProductDetailView`/`ProductDetailPresenter`/`ProductDetailForm` (with `NumericUpDown` for `UnitPrice`/`StockQuantity`), wired `MainForm`'s "Products" button and `Program.cs` registration, and added 9 new Presenter tests. Every Story 1.3 code-review fix (dialog disposal, `Owner` setting, reentrancy guards, `SelectionChanged` wiring, `nameof()`) was baked in from the start rather than left for review to catch. `dotnet build` green across all 7 projects; `OrderFlow.Tests` unaffected (28/28 still passing); `OrderFlow.Presentation.Tests` builds but is `UNVERIFIED-ENVIRONMENT` for execution, same posture as Story 1.3. AD-12 compliance (no `OrderFlow.DAL`/`Domain` references) verified directly via grep.
- 2026-08-09: Code review (3-layer: Blind Hunter, Edge Case Hunter, Acceptance Auditor) — 1 patch applied, 8 deferred (all confirmed as pre-existing patterns replicated verbatim from Story 1.3/1.4, logged to `deferred-work.md`), 16 dismissed as false positives/unreachable/methodology noise after direct code verification. Patch: `ProductDetailForm.ShowProduct` now clamps `product.UnitPrice`/`StockQuantity` to each `NumericUpDown`'s `[Minimum, Maximum]` before assignment, preventing an unhandled `ArgumentOutOfRangeException` (→ global Fatal Error dialog) when opening a pre-existing Product outside the UI's range. `dotnet build` green across all 7 projects post-fix.
