# Interview Topic Map

Every named interview topic from the OrderFlow Desktop PRD and Architecture Spine, mapped to
the specific class/file that demonstrates it in the real app, and — where one exists — the
standalone Before/After exhibit pair (`OrderFlow.Exhibits`) that isolates the pattern for
walkthrough purposes.

**Maintenance (FR-13):** this is the single file that "ships and maintains" this mapping. When
a new interview topic is introduced, add a row here — do not create a second map elsewhere.

| Topic | Real App | Exhibit Pair | Architecture Reference |
| --- | --- | --- | --- |
| DI & Composition Root | `OrderFlow.Presentation/Program.cs` (`ConfigureServices`) | `OrderFlow.Exhibits/{Before,After}/Dip` (Story 4.3) | AD-1, AD-2, AD-5 |
| Repository + Unit of Work | `OrderFlow.DAL/IUnitOfWork.cs`, `UnitOfWork.cs` (e.g. `IOrderRepository.cs`/`OrderRepository.cs`) | N/A — real app only | AD-9 |
| Strategy Pattern | `OrderFlow.BLL/IPricingStrategy.cs`, `StandardPricingStrategy.cs` | `OrderFlow.Exhibits/{Before,After}/Ocp` (Story 4.2) | AD-11 |
| Factory Pattern / keyed DI | `OrderFlow.BLL/OrderProcessorFactory.cs`, `IOrderProcessor.cs`, `StandardOrderProcessor.cs`, `RushOrderProcessor.cs` | N/A — real app only | AD-7 |
| SOLID: SRP | No single dedicated class — a general layering discipline (e.g. `OrderFlow.BLL/CustomerService.cs`, `OrderStatusService.cs`, each owning one responsibility) | `OrderFlow.Exhibits/{Before,After}/Srp` (Story 4.1) | — |
| SOLID: OCP | `OrderFlow.BLL/IPricingStrategy.cs`, `StandardPricingStrategy.cs` | `OrderFlow.Exhibits/{Before,After}/Ocp` (Story 4.2) | AD-11 |
| SOLID: DIP | `OrderFlow.Presentation/Program.cs` (`ConfigureServices` — every constructor-injected interface) | `OrderFlow.Exhibits/{Before,After}/Dip` (Story 4.3) | AD-1 |
| Optimistic Concurrency | `OrderFlow.Domain/Order.cs`, `Inventory.cs` (`RowVersion`); `OrderFlow.DAL/UnitOfWork.cs` (`DbUpdateConcurrencyException` catch), `ConcurrencyConflictException.cs` | N/A — real app only | AD-10 |
| Presenter/MVP | `OrderFlow.Presentation/OrderDetailPresenter.cs`, `IOrderDetailView.cs`, `OrderDetailForm.cs` | N/A — real app only | AD-3 |
| `Result<T>` error handling | `OrderFlow.BLL/Result.cs` | N/A — real app only | Consistency Conventions |
| async-all-the-way | `OrderFlow.Presentation/OrderCreatePresenter.cs`, `OrderDetailPresenter.cs` (`async Task` methods, no `.Result`/`.Wait()`) | N/A — real app only | NFR-1, AD-3 |

**On the "N/A — real app only" rows:** Epic 4 built exactly three exhibit pairs (SRP, OCP,
DIP). The remaining eight topics are demonstrated only in the real app — these cells are
intentionally honest rather than pointing at a fabricated exhibit.
