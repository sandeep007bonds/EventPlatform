namespace Identity.Tests.Architecture;

public sealed class LayeringTests
{
    [Fact]
    public void Domain_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(PhoneVerification).Assembly)
            .That()
            .ResideInNamespace("Identity.Domain")
            .ShouldNot()
            .HaveDependencyOn("Identity.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_DoesNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(PhoneVerification).Assembly)
            .That()
            .ResideInNamespace("Identity.Domain")
            .ShouldNot()
            .HaveDependencyOn("Identity.Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(RequestOtpHandler).Assembly)
            .That()
            .ResideInNamespace("Identity.Application")
            .ShouldNot()
            .HaveDependencyOn("Identity.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
