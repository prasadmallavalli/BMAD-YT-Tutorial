---
title: "Input Reconciliation: brief.md -> prd.md"
created: 2026-08-06
---

# Input Reconciliation: Brief -> PRD

**Input:** `briefs/brief-BMAD YT TUTORIAL-2026-08-06/brief.md`
**Output checked:** `prds/prd-BMAD YT TUTORIAL-2026-08-06/prd.md`

## Coverage Map

| Brief element | PRD coverage | Location | Assessment |
|---|---|---|---|
| Executive Summary | Reworded as Vision | PRD §1 | Faithful, near 1:1 in substance |
| The Problem (toy tutorials vs. real systems; candidates who recite but can't point) | Partially folded into Vision/JTBD | PRD §1, §2.1 | Weakened — see Gap 3 |
| The Solution — layered architecture (Presentation/BLL/DAL/cross-cutting) | Covered | PRD Glossary §3, FR-10, FR-11 | Faithful |
| The Solution — named patterns (Strategy, State-ish, Factory, Observer, Repo+UoW, before/after) | Mostly covered | FR-7 (Strategy), FR-8 (State-ish), FR-9 (Observer), FR-11 (Repo+UoW), FR-12 (before/after) | **Factory missing an FR** — see Gap 1 |
| The Solution — "MVP-style separation" for WinForms forms | Not present | — | Missing — see Gap 2 |
| Companion xUnit test project (mocks BLL/DAL) | Covered | FR-14, §4.9 | Faithful |
| Primary persona (candidate, 12+ yrs, needs deep ownership) | Covered | PRD §2.1 JTBD | Faithful |
| Secondary persona (mentor/mentee use) | Covered | PRD §2.1 JTBD ("As a mentor"), Vision §1 closing line | Faithful |
| Success Criteria (4 bullets: e2e run, topic map, test payoff, unaided explanation) | Covered | SM-1, SM-2, SM-3, SM-4 (§7) | Faithful, cleanly mapped 1:1 |
| Scope In (v1 list) | Covered | PRD §6.1 | Faithful, near-verbatim |
| Scope Out (v1 list) | Covered | PRD §5, §6.2 | Faithful, near-verbatim |
| Interview Topic -> Code Map (the table itself) | Referenced, not reproduced | FR-13, Glossary "Interview Topic Map" | Consistent with brief's own note that the table is draft-pending-Architecture; not a gap |
| Closing Vision paragraph | Covered | PRD §1, final paragraph | Near-verbatim, faithful |

## Gaps

1. **Factory pattern (Order Processor by order type) has no driving FR — and "order type" doesn't exist anywhere in the data model.** The brief names Factory as one of the core patterns the domain must justify ("a Factory for order processors by order type") and the brief's own Interview Topic -> Code Map table lists it explicitly. The PRD's Glossary (§3) still defines "Order Processor Factory," but none of FR-1 through FR-14 implements it, it's absent from §6.1 MVP Scope's BLL bullet list, and the Order glossary entry has no notion of an order "type" to dispatch on. As written, a team building strictly from this PRD's FRs would not build the Factory pattern at all, despite FR-13 claiming the topic map will cover "every named interview topic." This is the most consequential drop — it silently orphans one of five headline patterns.

2. **"MVP-style separation" for WinForms forms is dropped.** The brief's Solution section is specific: presentation-layer forms should follow "an MVP-style separation so forms are not doing business logic." This is a solution-shape decision, not a technical-how detail on the level of things the PRD explicitly defers to `addendum.md` (EF relationship shapes, LocalDB vs. SQL Server Developer Edition, etc.). Yet it appears nowhere in the PRD — not in Glossary, not in FR-10 (composition root), not in the UI-related FRs (1-4). Without it captured somewhere, Architecture/Epics has no PRD-level mandate to keep forms thin, which is itself an interview-relevant claim ("forms delegate to the business layer").

3. **The Problem's diagnostic "why" is compressed into implication rather than stated.** The brief's Problem section makes a specific two-part argument: (a) senior interviews now probe live code, not definitions, and candidates who only memorize principles fail that test; (b) most online "design patterns in C#" tutorials use toy examples disconnected from real enterprise WinForms+MSSQL systems, creating a specific credibility gap. The PRD's Vision (§1) and JTBD (§2.1) retain the *conclusion* ("not an isolated Foo/Bar example," "defend line-by-line under scrutiny") but drop the *argument* — there's no PRD passage that explains why toy tutorials fail candidates or why interviews escalated past definitional Q&A. This is qualitative/rationale content of exactly the kind an FR-structured PRD tends to lose; nothing downstream breaks without it, but it weakens the "why this project, why now" framing for anyone reading the PRD cold.

4. *(Minor)* Brief mentions forms should support "async operations" as part of the Solution's presentation-layer description. This doesn't appear in the PRD at all — not even as an `[ASSUMPTION]` or Open Question. Likely intended as a technical-how detail for Architecture, but unlike the LocalDB/soft-delete items, it isn't called out in §8's closing note as deferred, so it just silently disappeared.
