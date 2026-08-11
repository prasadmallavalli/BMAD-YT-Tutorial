# OrderFlow Desktop — Senior Team Lead Interview Q&A Guide

Every question here is answerable by opening a real file in this repo, not by reciting a
definition. Where a topic has a standalone Before/After exhibit (`OrderFlow.Exhibits/`), it's
linked — run it and show the diff live rather than describing it. For the raw topic → file
lookup table (no Q&A framing), see [`interview-topic-map.md`](interview-topic-map.md).

**How to use this:** each section opens with the "textbook" question, then the follow-ups a
strong interviewer actually asks — the ones that separate "I read about this" from "I built
this." Answers cite real classes/files and, where one exists, a real bug or decision from this
project's own history — the kind of concrete story interviewers are fishing for when they ask
"tell me about a time..."

---

## 1. System Design & Layered Architecture

**Q: Walk me through your architecture.**
Four projects, strict one-way dependency: `Presentation → BLL → DAL → Domain`, with `Domain`
having zero outward references. Each layer is its own class library; only `Presentation` is
executable — that's the one project with `OutputType=WinExe`. It's deliberately classic layered
architecture, not DDD or CQRS — see `ARCHITECTURE-SPINE.md`'s own framing: kept conventional "so
every layer boundary is a clean, defensible interview talking point." (AD-1, Design Paradigm)

**Q: How do you actually enforce that boundary — what stops someone from calling DAL straight
from a Form six months from now?**
Honestly: project references and code review, not a compiler gate. `OrderFlow.Presentation.csproj`
only references `BLL`/`DAL`/`Domain` at the project level, and `DAL`/`Domain` types other than
enums never appear in a Form's or Presenter's signature — but nothing stops a bad `using`
statement from compiling if the reference exists. This is a real, disclosed gap: `deferred-work.md`
lists "no architecture-fitness test (e.g. NetArchTest) enforcing AD-1's layer-direction rule" as
still open. Good interview answer, not a good production answer — say both parts.

**Q: Your composition root (`Program.cs`) references `OrderFlow.DAL` directly. Doesn't that
violate your own layering rule?**
This is the best "I know my own architecture's exceptions" question in the whole project.
Yes — and it's a documented, scoped exception (AD-1), not an oversight. The composition root is
the *one* place that performs DI registration for every layer, so it alone must see every
layer's types (it needs `AppDbContext` to call `AddPooledDbContextFactory<AppDbContext>(...)`).
The exception is scoped to `Program.cs` specifically — no Form or Presenter gets the same pass.
**The real story:** this wasn't planned upfront. Story 1.1's code review flagged it as a literal
rule violation, and the team amended AD-1/AD-9 *during* review to formally sanction it rather
than either quietly violating the rule or hacking around a legitimate need. That's the answer to
"tell me about a time your architecture doc turned out to be wrong" — it happened on the very
first story, and the fix was "update the doc," not "bend the code."

**Q: What's the single biggest maintainability risk in this design?**
`OrderFlow.Presentation.csproj` referencing `DAL` at all, even scoped to one file — a future
developer copy-pasting from `Program.cs` into a Form has a compiler that won't stop them. The
mitigation today is discipline + code review, not tooling. If I were staffing a team long-term
on this, the architecture-fitness test from the deferred list is where I'd spend the first
sprint of hardening.

---

## 2. SOLID Principles

Three of the five have dedicated, runnable Before/After exhibits (`OrderFlow.Exhibits/`) — walk
the interviewer through the actual diff, don't describe it from memory.

**Q: Show me SRP in this codebase.**
`OrderFlow.Exhibits/{Before,After}/Srp` — but be honest about this one before they catch you
overselling it: SRP is the *softest* entry in the interview-topic-map by design. There's no
single dedicated real-app class demonstrating it the way OCP/DIP cleanly mirror
`IPricingStrategy`/the composition root — it's a general layering discipline visible in e.g.
`CustomerService`/`OrderStatusService` each owning exactly one responsibility, not one killer
example. Saying "SRP is the principle I can't point to a single line for, and here's why" is a
stronger answer than pretending otherwise.

**Q: Show me OCP.**
`OrderFlow.Exhibits/{Before,After}/Ocp`, and its real-app mirror: `IPricingStrategy`/
`StandardPricingStrategy` (AD-11). Adding a new pricing strategy means adding a new class and
changing one DI registration line — zero changes to any caller. AD-11 also documents the
opposite decision made deliberately at the same layer: `OrderProcessorFactory` (AD-7) uses
*keyed* DI because processor selection varies per-Order (`OrderType`), while pricing strategy
selection is a single swap-the-registration decision, not a per-Order one. Being able to explain
*why* two adjacent abstractions in the same codebase use different DI shapes is a stronger signal
than reciting either pattern alone.

**Q: Show me DIP.**
`OrderFlow.Exhibits/{Before,After}/Dip` (Story 4.3 — swaps `SqlCustomerRepository` for
`FakeCustomerRepository` with zero change to the consuming `CustomerLookupService`), and the
real-app version: every constructor-injected interface in `Program.cs`'s `ConfigureServices`.
High-level modules (`BLL` services) depend on abstractions (`IUnitOfWork`, `I*Repository`)
defined in `DAL`, not concretions — and BLL never references EF Core types directly (AD-9).

**Q: What about LSP and ISP — no exhibits for those?**
Correct, and say so directly rather than stretching an answer. ISP is implicitly satisfied
throughout — interfaces are narrow and role-specific (`ICustomerRepository`,
`IInventoryService.HasSufficientStockAsync`) rather than one fat repository interface — but there's
no dedicated violation-then-fix pair for it. LSP has no exhibit at all since there's no
inheritance hierarchy in this codebase deep enough to violate it meaningfully. Naming this gap
unprompted is a better signal of real understanding than forcing an example that isn't there.

---

## 3. Design Patterns

**Q: Repository + Unit of Work — why both, and what's the actual boundary?**
`OrderFlow.DAL/IUnitOfWork.cs` + `UnitOfWork.cs`, `I*Repository`/`*Repository` per aggregate
(AD-9). `IUnitOfWork` owns the single `AppDbContext` for one business operation and exposes
repositories (`.Customers`, `.Orders`, `.Inventory`) backed by that same instance —
**repositories are never independently DI-registered**; `UnitOfWork` constructs them internally.
`OrderFlow.BLL` depends only on `IUnitOfWork`, never on EF Core types directly. The one sanctioned
crack in that wall: `OrderFlow.BLL` may depend on plain DAL-defined exception *types* like
`ConcurrencyConflictException` (see `OrderFlow.DAL/ConcurrencyConflictException.cs` — no EF Core
type in its public shape) — added as a scoped AD-9 amendment in Story 1.4 specifically to let
AD-10's concurrency translation happen without BLL ever seeing `DbUpdateConcurrencyException`
itself.

**Q: Why does `CustomerRepository.GetByIdAsync` *not* call `.AsNoTracking()`? Isn't that a
performance smell?**
Deliberate, not an oversight — and a great "explain a non-obvious tradeoff" question. Because
`UnitOfWork` shares one `DbContext` per business operation, an entity returned by
`GetByIdAsync` stays tracked. `CustomerService.UpdateAsync` mutates its properties directly, and
EF's change tracker marks only the changed properties `Modified` automatically — no blanket
`Update()`/reattach of a detached graph, which is exactly what AD-6 forbids (it would mark the
*entire* entity graph `Modified`, corrupting `CreatedAt` stamping). `.AsNoTracking()` here would
silently make `SaveChangesAsync()` persist zero changes.

**Q: Strategy vs. Factory — when do you reach for which?**
Both live in this codebase, deliberately shaped differently (see the OCP answer above):
`IPricingStrategy` is Strategy — one algorithm interface, single swappable implementation, no
runtime selection. `IOrderProcessor` via `OrderProcessorFactory` (AD-7) is Factory + keyed DI —
selection *varies per call* based on `OrderType`, so it needs a resolution mechanism, not just a
swap. `OrderProcessorFactory` itself is registered **Scoped, never Singleton** despite the name —
a Singleton factory capturing the root `IServiceProvider` would either throw on scope validation
or leak a captive Scoped dependency across operations. It's injected with the *ambient* scoped
`IServiceProvider` for the current business operation instead.

**Q: MVP/Presenter pattern — how does it keep Forms thin?**
AD-3: every Form implements a screen-specific `IXxxView`; a `XxxPresenter` is
constructor-injected with that view and an `IServiceScopeFactory` (never long-lived BLL
instances). For each user action, the Presenter opens one `IServiceScope`, resolves what it
needs, awaits it, and disposes the scope — see `OrderDetailPresenter.cs`/`IOrderDetailView.cs`.
Two narrow, explicitly-documented exceptions exist and are worth naming unprompted: (1) a Form
that *launches other Forms* (`MainForm` → `CustomerListForm`) constructor-injects the root
`IServiceProvider` solely to call `GetRequiredService<TForm>()` — a Service Locator pattern,
disclosed as a deliberate tradeoff, not a violation of DI principles by accident; (2) `MainForm`
also directly injects `INotifier` (a Singleton) to passively render its already-published log —
narrower than the first exception, since it never calls a BLL method that performs validation or
workflow logic.

**Q: Where's Observer in this codebase?**
`INotifier`/`InAppNotifier` (AD-4) — a plain C# event (`Notified`), not a full pub/sub framework.
`OrderStatusService.TransitionTo` is the *sole* caller of `INotifier.Notify(...)`, and only after
the Unit of Work confirms the status change persisted — notifications never fire from
inconsistent points. `MainForm` subscribes and marshals back to the UI thread via
`InvokeRequired`/`BeginInvoke` (see `MainForm.Notifier_Notified`), since `Notify` can in principle
be called from any thread.

---

## 4. Dependency Injection

**Q: What are your DI lifetimes, and why?**
AD-5: a **business operation** = exactly one Presenter-method invocation from one user action —
never a whole Form session. Repositories, `IUnitOfWork`, and BLL services are Scoped, resolved
from an `IServiceScope` the Presenter creates per operation and disposes at its end (WinForms has
no ASP.NET-style per-request scope, so this is manual). Only `INotifier` and the not-yet-built
`IAppSettings` are Singleton — reserved specifically for cross-cutting config/state that must
outlive any one operation. Nothing scoped-per-operation is ever resolved from a captured root
provider.

**Q: What does `ValidateOnBuild`/`ValidateScopes` actually buy you, and where did it come from?**
`Program.cs`: `services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true,
ValidateScopes = true })`. It fails fast at startup if a registration is misconfigured (missing
constructor dependency, wrong lifetime nesting) instead of silently passing and blowing up on
first use. **Real history:** this wasn't in the original scaffold — Story 1.1's code review
flagged its absence as a real gap ("a misconfigured registration would silently pass instead of
failing fast"), and it was added as a patch. Every story since has relied on it as a fast
feedback signal while wiring new services.

**Q: Show me keyed DI in this codebase.**
`OrderProcessorFactory` (AD-7): `services.AddKeyedScoped<IOrderProcessor,
StandardOrderProcessor>(OrderType.Standard)` / `...RushOrderProcessor>(OrderType.Rush)`. The
factory wraps `IServiceProvider.GetRequiredKeyedService` — no caller resolves `IOrderProcessor`
directly. Contrast this immediately with `IPricingStrategy`'s single plain registration (AD-11)
when asked — see Section 3's Strategy-vs-Factory answer.

---

## 5. Entity Framework Core & Data Access

**Q: How do you manage `DbContext` lifetime in a long-running desktop process?**
AD-2: `AppDbContext` resolves only through a singleton `IDbContextFactory<AppDbContext>`.
**Only `IUnitOfWork` calls the factory**, once per business operation, at construction. Every
repository used within one operation shares exactly that one `DbContext` instance via a
constructor parameter `UnitOfWork` supplies — no component holds a `DbContext` beyond one
operation's scope, avoiding stale tracked entities and change-tracker growth across the app's
whole lifetime.

**Q: DI constructors can't `await` — so how does a Scoped `UnitOfWork` get its `DbContext`
without blocking?**
Real gotcha, real answer: `UnitOfWork`'s constructor calls the **synchronous**
`IDbContextFactory<AppDbContext>.CreateDbContext()` overload — a genuine sync EF Core API, not a
`.Result`/`.Wait()` antipattern blocking on a Task. AD-2 says "only `IUnitOfWork` calls
`CreateDbContextAsync()`" in spirit, but Story 1.2's Dev Notes explicitly correct this: DI's
constructor injection has no async resolution path, so the sync overload is the correct
implementation of the same intent, not a violation of it.

**Q: How do EF migrations work when your app has no ASP.NET host for the CLI to discover?**
`AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` (`OrderFlow.DAL`) — a
design-time-only tooling class the `dotnet ef` CLI constructs directly, since `AppDbContext` has
no parameterless constructor and this WinForms app has no discoverable host. It's never
referenced by the runtime DI graph, so it doesn't reopen the "DAL types stay internal" debate —
it's DAL-internal, invoked only by the CLI.

**Q: How is auditing (`CreatedAt`/`UpdatedAt`) implemented — and how do you stop `CreatedAt` from
getting silently overwritten on every update?**
AD-6: every entity implements `IAuditable`. `AppDbContext` overrides `SaveChanges`/
`SaveChangesAsync` to stamp `UpdatedAt` on every save, but stamps `CreatedAt` **only** when
`EntityState == EntityState.Added` — it refuses to touch `CreatedAt` on any other state,
regardless of what an update call sends. No `IsDeleted`/soft-delete — cancellation is modeled via
`OrderStatus` instead, deliberately avoiding global-query-filter complexity for a domain that
already has a state machine.

**Q: Optimistic concurrency — walk me through a real race condition your review caught.**
This is the single best "tell me about a subtle bug" story in the whole project — see Section 6.
The short version: every entity carries a `RowVersion` token (AD-10), and
`UnitOfWork.SaveChangesAsync()` is the sole place that catches the real EF Core
`DbUpdateConcurrencyException` and translates it to `ConcurrencyConflictException` — a plain
wrapper type with the friendly message `"The record was modified by another user. Please reload
and try again."` (see `ConcurrencyConflictException.DefaultMessage`, centralized after Story 1.4's
review found the message duplicated and drifting between `UnitOfWork` and `ProductService`). But
`RowVersion` only catches *stale-read-then-write* races — it does **not** catch a
*fresh-read-then-oversell* sequence, which is exactly the bug Story 2.5's review found. Don't
stop at "we have RowVersion" — that's the incomplete answer. The complete answer explains what it
doesn't cover.

---

## 6. Concurrency, Correctness & Real Bugs You Can Talk About

Interviewers ask "tell me about a bug you found in code review" constantly. These are real,
with the actual mechanism and actual fix — not hypotheticals.

**Q: Tell me about a genuine correctness bug your process caught before it shipped.**
Two, from Story 2.5's code review (Order Creation & Confirmation UI):

1. **Duplicate line items bypassed the stock check.** If a request had two separate `OrderItem`
   lines for the same `ProductId`, each was stock-checked *independently* against the same
   on-hand quantity — so an order could persist despite *combined* demand exceeding stock,
   violating the "no partial decrement" guarantee. Fix: aggregate `request.Items` by `ProductId`
   before the stock check, in both `StandardOrderProcessor` and `RushOrderProcessor`.
2. **The decrement step blindly subtracted stock with no re-validation.** `inventory!.StockQuantity
   -= item.Quantity` ran with no null-check and no re-check against current stock — so two
   near-simultaneous confirmations against the same product could drive stock negative *without*
   tripping `ConcurrencyConflictException`, because `RowVersion` only catches stale-read-then-write
   races, not this fresh-read-then-oversell sequence. Fix: guard with
   `if (inventory is null || inventory.StockQuantity < quantity) return
   Result<OrderDto>.Failure(...)` before subtracting.

Both got regression tests. **The meta-lesson** (from this project's own Epic 2 retrospective):
these were the two most serious bugs found across the whole epic, and they shipped in the story
with the *fewest* code-review patches (2, vs. 3-7 across the epic's other four stories) — patch *count* is a weak
severity proxy. Don't let a quiet review lull you; ask what was actually tested, not how many
comments came back.

**Q: Tell me about a bug caused by getting operation *order* wrong, not logic wrong.**
Story 1.2: `CustomerService.UpdateAsync` validated the incoming DTO *before* fetching the
existing entity — so calling `UpdateAsync` with both an invalid DTO and a non-existent id
returned `"Name is required"` instead of `"Customer not found"`. Caught by an Acceptance Auditor
in code review, not by the story's own tests (there was no not-found-path test yet — see next
question). Fixed by reordering: fetch first, then validate. Checked in Epic 2's retro whether
this pattern recurred in `OrderStatusService.TransitionTo` — it didn't; the lesson transferred.

**Q: What's a bug your tests *should* have caught but didn't?**
The Story 1.2 bug above, directly — code review flagged "no test coverage for `GetAsync`,
`GetAllAsync`, or `UpdateAsync`'s not-found path... this exact gap is why the reorder bug shipped
undetected." Good self-critique answer: test coverage gaps aren't abstract risk, they're the
specific reason a specific bug reached review instead of getting caught earlier and cheaper.

---

## 7. Error Handling Strategy

**Q: Exceptions vs. return values — what's your convention and why?**
`OrderFlow.BLL/Result.cs` — a minimal `Result<T>` (`IsSuccess`, `Value`, `Error`,
`Success(T)`/`Failure(string)` factories). Convention (Architecture Spine's Consistency
Conventions table): BLL methods that can fail *validation* return `Result<T>` — never throw.
Exceptions are reserved for genuine infrastructure failures (DB unavailable, concurrency
conflicts) surfaced through one global handler at the composition root
(`Application.ThreadException`/`AppDomain.UnhandledException`), not per-Presenter try/catch.
"Insufficient inventory" is an expected outcome, not an exceptional one — it's a `Result<T>`
failure, not a thrown exception.

**Q: Where did you deliberately choose *not* to add exception handling, and why?**
Story 1.2's Dev Notes name this explicitly as a dismissed reviewer suggestion: wrapping
`SaveChangesAsync()` in try/catch to convert infrastructure exceptions into `Result<T>.Failure`
was proposed and rejected — the architecture's own convention reserves exceptions for
infrastructure failures surfaced through the global handler, not per-service catches. Knowing
when *not* to add error handling, and being able to cite the convention that says so, is a
stronger signal than reflexively wrapping everything in try/catch.

---

## 8. Testing Strategy

**Q: What do you mock, and what do you deliberately not mock?**
`OrderFlow.Tests` mocks `IUnitOfWork`/`I*Repository` (via Moq) to test BLL services without a
live database — see any `*ServiceTests.cs`. It does **not** have DAL-level tests against a real
provider (in-memory/SQLite) for `CustomerRepository`/`UnitOfWork`/the audit-stamp override — an
explicitly disclosed, still-open gap in `deferred-work.md`, not an oversight. Good answer:
"mocked-`IUnitOfWork` BLL tests satisfy the story's AC literally; real DAL-provider testing is a
deliberately deferred future investment, and here's exactly what it would need to cover."

**Q: Your Presenter tests can't run on your own dev machine. How do you know they're correct?**
Real, disclosed constraint: `OrderFlow.Presentation`/`OrderFlow.Presentation.Tests` target
`net10.0-windows` — they compile cross-platform (`EnableWindowsTargeting=true`) but need
`Microsoft.WindowsDesktop.App`, which doesn't exist on macOS/Linux at all. The actual fix wasn't
"work around it" — it was standing up GitHub Actions on `windows-latest`
(`.github/workflows/tests.yml`) so the suite genuinely *executes*, plus a dedicated smoke-test
step that launches the built `.exe` and asserts the window title to catch a startup-time
composition-root exception the test suite itself can't reach. Both are real CI gates now, not
aspirational — check the green badge in the repo's README.

**Q: How did test coverage discipline change across the project?**
Tracked and improving: `OrderFlow.Tests` went 1 → 28 across Epic 1, 28 → 58 across Epic 2. Review
patch counts per story in Epic 1 fell 12 → 10 → 8 → 4 → 1 — by the epic's last story, Story 1.5's
own Change Log records that "every Story 1.3 code-review fix... was baked in from the start
rather than left for review to catch." That's a concrete, numbers-backed answer to "how do you
drive quality up over a project's lifetime," not a platitude.

---

## 9. WinForms-Specific & Async Patterns

**Q: How do you keep a WinForms UI responsive during I/O?**
Async-all-the-way from the Presenter down: `OrderCreatePresenter`/`OrderDetailPresenter` expose
`async Task` methods; the top-level UI event handler may be `async void` (WinForms convention),
but nothing below it blocks with `.Result`/`.Wait()`. Marshal-to-UI-thread pattern shows up
explicitly in `MainForm.Notifier_Notified`: `if (InvokeRequired) { BeginInvoke(...); return; }` —
needed because `INotifier.Notify` can in principle fire from any thread.

**Q: What's a concrete disclosed gap in your async/disposal handling?**
If a Form is closed while a `TransitionToAsync`/`LoadAsync` await is still pending, the `finally`
block's control access could throw `ObjectDisposedException` — flagged in Story 3.3's code review
as an identical pre-existing pattern across every async Form in this codebase (no disposal guard
anywhere). Naming a *systemic*, not story-specific, gap unprompted is a stronger signal than
claiming the codebase has none.

---

## 10. Process, Leadership & Behavioral

These map straight onto "tell me about a time..." interview questions, with a real, specific
project incident as the answer instead of a generic anecdote.

**Q: Tell me about a time you disagreed with a decision that was already locked in.**
The brief explicitly locked .NET 8. The Architecture Spine overrode it to .NET 10 instead — with
the reasoning stated directly in the doc: .NET 8 reaches end-of-support ~3 months from the
spine's authoring date, .NET 10 is the current LTS through 2028, and it's a drop-in target for
the same WinForms/EF Core/DI stack. Surfaced to and confirmed by the stakeholder before binding,
not silently substituted. Good structure for this answer: state the constraint, state why it was
wrong for the timeline, state that you got explicit sign-off before overriding it — not that you
just did what you thought was right.

**Q: Tell me about a technical debt item you chose *not* to fix, and how you'd justify that to a
skeptical stakeholder.**
Story 1.3/1.5's UI debt (no busy/loading indicator, no empty-state messaging, `editButton.Enabled`
recomputation logic triplicated across three places) — confirmed *again* in this project's Epic 1
retrospective and explicitly left deferred on a direct question, because it's UX polish on a
non-production demo app, not a correctness gap. The answer isn't "we didn't have time" — it's "we
evaluated it against the app's actual purpose and decided the cost wasn't justified for this
context," with the decision documented in `deferred-work.md` so it's a conscious tradeoff a future
maintainer can revisit, not a silently-forgotten gap.

**Q: How do you run a retrospective when the "team" is effectively one person driving an AI
pairing session?**
Honestly — this project's retros (`epic-1-retro-2026-08-11.md`, `epic-2-retro-2026-08-11.md`)
were run retroactively, grounded entirely in real numbers pulled from story files (patch counts,
bug counts, deferred-item counts) rather than vague recollection, with genuine open questions put
to the human stakeholder rather than assumed answers. The discipline that transfers to a real team
retro: don't let "we shipped it" substitute for "here's what the data says we learned," and don't
let a retrospective closeout skip open questions just because the epic is already done.

**Q: How do you decide what's a blocking issue vs. deferred work?**
Every code review in this project's history sorts findings into Patch (fixed now) vs. Defer
(documented, not blocking) vs. Dismiss (false positive/out of scope) — see any story's "Review
Findings" section. The criterion consistently applied: does it violate a stated Acceptance
Criterion or introduce a real behavioral defect (Patch), or is it a disclosed scope/priority
tradeoff with no AC violation (Defer)? Being able to state that criterion crisply — not "it felt
important" — is what separates a defensible review process from an arbitrary one.

---

## Quick-Reference: Pattern → File Map

| Pattern/Concept | File(s) | Architecture Decision |
| --- | --- | --- |
| Composition root | `OrderFlow.Presentation/Program.cs` | AD-1, AD-2, AD-5 |
| Repository + Unit of Work | `OrderFlow.DAL/IUnitOfWork.cs`, `UnitOfWork.cs` | AD-9 |
| Strategy | `OrderFlow.BLL/IPricingStrategy.cs`, `StandardPricingStrategy.cs` | AD-11 |
| Factory + keyed DI | `OrderFlow.BLL/OrderProcessorFactory.cs`, `IOrderProcessor.cs` | AD-7 |
| MVP/Presenter | `OrderFlow.Presentation/*Presenter.cs`, `I*View.cs` | AD-3 |
| Observer | `OrderFlow.BLL/INotifier.cs`, `InAppNotifier.cs` | AD-4 |
| Optimistic concurrency | `OrderFlow.Domain/*.cs` (`RowVersion`), `ConcurrencyConflictException.cs` | AD-10 |
| Result-based error handling | `OrderFlow.BLL/Result.cs` | Consistency Conventions |
| Auditing | `OrderFlow.Domain/IAuditable.cs`, `AppDbContext.cs` (`SaveChanges` override) | AD-6 |
| SOLID exhibits | `OrderFlow.Exhibits/{Before,After}/{Srp,Ocp,Dip}` | AD-8 |

For the full FR-13 topic map (broader, table-only), see
[`interview-topic-map.md`](interview-topic-map.md). For the architecture's full reasoning behind
every decision above, read `_bmad-output/planning-artifacts/architecture/architecture-BMAD YT
TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md` directly — every AD there is written as prose, not just
rules, and reads like a design-review transcript.
