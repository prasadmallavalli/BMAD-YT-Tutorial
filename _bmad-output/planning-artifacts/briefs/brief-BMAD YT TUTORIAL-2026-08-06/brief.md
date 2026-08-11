---
title: "Product Brief: OrderFlow Desktop — WinForms Interview-Prep Reference App"
status: draft
created: 2026-08-06
updated: 2026-08-06
---

# Product Brief: OrderFlow Desktop — WinForms Interview-Prep Reference App

## Executive Summary

OrderFlow Desktop is a real, runnable WinForms order-management application built specifically to serve as interview-preparation material for a senior team-lead role requiring 12+ years of experience in ASP.NET WinForms, C#, Entity Framework, layered architecture, MSSQL, SOLID principles, design patterns, and dependency injection. It is not a slide deck or a Q&A cheat sheet — it is working code, backed by a real SQL Server database, that a candidate can open, run, walk through on a screen-share, and defend line-by-line under interview scrutiny.

The core idea: every topic that shows up in a senior WinForms interview (repository pattern, unit of work, SOLID violations vs. fixes, constructor injection, EF Core relationships, transaction handling) exists in this codebase as an actual feature solving an actual business problem — not an isolated snippet. A companion unit test project proves the architecture's testability claims rather than just asserting them.

## The Problem

Senior WinForms/.NET interviews rarely stop at "define the Single Responsibility Principle." They ask candidates to open a codebase, explain why a class is structured the way it is, point to where dependency injection is wired up, or refactor a tightly-coupled data-access call live. Candidates who only study definitions and isolated LeetCode-style pattern snippets struggle here — they can recite SOLID but can't point to a real class boundary and explain the trade-off they made.

At the same time, most "design patterns in C#" tutorials online use toy examples (shapes, animals, `Foo`/`Bar` classes) disconnected from the kind of layered enterprise WinForms + MSSQL systems a 12-year veteran is actually expected to have built and led teams on. There's a gap between "I can explain the Strategy pattern" and "I can show you the Strategy pattern doing real work in a pricing engine I built."

## The Solution

An Order Management System built as a desktop WinForms app on .NET 8, backed by MSSQL via EF Core, structured in classic enterprise layers:

- **Presentation (WinForms)** — production-grade forms (grids, validation, async operations) that stay thin and delegate to the business layer, following an MVP-style separation so forms are not doing business logic.
- **Business Logic Layer (BLL)** — order validation, pricing/discount calculation, inventory checks, order-status workflow, all expressed through interfaces so they're swappable and testable.
- **Data Access Layer (DAL)** — Repository + Unit of Work over EF Core, isolating persistence concerns from business rules.
- **Cross-cutting** — a DI container (`Microsoft.Extensions.DependencyInjection`) wires everything at startup; no `new` calls scattered through business/UI code.

The business domain — customers, products, orders, order items, inventory, pricing, order-status workflow, notifications — is rich enough to justify multiple design patterns appearing for real reasons: a pricing/discount **Strategy**, an order-status **State**-ish workflow, a **Factory** for order processors by order type, an **Observer**/event mechanism for order-status notifications, **Repository + Unit of Work** for persistence, and a couple of intentionally-included **SOLID-violating "before" snippets refactored to "after"** to make the principles concrete rather than abstract.

A companion xUnit test project mocks the BLL/DAL interfaces to prove the architecture is testable — turning "why does DI matter" from a talking point into a demonstrated fact.

## Who This Serves

**Primary user:** the candidate themself (you) — a senior engineer/team lead with 12+ years in WinForms/C#/EF/MSSQL preparing for interviews, who needs a project they personally understand deeply enough to defend under questioning, not a project they copy-pasted.

**Secondary use:** the same project doubles as onboarding/mentoring material — something to walk a mentee or interview panel through to demonstrate how these concepts fit together in a cohesive system rather than isolated examples.

## Success Criteria

- The app runs end-to-end against a real MSSQL database: create a customer, place an order with multiple line items, see inventory decrement, see pricing/discount rules apply, see order status progress, see a notification fire.
- Every named interview topic (each SOLID principle, each design pattern, DI, DAL/BLL separation, EF Core usage) maps to a specific, findable class or file — see **Interview Topic → Code Map** below, to be finalized once the architecture and epics exist.
- The test project passes and visibly demonstrates mocking a repository/service through its interface — the payoff of the DI + Repository investment.
- The candidate can, unaided, explain *why* each pattern was used in this specific context (not just what the pattern is) — the brief and downstream docs exist to support that, not replace the candidate's own understanding.

## Scope

**In scope (v1):**
- Customers, Products, Orders, Order Items, Inventory, Order Status workflow
- WinForms UI: customer/product management, order entry with grid line items, order list/detail with status transitions
- BLL: order validation, pricing/discount strategy, inventory availability checks, order-status transitions, notification triggering
- DAL: Repository + Unit of Work over EF Core / MSSQL, migrations for schema
- DI wiring via `Microsoft.Extensions.DependencyInjection`
- Companion xUnit test project with mocked dependencies
- Explicit before/after refactor examples for at least 2-3 SOLID principles

**Out of scope (v1):**
- Authentication/authorization, multi-user concurrency handling beyond basic optimistic concurrency
- Reporting/analytics, printing, exporting
- Deployment/installer packaging
- Web or API layer (this is a WinForms desktop app, not a web app with a WinForms front end)
- Multi-tenant or multi-currency support

## Interview Topic → Code Map

To be completed/finalized during Architecture and Epics/Stories — but the intent, locked here, is that this map ships as part of the project itself (e.g. a `docs/interview-topic-map.md`) so the codebase is directly usable as study material:

| Topic | Where it lives (planned) |
|---|---|
| Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion | BLL/DAL interface boundaries; explicit before/after refactor pair |
| Repository + Unit of Work | DAL project |
| Strategy | Pricing/discount calculation |
| Factory | Order processor creation by order type |
| Observer / eventing | Order-status change notifications |
| Dependency Injection | Composition root / `Program.cs` + constructor injection throughout BLL/DAL/forms |
| Entity Framework Core | DAL entity configs, relationships, migrations, transactions |
| DAL vs BLL separation | Project structure itself (separate class libraries) |
| Testability | Test project mocking BLL/DAL interfaces |

## Vision

If this proves useful beyond the immediate interview cycle, it becomes a living reference: a place to add one new pattern or refactor exercise at a time, and a teaching artifact for mentoring other engineers making the same "I know the definitions but not the application" leap in their own interview prep.
