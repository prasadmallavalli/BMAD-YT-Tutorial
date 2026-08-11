---
title: "Reconciliation: addendum.md → prd.md"
created: 2026-08-06
---

# Reconciliation: Addendum → PRD

Source input: `briefs/brief-BMAD YT TUTORIAL-2026-08-06/addendum.md`
Output PRD: `prds/prd-BMAD YT TUTORIAL-2026-08-06/prd.md`

Scope of this check: whether product-relevant open items from the addendum are correctly carried into PRD §8/§9, whether any product-shaping (capability-affecting) content was wrongly dropped, and whether the PRD's pointer/reference to the addendum accurately describes it. Tech-how content correctly staying out of the PRD is NOT flagged as a gap.

## Item-by-item

| # | Addendum item | Reflected in PRD? | Where / how |
|---|---|---|---|
| 1 | Tech stack decisions (.NET 8/WinForms, EF Core, MS.Extensions.DI, MSSQL, xUnit) | Correctly excluded as tech-how | PRD Vision (§1) and glossary (§3) name the technologies only insofar as they define capabilities (e.g. "real SQL Server database," "`Microsoft.Extensions.DependencyInjection`" as Composition Root definition), never the rationale/options-considered. §0 pointer correctly attributes rationale to addendum. |
| 2 | UI depth — production-grade UI chosen (async ops, rich validation, DataGridView, MVP-ish separation), justified by (a) live-interview defensibility and (b) portfolio-piece reuse | Partially reflected | Rich validation → FR-1/FR-2/FR-3 "Consequences (testable)"; DataGridView → FR-3 "line-item grid"; MVP-ish separation → correctly left as open architecture item (§8, closing note), consistent with addendum's own framing that this is still undecided. Interview-defensibility motive → strongly present in Vision §1 and UJ-1/UJ-2. **Not reflected:** "async operations" as a named UI characteristic, and the "portfolio piece" framing (see Gaps). |
| 3 | Domain — Order Management chosen over Employee/HR, Bank Loan, Hospital | Correctly excluded (rationale, not a capability) | Domain is simply the PRD's operating universe (Customers/Products/Orders throughout §3-§4). No need to restate why alternatives were rejected. |
| 3b | Domain-too-thin contingency: if Order Management can't naturally justify a requested pattern (Chain of Responsibility, Decorator, Adapter), fold in a secondary scenario — e.g. multi-step order-approval chain (COR), or a notification-channel adapter for email/SMS/in-app (Adapter) | **Not reflected anywhere**, not even as a flagged risk/open question | See Gaps #1. This is conditional but genuinely product-shaping (a new approval-workflow capability, or a multi-channel notification capability) and stands in quiet tension with FR-9's assumption of in-app-only notification. |
| 4 | Pattern list — confirmed minimum (Repository, UoW, Strategy, Factory, Observer, DI); Singleton mentioned in original request, architecture should find a legitimate use rather than force one | Reflected | Vision §1 names all six confirmed patterns. Singleton open item carried faithfully as PRD §8 Open Question 4, including the "carried from the brief's addendum" attribution and the "don't force it" spirit implicit in leaving it open rather than mandating an FR. |
| 5 | Testability rationale — companion test project exists because "how would you test this?" is a near-guaranteed follow-up; payoff is demonstrated mocking, not raw coverage | Reflected well | §4.9/FR-14, UJ-4, SM-3 all capture this precisely, including the "not coverage for its own sake" nuance (FR-14 targets specific mocked-interface demonstrations, not a coverage threshold). |
| 6a | Open item: EF Core relationship shapes / soft-delete-auditing (`IAuditable`) | Correctly excluded, referenced accurately | PRD §8 closing note explicitly names this as staying in addendum.md for Architecture. |
| 6b | Open item: before/after code as separate project/folder vs. git-history-only | Correctly excluded, referenced accurately | Same closing note; FR-12 states the product-level requirement (independently viewable/runnable pairs) without dictating the folder-vs-project mechanism — good separation of capability vs. implementation. |
| 6c | Open item: LocalDB vs. full SQL Server Developer Edition | Correctly excluded, referenced accurately | Same closing note. |
| 6d | Open item: MVP vs. plain code-behind-with-discipline for WinForms | Correctly excluded, referenced accurately | Same closing note. Minor observation (not a gap): FR-10 already constrains presentation code to constructor-injected dependencies with "no direct `new` instantiation of service/repository types," which quietly narrows this "still fully open" decision space. Worth Architecture noting, not a PRD defect. |

## PRD's pointer to the addendum

- §0 Document Purpose: "tech-stack rationale, options-considered detail, and open architecture questions live there and are not repeated here" — accurate summary of the addendum's actual contents (tech decisions, UI-depth options, domain options, pattern list, testability rationale, four open items).
- §8 closing parenthetical: enumerates the four "Open items for Architecture phase" from the addendum almost verbatim (EF Core relationship shapes/soft-delete auditing; before/after placement; LocalDB vs. SQL Server Developer Edition; MVP vs. code-behind) — accurate and complete, no distortion.

## Product-relevant open items required by the task brief (discount rule, OrderStatus sequence, Singleton, notification channel)

All four are present and correctly carried into PRD §8 Open Questions (Q1–Q4), each cross-referenced to its originating FR/assumption. No gap here.

## Gaps

Ranked by importance — genuine gaps only.

1. **Domain-too-thin contingency (Chain of Responsibility / Adapter fallback) is entirely absent from the PRD, even as a flagged open question.** The addendum explicitly anticipates that Order Management may need a secondary scenario folded in — a multi-step order-approval chain (new capability) or a multi-channel (email/SMS/in-app) notification adapter — if a requested pattern doesn't naturally fit. This is conditional but is squarely product-shaping: if triggered, it adds a whole approval-workflow capability or expands notification scope beyond FR-9's current in-app-only assumption. The PRD carries the sibling "Singleton, don't force it" item from the same addendum section into Open Question 4 but drops this one entirely, leaving downstream Architecture/Epics with no PRD signal that MVP scope could expand this way.

2. **"Async operations" — an explicitly named characteristic of the chosen production-grade UI option — has no corresponding FR/NFR.** The addendum lists async operations alongside rich validation, DataGridView, and MVP-ish separation as what "production-grade UI" means; the PRD reflects the latter three (via FR-1–FR-3 consequences and the open MVP-vs-code-behind item) but never states a responsiveness/non-blocking-UI requirement, even though UJ-2 is explicitly a live screen-share demo where a frozen UI during Order confirmation (a DB round trip) would undercut the Vision's core "real, runnable, defensible under live scrutiny" claim.

3. **"Portfolio piece" rationale for the UI-depth decision is not carried through.** The addendum gives two co-equal reasons for choosing production-grade UI: defensible in a live interview screen-share, and usable as a portfolio piece "not just an internal exercise." The PRD Vision (§1) captures the first strongly and gestures at longevity ("becomes a living reference... reuses to mentor other engineers") but never states the portfolio/reuse-as-work-sample framing explicitly. Minor — the spirit is roughly present, but the specific motivation is diluted.
