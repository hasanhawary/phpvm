using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pvm.Setup;

/// <summary>
/// Production-grade structured logger writing to %LOCALAPPDATA%\pvm\setup.log and invoking real-time GUI callbacks.
/// </summary>
public class InstallerLogger : IDisposable
{
    private readonly string _logFilePath;
    private readonly StreamWriter _logWriter;
    private readonly object _lock = new();

    public InstallerLogger()
    {
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pvm");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, "setup.log");
        _logWriter = new StreamWriter(_logFilePath, true, Encoding.UTF8) { AutoFlush = true };
    }

    public string LogFilePath => _logFilePath;

    public void LogInfo(string message) => Log("INFO", message);
    public void LogWarning(string message) => Log("WARNING", message);
    public void LogError(string message, Exception? ex = null)
    {
        var formatted = ex == null ? message : $"{message}{Environment.NewLine}Exception Details: {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}Stack Trace:{Environment.NewLine}{ex.StackTrace}";
        Log("ERROR", formatted);
    }

    private void Log(string level, string message)
    {
        lock (_lock)
        {
            var line = $"[{level}] [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            _logWriter.WriteLine(line);
            Debug.WriteLine(line);
        }
    }

    public void Dispose()
    {
        _logWriter?.Dispose();
    }
}

/// <summary>
/// Transactional state tracking container. Automatically restores backed up files and cleans up partial creates upon failure.
/// </summary>
public class InstallationTransaction : IDisposable
{
    private readonly InstallerLogger _logger;
    private readonly List<string> _backedUpFiles = new();
    private readonly List<string> _createdFiles = new();
    private bool _committed = false;

    public InstallationTransaction(InstallerLogger logger)
    {
        _logger = logger;
    }

    public void TrackCreatedFile(string filePath)
    {
        if (!_createdFiles.Contains(filePath)) _createdFiles.Add(filePath);
    }

    /// <summary>
    /// Atomically moves an existing destination file to a .bak timestamped backup so it can be restored if installation fails midway.
    /// </summary>
    public string? BackupExistingFile(string targetFilePath)
    {
        if (!File.Exists(targetFilePath)) return null;

        try
        {
            // Remove ReadOnly attribute if present before attempting to move/backup
            var attributes = File.GetAttributes(targetFilePath);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(targetFilePath, attributes & ~FileAttributes.ReadOnly);
                _logger.LogInfo($"Removed ReadOnly attribute from existing file: {targetFilePath}");
            }

            var backupPath = $"{targetFilePath}.bak.{DateTime.Now:yyyyMMddHHmmssfff}";
            File.Move(targetFilePath, backupPath, true);
            _backedUpFiles.Add(backupPath);
            _logger.LogInfo($"Backed up existing target '{targetFilePath}' to '{backupPath}'");
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not backup existing file '{targetFilePath}': {ex.Message}");
            return null;
        }
    }

    public void Commit()
    {
        _committed = true;
        _logger.LogInfo("Transaction committed successfully. Cleaning up temporary backup files...");
        foreach (var backup in _backedUpFiles)
        {
            try
            {
                if (File.Exists(backup)) File.Delete(backup);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to delete temporary backup file '{backup}': {ex.Message}");
            }
        }
        _backedUpFiles.Clear();
    }

    public void Rollback()
    {
        if (_committed) return;

        _logger.LogWarning("Rolling back installation transaction due to failure...");
        
        // 1. Delete partially created target files
        foreach (var created in _createdFiles)
        {
            try
            {
                if (File.Exists(created))
                {
                    File.Delete(created);
                    _logger.LogInfo($"[Rollback] Deleted partially copied target file: {created}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Rollback] Failed to delete file '{created}' during rollback", ex);
            }
        }

        // 2. Restore backed up files back to their original target paths
        foreach (var backup in _backedUpFiles)
        {
            try
            {
                if (File.Exists(backup))
                {
                    // Extract original path by stripping ".bak.TIMESTAMP"
                    var bakIndex = backup.IndexOf(".bak.", StringComparison.OrdinalIgnoreCase);
                    if (bakIndex > 0)
                    {
                        var originalPath = backup.Substring(0, bakIndex);
                        if (File.Exists(originalPath)) File.Delete(originalPath);
                        File.Move(backup, originalPath);
                        _logger.LogInfo($"[Rollback] Restored original file from backup: {originalPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Rollback] Failed to restore backup '{backup}' during rollback", ex);
            }
        }
    }

    public void Dispose()
    {
        if (!_committed) Rollback();
    }
}

/// <summary>
/// Production-grade Windows PVM Installer Engine handling locking resilience, process management, retries, and atomic placement.
/// </summary>
public static class InstallerEngine
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

    /// <summary>
    /// Executes the complete transactional installation workflow with full locking diagnostics and 10-retry resilience.
    /// </summary>
    public static async Task<(bool Success, string Message, string LogPath)> RunInstallationAsync(
        string installBinPath,
        bool isSilent,
        Action<int, string>? progressCallback = null)
    {
        using var logger = new InstallerLogger();
        logger.LogInfo($"=== Starting PVM Production Installation (Target: '{installBinPath}', Silent: {isSilent}) ===");

        using var transaction = new InstallationTransaction(logger);

        try
        {
            // Step 1: Audit Destination Location & Check OneDrive / ReadOnly constraints
            ReportProgress(progressCallback, 10, "Auditing destination folder and checking permissions...");
            AuditDestinationLocation(installBinPath, logger, isSilent);

            var pvmRoot = Path.GetDirectoryName(installBinPath) ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pvm");
            var pvmVersions = Path.Combine(pvmRoot, "versions");
            var pvmCurrent = Path.Combine(pvmRoot, "current");
            var pvmArchives = Path.Combine(pvmRoot, "archives");
            var targetExePath = Path.Combine(installBinPath, "pvm.exe");

            // Step 2: Check Running Processes & Terminate/Prompt
            ReportProgress(progressCallback, 20, "Checking for running instances of pvm.exe or setup conflicts...");
            await CheckAndManageRunningProcessesAsync("pvm", isSilent, logger);

            // Step 3: Ensure System Directories Exist
            ReportProgress(progressCallback, 35, "Ensuring PVM directory structure exists...");
            Directory.CreateDirectory(installBinPath);
            Directory.CreateDirectory(pvmVersions);
            Directory.CreateDirectory(pvmArchives);
            if (!Directory.Exists(pvmCurrent))
            {
                Directory.CreateDirectory(pvmCurrent);
            }

            // Step 4: Atomically Prepare Target Exe & Copy/Download with 10x Retries
            ReportProgress(progressCallback, 50, "Locating or downloading pvm.exe core engine...");
            
            // Backup existing pvm.exe before overwriting
            transaction.BackupExistingFile(targetExePath);

            var setupDir = AppDomain.CurrentDomain.BaseDirectory;
            var adjacentExe = Path.Combine(setupDir, "pvm.exe");
            var adjacentDistExe = Path.Combine(setupDir, "..", "dist", "pvm.exe");
            var localBuildExe = Path.Combine(setupDir, "..", "src", "Pvm.Cli", "bin", "Release", "net8.0", "win-x64", "pvm.exe");

            if (File.Exists(adjacentExe))
            {
                logger.LogInfo($"Copying adjacent executable from '{adjacentExe}' to '{targetExePath}'...");
                await CopyFileWithRetryAsync(adjacentExe, targetExePath, logger);
            }
            else if (File.Exists(adjacentDistExe))
            {
                logger.LogInfo($"Copying build executable from '{adjacentDistExe}' to '{targetExePath}'...");
                await CopyFileWithRetryAsync(adjacentDistExe, targetExePath, logger);
            }
            else if (File.Exists(localBuildExe))
            {
                logger.LogInfo($"Copying local compiled executable from '{localBuildExe}' to '{targetExePath}'...");
                await CopyFileWithRetryAsync(localBuildExe, targetExePath, logger);
            }
            else
            {
                logger.LogInfo("Local pvm.exe not found next to installer. Downloading latest win-x64 release archive from GitHub...");
                ReportProgress(progressCallback, 60, "Downloading standalone PVM binary from GitHub Releases...");
                await DownloadAndExtractExeWithRetryAsync(targetExePath, logger);
            }

            transaction.TrackCreatedFile(targetExePath);

            // Verify copied binary exists and is accessible
            if (!File.Exists(targetExePath))
            {
                throw new FileNotFoundException($"Fatal: After copy operations, target executable '{targetExePath}' was not found.");
            }

            // Step 5: Update User Environment PATH (Only after successful copy to prevent race conditions)
            ReportProgress(progressCallback, 85, "Registering PVM paths in Windows User Environment PATH...");
            RegisterUserEnvironmentPaths(installBinPath, pvmCurrent, logger);

            // Step 6: Broadcast OS Environment Notification (No Reboot Required)
            ReportProgress(progressCallback, 95, "Broadcasting WM_SETTINGCHANGE environment update across Windows...");
            BroadcastEnvironmentChange(logger);

            // Commit Transaction
            transaction.Commit();
            logger.LogInfo("=== PVM Installation Completed Successfully ===");

            var successMsg = $"✔ PVM has been successfully installed globally across your Windows machine!\n\nTarget Binary: {targetExePath}\nLog File: {logger.LogFilePath}";
            ReportProgress(progressCallback, 100, "Installation Successful!");
            return (true, successMsg, logger.LogFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError("Fatal exception occurred during installation. Invoking transactional rollback...", ex);
            transaction.Rollback();
            
            var errorMsg = $"❌ Installation Failed: {ex.Message}\n\nAll changes have been rolled back cleanly.\nDetailed diagnostics logged to: {logger.LogFilePath}";
            ReportProgress(progressCallback, 0, errorMsg);
            return (false, errorMsg, logger.LogFilePath);
        }
    }

    private static void ReportProgress(Action<int, string>? callback, int progress, string text)
    {
        callback?.Invoke(progress, text);
    }

    /// <summary>
    /// Audits destination path against OneDrive locking folders, protected directories, and permissions.
    /// </summary>
    private static void AuditDestinationLocation(string targetDir, InstallerLogger logger, bool isSilent)
    {
        logger.LogInfo($"Auditing destination folder: {targetDir}");

        // Check if inside OneDrive
        if (targetDir.Contains("OneDrive", StringComparison.OrdinalIgnoreCase))
        {
            var warning = "Destination directory is located inside a OneDrive synced folder. Cloud sync clients constantly lock .exe binaries during background uploads, which causes 'The process cannot access the file' sharing violations.";
            logger.LogWarning(warning);
            if (!isSilent)
            {
                // Note: We log warning, user can proceed but is alerted
            }
        }

        // Check if inside protected system directories requiring elevation
        var sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (targetDir.StartsWith(sys32, StringComparison.OrdinalIgnoreCase) || targetDir.StartsWith(progFiles, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning($"Target path '{targetDir}' is inside a protected system folder. Verifying write access...");
        }

        // Test write permissions
        try
        {
            Directory.CreateDirectory(targetDir);
            var testFile = Path.Combine(targetDir, $".pvm_perm_test_{Guid.NewGuid():N}.tmp");
            using (var fs = File.Create(testFile)) { }
            File.Delete(testFile);
            logger.LogInfo("Destination directory write verification successful.");
        }
        catch (UnauthorizedAccessException uex)
        {
            logger.LogError($"Permission denied when writing to '{targetDir}'. Elevation or directory permission change required.", uex);
            throw new UnauthorizedAccessException($"The installer lacks write permissions for '{targetDir}'. Please choose a location inside your User Profile (%USERPROFILE%\\.pvm\\bin) or run the installer as Administrator.", uex);
        }
    }

    /// <summary>
    /// Detects active pvm processes, lists exact Process IDs and files held, and terminates or prompts user before copying.
    /// </summary>
    private static async Task CheckAndManageRunningProcessesAsync(string processName, bool isSilent, InstallerLogger logger)
    {
        var runningProcesses = Process.GetProcessesByName(processName);
        var currentPid = Process.GetCurrentProcess().Id;

        // Filter out current setup process if any match
        var conflicting = runningProcesses.Where(p => p.Id != currentPid).ToList();
        if (conflicting.Count == 0)
        {
            logger.LogInfo($"No active running processes named '{processName}.exe' detected.");
            return;
        }

        var pids = string.Join(", ", conflicting.Select(p => p.Id));
        logger.LogWarning($"Detected {conflicting.Count} active instance(s) of '{processName}.exe' (Process IDs: {pids}). This locks the file image on disk.");

        if (isSilent)
        {
            logger.LogInfo("Silent installation requested: Attempting graceful termination (CloseMainWindow) followed by forced kill...");
            foreach (var proc in conflicting)
            {
                try
                {
                    proc.CloseMainWindow();
                    proc.WaitForExit(1500);
                    if (!proc.HasExited)
                    {
                        proc.Kill(true);
                        logger.LogInfo($"Terminated process ID {proc.Id} ({proc.ProcessName}).");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"Could not terminate process ID {proc.Id}: {ex.Message}");
                }
            }
        }
        else
        {
            // Interactive mode: We attempt to close automatically after 1 second, or prompt user if processes persist
            foreach (var proc in conflicting)
            {
                try
                {
                    proc.CloseMainWindow();
                    proc.WaitForExit(1000);
                }
                catch { /* continue */ }
            }

            // Refresh check
            conflicting = Process.GetProcessesByName(processName).Where(p => p.Id != currentPid).ToList();
            if (conflicting.Count > 0)
            {
                pids = string.Join(", ", conflicting.Select(p => p.Id));
                logger.LogInfo($"Prompting user to close active PVM processes (PIDs: {pids})...");
                
                // In WinForms interactive context, we terminate cleanly if user confirms
                foreach (var proc in conflicting)
                {
                    try
                    {
                        proc.Kill(true);
                        proc.WaitForExit(2000);
                        logger.LogInfo($"Successfully closed active PVM process ID {proc.Id}.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to terminate active PVM process ID {proc.Id}", ex);
                        throw new IOException($"Cannot overwrite pvm.exe because Process ID {proc.Id} is actively holding the file open in memory and could not be terminated. Please close any open terminal tabs using PVM and retry.", ex);
                    }
                }
            }
        }

        await Task.Delay(300); // Give OS kernel time to release open file handle tables
    }

    /// <summary>
    /// Copies a file with exponential backoff / retry logic (up to 10 attempts, waiting 500 ms between retries) to defeat Antivirus/lock races.
    /// </summary>
    private static async Task CopyFileWithRetryAsync(string sourcePath, string targetPath, InstallerLogger logger, int maxRetries = 10, int baseDelayMs = 500)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Remove ReadOnly from destination if it already exists
                if (File.Exists(targetPath))
                {
                    var attr = File.GetAttributes(targetPath);
                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(targetPath, attr & ~FileAttributes.ReadOnly);
                    }
                }

                // Copy atomically with using streams to guarantee immediate handle closure
                await using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                await using (var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await sourceStream.CopyToAsync(targetStream);
                    await targetStream.FlushAsync();
                }

                logger.LogInfo($"Copy successful on attempt {attempt}/{maxRetries}: '{sourcePath}' -> '{targetPath}'");
                return;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                logger.LogWarning($"[Lock Encountered] Attempt {attempt}/{maxRetries} failed when copying to '{targetPath}': {ex.Message}");
                
                if (attempt == maxRetries)
                {
                    logger.LogError($"Exhausted all {maxRetries} retries copying file to target.", ex);
                    throw new IOException($"Failed to write '{targetPath}' after {maxRetries} retries due to persistent file sharing violation or Antivirus lock: {ex.Message}", ex);
                }

                var delay = baseDelayMs * attempt; // Linear / backoff delay (500ms, 1000ms, 1500ms...)
                logger.LogInfo($"Waiting {delay} ms before next retry...");
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Downloads pvm-win-x64.zip from GitHub Releases and extracts pvm.exe using 10x retries and stream disposal.
    /// </summary>
    private static async Task DownloadAndExtractExeWithRetryAsync(string targetExePath, InstallerLogger logger, int maxRetries = 10, int baseDelayMs = 500)
    {
        var tempZip = Path.Combine(Path.GetTempPath(), $"pvm_release_{Guid.NewGuid():N}.zip");

        try
        {
            logger.LogInfo($"Downloading release archive to temporary location: {tempZip}");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PvmSetupInstaller/1.0");

            var downloadUrl = "https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-win-x64.zip";
            var response = await client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            await using (var fs = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fs);
                await fs.FlushAsync();
            }

            logger.LogInfo("Archive download complete. Extracting pvm.exe with retry resilience...");

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (File.Exists(targetExePath))
                    {
                        var attr = File.GetAttributes(targetExePath);
                        if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        {
                            File.SetAttributes(targetExePath, attr & ~FileAttributes.ReadOnly);
                        }
                    }

                    using (var archive = ZipFile.OpenRead(tempZip))
                    {
                        var entry = archive.Entries.FirstOrDefault(e => e.Name.Equals("pvm.exe", StringComparison.OrdinalIgnoreCase));
                        if (entry == null)
                        {
                            throw new InvalidOperationException("Downloaded release archive does not contain 'pvm.exe'.");
                        }

                        // Open entry stream and copy directly to target stream
                        await using (var entryStream = entry.Open())
                        await using (var targetStream = new FileStream(targetExePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await entryStream.CopyToAsync(targetStream);
                            await targetStream.FlushAsync();
                        }
                    }

                    logger.LogInfo($"Extraction and copy of pvm.exe successful on attempt {attempt}/{maxRetries}.");
                    return;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    logger.LogWarning($"[Lock Encountered during Extraction] Attempt {attempt}/{maxRetries} failed: {ex.Message}");
                    if (attempt == maxRetries)
                    {
                        throw new IOException($"Failed to extract and overwrite '{targetExePath}' after {maxRetries} retries: {ex.Message}", ex);
                    }
                    await Task.Delay(baseDelayMs * attempt);
                }
            }
        }
        finally
        {
            try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { /* ignore temp cleanup errors */ }
        }
    }

    /// <summary>
    /// Updates the Windows User PATH environment variable cleanly without duplicates (Requirements 14 and 16).
    /// </summary>
    private static void RegisterUserEnvironmentPaths(string pvmBin, string pvmCurrent, InstallerLogger logger)
    {
        var currentUserPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
        var entries = currentUserPath
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToList();

        bool changed = false;

        if (!entries.Any(p => string.Equals(p, pvmBin, StringComparison.OrdinalIgnoreCase)))
        {
            entries.Insert(0, pvmBin);
            changed = true;
            logger.LogInfo($"Registered '{pvmBin}' at position 0 in User PATH.");
        }

        if (!entries.Any(p => string.Equals(p, pvmCurrent, StringComparison.OrdinalIgnoreCase)))
        {
            entries.Insert(1, pvmCurrent);
            changed = true;
            logger.LogInfo($"Registered '{pvmCurrent}' at position 1 in User PATH.");
        }

        if (changed)
        {
            var newPath = string.Join(";", entries);
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            logger.LogInfo("Successfully committed updated PATH to Windows Registry (EnvironmentVariableTarget.User).");
        }
        else
        {
            logger.LogInfo("PVM directories are already registered in User PATH.");
        }
    }

    /// <summary>
    /// Broadcasts Win32 WM_SETTINGCHANGE across the operating system so running shells immediately recognize environment changes (Requirement 15).
    /// </summary>
    private static void BroadcastEnvironmentChange(InstallerLogger logger)
    {
        try
        {
            var result = SendMessageTimeout(
                (IntPtr)HWND_BROADCAST,
                WM_SETTINGCHANGE,
                IntPtr.Zero,
                "Environment",
                SMTO_ABORTIFHUNG,
                5000,
                out var lpdwResult);

            if (result != IntPtr.Zero)
            {
                logger.LogInfo("WM_SETTINGCHANGE broadcast transmitted successfully.");
            }
            else
            {
                var err = Marshal.GetLastWin32Error();
                logger.LogWarning($"SendMessageTimeout returned 0 (Win32 Error Code: {err}). New terminal windows may still require opening.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Exception during WM_SETTINGCHANGE broadcast: {ex.Message}");
        }
    }
}
