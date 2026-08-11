---
review: rubric-walker
target: ARCHITECTURE-SPINE.md (OrderFlow Desktop)
reviewer: rubric-walker (architecture spine)
date: 2026-08-06
---

# Rubric-Walker Review — OrderFlow Desktop Architecture Spine

## Overall Verdict: **ADEQUATE**

The spine is well-structured and mostly does its job: 9 ADs each have a stated Rule and Prevents clause, the capability map covers all 15 FRs, the Stack table matches the memlog's web-verified findings exactly, the Deferred section correctly identifies the operational/environmental envelope and closes it against the PRD's own Non-Goals, and the greenfield/no-parent-spine checks are cleanly N/A. It falls short of "strong" because of one internal contradiction in the flagship layering AD (AD-1's rule text vs. its own dependency diagram) and one real dimension — optimistic concurrency, which the PRD explicitly calls for — left completely unaddressed anywhere in the document (not decided, not deferred, not an open question).

## Finding Counts by Severity

- Critical: 0
- High: 1
- Medium: 2
- Low: 2

**Total: 5 findings**

---

## Findings

### [HIGH] AD-1's Rule text contradicts its own dependency diagram

**Section:** AD-1 — Strict layered dependency direction; dependency `mermaid graph TD` block.

AD-1's Rule states: *"`OrderFlow.Presentation` references `OrderFlow.BLL` only."* But the dependency diagram immediately below it draws an explicit edge `Presentation --> Domain`, in addition to `Presentation --> BLL`. These two statements of the same AD disagree about whether Presentation may reference Domain directly.

This matters because AD-1 binds "all" and is the primary enforcement mechanism the spine relies on to prevent layer-skipping — exactly the kind of divergence point this altitude exists to fix. Left as-is, one developer/story could follow the literal Rule text (Forms may never see `Order`/`OrderStatus`/`OrderType` types directly, requiring BLL-projected DTOs for all Presentation-layer data), while another follows the diagram (WinForms grids bind directly to Domain entities/enums, which is the far more natural and likely-intended pattern for this stack — e.g., a `DataGridView` bound to `List<Order>`, or a combo box populated from the `OrderStatus` enum). That is a real, incompatible divergence the spine was supposed to close, not leave open.

**Recommendation:** Fix AD-1's Rule line to read `OrderFlow.Presentation references OrderFlow.BLL and OrderFlow.Domain only` (matching the diagram), or remove the `Presentation --> Domain` edge from the diagram if direct Domain access from Presentation is genuinely disallowed and all UI-bound types must be BLL-projected. Given WinForms conventions and the rest of the spine's pragmatism (Result<T>, no DTO layer mentioned elsewhere), the diagram is almost certainly the intended design and the prose is the bug.

---

### [MEDIUM] Optimistic concurrency — a dimension the PRD explicitly assigns to this altitude — is silent

**Section:** whole document; relevant PRD anchor: §5 Non-Goals — *"no concurrent-user handling beyond basic optimistic concurrency."*

The PRD explicitly carries "basic optimistic concurrency" as an in-scope capability (not excluded, just bounded), which makes it a dimension this architecture altitude owns. The spine has no AD, Consistency Convention, or Deferred entry addressing how optimistic concurrency is implemented — e.g., a `RowVersion`/`[Timestamp]` concurrency token convention on entities, `DbUpdateConcurrencyException` handling policy, or which entities need it (Inventory decrement under FR-5 is the obvious candidate, since two near-simultaneous confirmations against the same Product's stock is exactly the scenario optimistic concurrency is meant to catch). Without a fixed convention, DAL implementations across stories could diverge — some entities getting concurrency tokens, others not, with inconsistent exception-handling at the BLL boundary.

This is precisely the checklist's "whole dimension left silent" case: it isn't deferred with a rationale (like the deployment/environment dimension correctly is), it simply isn't mentioned.

**Recommendation:** Either add a short AD/convention (e.g., "all entities carry a `RowVersion` concurrency token; DAL translates `DbUpdateConcurrencyException` into a `Result<T>` failure") or add it explicitly to Deferred with a rationale for why it's safe to leave open (e.g., "single-operator app, EF Core default optimistic concurrency behavior is acceptable, Epics decides which entities need `[Timestamp]`").

---

### [MEDIUM] FR-7's Pricing/Discount Strategy selection/wiring mechanism has no governing AD, and Deferred item 1's justification is inaccurate

**Section:** Deferred — "Concrete discount rule(s)"; Capability Map row for FR-7.

FR-7 requires a *"configurable Pricing/Discount Strategy"* where swapping the active strategy changes computed totals "without any code change to Order-entry or DAL code." The Order Processor Factory (an analogous swappable-behavior requirement, FR-15) gets its own AD (AD-7) that fixes exactly how `IOrderProcessor` implementations are registered and resolved (keyed DI + factory, single resolution path). `IPricingStrategy` gets no equivalent treatment anywhere in the nine ADs.

The Deferred section's entry for the discount rule says *"AD-1/AD-5 already fix how they're wired"* — but AD-1 governs dependency direction (BLL may depend on Domain/DAL interfaces) and AD-5 governs DI lifetimes (Scoped vs. Singleton); neither actually specifies the selection/registration mechanism for `IPricingStrategy` the way AD-7 does for `IOrderProcessor`. It's plausible the intended design is "single strategy registered at the composition root, swapped by changing that one registration line" — but that's never stated as a Rule, only implied by the Deferred item's (incorrect) cross-reference. Two stories could reasonably diverge here: one hardcodes a single `IPricingStrategy` registration, another builds a keyed-DI selection mechanism mirroring AD-7 on the (unstated) assumption that pricing should also vary per some key (e.g., customer tier at runtime, not just at composition-root swap time).

**Recommendation:** Either add a short AD fixing the wiring mechanism for `IPricingStrategy` (even if only "single Scoped registration at composition root, swappable by changing that registration — no keyed dispatch needed since the strategy doesn't vary per-Order the way OrderType does"), or correct the Deferred item's justification to cite the actual mechanism instead of AD-1/AD-5.

---

### [LOW] No convention for top-level UI exception/error surfacing from BLL failures

**Section:** Consistency Conventions — Validation & error handling.

The Validation & error handling convention correctly separates expected validation outcomes (`Result<T>`) from infrastructure exceptions (DB unavailable, etc. — real exceptions). But nothing in the spine says how those infrastructure exceptions, once thrown, get surfaced to the user at the Presentation layer — e.g., a global `Application.ThreadException`/`AppDomain.UnhandledException` handler at the composition root, vs. each Presenter wrapping its own try/catch around every BLL call. AD-3 gives Presenters the sole path to BLL calls, so this would be a natural place to also fix the failure-handling contract, but it isn't addressed. Low risk because it's cosmetic/UX-level rather than structurally divergent, but worth a one-line convention to keep 10+ Presenters from independently inventing their own error-display pattern.

---

### [LOW] AD-3's "Prevents" claim is only partially enforced by its Rule

**Section:** AD-3 — Presentation: constructor-injected Presenter + per-screen IView.

AD-3 states it **Prevents** "Validation, pricing, or workflow logic living in Form code-behind." The Rule itself only mandates that *BLL calls* go exclusively through the Presenter ("Only the Presenter may call BLL services; the Form calls only its Presenter"). It does not structurally prevent a Form from containing inline logic that never calls BLL at all (e.g., a Form computing a discount total itself using local arithmetic instead of delegating to the Presenter/BLL) — nothing in the Rule's wording rules that out, since the compiler has no way to enforce "no business logic," only "no direct BLL calls." This is a common soft spot in MVP-style rules and is low severity since the rest of the spine (Result<T> convention, single composition root) makes the violation easy to spot in review, but the "Prevents" clause overstates what the Rule mechanically guarantees.

---

## Checklist Pass/Fail Summary

| Checklist item | Result |
| --- | --- |
| Fixes real divergence points for the level below, misses none | Partial — misses optimistic concurrency (Medium) and pricing-strategy wiring (Medium) |
| Every AD's Rule is enforceable and prevents its stated divergence | 7/9 clean (AD-2, 4, 5, 6, 7, 8, 9); AD-1 has an internal contradiction (High); AD-3's Prevents overstates its Rule (Low) |
| Nothing under Deferred is actually load-bearing | 6/7 clean; Deferred item 1 ("Concrete discount rules") has an inaccurate justification that masks a real open wiring question (Medium, same as above) |
| Named tech is verified-current, Stack table matches memlog | Pass — .NET 10, EF Core 10.0.9, xUnit 3.2.2, keyed DI since .NET 8 all match memlog's 2026-08-06 web-verified findings exactly |
| Greenfield, no parent spine, no spec input — N/A checks | Pass — `companions: []`, sources are brief/addendum/PRD only, no inherited-AD or brownfield-ratification content present, correctly so |
| Every dimension decided/deferred/open, especially operational/environmental envelope | Deployment & environments: Pass (Deferred, correctly tied to PRD Non-Goals, no CI/CD or hosting topology needed). Optimistic concurrency: Fail — silent (Medium, above) |
| Mermaid diagrams valid and non-empty | Pass on syntax/non-emptiness for both the dependency graph and the ERD — no placeholder nodes, cardinalities in the ERD match the Glossary's entity relationships; but see AD-1 finding above for a content-level (not syntax-level) inconsistency in the dependency graph |

---

## Notes on What's Done Well

- All 9 ADs are individually scoped, tagged `[ADOPTED]`, and traceable to a specific memlog decision — no invented ADs, no unexplained gaps between memlog and spine.
- The Capability → Architecture Map is complete against all 15 FRs; nothing in the PRD's feature set (§4.1–4.9) is unmapped.
- The Deferred section is disciplined: six of seven items correctly identify *why* the item is safe to leave open (it doesn't change an already-fixed AD contract), and the deployment/environment item is the strongest entry in the section — it explicitly ties the omission back to the PRD's Non-Goals rather than just leaving it out.
- Stack table is a clean match to the memlog's (version) line — no drift, no stale versions, no unverified claims presented as fact.
