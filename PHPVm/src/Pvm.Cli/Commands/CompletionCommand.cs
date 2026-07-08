using System.ComponentModel;
using Pvm.Cli.Rendering;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pvm.Cli.Commands;

public sealed class CompletionCommandSettings : CommandSettings
{
    [CommandArgument(0, "<SHELL>")]
    [Description("The target shell: 'powershell', 'cmd', or 'bash'.")]
    public string Shell { get; init; } = "";
}

public sealed class CompletionCommand : Command<CompletionCommandSettings>
{
    public override int Execute(CommandContext context, CompletionCommandSettings settings)
    {
        var shell = settings.Shell.ToLowerInvariant().Trim();

        if (shell == "powershell" || shell == "pwsh")
        {
            var psScript = @"
Register-ArgumentCompleter -CommandName pvm -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    
    $commands = @('list', 'ls', 'current', 'use', 'install', 'uninstall', 'env', 'ini', 'alias', 'doctor', 'self-update', 'completion')
    $tokens = $commandAst.Tokens
    
    if ($tokens.Count -eq 2) {
        return $commands | Where-Object { $_ -like ""$wordToComplete*"" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }
    
    if ($tokens.Count -ge 3 -and ($tokens[1].Text -in @('use', 'install', 'uninstall'))) {
        $versions = @()
        try {
            $lsOutput = pvm list 2>$null | Out-String
            if ($lsOutput) {
                $versions += ($lsOutput -split ""\r?\n"" | Where-Object { $_ -match '^\s*([*\->]?)\s*([0-9]+\.[0-9]+(\.[0-9]+)?|[a-zA-Z0-9_-]+)' } | ForEach-Object { $Matches[2] })
            }
        } catch {}
        return $versions | Where-Object { $_ -like ""$wordToComplete*"" } | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }
}
";
            Console.Write(psScript.Trim());
            return 0;
        }

        if (shell == "cmd")
        {
            var cmdScript = @"
@echo off
REM Clink completion script for PVM in Windows Command Prompt
REM Add this to your %LOCALAPPDATA%\clink directory as pvm.lua
echo local pvm_parser = clink.arg.new_parser(
echo     'list', 'ls', 'current', 'use', 'install', 'uninstall',
echo     'env', 'ini', 'alias', 'doctor', 'self-update', 'completion'
echo )
echo clink.arg.register_parser('pvm', pvm_parser)
";
            Console.Write(cmdScript.Trim());
            return 0;
        }

        if (shell == "bash" || shell == "zsh")
        {
            var bashScript = @"
_pvm_completions() {
    local cur prev
    cur=""${COMP_WORDS[COMP_CWORD]}""
    prev=""${COMP_WORDS[COMP_CWORD-1]}""

    if [ $COMP_CWORD -eq 1 ]; then
        COMPREPLY=( $(compgen -W ""list ls current use install uninstall env ini alias doctor self-update completion"" -- ""$cur"") )
        return 0
    fi

    if [ ""$prev"" = ""use"" ] || [ ""$prev"" = ""install"" ] || [ ""$prev"" = ""uninstall"" ]; then
        local versions=$(pvm list 2>/dev/null | grep -E '^[ *->]+[0-9a-zA-Z._-]+' | awk '{print $NF}')
        COMPREPLY=( $(compgen -W ""$versions"" -- ""$cur"") )
        return 0
    fi
}
complete -F _pvm_completions pvm
";
            Console.Write(bashScript.Trim());
            return 0;
        }

        Theme.PrintError($"Unsupported shell '{settings.Shell}'. Supported shells: powershell, cmd, bash.");
        return 1;
    }
}
