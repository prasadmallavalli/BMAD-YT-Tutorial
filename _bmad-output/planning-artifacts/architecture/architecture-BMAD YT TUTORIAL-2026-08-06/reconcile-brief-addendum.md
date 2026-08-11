---
title: "Reconciliation: Brief + Addendum -> Architecture Spine (OrderFlow Desktop)"
status: draft
created: 2026-08-06
---

# Reconciliation — Brief + Addendum → ARCHITECTURE-SPINE.md (OrderFlow Desktop)

Inputs reviewed in full:
- `briefs/brief-BMAD YT TUTORIAL-2026-08-06/brief.md`
- `briefs/brief-BMAD YT TUTORIAL-2026-08-06/addendum.md`
- `architecture/architecture-BMAD YT TUTORIAL-2026-08-06/ARCHITECTURE-SPINE.md`

## Item-by-item resolution check

| Addendum/brief item | Resolved in spine? | Where / how |
| --- | --- | --- |
| **6a** — EF Core relationship shapes (Order–OrderItem, Order–Customer, OrderItem–Product) | **Yes**, at architecture altitude | Structural Seed ERD (lines 118–129 of spine) gives the exact cardinalities the addendum asked for: `CUSTOMER \|\|--o{ ORDER`, `ORDER \|\|--\|{ ORDERITEM`, `PRODUCT \|\|--o{ ORDERITEM`, `PRODUCT \|\|--\|\| INVENTORY`. Exact FK/column-level detail is explicitly named and deferred: "Exact EF Core relationship cardinalities/FKs beyond the ERD sketch — Epics/Dev finalize within the ERD's shape (addendum item 6a)" — a labeled, intentional deferral, not a drop. |
| **6a** — soft-delete / auditing (`IAuditable`) | **Yes** | AD-6 "Auditing via IAuditable, no soft-delete `[ADOPTED]`" — adopts `IAuditable` (`CreatedAt`/`UpdatedAt`, stamped in `SaveChanges` override) and explicitly rejects `IsDeleted`/global query filters, with rationale (cancellation already modeled via `OrderStatus`). This is a clean, reasoned resolution of the open question, not just an assumption. |
| **6b** — before/after code: separate project/folder vs. commented-out/git-history-only | **Yes** | AD-8 "Before/After Exhibits isolated from the runtime DI graph `[ADOPTED]`" — creates `OrderFlow.Exhibits` with `Before/` and `After/` folders, never referenced by the runtime layers. Matches (and slightly exceeds — full project, not just a folder inside an existing project) the addendum's own recommendation. |
| **6c** — LocalDB vs. SQL Server Developer Edition | **Yes** | Stack table: "SQL Server LocalDB — bundled with Visual Studio." Direct, unambiguous answer to the addendum's "architecture should confirm." |
| **6d** — MVP vs. plain code-behind-with-discipline | **Yes** | AD-3 "Presentation: constructor-injected Presenter + per-screen IView `[ADOPTED]`" — names MVP explicitly (`IXxxView` + `XxxPresenter`, composition-root wiring), exactly as the addendum asked ("architecture should make the call and name it explicitly"). |
| Domain-too-thin contingency (Chain of Responsibility / Adapter fallback) | **Acknowledged, appropriately deferred** (not "resolved" — correctly so, since it's a contingency, not a decision to make now) | Deferred section: "Domain-too-thin contingency — Chain of Responsibility / Adapter fallback (PRD §8 Q6) — revisit only if triggered; would add a new AD, not retrofit existing ones." This is the right treatment for a conditional fallback — it isn't silently dropped, it's carried forward with a trigger condition. |
| "Don't force Singleton" note | **Yes** | AD-5 "DI lifetimes: scoped-per-operation, Singleton reserved for config `[ADOPTED]`" — `IAppSettings` is the *only* Singleton-registered service, matching the addendum's own suggested legitimate use ("a configuration/settings accessor") rather than forcing Singleton onto something like a repository or DbContext. |
| **.NET 10 override** (brief + addendum both say **.NET 8**, chosen "deliberately... to show currency with modern .NET") | **Not surfaced as an intentional override** | Spine Stack table simply states "`.NET` \| `10 (LTS, supported through 2028-11)`" with a generic seed comment ("verified web-current 2026-08-06; code owns exact pins once it exists"). Nowhere in the spine — not in the Stack section, not in Deferred, not as a footnote — does it acknowledge that the brief and addendum's explicitly "locked during discovery" decision was .NET 8, or explain why 10 was substituted. A reader of the spine alone would not know this is a deliberate upgrade rather than a drift/error. See Gaps #1. |

## Gaps

Ranked by importance — genuine gaps only, not nitpicks on already-resolved items.

1. **.NET 10 vs. .NET 8 is an unexplained contradiction, not a surfaced override.** The addendum labels .NET 8 a decision "locked during discovery" with explicit rationale (showing currency with modern .NET, since interviewers probe for Framework-to-.NET migration awareness). The spine silently swaps in .NET 10 with only a generic "verified web-current" seed note — no line says "brief/addendum specified .NET 8; upgraded to .NET 10 because the addendum's own rationale (demonstrate currency with modern .NET) argues even more strongly for the newer LTS" (or whatever the actual reasoning is). This should be a one- or two-line explicit override note (e.g., in Stack or as a new short "Deviations from brief" callout) so it reads as intentional, not an oversight carried in from a stale brief.

2. **Basic optimistic concurrency (in-scope per brief) has no architectural home.** The brief's Out-of-Scope list reads "multi-user concurrency handling *beyond* basic optimistic concurrency," which by its own phrasing puts basic optimistic concurrency inside v1 scope. No AD, Domain note, or Consistency Convention addresses a concurrency token (e.g., `RowVersion`/`[Timestamp]`) on entities, or how the Repository/UoW layer should handle `DbUpdateConcurrencyException`. This is architecture-shaping (it affects `IAuditable`/entity base shape and AD-9's persistence boundary) and was dropped rather than explicitly deferred like the domain-too-thin contingency was.

3. **Addendum's UI-effort-allocation guidance isn't carried into an architectural constraint.** The addendum explicitly says architecture "should size form list/complexity accordingly, but should not let UI polish crowd out time on the BLL/DAL/pattern work that is the actual point of the project." AD-3 fixes the *structure* (MVP) but nothing in the spine (e.g., a scope note under Deferred, or a line in Consistency Conventions) carries forward the "don't over-invest in UI polish" guardrail. Minor — arguably more a scope/PM concern than a structural invariant — but it was explicit guidance aimed at the architecture phase and has no trace in the output.
