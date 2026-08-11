---
baseline_commit: NO_VCS
---

# Story 4.1: SRP Exhibit Pair (Before/After)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a viewer studying the codebase,
I want a Before/After exhibit demonstrating a Single Responsibility Principle violation and its refactor,
so that I can see a concrete, runnable example of the principle.

## Acceptance Criteria

1. **Given** `OrderFlow.Exhibits/Before/Srp`, **When** reviewed, **Then** it contains a single class combining order validation, persistence, and notification logic (multiple reasons to change), independently runnable with no reference to the main app's DI graph (AD-8).
2. **And Given** `OrderFlow.Exhibits/After/Srp`, **When** reviewed, **Then** the same scenario is refactored into separate single-responsibility classes (e.g. `OrderValidator`, `OrderPersister`, `OrderNotifier`) composed together, producing equivalent output to the Before version.
3. **And Given** both exhibits, **When** run, **Then** each is runnable independently, without requiring the other or the main OrderFlow app.

## Tasks / Subtasks

- [x] Task 1: Make `OrderFlow.Exhibits` executable and give it a shared dispatch entry point (AC: #1, #2, #3)
  - [x] **This is a new design decision this story must make** — three exhibit pairs (SRP here, OCP in Story 4.2, DIP in Story 4.3) all live in the *same* `OrderFlow.Exhibits` project (per the Architecture Spine's Structural Seed — one `OrderFlow.Exhibits/` line, not six separate projects), but a single .NET assembly can only have one `Main`. The fix: one small top-level-statement `Program.cs` that dispatches on a command-line argument to the requested exhibit's `Run()` method — "independently runnable" (AC #3) means running Before never executes After's code path and never touches `OrderFlow.Presentation`/`BLL`/`DAL`, not that each needs a literal separate `.exe`. Story 4.2/4.3 extend this same `Program.cs` with two more `case` arms; they do not invent their own entry points.
  - [x] `OrderFlow.Exhibits/OrderFlow.Exhibits.csproj`: add `<OutputType>Exe</OutputType>` (it's been a bare library since Story 1.1 — nothing made it runnable yet). Add `<ProjectReference Include="..\OrderFlow.Domain\OrderFlow.Domain.csproj" />` — the one reference AD-8 explicitly permits ("`OrderFlow.Exhibits` **may** take a compile-time project reference to `OrderFlow.Domain`"). Add **no other** `ProjectReference` — `OrderFlow.Presentation`/`BLL`/`DAL` stay unreferenced, which is what AD-8 actually forbids (the *exhibits* referencing the *app*; the reverse direction, app referencing exhibits, was never done and stays that way).
  - [x] `OrderFlow.Exhibits/Program.cs`: top-level statements, `switch` on `args.Length > 0 ? args[0] : null` with cases `"before-srp"` → `BeforeSrpRunner.Run()` and `"after-srp"` → `AfterSrpRunner.Run()`; a `default` case prints usage listing both. No dependency on any DI container — this is deliberately the opposite of the main app's composition root (AD-1's composition-root exception is `Program.cs` in `OrderFlow.Presentation`; this is a completely separate, unrelated `Program.cs` in a different project with zero DI).
- [x] Task 2: `OrderFlow.Exhibits/Before/Srp` — the SRP-violating god-class (AC: #1)
  - [x] `OrderProcessor.cs` (namespace `OrderFlow.Exhibits.Before.Srp`): one class, one public `bool Process(Order order)` method, that does all three jobs inline, each commented with which "reason to change" it represents:
    - Validation: reject (return `false`, log `"[Validation] Order {order.Id} rejected: no line items."`) if `order.OrderItems.Count == 0`; reject (log `"[Validation] Order {order.Id} rejected: item {item.ProductId} has non-positive quantity."`) if any `OrderItem.Quantity <= 0`.
    - Persistence: append to a private `List<Order>` field, log `"[Persistence] Order {order.Id} saved. Total persisted: {count}."`.
    - Notification: log `"[Notification] Order {order.Id} confirmed for customer {order.CustomerId}."`.
    - Use `Console.WriteLine` for all three logs (this is a standalone console demo, not the main app — no `ILogger<T>`/DI here, see Task 1).
  - [x] `BeforeSrpRunner.cs` (namespace `OrderFlow.Exhibits.Before.Srp`): `public static void Run()` — prints a `"=== Before: SRP Violation ==="` header, constructs one `OrderProcessor`, builds two sample `Order`s using real `OrderFlow.Domain` types (never a locally redefined toy type, per AD-8) — one valid (`Id = 1`, `CustomerId = 100`, one `OrderItem` with `Quantity = 2`) and one invalid (`Id = 2`, `CustomerId = 200`, empty `OrderItems`) — and calls `Process` on each in that order.
- [x] Task 3: `OrderFlow.Exhibits/After/Srp` — the SRP refactor (AC: #2, #3)
  - [x] `OrderValidator.cs`: `public bool Validate(Order order, out string? error)` — identical rejection conditions as Before's inline checks, and **the exact same error text** (`"no line items."` / `"item {item.ProductId} has non-positive quantity."`) so Before/After output is comparable line-for-line per AC #2.
  - [x] `OrderPersister.cs`: `public void Save(Order order)` appending to a private `List<Order>` field, plus `public int Count { get; }` — same persistence behavior as Before, split into its own class.
  - [x] `OrderNotifier.cs`: `public void Notify(Order order)` — same notification behavior as Before, split into its own class.
  - [x] `OrderProcessor.cs` (namespace `OrderFlow.Exhibits.After.Srp` — same class name as Before's, different namespace, mirroring how the real app's `OrderProcessorFactory` exhibit-parallel already reuses one vocabulary): constructor-injected with `OrderValidator`, `OrderPersister`, `OrderNotifier` (plain `new`-ed by the Runner — no DI container, per Task 1's note); `public bool Process(Order order)` calls `_validator.Validate`, logs the same `"[Validation]..."`/`"[Persistence]..."`/`"[Notification]..."` messages as Before using the validator's `error` and the persister's `Count`, producing **byte-identical** per-order output to Before's `OrderProcessor`.
  - [x] `AfterSrpRunner.cs` (namespace `OrderFlow.Exhibits.After.Srp`): `public static void Run()` — prints a `"=== After: SRP Refactor ==="` header (the only line expected to differ from Before's run — see Dev Notes), constructs `OrderValidator`/`OrderPersister`/`OrderNotifier`, composes them into one `OrderProcessor`, builds the **same two sample Orders** as `BeforeSrpRunner` (same Ids/CustomerIds/quantities), and calls `Process` on each in the same order.
- [x] Task 4: Verify end-to-end (AC: #1, #2, #3)
  - [x] `dotnet build` succeeds for the whole solution (all 8 projects) — 0 errors, 0 warnings.
  - [x] `dotnet run --project OrderFlow.Exhibits -- before-srp` runs standalone and produces the expected validation/persistence/notification log lines for both sample orders.
  - [x] `dotnet run --project OrderFlow.Exhibits -- after-srp` runs standalone and produces **the same** log lines (only the `"=== ... ==="` header line differs) — confirms AC #2's "equivalent output."
  - [x] Confirm `OrderFlow.Presentation`/`OrderFlow.BLL`/`OrderFlow.DAL`/`Program.cs` (main app) are untouched, and neither references `OrderFlow.Exhibits` (AD-8's actual forbidden direction) — confirm via File List below.
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` still passes at 82/82 (unchanged by this story — Exhibits are intentionally outside the app's test net, see Dev Notes).

### Review Findings

Reviewed together with Stories 4.2/4.3 (2026-08-11) since all three share this story's `Program.cs`/`.csproj`. No SRP-specific patch/defer findings — see individual findings below for the shared `Program.cs` dispatch entry point this story created.

- [x] [Review][Defer] `Program.cs`'s dispatch is case-sensitive with no trimming and no `--help`/alias handling — a typo (`Before-Srp`, trailing space) silently falls through to the generic usage block instead of a targeted hint [OrderFlow.Exhibits/Program.cs] — deferred (minor interactive-UX polish, not required by any AC)
- [x] [Review][Defer] `Program.cs` never sets a non-zero exit code on invalid/missing arguments — the `default` case prints usage and still exits 0, indistinguishable from success to any script/CI wrapper [OrderFlow.Exhibits/Program.cs] — deferred (no automation invokes this project today, per its own "interview purposes only" isolation)

## Dev Notes

- **Why one shared `Program.cs` instead of one exe per exhibit**: the Structural Seed lists exactly one `OrderFlow.Exhibits/` project (not six). A single assembly can have only one entry point, so Task 1's arg-dispatch `Program.cs` is the mechanism that lets three exhibit pairs (this story's SRP, plus OCP/DIP in Stories 4.2/4.3) coexist in one project while each stays independently invocable — satisfying AC #3 without needing six separate `.csproj` files that the Architecture never asked for.
- **"Equivalent output" (AC #2) means line-for-line identical logs, not just "does the same conceptual thing."** Both Runners build the *same* sample `Order`s in the *same* order and both `OrderProcessor`s emit the *same* `"[Validation]"`/`"[Persistence]"`/`"[Notification]"` text — this is what makes the Before/After comparison concrete and interview-usable ("run both, diff the output, see the structure changed but behavior didn't"). Only the `"=== Before: ... ==="` / `"=== After: ... ==="` header line is expected to differ; that's how a viewer tells the two runs apart, not a violation of "equivalent."
- **AD-8 forbids one specific reference direction — Presentation/BLL/DAL → Exhibits — not "Exhibits references nothing."** `OrderFlow.Exhibits` referencing `OrderFlow.Domain` (Task 1) is explicitly allowed and required ("every exhibit pair must use real Domain types... rather than a locally redefined toy type"), which is why both Runners build actual `OrderFlow.Domain.Order`/`OrderItem` instances instead of a hand-rolled DTO.
- **No `OrderFlow.Tests` coverage for Exhibits, and that's intentional, not a gap.** Epic 4's stories (unlike every Epic 1-3 story) have no "extend `OrderFlow.Tests`" task in the epics file — Exhibits are, per AD-8, "opened and run independently for interview purposes only," outside the app's regression net. Task 4 verifies both exhibits by running them and reading console output, not via xUnit.
- **No `ILogger<T>`/DI in either exhibit.** The main app's Consistency Conventions (constructor-injected `ILogger<T>`, no static loggers) govern `OrderFlow.Presentation`/`BLL`/`DAL` — Exhibits are a standalone console demo with zero DI container (Task 1), so plain `Console.WriteLine` is the right tool here, not a violation of the main app's logging convention (which doesn't apply to a project that isn't part of the main app's dependency graph at all).
- **Naming conventions table (Interfaces `IXxx`, `XxxDto`, etc.) governs the layered app, not Exhibits.** `OrderValidator`/`OrderPersister`/`OrderNotifier` are plain classes, not `IXxxService` implementations — Exhibits is a teaching sandbox, not a fourth application layer.
- **New project files**: this is the first story to add files under `OrderFlow.Exhibits/Before/` and `OrderFlow.Exhibits/After/` — the folders themselves don't exist yet (only the empty, reference-less `.csproj` does, since Story 1.1). Story 4.2/4.3 add `Ocp`/`Dip` sibling folders under the same `Before`/`After` roots later; this story does not scaffold placeholders for them.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.Exhibits/
    OrderFlow.Exhibits.csproj          # modified: + OutputType Exe, + ProjectReference to OrderFlow.Domain
    Program.cs                         # new: arg-dispatch entry point (before-srp | after-srp), extended by Stories 4.2/4.3
    Before/
      Srp/
        OrderProcessor.cs              # new: SRP-violating god-class
        BeforeSrpRunner.cs             # new: builds sample Orders, runs Before.OrderProcessor
    After/
      Srp/
        OrderValidator.cs              # new
        OrderPersister.cs              # new
        OrderNotifier.cs               # new
        OrderProcessor.cs              # new: composes the three above
        AfterSrpRunner.cs              # new: builds the same sample Orders, runs After.OrderProcessor
```

`OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.BLL`/`OrderFlow.Presentation`/`OrderFlow.Presentation.Tests`/`OrderFlow.Tests` are untouched by this story — all changes are new files inside `OrderFlow.Exhibits`, plus its own `.csproj`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.1: SRP Exhibit Pair (Before/After)] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 4: Architecture Teaching Exhibits and Interview Documentation] — "three SOLID violations, each deliberately mirroring a real pattern already built in Epics 1-3" locked decision (SRP here mirrors no single existing class, it's the canonical textbook violation the other two exhibits' real-app mirrors — `IPricingStrategy`/DI composition root — sit alongside)
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-8 — Before/After Exhibits isolated from the runtime DI graph] — the one-directional reference rule and "real Domain types, no toy types" rule this story's design follows
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Structural Seed] — confirms `OrderFlow.Exhibits` is one project with `Before/`/`After/` subfolders, the basis for this story's single-`Program.cs`-with-dispatch design decision
- [Source: OrderFlow/OrderFlow.Exhibits/OrderFlow.Exhibits.csproj] — current state: bare library, zero project references, comment noting "Populated with Before/After exhibit pairs starting Epic 4"
- [Source: OrderFlow/OrderFlow.Domain/Order.cs] — `Order` shape (`Id`, `CustomerId`, `OrderType`, `Status`, `OrderItems`, `IAuditable`) both Runners construct directly
- [Source: OrderFlow/OrderFlow.Domain/OrderItem.cs] — `OrderItem` shape (`Quantity`, `ProductId`, etc.) the validation logic checks

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 8 projects, after all tasks): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet run --project OrderFlow.Exhibits --no-build -- before-srp`:
  ```
  === Before: SRP Violation ===
  [Persistence] Order 1 saved. Total persisted: 1.
  [Notification] Order 1 confirmed for customer 100.
  [Validation] Order 2 rejected: no line items.
  ```
- `dotnet run --project OrderFlow.Exhibits --no-build -- after-srp`:
  ```
  === After: SRP Refactor ===
  [Persistence] Order 1 saved. Total persisted: 1.
  [Notification] Order 1 confirmed for customer 100.
  [Validation] Order 2 rejected: no line items.
  ```
  Confirms AC #2: identical output apart from the header line.
- `dotnet run --project OrderFlow.Exhibits --no-build` (no args): printed the expected usage text listing both `before-srp`/`after-srp`.
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` (final): Passed! Failed: 0, Passed: 82, Skipped: 0, Total: 82 — unchanged from before this story (Exhibits are intentionally outside the test net).

### Completion Notes List

- `OrderFlow.Exhibits.csproj` gained `<OutputType>Exe</OutputType>` and a single `ProjectReference` to `OrderFlow.Domain` (the only reference AD-8 permits). No reference to `OrderFlow.Presentation`/`BLL`/`DAL` in either direction.
- New `Program.cs`: top-level-statement dispatch entry point (`before-srp` / `after-srp` args), with a `default` usage case. Scaffolded to be extended by Stories 4.2/4.3 with more cases, not replaced.
- `Before/Srp/OrderProcessor.cs`: single god-class inlining validation, persistence, and notification, each commented with its "reason to change." `Before/Srp/BeforeSrpRunner.cs`: builds one valid + one invalid sample `Order` (real `OrderFlow.Domain` types) and runs them through it.
- `After/Srp/OrderValidator.cs`, `OrderPersister.cs`, `OrderNotifier.cs`: the three single-responsibility collaborators, with `OrderValidator`'s error text matching `Before/Srp.OrderProcessor`'s inline messages exactly. `After/Srp/OrderProcessor.cs`: composes the three via constructor injection (plain `new`-ed, no DI container). `After/Srp/AfterSrpRunner.cs`: builds the identical two sample `Order`s and runs them through the composed processor.
- Verified both exhibits run standalone via `dotnet run --project OrderFlow.Exhibits -- <exhibit>` and produce line-for-line identical `[Validation]`/`[Persistence]`/`[Notification]` output (only the `=== ... ===` header differs) — confirms AC #2's "equivalent output" concretely, not just by code inspection.
- `dotnet build` is green across all 8 projects with 0 warnings. `OrderFlow.Tests` remains 82/82 passing, untouched by this story — no `OrderFlow.Tests` coverage was added for Exhibits, per Epic 4's epics-file precedent and AD-8's "interview purposes only" isolation.
- No `OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.BLL`/`OrderFlow.Presentation`/`OrderFlow.Presentation.Tests`/`OrderFlow.Tests` file touched — confirmed via File List below; every change is a new file inside `OrderFlow.Exhibits`, plus its own `.csproj`.

### File List

- `OrderFlow/OrderFlow.Exhibits/OrderFlow.Exhibits.csproj` (modified: + `OutputType=Exe`, + `ProjectReference` to `OrderFlow.Domain`)
- `OrderFlow/OrderFlow.Exhibits/Program.cs` (new: arg-dispatch entry point)
- `OrderFlow/OrderFlow.Exhibits/Before/Srp/OrderProcessor.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/Before/Srp/BeforeSrpRunner.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Srp/OrderValidator.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Srp/OrderPersister.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Srp/OrderNotifier.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Srp/OrderProcessor.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Srp/AfterSrpRunner.cs` (new)

## Change Log

- 2026-08-11: Implemented Story 4.1 — `OrderFlow.Exhibits` made executable with a shared arg-dispatch `Program.cs` (extensible for Stories 4.2/4.3); `Before/Srp` (god-class) and `After/Srp` (composed `OrderValidator`/`OrderPersister`/`OrderNotifier`) exhibit pair added, verified to produce line-for-line equivalent console output for identical sample orders. `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 82/82 passed, unchanged.
- 2026-08-11: Code review (combined with Stories 4.2/4.3, since all three share this story's `Program.cs`/`.csproj`) — 0 AC violations found (Acceptance Auditor hand-verified SRP's "equivalent output" claim). 2 findings deferred (dispatch case-sensitivity/no exit-code-on-error, both on the shared `Program.cs`); no SRP-specific patch findings. See `deferred-work.md` for full detail.
