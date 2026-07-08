using System.ComponentModel;
using Pvm.Application.Services;
using Pvm.Cli.Rendering;
using Pvm.Core.Enums;
using Pvm.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class DoctorCommandSettings : CommandSettings
{
    [CommandOption("--fix|-f")]
    [Description("Automatically repair fixable issues.")]
    public bool Fix { get; init; }
}

public sealed class DoctorCommand : AsyncCommand<DoctorCommandSettings>
{
    private readonly DoctorService _doctorService;

    public DoctorCommand(DoctorService doctorService)
    {
        _doctorService = doctorService ?? throw new ArgumentNullException(nameof(doctorService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DoctorCommandSettings settings)
    {
        Theme.PrintInfo("Running PVM Doctor system health checks...");
        AnsiConsole.WriteLine();

        var results = await _doctorService.RunAllChecksAsync();
        PrintResultsTable(results);

        var hasErrors = results.Any(x => x.Status == DoctorStatus.Error);
        var hasWarnings = results.Any(x => x.Status == DoctorStatus.Warning);
        var fixable = results.Count(x => (x.Status == DoctorStatus.Error || x.Status == DoctorStatus.Warning) && x.CanFix);

        AnsiConsole.WriteLine();

        if (!hasErrors && !hasWarnings)
        {
            Theme.PrintSuccess("Your PVM installation and Windows environment are healthy!");
            return 0;
        }

        if (settings.Fix)
        {
            if (fixable == 0)
            {
                Theme.PrintWarning("No issues found that can be automatically repaired by PVM Doctor.");
                return hasErrors ? 1 : 0;
            }

            Theme.PrintInfo($"Attempting to automatically repair {fixable} issue(s)...");
            var fixResult = await _doctorService.FixAllAsync();
            if (fixResult.IsFailure)
            {
                Theme.PrintError(fixResult.Error);
                return 1;
            }

            Theme.PrintSuccess($"Successfully repaired {fixResult.Value} issue(s)! Re-running checks...");
            AnsiConsole.WriteLine();

            var newResults = await _doctorService.RunAllChecksAsync();
            PrintResultsTable(newResults);

            var stillErrors = newResults.Any(x => x.Status == DoctorStatus.Error);
            return stillErrors ? 1 : 0;
        }

        if (fixable > 0)
        {
            Theme.PrintInfo($"Found {fixable} issue(s) that can be automatically repaired. Run [yellow]pvm doctor --fix[/] to fix them.");
        }

        return hasErrors ? 1 : 0;
    }

    private static void PrintResultsTable(IReadOnlyList<DoctorCheckResult> results)
    {
        var table = new Table().Border(TableBorder.Rounded).Title("[bold cyan]PVM Doctor System Health Report[/]");
        table.AddColumn("Status");
        table.AddColumn("Diagnostic Check");
        table.AddColumn("Details & Recommendations");

        foreach (var r in results)
        {
            var statusStr = r.Status switch
            {
                DoctorStatus.Pass => "[green]PASS[/]",
                DoctorStatus.Warning => "[yellow]WARN[/]",
                DoctorStatus.Error => "[red]FAIL[/]",
                _ => "[grey]INFO[/]"
            };

            var details = r.Message;
            if (!string.IsNullOrWhiteSpace(r.FixRecommendation))
            {
                details += $"\n[yellow]Recommendation:[/] {r.FixRecommendation}";
            }

            table.AddRow(statusStr, $"[bold]{r.CheckName}[/]", details);
        }

        AnsiConsole.Write(table);
    }
}
