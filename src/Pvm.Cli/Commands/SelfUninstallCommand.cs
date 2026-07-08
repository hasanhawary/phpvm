using System.ComponentModel;
using System.Diagnostics;
using Pvm.Cli.Rendering;
using Pvm.Core.Ports;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class SelfUninstallCommandSettings : CommandSettings
{
    [CommandOption("-y|--yes")]
    [Description("Skip confirmation prompt and immediately uninstall PVM.")]
    public bool Yes { get; init; }
}

public sealed class SelfUninstallCommand : AsyncCommand<SelfUninstallCommandSettings>
{
    private readonly IPathManager _pathManager;
    private readonly IInstallationScanner _scanner;

    public SelfUninstallCommand(IPathManager pathManager, IInstallationScanner scanner)
    {
        _pathManager = pathManager ?? throw new ArgumentNullException(nameof(pathManager));
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    public override Task<int> ExecuteAsync(CommandContext context, SelfUninstallCommandSettings settings)
    {
        if (!settings.Yes)
        {
            Theme.PrintWarning("This will completely remove PVM, all installed PHP versions, junctions, and PATH entries from your system.");
            if (!AnsiConsole.Confirm("Are you sure you want to proceed with uninstallation?", false))
            {
                Theme.PrintInfo("Uninstallation cancelled.");
                return Task.FromResult(0);
            }
        }

        Theme.PrintInfo("Initiating PVM complete uninstallation...");

        var pvmRoot = Path.GetDirectoryName(_scanner.VersionsDirectory)
                      ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pvm");
        var pvmBin = Path.Combine(pvmRoot, "bin");
        var pvmCurrent = Path.Combine(pvmRoot, "current");

        // 1. Remove PATH entries
        Theme.PrintInfo("Removing PVM directories from User PATH...");
        _pathManager.RemoveFromUserPath(pvmBin);
        _pathManager.RemoveFromUserPath(pvmCurrent);

        // 2. Terminate other running PVM or PHP processes if possible
        try
        {
            var currentPid = Process.GetCurrentProcess().Id;
            foreach (var proc in Process.GetProcessesByName("php").Concat(Process.GetProcessesByName("pvm")))
            {
                if (proc.Id != currentPid)
                {
                    try { proc.Kill(); } catch { }
                }
            }
        }
        catch { }

        // 3. Schedule removal or direct removal depending on OS
        if (OperatingSystem.IsWindows())
        {
            try
            {
                // Rename running exe so folder cleanup can happen or cmd can delete it cleanly
                var currentExe = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(currentExe) && File.Exists(currentExe))
                {
                    var tempExe = currentExe + ".delete_me";
                    if (File.Exists(tempExe)) File.Delete(tempExe);
                    File.Move(currentExe, tempExe);
                }
            }
            catch { }

            Theme.PrintInfo("Spawning background cleanup worker to remove system files after shutdown...");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"{pvmRoot}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Theme.PrintError($"Failed to start cleanup worker: {ex.Message}");
            }
        }
        else
        {
            // On POSIX, active binary directory can be removed after unlinking
            try
            {
                Directory.Delete(pvmRoot, true);
            }
            catch (Exception ex)
            {
                Theme.PrintError($"Could not remove {pvmRoot} directly: {ex.Message}");
            }
        }

        Theme.PrintSuccess("=== PVM UNINSTALLED SUCCESSFULLY ===");
        Theme.PrintInfo("All binaries, versions, and environment variables have been scheduled for complete removal.");
        return Task.FromResult(0);
    }
}
