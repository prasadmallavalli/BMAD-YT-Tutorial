---
title: "Input Reconciliation — PRD vs. ARCHITECTURE-SPINE"
subject: OrderFlow Desktop
created: 2026-08-06
---

# Input Reconciliation: PRD → Architecture Spine

Checks that every PRD Functional Requirement (FR-1…FR-15) is governed by at least one AD/convention in the spine's Capability → Architecture Map, or legitimately deferred with a stated reason — and that no PRD NFR / testable "Consequences" bullet implies an architectural rule the spine's ADs silently drop.

## Coverage Table

| FR | PRD Requirement (short) | Governing AD(s) in spine | Verdict |
| --- | --- | --- | --- |
| FR-1 | Manage Customers (CRUD) | AD-1, AD-3, AD-9 | Covered |
| FR-2 | Manage Products (CRUD) | AD-1, AD-3, AD-9 | Covered |
| FR-3 | Create Order with line items | AD-2, AD-3, AD-9 | Covered |
| FR-4 | View Order list/detail | AD-2, AD-3, AD-9 | Covered |
| FR-5 | Decrement Inventory on confirm (atomic) | AD-2, AD-9, Validation convention | Covered |
| FR-6 | View current Inventory levels | AD-2, AD-9, Validation convention | Covered |
| FR-7 | Compute Order total via swappable Strategy | AD-1, AD-5 | Weakly covered — see Gap 3 |
| FR-8 | Progress OrderStatus through workflow | AD-4 | Covered |
| FR-9 | Raise notification on status change | AD-4 | Covered (surface choice correctly deferred) |
| FR-10 | Constructor-inject all BLL/DAL deps from composition root | AD-1, AD-2, AD-5, AD-9 (+AD-3 via its own binds) | Covered |
| FR-11 | Persist via Repository + UoW over EF Core | AD-1, AD-2, AD-5, AD-9 | Covered |
| FR-12 | Ship paired Before/After Exhibits | AD-8 | Covered |
| FR-13 | Maintain interview-topic-map.md | n/a — documentation artifact, correctly not architectural | Covered |
| FR-14 | Unit-test BLL against mocked DAL/BLL interfaces | AD-2, AD-5, AD-9 (mockability) | Covered |
| FR-15 | Select order-processing behavior via Factory | AD-7 | Covered — but see Gap 2 for its boundary with AD-4 |

Formally, every FR has at least one governing AD or an explicit non-architectural rationale (FR-13). No FR is silently dropped. The gaps below are about *rule strength/precision* and *unaddressed NFR/cross-AD boundaries*, not missing map entries.

## Gaps

1. **No AD enforces the PRD's async/UI-responsiveness NFR (§4.2).** The PRD states as a testable, feature-specific NFR: "Order confirmation (FR-5, FR-7, FR-9) runs asynchronously; the UI thread remains responsive and does not freeze during the database round trip." Nothing in the spine's ADs establishes this as an invariant — AD-2 mandates `CreateDbContextAsync()` per operation (async DAL access exists) but no AD says BLL service methods must be `Task`-returning end-to-end, that Presenters must `await` rather than block (`.Result`/`.Wait()`), or how UI updates get marshaled back onto the WinForms UI thread after an awaited call. Two independently-built Presenters/services could diverge exactly the way the NFR warns against — one doing sync-over-async and hanging the form during a live demo (UJ-2's climax), the other doing it correctly — with no AD to catch the drift.

2. **AD-4 and AD-7 don't state who owns status transitions when a Rush `IOrderProcessor` runs.** FR-15's own `[ASSUMPTION]` floats Rush applying "expedited status-transition/notification handling (e.g., compressed workflow or higher-priority Notification)." But AD-4 is explicit and absolute: `OrderStatusService` is "the sole owner of the allowed-transition table and the only caller of `INotifier.Notify(...)`... No other class evaluates transition validity or calls `INotifier`." AD-7 gives `IOrderProcessor` implementations control over "order-processing behavior" per `OrderType` but never states whether that behavior must delegate status changes/notifications through `OrderStatusService` or is barred from touching them directly. As written, a developer implementing `RushOrderProcessor` per FR-15's own assumption could plausibly transition status or fire notifications itself — directly contradicting AD-4. This is precisely the kind of cross-cutting boundary the spine exists to fix, and it's currently unfixed.

3. **FR-7's Strategy pattern gets no dedicated resolution/registration AD, unlike its sibling FR-15's Factory.** The map cites only AD-1 (generic layering) and AD-5 (generic Scoped-by-default DI lifetimes) for FR-7 — both apply to virtually every FR and say nothing about how `IPricingStrategy` is selected or swapped. Contrast this with FR-15, which got a dedicated AD-7 pinning the exact mechanism (`AddKeyedScoped<IOrderProcessor>`, single resolution path via a factory, no direct caller resolution). FR-7's own testable consequence — "changing the active strategy implementation changes computed totals without any code change to Order-entry or DAL code" — depends on exactly this kind of registration discipline, and FR-14 requires the test suite to swap implementations. Without an equivalent rule, one developer could constructor-inject `IPricingStrategy` properly while another inlines `new VolumeDiscountStrategy()` inside a service, and nothing in the spine forbids it.

4. **Inventory-availability validation has no stated single owner, unlike status transitions.** FR-3's consequence ("each OrderItem's requested quantity is validated against available Inventory before the Order can be confirmed") and FR-5's consequence ("no partial decrement occurs") both depend on one consistent validation path. AD-4 explicitly prevents "transition-validity logic duplicated or drifting between UI and BLL" for status — but the FR-5/FR-6 map row (AD-2, AD-9, Validation convention) has no equivalent ownership rule for inventory-availability checks. The generic "Validation & error handling" convention says failures return `Result<T>`, but not which class (`OrderService`? `InventoryService`?) is the sole checker — leaving room for the same duplication/drift risk AD-4 was written to prevent, just in the adjacent capability.

**File written:** `_bmad-output/planning-artifacts/architecture/architecture-BMAD YT TUTORIAL-2026-08-06/reconcile-prd.md`
