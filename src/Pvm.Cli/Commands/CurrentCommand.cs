using Pvm.Cli.Rendering;
using Pvm.Core.Ports;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class CurrentCommandSettings : CommandSettings
{
}

public sealed class CurrentCommand : AsyncCommand<CurrentCommandSettings>
{
    private readonly IInstallationScanner _scanner;
    private readonly IPhpProcess _phpProcess;

    public CurrentCommand(IInstallationScanner scanner, IPhpProcess phpProcess)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _phpProcess = phpProcess ?? throw new ArgumentNullException(nameof(phpProcess));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CurrentCommandSettings settings)
    {
        var activeVersion = await _scanner.GetActiveVersionAsync();
        if (activeVersion is null)
        {
            Theme.PrintWarning("No PHP version is currently active.");
            Theme.PrintInfo("Run 'pvm use <version>' to set an active version.");
            return 1;
        }

        var activeInstall = await _scanner.GetInstallationAsync(activeVersion);
        if (activeInstall is null)
        {
            Theme.PrintError($"Active version is set to {activeVersion}, but the installation directory was not found.");
            return 1;
        }

        var phpExe = Path.Combine(activeInstall.Path, "php.exe");
        var runtimeInfo = await _phpProcess.GetConfigurationAsync(phpExe);

        Theme.PrintSuccess($"Active PHP Version: {activeVersion} ({activeInstall.Architecture}, {activeInstall.ThreadSafety})");
        Theme.PrintInfo($"Location: {activeInstall.Path}");
        if (runtimeInfo.IniPath is not null)
        {
            Theme.PrintInfo($"Configuration File: {runtimeInfo.IniPath}");
        }
        else
        {
            Theme.PrintWarning("Configuration File: (none loaded)");
        }

        return 0;
    }
}
