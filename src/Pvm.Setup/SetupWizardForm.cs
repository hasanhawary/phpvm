using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pvm.Setup;

public class SetupWizardForm : Form
{
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
            Text = "Click 'Install' to begin transactional setup and register PVM in your User PATH.",
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

        var result = await InstallerEngine.RunInstallationAsync(
            installBinPath,
            isSilent: false,
            progressCallback: (progress, statusText) => UpdateStatus(progress, statusText));

        if (result.Success)
        {
            _statusLabel.ForeColor = Color.FromArgb(16, 124, 16);
            _progressBar.Value = 100;
            _installButton.Visible = false;
            _openTerminalButton.Visible = true;
            _closeButton.Text = "Finish";
            _closeButton.Enabled = true;
        }
        else
        {
            _statusLabel.ForeColor = Color.Red;
            _installButton.Enabled = true;
            _browseButton.Enabled = true;
            _installPathTextBox.Enabled = true;
            _closeButton.Enabled = true;
            MessageBox.Show(result.Message, "PVM Installation Rollback", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        var defaultBin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pvm", "bin");
        var result = await InstallerEngine.RunInstallationAsync(defaultBin, isSilent: true, progressCallback: null);
        return result.Success ? 0 : 1;
    }
}
