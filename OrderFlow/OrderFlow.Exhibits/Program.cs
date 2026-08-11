using OrderFlow.Exhibits.After.Dip;
using OrderFlow.Exhibits.After.Ocp;
using OrderFlow.Exhibits.After.Srp;
using OrderFlow.Exhibits.Before.Dip;
using OrderFlow.Exhibits.Before.Ocp;
using OrderFlow.Exhibits.Before.Srp;

// Standalone dispatch entry point — deliberately the opposite of OrderFlow.Presentation/
// Program.cs's DI composition root. No DI container, no ServiceCollection: each exhibit is
// a plain static Run() picked by a command-line argument, so running one never executes the
// other's code path and never touches the main app (AD-8). Stories 4.2/4.3 add more cases here
// for the OCP/DIP exhibit pairs; they don't invent their own entry points.
var mode = args.Length > 0 ? args[0] : null;

switch (mode)
{
    case "before-srp":
        BeforeSrpRunner.Run();
        break;
    case "after-srp":
        AfterSrpRunner.Run();
        break;
    case "before-ocp":
        BeforeOcpRunner.Run();
        break;
    case "after-ocp":
        AfterOcpRunner.Run();
        break;
    case "before-dip":
        BeforeDipRunner.Run();
        break;
    case "after-dip":
        AfterDipRunner.Run();
        break;
    default:
        Console.WriteLine("Usage: dotnet run --project OrderFlow.Exhibits -- <exhibit>");
        Console.WriteLine("Available exhibits:");
        Console.WriteLine("  before-srp   SRP violation (Story 4.1)");
        Console.WriteLine("  after-srp    SRP refactor (Story 4.1)");
        Console.WriteLine("  before-ocp   OCP violation (Story 4.2)");
        Console.WriteLine("  after-ocp    OCP refactor (Story 4.2)");
        Console.WriteLine("  before-dip   DIP violation (Story 4.3)");
        Console.WriteLine("  after-dip    DIP refactor (Story 4.3)");
        break;
}
