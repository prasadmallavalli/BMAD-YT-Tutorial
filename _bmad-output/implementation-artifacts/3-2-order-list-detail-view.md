---
baseline_commit: NO_VCS
---

# Story 3.2: Order List & Detail View

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a user,
I want to view a list of Orders and open an Order's detail,
so that I can see its line items, total, OrderType, and current status.

## Acceptance Criteria

1. **Given** the app is running, **When** I open the Order list, **Then** `OrderListForm`/`OrderListPresenter` displays all Orders (Customer name, OrderType, OrderStatus, total) loaded asynchronously via `IOrderService`.
2. **And Given** the Order list, **When** I open an Order, **Then** `OrderDetailForm`/`OrderDetailPresenter` shows its line items (Product, Quantity, `UnitPriceAtOrder`), computed total, OrderType, and current OrderStatus — fulfilling FR-4.
3. **And Given** the Order detail view, **When** reviewed, **Then** it references only `OrderDto`/`OrderItemDto` types — no Domain entities cross into Presentation (AD-12).

## Tasks / Subtasks

- [x] Task 1: Enrich `OrderDto`/`OrderItemDto` with display-only cross-entity fields (AC: #1, #2)
  - [x] `OrderFlow.BLL/OrderDto.cs`: add `public string CustomerName { get; set; } = string.Empty;`.
  - [x] `OrderFlow.BLL/OrderItemDto.cs`: add `public string ProductName { get; set; } = string.Empty;`.
  - [x] **These mirror `ProductDto.StockQuantity`'s existing precedent** (a DTO enriched with a related entity's display data) — **but the population mechanism differs**: `ProductDto.StockQuantity` comes from `Product.Inventory` (a real EF navigation property populated via `.Include()`, Story 1.4). `Order` has **no** `Customer` or `Product` navigation property at all — `OrderConfiguration.cs`'s own comment says so explicitly ("FK only, no nav on either side... mirrors `OrderItem`'s unidirectional relationship with Product"). So `CustomerName`/`ProductName` must be populated by a separate batch lookup in the new `OrderService` (Task 3), not by adding a `.Include()` anywhere.
  - [x] Both new fields default to `string.Empty` and are additive — existing `OrderDto`/`OrderItemDto` construction sites in `StandardOrderProcessor`/`RushOrderProcessor`/`OrderCreateForm`/existing tests do not set them and continue to compile and behave identically (they don't need the names for order *creation*, only for order *viewing*).
- [x] Task 2: Extract `OrderTotalCalculator` and refactor both processors to use it (AC: #1, #2)
  - [x] **This closes Story 2.5's own deferred code-review finding** ("near-total duplication between `StandardOrderProcessor`/`RushOrderProcessor`... revisit if a third processor variant appears (rule-of-three)") — this story is that third call site: `OrderService` (Task 3) needs the exact same "base total, +10% if Rush" calculation to recompute a persisted Order's total for display, since `Order` has no stored `Total` column (only `OrderItems` with `UnitPriceAtOrder` snapshots — Story 2.1's deliberate shape).
  - [x] `OrderFlow.BLL/OrderTotalCalculator.cs` (new, `internal static class` — BLL-internal helper, not part of any public service contract):
    ```csharp
    internal static class OrderTotalCalculator
    {
        private const decimal RushSurchargeRate = 0.10m;

        public static decimal Calculate(OrderType orderType, IPricingStrategy pricingStrategy, IEnumerable<OrderItemDto> items)
        {
            var baseTotal = pricingStrategy.CalculateTotal(items);
            return orderType == OrderType.Rush
                ? Math.Round(baseTotal + baseTotal * RushSurchargeRate, 2, MidpointRounding.AwayFromZero)
                : baseTotal;
        }
    }
    ```
  - [x] `StandardOrderProcessor.cs`/`RushOrderProcessor.cs`: replace Step 2's inline total calculation with `var total = OrderTotalCalculator.Calculate(request.OrderType, _pricingStrategy, request.Items);` in **both** classes (identical call — `StandardOrderProcessor` no longer needs its own "base total unmodified" comment/logic, `RushOrderProcessor` drops its private `RushSurchargeRate` const and rounding logic, both now delegate). **This is a pure behavior-preserving refactor** — same inputs produce the same outputs, so `StandardOrderProcessorTests.cs`/`RushOrderProcessorTests.cs`'s existing total-calculation assertions (`expectedTotal`/independently-computed rounding) must still pass unmodified. Do not change those tests as part of this task.
- [x] Task 3: `IOrderService`/`OrderService` (AC: #1, #2)
  - [x] `OrderFlow.BLL/IOrderService.cs`: `Task<Result<OrderDto>> GetAsync(int id); Task<Result<IReadOnlyList<OrderDto>>> GetAllAsync();` — matches `ICustomerService`/`IProductService`'s naming. **Read-only** — no `CreateAsync`/`UpdateAsync` here; creation goes through `IOrderProcessor` (Story 2.5), status changes go through `IOrderStatusService` (Story 3.3's UI, Story 2.4's service) — `IOrderService` never mutates an `Order`.
  - [x] `OrderFlow.BLL/OrderService.cs`: constructor `(IUnitOfWork unitOfWork, IPricingStrategy pricingStrategy)` — matches `ProductService`'s "depend on `IUnitOfWork` directly, no cross-BLL-service dependency for simple reads" pattern; `IPricingStrategy` is needed for `OrderTotalCalculator.Calculate` (Task 2).
    - `GetAsync(id)`: `_unitOfWork.Orders.GetByIdAsync(id)` (already `.Include(o => o.OrderItems)`, Story 2.1) → `NotFoundError` (`"Order not found"`, matching `OrderStatusService`'s existing constant text) if `null`; else fetch the single `Customer` via `_unitOfWork.Customers.GetByIdAsync(order.CustomerId)` (one extra query — acceptable for a single-order detail fetch) and map to `OrderDto` via a shared `ToDto` helper (below).
    - `GetAllAsync()`: `_unitOfWork.Orders.GetAllAsync()` + **one** `_unitOfWork.Customers.GetAllAsync()` + **one** `_unitOfWork.Products.GetAllAsync()` (batch fetches, not N+1 — build `Dictionary<int, string>` lookups for customer names and product names, then map every order in one pass). Do not fetch Customers/Products once per Order — that would reintroduce the N+1 pattern this codebase already has one deferred instance of (Story 2.5's review); a list screen showing every Order is exactly the case where batching matters most.
    - Shared `ToDto(Order order, string customerName, Func<int, string> productNameLookup)` (or equivalent): maps `OrderItems` to `OrderItemDto` (including the looked-up `ProductName`), computes `Total` via `OrderTotalCalculator.Calculate(order.OrderType, _pricingStrategy, items)`, sets `CustomerName`.
- [x] Task 4: `IOrderListView` + `OrderListPresenter` + `OrderListForm` (AC: #1)
  - [x] `OrderFlow.Presentation/IOrderListView.cs`: `void DisplayOrders(IReadOnlyList<OrderDto> orders); void ShowError(string message);` — matches `IProductListView`'s shape.
  - [x] `OrderFlow.Presentation/OrderListPresenter.cs`: constructor `(IOrderListView, IServiceScopeFactory)`; `Task LoadOrdersAsync()` resolves `IOrderService` in its own scope, `GetAllAsync()`, `DisplayOrders`/`ShowError` — matches `ProductListPresenter` exactly.
  - [x] `OrderFlow.Presentation/OrderListForm.cs` (+ Designer): list-form pattern matching `ProductListForm`/`CustomerListForm` — constructor `(IServiceProvider serviceProvider, IServiceScopeFactory scopeFactory)` (launches `OrderDetailForm`, so needs the root `IServiceProvider` per AD-3's form-launching exception, same as `ProductListForm`). A read-only `DataGridView` (`ReadOnly = true`, `AllowUserToAddRows = false`) bound to `List<OrderDto>` with `AutoGenerateColumns = true`, then hide `Id`, `CustomerId`, and `Items` columns after binding (`Items` is a nested `IReadOnlyList<OrderItemDto>` — `AutoGenerateColumns` would render it via a useless `ToString()`, so it must be hidden alongside `Id` the way `ProductListForm.DisplayProducts` already hides `Id`). A **"View"** button (not "Edit" — this story is read-only) opens `OrderDetailForm` via `_serviceProvider.GetRequiredService<OrderDetailForm>()` + `Initialize(selected.Id)` + `ShowDialog(this)` — **no reload-on-close**, unlike `ProductListForm`'s `OpenDetailFormAsync` (nothing on the list changes from viewing a detail; Story 3.3 is what will need a reload-on-close once status transitions are added here). A **"Refresh"** button. No "Add" button (order creation is `MainForm`'s existing "New Order" button → `OrderCreateForm`, Story 2.5 — do not duplicate it here).
- [x] Task 5: `IOrderDetailView` + `OrderDetailPresenter` + `OrderDetailForm` (AC: #2, #3)
  - [x] `OrderFlow.Presentation/IOrderDetailView.cs`: `void ShowOrder(OrderDto order); void ShowError(string message);` — matches `IProductDetailView`'s shape (satisfies AC #3: only `OrderDto`/`OrderItemDto` cross this boundary).
  - [x] `OrderFlow.Presentation/OrderDetailPresenter.cs`: constructor `(IOrderDetailView, IServiceScopeFactory)`; `Task LoadAsync(int orderId)` resolves `IOrderService` in its own scope, `GetAsync(orderId)`, `ShowOrder`/`ShowError` — matches `ProductDetailPresenter.LoadAsync` exactly.
  - [x] `OrderFlow.Presentation/OrderDetailForm.cs` (+ Designer): **leaf form** — constructor `(IServiceScopeFactory scopeFactory)` only, matching `ProductDetailForm`/`OrderCreateForm`'s leaf-form precedent (never launches another Form). `Initialize(int orderId)` (non-nullable — this form is always viewing an *existing* Order, there is no "create" case here, unlike `ProductDetailForm.Initialize(int? productId)`). `Form_Load` calls `await _presenter.LoadAsync(_orderId)`. Read-only display: Customer name / OrderType / OrderStatus labels, a read-only `DataGridView` bound to `order.Items` (`AutoGenerateColumns = true`, no hidden columns needed — `OrderItemDto` has no nested-collection properties), a Total label, and a single **Close** button (`DialogResult` left at its default `None` — there is nothing to signal back to `OrderListForm`, unlike `ProductDetailForm`'s `DialogResult.OK`/`Cancel` pattern which exists because editing can change list data).
- [x] Task 6: Composition root + navigation wiring (AC: #1)
  - [x] `OrderFlow.Presentation/Program.cs` `ConfigureServices`: `services.AddScoped<IOrderService, OrderService>();` alongside the other BLL services; `services.AddTransient<OrderListForm>(); services.AddTransient<OrderDetailForm>();` alongside the other Transient forms.
  - [x] `OrderFlow.Presentation/MainForm.cs`/`MainForm.Designer.cs`: add a fourth button (`ordersButton`, Text = `"Orders"`, `Point(408, 12)`, `Size(120, 30)`, continuing the existing `customersButton`/`productsButton`/`newOrderButton` row) whose `Click` handler mirrors `CustomersButton_Click`/`ProductsButton_Click` exactly: `var listForm = _serviceProvider.GetRequiredService<OrderListForm>(); listForm.Show(this);` — non-modal `.Show()`, not `.ShowDialog()`, matching the other two list-navigation buttons (only the "New Order" *create* action is modal).
- [x] Task 7: `OrderFlow.Tests` (AC: #1, #2, #3)
  - [x] `OrderFlow.Tests/OrderServiceTests.cs` (new), mocking `IUnitOfWork`/`IPricingStrategy` with Moq:
    - `GetAsync_WithExistingOrder_ReturnsDtoWithCustomerNameAndProductNamesAndComputedTotal`: mock `Orders.GetByIdAsync` returning an `Order` with `OrderItems`, mock `Customers.GetByIdAsync` returning a `Customer`, mock `Products.GetAllAsync` — wait, `GetAsync` needs per-item product names too; either mock `Products.GetByIdAsync` per item (small, bounded item count for a single order — acceptable, not the N+1-across-*orders* concern Task 3 warns about) or reuse the same batch-`GetAllAsync` approach for consistency with `GetAllAsync`. Pick one approach and apply it consistently in `OrderService`; whichever is chosen, assert the resulting `OrderDto.CustomerName`/`Items[].ProductName`/`Total` are correct.
    - `GetAsync_WithMissingOrder_ReturnsFailure`.
    - `GetAllAsync_ReturnsAllOrdersWithNamesAndTotals_UsingBatchLookupsNotPerOrderQueries`: assert `mockUnitOfWork.Verify(u => u.Customers.GetAllAsync(), Times.Once)` (or the repository mock directly) and similarly for Products — proving the batch-not-N+1 design from Task 3 is actually implemented, not just described.
  - [x] `OrderFlow.Tests/OrderTotalCalculatorTests.cs` (new): `Calculate_Standard_ReturnsBaseTotalUnmodified`, `Calculate_Rush_AppliesTenPercentSurchargeRounded` — port the exact assertions `StandardOrderProcessorTests`/`RushOrderProcessorTests` already made about total calculation, now against the extracted helper directly.
  - [x] Confirm `StandardOrderProcessorTests.cs`/`RushOrderProcessorTests.cs` **still pass unmodified** after Task 2's refactor (no test file changes expected — this is the point of a behavior-preserving extraction).
- [x] Task 8: `OrderFlow.Presentation.Tests` (AC: #1, #2)
  - [x] `OrderListPresenterTests.cs` (new): `LoadOrdersAsync_Success_DisplaysOrders` / `LoadOrdersAsync_Failure_ShowsError`, mirroring `ProductListPresenterTests.cs` via `MockScopeHelper.CreateMockScope<IOrderService>()`.
  - [x] `OrderDetailPresenterTests.cs` (new): `LoadAsync_WithExistingId_ShowsOrderAndReturnsTrue`-equivalent / `LoadAsync_WithMissingId_ShowsErrorAndReturnsFalse`-equivalent — note `OrderDetailPresenter.LoadAsync` returns `Task`, not `Task<bool>` like `ProductDetailPresenter.LoadAsync` (`ProductDetailForm` needs the `bool` to decide whether to proceed with editing; `OrderDetailForm` has nothing conditional to do after loading — just display or error), so adjust assertions accordingly (verify `ShowOrder`/`ShowError` calls, don't assert a return value).
- [x] Task 9: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 8 projects) — 0 errors, 0 warnings.
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` passes, including all new tests, and every pre-existing test still passes (especially `StandardOrderProcessorTests`/`RushOrderProcessorTests`, which must be unaffected by Task 2's refactor).
  - [x] Confirm `OrderFlow.Domain`/`OrderFlow.DAL` are untouched — this story is `OrderFlow.BLL` + `OrderFlow.Presentation` + both test projects only (same footprint shape as Story 2.5).

### Review Findings

- [x] [Review][Patch] `OrderService.GetAsync` fetches the entire `Products` table (`_unitOfWork.Products.GetAllAsync()`) just to resolve names for one order's handful of line items — scales with total catalog size, not order size, unlike `GetAllAsync`'s correctly-batched-once lookup. Fix: resolve only the distinct `ProductId`s referenced in `order.OrderItems` (bounded, small loop via `Products.GetByIdAsync` per distinct id — matches the established small-bounded-loop pattern from Story 2.5's stock-check loop). Update `OrderServiceTests.GetAsync_...` to assert `Products.GetAllAsync()` is never called. [`OrderFlow/OrderFlow.BLL/OrderService.cs`]
- [x] [Review][Patch] Dev Notes/Completion Notes overclaim: "closes Story 2.5's own deferred code-review finding about that duplication" implies the whole finding is resolved, but only the total-calculation slice (Step 2) was extracted — Steps 1, 3, and 4 (stock check, persist, decrement) remain fully duplicated between `StandardOrderProcessor`/`RushOrderProcessor`, untouched by this story. Fix: reword to precisely scope what was closed (the total-calculation portion only); Story 2.5's original deferred-work.md entry for the remaining duplication still stands unchanged. [`_bmad-output/implementation-artifacts/3-2-order-list-detail-view.md`]
- [x] [Review][Patch] `OrderListForm` doesn't disable `viewButton` during `RefreshButton_Click`, so a refresh started immediately before opening a detail dialog can have its async continuation reset `dataGridView.DataSource` (and column visibility) while `OrderDetailForm` is open modally on top of it — `ShowDialog` pumps a nested message loop, so this is reachable via ordinary double-click timing, not exotic concurrency. Fix: also disable/restore `viewButton` alongside `refreshButton` in `RefreshButton_Click`, matching the disable-during-async-operation pattern already used elsewhere in this codebase. [`OrderFlow/OrderFlow.Presentation/OrderListForm.cs`]
- [x] [Review][Defer] `order.Total.ToString("C")` uses the thread's current culture with no pinned format — on a non-USD-culture machine this renders in an unexpected currency symbol/format [`OrderFlow/OrderFlow.Presentation/OrderDetailForm.cs`] — deferred, UX polish not required by any AC; no established culture-pinning convention exists anywhere in this codebase to follow.
- [x] [Review][Defer] `StandardOrderProcessor`/`RushOrderProcessor`'s Step 1 has no validation rejecting an empty `Items` list or a zero/negative `Quantity` line item [`OrderFlow/OrderFlow.BLL/StandardOrderProcessor.cs`, `OrderFlow/OrderFlow.BLL/RushOrderProcessor.cs`] — deferred, pre-existing since Story 2.5 (unmodified by this story), and unreachable via the only existing caller (`OrderCreateForm` already guards both cases: an empty-list check before calling Confirm, and `quantityNumericUpDown.Minimum = 1`).
- Dismissed as noise / matches documented spec intent / already adjudicated in Story 2.5's own review / matches established convention / already tracked in `deferred-work.md` / unreachable (17): `OrderTotalCalculator` being `public` rather than the story text's literal `internal` (justified and necessary — `OrderTotalCalculatorTests.cs` calls it from a separate test assembly with no `InternalsVisibleTo` plumbing in this codebase); `OrderDetailForm` hiding the `ProductId` column despite the story text saying "no hidden columns needed" (a UX improvement, not a defect, consistent with `OrderListForm` hiding its own redundant FK columns); no pagination on `GetAllAsync` (matches every other list screen in this codebase, out of scope per the PRD's demo-app framing); total recomputing against whichever `IPricingStrategy` is currently registered rather than being pinned to order history (explicitly documented as an accepted characteristic in this story's own Dev Notes, not a bug); repository exceptions uncaught in `OrderService`'s read methods (matches the architecture's Consistency Convention that infrastructure exceptions surface via the global handler, not per-call try/catch — no read method anywhere in this codebase, including `ProductService`/`CustomerService`, wraps its repository calls either); a missing/deleted `Customer` silently rendering as an empty name (unreachable — no delete feature exists for `Customer` anywhere in this app); `OrderDetailForm.Initialize(int orderId)`'s two-phase-init pattern (matches `ProductDetailForm.Initialize`'s identical established precedent, Story 1.3); no loading indicator on either new screen (already tracked in `deferred-work.md` since Story 1.3/1.5); `OrderListForm`/`OrderDetailForm`'s code-behind having no direct unit tests (already adjudicated in Story 2.5's own review — matches this codebase's established Form-vs-Presenter test boundary); no `CancellationToken` anywhere in the new read path (already tracked in `deferred-work.md` since Story 1.2, codebase-wide); `OrdersButton_Click` allowing duplicate `OrderListForm` windows (pre-existing codebase-wide pattern already present for Customers/Products since Story 1.3, not introduced or worsened here); `OrderTotalCalculator.Calculate` taking an `IPricingStrategy` parameter being "speculative generality" (incorrect — this is the correct application of AD-11's Strategy pattern; removing it would mean hardcoding pricing logic inline, undermining the architecture); intermediate rounding correctness not re-verified end-to-end (pre-existing, already reasoned through in Story 2.2's Dev Notes — `UnitPriceAtOrder` is already currency-precision, so `Quantity × UnitPriceAtOrder` introduces no extra decimal places); `Order.OrderItems` accessed with no null-check (unreachable — the Domain entity defaults it to an empty collection and EF's `.Include()` never returns null); an unrecognized `OrderType` silently pricing as Standard (unreachable — `OrderType` is a pinned two-value set, Story 2.1, and only ever reaches this code via the two registered processors); `refreshButton` not disabled during `OrderListForm`'s *initial* load (already tracked in `deferred-work.md` since Story 1.5's identical `ProductListForm_Load` finding); the initial `SaveChangesAsync` in both processors not wrapped in try/catch (already adjudicated in Story 2.5's own review, code unchanged by this story).

## Dev Notes

- **`IOrderService` is new — there was no read-service for `Order` before this story.** `IOrderProcessor` (Story 2.3/2.5) is confirm-only; `IOrderStatusService` (Story 2.4) is transition-only. Neither reads/lists Orders for display. Do not add `GetAsync`/`GetAllAsync` to either of those — `IOrderService` is a new, separate, read-only BLL service, matching the shape (not the CRUD completeness) of `ICustomerService`/`IProductService`.
- **`Order` has no stored `Total` and no `Customer`/`Product` navigation properties — both are deliberate Story 2.1/2.5 decisions, not gaps this story is filling.** Total is recomputed at read time from `OrderItems`' `UnitPriceAtOrder` snapshots via the same calculation Story 2.2/2.3 established (base total, +10% if Rush) — now shared via `OrderTotalCalculator` (Task 2) rather than duplicated a third time. `CustomerName`/`ProductName` are populated via separate batch lookups (Task 3), not EF navigation — `OrderConfiguration.cs` explicitly chose no navigation properties on either side of the `Order`↔`Customer` and `OrderItem`↔`Product` relationships.
- **A subtle, accepted characteristic worth knowing (not a bug to fix here): total is recomputed against whichever `IPricingStrategy` is *currently* registered, not necessarily the one active when the Order was originally confirmed.** AD-11 pins exactly one `IPricingStrategy` implementation at a time with no keyed dispatch, and this project has only ever built `StandardPricingStrategy` (Story 2.2) — so in practice this can never diverge today. If a future story ever swaps the registered strategy, historical Orders' displayed totals would recompute under the new strategy rather than showing what was actually charged. That would be a real product decision (store `Total` on `Order` vs. accept recomputation) for whoever adds a second strategy — not something to speculatively solve now.
- **`OrderListForm` is a list-launcher form** (needs root `IServiceProvider` to launch `OrderDetailForm`, per AD-3's form-launching exception) — matches `ProductListForm`/`CustomerListForm`. **`OrderDetailForm` is a leaf form** (`IServiceScopeFactory` only) — matches `ProductDetailForm`/`OrderCreateForm`. This is the same split every prior UI story has followed; see Story 1.3 Dev Notes for the original rationale.
- **No status-transition actions in `OrderDetailForm` yet.** This story is view-only (AC #2 only asks for line items/total/OrderType/current status to be *shown*). Story 3.3 ("Order Status Transition UI") is what adds action buttons here calling `IOrderStatusService.TransitionTo` — don't build that ahead of schedule.
- **`OrderListForm` has no "Add" button.** Order creation already exists (`MainForm`'s "New Order" button → `OrderCreateForm`, Story 2.5) — adding a second creation entry point on this list would duplicate it. `OrderListForm` only *views* what already exists, plus Refresh.
- **AD-12 compliance**: `IOrderListView`/`IOrderDetailView`/their Presenters only ever see `OrderDto`/`OrderItemDto` — never `OrderFlow.Domain.Order`/`OrderItem`. Mapping happens entirely inside `OrderService`.
- **Naming/DTO conventions unchanged**: `IXxx` interfaces, `XxxPresenter`, `IXxxView`, `XxxService`/`IXxxService`. `CustomerName`/`ProductName` are plain `string` properties (default `string.Empty`, matching `CustomerDto`/`ProductDto`'s existing non-nullable-string convention) added to the existing DTOs, not new DTO types.
- **Trust internal callers convention continues** for `OrderService` — no defensive validation beyond the not-found checks the ACs require.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.BLL/
    OrderDto.cs                        # modified: + CustomerName
    OrderItemDto.cs                    # modified: + ProductName
    OrderTotalCalculator.cs            # new
    StandardOrderProcessor.cs          # modified: Step 2 delegates to OrderTotalCalculator
    RushOrderProcessor.cs              # modified: same, drops its own surcharge/rounding logic
    IOrderService.cs                   # new
    OrderService.cs                    # new
  OrderFlow.Presentation/
    IOrderListView.cs                  # new
    OrderListPresenter.cs              # new
    OrderListForm.cs                   # new
    OrderListForm.Designer.cs          # new
    IOrderDetailView.cs                # new
    OrderDetailPresenter.cs            # new
    OrderDetailForm.cs                 # new
    OrderDetailForm.Designer.cs        # new
    MainForm.cs                        # modified: + OrdersButton_Click
    MainForm.Designer.cs               # modified: + ordersButton
    Program.cs                         # modified: + AddScoped<IOrderService>, + AddTransient<OrderListForm>/<OrderDetailForm>
  OrderFlow.Tests/
    OrderServiceTests.cs                # new
    OrderTotalCalculatorTests.cs        # new
    StandardOrderProcessorTests.cs      # unchanged (behavior-preserving refactor verified, not modified)
    RushOrderProcessorTests.cs          # unchanged (same)
  OrderFlow.Presentation.Tests/
    OrderListPresenterTests.cs          # new
    OrderDetailPresenterTests.cs        # new
```

`OrderFlow.Domain`/`OrderFlow.DAL` are untouched by this story.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 3.2: Order List & Detail View] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 3: Order Lifecycle Visibility & Notifications] — FR-4 coverage
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-3 — Presentation: constructor-injected Presenter + per-screen IView] — list-launcher (`IServiceProvider`) vs. leaf-form (`IServiceScopeFactory`-only) split
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-12 — Domain entities never cross the BLL→Presentation boundary] — AC #3's origin
- [Source: _bmad-output/implementation-artifacts/2-5-order-creation-confirmation-ui.md#Review Findings] — "Near-total duplication between StandardOrderProcessor/RushOrderProcessor... deferred, pre-existing pattern (rule-of-three not yet met...)" — the finding this story's Task 2 closes
- [Source: _bmad-output/implementation-artifacts/2-1-order-orderitem-domain-repository.md] — `Order`'s deliberate no-stored-Total, no-Customer-navigation shape; `OrderItem.UnitPriceAtOrder`'s snapshot-at-add-time semantics
- [Source: _bmad-output/implementation-artifacts/1-4-product-inventory-domain-repository-service.md] — `ProductDto.StockQuantity`'s cross-entity-enrichment precedent (via EF navigation, contrasted with this story's batch-lookup approach since Order has no equivalent navigation)
- [Source: _bmad-output/implementation-artifacts/1-3-customer-management-ui.md] — `ProductListForm`/`ProductDetailForm`'s list-launcher vs. leaf-form Presentation split this story's `OrderListForm`/`OrderDetailForm` follow

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.BLL/OrderFlow.BLL.csproj`: Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests --filter "StandardOrderProcessorTests|RushOrderProcessorTests"` (after Task 2 refactor): Passed! 12/12 — confirmed the `OrderTotalCalculator` extraction is behavior-preserving.
- `dotnet build OrderFlow.Presentation/OrderFlow.Presentation.csproj`: Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet build OrderFlow.Tests/OrderFlow.Tests.csproj` + `dotnet test`: Passed! Failed: 0, Passed: 75, Skipped: 0, Total: 75.
- `dotnet build OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj`: Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet build OrderFlow.sln` (all 8 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` (final): Passed! Failed: 0, Passed: 75, Skipped: 0, Total: 75 (70 prior + 5 new: 3 `OrderServiceTests`, 2 `OrderTotalCalculatorTests`).
- `dotnet test OrderFlow.Presentation.Tests/OrderFlow.Presentation.Tests.csproj`: build succeeded but test host failed to launch on macOS (missing `Microsoft.WindowsDesktop.App` runtime) — same pre-existing, already-accepted **UNVERIFIED-ENVIRONMENT** gap as every prior UI story (2.5, 3.1). The 4 new `OrderListPresenterTests`/`OrderDetailPresenterTests` compile and are structurally correct but have not executed on this dev machine.

### Completion Notes List

- `OrderDto`/`OrderItemDto` (`OrderFlow.BLL`) gained `CustomerName`/`ProductName` — display-only fields populated by the new `OrderService`, not by EF navigation (`Order` has none, per Story 2.1's deliberate unidirectional-FK design).
- `OrderTotalCalculator` (new, public static helper) extracted the "base total, +10% if Rush" calculation that was duplicated between `StandardOrderProcessor`/`RushOrderProcessor`; both processors now delegate to it. **This closes only the total-calculation slice of Story 2.5's deferred duplication finding** — triggered by `OrderService` becoming a third call site needing that specific logic. Steps 1/3/4 (stock check, persist, decrement) remain fully duplicated between the two processors, untouched by this story; Story 2.5's original deferred-work.md entry for that remaining duplication still stands. Verified behavior-preserving: `StandardOrderProcessorTests`/`RushOrderProcessorTests` pass unmodified.
- `IOrderService`/`OrderService` (new, `OrderFlow.BLL`) — read-only (`GetAsync`/`GetAllAsync`). `GetAllAsync` batch-fetches `Customers`/`Products` once (not per-Order) to avoid an N+1 pattern; `GetAsync` fetches the single `Customer` plus only the distinct `Product`s referenced by that order's own line items (not the whole catalog — fixed during code review, see Review Findings). Total is recomputed per Order via `OrderTotalCalculator` since `Order` has no stored `Total` column.
- `IOrderListView`/`OrderListPresenter`/`OrderListForm` added — list-launcher form (needs root `IServiceProvider` to open `OrderDetailForm`, matching `ProductListForm`/`CustomerListForm`). Grid hides `Id`/`CustomerId`/`Items` columns. "View" (not "Add"/"Edit" — read-only) + "Refresh" buttons only; order creation stays on `MainForm`'s existing "New Order" button.
- `IOrderDetailView`/`OrderDetailPresenter`/`OrderDetailForm` added — leaf form (`IServiceScopeFactory` only, matching `ProductDetailForm`/`OrderCreateForm`). Read-only labels (Customer/OrderType/Status/Total) + a read-only line-items grid + a single Close button. `OrderDetailPresenter.LoadAsync` returns `Task`, not `Task<bool>` (nothing conditional happens after loading, unlike `ProductDetailPresenter`). Satisfies AC #3 — only `OrderDto`/`OrderItemDto` cross into this Form/Presenter.
- `MainForm` gained a fourth button ("Orders") launching `OrderListForm` via non-modal `.Show()`, matching `CustomersButton_Click`/`ProductsButton_Click`. `Program.cs` registers `IOrderService` (Scoped) and both new forms (Transient).
- `dotnet build` is green across all 8 projects with 0 warnings. `OrderFlow.Tests` is 75/75 passing. `OrderFlow.Presentation.Tests` builds clean but cannot execute on this macOS dev machine (missing Windows Desktop runtime).
- No `OrderFlow.Domain`/`OrderFlow.DAL` file touched — confirmed via File List below; this story is `OrderFlow.BLL` + `OrderFlow.Presentation` + both test projects only.

### File List

- `OrderFlow/OrderFlow.BLL/OrderDto.cs` (modified: + `CustomerName`)
- `OrderFlow/OrderFlow.BLL/OrderItemDto.cs` (modified: + `ProductName`)
- `OrderFlow/OrderFlow.BLL/OrderTotalCalculator.cs` (new)
- `OrderFlow/OrderFlow.BLL/StandardOrderProcessor.cs` (modified: Step 2 delegates to `OrderTotalCalculator`)
- `OrderFlow/OrderFlow.BLL/RushOrderProcessor.cs` (modified: same, dropped its own surcharge/rounding logic)
- `OrderFlow/OrderFlow.BLL/IOrderService.cs` (new)
- `OrderFlow/OrderFlow.BLL/OrderService.cs` (new; modified during code review: `GetAsync` looks up only the products the order references instead of the whole catalog)
- `OrderFlow/OrderFlow.Presentation/IOrderListView.cs` (new)
- `OrderFlow/OrderFlow.Presentation/OrderListPresenter.cs` (new)
- `OrderFlow/OrderFlow.Presentation/OrderListForm.cs` (new; modified during code review: `viewButton` disabled alongside `refreshButton` during Refresh)
- `OrderFlow/OrderFlow.Presentation/OrderListForm.Designer.cs` (new)
- `OrderFlow/OrderFlow.Presentation/IOrderDetailView.cs` (new)
- `OrderFlow/OrderFlow.Presentation/OrderDetailPresenter.cs` (new)
- `OrderFlow/OrderFlow.Presentation/OrderDetailForm.cs` (new)
- `OrderFlow/OrderFlow.Presentation/OrderDetailForm.Designer.cs` (new)
- `OrderFlow/OrderFlow.Presentation/MainForm.cs` (modified: + `OrdersButton_Click`)
- `OrderFlow/OrderFlow.Presentation/MainForm.Designer.cs` (modified: + `ordersButton`)
- `OrderFlow/OrderFlow.Presentation/Program.cs` (modified: + `AddScoped<IOrderService>`, + `AddTransient<OrderListForm>`/`<OrderDetailForm>`)
- `OrderFlow/OrderFlow.Tests/OrderServiceTests.cs` (new; modified during code review: `GetAsync` test updated to assert per-product lookups, not `Products.GetAllAsync()`)
- `OrderFlow/OrderFlow.Tests/OrderTotalCalculatorTests.cs` (new)
- `OrderFlow/OrderFlow.Presentation.Tests/OrderListPresenterTests.cs` (new)
- `OrderFlow/OrderFlow.Presentation.Tests/OrderDetailPresenterTests.cs` (new)

## Change Log

- 2026-08-10: Implemented Story 3.2 — `OrderDto`/`OrderItemDto` enriched with `CustomerName`/`ProductName`; `OrderTotalCalculator` extracted and both processors refactored to use it (closing the total-calculation slice of Story 2.5's deferred duplication finding — Steps 1/3/4 remain duplicated, untouched); new read-only `IOrderService`/`OrderService` with batched Customer/Product lookups; `OrderListForm`/`OrderDetailForm` UI added; `MainForm`/`Program.cs` wired up. `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 75/75 passed (70 prior + 5 new).
- 2026-08-10: Code review applied — 3 patches, 2 deferred, 17 dismissed. Real findings: `OrderService.GetAsync` fetched the entire `Products` table just to resolve names for one order's line items — fixed to look up only the distinct `ProductId`s that order actually references; the Completion Notes/Change Log overclaimed that the `OrderTotalCalculator` extraction "closes" Story 2.5's duplication finding — reworded to precisely scope it as the total-calculation slice only (Steps 1/3/4 still duplicated); `OrderListForm` didn't disable "View" during Refresh, allowing a refresh's async continuation to reset the grid under an open modal detail dialog — fixed by disabling/restoring `viewButton` alongside `refreshButton`. Two findings deferred to `deferred-work.md` (culture-dependent currency formatting, pre-existing unvalidated empty/negative-quantity order items). Seventeen findings dismissed: several matched this story's own explicitly-documented accepted characteristics (total recomputing against the current pricing strategy) or already-adjudicated Story 2.5 review decisions (unguarded initial `SaveChangesAsync`, untested Form code-behind, two-phase `Initialize`); others matched already-tracked `deferred-work.md` entries (no loading indicator, no `CancellationToken`, refresh-button race on initial load) or the architecture's documented global-handler convention for read-path exceptions; a few were unreachable (no `Customer` delete feature, `OrderType`'s pinned two-value set) or mischaracterized correct architecture usage (`IPricingStrategy` as a parameter is AD-11's Strategy pattern working as intended, not speculative generality). `dotnet build`/`dotnet test` re-verified green (0 warnings) after all changes.
