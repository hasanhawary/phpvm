using Spectre.Console;

namespace Pvm.Cli.Rendering;

/// <summary>
/// Provides consistent styling, color palettes, and formatting for pvm console output.
/// </summary>
public static class Theme
{
    public static Color Accent => Color.Cyan1;
    public static Color Success => Color.Green;
    public static Color Warning => Color.Yellow;
    public static Color Error => Color.Red;
    public static Color Muted => Color.Grey;
    public static Color ActiveHighlight => Color.SpringGreen3;

    public static void PrintError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]✗ error:[/] {message.EscapeMarkup()}");
    }

    public static void PrintSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]✓[/] {message.EscapeMarkup()}");
    }

    public static void PrintWarning(string message)
    {
        AnsiConsole.MarkupLine($"[bold yellow]![/] {message.EscapeMarkup()}");
    }

    public static void PrintInfo(string message)
    {
        AnsiConsole.MarkupLine($"[cyan]i[/] {message.EscapeMarkup()}");
    }
}
