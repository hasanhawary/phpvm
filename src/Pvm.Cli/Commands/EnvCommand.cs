using System.ComponentModel;
using Pvm.Application.Services;
using Pvm.Cli.Rendering;
using Pvm.Core.Models;
using Pvm.Core.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class EnvCommandSettings : CommandSettings
{
    [CommandArgument(0, "[version]")]
    [Description("Optional PHP version to generate session environment script for.")]
    public string? Version { get; init; }

    [CommandOption("--ps")]
    [Description("Generate PowerShell environment script (default).")]
    public bool PowerShell { get; init; }

    [CommandOption("--cmd")]
    [Description("Generate Command Prompt (cmd.exe) environment script.")]
    public bool CommandPrompt { get; init; }

    [CommandOption("-c|--check")]
    [Description("Check PATH status, junction presence, and detect PHP conflicts (e.g., XAMPP, WAMP).")]
    public bool Check { get; init; }

    [CommandOption("--clean")]
    [Description("Remove duplicate entries from User PATH environment variable.")]
    public bool Clean { get; init; }
}

public sealed class EnvCommand : AsyncCommand<EnvCommandSettings>
{
    private readonly PathService _pathService;
    private readonly IInstallationScanner _scanner;

    public EnvCommand(PathService pathService, IInstallationScanner scanner)
    {
        _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, EnvCommandSettings settings)
    {
        if (settings.Clean)
        {
            Theme.PrintInfo("Cleaning duplicate entries from User PATH...");
            var cleanResult = await _pathService.CleanDuplicatesAsync();
            if (cleanResult.IsFailure)
            {
                Theme.PrintError(cleanResult.Error);
                return 1;
            }
            Theme.PrintSuccess("Successfully removed duplicate entries from User PATH!");
            return 0;
        }

        if (settings.Check || (string.IsNullOrWhiteSpace(settings.Version) && !settings.PowerShell && !settings.CommandPrompt))
        {
            return await RunCheckAsync();
        }

        PhpInstallation? targetInst = null;
        if (!string.IsNullOrWhiteSpace(settings.Version))
        {
            var specifier = VersionSpecifier.Parse(settings.Version);
            var all = await _scanner.ScanInstalledAsync();
            targetInst = all
                .Where(x => specifier.Matches(x.Version))
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            if (targetInst is null)
            {
                Theme.PrintError($"PHP version matching '{settings.Version}' is not installed.");
                return 1;
            }
        }
        else
        {
            var activeVer = await _scanner.GetActiveVersionAsync();
            if (activeVer is not null)
            {
                targetInst = await _scanner.GetInstallationAsync(activeVer);
            }

            if (targetInst is null)
            {
                Theme.PrintError("No active PHP version set and no version specified.");
                return 1;
            }
        }

        var path = targetInst.Path;
        if (settings.CommandPrompt)
        {
            AnsiConsole.WriteLine($"set PATH={path};%PATH%");
            AnsiConsole.WriteLine($"set PHP_HOME={path}");
        }
        else
        {
            // Default to PowerShell
            AnsiConsole.WriteLine($"$env:PATH = \"{path};\" + $env:PATH");
            AnsiConsole.WriteLine($"$env:PHP_HOME = \"{path}\"");
        }

        return 0;
    }

    private async Task<int> RunCheckAsync()
    {
        var status = await _pathService.GetPathStatusAsync();

        var table = new Table().Border(TableBorder.Rounded).Title("[bold cyan]PVM Environment & PATH Status[/]");
        table.AddColumn("Check Item");
        table.AddColumn("Status");
        table.AddColumn("Details");

        var jStatus = status.IsJunctionInPath ? "[green]OK[/]" : "[yellow]Missing[/]";
        var jDetails = status.IsJunctionInPath
            ? $"Junction '{status.CurrentJunction}' is present in PATH."
            : $"Junction '{status.CurrentJunction}' is NOT in PATH. Run 'pvm use <version>' to auto-add.";
        table.AddRow("PVM Junction in PATH", jStatus, jDetails);

        var dupStatus = status.Duplicates.Count == 0 ? "[green]OK[/]" : "[yellow]Warning[/]";
        var dupDetails = status.Duplicates.Count == 0
            ? "No duplicate entries in User PATH."
            : $"{status.Duplicates.Count} duplicate(s) found. Run 'pvm env --clean' to fix.";
        table.AddRow("PATH Duplicates", dupStatus, dupDetails);

        var confStatus = status.ConflictingPhpEntries.Count == 0 ? "[green]OK[/]" : "[yellow]Conflict Detected[/]";
        var confDetails = status.ConflictingPhpEntries.Count == 0
            ? "No external PHP installations found in PATH."
            : string.Join("\n", status.ConflictingPhpEntries.Select(c => $"Found external PHP at: [yellow]{c}[/]"));
        table.AddRow("External PHP Conflicts", confStatus, confDetails);

        AnsiConsole.Write(table);

        if (!status.IsJunctionInPath || status.Duplicates.Count > 0 || status.ConflictingPhpEntries.Count > 0)
        {
            AnsiConsole.WriteLine();
            Theme.PrintInfo("Recommendations:");
            if (!status.IsJunctionInPath)
                AnsiConsole.MarkupLine(" • Run [bold cyan]pvm use <version>[/] to automatically register the PVM junction in your User PATH.");
            if (status.Duplicates.Count > 0)
                AnsiConsole.MarkupLine(" • Run [bold cyan]pvm env --clean[/] to remove duplicate directories from your User PATH.");
            if (status.ConflictingPhpEntries.Count > 0)
                AnsiConsole.MarkupLine(" • Remove external PHP tools (like XAMPP, WAMP, or Laragon) from your Windows PATH to prevent version shadowing.");
        }

        return 0;
    }
}
