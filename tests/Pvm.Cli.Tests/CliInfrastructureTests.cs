using Microsoft.Extensions.DependencyInjection;
using Pvm.Cli.Infrastructure;
using Xunit;

namespace Pvm.Cli.Tests;

public interface ITestService { string GetMessage(); }
public class TestService : ITestService { public string GetMessage() => "Hello PVM"; }

public class CliInfrastructureTests
{
    [Fact]
    public void TypeRegistrarAndResolver_ShouldResolveRegisteredServices()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        registrar.Register(typeof(ITestService), typeof(TestService));
        using var resolver = (TypeResolver)registrar.Build();

        var resolved = resolver.Resolve(typeof(ITestService)) as ITestService;

        Assert.NotNull(resolved);
        Assert.Equal("Hello PVM", resolved.GetMessage());
    }
}
