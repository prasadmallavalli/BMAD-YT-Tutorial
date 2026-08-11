---
baseline_commit: NO_VCS
---

# Story 1.4: Product & Inventory Domain, Repository & Service

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want Product and Inventory entities, repositories, and services wired through the Unit of Work,
so that catalog and stock-level data can be persisted per the architecture's 1:1 Product↔Inventory model.

## Acceptance Criteria

1. **Given** `OrderFlow.Domain`, **When** complete, **Then** a `Product` entity (`Id`, `Name`, `SKU`, `UnitPrice`, `IAuditable`) and an `Inventory` entity (`Id`, `ProductId` FK 1:1, `StockQuantity`, `RowVersion` per AD-10) exist, with configs in `OrderFlow.DAL`, and a migration adds both tables with the 1:1 relationship.
2. **And Given** `OrderFlow.DAL`, **When** implemented, **Then** `IProductRepository`/`IInventoryRepository` are exposed only via `IUnitOfWork.Products`/`.Inventory` (AD-9).
3. **And Given** `OrderFlow.BLL`, **When** implemented, **Then** `IProductService` exposes async CRUD over `ProductDto`, and `IInventoryService` exposes `GetStockLevelAsync(productId)` and the sole `HasSufficientStockAsync(productId, quantity)` method (AD-13) that Order stories will reuse. **"CRUD" here means Create/Read/Update only, no Delete** — see Dev Notes for why.
4. **And Given** `OrderFlow.Tests`, **When** complete, **Then** `ProductService`/`InventoryService` are covered with mocked `IUnitOfWork`, including `HasSufficientStockAsync` true/false cases.

## Tasks / Subtasks

- [x] Task 1: `Product`/`Inventory` entities + EF configuration (AC: #1)
  - [x] `OrderFlow.Domain/Product.cs`: `int Id`, `string Name`, `string SKU`, `decimal UnitPrice`, `Inventory? Inventory` (nav property — see Dev Notes on why this and not a two-step insert), implements `IAuditable`
  - [x] `OrderFlow.Domain/Inventory.cs`: `int Id`, `int ProductId`, `int StockQuantity`, `byte[] RowVersion`, implements `IAuditable` (see Dev Notes — the AC's field list for `Inventory` doesn't repeat "IAuditable" the way `Product`'s does, but AD-6 binds "all Domain entities" unconditionally; treat the AC's omission as non-exhaustive shorthand, not a deliberate exclusion)
  - [x] `OrderFlow.DAL/ProductConfiguration.cs`: table `Products`, `Name`/`SKU` required (`nvarchar(200)`/`nvarchar(50)` — not specified elsewhere, dev's call), `UnitPrice` as `decimal(18,2)`; configure the 1:1 relationship: `builder.HasOne(p => p.Inventory).WithOne().HasForeignKey<Inventory>(i => i.ProductId)` (unidirectional — `Inventory` has no back-navigation to `Product`, just the FK scalar)
  - [x] `OrderFlow.DAL/InventoryConfiguration.cs`: table `Inventory` (singular — explicit `.ToTable("Inventory")`; EF Core doesn't auto-pluralize table names by default, but naming it explicitly avoids any ambiguity about the deliberate choice), `RowVersion` configured via `.IsRowVersion()` (EF Core's concurrency-token + SQL Server `rowversion` column type)
  - [x] Add `internal DbSet<Product> Products` and `internal DbSet<Inventory> Inventory` to `AppDbContext` (same `internal` visibility as `Customers`, per AD-9 — see Story 1.2 code review); apply both new configurations in `OnModelCreating`
- [x] Task 2: Migration (AC: #1)
  - [x] `dotnet tool run dotnet-ef migrations add AddProductAndInventory --project OrderFlow.DAL --startup-project OrderFlow.DAL` (tooling already set up in Story 1.2 — `AppDbContextFactory`, local `dotnet-ef` tool)
  - [x] Confirm the generated migration creates `Products` and `Inventory` tables, a `ProductId` FK on `Inventory`, and — critically — a **unique index** on `Inventory.ProductId` (EF Core's convention for a `WithOne` relationship's dependent FK; this is what actually enforces "1:1" at the database level, not just "1:many with an unused nav property" — read the generated migration to confirm this, don't assume it) — **confirmed: `IX_Inventory_ProductId` generated with `unique: true`**
- [x] Task 3: `ConcurrencyConflictException` + `UnitOfWork.SaveChangesAsync` translation (AC: #1, AD-10) — **first entity with `RowVersion`, first concurrency-handling code in this codebase**
  - [x] `OrderFlow.DAL/ConcurrencyConflictException.cs`: plain `Exception` subclass, no EF Core types in its public shape (constructor takes `string message` and inner `Exception`) — see Dev Notes on why this exists instead of catching `DbUpdateConcurrencyException` directly in BLL
  - [x] `UnitOfWork.SaveChangesAsync()`: wrap `_context.SaveChangesAsync()` in `try`/`catch (DbUpdateConcurrencyException ex)`, throwing `new ConcurrencyConflictException("...", ex)` — this is a **DRY, one-time** change at the single `SaveChangesAsync` choke point, so every future entity/story that adds a `RowVersion` column inherits this translation for free without touching `UnitOfWork` again
- [x] Task 4: `IProductRepository`/`ProductRepository`, `IInventoryRepository`/`InventoryRepository`, `IUnitOfWork` additions (AC: #2)
  - [x] `OrderFlow.DAL/IUnitOfWork.cs`: add `IProductRepository Products { get; }` and `IInventoryRepository Inventory { get; }`
  - [x] `OrderFlow.DAL/UnitOfWork.cs`: construct `ProductRepository`/`InventoryRepository` alongside the existing `CustomerRepository`, same pattern (constructor takes the shared `_context`, never independently DI-registered — AD-9)
  - [x] `OrderFlow.DAL/IProductRepository.cs`: `Task<Product?> GetByIdAsync(int id)`, `Task<IReadOnlyList<Product>> GetAllAsync()`, `Task AddAsync(Product product)`
  - [x] `OrderFlow.DAL/ProductRepository.cs`: `GetByIdAsync`/`GetAllAsync` **must** `.Include(p => p.Inventory)` — without it, every `Product.Inventory` comes back `null` and `ProductDto.StockQuantity` silently reads as 0/missing. Same "no `.AsNoTracking()`" rule as `CustomerRepository.GetByIdAsync` (Story 1.2) — the returned `Product` (and its included `Inventory`) must stay tracked so `ProductService.UpdateAsync`'s direct-mutation pattern works
  - [x] `OrderFlow.DAL/IInventoryRepository.cs`: `Task<Inventory?> GetByProductIdAsync(int productId)`
  - [x] `OrderFlow.DAL/InventoryRepository.cs`: implements the above via `_context.Inventory.FirstOrDefaultAsync(i => i.ProductId == productId)` (also tracked, no `AsNoTracking`)
- [x] Task 5: `ProductDto` + `IProductService`/`ProductService` (AC: #3)
  - [x] `OrderFlow.BLL/ProductDto.cs`: `Id`, `Name`, `SKU`, `UnitPrice`, `StockQuantity` — see Dev Notes on why `StockQuantity` lives on `ProductDto` rather than a separate DTO, even though `Product` and `Inventory` are separate entities/repositories
  - [x] `OrderFlow.BLL/IProductService.cs`: `Task<Result<ProductDto>> CreateAsync(ProductDto dto)`, `Task<Result<ProductDto>> GetAsync(int id)`, `Task<Result<IReadOnlyList<ProductDto>>> GetAllAsync()`, `Task<Result<ProductDto>> UpdateAsync(int id, ProductDto dto)`
  - [x] `OrderFlow.BLL/ProductService.cs`: constructor takes `IUnitOfWork`. `Validate(dto)`: `Name`/`SKU` non-empty (+ max-length checks mirroring `ProductConfiguration`'s 200/50), `UnitPrice > 0`, `StockQuantity >= 0` — all `Result<T>.Failure(...)`, never throws
    - `CreateAsync`: validate → build `new Product { Name, SKU, UnitPrice, Inventory = new Inventory { StockQuantity = dto.StockQuantity } }` (setting the nav property lets EF Core cascade-insert both rows in one `SaveChangesAsync`, correctly wiring the FK — no need to know the generated `Product.Id` ahead of time) → `Products.AddAsync(product)` → `SaveChangesAsync()` → map back → `Result<ProductDto>.Success(...)`
    - `GetAsync`/`UpdateAsync`: fetch **before** validating (same ordering fix Story 1.2's code review applied to `CustomerService.UpdateAsync` — a missing product must report "Product not found" regardless of DTO shape, not a field-validation error)
    - `UpdateAsync` (once found): validate → mutate `product.Name`/`SKU`/`UnitPrice` **and** `product.Inventory!.StockQuantity` directly on the tracked entity (same targeted-property-change pattern as `CustomerService`, AD-6) → `SaveChangesAsync()` wrapped in `try`/`catch (ConcurrencyConflictException)` → `Result<ProductDto>.Failure("This product was modified by another user. Please reload and try again.")` on conflict, otherwise map back and `Result<ProductDto>.Success(...)`
    - `CreateAsync` does **not** need the `ConcurrencyConflictException` catch — an `INSERT` can never hit a concurrency conflict, only `UPDATE`s can
- [x] Task 6: `IInventoryService`/`InventoryService` (AC: #3)
  - [x] `OrderFlow.BLL/IInventoryService.cs`: `Task<Result<int>> GetStockLevelAsync(int productId)`, `Task<Result<bool>> HasSufficientStockAsync(int productId, int requestedQuantity)` — these are the **final, exact** method names (epics.md/AD-13 write them without the `Async` suffix as shorthand; this codebase's established convention, per every other service so far, always suffixes async methods with `Async` — Epic 2's `OrderService`, when it calls these, must use these exact suffixed names, not epics.md's shorthand)
  - [x] `OrderFlow.BLL/InventoryService.cs`: constructor takes `IUnitOfWork`. Both methods call `Inventory.GetByProductIdAsync(productId)`; if `null`, `Result<T>.Failure("Product not found")`; otherwise `GetStockLevelAsync` returns `Result<int>.Success(inventory.StockQuantity)`, `HasSufficientStockAsync` returns `Result<bool>.Success(inventory.StockQuantity >= requestedQuantity)` — this comparison is the **one and only** place stock sufficiency is evaluated in the entire codebase (AD-13); Epic 2's `OrderService` will call this method rather than reimplementing the check
- [x] Task 7: Composition root registration (AC: #2, #3, AD-9)
  - [x] `Program.cs` `ConfigureServices`: `services.AddScoped<IProductService, ProductService>();` and `services.AddScoped<IInventoryService, InventoryService>();`
  - [x] Do **not** register `IProductRepository`/`ProductRepository`/`IInventoryRepository`/`InventoryRepository` in DI — `UnitOfWork` constructs them internally (AD-9)
- [x] Task 8: BLL tests with mocked `IUnitOfWork` (AC: #4)
  - [x] `ProductServiceTests`: `CreateAsync` success path (verify `SaveChangesAsync` called, resulting DTO shape correct) and validation-failure path (missing `Name`/`SKU`, non-positive `UnitPrice`, negative `StockQuantity`); `UpdateAsync` fetch-before-validate regression test (mirroring Story 1.2's `UpdateAsync_WithMissingId_ReturnsNotFoundEvenWhenDtoIsInvalid`); `UpdateAsync` concurrency-conflict path (mock `IUnitOfWork.SaveChangesAsync()` to throw `ConcurrencyConflictException`, assert `Result.IsSuccess == false` with the friendly message, not an unhandled exception escaping the service)
  - [x] `InventoryServiceTests`: `HasSufficientStockAsync` **true** case (`StockQuantity >= requestedQuantity`) and **false** case (`StockQuantity < requestedQuantity`) — explicitly required by AC #4; `GetStockLevelAsync` found/not-found paths
  - [x] **Known gap, accepted as deferred (same posture as Story 1.2/1.3's deferred items):** no test exercises `UnitOfWork.SaveChangesAsync()`'s actual `DbUpdateConcurrencyException`-to-`ConcurrencyConflictException` translation itself — the `ProductServiceTests` concurrency test (above) mocks `IUnitOfWork.SaveChangesAsync()` to throw `ConcurrencyConflictException` directly, verifying `ProductService`'s handling of it, not `UnitOfWork`'s production of it. This is the first concurrency-handling code in the codebase and ships with that one link unverified; a real integration test would need a live/in-memory database exercising an actual concurrent write, which is out of scope for this story's mocked-`IUnitOfWork` testing approach.
- [x] Task 9: Verify end-to-end
  - [x] `dotnet build` succeeds for the whole solution — 0 errors, 0 warnings
  - [x] `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` passes, including all new tests — **confirmed: 24/24 passed (10 pre-existing + 14 new)**
  - [x] `dotnet ef migrations add` succeeds without a live LocalDB connection (same as Story 1.2's precedent); read the generated migration to confirm the `Products`/`Inventory` tables and the unique index on `Inventory.ProductId` (Task 2) — **confirmed in Task 2**

### Review Findings

- [x] [Review][Patch] `AppDbContext.SaveChanges()` (the public sync override) has no `DbUpdateConcurrencyException`→`ConcurrencyConflictException` translation — only `UnitOfWork.SaveChangesAsync()` does. Nothing calls the sync path today, but the "one choke point every future entity inherits" claim in Dev Notes is only true for the async path. Wrap the sync override in the same try/catch for symmetry. [`OrderFlow.DAL/AppDbContext.cs:31-35`]
- [x] [Review][Patch] The friendly concurrency message is defined twice and has drifted: `UnitOfWork.SaveChangesAsync` constructs "The record was modified by another user..." but `ProductService.UpdateAsync` discards it and hardcodes "This product was modified by another user..." instead of using the caught exception's `Message`. Two independently-maintained copies of the same text. [`OrderFlow.BLL/ProductService.cs:88-91`, `OrderFlow.DAL/UnitOfWork.cs:36-37`]
- [x] [Review][Patch] `InventoryService.HasSufficientStockAsync` doesn't validate `requestedQuantity` — a negative value trivially satisfies `StockQuantity >= requestedQuantity` and incorrectly reports sufficient stock. As "the one and only place stock sufficiency is evaluated" (AD-13), it should reject nonsensical input rather than assume well-formed callers. [`OrderFlow.BLL/InventoryService.cs:26-32`]
- [x] [Review][Patch] `ProductServiceTests` has zero coverage for `GetAsync`/`GetAllAsync` — only `CreateAsync`/`UpdateAsync` are tested. `InventoryServiceTests` covers the equivalent found/not-found paths for its sibling service; `ProductService` should match. This mirrors a gap Story 1.2's own code review already caught and fixed once for `CustomerService` — same category, recurring.
- [x] [Review][Defer] `Product.SKU` has no unique-index enforcement — deferred, pre-existing pattern (same as Story 1.2's dismissed `Customer.Email` uniqueness finding); not required by this story's AC or the PRD.
- [x] [Review][Defer] No validation that `UnitPrice` fits `decimal(18,2)`'s precision/scale before hitting the DB — deferred; a value large enough to overflow is not a realistic data-entry scenario, low priority.
- [x] [Review][Defer] No DAL/EF-configuration-level test coverage (in-memory/SQLite) for the `Product`↔`Inventory` 1:1 relationship or `.Include` behavior — deferred, extends the same gap already tracked from Story 1.2 (`CustomerRepository`/`UnitOfWork`/`StampAuditableEntries` untested at the DAL level); `deferred-work.md`'s existing entry updated to cover Product/Inventory too.
- [x] [Review][Defer] No test exercises `UnitOfWork.SaveChangesAsync()`'s actual `DbUpdateConcurrencyException`→`ConcurrencyConflictException` translation (only `ProductService`'s handling of an already-thrown `ConcurrencyConflictException` is tested) — already self-disclosed as an accepted gap in this story's own Task 8; reconfirmed by two independent reviewers.
- [x] [Review][Defer] `NotFoundError = "Product not found"` is now duplicated verbatim across `ProductService` and `InventoryService` (same pattern as `CustomerService`'s own constant) — deferred per the rule of three; revisit if a third service duplicates it.

## Dev Notes

- **"CRUD" in AC #3 means Create/Read/Update only — no `DeleteAsync`.** This is a deliberate reading, not an oversight: FR-2's verb list is "create, view, edit, and list Products" (no delete), and Story 1.5's own AC (the UI story consuming this one) never mentions a delete action anywhere. Story 1.2's equivalent AC for `Customer` used the more precise "Create/Get/GetAll/Update" wording specifically to avoid this ambiguity; Story 1.4's source AC in epics.md uses the word "CRUD" instead, which is why this note exists — don't let the literal word "CRUD" drive adding a `DeleteAsync`/`IProductRepository.Delete` that nothing else in the spec asks for or expects.
- **`ConcurrencyConflictException` is a sanctioned exception to AD-9's "BLL never depends on EF Core types directly" — now written into AD-9 itself** (not just this story's Dev Notes), mirroring how Story 1.1's composition-root exception and Story 1.3's Form-launching exception were folded into their respective ADs directly rather than left implicit. See `ARCHITECTURE-SPINE.md` AD-9's "Concurrency-exception exception" sub-bullet.
- **`product.Inventory!` (null-forgiving) is safe under this story's own invariant, not in general:** every `Product` in this codebase is created with an `Inventory` in the same atomic insert (Task 5's `CreateAsync`), and nothing in this story adds a way to create a `Product` without one or to delete an `Inventory` independently. If a future story ever allows either, this invariant — and every `product.Inventory!` call site — needs revisiting.
- **Builds directly on Story 1.2's `IUnitOfWork`/`UnitOfWork`/`Result<T>` foundation** — this story adds sibling repositories/services alongside `Customers`, following the exact same shape. No new architectural pattern beyond the 1:1 relationship and concurrency handling below.
- **Why `Product.Inventory` is a navigation property, not a two-step insert:** setting `product.Inventory = new Inventory { StockQuantity = dto.StockQuantity }` before `Products.AddAsync(product)` lets EF Core's change tracker cascade-insert both rows in **one** `SaveChangesAsync()` call, correctly wiring the FK without needing to know the generated `Product.Id` first. A manual two-step (`Add` Product → `SaveChanges` → set `Inventory.ProductId` → `Add` Inventory → `SaveChanges` again) works but is more code, two round-trips, and easier to get wrong. Use the nav-property approach.
- **Why `Inventory` has no back-navigation to `Product`:** the AC's field list only asks for `ProductId` as a scalar FK on `Inventory`, not a full bidirectional graph. `HasForeignKey<Inventory>(i => i.ProductId)` on the `Product` side's `HasOne(...).WithOne()` is sufficient to configure the relationship (including the FK-uniqueness that makes it truly 1:1) without a `Inventory.Product` property nobody asked for.
- **Why `StockQuantity` lives on `ProductDto`, not a separate `InventoryDto`:** FR-2 explicitly bundles "stock quantity" into the same Product create/edit surface ("User can create, view, edit, and list Products (name, SKU, unit price, stock quantity)"), and Story 1.5's own AC (epics.md) shows `ProductListForm` displaying `StockQuantity` alongside `Name`/`SKU`/`UnitPrice` as one row — the UI treats Product+Inventory as one concept the user manages, even though they're separate entities/tables/repositories underneath. `IInventoryService` stays a narrower, separate BLL surface (`GetStockLevelAsync`/`HasSufficientStockAsync`) because AD-13 needs one unambiguous owner for the sufficiency check that Epic 2's `OrderService` will call independently of any Product CRUD screen.
- **Why `ConcurrencyConflictException` exists instead of catching `DbUpdateConcurrencyException` directly in BLL:** AD-9 states "`OrderFlow.BLL`... never [depends] on EF Core types directly." `DbUpdateConcurrencyException` is an EF Core type (`Microsoft.EntityFrameworkCore`); catching it in `ProductService` would require adding an EF Core package reference to `OrderFlow.BLL`, which doesn't exist and shouldn't. `UnitOfWork.SaveChangesAsync()` (in `OrderFlow.DAL`, which *is* allowed to reference EF Core) catches the real EF Core exception and rethrows a plain, DAL-defined `ConcurrencyConflictException` that BLL can safely catch without any EF Core dependency.
- **Why this concurrency handling is being built now, not deferred to Epic 2:** AD-10's rule text is unconditional present tense ("Repository `Update`/`SaveChanges` calls that hit a `DbUpdateConcurrencyException` translate it into a `Result<T>` failure... never an unhandled exception reaching Presentation") — it doesn't say "once decrement logic exists." This story's own AC1 assigns `RowVersion` to `Inventory` *now*, and `ProductService.UpdateAsync` *now* has a real `UPDATE` path touching `Inventory.StockQuantity` (per FR-2's edit-includes-stock bundling above) — so the failure mode AD-10 describes is reachable in this story, not hypothetical. Building the translation at the single `UnitOfWork.SaveChangesAsync()` choke point means Epic 2's eventual decrement logic inherits it for free.
- **This does *not* contradict Story 1.2's dismissed "wrap `SaveChangesAsync` in try/catch" code-review finding.** That finding was about *generic* infrastructure exceptions (DB unavailable, etc.), which the architecture's Validation & error handling convention explicitly wants surfaced through the *global* handler (`Application.ThreadException`, Story 1.1), not swallowed per-service. `DbUpdateConcurrencyException` is different: AD-10 is a **specific, named carve-out** requiring exactly this one exception type to become a `Result<T>` failure. Don't generalize this into catching other exception types in `ProductService`/`InventoryService` — only `ConcurrencyConflictException`, only in `UpdateAsync`.
- **`AppDbContext.Products`/`.Inventory` `DbSet<T>` properties are `internal`, matching `Customers`** — this was a Story 1.2 code-review fix (AD-9 literal compliance), now the established pattern for every future `DbSet<T>` this codebase adds. Don't reintroduce `public`.
- **Repository update pattern (AD-6, reaffirmed):** `ProductRepository`/`InventoryRepository` have no `Update(...)` methods, same as `CustomerRepository` — `ProductService.UpdateAsync` mutates the already-tracked entity's properties directly (including `product.Inventory!.StockQuantity`, since `.Include(p => p.Inventory)` keeps that navigation tracked by the same shared `DbContext` too) and lets EF's change tracker do targeted property updates on `SaveChangesAsync()`.
- **Naming conventions (unchanged from Story 1.2):** `IXxx` interfaces; `IXxxRepository`/`XxxRepository`; `IXxxService`/`XxxService`; `XxxDto`; `XxxConfiguration : IEntityTypeConfiguration<Xxx>`.
- **Data & formats:** `Id`s are `int` identity. `UnitPrice` is `decimal` (never `float`/`double`, per the Consistency Conventions table), mapped `decimal(18,2)`. `RowVersion` is `byte[]`, EF Core `.IsRowVersion()` (maps to SQL Server `rowversion`).

### Project Structure Notes

```
OrderFlow/
  OrderFlow.Domain/
    Product.cs                       # new
    Inventory.cs                     # new
  OrderFlow.DAL/
    AppDbContext.cs                  # modified: + DbSet<Product>, DbSet<Inventory>, OnModelCreating registrations
    ProductConfiguration.cs          # new
    InventoryConfiguration.cs        # new
    ConcurrencyConflictException.cs  # new
    UnitOfWork.cs                    # modified: + Products/Inventory repos, SaveChangesAsync concurrency translation
    IUnitOfWork.cs                   # modified: + Products/Inventory properties
    IProductRepository.cs            # new
    ProductRepository.cs             # new
    IInventoryRepository.cs          # new
    InventoryRepository.cs           # new
    Migrations/                      # modified: + AddProductAndInventory migration
  OrderFlow.BLL/
    ProductDto.cs                    # new
    IProductService.cs               # new
    ProductService.cs                # new
    IInventoryService.cs             # new
    InventoryService.cs              # new
  OrderFlow.Presentation/
    Program.cs                       # modified: register IProductService, IInventoryService
  OrderFlow.Tests/
    ProductServiceTests.cs           # new
    InventoryServiceTests.cs         # new
```

No conflicts with the Structural Seed — this is the second story to populate `OrderFlow.DAL`/`OrderFlow.BLL` beyond Story 1.2's Customer slice, following the identical established shape.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.4: Product & Inventory Domain, Repository & Service] — acceptance criteria origin
- [Source: _bmad-output/planning-artifacts/epics.md#FR-2] — "stock quantity" bundled into the Product create/edit surface
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.5: Product Management & Inventory Visibility UI] — confirms `ProductListForm` displays `StockQuantity` alongside Product fields as one row
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-6 — Auditing via IAuditable, no soft-delete]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-9 — Repository + Unit of Work is the only persistence boundary]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-10 — Optimistic concurrency via RowVersion token]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#AD-13 — InventoryService is the sole owner of stock-sufficiency checks]
- [Source: _bmad-output/implementation-artifacts/1-2-customer-domain-repository-service.md] — `IUnitOfWork`/`Result<T>`/tracked-entity-mutation patterns this story extends; the fetch-before-validate ordering fix this story replicates for `ProductService.UpdateAsync`

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `dotnet build OrderFlow.sln` (all 7 projects): Build succeeded, 0 Warning(s), 0 Error(s).
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 24, Skipped: 0, Total: 24 (10 pre-existing + 14 new).
- `dotnet tool run dotnet-ef migrations add AddProductAndInventory --project OrderFlow.DAL --startup-project OrderFlow.DAL`: succeeded, no live LocalDB connection required. Read the generated migration directly — confirmed `Products`/`Inventory` tables, FK, and a **unique** `IX_Inventory_ProductId` index (the actual DB-level 1:1 enforcement).

### Completion Notes List

- `Product`/`Inventory` entities and their EF configurations added. `Product.Inventory` is a nav property (unidirectional — no `Inventory.Product` back-nav); `ProductConfiguration` wires the 1:1 via `HasOne(p => p.Inventory).WithOne().HasForeignKey<Inventory>(i => i.ProductId)`. `Inventory` implements `IAuditable` despite the AC's field list not repeating it — resolved per AD-6's unconditional "all Domain entities" rule (see Dev Notes).
- First `RowVersion`/optimistic-concurrency code in this codebase: `InventoryConfiguration` sets `.IsRowVersion()`; `UnitOfWork.SaveChangesAsync()` now catches `DbUpdateConcurrencyException` and rethrows a plain, EF-Core-free `ConcurrencyConflictException` (new `OrderFlow.DAL` type) — a single DRY choke point every future `RowVersion`-carrying entity inherits for free. This required a new, explicit "Concurrency-exception exception" carve-out written into AD-9 itself in the Architecture Spine (proactively, at story-creation time, mirroring what Story 1.3's code review had to retrofit after the fact).
- `IProductRepository`/`ProductRepository` and `IInventoryRepository`/`InventoryRepository` added alongside the existing `CustomerRepository` on `IUnitOfWork`, same construction pattern (never independently DI-registered, AD-9). `ProductRepository.GetByIdAsync`/`GetAllAsync` both `.Include(p => p.Inventory)` — confirmed necessary, without it `ProductDto.StockQuantity` would silently read as 0.
- `ProductService` implements Create/Read/Update only (explicitly not Delete — AC #3's literal "CRUD" wording was reconciled against FR-2 and Story 1.5's AC, neither of which mention deleting a Product; see Dev Notes). `CreateAsync` builds a `Product` with its `Inventory` nav pre-attached so EF Core cascade-inserts both rows in one `SaveChangesAsync`. `UpdateAsync` fetches before validating (Story 1.2 precedent), mutates `product.Name`/`SKU`/`UnitPrice` and `product.Inventory!.StockQuantity` directly on the tracked graph, and is the one place in this story that catches `ConcurrencyConflictException`, translating it to a friendly `Result<T>.Failure` per AD-10. `CreateAsync` deliberately does not catch it — an `INSERT` can't hit a concurrency conflict.
- `InventoryService` implements `GetStockLevelAsync`/`HasSufficientStockAsync` (final `Async`-suffixed names, per this codebase's convention — epics.md/AD-13 write them without the suffix as shorthand). `HasSufficientStockAsync`'s `StockQuantity >= requestedQuantity` comparison is the sole stock-sufficiency check in the codebase (AD-13), ready for Epic 2's `OrderService` to call directly.
- `Program.cs` composition root registers `IProductService`/`IInventoryService` as Scoped; `ValidateOnBuild`/`ValidateScopes` (Story 1.1) passed cleanly against the new registrations at build time (though full runtime resolution remains the same pre-existing `UNVERIFIED-ENVIRONMENT` gap as `MainForm`'s launch on this macOS machine).
- `ProductServiceTests`/`InventoryServiceTests` added: 9 + 5 = 14 new tests, covering Create success/5 validation-failure cases, the `UpdateAsync` fetch-before-validate regression (mirroring Story 1.2's), the concurrency-conflict path, and `HasSufficientStockAsync`'s explicit true/false cases (AC #4). One gap accepted as deferred: no test exercises `UnitOfWork.SaveChangesAsync()`'s actual `DbUpdateConcurrencyException`→`ConcurrencyConflictException` translation itself (would need a live/in-memory DB, out of scope for mocked-`IUnitOfWork` testing).
- No new `UNVERIFIED-ENVIRONMENT` gaps beyond what already existed — this story is pure DAL/BLL, fully verifiable on macOS (build, tests, migration generation all succeeded locally).
- **Code review (2026-08-09) — 4 patches applied:** wrapped `AppDbContext.SaveChanges()` (sync override) in the same `DbUpdateConcurrencyException` translation as the async path, closing a "one choke point" gap that only covered `SaveChangesAsync`; eliminated a duplicated/drifted friendly-message string by moving it to `ConcurrencyConflictException.DefaultMessage` (single source of truth — both throw sites and `ProductService.UpdateAsync`'s catch now reference it, `ProductService` no longer hardcodes its own copy); added `requestedQuantity < 0` validation to `InventoryService.HasSufficientStockAsync` (a negative value previously satisfied `>=` and silently reported sufficient stock); added `ProductServiceTests.GetAsync`/`GetAllAsync` coverage (previously zero, an asymmetry with `InventoryServiceTests`' equivalent coverage — the same category of gap Story 1.2's own review caught once already). Test count 24 → 28, all passing. 3 items deferred to `deferred-work.md` (extending an existing entry), 11 dismissed (including two false positives caught by direct verification: the model snapshot claim and `CreateAsync`'s "missing" concurrency catch, which is architecturally impossible for an `INSERT`).

### File List

- `OrderFlow/OrderFlow.Domain/Product.cs` (new)
- `OrderFlow/OrderFlow.Domain/Inventory.cs` (new)
- `OrderFlow/OrderFlow.DAL/ProductConfiguration.cs` (new)
- `OrderFlow/OrderFlow.DAL/InventoryConfiguration.cs` (new)
- `OrderFlow/OrderFlow.DAL/AppDbContext.cs` (modified: `DbSet<Product>`, `DbSet<Inventory>`, `OnModelCreating` registrations; modified again during code review: `SaveChanges()` sync-path concurrency translation)
- `OrderFlow/OrderFlow.DAL/ConcurrencyConflictException.cs` (new; modified during code review: single-arg constructor + `DefaultMessage` const, single source of truth for the friendly message)
- `OrderFlow/OrderFlow.DAL/UnitOfWork.cs` (modified: `Products`/`Inventory` repos, `SaveChangesAsync` concurrency translation; modified again during code review: use `ConcurrencyConflictException`'s new constructor)
- `OrderFlow/OrderFlow.DAL/IUnitOfWork.cs` (modified: `Products`/`Inventory` properties)
- `OrderFlow/OrderFlow.DAL/IProductRepository.cs` (new)
- `OrderFlow/OrderFlow.DAL/ProductRepository.cs` (new)
- `OrderFlow/OrderFlow.DAL/IInventoryRepository.cs` (new)
- `OrderFlow/OrderFlow.DAL/InventoryRepository.cs` (new)
- `OrderFlow/OrderFlow.DAL/Migrations/20260807092031_AddProductAndInventory.cs` (new)
- `OrderFlow/OrderFlow.DAL/Migrations/20260807092031_AddProductAndInventory.Designer.cs` (new)
- `OrderFlow/OrderFlow.DAL/Migrations/AppDbContextModelSnapshot.cs` (modified: regenerated by migration)
- `OrderFlow/OrderFlow.BLL/ProductDto.cs` (new)
- `OrderFlow/OrderFlow.BLL/IProductService.cs` (new)
- `OrderFlow/OrderFlow.BLL/ProductService.cs` (new; modified during code review: use `ex.Message` instead of a hardcoded duplicate string)
- `OrderFlow/OrderFlow.BLL/IInventoryService.cs` (new)
- `OrderFlow/OrderFlow.BLL/InventoryService.cs` (new; modified during code review: `requestedQuantity < 0` validation)
- `OrderFlow/OrderFlow.Presentation/Program.cs` (modified: register `IProductService`, `IInventoryService`)
- `OrderFlow/OrderFlow.Tests/ProductServiceTests.cs` (new; expanded during code review: `GetAsync`/`GetAllAsync` tests, updated `ConcurrencyConflictException` constructor call)
- `OrderFlow/OrderFlow.Tests/InventoryServiceTests.cs` (new; expanded during code review: negative-`requestedQuantity` test)
- `_bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md` (modified: AD-9 amended with the "Concurrency-exception exception" carve-out)

## Change Log

- 2026-08-07: Implemented Story 1.4 — `Product`/`Inventory` entities with a 1:1 EF relationship, migration (with unique `Inventory.ProductId` index confirmed), first `RowVersion`/optimistic-concurrency handling in the codebase (`ConcurrencyConflictException`, caught in `ProductService.UpdateAsync`), `IProductRepository`/`IInventoryRepository`, `ProductDto`/`IProductService`/`ProductService` (Create/Read/Update, no Delete), `IInventoryService`/`InventoryService` (`GetStockLevelAsync`/`HasSufficientStockAsync`, AD-13's sole stock-sufficiency check), composition-root registration, and 14 new tests. `dotnet build` green across all 7 projects; `dotnet test` 24/24 passed. AD-9 amended in the Architecture Spine to formally sanction the new `ConcurrencyConflictException` dependency.
- 2026-08-09: Code review applied — closed the sync-path `SaveChanges()` gap in the concurrency-translation "choke point," centralized the friendly conflict message onto `ConcurrencyConflictException.DefaultMessage` (was duplicated/drifted between `UnitOfWork` and `ProductService`), added negative-quantity validation to `HasSufficientStockAsync`, and added missing `ProductService.GetAsync`/`GetAllAsync` test coverage. 3 items deferred to `deferred-work.md`. `dotnet build`/`dotnet test` re-verified green (28/28) after all changes.
