using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pvm.Setup;

public class SetupWizardForm : Form
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

    private TextBox _installPathTextBox = null!;
    private Button _browseButton = null!;
    private Button _installButton = null!;
    private Button _closeButton = null!;
    private Button _openTerminalButton = null!;
    private Label _statusLabel = null!;
    private ProgressBar _progressBar = null!;

    public SetupWizardForm()
    {
        InitializeWizardUI();
    }

    private void InitializeWizardUI()
    {
        this.Text = "PVM (PHP Version Manager for Windows) Setup Wizard";
        this.Size = new Size(580, 420);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = true;
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular, GraphicsUnit.Point);

        // Top Banner Panel
        var bannerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 85,
            BackColor = Color.FromArgb(0, 120, 212)
        };

        var titleLabel = new Label
        {
            Text = "PVM — PHP Version Manager Setup",
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 18),
            AutoSize = true
        };

        var subtitleLabel = new Label
        {
            Text = "Install and configure instantaneous PHP version switching across your Windows PC",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
            ForeColor = Color.FromArgb(230, 240, 255),
            Location = new Point(22, 48),
            AutoSize = true
        };

        bannerPanel.Controls.Add(titleLabel);
        bannerPanel.Controls.Add(subtitleLabel);
        this.Controls.Add(bannerPanel);

        // Main Content Panel
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(25, 20, 25, 20)
        };

        var pathLabel = new Label
        {
            Text = "Select Destination Location for PVM Executable and Junctions:",
            Location = new Point(25, 105),
            Size = new Size(500, 22)
        };

        var defaultBin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pvm", "bin");
        _installPathTextBox = new TextBox
        {
            Text = defaultBin,
            Location = new Point(25, 130),
            Size = new Size(410, 25),
            ReadOnly = false
        };

        _browseButton = new Button
        {
            Text = "Browse...",
            Location = new Point(445, 129),
            Size = new Size(85, 27),
            BackColor = Color.FromArgb(245, 245, 245),
            FlatStyle = FlatStyle.System
        };
        _browseButton.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select installation folder for PVM",
                SelectedPath = _installPathTextBox.Text
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _installPathTextBox.Text = dialog.SelectedPath;
            }
        };

        _statusLabel = new Label
        {
            Text = "Click 'Install' to begin setup and register PVM in your User PATH.",
            Location = new Point(25, 180),
            Size = new Size(510, 45),
            ForeColor = Color.FromArgb(60, 60, 60)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(25, 235),
            Size = new Size(505, 22),
            Value = 0,
            Style = ProgressBarStyle.Blocks
        };

        contentPanel.Controls.Add(pathLabel);
        contentPanel.Controls.Add(_installPathTextBox);
        contentPanel.Controls.Add(_browseButton);
        contentPanel.Controls.Add(_statusLabel);
        contentPanel.Controls.Add(_progressBar);
        this.Controls.Add(contentPanel);

        // Bottom Button Panel
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            BackColor = Color.FromArgb(243, 243, 243)
        };

        var separator = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(220, 220, 220)
        };
        bottomPanel.Controls.Add(separator);

        _installButton = new Button
        {
            Text = "Install PVM",
            Location = new Point(320, 16),
            Size = new Size(110, 34),
            BackColor = Color.FromArgb(0, 120, 212),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold)
        };
        _installButton.FlatAppearance.BorderSize = 0;
        _installButton.Click += async (s, e) => await PerformInstallationAsync(_installPathTextBox.Text);

        _openTerminalButton = new Button
        {
            Text = "Open PowerShell",
            Location = new Point(320, 16),
            Size = new Size(130, 34),
            BackColor = Color.FromArgb(16, 124, 16),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold),
            Visible = false
        };
        _openTerminalButton.FlatAppearance.BorderSize = 0;
        _openTerminalButton.Click += (s, e) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoExit -Command \"Write-Host 'Welcome to PVM!' -ForegroundColor Cyan; pvm --help\"",
                    UseShellExecute = true
                });
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open terminal: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        _closeButton = new Button
        {
            Text = "Cancel",
            Location = new Point(445, 16),
            Size = new Size(85, 34),
            FlatStyle = FlatStyle.System
        };
        _closeButton.Click += (s, e) => this.Close();

        bottomPanel.Controls.Add(_installButton);
        bottomPanel.Controls.Add(_openTerminalButton);
        bottomPanel.Controls.Add(_closeButton);
        this.Controls.Add(bottomPanel);
    }

    private async Task PerformInstallationAsync(string installBinPath)
    {
        _installButton.Enabled = false;
        _browseButton.Enabled = false;
        _installPathTextBox.Enabled = false;
        _closeButton.Enabled = false;

        try
        {
            var pvmRoot = Path.GetDirectoryName(installBinPath) ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pvm");
            var pvmVersions = Path.Combine(pvmRoot, "versions");
            var pvmCurrent = Path.Combine(pvmRoot, "current");
            var pvmArchives = Path.Combine(pvmRoot, "archives");
            var targetExe = Path.Combine(installBinPath, "pvm.exe");

            // Step 1: Create Directories
            UpdateStatus(15, $"Creating directory structure inside {pvmRoot}...");
            await Task.Delay(200);

            Directory.CreateDirectory(installBinPath);
            Directory.CreateDirectory(pvmVersions);
            Directory.CreateDirectory(pvmArchives);
            if (!Directory.Exists(pvmCurrent))
            {
                Directory.CreateDirectory(pvmCurrent);
            }

            // Step 2: Locate and install pvm.exe
            UpdateStatus(40, "Installing pvm.exe core executable...");
            await Task.Delay(200);

            var setupDir = AppDomain.CurrentDomain.BaseDirectory;
            var adjacentExe = Path.Combine(setupDir, "pvm.exe");
            var adjacentDistExe = Path.Combine(setupDir, "..", "dist", "pvm.exe");
            var localBuildExe = Path.Combine(setupDir, "..", "src", "Pvm.Cli", "bin", "Release", "net8.0", "win-x64", "pvm.exe");

            if (File.Exists(adjacentExe))
            {
                File.Copy(adjacentExe, targetExe, true);
            }
            else if (File.Exists(adjacentDistExe))
            {
                File.Copy(adjacentDistExe, targetExe, true);
            }
            else if (File.Exists(localBuildExe))
            {
                File.Copy(localBuildExe, targetExe, true);
            }
            else
            {
                UpdateStatus(50, "Downloading latest standalone pvm.exe from official GitHub Releases...");
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PvmSetupInstaller/1.0");

                var downloadUrl = "https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-win-x64.zip";
                var tempZip = Path.Combine(Path.GetTempPath(), "pvm-win-x64.zip");

                var response = await client.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();
                await using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }

                using var archive = ZipFile.OpenRead(tempZip);
                var entry = archive.Entries.FirstOrDefault(e => e.Name.Equals("pvm.exe", StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                {
                    entry.ExtractToFile(targetExe, true);
                }
                else
                {
                    throw new InvalidOperationException("pvm.exe was not found inside downloaded release zip.");
                }
                if (File.Exists(tempZip)) File.Delete(tempZip);
            }

            if (!File.Exists(targetExe))
            {
                throw new FileNotFoundException("Failed to install pvm.exe to destination folder.");
            }

            // Step 3: Register in PATH
            UpdateStatus(80, "Registering PVM directories in Windows User Environment PATH...");
            await Task.Delay(250);

            var currentUserPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            var pathEntries = currentUserPath
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            bool pathChanged = false;
            if (!pathEntries.Any(p => string.Equals(p, installBinPath, StringComparison.OrdinalIgnoreCase)))
            {
                pathEntries.Insert(0, installBinPath);
                pathChanged = true;
            }
            if (!pathEntries.Any(p => string.Equals(p, pvmCurrent, StringComparison.OrdinalIgnoreCase)))
            {
                pathEntries.Insert(1, pvmCurrent);
                pathChanged = true;
            }

            if (pathChanged)
            {
                var newPath = string.Join(";", pathEntries);
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            }

            // Step 4: Broadcast environment notification
            UpdateStatus(90, "Broadcasting Windows environment notification (WM_SETTINGCHANGE)...");
            try
            {
                SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", SMTO_ABORTIFHUNG, 3000, out _);
            }
            catch { /* ignore broadcast timeout */ }

            UpdateStatus(100, $"✔ PVM Installed Successfully to {targetExe}!\nYour environment PATH is updated. Click 'Open PowerShell' or open any new terminal window to start.");
            _statusLabel.ForeColor = Color.FromArgb(16, 124, 16);
            _progressBar.Value = 100;

            _installButton.Visible = false;
            _openTerminalButton.Visible = true;
            _closeButton.Text = "Finish";
            _closeButton.Enabled = true;
        }
        catch (Exception ex)
        {
            UpdateStatus(_progressBar.Value, $"❌ Installation Error: {ex.Message}");
            _statusLabel.ForeColor = Color.Red;
            _installButton.Enabled = true;
            _browseButton.Enabled = true;
            _installPathTextBox.Enabled = true;
            _closeButton.Enabled = true;
        }
    }

    private void UpdateStatus(int progress, string text)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => UpdateStatus(progress, text)));
            return;
        }
        _progressBar.Value = Math.Min(100, Math.Max(0, progress));
        _statusLabel.Text = text;
        Application.DoEvents();
    }

    public static async Task<int> RunSilentInstallationAsync()
    {
        await Task.CompletedTask;
        try
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var pvmRoot = Path.Combine(userProfile, ".pvm");
            var pvmBin = Path.Combine(pvmRoot, "bin");
            var pvmVersions = Path.Combine(pvmRoot, "versions");
            var pvmCurrent = Path.Combine(pvmRoot, "current");
            var targetExe = Path.Combine(pvmBin, "pvm.exe");

            Directory.CreateDirectory(pvmBin);
            Directory.CreateDirectory(pvmVersions);
            Directory.CreateDirectory(Path.Combine(pvmRoot, "archives"));
            if (!Directory.Exists(pvmCurrent)) Directory.CreateDirectory(pvmCurrent);

            var setupDir = AppDomain.CurrentDomain.BaseDirectory;
            var adjacentExe = Path.Combine(setupDir, "pvm.exe");
            var adjacentDistExe = Path.Combine(setupDir, "..", "dist", "pvm.exe");
            if (File.Exists(adjacentExe)) File.Copy(adjacentExe, targetExe, true);
            else if (File.Exists(adjacentDistExe)) File.Copy(adjacentDistExe, targetExe, true);

            var currentUserPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            var entries = currentUserPath.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
            bool changed = false;
            if (!entries.Any(p => string.Equals(p, pvmBin, StringComparison.OrdinalIgnoreCase))) { entries.Insert(0, pvmBin); changed = true; }
            if (!entries.Any(p => string.Equals(p, pvmCurrent, StringComparison.OrdinalIgnoreCase))) { entries.Insert(1, pvmCurrent); changed = true; }
            if (changed) Environment.SetEnvironmentVariable("PATH", string.Join(";", entries), EnvironmentVariableTarget.User);

            try { SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", SMTO_ABORTIFHUNG, 3000, out _); } catch { }
            return 0;
        }
        catch { return 1; }
    }
}
