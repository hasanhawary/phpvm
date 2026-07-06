using System.ComponentModel;
using Pvm.Application.Services;
using Pvm.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class IniCommandSettings : CommandSettings
{
    [CommandArgument(0, "[action]")]
    [Description("Action: list, enable, disable, get, set, open (default is list).")]
    public string? Action { get; init; }

    [CommandArgument(1, "[target]")]
    [Description("Target extension name or directive name.")]
    public string? Target { get; init; }

    [CommandArgument(2, "[value]")]
    [Description("Value to set for directive (when using 'set').")]
    public string? Value { get; init; }

    [CommandOption("-v|--version")]
    [Description("Specify target PHP version (defaults to active version).")]
    public string? Version { get; init; }
}

public sealed class IniCommand : AsyncCommand<IniCommandSettings>
{
    private readonly IniService _iniService;

    public IniCommand(IniService iniService)
    {
        _iniService = iniService ?? throw new ArgumentNullException(nameof(iniService));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, IniCommandSettings settings)
    {
        var action = settings.Action?.ToLowerInvariant() ?? "list";

        switch (action)
        {
            case "list" or "ls":
                return await ListExtensionsAsync(settings.Version);

            case "enable" or "on":
                if (string.IsNullOrWhiteSpace(settings.Target))
                {
                    Theme.PrintError("Please specify the extension name to enable (e.g., 'pvm ini enable mbstring').");
                    return 1;
                }
                var enableResult = await _iniService.ToggleExtensionAsync(settings.Target, true, settings.Version);
                if (enableResult.IsFailure)
                {
                    Theme.PrintError(enableResult.Error);
                    return 1;
                }
                Theme.PrintSuccess($"Enabled extension '{settings.Target}' in php.ini!");
                return 0;

            case "disable" or "off":
                if (string.IsNullOrWhiteSpace(settings.Target))
                {
                    Theme.PrintError("Please specify the extension name to disable (e.g., 'pvm ini disable xdebug').");
                    return 1;
                }
                var disableResult = await _iniService.ToggleExtensionAsync(settings.Target, false, settings.Version);
                if (disableResult.IsFailure)
                {
                    Theme.PrintError(disableResult.Error);
                    return 1;
                }
                Theme.PrintSuccess($"Disabled extension '{settings.Target}' in php.ini!");
                return 0;

            case "open" or "edit":
                Theme.PrintInfo("Opening php.ini in your default editor...");
                var openResult = await _iniService.OpenEditorAsync(settings.Version);
                if (openResult.IsFailure)
                {
                    Theme.PrintError(openResult.Error);
                    return 1;
                }
                return 0;

            case "get":
                if (string.IsNullOrWhiteSpace(settings.Target))
                {
                    Theme.PrintError("Please specify the directive name to get (e.g., 'pvm ini get memory_limit').");
                    return 1;
                }
                var getResult = await _iniService.GetDirectiveAsync(settings.Target, settings.Version);
                if (getResult.IsFailure)
                {
                    Theme.PrintError(getResult.Error);
                    return 1;
                }
                AnsiConsole.WriteLine(getResult.Value ?? "(not set)");
                return 0;

            case "set":
                if (string.IsNullOrWhiteSpace(settings.Target) || string.IsNullOrWhiteSpace(settings.Value))
                {
                    Theme.PrintError("Please specify both directive name and value (e.g., 'pvm ini set memory_limit 512M').");
                    return 1;
                }
                var setResult = await _iniService.SetDirectiveAsync(settings.Target, settings.Value, settings.Version);
                if (setResult.IsFailure)
                {
                    Theme.PrintError(setResult.Error);
                    return 1;
                }
                Theme.PrintSuccess($"Set directive '{settings.Target} = {settings.Value}' in php.ini!");
                return 0;

            default:
                Theme.PrintError($"Unknown action '{action}'. Valid actions are: list, enable, disable, get, set, open.");
                return 1;
        }
    }

    private async Task<int> ListExtensionsAsync(string? versionSpecifier)
    {
        var result = await _iniService.ListExtensionsAsync(versionSpecifier);
        if (result.IsFailure)
        {
            Theme.PrintError(result.Error);
            return 1;
        }

        var extensions = result.Value!;
        var table = new Table().Border(TableBorder.Rounded).Title("[bold cyan]PHP Extensions (php.ini)[/]");
        table.AddColumn("Extension Name");
        table.AddColumn("Status");
        table.AddColumn("Type");

        foreach (var ext in extensions.OrderBy(x => x.Name))
        {
            var status = ext.IsEnabled ? "[green]Enabled[/]" : "[grey]Disabled[/]";
            var type = ext.IsZendExtension ? "Zend Extension" : "Standard";
            table.AddRow(ext.Name, status, type);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
