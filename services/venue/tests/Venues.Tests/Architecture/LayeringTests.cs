namespace Venues.Tests.Architecture;

public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Venue).Assembly)
            .That()
            .ResideInNamespace("Venues.Domain")
            .ShouldNot()
            .HaveDependencyOn("Venues.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(Venue).Assembly)
            .That()
            .ResideInNamespace("Venues.Domain")
            .ShouldNot()
            .HaveDependencyOn("Venues.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Venues.Application.DependencyInjection).Assembly)
            .That()
            .ResideInNamespace("Venues.Application")
            .ShouldNot()
            .HaveDependencyOn("Venues.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
