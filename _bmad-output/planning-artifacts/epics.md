---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - '_bmad-output/planning-artifacts/prds/prd-BMAD YT TUTORIAL-2026-08-06/prd.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md'
---

# OrderFlow Desktop - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for OrderFlow Desktop, decomposing the requirements from the PRD and Architecture Spine into implementable stories. No UX design contract exists for this project (single-operator WinForms desktop app; UX phase was skipped by design).

## Requirements Inventory

### Functional Requirements

FR-1: User can create, view, edit, and list Customers via WinForms grid/detail forms.
FR-2: User can create, view, edit, and list Products (name, SKU, unit price, stock quantity) via WinForms grid/detail forms.
FR-3: User can create a new Order for a selected Customer, adding one or more OrderItems (Product + quantity) via a line-item grid, selecting an OrderType (Standard or Rush) at creation.
FR-4: User can view a list of Orders and open an Order's detail, showing line items, computed total (post pricing/discount), OrderType, and current OrderStatus.
FR-5: System decrements Inventory stock quantity for each Product line item when an Order is confirmed; rejects confirmation with a validation message (no partial decrement) if requested quantity exceeds current stock.
FR-6: User can view current Inventory stock levels per Product.
FR-7: System calculates an Order's total by applying a configurable, swappable Pricing/Discount Strategy over its OrderItems.
FR-8: User can transition an Order through its OrderStatus sequence from the Order detail view; invalid transitions are blocked.
FR-9: System raises a Notification whenever an Order's OrderStatus changes, visible to the user as confirmation the event fired.
FR-10: All BLL and DAL dependencies are constructor-injected from a single composition root; WinForms forms contain no business logic and depend only on injected BLL interfaces via a Presenter.
FR-11: Data access is implemented via per-entity Repository interfaces coordinated by a Unit of Work managing EF Core transactions.
FR-12: The codebase includes at least 2-3 explicit Before/After Exhibit pairs (SOLID-violating vs. refactored), independently viewable and runnable.
FR-13: The project ships and maintains `docs/interview-topic-map.md`, mapping every named interview topic to the specific class/file demonstrating it.
FR-14: A companion xUnit test project exercises BLL behavior against mocked DAL/BLL interfaces (e.g. `IOrderRepository`, `IPricingStrategy`).
FR-15: System selects the applicable order-processing behavior via an Order Processor Factory keyed on the Order's OrderType (Standard vs. Rush).

### NonFunctional Requirements

NFR1: Order confirmation (FR-5, FR-7, FR-9) runs asynchronously; the UI thread remains responsive and does not freeze during the database round trip, so a live screen-share demo of the golden-path order flow isn't interrupted by a hung form. (PRD §4.2 Feature-specific NFR; enforced structurally by Architecture AD-3's async-all-the-way Presenter rule.)
NFR2: Basic optimistic concurrency — two near-simultaneous updates to the same entity (e.g. Inventory stock during near-simultaneous Order confirmations) must not silently overwrite each other. (PRD §5 Non-Goals boundary; implemented via Architecture AD-10's RowVersion token convention.)

### Additional Requirements

**No starter/scaffolding template specified.** This is a hand-scaffolded multi-project .NET solution, not a `dotnet new` template — Epic 1 Story 1 must create the six-project solution structure directly per the Architecture Spine's Structural Seed:
- `OrderFlow.Presentation` (WinForms Forms + IView interfaces + Presenters; composition root / `Program.cs`)
- `OrderFlow.BLL` (services, strategies, `OrderProcessorFactory`)
- `OrderFlow.DAL` (`AppDbContext`, EF entity configs, Repository + UnitOfWork)
- `OrderFlow.Domain` (entities, `OrderType`/`OrderStatus` enums, `IAuditable`)
- `OrderFlow.Exhibits` (Before/ and After/ SOLID exhibit pairs — standalone, no runtime reference)
- `OrderFlow.Tests` (xUnit v3, mocks BLL/DAL interfaces)

**Stack (pinned, verified 2026-08-06):** .NET 10 (LTS) · WinForms via `Microsoft.NET.Sdk.WindowsDesktop` · `Microsoft.EntityFrameworkCore`/`.SqlServer` 10.0.x · `Microsoft.Extensions.DependencyInjection` (ships with SDK; keyed-services API) · `xunit.v3` 3.2.2 · SQL Server LocalDB.

**Persistence & transaction boundary (AD-2, AD-9):** `AppDbContext` resolved only via a singleton `IDbContextFactory<AppDbContext>`; only `IUnitOfWork` calls `CreateDbContextAsync()`, once per business operation, and exposes repository properties (`.Orders`, `.Customers`, `.Products`, `.Inventory`) backed by that single instance — repositories are never independently DI-registered.

**Presentation pattern (AD-3, AD-12):** every Form implements a screen-specific `IXxxView`; a constructor-injected `XxxPresenter` (given an `IServiceScopeFactory`, not long-lived services) opens one `IServiceScope` per user-initiated action, calls BLL asynchronously, and disposes the scope at the end of that action. BLL methods called from Presentation accept/return `XxxDto` types only — Domain entities never cross that boundary (Presentation may reference Domain only for enums like `OrderType`/`OrderStatus` used in UI binding).

**DI lifetimes (AD-5, AD-7):** a "business operation" = one Presenter-method invocation. Everything resolves Scoped-per-operation except `IAppSettings` and `INotifier`, which are the only Singletons. `OrderProcessorFactory` is registered Scoped (not Singleton, despite the name) and resolves keyed `IOrderProcessor` implementations (`AddKeyedScoped<IOrderProcessor>(OrderType, ...)`) from the ambient scoped provider.

**Order status + notifications (AD-4):** `OrderStatusService.TransitionTo(orderId, newStatus)` is the sole owner of the allowed-transition table (partitioned by `OrderType`) and the only caller of `INotifier.Notify(...)`, fired only after the Unit of Work commits. Notification payload is a dedicated `OrderStatusChangedNotification { OrderId, OldStatus, NewStatus }` DTO.

**Pricing Strategy wiring (AD-11):** exactly one `IPricingStrategy` implementation registered Scoped at the composition root at a time — no keyed dispatch (unlike the Order Processor Factory).

**Inventory ownership (AD-13):** `IInventoryService.HasSufficientStock(...)` is the sole method evaluating stock sufficiency; both Order confirmation (FR-3) and the actual decrement (FR-5) call it rather than reimplementing the check.

**Auditing (AD-6):** every Domain entity implements `IAuditable` (`CreatedAt`, `UpdatedAt`), stamped via a `SaveChanges` override that only sets `CreatedAt` on `EntityState.Added`; repositories update via targeted property changes, never a blanket `Update()`.

**Error handling & logging (Consistency Conventions):** BLL validation failures return `Result<T>`, never exceptions; infrastructure exceptions surface through one global handler at the composition root, not per-Presenter try/catch; logging via `Microsoft.Extensions.Logging`/`ILogger<T>`, constructor-injected, no static loggers.

**Naming conventions:** `IXxx` interfaces; `IXxxRepository`/`XxxRepository`; `IXxxService`/`XxxService`; `XxxPresenter`; `IXxxView`; `XxxDto`; `XxxConfiguration : IEntityTypeConfiguration<Xxx>`.

**Explicitly out of scope for this epic/story breakdown (Architecture Deferred, mirrors PRD Non-Goals):** deployment/installer packaging, CI/CD pipeline, hosting/infra topology, authentication/authorization, reporting/export, web or API surface, multi-tenant/multi-currency support.

### UX Design Requirements

Not applicable — no UX design contract exists for this project.

### FR Coverage Map

```text
FR-1:  Epic 1 - Customer CRUD
FR-2:  Epic 1 - Product CRUD
FR-3:  Epic 2 - Order creation w/ OrderItems + OrderType
FR-4:  Epic 3 - Order list/detail view
FR-5:  Epic 2 - Inventory decrement on confirm
FR-6:  Epic 1 - View Inventory levels (Product Management UI)
FR-7:  Epic 2 - Pricing/Discount Strategy total calc
FR-8:  Epic 3 - OrderStatus transitions
FR-9:  Epic 3 - Notification on status change
FR-10: Epic 1 - DI composition root, Presenter pattern
FR-11: Epic 1 - Repository + UnitOfWork + EF Core
FR-12: Epic 4 - Before/After SOLID exhibits
FR-13: Epic 4 - interview-topic-map.md
FR-14: Epic 1 - xUnit test project scaffold
FR-15: Epic 2 - Order Processor Factory
NFR-1: Epic 2 - Async order confirmation
NFR-2: Epic 2 - Optimistic concurrency (RowVersion)
```

## Epic List

### Epic 1: Foundation & Customer/Product Management

Stand up the six-project solution (composition root, DI, Repository+UnitOfWork+EF Core DAL, Presenter/IView pattern, xUnit test project) and deliver the first real user value: manage Customers and Products via WinForms CRUD. Also anchors the cross-cutting conventions (persistence/transaction boundary, presentation pattern, DI lifetimes, auditing, error handling/logging, naming) that later epics simply follow.
**FRs covered:** FR-1, FR-2, FR-6, FR-10, FR-11, FR-14

### Epic 2: Order Creation, Pricing & Inventory

Users can create Orders for a Customer, pick an OrderType, add OrderItems, see the total computed via a swappable Pricing Strategy, and have Inventory checked/decremented safely at confirmation (with optimistic concurrency protection, async so the UI doesn't hang).
**FRs covered:** FR-3, FR-5, FR-7, FR-15, NFR-1, NFR-2

### Epic 3: Order Lifecycle Visibility & Notifications

Users can view the Orders list/detail (line items, total, status), transition an Order through its status workflow (invalid transitions blocked), and see a Notification fire on each status change.
**FRs covered:** FR-4, FR-8, FR-9

### Epic 4: Architecture Teaching Exhibits & Interview Documentation

The tutorial's teaching deliverable: standalone Before/After SOLID exhibit pairs, plus a maintained `docs/interview-topic-map.md` mapping every named interview topic to the class/file that demonstrates it.
**FRs covered:** FR-12, FR-13

## Epic 1: Foundation & Customer/Product Management

Stand up the six-project solution (composition root, DI, Repository+UnitOfWork+EF Core DAL, Presenter/IView pattern, xUnit test project) and deliver the first real user value: manage Customers and Products via WinForms CRUD. Also anchors the cross-cutting conventions (persistence/transaction boundary, presentation pattern, DI lifetimes, auditing, error handling/logging, naming) that later epics simply follow.

**FRs covered:** FR-1, FR-2, FR-6, FR-10, FR-11, FR-14

### Story 1.1: Solution Scaffold & Composition Root

As a developer,
I want the six-project OrderFlow solution scaffolded with a working DI composition root,
So that every later story has a consistent structure and dependency graph to build into.

**Acceptance Criteria:**

**Given** no existing solution
**When** the story is complete
**Then** `OrderFlow.sln` exists containing six projects — `OrderFlow.Presentation` (WinForms, `Microsoft.NET.Sdk.WindowsDesktop`), `OrderFlow.BLL`, `OrderFlow.DAL`, `OrderFlow.Domain`, `OrderFlow.Exhibits`, `OrderFlow.Tests` (xunit.v3) — each referencing only the layers permitted by AD-1 (Presentation→BLL→DAL→Domain; Domain has no outward refs; Exhibits stands alone per AD-8)

**And Given** the solution
**When** `OrderFlow.Presentation` starts
**Then** `Program.cs` is the sole composition root: builds an `IServiceProvider`, registers an empty `AppDbContext` (no `DbSet`s yet) behind a singleton `IDbContextFactory<AppDbContext>` (AD-2) pointed at SQL Server LocalDB, and launches a minimal `MainForm` shell with no business logic — proving the DI graph boots end-to-end

**And Given** `OrderFlow.Tests`
**When** the solution builds
**Then** it references `OrderFlow.BLL`/`OrderFlow.Domain` only (not `OrderFlow.DAL` directly, per AD-9 mockability) and contains one placeholder passing test

**And Given** `OrderFlow.Domain`
**When** reviewed
**Then** it contains `IAuditable` (`CreatedAt`, `UpdatedAt`) and the `OrderType`/`OrderStatus` enums (values populated when Order stories need them)

### Story 1.2: Customer Domain, Repository & Service

As a developer,
I want a Customer entity, repository, and service wired through the Unit of Work,
So that Customer data can be persisted per the architecture's DAL/BLL conventions.

**Acceptance Criteria:**

**Given** `OrderFlow.Domain`
**When** complete
**Then** a `Customer` entity (Id, Name, Email, Phone, `IAuditable`) exists with `CustomerConfiguration : IEntityTypeConfiguration<Customer>`, and a migration adds the `Customers` table

**And Given** `OrderFlow.DAL`
**When** implemented
**Then** `ICustomerRepository`/`CustomerRepository` is exposed only via `IUnitOfWork.Customers` (AD-9), and `SaveChanges` stamps `CreatedAt` only on `EntityState.Added` (AD-6)

**And Given** `OrderFlow.BLL`
**When** implemented
**Then** `ICustomerService`/`CustomerService` exposes async Create/Get/GetAll/Update over `CustomerDto` (never the entity, AD-12), validates required fields, and returns `Result<T>` on failure

**And Given** `OrderFlow.Tests`
**When** complete
**Then** `CustomerService` is covered with a mocked `IUnitOfWork`, including a success path and a validation-failure path

### Story 1.3: Customer Management UI

As a user,
I want to create, view, edit, and list Customers from the desktop app,
So that I can manage the customers I take orders for.

**Acceptance Criteria:**

**Given** the app is running
**When** I open the Customer list
**Then** `CustomerListForm` (`ICustomerListView`) displays all Customers via `CustomerListPresenter`, which opens one `IServiceScope` per action (AD-3) and calls `ICustomerService` asynchronously without blocking the UI

**And Given** the list
**When** I create or edit a Customer via `CustomerDetailForm`/`CustomerDetailPresenter`
**Then** valid submission persists the change and returns to the refreshed list

**And Given** invalid input (e.g. missing Name/Email)
**When** I submit
**Then** the `Result<T>` failure message is surfaced on the form without a crash

**And Given** the Presentation project
**When** reviewed
**Then** Customer forms reference only `CustomerDto` and the injected service/Presenter — no `OrderFlow.DAL`/`Domain` types — satisfying FR-10

### Story 1.4: Product & Inventory Domain, Repository & Service

As a developer,
I want Product and Inventory entities, repositories, and services wired through the Unit of Work,
So that catalog and stock-level data can be persisted per the architecture's 1:1 Product↔Inventory model.

**Acceptance Criteria:**

**Given** `OrderFlow.Domain`
**When** complete
**Then** a `Product` entity (Id, Name, SKU, UnitPrice, `IAuditable`) and an `Inventory` entity (Id, ProductId FK 1:1, StockQuantity, `RowVersion` per AD-10) exist, with configs in `OrderFlow.DAL`, and a migration adds both tables with the 1:1 relationship

**And Given** `OrderFlow.DAL`
**When** implemented
**Then** `IProductRepository`/`IInventoryRepository` are exposed only via `IUnitOfWork.Products`/`.Inventory` (AD-9)

**And Given** `OrderFlow.BLL`
**When** implemented
**Then** `IProductService` exposes async CRUD over `ProductDto`, and `IInventoryService` exposes `GetStockLevel(productId)` and the sole `HasSufficientStock(productId, quantity)` method (AD-13) that Order stories will reuse

**And Given** `OrderFlow.Tests`
**When** complete
**Then** `ProductService`/`InventoryService` are covered with mocked `IUnitOfWork`, including `HasSufficientStock` true/false cases

### Story 1.5: Product Management & Inventory Visibility UI

As a user,
I want to create, view, edit, and list Products, and see current stock levels,
So that I can manage the catalog and know what's available to sell.

**Acceptance Criteria:**

**Given** the app is running
**When** I open the Product list
**Then** `ProductListForm`/`ProductListPresenter` displays all Products (Name, SKU, UnitPrice) alongside each Product's current `StockQuantity`, loaded asynchronously

**And Given** the list
**When** I create or edit a Product
**Then** valid submission persists and refreshes the list; invalid input (e.g. missing SKU) surfaces the `Result<T>` failure without crashing

**And Given** the Product list
**When** I view stock levels
**Then** `StockQuantity` reflects the latest committed value — fulfilling FR-6

## Epic 2: Order Creation, Pricing & Inventory

Users can create Orders for a Customer, pick an OrderType, add OrderItems, see the total computed via a swappable Pricing Strategy, and have Inventory checked/decremented safely at confirmation (with optimistic concurrency protection, async so the UI doesn't hang).

**FRs covered:** FR-3, FR-5, FR-7, FR-15, NFR-1, NFR-2

**Epics-level decisions (deferred by Architecture, locked here):** Pricing Strategy is a single `StandardPricingStrategy` (sums `Quantity × UnitPriceAtOrder`, no discount). `RushOrderProcessor` applies the base total plus a 10% rush surcharge; `StandardOrderProcessor` applies it unmodified. Notification surface is a minimal in-app notification log. "Create" and "confirm" are a single atomic user action — there is no separate draft state; order creation directly validates stock, prices, persists, decrements inventory, and sets the initial status to `Confirmed` (firing one notification) in one async transaction.

### Story 2.1: Order & OrderItem Domain, Repository

As a developer,
I want Order and OrderItem entities and a repository wired through the Unit of Work,
So that Orders and their line items can be persisted.

**Acceptance Criteria:**

**Given** `OrderFlow.Domain`
**When** complete
**Then** an `Order` entity (Id, CustomerId FK, OrderType, OrderStatus, `RowVersion`, `IAuditable`) and `OrderItem` entity (Id, OrderId FK, ProductId FK, Quantity, `UnitPriceAtOrder` — a snapshot of `Product.UnitPrice` at add-time) exist with configs in `OrderFlow.DAL`, and a migration adds `Orders`/`OrderItems` tables with FKs to `Customers`/`Products`

**And Given** `OrderFlow.DAL`
**When** implemented
**Then** `IOrderRepository`/`OrderRepository` is exposed only via `IUnitOfWork.Orders` (AD-9), supports persisting an Order with its OrderItems in a single `SaveChanges` call, and stamps `CreatedAt` only on `EntityState.Added` (AD-6)

**And Given** `OrderFlow.Domain`
**When** reviewed
**Then** `OrderStatus` includes at minimum a `Confirmed` value sufficient for order creation; the rest of the lifecycle is added in Epic 3

**And Given** `OrderFlow.Tests`
**When** complete
**Then** `IOrderRepository` is covered by a test verifying an Order + OrderItems round-trip persists correctly

### Story 2.2: Pricing Strategy — Order Total Calculation

As a developer,
I want a swappable Pricing/Discount Strategy applied to an Order's line items,
So that the total is calculated consistently and can be changed by swapping one composition-root registration.

**Acceptance Criteria:**

**Given** `OrderFlow.BLL`
**When** implemented
**Then** `IPricingStrategy` (`CalculateTotal(IEnumerable<OrderItemDto>) => decimal`) exists with one concrete `StandardPricingStrategy` (sums `Quantity × UnitPriceAtOrder`, no discount), registered Scoped at the composition root per AD-11 (single registration, no keyed dispatch)

**And Given** `IPricingStrategy`
**When** a different pricing rule is needed later
**Then** swapping is a one-line change at the composition root — no changes required in `OrderService` or Presentation

**And Given** `OrderFlow.Tests`
**When** complete
**Then** `StandardPricingStrategy` is covered by tests over multiple line items with varying quantities/prices

### Story 2.3: Order Processor Factory (Standard vs. Rush)

As a developer,
I want an Order Processor Factory that selects Standard or Rush processing behavior via keyed DI,
So that the system applies OrderType-specific rules when confirming an order.

**Acceptance Criteria:**

**Given** `OrderFlow.BLL`
**When** implemented
**Then** `IOrderProcessor` (`ConfirmAsync(CreateOrderRequest) => Result<OrderDto>`) exists with `StandardOrderProcessor`/`RushOrderProcessor` registered via `AddKeyedScoped<IOrderProcessor>(OrderType, ...)` (AD-7), resolved by `OrderProcessorFactory` — itself registered Scoped, not Singleton (AD-5) — from the ambient scope by `OrderType`

**And Given** `RushOrderProcessor`
**When** it computes a total
**Then** it applies `IPricingStrategy`'s base total plus a 10% rush surcharge; `StandardOrderProcessor` applies the base total unmodified

**And Given** `OrderFlow.Tests`
**When** complete
**Then** `OrderProcessorFactory` is tested resolving the correct processor per `OrderType`, and each processor's total-calculation difference is unit tested

### Story 2.4: Order Status Foundation & Notification Plumbing

As a developer,
I want a minimal OrderStatusService and INotifier wired so confirming an order can set its initial status and fire a notification,
So that later stories can extend the same mechanism for the full order lifecycle.

**Acceptance Criteria:**

**Given** `OrderFlow.BLL`
**When** implemented
**Then** `IOrderStatusService`/`OrderStatusService` exists with `TransitionTo(orderId, newStatus)` as the sole owner of the allowed-transition table (AD-4), initially supporting only the "new order" → `Confirmed` transition (the rest of the lifecycle is added in Epic 3), and is the only caller of `INotifier.Notify(...)`, fired only after the `UnitOfWork` commits

**And Given** `OrderFlow.BLL`
**When** implemented
**Then** `INotifier` is registered Singleton (AD-5) with a minimal in-app-log implementation publishing an `OrderStatusChangedNotification { OrderId, OldStatus, NewStatus }` DTO

**And Given** `OrderFlow.Tests`
**When** complete
**Then** `OrderStatusService.TransitionTo` is tested confirming the initial transition succeeds and fires exactly one notification

### Story 2.5: Order Creation & Confirmation UI

As a user,
I want to create a new Order for a Customer, add OrderItems, choose an OrderType, and confirm it,
So that the customer's purchase is recorded and stock is reserved.

**Acceptance Criteria:**

**Given** the app is running
**When** I open "New Order"
**Then** `OrderCreateForm` (`IOrderCreateView`) lets me pick a Customer, an OrderType (Standard/Rush), and add OrderItems (Product + quantity) via a line-item grid, through `OrderCreatePresenter` (one `IServiceScope` per action, AD-3)

**And Given** a filled-out order
**When** I click Confirm
**Then** the resolved `IOrderProcessor` asynchronously (NFR-1) validates stock via `IInventoryService.HasSufficientStock` for every line item, computes the total (via `IPricingStrategy` + rush surcharge if applicable), persists the Order+OrderItems, decrements Inventory, and transitions status to `Confirmed` — all within one `UnitOfWork` transaction — with the UI staying responsive throughout

**And Given** a line item requests more stock than available
**When** I click Confirm
**Then** confirmation is rejected with a message identifying the insufficient-stock item(s); no Order is persisted and no Inventory is decremented (no partial decrement) — fulfilling FR-5

**And Given** two near-simultaneous Confirm actions touch the same Product's Inventory
**When** the second commit detects a stale `RowVersion`
**Then** it fails with a `Result<T>` concurrency error surfaced to the user, not a silent overwrite — fulfilling NFR-2

## Epic 3: Order Lifecycle Visibility & Notifications

Users can view the Orders list/detail (line items, total, status), transition an Order through its status workflow (invalid transitions blocked), and see a Notification fire on each status change.

**FRs covered:** FR-4, FR-8, FR-9

**Epics-level decision (deferred by Architecture, locked here):** the full per-OrderType `OrderStatus` transition table. Standard: `Confirmed → Processing → Shipped → Delivered`, with `Cancelled` reachable from `Confirmed` or `Processing` (not after `Shipped`). Rush: same forward sequence, but `Cancelled` is reachable only from `Confirmed` — Rush orders begin processing immediately, so once `Processing` starts it's committed.

### Story 3.1: OrderStatus Full Transition Table

As a developer,
I want the full per-OrderType OrderStatus transition table implemented in OrderStatusService,
So that Orders can progress through their complete lifecycle with invalid transitions blocked.

**Acceptance Criteria:**

**Given** `OrderStatusService` (AD-4)
**When** extended
**Then** its allowed-transition table is partitioned by OrderType per the sequences above

**And Given** a requested transition not in the table for the Order's OrderType
**When** `TransitionTo` is called
**Then** it returns a `Result<T>` failure without persisting a status change or firing a notification

**And Given** a valid transition
**When** `TransitionTo` succeeds
**Then** it fires `INotifier.Notify(OrderStatusChangedNotification)` only after the `UnitOfWork` commits (unchanged from Epic 2's foundation)

**And Given** `OrderFlow.Tests`
**When** complete
**Then** `TransitionTo` is tested for at least one valid and one invalid transition per OrderType, including the Rush-specific `Cancelled` restriction

### Story 3.2: Order List & Detail View

As a user,
I want to view a list of Orders and open an Order's detail,
So that I can see its line items, total, OrderType, and current status.

**Acceptance Criteria:**

**Given** the app is running
**When** I open the Order list
**Then** `OrderListForm`/`OrderListPresenter` displays all Orders (Customer name, OrderType, OrderStatus, total) loaded asynchronously via `IOrderService`

**And Given** the Order list
**When** I open an Order
**Then** `OrderDetailForm`/`OrderDetailPresenter` shows its line items (Product, Quantity, `UnitPriceAtOrder`), computed total, OrderType, and current OrderStatus — fulfilling FR-4

**And Given** the Order detail view
**When** reviewed
**Then** it references only `OrderDto`/`OrderItemDto` types — no Domain entities cross into Presentation (AD-12)

### Story 3.3: Order Status Transition UI

As a user,
I want to transition an Order through its status from the Order detail view,
So that I can track and advance its lifecycle.

**Acceptance Criteria:**

**Given** an open Order detail
**When** I view available actions
**Then** only statuses valid per the current OrderStatus/OrderType are offered; invalid transitions are not presented as options

**And Given** a valid transition
**When** I select it
**Then** `OrderDetailPresenter` calls `IOrderStatusService.TransitionTo` asynchronously, and on success the detail view refreshes to show the new status

**And Given** an attempted invalid transition (e.g. stale UI state)
**When** `TransitionTo` rejects it
**Then** the rejection message is surfaced without a crash and the displayed status is unchanged — fulfilling FR-8

### Story 3.4: Notification Visibility

As a user,
I want to see a visible confirmation whenever an Order's status changes,
So that I know the event fired.

**Acceptance Criteria:**

**Given** the in-app notification log (Epic 2's `INotifier`)
**When** an `OrderStatusChangedNotification` is published
**Then** it becomes visible in the UI (a notification panel on `MainForm`) showing OrderId, OldStatus, NewStatus, and a timestamp

**And Given** multiple status changes occur during a session
**When** I view the notification panel
**Then** all fired notifications for the session are listed in order — fulfilling FR-9

**And Given** the notification panel
**When** reviewed
**Then** it is populated via the same `INotifier` singleton — no duplicate notification pathway, consistent with AD-4's single-caller rule

## Epic 4: Architecture Teaching Exhibits & Interview Documentation

The tutorial's teaching deliverable: standalone Before/After SOLID exhibit pairs, plus a maintained `docs/interview-topic-map.md` mapping every named interview topic to the class/file that demonstrates it.

**FRs covered:** FR-12, FR-13

**Epics-level decision (exhibit pairs not named by FR-12, locked here):** three SOLID violations, each deliberately mirroring a real pattern already built in Epics 1-3 so the exhibit reinforces the lesson from the actual app — SRP (god-class vs. separated responsibilities), OCP (switch/if-else vs. Strategy pattern, mirrors `IPricingStrategy`), DIP (`new`-ed concrete dependency vs. constructor-injected interface, mirrors the DI composition root).

### Story 4.1: SRP Exhibit Pair (Before/After)

As a viewer studying the codebase,
I want a Before/After exhibit demonstrating a Single Responsibility Principle violation and its refactor,
So that I can see a concrete, runnable example of the principle.

**Acceptance Criteria:**

**Given** `OrderFlow.Exhibits/Before/Srp`
**When** reviewed
**Then** it contains a single class combining order validation, persistence, and notification logic (multiple reasons to change), independently runnable with no reference to the main app's DI graph (AD-8)

**And Given** `OrderFlow.Exhibits/After/Srp`
**When** reviewed
**Then** the same scenario is refactored into separate single-responsibility classes (e.g. `OrderValidator`, `OrderPersister`, `OrderNotifier`) composed together, producing equivalent output to the Before version

**And Given** both exhibits
**When** run
**Then** each is runnable independently, without requiring the other or the main OrderFlow app

### Story 4.2: OCP Exhibit Pair (Before/After)

As a viewer studying the codebase,
I want a Before/After exhibit demonstrating an Open/Closed Principle violation and its refactor via the Strategy pattern,
So that I can see why the real app's `IPricingStrategy` design matters.

**Acceptance Criteria:**

**Given** `OrderFlow.Exhibits/Before/Ocp`
**When** reviewed
**Then** it contains a pricing calculator using a switch/if-else chain on a discount-type enum, requiring modification to add a new discount type

**And Given** `OrderFlow.Exhibits/After/Ocp`
**When** reviewed
**Then** the same scenario is refactored using the Strategy pattern, where adding a new discount type requires no changes to existing classes — mirroring AD-11

**And Given** both exhibits
**When** run
**Then** each produces equivalent pricing output for the same inputs, independently runnable

### Story 4.3: DIP Exhibit Pair (Before/After)

As a viewer studying the codebase,
I want a Before/After exhibit demonstrating a Dependency Inversion Principle violation and its refactor via constructor injection,
So that I can see why the main app's DI composition root matters.

**Acceptance Criteria:**

**Given** `OrderFlow.Exhibits/Before/Dip`
**When** reviewed
**Then** it contains a class that directly instantiates a concrete data-access class internally, tightly coupled and untestable without a real database

**And Given** `OrderFlow.Exhibits/After/Dip`
**When** reviewed
**Then** the same scenario is refactored to depend on an injected interface, with a runnable demo substituting a fake implementation

**And Given** both exhibits
**When** run
**Then** each is independently runnable, and the After demo swaps in a fake without modifying the consuming class

### Story 4.4: Interview Topic Map

As a viewer preparing for interviews,
I want `docs/interview-topic-map.md` mapping every named interview topic to the specific class/file demonstrating it,
So that I can quickly locate a working example for any topic.

**Acceptance Criteria:**

**Given** `docs/interview-topic-map.md`
**When** created
**Then** it lists every interview topic named across the PRD/Architecture (DI & Composition Root, Repository + Unit of Work, Strategy Pattern, Factory Pattern/keyed DI, SOLID: SRP/OCP/DIP, Optimistic Concurrency, Presenter/MVP, `Result<T>` error handling, async-all-the-way) and maps each to the specific class/file (real app) and exhibit pair demonstrating it

**And Given** the map
**When** reviewed
**Then** no named topic is missing an entry, and no entry references a class/file that doesn't exist

**And Given** new topics are added later
**When** the map is updated
**Then** it remains a single maintained file, per FR-13's "ships and maintains" requirement
