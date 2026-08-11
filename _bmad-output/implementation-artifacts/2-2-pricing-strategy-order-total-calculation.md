---
baseline_commit: NO_VCS
---

# Story 2.2: Pricing Strategy — Order Total Calculation

Status: done

## Story

As a developer,
I want a swappable Pricing/Discount Strategy applied to an Order's line items,
so that the total is calculated consistently and can be changed by swapping one composition-root registration.

## Acceptance Criteria

1. **Given** `OrderFlow.BLL`, **When** implemented, **Then** `IPricingStrategy` (`CalculateTotal(IEnumerable<OrderItemDto>) => decimal`) exists with one concrete `StandardPricingStrategy` (sums `Quantity × UnitPriceAtOrder`, no discount), registered Scoped at the composition root per AD-11 (single registration, no keyed dispatch).
2. **And Given** `IPricingStrategy`, **When** a different pricing rule is needed later, **Then** swapping is a one-line change at the composition root — no changes required in `OrderService` or Presentation.
3. **And Given** `OrderFlow.Tests`, **When** complete, **Then** `StandardPricingStrategy` is covered by tests over multiple line items with varying quantities/prices.

## Tasks / Subtasks

- [x] Task 1: `OrderItemDto` (AC: #1)
  - [x] `OrderFlow.BLL/OrderItemDto.cs`: `ProductId` (int), `Quantity` (int), `UnitPriceAtOrder` (decimal) — only the fields `CalculateTotal` needs to price a line item. No `Id`/`OrderId`: those are persistence-only concerns on the `OrderItem` Domain entity (Story 2.1) that a pre-persist pricing calculation has no use for. This is the first `OrderFlow.BLL` type this story adds — nothing here references `OrderFlow.Domain` or `OrderFlow.DAL` (AD-1: `OrderFlow.BLL` may reference `OrderFlow.Domain`, but this DTO doesn't need to — plain primitives suffice, same as `ProductDto`)
- [x] Task 2: `IPricingStrategy` + `StandardPricingStrategy` (AC: #1)
  - [x] `OrderFlow.BLL/IPricingStrategy.cs`: `decimal CalculateTotal(IEnumerable<OrderItemDto> items)` — **exact signature from epics.md, returns plain `decimal`, not `Result<decimal>`.** This is a pure, deterministic arithmetic calculation with no failure mode (unlike `ProductService`/`InventoryService`'s `Result<T>`-returning methods, which validate untrusted input) — don't wrap it in `Result<T>`, the AC is explicit about the return type
  - [x] `OrderFlow.BLL/StandardPricingStrategy.cs`: `public class StandardPricingStrategy : IPricingStrategy` — `CalculateTotal` sums `item.Quantity * item.UnitPriceAtOrder` across all `items`; an empty collection sums to `0m` (a valid, correct result, not an error — don't add empty-collection validation). No discount logic of any kind — that's this concrete strategy's whole definition per the AC and per epics.md's Epic 2 "Epics-level decisions" note ("Pricing Strategy is a single `StandardPricingStrategy`... no discount")
- [x] Task 3: Composition root registration (AC: #1, #2)
  - [x] `OrderFlow.Presentation/Program.cs` `ConfigureServices`: `services.AddScoped<IPricingStrategy, StandardPricingStrategy>();` — per AD-11, exactly one `IPricingStrategy` implementation registered Scoped, no keyed dispatch (unlike Story 2.3's upcoming `OrderProcessorFactory`, which *does* use keyed DI — don't confuse the two patterns). This single registration line is also what makes AC #2 true structurally: swapping strategies later means changing only this one line, nothing in `OrderFlow.BLL`/`OrderFlow.Presentation` needs to change to support it — no separate test can meaningfully verify "one-line swappability" beyond the interface existing and being consumed only through DI, which this registration already guarantees
- [x] Task 4: `OrderFlow.Tests` — `StandardPricingStrategy` tests (AC: #3)
  - [x] `OrderFlow.Tests/StandardPricingStrategyTests.cs`: no mocking needed (unlike every other `OrderFlow.Tests` file so far) — `StandardPricingStrategy` has zero dependencies, construct it directly. Cover: (a) multiple line items with varying quantities/prices summing correctly (e.g. 2×`9.99m` + 1×`19.99m` + 3×`4.50m` = expected total), (b) a single line item, (c) an empty collection returning `0m`. Use `[Theory]`/`[InlineData]` or explicit `[Fact]`s per the existing `OrderFlow.Tests` style (see `ProductServiceTests`/`InventoryServiceTests` for the project's `[Theory]` conventions)
- [x] Task 5: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution (all 7 projects) — 0 errors, 0 warnings
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` passes, including the new `StandardPricingStrategyTests`, and all pre-existing tests still pass (31 prior + new) — **confirmed: 34/34 passed (31 prior + 3 new)**
  - [x] Confirm no `OrderFlow.Domain`/`OrderFlow.DAL` file was touched, and the only `OrderFlow.Presentation` change is the one `Program.cs` registration line — this story is `OrderFlow.BLL` + `OrderFlow.Tests` (+ 1 line in `Program.cs`) only, per its own AC (no `OrderDto`/`IOrderService`/`OrderProcessor` — those belong to Stories 2.3–2.5) — **confirmed via File List below**

### Review Findings

- [x] [Review][Patch] `CalculateTotal_WithMultipleLineItems_SumsQuantityTimesUnitPrice` asserted against `2 * 9.99m + 1 * 19.99m + 3 * 4.50m` — a mirror of the production expression's own structure (quantity × price, summed), which wouldn't catch a swapped-operand or off-by-one summation bug. Fixed: replaced with the independently-computed literal `53.47m` (matching the single-line-item test's already-correct pattern). [`OrderFlow.Tests/StandardPricingStrategyTests.cs`]
- [x] [Review][Patch] The Architecture Spine's "Deferred" section still listed "Concrete discount rule(s) (PRD §8 Q1) — Epics/Stories choose the actual `IPricingStrategy` implementations" as open, even though this story resolves it (`StandardPricingStrategy`, no discount). Fixed: struck through and annotated "Resolved in Story 2.2." [`_bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md:193`]
- [x] [Review][Patch] `IPricingStrategy.CalculateTotal`'s "assumes pre-validated input, no discount logic" precondition existed only in this story's Markdown, not in the shipped code itself — a reader of just `IPricingStrategy.cs` had no way to know negative/invalid input isn't validated. Fixed: added a doc comment on the interface method stating the precondition. [`OrderFlow.BLL/IPricingStrategy.cs`]
- [x] [Review][Defer] AC #2 ("swapping strategies later is a one-line change") has no automated regression guard — nothing (no architecture-fitness test) stops a future story from quietly adding a second `IPricingStrategy` registration or keyed dispatch that violates AD-11's "single registration" rule, and build/tests would stay green throughout. Same category as the already-deferred "no architecture-fitness test (e.g. NetArchTest) enforcing AD-1's layer-direction rule" from Story 1.1 — out of proportion to add just for this story. [`OrderFlow.Presentation/Program.cs`] — deferred, same category as an existing gap
- Dismissed as noise / out of scope / already covered (10): no null-argument guard on `CalculateTotal` and no guard against a null element in the collection (matches this codebase's established "trust internal callers, rely on nullable-reference-types" convention — same reasoning Story 2.1's review applied to `OrderRepository.AddAsync` lacking a null-guard); `OverflowException` on an unrealistically large `Quantity` (falls to the existing global `Application.ThreadException` handler by design — not a gap, that handler exists for exactly this class of unexpected failure); negative `Quantity`/`UnitPriceAtOrder` producing a nonsensical total (explicitly disclosed, deliberate scope decision in this story's own Dev Notes, confirmed intentional); `IEnumerable<OrderItemDto>` re-enumeration-safety risk (speculative about hypothetical future caller misuse, no concrete defect in this story's code); AD-11 rationale duplicated across the Spine/`Program.cs` comment/class comment (matches this codebase's established pattern of duplicating architecture rationale in code comments everywhere, not a regression); tests using `[Fact]` instead of `[Theory]` (the story's own task text explicitly permitted either); `OrderItemDto.ProductId` currently unused (cheap, reasonable bet on Story 2.5's line-item grid needing it; removing now would be pure churn); DI registration not exercised by an automated test (duplicate of the already-accepted, already-deferred Story 1.1 DI-resolution-smoke-test gap, blocked by the same Windows-only `OrderFlow.Presentation` platform limitation); `StandardPricingStrategy` registered `Scoped` despite being stateless (spec-compliant per AD-11, not a defect); no XML doc comments on the new BLL types (pre-existing codebase-wide pattern, not a regression).

## Dev Notes

- **This story is pure `OrderFlow.BLL` — no Domain, no DAL, no repository/`IUnitOfWork` involvement.** Unlike Story 2.1 (Domain + DAL) or Story 1.2/1.4 (Domain + DAL + BLL service in one story), this story adds exactly one interface, one concrete implementation, and one DTO — all pure, dependency-free BLL types. Don't reach for `IUnitOfWork` or any repository; `CalculateTotal` takes its input as a plain parameter and has nothing to fetch or persist.
- **Do not invent `OrderDto` or `IOrderService` here.** Per Story 2.1's own Dev Notes, Order's BLL surface is deliberately split across this story (`IPricingStrategy`), Story 2.3 (`IOrderProcessor`/`OrderProcessorFactory`), and Story 2.4 (`IOrderStatusService`/`INotifier`) — no single `IOrderService` is even named across any of them. `OrderItemDto` (Task 1) is the only new BLL type this story needs.
- **`CalculateTotal` assumes valid, already-checked input.** Quantity/price validation (e.g. rejecting a negative `Quantity`) is explicitly out of scope for both this story and Story 2.1 (see Story 2.1 Dev Notes: "stock-sufficiency checks... and total calculation... are BLL concerns for later stories (2.2, 2.5)" — read as "the calculation itself lives here," not "input validation lives here"). Line-item validation belongs to Story 2.5's order-creation flow, which validates before ever calling `CalculateTotal`. Don't add defensive checks (e.g. throwing on negative `Quantity`) that no AC asks for.
- **No rounding logic needed.** `UnitPriceAtOrder` is already persisted at `decimal(18,2)` precision (Story 2.1), and `Quantity` is a plain `int` — `Quantity × UnitPriceAtOrder` and the sum of several such products stay at ≤2 decimal places by construction. Don't add `Math.Round` calls; there's nothing to round.
- **`IPricingStrategy`'s registration pattern is deliberately different from Story 2.3's upcoming `OrderProcessorFactory`.** AD-11 (this story) mandates exactly one plain `AddScoped<IPricingStrategy, ConcreteStrategy>()` registration — no keyed dispatch, no runtime selection. AD-7 (Story 2.3) mandates `AddKeyedScoped<IOrderProcessor>(OrderType, ...)` for `IOrderProcessor` instead, because processing behavior *does* vary per-`OrderType` while pricing (per this Epic's own "Epics-level decisions") does not. Don't carry keyed-DI patterns into this story, and don't let this story's simplicity leak into Story 2.3's factory.
- **Naming conventions (unchanged from prior stories):** `IXxx` interfaces, `XxxDto` DTOs — `IPricingStrategy`/`StandardPricingStrategy`/`OrderItemDto` all follow the existing table in the Architecture Spine's Consistency Conventions.
- **Data & formats:** `Quantity` is `int`; `UnitPriceAtOrder` and the returned total are `decimal` (never `float`/`double`, per the Consistency Conventions table) — this is already how `Order.OrderItems`' matching Domain fields are typed (Story 2.1), so `OrderItemDto`'s field types are a direct match, not a new decision.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.BLL/
    OrderItemDto.cs             # new
    IPricingStrategy.cs         # new
    StandardPricingStrategy.cs  # new
  OrderFlow.Presentation/
    Program.cs                  # modified: + AddScoped<IPricingStrategy, StandardPricingStrategy>()
  OrderFlow.Tests/
    StandardPricingStrategyTests.cs  # new
```

`OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.Presentation.Tests` are untouched by this story.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 2.2: Pricing Strategy — Order Total Calculation] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2: Order Creation, Pricing & Inventory] — "Epics-level decisions (deferred by Architecture, locked here): Pricing Strategy is a single `StandardPricingStrategy` (sums `Quantity × UnitPriceAtOrder`, no discount)"
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-11 — Pricing/Discount Strategy: single composition-root registration] — exactly one `IPricingStrategy` implementation registered Scoped, no keyed dispatch
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-7 — Order Processor Factory over keyed DI services] — the contrasting keyed-DI pattern this story's registration must *not* follow (that's Story 2.3's job)
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Consistency Conventions] — `Result<T>` convention (why `CalculateTotal` deliberately doesn't use it), naming table, money-as-`decimal` rule
- [Source: _bmad-output/implementation-artifacts/2-1-order-orderitem-domain-repository.md] — `OrderItem.Quantity`/`UnitPriceAtOrder` field types this story's `OrderItemDto` mirrors; the Dev Notes establishing that Order's BLL surface is split across Stories 2.2/2.3/2.4 with no `IOrderService`

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 7 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 34, Skipped: 0, Total: 34 (31 prior + 3 new).
- **Code review (2026-08-10):** `dotnet build`: 0 Warning(s), 0 Error(s). `dotnet test`: Passed! Failed: 0, Passed: 34, Skipped: 0, Total: 34 (unchanged — one test's assertion was hardened, no new tests added).

### Completion Notes List

- `OrderItemDto` added to `OrderFlow.BLL` — `ProductId`/`Quantity`/`UnitPriceAtOrder` only, no `Id`/`OrderId` (those are `OrderItem`-entity persistence concerns this pre-persist DTO doesn't need).
- `IPricingStrategy.CalculateTotal(IEnumerable<OrderItemDto>) => decimal` and its sole implementation `StandardPricingStrategy` added, matching epics.md's exact signature (plain `decimal`, not `Result<decimal>` — this is a pure arithmetic calculation with no failure mode, unlike `ProductService`/`InventoryService`'s validating methods). `StandardPricingStrategy` sums `Quantity × UnitPriceAtOrder` with no discount logic; an empty collection correctly returns `0m`.
- `Program.cs` composition root registers `services.AddScoped<IPricingStrategy, StandardPricingStrategy>();` per AD-11 — a single, non-keyed registration, deliberately distinct from Story 2.3's upcoming keyed-DI `OrderProcessorFactory` pattern (AD-7). AC #2's "one-line swap" requirement is satisfied structurally by this registration shape; `ValidateOnBuild`/`ValidateScopes` (Story 1.1) passed cleanly against the new registration at build time.
- `StandardPricingStrategyTests` added: 3 new tests covering multiple line items with varying quantities/prices, a single line item, and an empty collection (AC #3). `StandardPricingStrategy` has zero dependencies, so no mocking was needed — the first `OrderFlow.Tests` file that doesn't touch `Moq`/`IUnitOfWork`.
- No new `UNVERIFIED-ENVIRONMENT` gaps — this story is pure `OrderFlow.BLL` logic plus one DI registration line, fully verifiable on macOS (build and tests both succeeded locally, no DB/migration involved).
- No `OrderFlow.Domain`/`OrderFlow.DAL` file touched; only `Program.cs` changed in `OrderFlow.Presentation` (the one registration line) — confirmed via File List below. No `OrderDto`/`IOrderService`/`OrderProcessor` invented, per this story's explicit scope boundary.
- **Code review (2026-08-10) — 3 patches applied, 1 deferred, 10 dismissed:** the review's three parallel layers (Blind Hunter, Edge Case Hunter, Acceptance Auditor — the latter found zero violations, a clean pass) surfaced a tautological test assertion (`CalculateTotal_WithMultipleLineItems...` asserted against an expression with the same shape as the production code, replaced with the independently-computed literal `53.47m`), a stale Architecture Spine "Deferred" entry (the "Concrete discount rule(s)" item this story actually resolves — struck through and annotated), and an undocumented precondition (`IPricingStrategy.CalculateTotal` now carries a doc comment stating it assumes pre-validated, non-null input). One item deferred to `deferred-work.md`: no architecture-fitness test guards AD-11's single-registration rule (same category as an already-deferred Story 1.1 gap). Ten findings dismissed as noise, already-accepted convention, or already covered by an existing deferred item (see Review Findings below for the full list — notably: no null-argument guards, matching this codebase's established NRT-reliant convention; decimal-overflow on unrealistic input, which is what the existing global exception handler is for). `dotnet build`/`dotnet test` re-verified green (34/34) after all changes.

### File List

- `OrderFlow/OrderFlow.BLL/OrderItemDto.cs` (new)
- `OrderFlow/OrderFlow.BLL/IPricingStrategy.cs` (new; modified during code review: added doc comment on `CalculateTotal`'s precondition)
- `OrderFlow/OrderFlow.BLL/StandardPricingStrategy.cs` (new)
- `OrderFlow/OrderFlow.Presentation/Program.cs` (modified: `+ AddScoped<IPricingStrategy, StandardPricingStrategy>()`)
- `OrderFlow/OrderFlow.Tests/StandardPricingStrategyTests.cs` (new; modified during code review: hardened one test's assertion to an independently-computed literal)
- `_bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md` (modified during code review: "Concrete discount rule(s)" Deferred entry struck through and marked resolved)
- `_bmad-output/implementation-artifacts/deferred-work.md` (modified during code review: 1 new item appended)

## Change Log

- 2026-08-10: Implemented Story 2.2 — `OrderItemDto`, `IPricingStrategy`/`StandardPricingStrategy` (sums `Quantity × UnitPriceAtOrder`, no discount), registered Scoped at the composition root per AD-11. `dotnet build` green across all 7 projects with 0 warnings; `dotnet test` 34/34 passed.
- 2026-08-10: Code review applied — hardened a tautological test assertion, resolved a stale Architecture Spine "Deferred" entry, and documented `CalculateTotal`'s input-validation precondition. 1 item deferred to `deferred-work.md`, 10 dismissed. `dotnet build`/`dotnet test` re-verified green (34/34) after all changes.
