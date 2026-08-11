---
title: "Addendum: OrderFlow Desktop"
status: draft
created: 2026-08-06
updated: 2026-08-06
---

# Addendum: OrderFlow Desktop

Technical depth and options-considered detail captured during brief discovery, intended for the Architecture workflow rather than the brief itself.

## Technology decisions (locked during discovery)

- **.NET 8**, WinForms, over .NET Framework 4.8 — chosen deliberately over classic Framework/EF6/Unity to show currency with modern .NET, since many WinForms shops are mid-migration and interviewers probe for this.
- **EF Core** as the ORM (over Dapper or ADO.NET raw) — the request specifically named Entity Framework as a topic to demonstrate.
- **Microsoft.Extensions.DependencyInjection** as the DI container (built into modern .NET, no need for Unity/Autofac/Ninject) — simplest to defend as "the standard choice" in an interview, while still letting DI concepts be fully demonstrated.
- **MSSQL** as the database (explicitly required), via EF Core migrations — LocalDB is acceptable for local dev/demo, architecture should confirm.
- **xUnit** assumed for the test project (architecture may confirm/replace with NUnit/MSTest — no strong preference stated, xUnit is simply the modern default).

## UI depth — options considered

User explicitly chose **production-grade UI** (async operations, rich validation, DataGridView, proper MVP-ish separation) over "minimal, logic-first" and "functional but not fancy." Rationale given: it should be both defensible in a live interview screen-share and usable as a portfolio piece, not just an internal exercise. Architecture should size form list/complexity accordingly, but should not let UI polish crowd out time on the BLL/DAL/pattern work that is the actual point of the project.

## Domain — options considered, not chosen

Other domains discussed and rejected in favor of Order Management: Employee/HR Management (payroll, leave approval), Bank Loan Approval (credit checks, multi-step approval, interest strategies), Hospital/Patient Management (scheduling, prescriptions, billing). Any of these would have worked; Order Management was chosen as the most universally recognizable domain for an interview audience and cleanly supports the target pattern list (pricing strategy, order-type factory, status-change observer, repository/UoW).

If, during architecture or epics work, Order Management proves too thin to naturally justify a specific requested pattern (e.g. Chain of Responsibility, Decorator, Adapter), consider folding in a secondary scenario rather than switching domains — e.g. a multi-step order-approval chain for large orders (Chain of Responsibility), or a notification-channel adapter (email/SMS/in-app) for the Observer payload (Adapter).

## Pattern list — not exhaustive, expand during architecture

Confirmed intent to cover at minimum: Repository, Unit of Work, Strategy, Factory, Observer, and DI itself as a "pattern." Singleton was mentioned in the original request; architecture should find a legitimate use (e.g. a configuration/settings accessor or a DbContext-per-request factory boundary) rather than forcing it in artificially — a forced Singleton is a worse interview answer than an honest "I didn't need one here, and that's itself worth explaining."

## Testability — rationale

User confirmed a companion test project specifically because "how would you test this?" is a near-guaranteed senior-level follow-up to any DI/SOLID/Repository discussion. The test project's job is to make the *testability payoff* of the architecture concrete (mock an `IOrderRepository` or `IPricingStrategy` and verify BLL behavior in isolation) rather than to achieve high coverage for its own sake.

## Open items for Architecture phase

- Confirm exact EF Core relationship shapes (Order 1–many OrderItem, Order–Customer, OrderItem–Product) and whether soft-delete / auditing fields are worth including as another realistic interview topic (e.g. `IAuditable` interface — ties back to SRP/ISP).
- Decide whether the "before" (SOLID-violating) code lives as a separate small project/folder clearly labeled "before," or as commented-out/git-history-only — recommend a separate folder so it's easy to open both side by side in an interview.
- Confirm LocalDB vs. full SQL Server Developer Edition for local setup instructions.
- Decide MVP vs. plain code-behind-with-discipline for the WinForms presentation layer — brief assumes MVP-ish but architecture should make the call and name it explicitly, since "how do you structure WinForms to keep it testable" is itself an interview-relevant decision worth documenting.
