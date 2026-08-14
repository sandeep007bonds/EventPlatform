namespace Payments.Tests.Architecture;

/// <summary>Enforces the Clean Architecture dependency direction (root CLAUDE.md rule 5).</summary>
public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Payment).Assembly)
            .That()
            .ResideInNamespace("Payments.Domain")
            .ShouldNot()
            .HaveDependencyOn("Payments.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(Payment).Assembly)
            .That()
            .ResideInNamespace("Payments.Domain")
            .ShouldNot()
            .HaveDependencyOn("Payments.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(PaymentSyncService).Assembly)
            .That()
            .ResideInNamespace("Payments.Application")
            .ShouldNot()
            .HaveDependencyOn("Payments.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    /// <summary>
    /// Stripe's SDK stays behind the gateway adapter. A Stripe type leaking into Application or
    /// Domain would make the provider un-swappable and drag PCI surface into the core.
    /// </summary>
    [Fact]
    public void OnlyInfrastructure_DependsOn_Stripe()
    {
        var result = Types.InAssembly(typeof(PaymentSyncService).Assembly)
            .That()
            .ResideInNamespace("Payments.Application")
            .ShouldNot()
            .HaveDependencyOn("Stripe")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
