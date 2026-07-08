using System.ComponentModel;
using Pvm.Application.Services;
using Pvm.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class SelfUpdateCommandSettings : CommandSettings
{
    [CommandOption("--check|-c")]
    [Description("Only check for available updates without downloading.")]
    public bool CheckOnly { get; init; }
}

public sealed class SelfUpdateCommand : AsyncCommand<SelfUpdateCommandSettings>
{
    private readonly SelfUpdateService _updateService;

    public SelfUpdateCommand(SelfUpdateService updateService)
    {
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, SelfUpdateCommandSettings settings)
    {
        Theme.PrintInfo("Checking official GitHub repository for PVM updates...");

        var checkResult = await _updateService.CheckForUpdateAsync();
        if (checkResult.IsFailure)
        {
            Theme.PrintError($"Update check failed: {checkResult.Error}");
            return 1;
        }

        var info = checkResult.Value;
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]Current Version:[/] [bold]{info.CurrentVersion}[/]");
        AnsiConsole.MarkupLine($"[grey]Latest Version:[/]  [bold]{info.LatestVersion}[/]");
        AnsiConsole.WriteLine();

        if (!info.IsUpdateAvailable)
        {
            Theme.PrintSuccess("You are already running the latest version of PVM!");
            return 0;
        }

        Theme.PrintSuccess($"A new version ([bold cyan]v{info.LatestVersion}[/]) is available!");
        if (!string.IsNullOrWhiteSpace(info.ReleaseNotes))
        {
            var panel = new Panel(info.ReleaseNotes)
                .Border(BoxBorder.Rounded)
                .Header($"[bold yellow]Release Notes (v{info.LatestVersion})[/]");
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        if (settings.CheckOnly)
        {
            Theme.PrintInfo("Run [yellow]pvm self-update[/] to download and apply this update.");
            return 0;
        }

        AnsiConsole.MarkupLine($"[cyan]Downloading and installing PVM v{info.LatestVersion}...[/]");
        var applyResult = await _updateService.ApplyUpdateAsync(info);
        if (applyResult.IsFailure)
        {
            Theme.PrintError(applyResult.Error);
            return 1;
        }

        Theme.PrintSuccess($"PVM successfully updated to v{info.LatestVersion}! Run [bold yellow]pvm --version[/] to verify.");
        return 0;
    }
}
