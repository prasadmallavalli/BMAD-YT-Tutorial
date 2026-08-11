---
baseline_commit: NO_VCS
---

# Story 4.4: Interview Topic Map

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a viewer preparing for interviews,
I want `docs/interview-topic-map.md` mapping every named interview topic to the specific class/file demonstrating it,
so that I can quickly locate a working example for any topic.

## Acceptance Criteria

1. **Given** `docs/interview-topic-map.md`, **When** created, **Then** it lists every interview topic named across the PRD/Architecture (DI & Composition Root, Repository + Unit of Work, Strategy Pattern, Factory Pattern/keyed DI, SOLID: SRP/OCP/DIP, Optimistic Concurrency, Presenter/MVP, `Result<T>` error handling, async-all-the-way) and maps each to the specific class/file (real app) and exhibit pair demonstrating it.
2. **And Given** the map, **When** reviewed, **Then** no named topic is missing an entry, and no entry references a class/file that doesn't exist.
3. **And Given** new topics are added later, **When** the map is updated, **Then** it remains a single maintained file, per FR-13's "ships and maintains" requirement.

## Tasks / Subtasks

- [x] Task 1: Verify every real-app class/file reference before writing the map (AC #2)
  - [x] This is a documentation-only story with one hard constraint: AC #2 forbids a single dangling reference. Every path below was confirmed to exist in the current codebase during story creation (not assumed from Dev Notes elsewhere) — re-verify each with `ls`/`find` immediately before writing the doc, since Stories 4.1-4.3 may have touched `OrderFlow.Exhibits` since this list was compiled. All 26 paths confirmed present via `test -e` immediately before Task 2.
    - `OrderFlow.Presentation/Program.cs` (DI & Composition Root, SOLID: DIP)
    - `OrderFlow.DAL/IUnitOfWork.cs`, `OrderFlow.DAL/UnitOfWork.cs`, `OrderFlow.DAL/IOrderRepository.cs`, `OrderFlow.DAL/OrderRepository.cs` (Repository + Unit of Work)
    - `OrderFlow.BLL/IPricingStrategy.cs`, `OrderFlow.BLL/StandardPricingStrategy.cs` (Strategy Pattern, SOLID: OCP)
    - `OrderFlow.BLL/OrderProcessorFactory.cs`, `OrderFlow.BLL/IOrderProcessor.cs`, `OrderFlow.BLL/StandardOrderProcessor.cs`, `OrderFlow.BLL/RushOrderProcessor.cs` (Factory Pattern/keyed DI)
    - `OrderFlow.BLL/CustomerService.cs`, `OrderFlow.BLL/OrderStatusService.cs` (SOLID: SRP — representative examples, not one dedicated class, see Dev Notes)
    - `OrderFlow.Domain/Order.cs`, `OrderFlow.Domain/Inventory.cs`, `OrderFlow.DAL/UnitOfWork.cs`, `OrderFlow.DAL/ConcurrencyConflictException.cs` (Optimistic Concurrency)
    - `OrderFlow.Presentation/OrderDetailPresenter.cs`, `OrderFlow.Presentation/IOrderDetailView.cs`, `OrderFlow.Presentation/OrderDetailForm.cs` (Presenter/MVP)
    - `OrderFlow.BLL/Result.cs` (`Result<T>` error handling)
    - `OrderFlow.Presentation/OrderCreatePresenter.cs`, `OrderFlow.Presentation/OrderDetailPresenter.cs` (async-all-the-way)
    - `OrderFlow.Exhibits/Before/Srp/`, `OrderFlow.Exhibits/After/Srp/` (Story 4.1); `OrderFlow.Exhibits/Before/Ocp/`, `OrderFlow.Exhibits/After/Ocp/` (Story 4.2); `OrderFlow.Exhibits/Before/Dip/`, `OrderFlow.Exhibits/After/Dip/` (Story 4.3)
- [x] Task 2: Create `OrderFlow/docs/interview-topic-map.md` (AC #1, #2, #3)
  - [x] `docs/` doesn't exist yet under `OrderFlow/` — create it. This is the exact location the Architecture Spine's Structural Seed reserves for this file (`OrderFlow/docs/interview-topic-map.md # FR-13`) — not the outer BMad project's `docs/` (that one holds `project_knowledge`, unrelated).
  - [x] Structure as one Markdown table, columns: **Topic** | **Real App (class/file)** | **Exhibit Pair** | **Architecture Reference**. One row per topic (11 rows — SOLID's three sub-principles get their own row each, since each maps to different files/exhibits):
    | Topic | Real App | Exhibit Pair | Architecture Reference |
    |---|---|---|---|
    | DI & Composition Root | `OrderFlow.Presentation/Program.cs` (`ConfigureServices`) | `OrderFlow.Exhibits/{Before,After}/Dip` (Story 4.3) | AD-1, AD-2, AD-5 |
    | Repository + Unit of Work | `OrderFlow.DAL/IUnitOfWork.cs`, `UnitOfWork.cs` (e.g. `IOrderRepository.cs`/`OrderRepository.cs`) | N/A — real app only | AD-9 |
    | Strategy Pattern | `OrderFlow.BLL/IPricingStrategy.cs`, `StandardPricingStrategy.cs` | `OrderFlow.Exhibits/{Before,After}/Ocp` (Story 4.2) | AD-11 |
    | Factory Pattern / keyed DI | `OrderFlow.BLL/OrderProcessorFactory.cs`, `IOrderProcessor.cs`, `StandardOrderProcessor.cs`, `RushOrderProcessor.cs` | N/A — real app only | AD-7 |
    | SOLID: SRP | No single dedicated class — a general layering discipline (e.g. `OrderFlow.BLL/CustomerService.cs`, `OrderStatusService.cs`, each owning one responsibility) | `OrderFlow.Exhibits/{Before,After}/Srp` (Story 4.1) | — |
    | SOLID: OCP | `OrderFlow.BLL/IPricingStrategy.cs`, `StandardPricingStrategy.cs` | `OrderFlow.Exhibits/{Before,After}/Ocp` (Story 4.2) | AD-11 |
    | SOLID: DIP | `OrderFlow.Presentation/Program.cs` (`ConfigureServices` — every constructor-injected interface) | `OrderFlow.Exhibits/{Before,After}/Dip` (Story 4.3) | AD-1 |
    | Optimistic Concurrency | `OrderFlow.Domain/Order.cs`, `Inventory.cs` (`RowVersion`); `OrderFlow.DAL/UnitOfWork.cs` (`DbUpdateConcurrencyException` catch), `ConcurrencyConflictException.cs` | N/A — real app only | AD-10 |
    | Presenter/MVP | `OrderFlow.Presentation/OrderDetailPresenter.cs`, `IOrderDetailView.cs`, `OrderDetailForm.cs` | N/A — real app only | AD-3 |
    | `Result<T>` error handling | `OrderFlow.BLL/Result.cs` | N/A — real app only | Consistency Conventions |
    | async-all-the-way | `OrderFlow.Presentation/OrderCreatePresenter.cs`, `OrderDetailPresenter.cs` (`async Task` methods, no `.Result`/`.Wait()`) | N/A — real app only | NFR-1, AD-3 |
  - [x] Every "N/A — real app only" cell is a deliberate, honest entry, not a gap: Epic 4 built exactly three exhibit pairs (SRP/OCP/DIP); the other eight topics are demonstrated only in the real app. Writing "N/A" instead of inventing a fake exhibit reference is what keeps AC #2 satisfied (an honest N/A cannot be "a reference to a class/file that doesn't exist").
  - [x] Add a short intro paragraph above the table stating the file's purpose (mirrors AC #1's own framing) and a one-line maintenance note referencing FR-13 ("ships and maintains") so a future contributor knows to add a row here, not a separate file, when a new topic is introduced — this is what satisfies AC #3.
- [x] Task 3: Verify end-to-end (AC #2, #3)
  - [x] Re-check every path cited in the table actually exists on disk (`ls` each, or one `find`/`test -f` pass) — confirms AC #2 with the file as actually written, not just as planned in Task 1. All 26 re-confirmed present.
  - [x] Confirm all 11 topics named in AC #1's parenthetical list have exactly one row each (no omissions, no duplicates). Table has 13 `|`-prefixed lines (header + separator + 11 data rows) — exact match.
  - [x] Confirm no code files changed — this story only adds `OrderFlow/docs/interview-topic-map.md` (and the new `OrderFlow/docs/` directory). No `dotnet build`/`dotnet test` impact is possible from a Markdown-only change, but re-run `dotnet build OrderFlow.sln` and `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj` anyway as a cheap confirmation nothing else drifted — confirmed unchanged: 0 warnings, 82/82 tests.

### Review Findings

**✅ Clean review — no findings.** This is a pure documentation deliverable (no code changed) with one hard constraint — AC #2's "no entry references a class/file that doesn't exist." Independently re-verified (2026-08-11, not relying on this story's own Debug Log) all 20 distinct file paths and 6 exhibit-pair folders cited in `OrderFlow/docs/interview-topic-map.md` against the current codebase — all present. Also spot-checked three of the harder-to-fake substantive claims: `OrderFlow.Domain/Order.cs`/`Inventory.cs` genuinely have a `RowVersion` concurrency token, `OrderFlow.DAL/UnitOfWork.cs` genuinely catches `DbUpdateConcurrencyException`, and `OrderCreatePresenter.cs`/`OrderDetailPresenter.cs` are genuinely `async Task` throughout with no `.Result`/`.Wait()`. Table has exactly 11 data rows matching AC #1's topic list. Given a full adversarial/edge-case review layer would add no signal beyond this direct fact-check on a Markdown-only artifact, none was run.

## Dev Notes

- **This is the only Epic 4 story that touches no `.cs` file at all.** Stories 4.1-4.3 built the three exhibit pairs this map now indexes; this story's entire job is accurate cross-referencing, not new runtime behavior. Treat every path in Task 2's table as a claim to verify, not a given — that's the whole point of AC #2.
- **File location is `OrderFlow/docs/interview-topic-map.md`, not the outer BMad project's `docs/`.** The Architecture Spine's Structural Seed places it explicitly inside the `OrderFlow/` solution tree (`OrderFlow/docs/interview-topic-map.md # FR-13`), alongside the six (now seven, since `OrderFlow.Presentation.Tests` was added in Story 1.3) project folders — not `{project-root}/docs` at the repository root, which this BMad project instead uses for `project_knowledge` (currently empty, unrelated to this story).
- **"N/A — real app only" is correct for 8 of 11 rows, not a shortfall.** Epic 4's own scope (FR-12) only asked for three exhibit pairs (SRP/OCP/DIP, per the epics file's "three SOLID violations" locked decision) — Repository+UoW, Factory, Optimistic Concurrency, Presenter/MVP, `Result<T>`, and async-all-the-way were never asked to get exhibit pairs. Padding those cells with a fabricated exhibit reference would directly violate AC #2 ("no entry references a class/file that doesn't exist"); leaving them honestly N/A is what satisfies it.
- **SRP's "Real App" cell is the one soft entry, and that's inherited from Story 4.1, not invented here.** Story 4.1's own Dev Notes state SRP's exhibit "mirrors no single existing class, it's the canonical textbook violation" (unlike OCP mirroring `IPricingStrategy` and DIP mirroring the composition root). This story is consistent with that: it points to representative examples of the *discipline* (`CustomerService`, `OrderStatusService`) rather than fabricating a single "SRP class" that doesn't exist.
- **Maintenance note (AC #3) is a sentence in the doc, not a process.** FR-13 says the map "ships and maintains" — this story's job is to leave a clear instruction in the file itself (add a row here for new topics) so that intent survives without needing a separate maintenance workflow.

### Project Structure Notes

```text
OrderFlow/
  docs/
    interview-topic-map.md            # new: FR-13 deliverable
```

Every other file in the solution — `OrderFlow.Domain`/`OrderFlow.DAL`/`OrderFlow.BLL`/`OrderFlow.Presentation`/`OrderFlow.Presentation.Tests`/`OrderFlow.Tests`/`OrderFlow.Exhibits` — is untouched by this story; it only reads them to verify the map's references.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.4: Interview Topic Map] — acceptance criteria origin, including the exact topic list in AC #1
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Structural Seed] — confirms `OrderFlow/docs/interview-topic-map.md`'s exact location
- [Source: _bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md#Invariants & Rules] — AD-1, AD-2, AD-3, AD-5, AD-7, AD-9, AD-10, AD-11 — the architecture references cited in the table
- [Source: OrderFlow/OrderFlow.Presentation/Program.cs] — composition root / DI registrations verified for the DI & Composition Root and SOLID: DIP rows
- [Source: OrderFlow/OrderFlow.DAL/IUnitOfWork.cs, UnitOfWork.cs, IOrderRepository.cs, OrderRepository.cs] — verified for the Repository + Unit of Work row
- [Source: OrderFlow/OrderFlow.BLL/IPricingStrategy.cs, StandardPricingStrategy.cs] — verified for the Strategy Pattern / SOLID: OCP rows
- [Source: OrderFlow/OrderFlow.BLL/OrderProcessorFactory.cs, IOrderProcessor.cs, StandardOrderProcessor.cs, RushOrderProcessor.cs] — verified for the Factory Pattern row
- [Source: OrderFlow/OrderFlow.Domain/Order.cs, Inventory.cs; OrderFlow.DAL/UnitOfWork.cs, ConcurrencyConflictException.cs] — verified for the Optimistic Concurrency row
- [Source: OrderFlow/OrderFlow.Presentation/OrderDetailPresenter.cs, IOrderDetailView.cs, OrderDetailForm.cs] — verified for the Presenter/MVP row
- [Source: OrderFlow/OrderFlow.BLL/Result.cs] — verified for the `Result<T>` error handling row
- [Source: OrderFlow/OrderFlow.Presentation/OrderCreatePresenter.cs, OrderDetailPresenter.cs] — verified for the async-all-the-way row
- [Source: _bmad-output/implementation-artifacts/4-1-srp-exhibit-pair-before-after.md] — "SRP mirrors no single existing class" note this story's SRP row is consistent with
- [Source: _bmad-output/implementation-artifacts/4-2-ocp-exhibit-pair-before-after.md, 4-3-dip-exhibit-pair-before-after.md] — confirm the exact `OrderFlow.Exhibits/{Before,After}/{Ocp,Dip}` folder names this story's table cites

## Dev Agent Record

### Agent Model Used

claude-sonnet-5 (Claude Code)

### Debug Log References

- `test -e` pass over all 26 cited paths (Task 1, pre-write): all present.
- `test -e` pass over all 26 cited paths (Task 3, post-write, re-verified against the file as actually written): all present.
- `grep -c '^|' OrderFlow/docs/interview-topic-map.md`: 13 (header + separator + 11 data rows) — matches AC #1's 11-topic list exactly, no omissions or duplicates.
- `dotnet build OrderFlow.sln` (all 8 projects): Build succeeded, 0 Warning(s), 0 Error(s) — unchanged from before this story.
- `dotnet test OrderFlow.Tests/OrderFlow.Tests.csproj`: Passed! Failed: 0, Passed: 82, Skipped: 0, Total: 82 — unchanged from before this story.

### Completion Notes List

- Created `OrderFlow/docs/interview-topic-map.md` — the exact location the Architecture Spine's Structural Seed reserves for FR-13's deliverable.
- Table has 11 rows covering every topic named in AC #1's parenthetical list, with SOLID's three sub-principles (SRP/OCP/DIP) each getting their own row since each maps to different real-app/exhibit references.
- 3 rows (DI & Composition Root / SOLID: OCP+DIP overlap with Strategy Pattern's row / SOLID: SRP+OCP+DIP) link to their Story 4.1-4.3 exhibit pair; the remaining 8 rows are honestly marked "N/A — real app only" since Epic 4 built only three exhibit pairs — no fabricated exhibit references were added to fill cells.
- Every real-app path cited was verified to exist on disk twice: once before writing (Task 1) and once after (Task 3), directly satisfying AC #2's "no entry references a class/file that doesn't exist."
- Added an intro paragraph stating the file's purpose and a one-line maintenance note ("add a row here, do not create a second map elsewhere") satisfying AC #3's "remains a single maintained file" requirement.
- No `.cs` file touched anywhere in the solution — confirmed via File List below; `dotnet build`/`dotnet test` re-run as a cheap sanity check and confirmed unchanged (0 warnings, 82/82 tests), even though a Markdown-only change couldn't plausibly affect either.

### File List

- `OrderFlow/docs/interview-topic-map.md` (new)

## Change Log

- 2026-08-11: Implemented Story 4.4 — created `OrderFlow/docs/interview-topic-map.md` mapping all 11 named interview topics (DI & Composition Root, Repository + Unit of Work, Strategy Pattern, Factory Pattern/keyed DI, SOLID: SRP/OCP/DIP, Optimistic Concurrency, Presenter/MVP, `Result<T>` error handling, async-all-the-way) to real-app classes/files and, where Epic 4 built one, the corresponding Before/After exhibit pair. Every cited path verified to exist twice (pre- and post-write). No code changed; `dotnet build`/`dotnet test` confirmed unchanged (0 warnings, 82/82). This completes Epic 4's FR-12/FR-13 scope.
- 2026-08-11: Code review — independently re-verified every cited path and spot-checked three substantive claims (RowVersion concurrency tokens, DbUpdateConcurrencyException handling, async-all-the-way presenters). Clean review, zero findings.
