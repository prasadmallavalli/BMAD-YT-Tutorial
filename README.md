# OrderFlow Desktop

[![Tests](https://github.com/prasadmallavalli/BMAD-YT-Tutorial/actions/workflows/tests.yml/badge.svg)](https://github.com/prasadmallavalli/BMAD-YT-Tutorial/actions/workflows/tests.yml)

A real, runnable WinForms order-management app built as interview-prep reference material for a senior .NET/WinForms role — repository pattern, unit of work, strategy/factory patterns, SOLID before/after pairs, DI, EF Core, and optimistic concurrency, all demonstrated as actual working features solving actual business problems, not toy snippets.

## What this is

Every topic that shows up in a senior WinForms interview exists in this codebase as something you can open, run, and defend line-by-line under questioning:

- **Presentation (WinForms)** — thin forms following an MVP-style Presenter/`IView` split, no business logic in code-behind.
- **Business Logic Layer** — order validation, pricing (Strategy pattern), order-status workflow, inventory checks, all behind interfaces.
- **Data Access Layer** — Repository + Unit of Work over EF Core, isolating persistence from business rules.
- **Composition root** — a single `Program.cs` wires the whole DI graph at startup; no `new` calls scattered through business/UI code.

See [`OrderFlow/docs/interview-topic-map.md`](OrderFlow/docs/interview-topic-map.md) for the full topic → class/file mapping, and `OrderFlow.Exhibits/` for standalone Before/After SOLID pairs (SRP, OCP, DIP) you can run and diff independently of the full app.

## Tech stack

| | |
|---|---|
| Runtime | .NET 10 (LTS) |
| UI | WinForms (`Microsoft.NET.Sdk.WindowsDesktop`) |
| ORM | EF Core 10.0.x + SQL Server LocalDB |
| DI | `Microsoft.Extensions.DependencyInjection` |
| Tests | xUnit v3 + Moq |

## Project structure

```
OrderFlow/
  OrderFlow.sln / OrderFlow.slnx
  OrderFlow.Domain/              # Entities, enums, IAuditable — no outward references
  OrderFlow.DAL/                 # AppDbContext, EF configs, Repository + Unit of Work
  OrderFlow.BLL/                 # Services, DTOs, Result<T>, pricing/order-processor strategies
  OrderFlow.Presentation/        # WinForms Forms, Presenters, IViews, composition root (Program.cs)
  OrderFlow.Exhibits/            # Standalone Before/After SOLID pairs — isolated from the runtime DI graph
  OrderFlow.Tests/               # BLL/DAL unit tests (mocked IUnitOfWork)
  OrderFlow.Presentation.Tests/  # Presenter unit tests — requires Windows to execute (see below)
  docs/interview-topic-map.md    # Topic -> file mapping, maintained per FR-13

_bmad-output/    # Planning artifacts (brief, PRD, architecture, epics) and implementation
                 # records (per-story dev/review notes, sprint status, retrospectives)
```

## Building & running

Requires the .NET 10 SDK (pinned in `OrderFlow/global.json`).

```bash
cd OrderFlow
dotnet build OrderFlow.sln -c Release
dotnet run --project OrderFlow.Presentation
```

`OrderFlow.Presentation` and `OrderFlow.Presentation.Tests` target `net10.0-windows`. They **compile** cross-platform (via `EnableWindowsTargeting`), but the WinForms app and its Presenter tests can only **run** on Windows — there's no `Microsoft.WindowsDesktop.App` runtime for macOS/Linux. `OrderFlow.Tests` (BLL/DAL, no WinForms dependency) runs anywhere.

## Testing & CI

```bash
dotnet test OrderFlow.sln -c Release
```

GitHub Actions (`.github/workflows/tests.yml`) runs on `windows-latest` for every push/PR to `main`:

1. Restore, build, and run the full test suite — both `OrderFlow.Tests` and `OrderFlow.Presentation.Tests` execute for real, not just compile.
2. A smoke-test step launches the built `OrderFlow.Presentation.exe` and verifies its window opens (`MainForm` launches without throwing) before tearing it down.

## Project history

This app was built story-by-story through a structured planning → architecture → implementation workflow, with every story's acceptance criteria, code review findings, and deferred-work decisions recorded under `_bmad-output/implementation-artifacts/`. Epic retrospectives live alongside the stories (`epic-N-retro-*.md`) if you want the "what we learned" narrative rather than just the diffs.
