using System.ComponentModel;
using Pvm.Cli.Rendering;
using Pvm.Core.Ports;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class ListCommandSettings : CommandSettings
{
    [CommandOption("-r|--remote")]
    [Description("List available remote versions for download instead of installed versions.")]
    public bool Remote { get; init; }
}

public sealed class ListCommand : AsyncCommand<ListCommandSettings>
{
    private readonly IInstallationScanner _scanner;
    private readonly IBuildSource _buildSource;

    public ListCommand(IInstallationScanner scanner, IBuildSource buildSource)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _buildSource = buildSource ?? throw new ArgumentNullException(nameof(buildSource));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ListCommandSettings settings)
    {
        if (settings.Remote)
        {
            Theme.PrintInfo("Scanning remote builds from official PHP mirrors...");
            var builds = await _buildSource.GetAvailableVersionsAsync();
            if (builds.Count == 0)
            {
                Theme.PrintWarning("No remote versions found or mirrors are unreachable.");
                return 1;
            }

            foreach (var b in builds)
            {
                Theme.PrintInfo($"{b.Version} ({b.Architecture}, {b.ThreadSafety})");
            }
            return 0;
        }

        var installations = await _scanner.ScanInstalledAsync();
        VersionTableRenderer.Render(installations);
        return 0;
    }
}
