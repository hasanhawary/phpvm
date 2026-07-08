using System.Reflection;
using Pvm.Core.Models;
using Pvm.Core.Ports;
using Xunit;

namespace Pvm.Architecture.Tests;

public class ArchitectureTests
{
    private static readonly Assembly s_coreAssembly = typeof(PhpVersion).Assembly;

    [Fact]
    public void Core_ShouldNotReferenceOtherProjects()
    {
        var referencedAssemblies = s_coreAssembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .ToList();

        Assert.DoesNotContain("Pvm.Application", referencedAssemblies);
        Assert.DoesNotContain("Pvm.Infrastructure", referencedAssemblies);
        Assert.DoesNotContain("Pvm.Cli", referencedAssemblies);
    }

    [Fact]
    public void CorePorts_ShouldStartWithI()
    {
        var portTypes = s_coreAssembly.GetTypes()
            .Where(t => t.Namespace == "Pvm.Core.Ports" && t.IsInterface);

        foreach (var type in portTypes)
        {
            Assert.StartsWith("I", type.Name);
        }
    }

    [Fact]
    public void CoreEnums_ShouldResideInEnumsNamespace()
    {
        var enumTypes = s_coreAssembly.GetTypes()
            .Where(t => t.IsEnum && t.IsPublic);

        foreach (var type in enumTypes)
        {
            Assert.Equal("Pvm.Core.Enums", type.Namespace);
        }
    }

    [Fact]
    public void CoreModels_ShouldResideInModelsNamespace()
    {
        var modelTypes = s_coreAssembly.GetTypes()
            .Where(t => t.Namespace == "Pvm.Core.Models" && t.IsPublic);

        Assert.NotEmpty(modelTypes);
    }
}
