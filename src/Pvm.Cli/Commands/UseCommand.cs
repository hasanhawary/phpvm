using System.ComponentModel;
using Pvm.Application.Services;
using Pvm.Cli.Rendering;
using Pvm.Core.Models;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class UseCommandSettings : CommandSettings
{
    [CommandArgument(0, "<version>")]
    [Description("The PHP version or alias to switch to (e.g., '8.4', '8.4.23', 'default').")]
    public string Version { get; init; } = string.Empty;
}

public sealed class UseCommand : AsyncCommand<UseCommandSettings>
{
    private readonly SwitchService _switchService;

    public UseCommand(SwitchService switchService)
    {
        _switchService = switchService ?? throw new ArgumentNullException(nameof(switchService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, UseCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Version))
        {
            Theme.PrintError("Please specify a PHP version to use.");
            return 1;
        }

        Theme.PrintInfo($"Switching to PHP {settings.Version}...");
        var specifier = VersionSpecifier.Parse(settings.Version);
        var result = await _switchService.SwitchAsync(specifier);

        if (result.IsFailure)
        {
            Theme.PrintError(result.Error);
            return 1;
        }

        var inst = result.Value;
        Theme.PrintSuccess($"Switched to PHP {inst.Version} ({inst.Architecture}, {inst.ThreadSafety})");
        return 0;
    }
}
