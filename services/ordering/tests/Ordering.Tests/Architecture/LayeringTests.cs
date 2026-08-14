namespace Ordering.Tests.Architecture;

/// <summary>Enforces the Clean Architecture dependency direction (root CLAUDE.md rule 5).</summary>
public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Order).Assembly)
            .That()
            .ResideInNamespace("Ordering.Domain")
            .ShouldNot()
            .HaveDependencyOn("Ordering.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(Order).Assembly)
            .That()
            .ResideInNamespace("Ordering.Domain")
            .ShouldNot()
            .HaveDependencyOn("Ordering.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(HoldSnapshot).Assembly)
            .That()
            .ResideInNamespace("Ordering.Application")
            .ShouldNot()
            .HaveDependencyOn("Ordering.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// The saga orchestrates through ports and activities only. A direct Infrastructure reference
    /// here would put I/O one careless edit away from running inside the orchestrator itself.
    /// </summary>
    [Fact]
    public void Workflow_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(CheckoutWorkflow).Assembly)
            .That()
            .ResideInNamespace("Ordering.Workflow")
            .ShouldNot()
            .HaveDependencyOn("Ordering.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
