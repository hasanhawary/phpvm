using System.ComponentModel;
using Pvm.Application.Services;
using Pvm.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class AliasCommandSettings : CommandSettings
{
    [CommandArgument(0, "[name]")]
    [Description("Alias name (e.g., 'default', 'lts', 'prod').")]
    public string? Name { get; init; }

    [CommandArgument(1, "[target]")]
    [Description("Target PHP version specifier (e.g., '8.4', '8.3.32').")]
    public string? Target { get; init; }

    [CommandOption("--remove|-r")]
    [Description("Remove the specified alias.")]
    public bool Remove { get; init; }
}

public sealed class AliasCommand : AsyncCommand<AliasCommandSettings>
{
    private readonly AliasService _aliasService;

    public AliasCommand(AliasService aliasService)
    {
        _aliasService = aliasService ?? throw new ArgumentNullException(nameof(aliasService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AliasCommandSettings settings)
    {
        if (settings.Remove)
        {
            if (string.IsNullOrWhiteSpace(settings.Name))
            {
                Theme.PrintError("Please specify the alias name to remove (e.g., 'pvm alias --remove lts').");
                return 1;
            }

            var remResult = await _aliasService.RemoveAliasAsync(settings.Name);
            if (remResult.IsFailure)
            {
                Theme.PrintError(remResult.Error);
                return 1;
            }
            Theme.PrintSuccess($"Removed alias '{settings.Name}'.");
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(settings.Name) && !string.IsNullOrWhiteSpace(settings.Target))
        {
            var setResult = await _aliasService.SetAliasAsync(settings.Name, settings.Target);
            if (setResult.IsFailure)
            {
                Theme.PrintError(setResult.Error);
                return 1;
            }
            Theme.PrintSuccess($"Set alias '{settings.Name}' -> '{settings.Target}'.");
            return 0;
        }

        var aliases = await _aliasService.GetAllAliasesAsync();
        if (aliases.Count == 0)
        {
            Theme.PrintInfo("No version aliases configured. Create one with 'pvm alias <name> <target>'.");
            return 0;
        }

        var table = new Table().Border(TableBorder.Rounded).Title("[bold cyan]Configured PHP Version Aliases[/]");
        table.AddColumn("Alias Name");
        table.AddColumn("Target Version Specifier");

        foreach (var kvp in aliases.OrderBy(x => x.Key))
        {
            table.AddRow($"[yellow]{kvp.Key}[/]", kvp.Value);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
