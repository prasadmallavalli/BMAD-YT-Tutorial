---
baseline_commit: NO_VCS
---

# Story 2.3: Order Processor Factory (Standard vs. Rush)

Status: done

## Story

As a developer,
I want an Order Processor Factory that selects Standard or Rush processing behavior via keyed DI,
so that the system applies OrderType-specific rules when confirming an order.

## Acceptance Criteria

1. **Given** `OrderFlow.BLL`, **When** implemented, **Then** `IOrderProcessor` (`ConfirmAsync(CreateOrderRequest) => Result<OrderDto>`) exists with `StandardOrderProcessor`/`RushOrderProcessor` registered via `AddKeyedScoped<IOrderProcessor>(OrderType, ...)` (AD-7), resolved by `OrderProcessorFactory` — itself registered Scoped, not Singleton (AD-5) — from the ambient scope by `OrderType`.
2. **And Given** `RushOrderProcessor`, **When** it computes a total, **Then** it applies `IPricingStrategy`'s base total plus a 10% rush surcharge; `StandardOrderProcessor` applies the base total unmodified.
3. **And Given** `OrderFlow.Tests`, **When** complete, **Then** `OrderProcessorFactory` is tested resolving the correct processor per `OrderType`, and each processor's total-calculation difference is unit tested.

## Tasks / Subtasks

- [x] Task 1: `CreateOrderRequest`/`OrderDto` (AC: #1)
  - [x] `OrderFlow.BLL/CreateOrderRequest.cs`: `CustomerId` (int), `OrderType` (enum), `Items` (`List<OrderItemDto>`, default `= []`) — the input shape a future Presenter (Story 2.5) will build from the "New Order" screen's line-item grid
  - [x] `OrderFlow.BLL/OrderDto.cs`: `Id` (int), `CustomerId` (int), `OrderType` (enum), `Status` (`OrderStatus` enum), `Total` (decimal), `Items` (`IReadOnlyList<OrderItemDto>`, default `= []`) — **`Total` has no matching column on the `Order` Domain entity (Story 2.1); it's always a derived value computed via `IPricingStrategy`, per FR-7, never persisted.** See Dev Notes on why `Id`/`Status` are placeholder values in this story specifically
- [x] Task 2: `IOrderProcessor` + `StandardOrderProcessor`/`RushOrderProcessor` (AC: #1, #2)
  - [x] `OrderFlow.BLL/IOrderProcessor.cs`: `Task<Result<OrderDto>> ConfirmAsync(CreateOrderRequest request)` — `Task<Result<T>>`, matching every other BLL service's async-`Result<T>` shape (`ProductService`/`InventoryService`), even though this story's own implementation has no `await` yet (see Dev Notes — Story 2.5 adds the real I/O)
  - [x] `OrderFlow.BLL/StandardOrderProcessor.cs`: constructor takes `IPricingStrategy`. `ConfirmAsync` calls `pricingStrategy.CalculateTotal(request.Items)` unmodified as `Total`, builds an `OrderDto` (`Id = 0`, `Status = OrderStatus.Unspecified` — see Dev Notes), wraps in `Result<OrderDto>.Success(...)`, returns via `Task.FromResult(...)`
  - [x] `OrderFlow.BLL/RushOrderProcessor.cs`: same shape as `StandardOrderProcessor`, but `Total = baseTotal + baseTotal * 0.10m` (10% rush surcharge per AC #2 and epics.md's Epic 2 "Epics-level decisions" note) — name the `0.10m` constant (e.g. `RushSurchargeRate`), don't inline a bare magic number
- [x] Task 3: `OrderProcessorFactory` + composition root registration (AC: #1)
  - [x] Add `Microsoft.Extensions.DependencyInjection.Abstractions` (Version `10.0.10`, matching `OrderFlow.Presentation.csproj`'s existing `Microsoft.Extensions.DependencyInjection` pin) to `OrderFlow.BLL.csproj` — this is new; `OrderFlow.BLL` has had zero package references until now (only `ProjectReference`s to `OrderFlow.DAL`/`OrderFlow.Domain`). Only the lighter `.Abstractions` package is needed here (interfaces + `GetRequiredKeyedService` extension method) — `OrderFlow.BLL` doesn't need the concrete `ServiceCollection`/`ServiceProvider` implementation that the full metapackage (already referenced by `OrderFlow.Presentation`) provides
  - [x] `OrderFlow.BLL/OrderProcessorFactory.cs`: **no interface** — per AD-7's own wording ("resolved by `OrderProcessorFactory`... `OrderProcessorFactory.Create(OrderType)`"), this is referred to by concrete type throughout the epics/architecture text, unlike `IPricingStrategy`/`IOrderProcessor`. Constructor takes `IServiceProvider` (the ambient **scoped** provider — DI containers inject the current scope's own `IServiceProvider` when a Scoped service requests one, not a captured root provider, satisfying AD-5/AD-7's "never a captured root provider" rule). `Create(OrderType orderType)` returns `_serviceProvider.GetRequiredKeyedService<IOrderProcessor>(orderType)`
  - [x] `OrderFlow.Presentation/Program.cs` `ConfigureServices`: `services.AddKeyedScoped<IOrderProcessor, StandardOrderProcessor>(OrderType.Standard);`, `services.AddKeyedScoped<IOrderProcessor, RushOrderProcessor>(OrderType.Rush);`, `services.AddScoped<OrderProcessorFactory>();` — **this keyed-DI shape is deliberately different from Story 2.2's `IPricingStrategy` registration** (one plain `AddScoped`, no keyed dispatch, per AD-11) — don't confuse the two patterns; `IOrderProcessor` varies per `OrderType` (AD-7), `IPricingStrategy` does not (AD-11)
- [x] Task 4: `OrderFlow.Tests` — factory + processor tests (AC: #3)
  - [x] Add `Microsoft.Extensions.DependencyInjection` (Version `10.0.10`, matching the pin used elsewhere) to `OrderFlow.Tests.csproj` — needed to build a real `ServiceCollection`/`ServiceProvider` for the factory-resolution test (below); this is new, no prior `OrderFlow.Tests` file has built its own DI container
  - [x] `OrderFlow.Tests/OrderProcessorFactoryTests.cs`: build a `ServiceCollection`, register `IPricingStrategy`→`StandardPricingStrategy` (a real instance — cheap and pure, no need to mock, same reasoning as Story 2.2's own tests), both keyed `IOrderProcessor` registrations, and `OrderProcessorFactory` itself; `BuildServiceProvider()`; resolve `OrderProcessorFactory` and assert `.Create(OrderType.Standard)` returns a `StandardOrderProcessor` instance and `.Create(OrderType.Rush)` returns a `RushOrderProcessor` instance (AC #3's "resolving the correct processor per `OrderType`")
  - [x] `OrderFlow.Tests/StandardOrderProcessorTests.cs`: construct with a real `StandardPricingStrategy` (no mocking — same reasoning as above), call `ConfirmAsync` with a `CreateOrderRequest` carrying 2+ `OrderItemDto`s, assert `Result.IsSuccess` and `Total` equals `StandardPricingStrategy.CalculateTotal(...)` on the same items (unmodified base total)
  - [x] `OrderFlow.Tests/RushOrderProcessorTests.cs`: same setup, assert `Total` equals the base total **plus 10%** — compute the expected value independently (e.g. `baseTotal * 1.10m`), don't mirror the production expression's own structure (see Story 2.2's code-review finding on tautological assertions — apply the same lesson here)
- [x] Task 5: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 7 projects) — 0 errors, 0 warnings
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` passes, including all new tests, and all 34 pre-existing tests still pass — **confirmed: 38/38 passed (34 prior + 4 new)**
  - [x] Confirm no `OrderFlow.Domain`/`OrderFlow.DAL` file was touched, and the only `OrderFlow.Presentation` change is the `Program.cs` registrations — this story is `OrderFlow.BLL` + `OrderFlow.Tests` (+ `Program.cs`) only, per its own AC — **confirmed via File List below**

### Review Findings

- [x] [Review][Patch] `RushOrderProcessor`'s total had no rounding to currency precision — multiplying a 2-decimal base total by the 10% surcharge rate can produce sub-cent values (e.g. `39.97 × 1.10 = 43.967`), an invalid currency amount. Fixed: wrapped in `Math.Round(..., 2, MidpointRounding.AwayFromZero)`; test's independently-computed expected value updated to apply the same rounding (the surcharge arithmetic itself is still computed independently, not mirrored from production — only the standard rounding step is shared). [`OrderFlow.BLL/RushOrderProcessor.cs`]
- [x] [Review][Patch] Both processors assigned `OrderDto.Items = request.Items` — the caller's own mutable `List<OrderItemDto>` reference exposed through a `IReadOnlyList<OrderItemDto>` property, so a caller retaining the original `CreateOrderRequest` could mutate the "already-returned" DTO's contents. Fixed: both now assign `request.Items.ToList()` (a defensive copy). [`OrderFlow.BLL/StandardOrderProcessor.cs`, `OrderFlow.BLL/RushOrderProcessor.cs`]
- [x] [Review][Patch] No test locked down the deliberate placeholder values (`OrderDto.Id == 0`, `Status == OrderStatus.Unspecified`) that the Dev Notes call out as an easy-to-regress scope boundary (AD-4's "sole owner of transitions" rule) — a careless Story 2.5 edit that set `Status = Confirmed` too early wouldn't have been caught by anything in the suite. Fixed: added assertions to both `StandardOrderProcessorTests` and `RushOrderProcessorTests`. [`OrderFlow.Tests/StandardOrderProcessorTests.cs`, `OrderFlow.Tests/RushOrderProcessorTests.cs`]
- [x] [Review][Patch] `IOrderProcessor.ConfirmAsync` had no documentation of its current-scope contract (no validation, no persistence yet), unlike `IPricingStrategy.CalculateTotal`'s equivalent comment from Story 2.2's own code review. Fixed: added a doc comment. [`OrderFlow.BLL/IOrderProcessor.cs`]
- [x] [Review][Patch] `OrderProcessorFactory.Create`'s throwing behavior for an `OrderType` with no keyed registration (e.g. `OrderType.Unspecified`) was undocumented and untested. Fixed: added a doc comment stating the contract, and a test (`Create_WithUnmappedOrderType_Throws`) locking it down. [`OrderFlow.BLL/OrderProcessorFactory.cs`, `OrderFlow.Tests/OrderProcessorFactoryTests.cs`]
- Dismissed as noise / out of scope / already covered (8): duplicated `ConfirmAsync` bodies between `StandardOrderProcessor`/`RushOrderProcessor` (a shared base class now would be a premature abstraction — Story 2.5 will reshape both classes with real `IUnitOfWork`/`IInventoryService`/`IOrderStatusService` dependencies, and the actual common shape isn't known until then); `OrderProcessorFactoryTests` hand-building its own `ServiceCollection` instead of testing against `Program.cs`'s real registrations, and resolving from the root provider rather than an explicit scope (same already-accepted category as Story 2.2's dismissed "DI registration not exercised by an automated test" — blocked by the same Windows-only `OrderFlow.Presentation` platform limitation, Story 1.1); no `CancellationToken` on `ConfirmAsync` (same already-deferred gap as `ICustomerRepository`/`IUnitOfWork`/`ICustomerService` since Story 1.2 — adding it now would touch every method signature across three layers, no AC asks for it); `Result<OrderDto>`'s `Failure` path being entirely unexercised (no failure path exists in this story's code at all — validation is explicitly Story 2.5's job per this story's own Dev Notes, so there's nothing to test yet); no additional comment on why `RushOrderProcessorTests`' expected-value expression is structured differently from production (the test already carries this rationale in its existing comment); null/empty `Items` on `CreateOrderRequest` producing an unhandled exception or a silent `Total = 0m` (matches the already-established "trust internal callers" convention from Stories 2.1/2.2, and Story 2.2's own explicit precedent that an empty collection correctly summing to `0m` is "a valid, correct result, not an error"); decimal overflow on an unrealistically large base total (matches Story 2.2's dismissed overflow finding — the existing global exception handler exists for exactly this class of unexpected failure).

## Dev Notes

- **`ConfirmAsync` does NOT validate stock, persist anything, decrement Inventory, or transition status in this story — that is Story 2.5's job, not this one.** Read literally, "Order Processor" and "`ConfirmAsync`" sound like they should fully confirm an order, but cross-referencing this story's own AC #3 ("each processor's total-calculation difference is unit tested" — nothing about persistence or stock) against Story 2.5's AC ("the resolved `IOrderProcessor` asynchronously validates stock... computes the total... persists the Order+OrderItems, decrements Inventory, and transitions status to `Confirmed`... all within one `UnitOfWork` transaction") and epics.md's Epic 2 "Epics-level decisions" note ("order creation directly validates stock, prices, persists, decrements inventory, and sets the initial status to `Confirmed`... in one async transaction") makes the split unambiguous: **this story builds the processor-selection-and-pricing building block only.** Story 2.5 will extend `StandardOrderProcessor`/`RushOrderProcessor` (same classes, not new ones) with the real `IUnitOfWork`/`IInventoryService`/`IOrderStatusService` orchestration — don't build that orchestration now, and don't be surprised that these processors only take `IPricingStrategy` as a dependency in this story.
- **`OrderDto.Id = 0` and `OrderDto.Status = OrderStatus.Unspecified` are correct, deliberate placeholders — not bugs.** `Id` stays `0` because nothing is persisted yet (no `IUnitOfWork.Orders.AddAsync` call exists in this story — that's Story 2.5). `Status` stays `Unspecified` rather than `Confirmed` because of AD-4: "`OrderStatusService.TransitionTo`... is the sole owner of the allowed-transition table... an `IOrderProcessor` (AD-7) requests a transition only by calling `TransitionTo`; it never evaluates transition validity itself." `OrderStatusService` doesn't exist until Story 2.4, and even then it needs a persisted `orderId` to transition — so this story's `IOrderProcessor` must not set `Status` to `Confirmed` itself; that would bypass AD-4's sole-owner rule. Both fields become real once Story 2.5 wires `IUnitOfWork`/`IOrderStatusService` into these same processors.
- **`ConfirmAsync` assumes valid, already-checked `CreateOrderRequest` input — no defensive validation added.** Matches the same "trust internal callers" reasoning already established for `IPricingStrategy.CalculateTotal` (Story 2.2) and `OrderRepository.AddAsync` (Story 2.1's code review) — this is an internal BLL-to-BLL call, not a system boundary. Don't add null/empty checks that no AC asks for.
- **`OrderProcessorFactory` has no interface — this is deliberate, not an inconsistency with the `IXxx` naming convention.** AD-7's own text names it by concrete type throughout ("resolved by `OrderProcessorFactory`", "`OrderProcessorFactory.Create(OrderType)`"), unlike `IPricingStrategy`/`IOrderProcessor`, which are always referred to as interfaces. It's a factory utility resolved by its own concrete registration (`services.AddScoped<OrderProcessorFactory>()`), not a swappable abstraction — don't add an `IOrderProcessorFactory` interface nobody asked for.
- **Keyed DI (`AddKeyedScoped`) is new to this codebase — don't confuse it with Story 2.2's `IPricingStrategy` registration.** AD-7 (`IOrderProcessor`, this story) varies behavior per `OrderType` via `AddKeyedScoped<IOrderProcessor>(OrderType, ...)` + `GetRequiredKeyedService`. AD-11 (`IPricingStrategy`, Story 2.2) is a single plain `AddScoped` with no keying at all, because pricing doesn't vary per `OrderType` (per Epic 2's own scope decision). These are two intentionally different wiring shapes for two different problems — see Story 2.2's own Dev Notes, which called this contrast out in advance.
- **`OrderProcessorFactory`'s `IServiceProvider` is the ambient scoped provider, not a captured root provider.** When a Scoped service (like `OrderProcessorFactory`, registered `AddScoped`) is constructor-injected with `IServiceProvider`, the DI container supplies the current operation's own scoped provider — this is standard `Microsoft.Extensions.DependencyInjection` behavior, not something this story's code needs to arrange manually. This satisfies AD-5's "never resolved from a captured root provider" rule without extra code.
- **New package reference split: `OrderFlow.BLL` gets `.Abstractions` only, `OrderFlow.Tests` gets the full metapackage.** `OrderFlow.BLL`'s own code only ever calls interface members/extension methods (`IServiceProvider.GetRequiredKeyedService<T>`), so it doesn't need the concrete `ServiceCollection`/`ServiceProvider` implementation — only `OrderFlow.Presentation` (the composition root) and `OrderFlow.Tests` (which builds its own throwaway container for the factory test) need the full `Microsoft.Extensions.DependencyInjection` package.
- **Naming conventions (unchanged):** `IXxx` interfaces, `XxxDto`/`XxxRequest` — `CreateOrderRequest` mirrors the "Request" suffix implied by epics.md's own naming, `OrderDto`/`IOrderProcessor`/`StandardOrderProcessor`/`RushOrderProcessor` all follow the existing table.
- **Data & formats:** `Total` is `decimal` (never `float`/`double`); the `0.10m` rush surcharge rate should be a named constant, not a bare literal repeated at each use site.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.BLL/
    CreateOrderRequest.cs     # new
    OrderDto.cs               # new
    IOrderProcessor.cs        # new
    StandardOrderProcessor.cs # new
    RushOrderProcessor.cs     # new
    OrderProcessorFactory.cs  # new
    OrderFlow.BLL.csproj      # modified: + Microsoft.Extensions.DependencyInjection.Abstractions
  OrderFlow.Presentation/
    Program.cs                # modified: + keyed IOrderProcessor registrations, + OrderProcessorFactory
  OrderFlow.Tests/
    OrderProcessorFactoryTests.cs   # new
    StandardOrderProcessorTests.cs  # new
    RushOrderProcessorTests.cs      # new
    OrderFlow.Tests.csproj          # modified: + Microsoft.Extensions.DependencyInjection
```

`OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.Presentation.Tests` are untouched by this story.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.3: Order Processor Factory (Standard vs. Rush)] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2: Order Creation, Pricing & Inventory] — "Epics-level decisions": the 10% rush surcharge value, and "order creation directly validates stock, prices, persists, decrements inventory, and sets the initial status to Confirmed... in one async transaction" — the source of this story's Domain-of-scope split against Story 2.5
- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.5: Order Creation & Confirmation UI] — the full validate-stock/persist/decrement/transition orchestration that this story's `ConfirmAsync` deliberately does not implement yet
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-7 — Order Processor Factory over keyed DI services] — `AddKeyedScoped<IOrderProcessor>(OrderType, ...)`, `OrderProcessorFactory` registered Scoped (never Singleton), ambient scoped `IServiceProvider`, no interface on the factory
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-4 — OrderStatus transitions + notifications are BLL-orchestrated] — why `IOrderProcessor` must not set `Status` to `Confirmed` itself
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-5 — DI lifetimes: scoped-per-operation, Singleton reserved for config] — `OrderProcessorFactory` Scoped, never a captured root provider
- [Source: _bmad-output/implementation-artifacts/2-2-pricing-strategy-order-total-calculation.md] — `IPricingStrategy`/`StandardPricingStrategy` this story's processors consume; the AD-11-vs-AD-7 registration contrast anticipated there; the tautological-assertion lesson this story's `RushOrderProcessorTests` applies
- [Source: _bmad-output/implementation-artifacts/2-1-order-orderitem-domain-repository.md] — `Order`/`OrderItem` Domain shape `OrderDto`/`CreateOrderRequest` mirror; confirms `Order` has no persisted `Total` column

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 7 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 38, Skipped: 0, Total: 38 (34 prior + 4 new).
- **Code review (2026-08-10):** `dotnet build`: 0 Warning(s), 0 Error(s). `dotnet test`: Passed! Failed: 0, Passed: 39, Skipped: 0, Total: 39 (38 prior + 1 new test, plus assertions added to 2 existing tests).

### Completion Notes List

- `CreateOrderRequest`/`OrderDto` added to `OrderFlow.BLL`. `OrderDto.Total` is a derived, never-persisted field (no matching `Order` entity column). `OrderDto.Id`/`Status` are deliberate placeholders (`0`/`Unspecified`) in this story — see Dev Notes on why (no persistence yet, and `IOrderProcessor` must not set `Confirmed` status itself per AD-4).
- `IOrderProcessor`/`StandardOrderProcessor`/`RushOrderProcessor` added. Both processors take only `IPricingStrategy` as a dependency in this story — this story deliberately does **not** implement stock validation, persistence, inventory decrement, or status transition (that's Story 2.5, extending these same classes). `RushOrderProcessor` applies a named `RushSurchargeRate = 0.10m` constant on top of the base total; `StandardOrderProcessor` applies it unmodified.
- `OrderProcessorFactory` added (no interface, per AD-7's own wording) — constructor-injected with the ambient scoped `IServiceProvider`, `Create(OrderType)` wraps `GetRequiredKeyedService<IOrderProcessor>`. Added `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.10 to `OrderFlow.BLL.csproj` (its first-ever package reference) for the keyed-resolution extension method.
- `Program.cs` composition root registers both processors via `AddKeyedScoped<IOrderProcessor, ...>(OrderType.Standard/.Rush)` and `OrderProcessorFactory` via plain `AddScoped` — a deliberately different DI shape from Story 2.2's single non-keyed `IPricingStrategy` registration (AD-7 vs. AD-11). `ValidateOnBuild`/`ValidateScopes` (Story 1.1) passed cleanly against the new keyed registrations at build time.
- `OrderProcessorFactoryTests` (2 tests) added: builds a real `ServiceCollection`/`ServiceProvider` (first `OrderFlow.Tests` file to do so; added `Microsoft.Extensions.DependencyInjection` 10.0.10 to `OrderFlow.Tests.csproj`) and asserts `Create(OrderType.Standard)`/`Create(OrderType.Rush)` resolve to the correct concrete processor type (AC #3).
- `StandardOrderProcessorTests`/`RushOrderProcessorTests` (1 test each) added, both using a real `StandardPricingStrategy` (no mocking — it's pure and already tested). `RushOrderProcessorTests` computes its expected value independently (`baseTotal * 1.10m`) rather than mirroring the production expression, applying the lesson from Story 2.2's code review.
- No new `UNVERIFIED-ENVIRONMENT` gaps — this story is pure `OrderFlow.BLL` logic plus DI wiring, fully verifiable on macOS (build and tests both succeeded locally).
- No `OrderFlow.Domain`/`OrderFlow.DAL` file touched; only `Program.cs` changed in `OrderFlow.Presentation` (the keyed registrations + factory registration) — confirmed via File List below. No `OrderDto`/`IOrderService` naming collision with Story 2.1's explicit "don't invent these" scope note — `OrderDto` here is this story's own, epics-mandated type, not the `IOrderService` that story explicitly avoided.
- **Code review (2026-08-10) — 5 patches applied, 0 deferred, 8 dismissed:** the review's three parallel layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor) independently re-verified this story's `ConfirmAsync` scope-narrowing decision against epics.md directly and confirmed it's well-supported, not a misreading. Real findings: `RushOrderProcessor`'s total lacked currency rounding (a 10% surcharge on a 2-decimal base total can produce 3-decimal sub-cent values — fixed with `Math.Round(..., 2, MidpointRounding.AwayFromZero)`); both processors aliased the caller's mutable `Items` list into a nominally-read-only DTO property (fixed with a defensive `.ToList()` copy); the deliberate `OrderDto.Id`/`Status` placeholder values had no regression test locking them down (added); `IOrderProcessor`/`OrderProcessorFactory.Create`'s scope/throwing contracts were undocumented (added doc comments, plus a test locking down the unmapped-`OrderType` throw). Eight findings dismissed as premature abstraction, already-accepted/deferred categories from prior stories, or already-established precedent (e.g. Story 2.2's "empty collection sums to 0m, not an error"). Test count 38 → 39, all passing; `dotnet build`/`dotnet test` re-verified green (0 warnings) after all changes.

### File List

- `OrderFlow/OrderFlow.BLL/CreateOrderRequest.cs` (new)
- `OrderFlow/OrderFlow.BLL/OrderDto.cs` (new)
- `OrderFlow/OrderFlow.BLL/IOrderProcessor.cs` (new; modified during code review: added doc comment on current-scope contract)
- `OrderFlow/OrderFlow.BLL/StandardOrderProcessor.cs` (new; modified during code review: defensive copy of `Items`)
- `OrderFlow/OrderFlow.BLL/RushOrderProcessor.cs` (new; modified during code review: rounded `Total` to currency precision, defensive copy of `Items`)
- `OrderFlow/OrderFlow.BLL/OrderProcessorFactory.cs` (new; modified during code review: added doc comment on `Create`'s throwing contract)
- `OrderFlow/OrderFlow.BLL/OrderFlow.BLL.csproj` (modified: `+ Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.10)
- `OrderFlow/OrderFlow.Presentation/Program.cs` (modified: `+ using OrderFlow.Domain;`, `+` keyed `IOrderProcessor` registrations, `+ AddScoped<OrderProcessorFactory>()`)
- `OrderFlow/OrderFlow.Tests/OrderFlow.Tests.csproj` (modified: `+ Microsoft.Extensions.DependencyInjection` 10.0.10)
- `OrderFlow/OrderFlow.Tests/OrderProcessorFactoryTests.cs` (new)
- `OrderFlow/OrderFlow.Tests/StandardOrderProcessorTests.cs` (new)
- `OrderFlow/OrderFlow.Tests/RushOrderProcessorTests.cs` (new)

## Change Log

- 2026-08-10: Implemented Story 2.3 — `IOrderProcessor`/`StandardOrderProcessor`/`RushOrderProcessor` (10% rush surcharge over `IPricingStrategy`'s base total), `OrderProcessorFactory` resolving via keyed DI per AD-7, `CreateOrderRequest`/`OrderDto` types. `dotnet build` green across all 7 projects with 0 warnings; `dotnet test` 38/38 passed.
- 2026-08-10: Code review applied — rounded `RushOrderProcessor`'s total to currency precision (closing a sub-cent-value bug), defensively copied `Items` in both processors, documented and locked down `IOrderProcessor`'s scope contract and `OrderProcessorFactory.Create`'s throwing behavior, and added a regression test for the deliberate `OrderDto.Id`/`Status` placeholder values. 8 findings dismissed. `dotnet build`/`dotnet test` re-verified green (39/39) after all changes.
