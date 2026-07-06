using System.ComponentModel;
using Pvm.Application.Services;
using Pvm.Cli.Rendering;
using Pvm.Core.Models;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class UninstallCommandSettings : CommandSettings
{
    [CommandArgument(0, "<version>")]
    [Description("The exact PHP version to uninstall (e.g., '8.4.23').")]
    public string Version { get; init; } = string.Empty;
}

public sealed class UninstallCommand : AsyncCommand<UninstallCommandSettings>
{
    private readonly UninstallService _uninstallService;

    public UninstallCommand(UninstallService uninstallService)
    {
        _uninstallService = uninstallService ?? throw new ArgumentNullException(nameof(uninstallService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, UninstallCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Version))
        {
            Theme.PrintError("Please specify a PHP version to uninstall.");
            return 1;
        }

        Theme.PrintInfo($"Uninstalling PHP {settings.Version}...");
        var specifier = VersionSpecifier.Parse(settings.Version);
        var result = await _uninstallService.UninstallAsync(specifier);

        if (result.IsFailure)
        {
            Theme.PrintError(result.Error);
            return 1;
        }

        Theme.PrintSuccess($"Successfully uninstalled PHP {settings.Version}.");
        return 0;
    }
}
