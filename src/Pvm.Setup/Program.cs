using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pvm.Setup;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        bool isSilent = args.Any(a => a.Equals("/S", StringComparison.OrdinalIgnoreCase) ||
                                      a.Equals("-s", StringComparison.OrdinalIgnoreCase) ||
                                      a.Equals("--silent", StringComparison.OrdinalIgnoreCase) ||
                                      a.Equals("-y", StringComparison.OrdinalIgnoreCase));

        if (isSilent)
        {
            return SetupWizardForm.RunSilentInstallationAsync().GetAwaiter().GetResult();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new SetupWizardForm());
        return 0;
    }
}

