# PHPVM — Universal Pure Shell PHP Version Manager for Windows, macOS & Linux

<div align="center">

![Pure Shell](https://img.shields.io/badge/Engine-Pure%20Shell%20(Bash%20%2B%20PowerShell)-512BD4?style=for-the-badge&logo=gnu-bash&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![Dependencies](https://img.shields.io/badge/Dependencies-Zero%20Runtime%20Dependencies-00C7B7?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**The production-grade, lightning-fast, and universal pure-shell PHP version manager.**  
*Engineered to run natively across all terminal sessions (`Bash`, `Zsh`, `PowerShell 5.1/7`, `cmd.exe`) without requiring `.NET`, `Node.js`, `Python`, or any external build tooling.*

---

[**macOS / Linux Installation (`curl`)**](#-1-one-line-installer-macos--linux--git-bash--wsl) • [**Windows Installation (`irm`)**](#-2-one-line-installer-windows-powershell--cmd) • [**⚡ Quick Start Guide**](#-quick-start-step-by-step-guide) • [**📖 Command Reference**](#-complete-command-reference)

</div>

---

## 💡 Why PHPVM?

Managing multiple PHP versions across projects on Windows, macOS, or Linux traditionally requires tedious manual editing of Environment Variables (`$PATH`), wrestling with system permissions (`sudo`), restarting terminal sessions, or installing heavy, monolithic local servers (like WAMP, XAMPP, Laragon, or MAMP) that lock your development environment into rigid configurations.

**PHPVM (`pvm`) solves this completely across all operating systems with zero friction:**

- 🚀 **100% Pure Shell — Zero Compilation & Zero Dependencies**: Built entirely using native OS scripting (`Bash` for POSIX systems and strict AST-safe `PowerShell` for Windows). No C# compilation, no `.exe` wrappers, and no external runtime installers required.
- ⚡ **Instant Atomic Version Switching**: 
  - **On Windows (`PowerShell` / `cmd` / `Git Bash`)**: Uses high-performance **NTFS Directory Junctions (`mklink /J`)** combined with native `WM_SETTINGCHANGE` OS broadcasts (`HWND_BROADCAST`).
  - **On macOS & Linux (`Bash` / `Zsh`)**: Uses atomic **POSIX Symbolic Links (`ln -sfn`)**.
  When you run `pvm use <version>`, your entire system switches instantly across all active terminal sessions without moving multi-gigabyte files or restarting computers.
- 🔓 **100% User-Space — Works in Any Terminal Without Admin Rights**: PHPVM installs cleanly inside your user home directory (`~/.pvm`). It works out of the box in standard user-level terminals right away without ever prompting for UAC Administrator privileges or root (`sudo`) access.
- 🛠️ **Built-in `php.ini` & Extension Manager**: Toggle extensions (`pvm ini enable curl`), inspect configuration directives (`pvm ini get memory_limit`), update limits (`pvm ini set memory_limit 1024M`), or open your active `php.ini` file in your favorite editor straight from the CLI.
- 🩺 **Self-Healing Doctor & PATH Hygiene**: Built-in system diagnostic engine (`pvm doctor --fix`) that detects duplicate PATH entries, cleans up external PHP shadowing conflicts, and automatically repairs directory structures.
- 🔄 **In-Place Self-Updating & Uninstallation**: Upgrade your PHPVM scripts directly from the main repository (`pvm self-update`) or perform a complete, clean uninstallation (`pvm self-uninstall -y`) right from the command line anytime.

---

## 🌐 Cross-Platform Compatibility Matrix

| Operating System | Supported Engines | Primary Script | Link Mechanism | Shell Profile Automation |
| :--- | :--- | :--- | :--- | :--- |
| **macOS (Darwin)** | `Bash`, `Zsh` *(Intel & Apple Silicon M1/M2/M3)* | `pvm` (`~35 KB`) | POSIX Symbolic Link (`ln -sfn`) | `~/.bashrc`, `~/.zshrc`, `~/.bash_profile` |
| **Linux (Ubuntu/Debian/Alpine)** | `Bash`, `Zsh` | `pvm` (`~35 KB`) | POSIX Symbolic Link (`ln -sfn`) | `~/.bashrc`, `~/.zshrc`, `~/.profile` |
| **Windows 10 / 11** | `PowerShell 5.1`, `PowerShell 7 (pwsh)`, `cmd.exe` | `pvm.ps1` + `pvm.cmd` | NTFS Directory Junction (`mklink /J`) | Windows Registry User `PATH` + `WM_SETTINGCHANGE` + `$PROFILE` |
| **Git Bash / MSYS2 / WSL** | `Bash` on Windows | `pvm` (`~35 KB`) | NTFS Directory Junction (`mklink /J`) | `~/.bashrc`, `~/.bash_profile` |

---

## 📦 Installation Guide (One-Line Installers)

### Method 1: One-Line Installer (macOS / Linux / Git Bash / WSL)
For developers on **macOS**, **Linux (Ubuntu/Debian/Alpine)**, **Git Bash**, or **WSL**, install PHPVM in seconds using our universal POSIX installer via `curl` or `wget`:

**Via cURL (Recommended):**
```bash
curl -fsSL https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.sh | bash
```

**Or via Wget:**
```bash
wget -qO- https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.sh | bash
```
*Automatically detects your OS, places the pure shell scripts in `$HOME/.pvm/bin`, and configures your shell profiles (`~/.bashrc`, `~/.zshrc`) so `pvm` is ready right away across all terminals!*

---

### Method 2: One-Line Installer (Windows PowerShell / cmd)
If you work inside Windows PowerShell or standard `cmd.exe` sessions, install or upgrade PHPVM instantly using our universal PowerShell installer:

```powershell
irm https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.ps1 | iex
```
*Downloads `pvm.ps1` and `pvm.cmd` to `$HOME\.pvm\bin`, registers your User Environment `PATH` cleanly in the registry without Admin rights, and broadcasts `WM_SETTINGCHANGE` so newly opened terminal windows pick up `pvm` instantly!*

---

## ⚡ Quick Start: Step-by-Step Guide

### 1. Verify Installation
Open any standard terminal and check PHPVM version:
```bash
pvm --version
```

### 2. Discover Available PHP Versions
List official TS (Thread Safe) and NTS (Non-Thread Safe) releases available from `windows.php.net` (Windows) or `php.net` (POSIX):
```bash
pvm list --remote
```

### 3. Install PHP Runtimes
Download and configure one or more PHP runtimes effortlessly:
```bash
# Install PHP 8.4 (downloads latest 8.4.x patch release automatically)
pvm install 8.4

# Install PHP 8.3 and PHP 8.2
pvm install 8.3
pvm install 8.2
```

### 4. Switch PHP Versions Globally
Switch your active system PHP runtime anywhere in user space:
```bash
pvm use 8.4
```
Check your active runtime instantly:
```bash
php -v
pvm current
```

---

## 📖 Complete Command Reference

PHPVM (`pvm`) provides an intuitive, rich set of commands across all platforms:

```
Usage: pvm <command> [options]

Commands:
  list, ls [options]         List installed PHP versions (--remote to view available releases)
  current                    Display active PHP runtime and system link diagnostics
  use <version|alias>        Atomically switch global active PHP runtime (`mklink /J` or `ln -sfn`)
  install <version>          Download, verify SHA256 checksum, and extract PHP runtime
  uninstall <version>        Remove an installed PHP runtime from local disk
  alias <name> [target]      Create, list, resolve, or remove (-r) semantic version shortcuts
  ini <subcommand>           Interactive php.ini and extension manager (ls, enable, disable, get, set, open)
  env [options]              Audit PATH diagnostics (--check), clean duplicates (--clean), or export vars
  doctor [--fix]             System health diagnostic engine with automatic PATH and junction repair
  self-update [--check]      Check and update PHPVM pure shell scripts in-place from GitHub
  self-uninstall [-y]        Complete clean uninstallation of PHPVM and shell profile configurations
  completion <shell>         Generate tab-completion script (powershell, bash, zsh)

Options:
  --help, -h                 Show command usage and detailed help
  --version, -v              Show version information
```

### Subcommand Breakdown & Examples

#### `pvm ini` (php.ini & Extension Management)
Easily configure `php.ini` settings without hunting for config files:
```bash
# List all extensions and their enable/disable status
pvm ini ls

# Enable or disable specific PHP extensions
pvm ini enable curl
pvm ini enable mbstring
pvm ini disable xdebug

# Read or modify php.ini directives directly
pvm ini get memory_limit
pvm ini set memory_limit 1024M
pvm ini set upload_max_filesize 100M

# Open the active php.ini file in your default editor
pvm ini open
```

#### `pvm alias` (Semantic Version Shortcuts)
Map semantic aliases like `default` or `prod` to specific PHP versions:
```bash
# Create semantic alias
pvm alias prod 8.4
pvm alias legacy 8.1

# Use alias to switch versions
pvm use prod

# List configured aliases
pvm alias

# Remove an alias
pvm alias -r legacy
```

#### `pvm doctor` (Diagnostic Engine & Auto-Repair)
If your environment variables get messy or another tool shadows your PHP binary, run the doctor:
```bash
# Run full system diagnostic audit
pvm doctor

# Automatically repair directory structures and clean PATH conflicts
pvm doctor --fix
```

#### `pvm self-update` & `pvm self-uninstall`
Keep your PHPVM scripts updated or perform a clean uninstallation:
```bash
# Update PVM to the latest release on GitHub
pvm self-update

# Cleanly uninstall PVM and remove all files and PATH entries
pvm self-uninstall -y
```

---

## 🏗️ Repository & File Architecture

```
phpvm/
├── pvm                 # Primary POSIX / Git Bash pure shell script
├── pvm.ps1             # Primary Windows PowerShell pure script (strict AST-safe formatting)
├── pvm.cmd             # Windows cmd.exe forwarding wrapper
├── install.sh          # One-line POSIX / Bash bootstrap installer (`curl ... | bash`)
├── install.ps1         # One-line PowerShell bootstrap installer (`irm ... | iex`)
├── uninstall.sh        # One-line POSIX / Bash uninstaller
├── uninstall.ps1       # One-line PowerShell uninstaller
├── test.sh             # Automated Bash integration test suite (9/9 checks)
├── test.ps1            # Automated PowerShell integration test suite (9/9 checks)
├── shell/              # Mirrored distribution scripts for backwards compatibility
├── README.md           # Documentation
├── LICENSE             # MIT License
└── CONTRIBUTING.md     # Contributor guidelines
```

---

## 🧪 Automated Testing & Quality Assurance

PHPVM maintains a 100% automated test verification suite covering all subcommands, edge cases, and JSON state manipulations across both Bash and PowerShell engines. Run the tests locally anytime:

```bash
# Run Bash test suite (macOS / Linux / Git Bash)
bash -c "./test.sh"

# Run PowerShell test suite (Windows)
powershell -ExecutionPolicy Bypass -File .\test.ps1
```

---

## 📄 License

PHPVM is open-source software licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.
