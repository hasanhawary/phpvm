using Pvm.Core.Models;
using Spectre.Console;

namespace Pvm.Cli.Rendering;

/// <summary>
/// Renders installed PHP versions in a clean, styled Spectre.Console table or list.
/// </summary>
public static class VersionTableRenderer
{
    public static void Render(IReadOnlyList<PhpInstallation> installations)
    {
        if (installations.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No PHP versions installed.[/]");
            AnsiConsole.MarkupLine("Run [cyan]pvm install <version>[/] to install a PHP version.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Active[/]").Centered())
            .AddColumn(new TableColumn("[bold]Version[/]"))
            .AddColumn(new TableColumn("[bold]Arch[/]").Centered())
            .AddColumn(new TableColumn("[bold]Thread Safety[/]").Centered())
            .AddColumn(new TableColumn("[bold]Path[/]"));

        foreach (var inst in installations)
        {
            var activeMark = inst.IsActive ? "[bold springgreen3]*[/]" : "";
            var versionText = inst.IsActive
                ? $"[bold springgreen3]{inst.Version}[/]"
                : $"[white]{inst.Version}[/]";
            var archText = $"[grey]{inst.Architecture.ToString().ToLowerInvariant()}[/]";
            var tsText = $"[grey]{inst.ThreadSafety.ToString().ToUpperInvariant()}[/]";
            var pathText = $"[grey]{inst.Path.EscapeMarkup()}[/]";

            table.AddRow(activeMark, versionText, archText, tsText, pathText);
        }

        AnsiConsole.Write(table);
    }
}
