namespace OrderFlow.DAL;

// Plain wrapper — no EF Core types in its public shape — so OrderFlow.BLL can catch it
// without depending on EF Core directly (AD-9's "Concurrency-exception exception").
public class ConcurrencyConflictException : Exception
{
    // Single source of truth for the friendly message — both AppDbContext.SaveChanges and
    // UnitOfWork.SaveChangesAsync construct this the same way; BLL callers (e.g.
    // ProductService) should surface ex.Message rather than hardcoding their own copy.
    public const string DefaultMessage = "The record was modified by another user. Please reload and try again.";

    public ConcurrencyConflictException(Exception innerException)
        : base(DefaultMessage, innerException)
    {
    }
}
