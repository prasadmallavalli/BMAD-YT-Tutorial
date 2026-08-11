# Adversarial Review — OrderFlow Desktop Architecture Spine

**Reviewer stance:** adversarial / red-team. Method: for each AD, construct two concrete developers each building a different Epic/Story one level below the spine, each following every applicable AD's Rule text literally, and ask whether their two outputs can be wired together without a merge-time surprise. A "hole" is reported only when both implementations are individually spine-compliant (would pass code review against the AD text as written) yet integration produces incompatible shapes, dueling owners of the same state, or a runtime failure.

**Verdict:** 8 genuine divergence holes found, 2 of them critical (they compound each other and threaten transactional correctness on the app's central use case: an order that also touches inventory). Technology claims: all verified real and current; one is already slightly stale as of today.

---

## Part 1 — Technology stack verification

| Claim in spine | Verified? | Note |
| --- | --- | --- |
| .NET 10, LTS, supported through 2028-11 | **Verified** | .NET 10 is the Nov 2025 LTS release; Microsoft's 3-year LTS window puts end-of-support at 2028-11-10. Matches the spine. |
| WinForms via `Microsoft.NET.Sdk.WindowsDesktop` on .NET 10 | **Verified** (general knowledge, SDK has carried WinForms/WPF since .NET Core 3.0; no reason to doubt continuity into .NET 10) | Not independently re-searched — low-risk, well-established claim. |
| `Microsoft.EntityFrameworkCore` / `.SqlServer` 10.0.9 | **Verified, but already stale** | NuGet confirms `Microsoft.EntityFrameworkCore.SqlServer` 10.0.9 is a real, published package. However the same search surfaced `Microsoft.EntityFrameworkCore` **10.0.10** already on NuGet — i.e., a newer patch exists as of today (2026-08-06). The spine's own footnote ("code owns exact pins once it exists") already anticipates this, so it's not a defect, but the "latest verified" framing is already one patch behind at the moment of writing. Flag as a freshness note, not a fabrication. |
| `Microsoft.Extensions.DependencyInjection` ships with .NET 10 SDK; keyed-services API stable since .NET 8 | **Verified** | `AddKeyedSingleton`/`AddKeyedScoped`/`AddKeyedTransient` and `GetRequiredKeyedService`/`GetKeyedService` were introduced in .NET 8 and are stable, unchanged API surface carried into .NET 10. AD-7's exact API names (`AddKeyedScoped<IOrderProcessor>`, `GetRequiredKeyedService`) are real, current method names. |
| `xunit.v3` 3.2.2 | **Verified** | Real, published release (xunit.net release notes date it 2026-01-14), consistent with a spine authored 2026-08-06. |

No claim was found to be asserted-but-unverifiable or fabricated. The only actionable item is the EF Core patch-version freshness note above.

---

## Part 2 — Divergence holes

### Hole 1 (Critical) — AD-3 and AD-5 define "business operation" scope boundary contradictorily

**AD-3's Rule:** Presenter is *constructor-injected* with the BLL service interfaces it needs, resolved at the composition root.
**AD-5's Rule:** Scoped services resolve from "an `IServiceScope` created **per business operation** and disposed at its end."

Neither AD says what a "business operation" *is*, and the two Rules force two different answers:

- If a Presenter is constructor-injected once (AD-3), the scope containing its BLL services must already exist *before* the Presenter is built — i.e., scope = Form-session lifetime, not per-click.
- If the scope is genuinely per-operation and disposed at the end of each operation (AD-5, literally read), a Presenter *cannot* hold long-lived constructor-injected service references across multiple user actions, because those services (and their DbContexts, per AD-2) would already be disposed after the first operation completes.

**Concrete pair:**
- **Developer A** (Order Entry, FR-3/FR-4 — multi-step form: add customer, add line items, submit) reads AD-3 literally: creates one `IServiceScope` when `OrderForm` opens, constructor-injects `OrderPresenter` with `IOrderService` resolved from that scope, and reuses it across every field edit and the final Submit click. "Business operation" = the whole form session.
- **Developer B** (Inventory, FR-5/FR-6 — availability checks) reads AD-5 literally: creates a **fresh** `IServiceScope` for every single availability-check call, resolves `IInventoryService` from it, and disposes the scope immediately after. "Business operation" = one method call.

Both pass individual review against their respective AD's text. They collide the moment Order Entry needs to consult Inventory as part of the *same* atomic submit (decrement stock when the order is placed — the app's central transactional use case). `IOrderService` is running under Developer A's session-long scope/DbContext; `IInventoryService`, if reused as built, spins up its own short-lived scope/DbContext per Developer B's convention. Two DbContext instances now participate in what must be one transaction — see Hole 2 for the resulting failure mode.

**Fix — tighten AD-5 (and cross-reference from AD-3):** Define "business operation" explicitly, e.g.: *"A business operation is exactly one Presenter method invocated by one user-initiated action (button click, grid commit, etc.), never a whole Form session."* Then amend AD-3's Rule so Presenters are constructor-injected with an `IServiceScopeFactory` (or a thin per-operation invoker), not with long-lived BLL service instances — otherwise AD-3's own literal text is incompatible with AD-5's literal text.

---

### Hole 2 (Critical) — AD-2 + AD-9 don't say who owns DbContext creation across multiple repositories in one operation

AD-2's Rule: *"Repositories **and** the Unit of Work call `CreateDbContextAsync()` once per business operation."* This sentence grants the calling right to **both** repositories and the UnitOfWork, without saying which one actually does it when both are in play, or whether they must share the resulting instance.

AD-9's Rule only says BLL depends on `I*Repository` and `IUnitOfWork` interfaces — it never states whether repositories are exposed *through* `IUnitOfWork` (guaranteeing one shared context) or injected as independent siblings alongside it (each free to open its own).

**Concrete pair:**
- **Developer A** (Order Entry, FR-3/FR-4) implements `IUnitOfWork` as the sole DbContext owner: `IUnitOfWork.Orders`, `.Customers` are repository properties backed by one `AppDbContext` obtained once via the factory when the UoW is constructed; `SaveChangesAsync()` commits everything together.
- **Developer B** (Inventory, FR-5/FR-6) implements `InventoryRepository` as an independently DI-registered, constructor-injected class that calls `IDbContextFactory<AppDbContext>.CreateDbContextAsync()` itself inside each method — a completely literal reading of AD-2's "Repositories... call `CreateDbContextAsync()`."

Both satisfy AD-2's text standalone, and both pass unit tests (which mock the interfaces, never exercise the real factory contention). Wire them together for order placement + inventory decrement and you get **two separate DbContexts / two separate change trackers / two separate implicit transactions** for one logical operation. `IUnitOfWork.SaveChangesAsync()` commits the order but never touches Inventory's independently-managed context — silent partial failure (order persisted, stock not decremented, no exception, no rollback) that no unit test targeting either developer's module in isolation would ever catch.

**Fix — tighten AD-2 and AD-9 together:** State a single owner, e.g.: *"`IUnitOfWork` alone calls `CreateDbContextAsync()`. Repositories never call the factory themselves; they receive the ambient `DbContext` via a constructor parameter supplied by `IUnitOfWork` at construction time. Every repository used within one business operation shares exactly one `DbContext` instance, obtained by the `IUnitOfWork` that scopes that operation."* This also closes the "repository vs UoW" ambiguity for every future Epic that spans more than one entity.

---

### Hole 3 (High) — `OrderProcessorFactory`'s own DI lifetime is unstated, and "Factory" naming invites the wrong one

AD-7 pins the *processors'* lifetime (`AddKeyedScoped<IOrderProcessor>`) but never states the **factory's own** registration lifetime. AD-5's enumeration of what's Scoped ("Repositories, `IUnitOfWork`, and BLL services") is ambiguous about whether a factory class counts as a "BLL service" — and the word "Factory" conventionally reads as "stateless, register Singleton" to most .NET developers, which is exactly the wrong choice here.

**Concrete pair:**
- **Developer A** (composition root / DI wiring, FR-10) registers `OrderProcessorFactory` as `AddSingleton<OrderProcessorFactory>()` — natural instinct for a class with no mutable state, one method (`Create`), and a name ending in "Factory."
- **Developer B** (FR-15, AD-7) implements each `IOrderProcessor` as `AddKeyedScoped`, correctly per AD-7's Rule, and each processor takes a Scoped repository/UnitOfWork dependency.

If `OrderProcessorFactory` is Singleton and captures `IServiceProvider` at construction (the **root** provider, not a per-operation scope), then `_serviceProvider.GetRequiredKeyedService<IOrderProcessor>(orderType)` either throws `InvalidOperationException: Cannot resolve scoped service from root provider` (if scope validation is enabled, the .NET default in dev builds), or — if validation is off — silently resolves against the root and creates a processor instance that captures a `DbContext`-backed dependency that outlives any single operation, a classic captive-dependency leak. Either way this is invisible in each developer's own tests (mocked `IServiceProvider` in unit tests never exercises real scope validation) and only detonates at runtime, at the composition root, once both are wired.

**Fix — tighten AD-7 (or AD-5):** *"`OrderProcessorFactory` is registered Scoped, resolves keyed services from the ambient scoped `IServiceProvider` injected into it (never a captured root provider or `IServiceScopeFactory` held from Singleton construction)."*

---

### Hole 4 (Medium-High) — `INotifier`'s payload contract and DI lifetime are both unspecified

AD-4 fixes *who* calls `INotifier.Notify(...)` and *when* (after commit), but never states the method's signature, and the Deferred list only defers **where** notifications surface (toast/log/both) — not the **data shape** passed into `Notify`. AD-5's Scoped/Singleton enumeration never mentions `INotifier` at all, so its lifetime is also open.

**Concrete pair:**
- **Developer A** (Order Detail screen) only needs to refresh the one open screen, so builds `INotifier.Notify(int orderId, OrderStatus newStatus)` — minimal payload.
- **Developer B** (Order List / Dashboard screen, same FR-8/FR-9 capability) needs to render a cross-screen toast/log line with customer name and total, so needs `INotifier.Notify(OrderDto order, string message)`.

Only one interface can exist. Whichever wins, the other developer's screen is forced to make an extra BLL round-trip just to re-fetch the data the notification didn't carry — quietly reintroducing UI-initiated BLL calls outside the Presenter-owns-the-call-graph model AD-3 was written to prevent, and duplicating the exact data OrderStatusService already had in hand at the point it called `Notify`.

**Fix — new/tightened AD-4 clause:** Pin the exact signature/payload (e.g., a dedicated `OrderStatusChangedNotification { int OrderId; OrderStatus Old; OrderStatus New; }` DTO) and its DI lifetime (Singleton is the natural choice if UI-side subscribers must outlive individual Scoped operations — but say so explicitly, don't leave it to inference).

---

### Hole 5 (Medium) — AD-4's transition table doesn't say whether it's OrderType-aware, but AD-7 exists specifically because OrderType changes behavior

AD-4 describes "the allowed-transition table" as a single, generic, OrderType-agnostic structure. AD-7's entire premise is that `OrderType` (Standard vs Rush) drives materially different processing. If Rush orders are meant to skip a state Standard orders must pass through (a very plausible FR-15 requirement, and PRD §8 Q5's "Standard vs. Rush exact behavioral difference" is explicitly Deferred to Epics), the two owners must agree on whether `OrderStatusService`'s transition check takes `OrderType` as an input.

**Concrete pair:**
- **Developer A** (owns `OrderStatusService`, FR-8/FR-9) ships `Result<Unit> TransitionTo(int orderId, OrderStatus newStatus)` — no `OrderType` parameter, because AD-4's text never mentions one.
- **Developer B** (owns `RushOrderProcessor`, FR-15/AD-7) needs Rush orders to reach `Processing` directly from `Created`, a transition Standard orders must not be allowed to make. Unable to express that through Developer A's signature, Developer B either (a) calls a nonexistent overload, breaking the build against A's actual interface, or (b) adds an `if (orderType == Rush)` bypass check inside the processor before calling `TransitionTo`, which is exactly "another class evaluates transition validity" — a direct violation of AD-4's own Prevents clause, produced *because* AD-4 didn't give Developer B a compliant way to express the requirement.

**Fix — tighten AD-4:** State explicitly whether `TransitionTo` accepts `OrderType` and whether the transition table is partitioned by it, so AD-7 processors have exactly one compliant path to request type-specific transitions.

---

### Hole 6 (Medium) — Domain-entity vs DTO boundary at BLL service signatures is unpinned

The Consistency Conventions table lists `XxxDto` as a naming convention but no AD states *when* BLL methods must use DTOs vs Domain entities. AD-1's own diagram draws `Presentation --> Domain` directly (in addition to `Presentation --> BLL`), which invites Presentation code to hold Domain entity references directly rather than exclusively through BLL-returned DTOs.

**Concrete pair:**
- **Developer A** (Order Entry) designs `IOrderService.CreateOrder(OrderDto dto) : Result<OrderDto>` — DTO-in/DTO-out, mapping fully inside BLL, no leaked EF-tracked entities.
- **Developer B** (Inventory) designs `IInventoryService.CheckAvailability(Product product) : Result<bool>` — takes and returns the real `Domain.Product` entity directly, legal because AD-1 permits Presentation to reference Domain.

When the Order Entry Presenter needs `Product` data to build order line items and pass them through Inventory's entity-typed API, but must hand DTO-shaped data to Order's DTO-typed API, the Presenter ends up doing ad hoc mapping between the two shapes that neither AD-1, AD-3, nor the naming-convention table assigns an owner for — and worse, a `Product` entity obtained from Inventory's per-operation (AD-2) DbContext is detached the instant that operation's scope disposes, yet nothing stops the Presenter from holding and later re-passing that now-stale, un-tracked entity into a different operation, silently defeating AD-2's per-operation isolation.

**Fix — new AD (e.g. AD-10):** *"BLL service methods that are called from Presentation always accept/return `XxxDto` types; Domain entities never cross the BLL→Presentation boundary. Mapping between entity and DTO lives entirely inside BLL (or a dedicated mapping layer within it)."*

---

### Hole 7 (Lower-Medium) — AD-6's stamping rule doesn't protect `CreatedAt` against the disconnected-entity pattern AD-2 mandates

AD-2 forces every operation onto a fresh, short-lived `DbContext` (a disconnected-entity pattern by construction — entities loaded in one operation are detached by the time a later operation needs to update them). AD-6 says `AppDbContext` stamps `CreatedAt`/`UpdatedAt` in an overridden `SaveChanges`, but never states the stamping logic must special-case `EntityState.Added` for `CreatedAt` vs `EntityState.Modified`, nor how repositories are expected to re-attach a detached/DTO-rehydrated entity for an update (`Update()`-marks-everything-modified vs targeted property updates).

**Concrete pair:**
- **Developer A**'s `OrderRepository.Update(Order order)` calls `_context.Update(order)` (marks the whole graph `Modified`), relying on the incoming object to already carry the correct `CreatedAt` (round-tripped faithfully from whatever DTO shape Hole 6 leaves unresolved).
- **Developer B**'s `CustomerRepository.Update(Customer customer)` does a targeted `Entry(customer).Property(x => x.Name).IsModified = true`-style partial update, never touching `CreatedAt` regardless of what the incoming object contains.

If the shared `AppDbContext.SaveChanges` override stamps `CreatedAt` only by checking `EntityState.Added`, Developer A's approach is safe *only if* the DTO→entity mapping (Hole 6) reliably carries the original `CreatedAt` through every update round-trip — a fact no AD guarantees, and easy to get wrong (e.g., an `OrderDto` built for the create form has no `CreatedAt` field at all, defaults to `DateTime.MinValue` on the entity reconstructed for an edit, and `Update()` marks it `Modified`, silently corrupting the audit trail).

**Fix — tighten AD-6:** Require the `SaveChanges` override to explicitly ignore/refuse to overwrite `CreatedAt` for any entity not in `EntityState.Added` (defense in depth, independent of what repositories send it), and state whether repositories must use targeted property updates or full-graph `Update()`.

---

### Hole 8 (Low) — AD-8 is silent on whether `OrderFlow.Exhibits` may reference `OrderFlow.Domain`

AD-8's Rule text only forbids the *runtime* graph (Presentation/BLL/DAL) from referencing Exhibits. The structural mermaid diagram draws an edge `Exhibits -.no runtime reference.-> Domain`, which is genuinely ambiguous: is that a real compile-time project reference to `OrderFlow.Domain` that's simply never wired into the app's DI container, or is the label saying no reference of any kind exists?

**Concrete pair:**
- **Exhibit author A** (an OCP/DIP exhibit) references `OrderFlow.Domain.Order` directly, reusing the real entity to keep the example grounded.
- **Exhibit author B** (an SRP/LSP exhibit) reads AD-8 as "Exhibits must be fully standalone" and redefines a local toy `Order` class with no project reference to Domain at all.

Not a runtime defect (AD-8's actual Prevents clause — nothing wired into the running app — holds either way), but it produces an internally inconsistent `OrderFlow.Exhibits` project where different Before/After pairs use different vocabularies for the "same" domain concept, undermining the pedagogical/interview-prep purpose that is FR-12's entire point.

**Fix — tighten AD-8:** State explicitly whether Exhibits may take a compile-time reference to `OrderFlow.Domain` (recommended: yes, to keep exhibits grounded in real domain vocabulary) and require all exhibit pairs follow the same convention.

---

## Summary table

| # | Severity | ADs implicated | One-line fix |
| --- | --- | --- | --- |
| 1 | Critical | AD-3, AD-5 | Define "business operation" as one Presenter-invoked user action; make Presenters take an `IServiceScopeFactory`, not long-lived injected services. |
| 2 | Critical | AD-2, AD-9 | Only `IUnitOfWork` calls `CreateDbContextAsync()`; repositories receive the DbContext from it, never open their own. |
| 3 | High | AD-7, AD-5 | Register `OrderProcessorFactory` Scoped; forbid resolving keyed services from a captured root provider. |
| 4 | Medium-High | AD-4 | Pin `INotifier.Notify(...)`'s exact payload DTO and its DI lifetime. |
| 5 | Medium | AD-4, AD-7 | State whether the transition table takes/is partitioned by `OrderType`. |
| 6 | Medium | AD-1, new AD-10 | BLL↔Presentation boundary is DTO-only; Domain entities never cross it. |
| 7 | Lower-Medium | AD-6 | `SaveChanges` override must refuse to touch `CreatedAt` outside `EntityState.Added`, independent of repository behavior. |
| 8 | Low | AD-8 | State explicitly whether Exhibits may reference `OrderFlow.Domain`. |

Every hole above is reported as a fix to a specific AD (tightened Rule text) or a new AD, per the review brief — none is left as an abstract risk.
