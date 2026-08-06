namespace Queue.Tests.Architecture;

public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(QueueSettings).Assembly)
            .That()
            .ResideInNamespace("Queue.Domain")
            .ShouldNot()
            .HaveDependencyOn("Queue.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(QueueSettings).Assembly)
            .That()
            .ResideInNamespace("Queue.Domain")
            .ShouldNot()
            .HaveDependencyOn("Queue.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(JoinQueueHandler).Assembly)
            .That()
            .ResideInNamespace("Queue.Application")
            .ShouldNot()
            .HaveDependencyOn("Queue.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
