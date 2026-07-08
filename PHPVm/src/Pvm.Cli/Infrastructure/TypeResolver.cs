using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Pvm.Cli.Infrastructure;

public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public object? Resolve(Type? type)
    {
        if (type is null) return null;
        var instance = _provider.GetService(type);
        if (instance is not null) return instance;
        try
        {
            return ActivatorUtilities.CreateInstance(_provider, type);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
