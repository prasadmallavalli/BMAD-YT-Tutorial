---
title: "PRD: OrderFlow Desktop — WinForms Interview-Prep Reference App"
status: final
created: 2026-08-06
updated: 2026-08-06
---

# PRD: OrderFlow Desktop — WinForms Interview-Prep Reference App
*Working title — confirm.*

## 0. Document Purpose

This PRD defines what OrderFlow Desktop must do, for the candidate (acting as their own PM) and for downstream Architecture and Epics/Stories work. It builds on `brief.md` and `addendum.md` at `_bmad-output/planning-artifacts/briefs/brief-BMAD YT TUTORIAL-2026-08-06/` — tech-stack rationale, options-considered detail, and open architecture questions live there and are not repeated here. Vocabulary is Glossary-anchored (§3); features are grouped with Functional Requirements (FRs) nested and globally numbered (FR-1…FR-N); inline `[ASSUMPTION]` tags mark inferences made under Fast Path and are indexed in §9.

## 1. Vision

Senior WinForms/.NET interviews rarely stop at definitions — they ask a candidate to open a codebase live and explain why a class is shaped the way it is. Most "design patterns in C#" material online uses toy examples (shapes, animals) disconnected from the layered enterprise systems a 12-year veteran is expected to have built. OrderFlow Desktop closes that gap: it is a real, runnable WinForms order-management application whose actual purpose is to serve as defensible interview material for a senior team-lead role requiring 12+ years in ASP.NET WinForms, C#, Entity Framework, layered architecture, MSSQL, SOLID, design patterns, and dependency injection. It is working code against a real SQL Server database — not a slide deck, not a snippet collection — that the candidate can run, click through, and defend line-by-line under live interview scrutiny.

Every interview topic that matters (Repository, Unit of Work, Strategy, Factory, Observer, SOLID violations and their fixes, constructor injection, EF Core relationships, transaction handling) exists here as a real feature solving a real problem inside an Order Management domain, not as an isolated `Foo`/`Bar` example. A companion test project makes the testability payoff of that architecture a demonstrated fact rather than a talking point.

The production-grade UI investment is deliberate: this is meant to double as a portfolio piece the candidate can point to beyond the interview itself, not just an internal study exercise. If it proves useful past the immediate interview cycle, it becomes a living reference the candidate keeps extending — one new pattern or refactor exercise at a time — and reuses to mentor other engineers making the same leap from "I can define the pattern" to "I can show you the pattern doing real work."

## 2. Target User

### 2.1 Jobs To Be Done

- As the candidate, I need a codebase I built and deeply understand — not one I copy-pasted — so I can open any file in a live screen-share and defend the design decision behind it.
- As the candidate, I need each interview topic (SOLID principle, pattern, DI, EF usage) tied to a specific, findable class so I'm never reciting a definition with nothing to point at.
- As the candidate, I need to demonstrate testability, not just claim it, because "how would you test this?" is a near-guaranteed senior-level follow-up.
- As a mentor (secondary), I need the same project to be walkable end-to-end with someone else, so it doubles as teaching material.

### 2.2 Non-Users (v1)

- End customers of a real order-management business — this is demo/reference software, not a product being sold or operated in production.
- Multiple concurrent operators — the app is used by one person at a time (the candidate, live or in prep), not a team.

### 2.3 Key User Journeys

- **UJ-1. Candidate defends the codebase cold, live in an interview.**
  Candidate, mid-interview, is asked to open the project and explain a class boundary. They navigate directly to the relevant BLL/DAL interface, point to the composition root, and explain the trade-off — because §4.8's topic map told them exactly where to look before the interview even started.

- **UJ-2. Candidate runs the golden-path order end-to-end on a screen-share.**
  - **Persona + context:** the candidate, demoing the app live as proof it's real and runnable, not just architecturally sound on paper.
  - **Entry state:** app open, connected to a real MSSQL database seeded with at least one Customer and Product.
  - **Path:** (1) create a Customer, (2) create/select a Product, (3) open Order entry, add the Customer, add one or more OrderItem lines via the grid, (4) confirm the Order.
  - **Climax:** on confirm, Inventory visibly decrements, the pricing/discount Strategy computes the total, the OrderStatus advances, and a status-change notification fires — four architectural claims proven in one action.
  - **Resolution:** Order appears in the Order list/detail with correct totals and status; candidate can narrate which class did which job as it happened.
  - **Edge case:** requested quantity exceeds available Inventory — Order confirmation is blocked with a clear validation message rather than silently overselling.

- **UJ-3. Candidate (or mentee) walks a SOLID before/after exhibit.**
  Candidate opens the "before" (violating) version of a class next to its "after" (refactored) counterpart and narrates, concretely, what changed and why — turning an abstract principle into a pointed-at diff.

- **UJ-4. Candidate proves testability under questioning.**
  Asked "how would you test this?", candidate runs the xUnit project live, opens a test that mocks `IOrderRepository`/`IPricingStrategy`, and explains why constructor injection is what makes the mock possible.

## 3. Glossary

- **Customer** — a person/entity that places Orders. Has a name and contact info.
- **Product** — a sellable item with a unit price and stock quantity, tracked via Inventory.
- **Inventory** — the stock-quantity record per Product; decremented when an Order is confirmed.
- **Order** — a purchase transaction for one Customer, containing one or more OrderItems, an OrderStatus, an OrderType, and a computed total.
- **OrderItem** — a single Product + quantity line within an Order.
- **OrderType** — the classification an Order is created with (e.g., Standard, Rush) that determines which order-processing behavior the Order Processor Factory selects.
- **OrderStatus** — the current stage of an Order's lifecycle (see FR-8 for the sequence).
- **Pricing/Discount Strategy** — the swappable calculation (Strategy pattern) that computes an Order's total from its OrderItems.
- **Order Processor Factory** — the component (Factory pattern) that creates the correct order-processing behavior based on OrderType.
- **Notification** — an event (Observer pattern) raised when an Order's OrderStatus changes.
- **Repository** — a DAL interface abstracting persistence for a given entity (e.g., `IOrderRepository`).
- **Unit of Work** — the DAL component coordinating multiple Repository operations and EF Core transactions as one atomic unit.
- **BLL (Business Logic Layer)** — the layer holding validation, pricing, inventory, and status-workflow rules, expressed through interfaces.
- **DAL (Data Access Layer)** — the layer holding Repository + Unit of Work implementations over EF Core.
- **Composition Root** — the single startup location where all DI wiring occurs (`Microsoft.Extensions.DependencyInjection`).
- **Interview Topic Map** — `docs/interview-topic-map.md`, mapping each interview topic to the class/file demonstrating it.
- **Before/After Exhibit** — a paired "SOLID-violating" and "refactored" implementation of the same scenario, kept viewable side-by-side.

## 4. Features

### 4.1 Customer & Product Management
**Description:** Baseline CRUD screens that exist so Orders have real Customers and Products to reference — not the interview focus themselves, but the substrate everything else runs on.

**Functional Requirements:**

#### FR-1: Manage Customers
User can create, view, edit, and list Customers via WinForms grid/detail forms.

**Consequences (testable):**
- A Customer requires a name to save.
- The Customer list reflects creates/edits without requiring app restart.

#### FR-2: Manage Products
User can create, view, edit, and list Products (name, SKU, unit price, stock quantity) via WinForms grid/detail forms.

**Consequences (testable):**
- A Product requires a name and non-negative unit price to save.
- Product stock quantity is visible from the same screen (read view onto Inventory).

### 4.2 Order Entry & Line Items
**Description:** The core data-entry workflow — creating an Order against a Customer with one or more OrderItem lines via a grid. Realizes UJ-2.

**Functional Requirements:**

#### FR-3: Create Order with line items
User can create a new Order for a selected Customer, adding one or more OrderItems (Product + quantity) via a line-item grid. Realizes UJ-2.

**Consequences (testable):**
- An Order cannot be saved with zero OrderItems.
- An Order requires an OrderType (Standard or Rush) at creation, selected by the user.
- Each OrderItem's requested quantity is validated against available Inventory before the Order can be confirmed (see FR-5).

#### FR-4: View Order list and detail
User can view a list of Orders and open an Order's detail, showing its line items, computed total (post pricing/discount), OrderType, and current OrderStatus.

**Consequences (testable):**
- Order detail total matches the output of the active Pricing/Discount Strategy for that Order's line items.

**Feature-specific NFRs:**
- Order confirmation (FR-5, FR-7, FR-9) runs asynchronously; the UI thread remains responsive and does not freeze during the database round trip, so a live screen-share demo of UJ-2 isn't interrupted by a hung form.

### 4.3 Inventory Management & Availability
**Description:** Keeps stock quantities consistent with confirmed Orders and blocks overselling. Realizes UJ-2's edge case.

**Functional Requirements:**

#### FR-5: Decrement Inventory on Order confirmation
System decrements Inventory stock quantity for each Product line item when an Order is confirmed. Realizes UJ-2.

**Consequences (testable):**
- Confirming an Order whose requested quantity exceeds current Inventory for any line is rejected with a validation message; no partial decrement occurs.
- Inventory decrement and Order confirmation succeed or fail together (atomic via Unit of Work).

#### FR-6: View current Inventory levels
User can view current Inventory stock levels per Product.

**Consequences (testable):**
- Displayed stock level matches the Inventory record's current quantity and updates without an app restart after a confirming Order (FR-5) decrements it.

### 4.4 Pricing & Discount Strategy
**Description:** Demonstrates the Strategy pattern doing real work — computing Order totals in a way that's swappable without touching Order-entry or persistence code. Realizes UJ-2.

**Functional Requirements:**

#### FR-7: Compute Order total via swappable pricing strategy
System calculates an Order's total by applying a configurable Pricing/Discount Strategy (e.g., volume or customer-tier discount) over its OrderItems. Realizes UJ-2.

**Consequences (testable):**
- Changing the active strategy implementation changes computed totals without any code change to Order-entry or DAL code — verified by the companion test project (FR-14) exercising at least two strategy implementations against the same OrderItem set with different results.

**Notes:** Concrete discount rule(s) to implement (volume-based, customer-tier, or both) are an open product decision — see §8, Q3.

### 4.5 Order Status Workflow & Notifications
**Description:** Order lifecycle progression plus an Observer-pattern notification on every status change. Realizes UJ-2.

**Functional Requirements:**

#### FR-8: Progress Order status through a defined workflow
User can transition an Order through its OrderStatus sequence from the Order detail view; invalid transitions are blocked. `[ASSUMPTION: sequence is Created → Confirmed → Shipped → Completed, with Cancelled reachable from any pre-Shipped state — confirm during Architecture/Epics.]`

**Consequences (testable):**
- Attempting a transition outside the defined sequence (e.g., Created → Completed directly) is rejected.

#### FR-9: Raise notification on status change
System raises a Notification whenever an Order's OrderStatus changes, visible to the user as confirmation the event fired. Realizes UJ-2. `[ASSUMPTION: notification surface is in-app (log panel or toast), not an external channel like email/SMS — no external-integration requirement was stated and the brief excludes web/API surfaces.]`

**Consequences (testable):**
- Every OrderStatus change produces exactly one corresponding Notification event, observable in the UI.

### 4.6 Layered Architecture & Dependency Injection Foundation
**Description:** The structural backbone every other feature sits on — DI, Repository, Unit of Work, and strict DAL/BLL/presentation separation. This is not a UI feature; it's the thing the whole project exists to demonstrate. Realizes UJ-1, UJ-3.

**Functional Requirements:**

#### FR-10: Constructor-inject all BLL/DAL dependencies from a single composition root
All BLL and DAL dependencies are resolved via constructor injection, wired at one composition root (`Microsoft.Extensions.DependencyInjection`); no direct `new` instantiation of service/repository types within business or presentation code. Realizes UJ-1, UJ-3.

**Consequences (testable):**
- Swapping any BLL/DAL interface's implementation requires changing only composition-root wiring — no calling-code changes.
- WinForms forms depend only on injected BLL interfaces and contain no business logic (validation, pricing, workflow rules) in their code-behind — forms orchestrate UI and delegate everything else. `[ASSUMPTION: this states the product-level requirement from the brief ("MVP-style separation so forms are not doing business logic"); the specific pattern name (MVP vs. another passive-view variant) stays open per addendum item 6d — see §8 closing note.]`

#### FR-11: Persist via Repository + Unit of Work over EF Core
Data access is implemented via per-entity Repository interfaces, coordinated by a Unit of Work that manages EF Core transactions.

**Consequences (testable):**
- Multi-step persistence operations (e.g., Order confirmation + Inventory decrement, FR-5) commit or roll back together.

#### FR-15: Select order-processing behavior via Order Processor Factory
System selects the applicable order-processing behavior via an Order Processor Factory keyed on the Order's OrderType (FR-3). Realizes UJ-1, UJ-3. `[ASSUMPTION: v1 ships two OrderTypes — Standard and Rush — where Rush applies expedited status-transition/notification handling (e.g., compressed workflow or higher-priority Notification). The exact behavioral difference is a product decision for Architecture/Epics, not invented further here — see §8, Q5.]`

**Consequences (testable):**
- The processor selected for a Rush Order demonstrably differs in behavior from a Standard Order for the same input, provable by the companion test project (FR-14) exercising both factory outputs and asserting the difference.
- Adding a new OrderType requires only a new processor implementation and factory registration — no changes to Order entry (FR-3) or persistence (FR-11).

### 4.7 SOLID Before/After Refactor Exhibits
**Description:** Makes SOLID concrete by pairing a violating implementation with its refactor, both viewable side-by-side. Realizes UJ-3.

**Functional Requirements:**

#### FR-12: Ship paired Before/After Exhibits
The codebase includes at least 2-3 explicit Before/After Exhibit pairs (SOLID-violating vs. refactored), independently viewable and, where practical, independently runnable. Realizes UJ-3.

**Consequences (testable):**
- Each exhibit pair is documented (inline or in the topic map, FR-13) with which principle it demonstrates and why the "after" version is preferable.

### 4.8 Interview Topic → Code Map Documentation
**Description:** The artifact that makes the whole project usable as study material under time pressure — a lookup table from "topic asked about" to "file to open." Realizes UJ-1.

**Functional Requirements:**

#### FR-13: Maintain `docs/interview-topic-map.md`
The project ships a maintained topic map covering every named interview topic (each SOLID principle, each design pattern, DI, EF Core usage, DAL/BLL separation, testability) mapped to the specific class/file demonstrating it. Realizes UJ-1.

**Consequences (testable):**
- Every row in the map resolves to a file/class that actually exists in the codebase at time of interview prep (verified, not assumed — see SM-2).
- Map is updated whenever Epics/Stories or Dev work adds, moves, or renames a mapped class.

### 4.9 Companion Automated Test Project
**Description:** Proves the testability claim rather than asserting it. Realizes UJ-4.

**Functional Requirements:**

#### FR-14: Unit-test BLL behavior against mocked DAL/BLL interfaces
A companion xUnit test project exercises BLL behavior (order validation, pricing calculation via FR-7, status transitions via FR-8) against mocked interfaces such as `IOrderRepository` and `IPricingStrategy`. Realizes UJ-4.

**Consequences (testable):**
- Test suite passes with no live database dependency.
- At least one test demonstrates mocking a Repository interface and at least one demonstrates mocking the Pricing/Discount Strategy interface.

## 5. Non-Goals (Explicit)

- Not a multi-user or production order-management system — no authentication/authorization, no concurrent-user handling beyond basic optimistic concurrency.
- Not a reporting, printing, or export tool.
- Not a deployment/installer product — packaging is out of scope.
- Not a web or API product — WinForms desktop only, no web front end or public API surface.
- Not multi-tenant or multi-currency.

## 6. MVP Scope

### 6.1 In Scope
- Customers, Products, Orders (with OrderType), OrderItems, Inventory, OrderStatus workflow (§4.1–4.5)
- WinForms UI: customer/product management, order entry with grid line items, order list/detail with status transitions
- BLL: order validation, pricing/discount strategy, inventory availability checks, status transitions, notification triggering, order-processor dispatch (§4.6)
- DAL: Repository + Unit of Work over EF Core/MSSQL, with migrations
- DI wiring via `Microsoft.Extensions.DependencyInjection`
- Companion xUnit test project with mocked dependencies (§4.9)
- 2-3 explicit Before/After Exhibit pairs for SOLID refactors (§4.7)
- `docs/interview-topic-map.md` (§4.8)

### 6.2 Out of Scope for MVP
- Authentication/authorization, and any multi-user concurrency beyond basic optimistic concurrency
- Reporting, analytics, printing, exporting
- Deployment/installer packaging
- Web or API layer
- Multi-tenant or multi-currency support

## 7. Success Metrics

**Primary**
- **SM-1**: Candidate runs the full golden-path order flow (UJ-2) live against a real MSSQL database with zero errors. Validates FR-3, FR-5, FR-7, FR-9.
- **SM-2**: 100% of rows in the Interview Topic Map resolve to an actual, current class/file in the codebase. Validates FR-13.
- **SM-3**: Companion test suite passes and visibly mocks at least one Repository and one Strategy interface. Validates FR-14.

**Secondary**
- **SM-4**: Candidate can, unaided, explain *why* each pattern was used in this specific context — not just what the pattern is (self-assessed; the PRD and downstream docs support this, they don't replace it).

**Counter-metrics (do not optimize)**
- **SM-C1**: Number of screens/forms is not a target. Adding UI surface area beyond what's needed to exercise the topic map is a regression — it burns interview-prep time without adding defensible architecture depth. Counterbalances FR-1/FR-2/FR-4.

## 8. Open Questions

1. Concrete discount rule(s) for the Pricing/Discount Strategy (FR-7) — volume-based, customer-tier, or both — decide during Architecture/Epics.
2. Full OrderStatus sequence and whether Cancelled is reachable from every pre-Shipped state, or only specific ones — confirm during Architecture/Epics (see FR-8's assumption).
3. Notification surface — in-app log, toast, or both — decide during UX/Architecture (see FR-9's assumption).
4. Whether Singleton earns a legitimate use in this codebase (e.g., a settings accessor) or is honestly left out — carried from the brief's addendum, decide during Architecture.
5. Exact behavioral difference between Standard and Rush OrderTypes for the Order Processor Factory (FR-15) — decide during Architecture/Epics (see FR-15's assumption).
6. Whether Order Management stays sufficient to justify every requested pattern, or whether a secondary scenario needs folding in — e.g. a multi-step order-approval chain (Chain of Responsibility) for large Orders, or a notification-channel Adapter (email/SMS/in-app) for FR-9's Notification payload. Carried from the brief's addendum as a contingency, not a current requirement — revisit only if Architecture/Epics finds Order Management too thin for a pattern the candidate still wants to cover.

*(Technical-how questions — EF Core relationship shapes/soft-delete auditing, before/after exhibit placement as separate project vs. folder, LocalDB vs. SQL Server Developer Edition, MVP vs. disciplined code-behind for WinForms — remain captured in `addendum.md` at the brief workspace for Architecture to resolve; not duplicated here per PRD capabilities-not-implementation discipline.)*

## 9. Assumptions Index

*Inline `[ASSUMPTION]`-tagged inferences, each open for confirmation:*
- FR-8 — OrderStatus sequence assumed as Created → Confirmed → Shipped → Completed, with Cancelled reachable pre-Shipped. Open Question 2.
- FR-9 — Notification surface assumed in-app (log/toast), not an external channel. Open Question 3.
- FR-10 — "forms contain no business logic" stated as the product-level requirement from the brief; the specific separation pattern name stays open (addendum item 6d).
- FR-15 — OrderType set assumed as Standard/Rush, with Rush behavior undefined beyond "expedited." Open Question 5.

*Fast Path additions not drawn from an explicit brief statement (design calls, not inferences — no matching Open Question):*
- §2.3 UJ-4 — testability journey added as a named UJ because "how would you test this?" is called out in the brief/addendum as a near-guaranteed follow-up worth its own journey.
