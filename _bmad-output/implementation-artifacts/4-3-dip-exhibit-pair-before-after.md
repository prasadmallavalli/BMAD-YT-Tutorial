---
baseline_commit: NO_VCS
---

# Story 4.3: DIP Exhibit Pair (Before/After)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a viewer studying the codebase,
I want a Before/After exhibit demonstrating a Dependency Inversion Principle violation and its refactor via constructor injection,
so that I can see why the main app's DI composition root matters.

## Acceptance Criteria

1. **Given** `OrderFlow.Exhibits/Before/Dip`, **When** reviewed, **Then** it contains a class that directly instantiates a concrete data-access class internally, tightly coupled and untestable without a real database.
2. **And Given** `OrderFlow.Exhibits/After/Dip`, **When** reviewed, **Then** the same scenario is refactored to depend on an injected interface, with a runnable demo substituting a fake implementation.
3. **And Given** both exhibits, **When** run, **Then** each is independently runnable, and the After demo swaps in a fake without modifying the consuming class.

## Tasks / Subtasks

- [x] Task 1: Extend the shared `Program.cs` dispatch with two more cases (AC: #1, #2, #3)
  - [x] Same extension mechanism Stories 4.1/4.2 established — no new design decision here. Add `case "before-dip": BeforeDipRunner.Run(); break;` and `case "after-dip": AfterDipRunner.Run(); break;`. Add `using OrderFlow.Exhibits.Before.Dip;` and `using OrderFlow.Exhibits.After.Dip;`. Update the `default` usage text to list all six exhibits now available.
  - [x] No changes to `OrderFlow.Exhibits.csproj` — `OutputType=Exe` and the `OrderFlow.Domain` reference already exist (Story 4.1); this story only needs `OrderFlow.Domain.Customer`, already available.
- [x] Task 2: `OrderFlow.Exhibits/Before/Dip` — the DIP-violating tight coupling (AC: #1)
  - [x] `SqlCustomerRepository.cs` (namespace `OrderFlow.Exhibits.Before.Dip`): a plain class simulating a real data-access class — `public Customer? FindById(int id)` logs `"[SqlCustomerRepository] Querying real database for Customer {id}..."` and returns a hardcoded `Customer { Id = 1, Name = "Ada Lovelace", Email = "ada@example.com" }` when `id == 1`, `null` otherwise. This stands in for what would be a real ADO.NET/EF Core class talking to an actual database — there is deliberately no interface here yet.
  - [x] `CustomerLookupService.cs` (same namespace): `private readonly SqlCustomerRepository _repository = new();` — the DIP violation itself: the concrete data-access class is instantiated **inside** the consumer, not passed in. `public Customer? FindCustomer(int id)` calls `_repository.FindById(id)`, logs `"[CustomerLookupService] Found: {customer.Name} <{customer.Email}>"` or `"[CustomerLookupService] Customer {id} not found."`, and returns the result. Because `_repository` is a field-initializer `new SqlCustomerRepository()`, there is no way to substitute anything else — this class is "untestable without a real database" (AC #1) by construction, not just by convention.
  - [x] `BeforeDipRunner.cs` (same namespace): `public static void Run()` — prints `"=== Before: DIP Violation ==="`, constructs one `CustomerLookupService()` (no constructor args — it has none to give), and calls `FindCustomer(1)` then `FindCustomer(99)`.
- [x] Task 3: `OrderFlow.Exhibits/After/Dip` — the DIP refactor via constructor injection (AC: #2, #3)
  - [x] `ICustomerRepository.cs` (namespace `OrderFlow.Exhibits.After.Dip`): `public interface ICustomerRepository { Customer? FindById(int id); }`.
  - [x] `SqlCustomerRepository.cs` (same namespace): `: ICustomerRepository`, identical body/log line to Before's `SqlCustomerRepository` — the same "real database" behavior, now behind the abstraction.
  - [x] `FakeCustomerRepository.cs` (same namespace): `: ICustomerRepository`, backed by an in-memory `Dictionary<int, Customer>` seeded with the same `Id = 1, Name = "Ada Lovelace", Email = "ada@example.com"` fixture; logs `"[FakeCustomerRepository] Looking up in-memory fixture for Customer {id}..."`. This is the concrete "runnable demo substituting a fake implementation" AC #2 asks for — no real database involved at all.
  - [x] `CustomerLookupService.cs` (same namespace, same class name as Before's): constructor-injected `ICustomerRepository repository` (stored in a `private readonly` field, never `new`-ed internally); `FindCustomer(int id)` body is otherwise identical to Before's version (same log lines, same return shape).
  - [x] `AfterDipRunner.cs` (same namespace): `public static void Run()` — prints `"=== After: DIP Refactor ==="`, then demonstrates the swap concretely: constructs `new CustomerLookupService(new SqlCustomerRepository())` and calls `FindCustomer(1)` under a `"-- with SqlCustomerRepository --"` sub-header, then constructs a **second** `new CustomerLookupService(new FakeCustomerRepository())` and calls `FindCustomer(1)`/`FindCustomer(99)` under a `"-- with FakeCustomerRepository (no real database needed) --"` sub-header. `CustomerLookupService`'s own source is identical in both calls — only the constructor argument changes — which is AC #3's "swaps in a fake without modifying the consuming class" made literal and visible in one run's output, not just true by code inspection.
- [x] Task 4: Verify end-to-end (AC: #1, #2, #3)
  - [x] `dotnet build` succeeds for the whole solution (all 8 projects) — 0 errors, 0 warnings.
  - [x] `dotnet run --project OrderFlow.Exhibits -- before-dip` runs standalone, using the real-database-simulating `SqlCustomerRepository` for both lookups (no way to avoid it, per AC #1).
  - [x] `dotnet run --project OrderFlow.Exhibits -- after-dip` runs standalone and shows both the `SqlCustomerRepository` and `FakeCustomerRepository` paths through the same unmodified `CustomerLookupService` class.
  - [x] Re-run `dotnet run --project OrderFlow.Exhibits -- before-srp` / `after-srp` / `before-ocp` / `after-ocp` to confirm Stories 4.1/4.2's exhibits still work unchanged (regression check on the shared `Program.cs`).
  - [x] `dotnet run --project OrderFlow.Exhibits` (no args) prints updated usage text listing all six exhibits.
  - [x] Confirm `OrderFlow.Presentation`/`OrderFlow.BLL`/`OrderFlow.DAL`/`Program.cs` (main app) and `OrderFlow.Domain` are untouched — this story only adds files under `OrderFlow.Exhibits/Before/Dip` and `.../After/Dip`, plus extending the existing `OrderFlow.Exhibits/Program.cs`. Confirm via File List below.
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` still passes at 82/82 (unchanged — Exhibits remain outside the app's test net, per Stories 4.1/4.2's precedent).

### Review Findings

Reviewed together with Stories 4.1/4.2 (2026-08-11). Acceptance Auditor diffed `Before/Dip/CustomerLookupService.cs` against `After/Dip/CustomerLookupService.cs` line by line and confirmed AC #2/#3 hold exactly — the only structural differences are the field type (`SqlCustomerRepository` → `ICustomerRepository`) and constructor shape; `FindCustomer`'s body is byte-identical.

- [x] [Review][Defer] The `Id=1, Name="Ada Lovelace", Email="ada@example.com"` customer fixture is hand-duplicated across `Before/Dip/SqlCustomerRepository.cs`, `After/Dip/SqlCustomerRepository.cs`, and `After/Dip/FakeCustomerRepository.cs` with no single source of truth — a future fixture change requires manually touching all three [OrderFlow.Exhibits/Before/Dip/SqlCustomerRepository.cs, OrderFlow.Exhibits/After/Dip/SqlCustomerRepository.cs, OrderFlow.Exhibits/After/Dip/FakeCustomerRepository.cs] — deferred (the Before/After duplication itself is an explicit, documented design choice per this story's own Dev Notes; only the third file's copy is unaccounted for, and it's a one-line teaching fixture, low value to fix)

## Dev Notes

- **This story has no "equivalent output" AC, unlike Stories 4.1/4.2 — don't over-impose that pattern here.** SRP/OCP's AC #2/#3 explicitly required Before/After to produce matching output; DIP's AC #3 instead requires "the After demo swaps in a fake without modifying the consuming class." `AfterDipRunner` is designed to make that swap *visible in the output* (two sub-headers, two constructions of the same `CustomerLookupService` class with different repository arguments) rather than to numerically match Before's output line-for-line — Before only ever exercises the SQL path, so there is nothing to match against for the fake path anyway.
- **Why `CustomerLookupService` has the same class name in both namespaces but genuinely different shapes.** Before's has a zero-arg constructor (nothing to inject — the violation). After's has a one-arg constructor taking `ICustomerRepository`. This is intentional and mirrors Stories 4.1/4.2's convention (same class name, different namespace, one shared vocabulary) — a reviewer diffing `Before/Dip/CustomerLookupService.cs` against `After/Dip/CustomerLookupService.cs` should see exactly one structural change: where the repository comes from.
- **`SqlCustomerRepository` is duplicated (not shared) between `Before/Dip` and `After/Dip`, deliberately.** Before's has no interface to implement; After's does (`: ICustomerRepository`). Sharing one class between the two folders would either force Before to implement an interface it's supposed to lack, or force After's to not implement one it needs — both defeat the pair's teaching purpose. The two versions have identical bodies/log text so a reviewer can confirm nothing except "does this implement `ICustomerRepository`" changed between them.
- **`FakeCustomerRepository` is what Before's design makes impossible — that's the whole point.** It has zero database dependency (a plain `Dictionary`), and because `After/Dip.CustomerLookupService` depends on `ICustomerRepository` rather than `SqlCustomerRepository` directly, substituting it requires touching only the *composition* (what gets passed to the constructor), never `CustomerLookupService.cs` itself.
- **`Customer` is a real `OrderFlow.Domain` type** (per AD-8 — same rule Stories 4.1/4.2 followed), used identically across all four new classes; no toy customer type is defined anywhere in this story.
- **This extends Story 4.1's `Program.cs`, following the same pattern as Story 4.2.** No new entry-point or dispatch design decisions in this story — Task 1 is purely additive, two more `case` arms.
- **No `OrderFlow.Tests` coverage, same as Stories 4.1/4.2.** Exhibits stay outside the app's test net per AD-8; Task 4 verifies by running both and reading console output.

### Project Structure Notes

```text
OrderFlow/
  OrderFlow.Exhibits/
    Program.cs                         # modified: + before-dip/after-dip cases, updated usage text
    Before/
      Dip/
        SqlCustomerRepository.cs        # new: concrete data-access class, no interface
        CustomerLookupService.cs        # new: `new`s SqlCustomerRepository internally (violation)
        BeforeDipRunner.cs              # new
    After/
      Dip/
        ICustomerRepository.cs          # new
        SqlCustomerRepository.cs        # new: same behavior as Before's, now : ICustomerRepository
        FakeCustomerRepository.cs       # new: in-memory fixture, no real database
        CustomerLookupService.cs        # new: constructor-injected ICustomerRepository
        AfterDipRunner.cs               # new: demonstrates swapping Sql -> Fake with zero change to CustomerLookupService
```

`OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.BLL`/`OrderFlow.Presentation`/`OrderFlow.Presentation.Tests`/`OrderFlow.Tests` are untouched by this story. `OrderFlow.Exhibits/Before/Srp`, `.../After/Srp`, `.../Before/Ocp`, `.../After/Ocp`, and `OrderFlow.Exhibits.csproj` (Stories 4.1/4.2) are also untouched — only `Program.cs` gains two more lines of dispatch.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.3: DIP Exhibit Pair (Before/After)] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-8 — Before/After Exhibits isolated from the runtime DI graph] — the "real Domain types" rule (`Customer`) this story follows, same as Stories 4.1/4.2
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Design Paradigm / AD-1] — the main app's own composition-root/constructor-injection discipline this exhibit pair explains "why it matters" for (per the story's own framing)
- [Source: OrderFlow/OrderFlow.Domain/Customer.cs] — `Customer` shape (`Id`, `Name`, `Email`, `Phone`, `IAuditable`) both `SqlCustomerRepository` variants and `FakeCustomerRepository` construct
- [Source: OrderFlow/OrderFlow.Exhibits/Program.cs] — Stories 4.1/4.2's dispatch entry point this story extends with two more `case` arms
- [Source: _bmad-output/implementation-artifacts/4-1-srp-exhibit-pair-before-after.md] — precedent for the shared-`Program.cs`-with-dispatch design and the "no `OrderFlow.Tests` coverage" decision
- [Source: _bmad-output/implementation-artifacts/4-2-ocp-exhibit-pair-before-after.md] — precedent for extending `Program.cs` additively (Task 1) without touching prior exhibits, and for citing exactly which AC requires "equivalent output" vs. which doesn't (this story's Dev Notes call out that DIP's AC #3 does not)

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 8 projects, after all tasks): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet run --project OrderFlow.Exhibits --no-build -- before-dip`:
  ```
  === Before: DIP Violation ===
  [SqlCustomerRepository] Querying real database for Customer 1...
  [CustomerLookupService] Found: Ada Lovelace <ada@example.com>
  [SqlCustomerRepository] Querying real database for Customer 99...
  [CustomerLookupService] Customer 99 not found.
  ```
- `dotnet run --project OrderFlow.Exhibits --no-build -- after-dip`:
  ```
  === After: DIP Refactor ===
  -- with SqlCustomerRepository --
  [SqlCustomerRepository] Querying real database for Customer 1...
  [CustomerLookupService] Found: Ada Lovelace <ada@example.com>
  -- with FakeCustomerRepository (no real database needed) --
  [FakeCustomerRepository] Looking up in-memory fixture for Customer 1...
  [CustomerLookupService] Found: Ada Lovelace <ada@example.com>
  [FakeCustomerRepository] Looking up in-memory fixture for Customer 99...
  [CustomerLookupService] Customer 99 not found.
  ```
  Confirms AC #3: the same `CustomerLookupService` class works with both `SqlCustomerRepository` and `FakeCustomerRepository`, swapped only via the constructor argument.
- `dotnet run --project OrderFlow.Exhibits --no-build -- before-srp` / `-- after-srp` / `-- before-ocp` / `-- after-ocp`: re-verified unchanged from Stories 4.1/4.2 (regression check on the shared `Program.cs` dispatch).
- `dotnet run --project OrderFlow.Exhibits --no-build` (no args): printed updated usage text listing all six exhibits.
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` (final): Passed! Failed: 0, Passed: 82, Skipped: 0, Total: 82 — unchanged from before this story.

### Completion Notes List

- `OrderFlow.Exhibits/Program.cs` (Stories 4.1/4.2) extended with `before-dip`/`after-dip` cases and updated usage text — purely additive, no changes to its dispatch design or any prior exhibit.
- `Before/Dip/SqlCustomerRepository.cs`, `CustomerLookupService.cs`, `BeforeDipRunner.cs`: DIP violation — `CustomerLookupService` field-initializes a concrete `SqlCustomerRepository` internally, with no way to substitute anything else.
- `After/Dip/ICustomerRepository.cs`, `SqlCustomerRepository.cs` (now `: ICustomerRepository`), `FakeCustomerRepository.cs` (in-memory fixture, no real database), `CustomerLookupService.cs` (constructor-injected `ICustomerRepository`), `AfterDipRunner.cs`: DIP refactor demonstrating the same `CustomerLookupService` class working with both a real-simulating and a fake repository, swapped only at the composition point.
- Verified both new exhibits run standalone; `after-dip`'s output visibly shows the identical `CustomerLookupService` handling both `SqlCustomerRepository` and `FakeCustomerRepository` — confirms AC #3 concretely, not just by code inspection. Re-ran all four Story 4.1/4.2 exhibits to confirm no regression on the shared entry point.
- `dotnet build` is green across all 8 projects with 0 warnings. `OrderFlow.Tests` remains 82/82 passing, untouched — no test coverage added for Exhibits, per Stories 4.1/4.2's established precedent (this story has no "equivalent output" AC either, unlike 4.1/4.2 — see Dev Notes).
- No `OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.BLL`/`OrderFlow.Presentation`/`OrderFlow.Presentation.Tests`/`OrderFlow.Tests`/`OrderFlow.Exhibits.csproj`/`Before/Srp`/`After/Srp`/`Before/Ocp`/`After/Ocp` file touched — confirmed via File List below; every change is either a new file under `OrderFlow.Exhibits/Before/Dip` or `.../After/Dip`, or two added `case` arms plus usage text in the existing `Program.cs`.

### File List

- `OrderFlow/OrderFlow.Exhibits/Program.cs` (modified: + `before-dip`/`after-dip` cases, updated usage text)
- `OrderFlow/OrderFlow.Exhibits/Before/Dip/SqlCustomerRepository.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/Before/Dip/CustomerLookupService.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/Before/Dip/BeforeDipRunner.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Dip/ICustomerRepository.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Dip/SqlCustomerRepository.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Dip/FakeCustomerRepository.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Dip/CustomerLookupService.cs` (new)
- `OrderFlow/OrderFlow.Exhibits/After/Dip/AfterDipRunner.cs` (new)

## Change Log

- 2026-08-11: Implemented Story 4.3 — extended `OrderFlow.Exhibits/Program.cs`'s dispatch with `before-dip`/`after-dip`; added `Before/Dip` (`CustomerLookupService` internally `new`s a concrete `SqlCustomerRepository`) and `After/Dip` (`CustomerLookupService` constructor-injected with `ICustomerRepository`, demoed against both `SqlCustomerRepository` and a `FakeCustomerRepository` with zero changes to the consuming class). `dotnet build` green across all 8 projects with 0 warnings; `dotnet test OrderFlow.Tests` 82/82 passed, unchanged. Epic 4's exhibit trilogy (SRP/OCP/DIP) is now complete.
- 2026-08-11: Code review (combined with Stories 4.1/4.2) — 0 AC violations (Acceptance Auditor diffed `Before/Dip/CustomerLookupService.cs` against `After/Dip/CustomerLookupService.cs` line by line and confirmed the only structural differences are the documented ones). 1 finding deferred: the customer fixture is hand-duplicated across 3 files with no single source of truth (only the `FakeCustomerRepository` copy was unaccounted for by this story's own Dev Notes — the Before/After duplication itself is deliberate).
