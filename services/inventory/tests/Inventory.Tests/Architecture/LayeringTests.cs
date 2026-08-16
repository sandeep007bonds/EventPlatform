namespace Inventory.Tests.Architecture;

public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(GeneralAdmissionAllocation).Assembly)
            .That()
            .ResideInNamespace("Inventory.Domain")
            .ShouldNot()
            .HaveDependencyOn("Inventory.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(GeneralAdmissionAllocation).Assembly)
            .That()
            .ResideInNamespace("Inventory.Domain")
            .ShouldNot()
            .HaveDependencyOn("Inventory.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(HoldService).Assembly)
            .That()
            .ResideInNamespace("Inventory.Application")
            .ShouldNot()
            .HaveDependencyOn("Inventory.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
