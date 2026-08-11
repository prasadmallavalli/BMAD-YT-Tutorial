---
baseline_commit: NO_VCS
---

# Story 4.2: OCP Exhibit Pair (Before/After)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a viewer studying the codebase,
I want a Before/After exhibit demonstrating an Open/Closed Principle violation and its refactor via the Strategy pattern,
so that I can see why the real app's `IPricingStrategy` design matters.

## Acceptance Criteria

1. **Given** `OrderFlow.Exhibits/Before/Ocp`, **When** reviewed, **Then** it contains a pricing calculator using a switch/if-else chain on a discount-type enum, requiring modification to add a new discount type.
2. **And Given** `OrderFlow.Exhibits/After/Ocp`, **When** reviewed, **Then** the same scenario is refactored using the Strategy pattern, where adding a new discount type requires no changes to existing classes — mirroring AD-11.
3. **And Given** both exhibits, **When** run, **Then** each produces equivalent pricing output for the same inputs, independently runnable.

## Tasks / Subtasks

- [x] Task 1: Extend the shared `Program.cs` dispatch with two more cases (AC: #1, #2, #3)
  - [x] Story 4.1 built `OrderFlow.Exhibits/Program.cs` specifically so later exhibit pairs extend it rather than invent a new entry point — this story is that extension, not a new design decision.
  - [x] Add `case "before-ocp": BeforeOcpRunner.Run(); break;` and `case "after-ocp": AfterOcpRunner.Run(); break;` to the existing `switch`. Add both `using` directives (`OrderFlow.Exhibits.Before.Ocp`, `OrderFlow.Exhibits.After.Ocp`). Update the `default` usage-text case to list all four exhibits now available (`before-srp`, `after-srp`, `before-ocp`, `after-ocp`).
  - [x] No changes to `OrderFlow.Exhibits.csproj` — it already has `OutputType=Exe` and the one permitted `ProjectReference` to `OrderFlow.Domain` (Story 4.1); this story needs nothing beyond `OrderFlow.Domain.OrderItem`, already available.
- [x] Task 2: `OrderFlow.Exhibits/Before/Ocp` — the OCP-violating switch/if-else pricing calculator (AC: #1)
  - [x] **New design decision this story must make**: AD-8 requires real Domain types for things Domain already models (`Order`, `OrderItem`) — it does not forbid a brand-new, exhibit-only concept that Domain has no equivalent of. `DiscountType` is exactly that: Domain has no discount concept at all (Epic 2 locked `StandardPricingStrategy` as a no-discount, sum-only calculation — see `_bmad-output/planning-artifacts/epics.md` Epic 2's "Epics-level decisions" note), so defining `DiscountType` locally in `Before/Ocp` isn't redefining an existing Domain type, it's introducing a new one Domain was never asked to own.
  - [x] `DiscountType.cs` (namespace `OrderFlow.Exhibits.Before.Ocp`): `public enum DiscountType { None, Percentage, FlatAmount }`.
  - [x] `PricingCalculator.cs` (same namespace): `public decimal CalculateTotal(IEnumerable<OrderItem> items, DiscountType discountType, decimal discountValue)` — computes `var baseTotal = items.Sum(i => i.Quantity * i.UnitPriceAtOrder);` (mirrors `StandardPricingStrategy.CalculateTotal`'s real logic, Story 2.2 — same formula, but over `OrderFlow.Domain.OrderItem` directly since Exhibits cannot reference `OrderFlow.BLL`'s `OrderItemDto`, per AD-8), then a `switch (discountType)` applying: `None` → `baseTotal` unchanged; `Percentage` → `baseTotal - (baseTotal * discountValue / 100m)`; `FlatAmount` → `baseTotal - discountValue`; clamp the result to `0` if negative in all cases. Adding a fourth `DiscountType` member requires adding a new `case` here — that's the violation AC #1 names.
  - [x] `BeforeOcpRunner.cs` (same namespace): `public static void Run()` — prints `"=== Before: OCP Violation ==="`, builds two sample `OrderItem`s (`Quantity = 2, UnitPriceAtOrder = 25.00m` and `Quantity = 1, UnitPriceAtOrder = 30.00m` — base total `80.00`), and prints three `CalculateTotal` results in this order: `(DiscountType.None, 0)`, `(DiscountType.Percentage, 10)`, `(DiscountType.FlatAmount, 5)`, each as `"[Pricing] {label}: {total:0.00}"` with labels `"No discount"` / `"Percentage discount (10%)"` / `"Flat discount ($5)"`.
- [x] Task 3: `OrderFlow.Exhibits/After/Ocp` — the Strategy-pattern refactor (AC: #2, #3)
  - [x] `IDiscountStrategy.cs` (namespace `OrderFlow.Exhibits.After.Ocp`): `public interface IDiscountStrategy { decimal Apply(decimal baseTotal); }`.
  - [x] `NoDiscountStrategy.cs`: `Apply` returns `baseTotal` unchanged.
  - [x] `PercentageDiscountStrategy.cs`: constructor takes `decimal percent`; `Apply` returns `baseTotal - (baseTotal * percent / 100m)`.
  - [x] `FlatAmountDiscountStrategy.cs`: constructor takes `decimal amount`; `Apply` returns `baseTotal - amount`.
  - [x] `PricingCalculator.cs` (same namespace, same class name as Before's — different namespace, one shared vocabulary): `public decimal CalculateTotal(IEnumerable<OrderItem> items, IDiscountStrategy discountStrategy)` — same `baseTotal` formula as Before, then `var total = discountStrategy.Apply(baseTotal); return total < 0 ? 0 : total;`. Adding a new discount type from here on is a new `IDiscountStrategy` implementation — **zero changes to this class or any existing strategy**, which is AC #2's "no changes to existing classes" and the direct mirror of AD-11's single-registration/swap-by-adding-a-class shape.
  - [x] `AfterOcpRunner.cs` (same namespace): `public static void Run()` — prints `"=== After: OCP Refactor ==="`, builds the **same two sample `OrderItem`s** as `BeforeOcpRunner`, and prints three results using `new NoDiscountStrategy()`, `new PercentageDiscountStrategy(10)`, `new FlatAmountDiscountStrategy(5)` in that order, with **identical** `"[Pricing] {label}: {total:0.00}"` lines to Before's (same labels, same values) — confirms AC #3's "equivalent pricing output."
- [x] Task 4: Verify end-to-end (AC: #1, #2, #3)
  - [x] `dotnet build` succeeds for the whole solution (all 8 projects) — 0 errors, 0 warnings.
  - [x] `dotnet run --project OrderFlow.Exhibits -- before-ocp` and `... -- after-ocp` both run standalone and print **identical** `[Pricing]` lines (only the `=== ... ===` header differs) — `80.00` base, `72.00` after 10% (`80 - 8`), `75.00` after flat `$5`.
  - [x] Re-run `dotnet run --project OrderFlow.Exhibits -- before-srp` / `after-srp` to confirm Story 4.1's exhibits still work unchanged (regression check on the shared `Program.cs`).
  - [x] `dotnet run --project OrderFlow.Exhibits` (no args) prints updated usage text listing all four exhibits.
  - [x] Confirm `OrderFlow.Presentation`/`OrderFlow.BLL`/`OrderFlow.DAL`/`Program.cs` (main app) and `OrderFlow.Domain` are untouched — this story only adds files under `OrderFlow.Exhibits/Before/Ocp` and `.../After/Ocp`, plus extending the existing `OrderFlow.Exhibits/Program.cs`. Confirm via File List below.
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` still passes at 82/82 (unchanged — Exhibits remain outside the app's test net, per Story 4.1's precedent).

### Review Findings

Reviewed together with Stories 4.1/4.3 (2026-08-11). Acceptance Auditor confirmed AC #1-#3 hold exactly (hand-computed both `PricingCalculator`s against the sample `OrderItem`s: `80.00` base, `72.00` at 10%, `75.00` at flat $5, identical on both sides).

- [x] [Review][Patch] `BeforeOcpRunner`/`AfterOcpRunner` format decimals with `{value:0.00}` using the current thread's culture rather than `CultureInfo.InvariantCulture` — output separators (`72.00` vs `72,00`) would vary by machine locale, undermining the "comparable console transcript" this exhibit exists to produce [OrderFlow.Exhibits/Before/Ocp/BeforeOcpRunner.cs, OrderFlow.Exhibits/After/Ocp/AfterOcpRunner.cs]

## Dev Notes

- **`DiscountType`/`IDiscountStrategy` are new, exhibit-only vocabulary — not toy re-definitions of a Domain concept.** AD-8's "real Domain types, no toy types" rule targets things Domain already models (`Order`, `OrderItem`, `OrderStatus`, `OrderType`) — `DiscountType` isn't one of those; Domain and `StandardPricingStrategy` (Story 2.2) deliberately have zero discount concept (Epic 2 locked "no discount" as the actual app's scope). Both `PricingCalculator`s still build on real `OrderFlow.Domain.OrderItem` instances for the line items themselves, satisfying the actual "use real Domain types" intent.
- **Why `OrderItem`, not `OrderItemDto`.** The real `StandardPricingStrategy.CalculateTotal` (Story 2.2, `OrderFlow.BLL`) takes `IEnumerable<OrderItemDto>` — but `OrderItemDto` lives in `OrderFlow.BLL`, which Exhibits cannot reference (AD-8's only permitted reference is `OrderFlow.Domain`). Both exhibit `PricingCalculator`s take `IEnumerable<OrderItem>` (the Domain entity) instead — same `Quantity * UnitPriceAtOrder` formula, just over the Domain type Exhibits is actually allowed to see.
- **This extends Story 4.1's `Program.cs`, it doesn't touch its design.** The dispatch-by-argument entry point, "no DI container," and "one shared project, `Before`/`After` root folders" decisions were already made in Story 4.1 — this story is exactly the extension that design anticipated (two more `case` arms), and Story 4.3 (DIP) will do the same again.
- **"Equivalent output" (AC #3) means line-for-line identical, same as Story 4.1's SRP pair.** Both Runners build the same two sample `OrderItem`s in the same order and both `PricingCalculator`s are called with matching discount values (`10` / `5`) so every `[Pricing]` line matches exactly; only the `=== Before/After ... ===` header differs.
- **No `OrderFlow.Tests` coverage, same as Story 4.1.** Epic 4's stories have no "extend `OrderFlow.Tests`" task in the epics file; Exhibits stay outside the app's test net per AD-8 ("opened and run independently for interview purposes only"). Task 4 verifies by running both and reading console output.
- **Clamping negative totals to `0`** in both `PricingCalculator`s is a defensive floor (e.g. a flat discount larger than the base total) — not required by any AC's stated inputs (the sample data never goes negative), but cheap and keeps both sides symmetric; omitting it from only one side would itself be a subtle behavioral mismatch between Before/After.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.Exhibits/
    Program.cs                         # modified: + before-ocp/after-ocp cases, updated usage text
    Before/
      Ocp/
        DiscountType.cs                 # new
        PricingCalculator.cs            # new: switch/if-else OCP violation
        BeforeOcpRunner.cs              # new
    After/
      Ocp/
        IDiscountStrategy.cs            # new
        NoDiscountStrategy.cs           # new
        PercentageDiscountStrategy.cs   # new
        FlatAmountDiscountStrategy.cs   # new
        PricingCalculator.cs            # new: Strategy-pattern composition
        AfterOcpRunner.cs               # new
```

`OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.BLL`/`OrderFlow.Presentation`/`OrderFlow.Presentation.Tests`/`OrderFlow.Tests` are untouched by this story. `OrderFlow.Exhibits/Before/Srp`, `.../After/Srp`, and `OrderFlow.Exhibits.csproj` (Story 4.1) are also untouched — only `Program.cs` gains two lines of dispatch.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.2: OCP Exhibit Pair (Before/After)] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2: Order Creation, Pricing & Inventory] — "Epics-level decisions... Pricing Strategy is a single `StandardPricingStrategy`... no discount" — confirms `DiscountType` has no existing Domain/BLL equivalent to redefine
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-11 — Pricing/Discount Strategy: single composition-root registration] — the real app's Strategy-shaped `IPricingStrategy` design this exhibit pair explains "why it matters" for
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-8 — Before/After Exhibits isolated from the runtime DI graph] — the one-directional reference rule (Domain only) this story's `PricingCalculator` follows by using `OrderItem`, not `OrderItemDto`
- [Source: OrderFlow/OrderFlow.BLL/IPricingStrategy.cs, OrderFlow/OrderFlow.BLL/StandardPricingStrategy.cs] — the real `CalculateTotal` formula (`Quantity * UnitPriceAtOrder` sum) both exhibit `PricingCalculator`s mirror
- [Source: OrderFlow/OrderFlow.Exhibits/Program.cs] — Story 4.1's dispatch entry point this story extends with two more `case` arms
- [Source: _bmad-output/implementation-artifacts/4-1-srp-exhibit-pair-before-after.md] — precedent for the shared-`Program.cs`-with-dispatch design, the "no `OrderFlow.Tests` coverage" decision, and the "identical sample inputs, line-for-line equivalent output, header differs" verification pattern this story repeats exactly

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 8 projects, after all tasks): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet run --project OrderFlow.Exhibits --no-build -- before-ocp`:
  ```
  === Before: OCP Violation ===
  [Pricing] No discount: 80.00
  [Pricing] Percentage discount (10%): 72.00
  [Pricing] Flat discount ($5): 75.00
  ```
- `dotnet run --project OrderFlow.Exhibits --no-build -- after-ocp`:
  ```
  === After: OCP Refactor ===
  [Pricing] No discount: 80.00
  [Pricing] Percentage discount (10%): 72.00
  [Pricing] Flat discount ($5): 75.00
  ```
  Confirms AC #3: identical output apart from the header line.
- `dotnet run --project OrderFlow.Exhibits --no-build -- before-srp` / `-- after-srp`: re-verified unchanged from Story 4.1 (regression check on the shared `Program.cs` dispatch).
- `dotnet run --project OrderFlow.Exhibits --no-build` (no args): printed updated usage text listing all four exhibits (`before-srp`, `after-srp`, `before-ocp`, `after-ocp`).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` (final): Passed! Failed: 0, Passed: 82, Skipped: 0, Total: 82 — unchanged from before this story.

### Completion Notes List

- `OrderFlow.Exhibits/Program.cs` (Story 4.1) extended with `before-ocp`/`after-ocp` cases and updated usage text — no changes to its dispatch design, `OrderFlow.Exhibits.csproj`, or the existing SRP exhibits.
- `Before/Ocp/DiscountType.cs`, `PricingCalculator.cs`, `BeforeOcpRunner.cs`: switch/if-else OCP violation over real `OrderFlow.Domain.OrderItem`s, mirroring `StandardPricingStrategy`'s base-total formula.
- `After/Ocp/IDiscountStrategy.cs` + `NoDiscountStrategy.cs`/`PercentageDiscountStrategy.cs`/`FlatAmountDiscountStrategy.cs` + `PricingCalculator.cs` + `AfterOcpRunner.cs`: Strategy-pattern refactor mirroring AD-11's real `IPricingStrategy` shape — adding a new discount type is a new class, zero changes to `PricingCalculator` or existing strategies.
- Verified both new exhibits run standalone and produce line-for-line identical `[Pricing]` output for identical sample `OrderItem`s (`80.00` base, `72.00` at 10%, `75.00` at flat `$5`) — confirms AC #3 concretely. Re-ran Story 4.1's `before-srp`/`after-srp` to confirm no regression on the shared entry point.
- `dotnet build` is green across all 8 projects with 0 warnings. `OrderFlow.Tests` remains 82/82 passing, untouched — no test coverage added for Exhibits, per Story 4.1's established precedent.
- No `OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.BLL`/`OrderFlow.Presentation`/`OrderFlow.Presentation.Tests`/`OrderFlow.Tests`/`OrderFlow.Exhibits.csproj`/`Before/Srp`/`After/Srp` file touched — confirmed via File List below; every change is either a new file under `OrderFlow.Exhibits/Before/Ocp` or `.../After/Ocp`, or two added `case` arms plus usage text in the existing `Program.cs`.

### File List

- `OrderFlow/OrderFlow.Exhibits/Program.cs` (modified: + `before-ocp`/`after-ocp` cases, updated usage text)
- `OrderFlow/OrderFlow.Exhibits/Before/Ocp/DiscountType.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/Before/Ocp/PricingCalculator.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/Before/Ocp/BeforeOcpRunner.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Ocp/IDiscountStrategy.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Ocp/NoDiscountStrategy.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Ocp/PercentageDiscountStrategy.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Ocp/FlatAmountDiscountStrategy.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Ocp/PricingCalculator.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Ocp/AfterOcpRunner.cs` (new)

## Change Log

- 2026-08-11: Implemented Story 4.2 — extended `OrderFlow.Exhibits/Program.cs`'s dispatch with `before-ocp`/`after-ocp`; added `Before/Ocp` (switch/if-else `PricingCalculator` over a new exhibit-only `DiscountType` enum) and `After/Ocp` (Strategy-pattern `PricingCalculator` + `IDiscountStrategy` implementations, mirroring AD-11), verified to produce line-for-line equivalent pricing output for identical sample order items. `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 82/82 passed, unchanged.
- 2026-08-11: Code review (combined with Stories 4.1/4.3) — 0 AC violations (Acceptance Auditor hand-computed both `PricingCalculator`s against the sample data and confirmed identical output). 1 patch applied: `BeforeOcpRunner`/`AfterOcpRunner` now format decimals with `CultureInfo.InvariantCulture` instead of the current thread's culture, so the console transcript stays comparable regardless of machine locale. Re-verified `dotnet build` green and both runners still produce identical `80.00`/`72.00`/`75.00` output.
