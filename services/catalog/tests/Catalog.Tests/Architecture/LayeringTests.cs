namespace Catalog.Tests.Architecture;

public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Event).Assembly)
            .That()
            .ResideInNamespace("Catalog.Domain")
            .ShouldNot()
            .HaveDependencyOn("Catalog.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(Event).Assembly)
            .That()
            .ResideInNamespace("Catalog.Domain")
            .ShouldNot()
            .HaveDependencyOn("Catalog.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CreateEventCommand).Assembly)
            .That()
            .ResideInNamespace("Catalog.Application")
            .ShouldNot()
            .HaveDependencyOn("Catalog.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
