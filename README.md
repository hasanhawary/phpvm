# pvm — PHP Version Manager for Windows

<div align="center">

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20%7C%20SOLID-blueviolet?style=for-the-badge)
![AOT Compatible](https://img.shields.io/badge/Standalone-Self--Contained-00C7B7?style=for-the-badge)

**The production-grade, fast, and zero-friction PHP version manager for Windows.**  
*Inspired by `nvm-windows`, `pyenv`, `sdkman`, and `asdf` — engineered specifically for the Windows developer ecosystem.*

</div>

---

## 💡 Why PVM?

On Windows, managing multiple PHP versions across different projects traditionally requires tedious manual editing of System Environment Variables (`PATH`), dealing with complex directory structures, restarting terminal tabs, or installing bulky, monolithic server packages (like WAMP, XAMPP, or Laragon) that lock your system into inflexible setups.

**PVM (`pvm`) solves this completely with zero friction:**
- ⚡ **Instant NTFS Directory Junctions (`mklink /J`)**: Instead of constantly editing, polluting, or reordering your Windows `PATH`, PVM registers a single, permanent directory junction (`%USERPROFILE%\.pvm\current`). When you run `pvm use <version>`, PVM atomically updates the junction point—making version switching **instantaneous** across all open terminal sessions.
- 🔓 **No Administrator Privileges Required**: Runs entirely in user space (`%USERPROFILE%\.pvm` and `%LOCALAPPDATA%\pvm`), keeping your system clean and secure without requiring `Right-Click -> Run as Administrator`.
- 🔄 **Native Windows Shell Awareness**: Broadcasts native Windows `WM_SETTINGCHANGE` environment notifications across the operating system (`HWND_BROADCAST`), ensuring new terminal windows recognize environment changes immediately.
- 🛠️ **Interactive `php.ini` & Alias Manager**: Toggle PHP extensions (`pvm ini enable curl`), inspect configuration values (`pvm ini get memory_limit`), update directives (`pvm ini set memory_limit 1024M`), and create semantic version shortcuts (`pvm alias default 8.4`) directly from your command line.
- 🩺 **Automated Doctor & Remediation**: Built-in system diagnostic tool (`pvm doctor --fix`) that detects and repairs PATH duplicates, resolves external PHP shadowing conflicts, and verifies required C++ redistributable runtime libraries (`vcruntime140.dll`).
- 🚀 **Self-Contained & Zero Dependencies**: Compiled as a trimmed, self-contained native executable (`pvm.exe`, ~34 MB). End users do **not** need .NET SDKs or runtimes installed on their machines.

---

## 📦 Installation (Choose One of 3 Ways)

PVM is designed to be installed in under 10 seconds. Choose the installation method that fits your workflow:

### Option 1: One-Line Script Installation via URL (Recommended)
Just like `rustup`, `bun`, or `nvm`, you can install PVM directly from PowerShell without manual downloading. Open PowerShell and run:

```powershell
irm https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.ps1 | iex
```

**What this script does automatically:**
1. Downloads the latest self-contained `pvm-win-x64.zip` from official GitHub Releases.
2. Extracts `pvm.exe` into `%USERPROFILE%\.pvm\bin\pvm.exe`.
3. Permanently registers `%USERPROFILE%\.pvm\bin` into your Windows User `PATH` environment variable.
4. Refreshes your session so you can start using `pvm` immediately!

---

### Option 2: Standalone Desktop Setup Wizard (`pvm-setup.exe`) or Zip Archive
If you prefer downloading files directly from your browser just like `nvm-windows` (`nvm-setup.exe`):

1. Go to the official **[GitHub Releases Page](https://github.com/hasanhawary/phpvm/releases)**.
2. Download **`pvm-setup.exe`** (our standalone 1-click desktop setup wizard executable).
3. Double-click `pvm-setup.exe` (or run `pvm-setup.exe /S` for silent unattended installation).
   - *The wizard automatically installs `pvm.exe` to `%USERPROFILE%\.pvm\bin\pvm.exe`, creates your system directories (`versions/`, `current/`), registers PVM globally in your Windows User `PATH`, and broadcasts environment change notifications!*
4. Open a **new** PowerShell or Command Prompt terminal and verify installation:
   ```powershell
   pvm --help
   ```

*(Alternatively, you can download `pvm-win-x64.zip` and manually extract `pvm.exe` to your `~/.pvm/bin/` folder).*

---

### Option 3: Manual Developer Build from Source (`git clone`)
If you want to build PVM directly from source code using the .NET 8 SDK:

1. **Prerequisite**: Ensure you have [Git](https://git-scm.com/) and the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed.
2. Clone the repository and navigate into the project folder:
   ```powershell
   git clone https://github.com/hasanhawary/phpvm.git
   cd phpvm
   ```
3. Run our automated build and install script:
   ```powershell
   # Compiles Release build, runs all 57 unit tests, and installs pvm.exe globally
   .\build.ps1
   ```
   *Note: `build.ps1` automatically builds `pvm.exe`, copies it to `%USERPROFILE%\.pvm\bin\pvm.exe`, and registers it in your User `PATH`.*

---

## ⚡ Quick Start Step-by-Step Guide

Once installed, follow these 5 simple steps to get your Windows PHP environment running cleanly:

### Step 1: Discover Available Remote PHP Versions
Fetch and display all available PHP builds from the official `windows.php.net` mirrors:
```powershell
pvm list --remote
```

### Step 2: Download & Install a PHP Version
Install any desired version (PVM automatically downloads the Thread Safe x64 binary by default, validates SHA256 cryptographic checksums, and extracts the clean binaries):
```powershell
pvm install 8.3
```

### Step 3: Switch Active System PHP Version
Activate PHP 8.3 across your Windows machine. PVM updates the `%USERPROFILE%\.pvm\current` NTFS junction point:
```powershell
pvm use 8.3
```

### Step 4: Verify Your Active PHP Environment
Check both PVM runtime status and the native `php -v` binary output:
```powershell
pvm current
php -v
```

### Step 5: Run Automated Health Diagnostics
Ensure your `PATH` is clean and free of conflicts or missing C++ runtime libraries:
```powershell
pvm doctor
```

---

## 🌟 Comprehensive Feature Guide

### 1. Semantic Version Aliasing (`pvm alias`)
Create memorable shortcuts for specific versions so you don't have to remember patch numbers:
```powershell
# Create semantic aliases
pvm alias default 8.4
pvm alias lts 8.2
pvm alias legacy 7.4

# Switch or install using your custom aliases directly!
pvm use default
pvm install lts

# View all saved aliases
pvm alias
```

### 2. Smart `php.ini` Extension & Directive Editor (`pvm ini`)
Inspect, toggle, and configure your active `php.ini` file without leaving your terminal or losing your formatting/comments:
```powershell
# Display a formatted status table of all PHP extensions in the active php.ini
pvm ini ls

# Enable or disable standard and Zend extensions instantly
pvm ini enable curl
pvm ini enable mbstring
pvm ini enable openssl
pvm ini disable xdebug

# Inspect and update configuration directives
pvm ini get memory_limit
pvm ini set memory_limit 1024M
pvm ini set upload_max_filesize 64M

# Open php.ini in your system default text editor (Notepad, VS Code, etc.)
pvm ini open
```

### 3. Automated System Health & Self-Healing (`pvm doctor`)
PVM includes a diagnostic engine that continuously audits your Windows environment:
```powershell
# Run diagnostics table
pvm doctor

# Automatically repair missing folders, PATH duplicates, and junction registrations
pvm doctor --fix
```
**What PVM Doctor checks:**
- `[PASS/FAIL]` **Directory Structure & Permissions**: Verifies `~/.pvm` folder integrity.
- `[PASS/WARN]` **Junction PATH Registration**: Verifies that `~/.pvm/current` is active in `PATH`.
- `[PASS/WARN]` **Windows PATH Hygiene**: Scans for redundant duplicate directory entries in your `PATH` that slow down command resolution.
- `[PASS/WARN]` **External PHP Shadowing Conflicts**: Detects if another PHP tool (like XAMPP, WAMP, or `C:\Program Files\PHP`) is placed higher in your `PATH` than PVM.
- `[PASS/FAIL]` **Visual C++ Redistributable Check**: Audits `System32` for `vcruntime140.dll` to prevent PHP 8.x missing DLL popup errors.

### 4. Shell Autocompletion (`pvm completion`)
Enable `<TAB>` autocompletion for PVM commands and installed version numbers across your shells:
```powershell
# For PowerShell: Add this line to your PowerShell $PROFILE
pvm completion powershell | Out-String | Invoke-Expression

# For Windows Command Prompt (cmd via Clink):
pvm completion cmd > %LOCALAPPDATA%\clink\pvm.lua

# For Git Bash / WSL:
pvm completion bash >> ~/.bashrc
```

### 5. In-Place Self-Updating (`pvm self-update`)
Update `pvm.exe` itself to the latest release directly from GitHub without opening a browser:
```powershell
# Check if a new version is available
pvm self-update --check

# Download and atomically replace pvm.exe with the latest version
pvm self-update
```

---

## 📖 Complete Command Reference (`pvm --help`)

| Command | Alias | Syntax | Description |
| :--- | :--- | :--- | :--- |
| **`list`** | `ls` | `pvm list [--remote]` | List installed local PHP versions or fetch available versions from official mirrors. |
| **`current`** | | `pvm current` | Display the active PHP version, binary path, and runtime status. |
| **`use`** | | `pvm use <version>` | Switch the active PHP version by atomically updating the NTFS directory junction. |
| **`install`** | | `pvm install <version> [--force]` | Download, verify SHA256 checksum, and install a PHP version from official mirrors. |
| **`uninstall`**| | `pvm uninstall <version>` | Remove an installed PHP version from your local disk (`~/.pvm/versions`). |
| **`env`** | | `pvm env [--check\|--clean\|--ps1]` | Inspect `PATH` hygiene (`--check`), clean duplicates (`--clean`), or output session evaluation scripts (`--ps1`, `--cmd`). |
| **`ini`** | | `pvm ini <ls\|enable\|disable\|get\|set\|open>` | Inspect or modify the active `php.ini` file's extensions and configuration directives. |
| **`alias`** | | `pvm alias [name] [target] [--remove]` | Create, list, or delete (`--remove`) semantic version aliases. |
| **`doctor`** | | `pvm doctor [--fix]` | Run system health audits. Pass `--fix` to automatically remediate issues. |
| **`self-update`**| `update` | `pvm self-update [--check]` | Check for updates (`--check`) or self-update `pvm.exe` directly from GitHub Releases. |
| **`completion`** | | `pvm completion <powershell\|cmd\|bash>` | Output shell parameter argument completion scripts. |

---

## 📂 System Directory & File Structure

PVM stores all files cleanly inside your user profile (`%USERPROFILE%` and `%LOCALAPPDATA%`), ensuring zero permission conflicts and no clutter across your C: drive:

```text
%USERPROFILE%\.pvm\
├── bin\                  <-- Contains pvm.exe (Registered globally in your User PATH)
├── current\              <-- NTFS Directory Junction pointing to active installed PHP version
├── versions\             <-- Installed PHP binaries (e.g., 8.2.18, 8.3.32, 8.4.23)
├── archives\             <-- Cached zip archive downloads from windows.php.net
└── temp\                 <-- Temporary extraction workspace

%LOCALAPPDATA%\pvm\config\
├── config.json           <-- PVM global configuration settings (default arch, TS/NTS preferences)
└── aliases.json          <-- Saved semantic version aliases
```

---

## 🏗️ Clean Architecture & Engineering Standards

PVM (`pvm`) is engineered using **.NET 8** following strict **Clean Architecture**, **SOLID**, and **Domain-Driven Design** guidelines to serve as a rock-solid, production-ready reference codebase:

```text
+-----------------------------------------------------------------------------+
|                                  Pvm.Cli                                    |
|             (Spectre.Console Presentation Layer & Command Routing)          |
+-------------------------------------+---------------------------------------+
                                      |
                                      v
+-----------------------------------------------------------------------------+
|                              Pvm.Application                                |
|          (Use Case Orchestrators, Version Resolution, Doctor Service)       |
+-------------------------------------+---------------------------------------+
                                      |
                                      v
+-----------------------------------------------------------------------------+
|                                 Pvm.Core                                    |
|          (Pure Domain Models, Value Objects, Enums, Ports / Interfaces)     |
+-------------------------------------+---------------------------------------+
                                      ^
                                      |
+-------------------------------------+---------------------------------------+
|                            Pvm.Infrastructure                               |
| (NTFS Junctions, Win32 Registry PATH, HTTP Scraper, ZIP, JSON Serialization)|
+-----------------------------------------------------------------------------+
```

### Key Architectural Highlights:
1. **Strict Dependency Rule**: `Pvm.Core` has **zero dependencies** on external frameworks, UI, or file access. `Pvm.Application` and `Pvm.Infrastructure` depend strictly inward on `Pvm.Core`.
2. **Result Monad Pattern (`Result<T>`)**: Zero exception-driven control flow across domain and application layers. Operations return explicit monad results, ensuring absolute predictability and graceful error handling.
3. **Source-Generated JSON (`PvmJsonSerializerContext`)**: 100% reflection-free serialization, making PVM fully compatible with **.NET 8 Native AOT** and **Trimming** (single-file compilation without warnings).
4. **Automated Architecture Enforcement**: Any violation of layering or namespace rules is immediately caught at compile-time by `Pvm.Architecture.Tests`.
5. **100% Unit Test Verification**: **57 unit tests** across 5 suites (`Core`, `Architecture`, `Application`, `Infrastructure`, `Cli`) pass cleanly on every build.

---

## 🛠️ Troubleshooting & FAQ

### Q1: When I run `php -v`, Windows says `vcruntime140.dll was not found`.
**Answer**: PHP 8.0+ binaries on Windows require the Microsoft Visual C++ 2015-2022 Redistributable runtime DLLs.  
**Fix**: Run `pvm doctor`. If the **Visual C++ Redistributable** check fails, download and install the official Microsoft x64 runtime: [https://aka.ms/vs/17/release/vc_redist.x64.exe](https://aka.ms/vs/17/release/vc_redist.x64.exe).

### Q2: I ran `pvm use 8.3`, but when I type `php -v`, it still shows XAMPP or Laragon's older PHP 8.1 version.
**Answer**: Another PHP directory (`C:\xampp\php` or `C:\Program Files\PHP`) is positioned *higher* in your Windows `PATH` than PVM's `%USERPROFILE%\.pvm\current` junction.  
**Fix**: Run `pvm doctor`. The **External PHP Shadowing Conflicts** check will list exactly which conflicting directories are blocking PVM. Remove those paths from your Windows Environment Variables or run `pvm doctor --fix`.

### Q3: Why does PowerShell show `Execution_Policy` errors when running `.ps1` scripts?
**Answer**: By default, Windows restricts running scripts on unconfigured systems.  
**Fix**: Open PowerShell as Administrator and enable local script execution for your user:
```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Q4: How do I use PVM inside CI/CD pipelines (like GitHub Actions) or temporary scripts?
**Answer**: Instead of altering system-wide junctions, use `pvm env --ps1` (PowerShell) or `pvm env --cmd` (Command Prompt) to evaluate and export temporary environment variables directly into the current shell session:
```powershell
pvm env --ps1 | Invoke-Expression
```

---

## 📄 Contributing & License

We welcome contributions from Windows software developers! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines on architectural rules, unit testing, and submitting pull requests.

This project is open-source and licensed under the **[MIT License](LICENSE)**.
