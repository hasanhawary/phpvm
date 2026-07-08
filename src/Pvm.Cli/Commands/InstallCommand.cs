using System.ComponentModel;
using Pvm.Application.Services;
using Pvm.Cli.Rendering;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class InstallCommandSettings : CommandSettings
{
    [CommandArgument(0, "<version>")]
    [Description("The PHP version to install (e.g., '8.4', '8.4.23', '8.3').")]
    public string Version { get; init; } = string.Empty;

    [CommandOption("-a|--arch")]
    [Description("Target architecture (x64, x86, arm64). Defaults to x64.")]
    public string? Architecture { get; init; }

    [CommandOption("--ts")]
    [Description("Install Thread Safe (TS) build.")]
    public bool ThreadSafe { get; init; }

    [CommandOption("--nts")]
    [Description("Install Non-Thread Safe (NTS) build (recommended for CLI/IIS/FastCGI).")]
    public bool NonThreadSafe { get; init; }

    [CommandOption("-f|--force")]
    [Description("Force re-installation if already installed.")]
    public bool Force { get; init; }
}

public sealed class InstallCommand : AsyncCommand<InstallCommandSettings>
{
    private readonly InstallService _installService;

    public InstallCommand(InstallService installService)
    {
        _installService = installService ?? throw new ArgumentNullException(nameof(installService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, InstallCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Version))
        {
            Theme.PrintError("Please specify a PHP version to install.");
            return 1;
        }

        if (settings.ThreadSafe && settings.NonThreadSafe)
        {
            Theme.PrintError("Cannot specify both --ts and --nts flags.");
            return 1;
        }

        Architecture? arch = null;
        if (!string.IsNullOrWhiteSpace(settings.Architecture))
        {
            if (settings.Architecture.Equals("x64", StringComparison.OrdinalIgnoreCase)) arch = Core.Enums.Architecture.X64;
            else if (settings.Architecture.Equals("x86", StringComparison.OrdinalIgnoreCase) || settings.Architecture.Equals("x32", StringComparison.OrdinalIgnoreCase)) arch = Core.Enums.Architecture.X86;
            else if (settings.Architecture.Equals("arm64", StringComparison.OrdinalIgnoreCase)) arch = Core.Enums.Architecture.Arm64;
            else
            {
                Theme.PrintError($"Unsupported architecture '{settings.Architecture}'. Valid options: x64, x86, arm64.");
                return 1;
            }
        }

        ThreadSafety? ts = null;
        if (settings.ThreadSafe) ts = ThreadSafety.Ts;
        else if (settings.NonThreadSafe) ts = ThreadSafety.Nts;

        var specifier = VersionSpecifier.Parse(settings.Version);
        Theme.PrintInfo($"Resolving PHP {settings.Version} from official Windows mirrors...");

        return await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async progressContext =>
            {
                var downloadTask = progressContext.AddTask("[cyan]Downloading PHP archive...[/]");
                var extractTask = progressContext.AddTask("[green]Extracting & configuring...[/]");
                extractTask.StopTask();

                var downloadProgress = new Progress<double>(pct =>
                {
                    downloadTask.Value = pct;
                });

                var extractProgress = new Progress<double>(pct =>
                {
                    if (downloadTask.Value < 100)
                    {
                        downloadTask.Value = 100;
                        downloadTask.StopTask();
                        extractTask.StartTask();
                    }
                    extractTask.Value = pct;
                });

                var result = await _installService.InstallAsync(
                    specifier,
                    arch,
                    ts,
                    settings.Force,
                    downloadProgress,
                    extractProgress);

                downloadTask.StopTask();
                extractTask.StopTask();

                if (result.IsFailure)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] {result.Error}");
                    return 1;
                }

                var inst = result.Value;
                Theme.PrintSuccess($"Successfully installed PHP {inst.Version} ({inst.Architecture}, {inst.ThreadSafety}) to {inst.Path}!");
                Theme.PrintInfo($"Run 'pvm use {inst.Version}' to switch to this version.");
                return 0;
            });
    }
}
