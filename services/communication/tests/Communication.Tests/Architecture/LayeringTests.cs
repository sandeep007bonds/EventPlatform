namespace Communication.Tests.Architecture;

public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(DeliveryLogEntry).Assembly)
            .That()
            .ResideInNamespace("Communication.Domain")
            .ShouldNot()
            .HaveDependencyOn("Communication.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(DeliveryLogEntry).Assembly)
            .That()
            .ResideInNamespace("Communication.Domain")
            .ShouldNot()
            .HaveDependencyOn("Communication.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(NotificationSendService).Assembly)
            .That()
            .ResideInNamespace("Communication.Application")
            .ShouldNot()
            .HaveDependencyOn("Communication.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
