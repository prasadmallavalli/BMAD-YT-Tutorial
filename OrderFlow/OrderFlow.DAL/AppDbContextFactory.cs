using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderFlow.DAL;

// Design-time only — invoked exclusively by the `dotnet ef` CLI to construct AppDbContext
// for migrations tooling. Never referenced by the runtime DI graph (Program.cs owns that,
// per AD-1/AD-9's composition-root exception). See Story 1.2 Dev Notes.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=OrderFlow;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new AppDbContext(options);
    }
}
