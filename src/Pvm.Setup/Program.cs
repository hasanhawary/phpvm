using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Pvm.Setup;

internal static class Program
{
    private const int HWND_BROADCAST = 0xffff;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int Msg,
        IntPtr wParam,
        string lParam,
        int fuFlags,
        int uTimeout,
        out IntPtr lpdwResult);

    public static async Task<int> Main(string[] args)
    {
        bool isSilent = args.Any(a => a.Equals("/S", StringComparison.OrdinalIgnoreCase) ||
                                      a.Equals("-s", StringComparison.OrdinalIgnoreCase) ||
                                      a.Equals("--silent", StringComparison.OrdinalIgnoreCase) ||
                                      a.Equals("-y", StringComparison.OrdinalIgnoreCase));

        Console.Title = "PVM (PHP Version Manager for Windows) Setup";
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================================================");
        Console.WriteLine("          PVM — PHP Version Manager for Windows Installer Setup           ");
        Console.WriteLine("==========================================================================");
        Console.ResetColor();

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var pvmRoot = Path.Combine(userProfile, ".pvm");
        var pvmBin = Path.Combine(pvmRoot, "bin");
        var pvmVersions = Path.Combine(pvmRoot, "versions");
        var pvmCurrent = Path.Combine(pvmRoot, "current");
        var pvmArchives = Path.Combine(pvmRoot, "archives");
        var targetExe = Path.Combine(pvmBin, "pvm.exe");

        Console.WriteLine();
        Console.WriteLine($"Installation Directory: {pvmBin}");
        Console.WriteLine($"PHP Junction Target:    {pvmCurrent}");
        Console.WriteLine();

        if (!isSilent)
        {
            Console.Write("Press [ENTER] to begin installation (or Ctrl+C to cancel)... ");
            Console.ReadLine();
            Console.WriteLine();
        }

        try
        {
            // Step 1: Create directories
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[1/4] Creating PVM system directory structure...");
            Console.ResetColor();

            Directory.CreateDirectory(pvmBin);
            Directory.CreateDirectory(pvmVersions);
            Directory.CreateDirectory(pvmArchives);
            if (!Directory.Exists(pvmCurrent))
            {
                Directory.CreateDirectory(pvmCurrent);
            }

            // Step 2: Locate or download pvm.exe
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[2/4] Installing PVM core executable (pvm.exe)...");
            Console.ResetColor();

            var setupDir = AppDomain.CurrentDomain.BaseDirectory;
            var adjacentExe = Path.Combine(setupDir, "pvm.exe");
            var adjacentDistExe = Path.Combine(setupDir, "..", "dist", "pvm.exe");
            var localBuildExe = Path.Combine(setupDir, "..", "src", "Pvm.Cli", "bin", "Release", "net8.0", "win-x64", "pvm.exe");

            if (File.Exists(adjacentExe))
            {
                Console.WriteLine($"Found local pvm.exe ({adjacentExe}). Copying to {targetExe}...");
                File.Copy(adjacentExe, targetExe, true);
            }
            else if (File.Exists(adjacentDistExe))
            {
                Console.WriteLine($"Found build pvm.exe ({adjacentDistExe}). Copying to {targetExe}...");
                File.Copy(adjacentDistExe, targetExe, true);
            }
            else if (File.Exists(localBuildExe))
            {
                Console.WriteLine($"Found compiled pvm.exe ({localBuildExe}). Copying to {targetExe}...");
                File.Copy(localBuildExe, targetExe, true);
            }
            else
            {
                Console.WriteLine("Local pvm.exe not found next to installer. Downloading from official GitHub Releases...");
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PvmSetupInstaller/1.0");

                var downloadUrl = "https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-win-x64.zip";
                var tempZip = Path.Combine(Path.GetTempPath(), "pvm-win-x64.zip");
                
                try
                {
                    var response = await client.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    await using var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs);
                    fs.Close();

                    Console.WriteLine("Extracting archive...");
                    using var archive = ZipFile.OpenRead(tempZip);
                    var entry = archive.Entries.FirstOrDefault(e => e.Name.Equals("pvm.exe", StringComparison.OrdinalIgnoreCase));
                    if (entry != null)
                    {
                        entry.ExtractToFile(targetExe, true);
                    }
                    else
                    {
                        throw new InvalidOperationException("pvm.exe was not found inside the downloaded release archive.");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error downloading online release: {ex.Message}");
                    Console.ResetColor();
                    Console.WriteLine("Please ensure you are connected to the internet, or download pvm-setup.exe alongside pvm.exe from GitHub Releases.");
                    return 1;
                }
                finally
                {
                    if (File.Exists(tempZip)) File.Delete(tempZip);
                }
            }

            if (!File.Exists(targetExe))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Installation failed: pvm.exe could not be copied or extracted.");
                Console.ResetColor();
                return 1;
            }

            // Step 3: Register in User Environment PATH
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[3/4] Registering PVM directories in Windows User Environment PATH...");
            Console.ResetColor();

            var currentUserPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            var pathEntries = currentUserPath
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            bool pathChanged = false;

            // Ensure ~/.pvm/bin is registered
            if (!pathEntries.Any(p => string.Equals(p, pvmBin, StringComparison.OrdinalIgnoreCase)))
            {
                pathEntries.Insert(0, pvmBin);
                pathChanged = true;
                Console.WriteLine($"Added {pvmBin} to User PATH.");
            }

            // Ensure ~/.pvm/current is registered
            if (!pathEntries.Any(p => string.Equals(p, pvmCurrent, StringComparison.OrdinalIgnoreCase)))
            {
                pathEntries.Insert(1, pvmCurrent);
                pathChanged = true;
                Console.WriteLine($"Added {pvmCurrent} to User PATH.");
            }

            if (pathChanged)
            {
                var newPath = string.Join(";", pathEntries);
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            }
            else
            {
                Console.WriteLine("PVM directories are already registered in your User PATH.");
            }

            // Step 4: Broadcast environment notification
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[4/4] Broadcasting Windows environment change notification (WM_SETTINGCHANGE)...");
            Console.ResetColor();

            try
            {
                SendMessageTimeout(
                    (IntPtr)HWND_BROADCAST,
                    WM_SETTINGCHANGE,
                    IntPtr.Zero,
                    "Environment",
                    SMTO_ABORTIFHUNG,
                    5000,
                    out _);
            }
            catch
            {
                // Ignore broadcast timeouts or permissions on restricted sessions
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==========================================================================");
            Console.WriteLine("                      INSTALLATION SUCCESSFUL! 🎉                         ");
            Console.WriteLine($" PVM Executable Installed to: {targetExe}");
            Console.WriteLine("==========================================================================");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Quick Start Guidance:");
            Console.WriteLine("  1. Open a NEW PowerShell or Command Prompt window.");
            Console.WriteLine("  2. Type: pvm list --remote    (To see available Windows PHP versions)");
            Console.WriteLine("  3. Type: pvm install 8.3      (To install PHP 8.3)");
            Console.WriteLine("  4. Type: pvm use 8.3          (To activate PHP 8.3 system-wide)");
            Console.WriteLine();

            if (!isSilent)
            {
                Console.Write("Press any key to close the installer...");
                Console.ReadKey(true);
                Console.WriteLine();
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn unexpected error occurred during installation: {ex.Message}");
            Console.ResetColor();
            if (!isSilent)
            {
                Console.Write("Press any key to exit...");
                Console.ReadKey(true);
            }
            return 1;
        }
    }
}
