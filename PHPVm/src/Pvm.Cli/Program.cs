using Microsoft.Extensions.DependencyInjection;
using Pvm.Application.Services;
using Pvm.Cli.Commands;
using Pvm.Cli.Infrastructure;
using Pvm.Cli.Rendering;
using Pvm.Core.Ports;
using Pvm.Infrastructure.Configuration;
using Pvm.Infrastructure.Diagnostics;
using Pvm.Infrastructure.FileSystem;
using Pvm.Infrastructure.Network;
using Pvm.Infrastructure.Platform;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        var registrar = new TypeRegistrar(services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("pvm");
            config.ValidateExamples();

            config.AddCommand<ListCommand>("list")
                .WithAlias("ls")
                .WithDescription("List installed or remote PHP versions.");

            config.AddCommand<CurrentCommand>("current")
                .WithDescription("Display the currently active PHP version and runtime info.");

            config.AddCommand<UseCommand>("use")
                .WithDescription("Switch the active PHP version and register junction in PATH.");

            config.AddCommand<InstallCommand>("install")
                .WithDescription("Download and install a PHP version from official mirrors.");

            config.AddCommand<UninstallCommand>("uninstall")
                .WithDescription("Uninstall an installed PHP version.");

            config.AddCommand<EnvCommand>("env")
                .WithDescription("Check PATH status, clean duplicates, or generate session environment scripts.");

            config.AddCommand<IniCommand>("ini")
                .WithDescription("List, enable/disable extensions, edit directives, or open php.ini.");

            config.AddCommand<AliasCommand>("alias")
                .WithDescription("Create, remove, or list PHP version aliases.");

            config.AddCommand<DoctorCommand>("doctor")
                .WithDescription("Run automated system health checks and diagnostics.");
        });

        try
        {
            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            Theme.PrintError($"An unexpected error occurred: {ex.Message}");
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IBuildSource>(_ => new OfficialPhpBuildSource(new HttpClient()));
        services.AddSingleton<IConfigStore, JsonConfigStore>();
        services.AddSingleton<IJunctionManager, WindowsJunctionManager>();
        services.AddSingleton<IPhpProcess, PhpProcessRunner>();
        services.AddSingleton<IInstallationScanner, InstallationDirectoryScanner>();
        services.AddSingleton<IArchiveExtractor, ArchiveExtractor>();
        services.AddSingleton<IEnvironmentNotifier, WindowsEnvironmentNotifier>();
        services.AddSingleton<IPathManager, WindowsPathManager>();
        services.AddSingleton<IIniManager, PhpIniManager>();
        services.AddSingleton<IAliasManager, JsonAliasManager>();
        services.AddSingleton<IDoctorCheck, PvmDirectoryCheck>();
        services.AddSingleton<IDoctorCheck, JunctionPathCheck>();
        services.AddSingleton<IDoctorCheck, PathHygieneCheck>();
        services.AddSingleton<IDoctorCheck, ExternalPhpConflictCheck>();
        services.AddSingleton<IDoctorCheck, VCRuntimeCheck>();
        services.AddSingleton<PathService>();
        services.AddSingleton<IniService>();
        services.AddSingleton<AliasService>();
        services.AddSingleton<DoctorService>();
        services.AddSingleton<SwitchService>();
        services.AddSingleton<InstallService>();
        services.AddSingleton<UninstallService>();
    }
}

