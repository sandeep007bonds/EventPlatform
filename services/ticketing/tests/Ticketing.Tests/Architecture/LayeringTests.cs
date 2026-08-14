namespace Ticketing.Tests.Architecture;

public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Ticket).Assembly)
            .That()
            .ResideInNamespace("Ticketing.Domain")
            .ShouldNot()
            .HaveDependencyOn("Ticketing.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(Ticket).Assembly)
            .That()
            .ResideInNamespace("Ticketing.Domain")
            .ShouldNot()
            .HaveDependencyOn("Ticketing.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(TicketIssuingService).Assembly)
            .That()
            .ResideInNamespace("Ticketing.Application")
            .ShouldNot()
            .HaveDependencyOn("Ticketing.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
