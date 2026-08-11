---
baseline_commit: NO_VCS
---

# Story 1.2: Customer Domain, Repository & Service

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want a Customer entity, repository, and service wired through the Unit of Work,
so that Customer data can be persisted per the architecture's DAL/BLL conventions.

## Acceptance Criteria

1. **Given** `OrderFlow.Domain`, **When** complete, **Then** a `Customer` entity (`Id`, `Name`, `Email`, `Phone`, `IAuditable`) exists with `CustomerConfiguration : IEntityTypeConfiguration<Customer>`, and a migration adds the `Customers` table.
2. **And Given** `OrderFlow.DAL`, **When** implemented, **Then** `ICustomerRepository`/`CustomerRepository` is exposed only via `IUnitOfWork.Customers` (AD-9), and `SaveChanges` stamps `CreatedAt` only on `EntityState.Added` (AD-6).
3. **And Given** `OrderFlow.BLL`, **When** implemented, **Then** `ICustomerService`/`CustomerService` exposes async Create/Get/GetAll/Update over `CustomerDto` (never the entity, AD-12), validates required fields, and returns `Result<T>` on failure.
4. **And Given** `OrderFlow.Tests`, **When** complete, **Then** `CustomerService` is covered with a mocked `IUnitOfWork`, including a success path and a validation-failure path.

## Tasks / Subtasks

- [x] Task 1: `Customer` entity + EF configuration (AC: #1)
  - [x] `OrderFlow.Domain/Customer.cs`: `int Id`, `string Name`, `string Email`, `string? Phone`, implements `IAuditable`
  - [x] `OrderFlow.DAL/CustomerConfiguration.cs`: `CustomerConfiguration : IEntityTypeConfiguration<Customer>` — table `Customers`, `Name`/`Email` required with reasonable max lengths (e.g. `nvarchar(200)`/`nvarchar(256)` — not specified elsewhere, dev's call), `Phone` optional
  - [x] Add `DbSet<Customer> Customers` to `AppDbContext`; override `OnModelCreating` to call `modelBuilder.ApplyConfiguration(new CustomerConfiguration())`
- [x] Task 2: EF Core migrations tooling (AC: #1) — **first use in this codebase, must be set up now**
  - [x] Add `Microsoft.EntityFrameworkCore.Design` `10.0.10` to `OrderFlow.DAL.csproj` with `PrivateAssets="all"` (build-only tooling package, matches the `10.0.10` pin already on `Microsoft.EntityFrameworkCore.SqlServer` from Story 1.1's code review)
  - [x] Implement `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` in `OrderFlow.DAL` — see Dev Notes for why this is required and exactly what it should contain
  - [x] Set up `dotnet-ef` as a local tool: `dotnet new tool-manifest` (repo root, if not already present) then `dotnet tool install dotnet-ef --version 10.0.10`
  - [x] Run `dotnet tool run dotnet-ef migrations add InitialCustomer --project OrderFlow.DAL --startup-project OrderFlow.DAL` — generates `OrderFlow.DAL/Migrations/`
  - [x] Confirm the generated migration creates a `Customers` table matching the entity/configuration
- [x] Task 3: Auditing stamp override (AC: #2, AD-6) — **first auditable entity, override doesn't exist yet**
  - [x] Override `SaveChangesAsync`/`SaveChanges` in `AppDbContext`: for every tracked entry implementing `IAuditable`, set `UpdatedAt = DateTime.UtcNow` always; set `CreatedAt = DateTime.UtcNow` only when `EntityState == EntityState.Added` — never overwrite `CreatedAt` on `Modified`
- [x] Task 4: `IUnitOfWork`/`UnitOfWork` + `ICustomerRepository`/`CustomerRepository` (AC: #2) — **first introduction of the Unit of Work in this codebase**
  - [x] `OrderFlow.DAL/IUnitOfWork.cs`: `ICustomerRepository Customers { get; }`, `Task<int> SaveChangesAsync()`, implements **both** `IDisposable` and `IAsyncDisposable` (see Dev Notes — Story 1.3's Presenter-per-action scope disposal needs both)
  - [x] `OrderFlow.DAL/UnitOfWork.cs`: constructor takes `IDbContextFactory<AppDbContext>`, calls the **synchronous** `CreateDbContext()` once at construction (see Dev Notes — DI constructors can't `await`), exposes `Customers` backed by `new CustomerRepository(_context)`, `SaveChangesAsync()` delegates to `_context.SaveChangesAsync()`, `Dispose()`/`DisposeAsync()` both dispose `_context`
  - [x] `OrderFlow.DAL/ICustomerRepository.cs`: `Task<Customer?> GetByIdAsync(int id)`, `Task<IReadOnlyList<Customer>> GetAllAsync()`, `Task AddAsync(Customer customer)`
  - [x] `OrderFlow.DAL/CustomerRepository.cs`: constructor takes `AppDbContext` (never the factory — only `UnitOfWork` touches the factory, per AD-2); implements the above via `_context.Customers`. **`GetByIdAsync` must NOT call `.AsNoTracking()`** — the returned entity must stay tracked by the shared `DbContext` so `CustomerService.UpdateAsync`'s direct-mutation pattern (Task 5) actually registers changes with EF's change tracker. Using `AsNoTracking()` here would make `SaveChangesAsync()` silently persist zero changes.
- [x] Task 5: `CustomerDto` + `ICustomerService`/`CustomerService` + `Result<T>` (AC: #3) — **first use of `Result<T>` in this codebase**
  - [x] `OrderFlow.BLL/Result.cs`: generic `Result<T>` with `IsSuccess`, `Value`, `Error`, static `Success(T)`/`Failure(string)` factories
  - [x] `OrderFlow.BLL/CustomerDto.cs`: `Id`, `Name`, `Email`, `Phone`
  - [x] `OrderFlow.BLL/ICustomerService.cs`: `Task<Result<CustomerDto>> CreateAsync(CustomerDto dto)`, `Task<Result<CustomerDto>> GetAsync(int id)`, `Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync()`, `Task<Result<CustomerDto>> UpdateAsync(int id, CustomerDto dto)`
  - [x] `OrderFlow.BLL/CustomerService.cs`: constructor takes `IUnitOfWork`; validates `Name`/`Email` non-empty on Create/Update (`Result<T>.Failure(...)` if missing — never throws for this); maps `Customer` ↔ `CustomerDto` inside BLL (AD-12)
    - `CreateAsync`: validate → map `dto` to a new `Customer` (no `Id`) → `await Customers.AddAsync(customer)` → **`await SaveChangesAsync()`** (do not skip this — it's the only step that actually persists and populates the generated `Id`) → map the saved entity back to `CustomerDto` → `Result<CustomerDto>.Success(...)`
    - `GetAsync`/`UpdateAsync`: call `Customers.GetByIdAsync(id)` first; **if it returns `null`, return `Result<CustomerDto>.Failure("Customer not found")` — do not dereference a null entity**
    - `UpdateAsync` (once the entity is found): validate → **mutate its properties directly** (EF's change tracker marks only the changed properties `Modified` automatically because the entity is already tracked by this operation's shared `DbContext` — do not call a blanket `Update()`/reattach a detached graph, per AD-6) → `await SaveChangesAsync()` → map back to `CustomerDto`
- [x] Task 6: Composition root registration (AC: #2, #3, AD-9)
  - [x] `Program.cs` `ConfigureServices`: `services.AddScoped<IUnitOfWork, UnitOfWork>();` and `services.AddScoped<ICustomerService, CustomerService>();`
  - [x] Do **not** register `ICustomerRepository`/`CustomerRepository` in DI — `UnitOfWork` constructs it internally (AD-9: "repositories are never independently DI-registered")
- [x] Task 7: BLL tests with mocked `IUnitOfWork` (AC: #4)
  - [x] Add a `ProjectReference` to `OrderFlow.DAL` in `OrderFlow.Tests.csproj` (see Dev Notes — required now because `IUnitOfWork`/`ICustomerRepository` live in `OrderFlow.DAL` per AD-1, superseding Story 1.1 AC#3's "not `OrderFlow.DAL` directly" for interface-mocking purposes only)
  - [x] Add `Moq` `4.20.72` to `OrderFlow.Tests.csproj` — first mocking library in this codebase, none exists yet
  - [x] `CustomerServiceTests`: success path (valid `CreateAsync` → `Result.IsSuccess == true`, correct `CustomerDto` shape, **and `Mock<IUnitOfWork>.Verify(u => u.SaveChangesAsync(), Times.Once)`** so a missing `SaveChangesAsync()` call actually fails the test) and validation-failure path (missing `Name` or `Email` → `Result.IsSuccess == false` with a message, and `SaveChangesAsync()` never called) — mock `IUnitOfWork.Customers` to return a mocked `ICustomerRepository`
- [x] Task 8: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution — 0 errors, 0 warnings
  - [x] `dotnet test` passes, including new `CustomerServiceTests`
  - [x] `dotnet ef migrations add` succeeds and produces the expected `Customers` table migration (migration generation is model-based and should not require a live LocalDB connection; if it unexpectedly does and fails on this dev machine, document it the same way Story 1.1 documented its `UNVERIFIED-ENVIRONMENT` gap — don't silently skip it) — **confirmed: no live LocalDB connection was needed, migration generated cleanly on macOS**

### Review Findings

- [x] [Review][Patch] `AppDbContext.Customers` `DbSet<Customer>` is declared `public` — violates AD-9's literal Rule ("`DbSet<T>` types remain fully internal — nothing outside `OrderFlow.DAL` ever sees or declares one"; only `AppDbContext` itself is the sanctioned public exception, not its `DbSet<T>` members). `CustomerRepository` is in the same assembly, so `internal` satisfies every real caller. [`OrderFlow.DAL/AppDbContext.cs:16`]
- [x] [Review][Patch] `CustomerService.UpdateAsync` validates the DTO *before* fetching the entity, reversing the story's own Task 5 spec ("once the entity is found: validate → mutate"). Calling `UpdateAsync` with both an invalid DTO and a non-existent id returns "Name is required" instead of "Customer not found" — a real behavioral bug caught by the Acceptance Auditor, not just a style nit. [`OrderFlow.BLL/CustomerService.cs:50-56`]
- [x] [Review][Patch] `CreateAsync`/`UpdateAsync` NRE on `dto == null` — `Validate()` dereferences `dto.Name` with no null guard. [`OrderFlow.BLL/CustomerService.cs:75`]
- [x] [Review][Patch] `Validate()` never checks `Name`/`Email`/`Phone` against `CustomerConfiguration`'s max lengths (200/256/30) — an over-length value reaches `SaveChangesAsync()` and throws an unhandled `DbUpdateException` instead of a clean `Result<T>.Failure`, for a very plausible real input (pasted text in a WinForms field). [`OrderFlow.BLL/CustomerService.cs:75-88`]
- [x] [Review][Patch] `AppDbContext.OnModelCreating` never calls `base.OnModelCreating(modelBuilder)` — harmless today, but a known EF Core footgun and bad precedent as the model grows. [`OrderFlow.DAL/AppDbContext.cs:18-21`]
- [x] [Review][Patch] No input trimming on `Name`/`Email`/`Phone` — `"Ada "` and `"Ada"` persist as distinct values. [`OrderFlow.BLL/CustomerService.cs`]
- [x] [Review][Patch] The literal `"Customer not found"` is duplicated in `GetAsync` and `UpdateAsync` with no shared constant — a future wording change in one spot silently diverges from the other. [`OrderFlow.BLL/CustomerService.cs:40,61`]
- [x] [Review][Patch] `CreateAsync(CustomerDto dto)` silently ignores any `Id` the caller sets, with no doc note — invites a caller to reasonably assume passing `Id` targets an existing record. [`OrderFlow.BLL/CustomerService.cs:15`]
- [x] [Review][Patch] The `OrderFlow.Tests.csproj` → `OrderFlow.DAL` reference is justified only by an inline code comment claiming it "supersedes Story 1.1 AC#3," with no cross-reference recorded on the Story 1.1 file itself — add a traceability note there.
- [x] [Review][Patch] No test coverage for `GetAsync`, `GetAllAsync`, or `UpdateAsync` (including the not-found path both the AC and Task 5 specify) — this exact gap is why the validate-before-fetch bug above shipped undetected. [`OrderFlow.Tests/CustomerServiceTests.cs`]
- [x] [Review][Defer] No unique constraint/index on `Customer.Email` — deferred, pre-existing; not required by this story's AC or the PRD, a product-scope decision for a later story.
- [x] [Review][Defer] `Validate()` doesn't check `Email` is a well-formed email address — deferred, pre-existing; AC only requires "validates required fields," not format.
- [x] [Review][Defer] No DAL-level tests for `CustomerRepository`/`UnitOfWork`/`StampAuditableEntries` against an in-memory/SQLite provider — deferred, pre-existing; the mocked-`IUnitOfWork` BLL tests already satisfy AC #4 literally, this is a future testing-infrastructure investment.
- [x] [Review][Defer] No `CancellationToken` threaded through `ICustomerRepository`/`IUnitOfWork`/`ICustomerService` (inconsistent with `AppDbContext.SaveChangesAsync`'s own override, which does take one) — deferred, pre-existing; not mandated by architecture/epics yet, and adding it now touches every method signature across three layers.
- [x] [Review][Defer] No index on `Email`/`Name` for lookups — deferred, pre-existing; no `GetByEmail`-style lookup method exists in spec yet to make the gap concrete.

## Dev Notes

- **Builds directly on Story 1.1's scaffold** — six projects, empty `AppDbContext`, composition root in `Program.cs` all exist. This story adds the first real entity, the first Unit of Work, and the first BLL service.
- **`Program.cs` already has `ValidateOnBuild = true`/`ValidateScopes = true`** on `BuildServiceProvider()` (added in Story 1.1's code review). A misconfigured registration here (e.g. missing constructor dependency) will now throw immediately at startup instead of silently passing — use this as a fast feedback signal while wiring `IUnitOfWork`/`ICustomerService`.
- **Why a design-time factory is required now, not before:** Story 1.1's code review deferred "no `IDesignTimeDbContextFactory<AppDbContext>`" specifically because no migrations existed yet. This story is the first to run `dotnet ef migrations add`, and `AppDbContext` has no parameterless constructor (`DbContextOptions<AppDbContext> options` is required), so the EF Core CLI cannot construct it without either a design-time factory or a discoverable host — this WinForms app has neither. Implement `AppDbContextFactory` directly in `OrderFlow.DAL`:
  ```csharp
  public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
  {
      public AppDbContext CreateDbContext(string[] args)
      {
          var options = new DbContextOptionsBuilder<AppDbContext>()
              .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=OrderFlow;Trusted_Connection=True;TrustServerCertificate=True;")
              .Options;
          return new AppDbContext(options);
      }
  }
  ```
  This is a design-time-only tooling class (standard EF Core pattern) — it is never referenced by the runtime DI graph, so it does not reopen the AD-1/AD-9 composition-root exception debate; it's DAL-internal, invoked only by the `dotnet ef` CLI. The connection string is duplicated from `Program.cs` intentionally — no configuration system exists yet (same "out of scope for now" call Story 1.1 made).
- **Why `UnitOfWork` uses the synchronous `CreateDbContext()`, not `CreateDbContextAsync()`:** AD-2 says "Only `IUnitOfWork` calls `CreateDbContextAsync()` — once per business operation, at construction." But DI constructors cannot `await`, and MS.DI's constructor injection has no async resolution path. Resolve this by having `UnitOfWork`'s constructor call the **synchronous** `IDbContextFactory<AppDbContext>.CreateDbContext()` overload — this is a genuine sync EF Core API (not a blocked-on-Task antipattern like `.Result`/`.Wait()`), and it satisfies AD-2's actual intent ("only `IUnitOfWork` touches the factory, once, at construction"). Do not invent a lazy-initialization or `InitializeAsync()` pattern for this — it isn't needed and adds complexity later stories don't expect.
- **Repository update pattern (AD-6) — do not write a blanket `Update()`:** Because `UnitOfWork` shares one `DbContext` per business operation, an entity loaded via `Customers.GetByIdAsync(id)` is already tracked by that same context. Mutating its properties directly in `CustomerService.UpdateAsync` is enough — EF Core's change tracker marks only the properties that actually changed as `Modified`. Do **not** add a `CustomerRepository.Update(Customer)` method that calls `_context.Update(entity)` or `_context.Customers.Update(entity)` on a reconstructed/detached instance — that marks the *entire* entity graph `Modified` and is exactly what AD-6 forbids.
- **`OrderFlow.Tests` now needs a project reference to `OrderFlow.DAL`.** Story 1.1's AC#3 locked `OrderFlow.Tests` to reference "`OrderFlow.BLL`/`OrderFlow.Domain` only (not `OrderFlow.DAL` directly, per AD-9 mockability)" — that constraint was written before this story placed `IUnitOfWork`/`ICustomerRepository` in `OrderFlow.DAL` per AD-1's literal text. The Architecture Spine's own mermaid diagram already shows `Tests -.mocks.-> DAL` as legitimate (alongside `Tests -.mocks.-> BLL`) — mocking DAL's interfaces from Tests is exactly what "AD-9 mockability" was for, so this is a superseding clarification of Story 1.1's AC#3, not a violation of it. Add the reference now.
- **`UnitOfWork` implements both `IDisposable` and `IAsyncDisposable`.** Story 1.3 (Customer UI) will have `XxxPresenter` open one `IServiceScope` per user action (AD-3) and dispose it at the end. If that disposal is ever synchronous, .NET's DI container requires every scoped service to support `IDisposable` — a Scoped service with only `IAsyncDisposable` throws `InvalidOperationException` on a sync `scope.Dispose()`. Implementing both now avoids a forced rework in Story 1.3.
- **Interface placement follows AD-1 literally:** `IUnitOfWork` and `ICustomerRepository` live in `OrderFlow.DAL` (not `OrderFlow.BLL`) — AD-1 states "`OrderFlow.BLL` references `OrderFlow.DAL` interfaces and `OrderFlow.Domain` only." `OrderFlow.BLL` consumes these interfaces via DI; it never touches `AppDbContext`, EF Core types, or `CustomerRepository` directly (AD-9).
- **`Result<T>` and `CustomerDto` live in `OrderFlow.BLL`** (not `OrderFlow.Domain`) — the Architecture Spine's Structural Seed reserves `OrderFlow.Domain` for "entities, `OrderType`/`OrderStatus` enums, `IAuditable`" only; `Result<T>` is a BLL-layer error-handling convention (Consistency Conventions table) and `CustomerDto` is explicitly a BLL/Presentation-boundary type (AD-12).
- **Directory.Build.props already sets `ImplicitUsings`/`Nullable`** for every project (added in Story 1.1's code review) — new `.cs` files don't need those properties repeated per-project.
- **Naming conventions (Architecture Consistency Conventions table):** `IXxx` interfaces; `IXxxRepository`/`XxxRepository`; `IXxxService`/`XxxService`; `XxxDto`; `XxxConfiguration : IEntityTypeConfiguration<Xxx>`. Already followed above.
- **Data & formats:** `Id` is `int` identity (EF Core default, no explicit key config needed beyond convention). No money/date fields on `Customer` this story.
- **`RowVersion`/optimistic concurrency (AD-10) does NOT apply to `Customer`** — the epics/AC for this story don't list it, and AD-10 explicitly leaves entity prioritization to the Epics level; `Inventory` gets it first, in Story 1.4.
- **Do not touch `OrderType`/`OrderStatus`** — still single-placeholder-member enums per Story 1.1, untouched until Epic 2/3.

### Project Structure Notes

```
OrderFlow/
  OrderFlow.Domain/
    Customer.cs                    # new
  OrderFlow.DAL/
    AppDbContext.cs                 # modified: DbSet<Customer>, OnModelCreating, SaveChanges(Async) override
    AppDbContextFactory.cs          # new — design-time only
    CustomerConfiguration.cs        # new
    IUnitOfWork.cs                  # new
    UnitOfWork.cs                   # new
    ICustomerRepository.cs          # new
    CustomerRepository.cs           # new
    Migrations/                     # new — generated by `dotnet ef migrations add`
  OrderFlow.BLL/
    Result.cs                       # new
    CustomerDto.cs                  # new
    ICustomerService.cs             # new
    CustomerService.cs              # new
  OrderFlow.Presentation/
    Program.cs                      # modified: register IUnitOfWork, ICustomerService
  OrderFlow.Tests/
    OrderFlow.Tests.csproj           # modified: + ProjectReference to OrderFlow.DAL, + Moq 4.20.72
    CustomerServiceTests.cs          # new
```

No conflicts with the Structural Seed — this is the first story to populate `OrderFlow.DAL`/`OrderFlow.BLL` beyond the empty scaffold, matching the target layout exactly. The `OrderFlow.Tests.csproj` DAL reference is a superseding clarification of Story 1.1 AC#3 (see Dev Notes), not a variance from it.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2: Customer Domain, Repository & Service] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#Additional Requirements] — persistence/transaction boundary, naming conventions, error handling conventions
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-1 — Strict layered dependency direction (incl. composition-root exception amended during Story 1.1 code review)]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-2 — DbContext lifetime: per-operation via factory]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-6 — Auditing via IAuditable, no soft-delete]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-9 — Repository + Unit of Work is the only persistence boundary (incl. public AppDbContext exception amended during Story 1.1 code review)]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-12 — Domain entities never cross the BLL→Presentation boundary]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Consistency Conventions] — naming, data/formats, validation & error handling, logging
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Stack] — EF Core version pin
- [Source: _bmad-output/implementation-artifacts/1-1-solution-scaffold-composition-root.md] — established scaffold, composition root, code-review-amended AD-1/AD-9 composition-root exception, `Directory.Build.props`/`global.json`/`.gitignore`, EF Core `10.0.10` pin, `ValidateOnBuild`

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build` (whole solution, `OrderFlow.sln`): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 4, Skipped: 0, Total: 4. (Post-review: 10, Skipped: 0, Total: 10.)
- `dotnet tool run dotnet-ef migrations add InitialCustomer --project OrderFlow.DAL --startup-project OrderFlow.DAL`: succeeded, no live LocalDB connection required (model-based generation, confirmed working on this macOS dev machine).
- `dotnet tool run dotnet-ef migrations has-pending-model-changes --project OrderFlow.DAL --startup-project OrderFlow.DAL` (post-review): "No changes have been made to the model since the last migration." — confirms making `Customers` `internal` and adding `base.OnModelCreating(...)` did not drift the EF model away from the already-generated `InitialCustomer` migration.

### Completion Notes List

- `Customer` entity + `CustomerConfiguration` added; `AppDbContext` now has `DbSet<Customer>`, `OnModelCreating` applying the configuration, and an overridden `SaveChanges`/`SaveChangesAsync` stamping `IAuditable.CreatedAt`/`UpdatedAt` per AD-6 (`UpdatedAt` on Added/Modified, `CreatedAt` only on Added).
- EF Core migrations tooling stood up for the first time in this codebase: `Microsoft.EntityFrameworkCore.Design` (`PrivateAssets="all"`) added to `OrderFlow.DAL`, a design-time-only `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` implemented, `dotnet-ef` installed as a local tool (`dotnet-tools.json` manifest created). Generated migration `InitialCustomer` creates the `Customers` table matching the entity/config exactly (verified by reading the generated migration).
- `IUnitOfWork`/`UnitOfWork` introduced for the first time (both `OrderFlow.DAL`, per AD-1's literal "BLL references DAL interfaces"). `UnitOfWork` constructs its `AppDbContext` via the **synchronous** `IDbContextFactory<AppDbContext>.CreateDbContext()` at construction (DI constructors can't `await`; this is a real sync EF Core API, not a blocking-on-Task antipattern), constructs `CustomerRepository` internally (never independently DI-registered, per AD-9), and implements both `IDisposable`/`IAsyncDisposable` for Story 1.3's upcoming Presenter-per-action scope disposal.
- `CustomerRepository.GetByIdAsync` intentionally does **not** call `.AsNoTracking()` — `CustomerService.UpdateAsync` relies on the returned entity staying tracked so direct property mutation is picked up by EF's change tracker (no blanket `Update()`/reattach, per AD-6).
- `Result<T>` (first use in this codebase) and `CustomerDto` placed in `OrderFlow.BLL` per the Architecture Spine's Structural Seed (Domain reserved for entities/enums/`IAuditable` only). `CustomerService` implements Create/Get/GetAll/Update, validating `Name`/`Email` as non-empty and returning `Result<T>.Failure(...)` rather than throwing; `GetAsync`/`UpdateAsync` return `Result<T>.Failure("Customer not found")` on a missing id rather than risking a null dereference.
- `Program.cs` composition root registers `IUnitOfWork`/`ICustomerService` as Scoped; `ServiceProviderOptions.ValidateOnBuild`/`ValidateScopes` (added in Story 1.1's code review) passed cleanly against the new registrations, confirming the DI graph resolves correctly.
- `OrderFlow.Tests.csproj` gained a `ProjectReference` to `OrderFlow.DAL` (needed to mock `IUnitOfWork`/`ICustomerRepository` — a superseding clarification of Story 1.1 AC#3, not a violation of it, per the Architecture Spine's own `Tests -.mocks.-> DAL` diagram) and `Moq 4.20.72` (first mocking library in this codebase). `CustomerServiceTests` covers a success path (asserts `Result.IsSuccess`, correct DTO shape, and verifies `SaveChangesAsync()`/`AddAsync()` were actually called) and a validation-failure path via `[Theory]` over missing `Name`/missing `Email` (asserts failure and that persistence was never attempted).
- No `UNVERIFIED-ENVIRONMENT` gaps introduced by this story — unlike Story 1.1's WinForms runtime-launch gap, everything in this story (build, tests, migration generation) is fully verifiable on macOS. The pre-existing Story 1.1 gap (`MainForm` launch) remains open and unaffected by this story.
- **Code review (2026-08-07) — 10 patches applied:** `AppDbContext.Customers` changed `public` → `internal` (AD-9 literal compliance — same-assembly `CustomerRepository` unaffected, `has-pending-model-changes` confirms no EF model drift); fixed a real behavioral bug in `UpdateAsync` where DTO validation ran *before* the not-found check (now fetches first, matching Task 5's spec — a missing customer now always reports "Customer not found" regardless of DTO shape, covered by a new regression test); added a null-`dto` guard, proactive max-length validation (mirroring `CustomerConfiguration`'s 200/256/30 limits) so an over-length input returns a clean `Result<T>.Failure` instead of an unhandled `DbUpdateException`; added `base.OnModelCreating(...)`; trimmed `Name`/`Email`/`Phone` on write; extracted the duplicated `"Customer not found"` literal to a `NotFoundError` const; documented that `CreateAsync` ignores any caller-set `Id`; added a Story 1.1 cross-reference note for the Tests→DAL supersession; added 6 new tests (`GetAsync` found/not-found, `GetAllAsync`, `UpdateAsync` success/regression/validation-failure) — test count 4 → 10, all passing.
- **Deliberately dismissed, not fixed:** wrapping `SaveChangesAsync()` in try/catch to convert infrastructure exceptions into `Result<T>.Failure` — the architecture's own Validation & error handling convention reserves exceptions for infrastructure failures, surfaced through the global handler (Story 1.1), not per-service catches. `RowVersion`/optimistic concurrency on `Customer` — explicitly out of scope per this story's own Dev Notes (Inventory gets it first, Story 1.4).

### File List

- `OrderFlow/OrderFlow.Domain/Customer.cs` (new)
- `OrderFlow/OrderFlow.DAL/CustomerConfiguration.cs` (new)
- `OrderFlow/OrderFlow.DAL/AppDbContext.cs` (modified: `DbSet<Customer>`, `OnModelCreating`, `SaveChanges`/`SaveChangesAsync` audit-stamp override)
- `OrderFlow/OrderFlow.DAL/AppDbContextFactory.cs` (new — design-time only)
- `OrderFlow/OrderFlow.DAL/OrderFlow.DAL.csproj` (modified: + `Microsoft.EntityFrameworkCore.Design` 10.0.10, `PrivateAssets="all"`)
- `OrderFlow/OrderFlow.DAL/Migrations/20260807023318_InitialCustomer.cs` (new)
- `OrderFlow/OrderFlow.DAL/Migrations/20260807023318_InitialCustomer.Designer.cs` (new)
- `OrderFlow/OrderFlow.DAL/Migrations/AppDbContextModelSnapshot.cs` (new)
- `OrderFlow/OrderFlow.DAL/IUnitOfWork.cs` (new)
- `OrderFlow/OrderFlow.DAL/UnitOfWork.cs` (new)
- `OrderFlow/OrderFlow.DAL/ICustomerRepository.cs` (new)
- `OrderFlow/OrderFlow.DAL/CustomerRepository.cs` (new)
- `OrderFlow/OrderFlow.BLL/Result.cs` (new)
- `OrderFlow/OrderFlow.BLL/CustomerDto.cs` (new)
- `OrderFlow/OrderFlow.BLL/ICustomerService.cs` (new)
- `OrderFlow/OrderFlow.BLL/CustomerService.cs` (new; modified during code review: `UpdateAsync` fetch/validate reorder, null-`dto` guard, max-length validation, trimming, `NotFoundError` const, `CreateAsync` doc note)
- `OrderFlow/OrderFlow.Presentation/Program.cs` (modified: register `IUnitOfWork`, `ICustomerService`)
- `OrderFlow/OrderFlow.Tests/OrderFlow.Tests.csproj` (modified: + `ProjectReference` to `OrderFlow.DAL`, + `Moq` 4.20.72)
- `OrderFlow/OrderFlow.Tests/CustomerServiceTests.cs` (new; expanded during code review: + `GetAsync`/`GetAllAsync`/`UpdateAsync` tests, incl. reorder-bug regression test)
- `OrderFlow/dotnet-tools.json` (new — local tool manifest, `dotnet-ef` 10.0.10)
- `_bmad-output/implementation-artifacts/1-1-solution-scaffold-composition-root.md` (modified during code review: AC#3 superseded-by cross-reference note)

## Change Log

- 2026-08-07: Implemented Story 1.2 — `Customer` entity/config/migration, AD-6 audit-stamp override, first `IUnitOfWork`/`UnitOfWork`/`ICustomerRepository`/`CustomerRepository`, first `Result<T>`/`CustomerDto`/`ICustomerService`/`CustomerService`, composition-root DI registration, `CustomerServiceTests` with Moq. `dotnet build`/`dotnet test` green; `dotnet ef migrations add` verified working without a live LocalDB connection.
- 2026-08-07: Code review applied — fixed a real `UpdateAsync` validate-before-fetch ordering bug (Acceptance Auditor finding), made `AppDbContext.Customers` `internal` per AD-9, added null-`dto`/max-length guards, `base.OnModelCreating`, input trimming, a shared not-found constant, and 6 new tests (4 → 10, all passing). 5 items deferred to `deferred-work.md`; `dotnet ef migrations has-pending-model-changes` confirms no EF model drift from the review changes.
