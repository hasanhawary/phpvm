# PVM — Universal PHP Version Manager for Windows, macOS & Linux

<div align="center">

![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20macOS%20%7C%20Linux-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-x64%20%7C%20arm64%20(Apple%20Silicon)-00C7B7?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![Self-Contained](https://img.shields.io/badge/Binary-Zero%20Dependencies-blueviolet?style=for-the-badge)

**The production-grade, lightning-fast, and cross-platform PHP version manager.**  
*Engineered from the ground up with clean architecture (`SOLID`) as a standalone native executable (~34 MB) requiring zero runtime dependencies.*

---

[**Download Windows EXE (`pvm-setup.exe`)**](https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-setup.exe) • [**macOS / Linux Installation**](#method-2-one-line-macos--linux-installer-curl--wget--recommended-for-macos--linux) • [**Quick Start**](#⚡-quick-start-step-by-step-guide) • [**Command Reference**](#📖-complete-command-reference-pvm---help)

</div>

---

## 💡 Why PVM?

Managing multiple PHP versions across projects on Windows, macOS, or Linux traditionally requires tedious manual editing of Environment Variables (`$PATH`), wrestling with system permissions (`sudo`), restarting terminal sessions, or installing heavy, monolithic local servers (like WAMP, XAMPP, Laragon, or MAMP) that lock your development environment into rigid configurations.

**PVM (`pvm`) solves this completely across all operating systems with zero friction:**

- 🚀 **Universal Cross-Platform Native Binaries**: Whether you are on **Windows 10/11 (x64)**, **macOS (Intel & Apple Silicon M1/M2/M3)**, or **Linux (Ubuntu, Debian, Alpine x64/arm64)**, PVM installs as a single, ultra-fast standalone native executable (`pvm.exe` on Windows, `pvm` on POSIX).
- ⚡ **Instant Atomic Version Switching**: 
  - **On Windows**: Uses high-performance **NTFS Directory Junctions (`mklink /J`)** combined with native `WM_SETTINGCHANGE` OS broadcasts (`HWND_BROADCAST`).
  - **On macOS & Linux**: Uses atomic **POSIX Symbolic Links (`ln -sfn`)**.
  When you run `pvm use <version>`, your entire system switches instantly without moving multi-gigabyte files or restarting computers.
- 🔓 **100% User-Space — Zero Administrator / `sudo` Rights Needed**: PVM installs cleanly inside your user home directory (`~/.pvm`). Our automatic shell login profile injection (`~/.bashrc`, `~/.zshrc`, `Profile.ps1`) ensures PVM always takes priority over any pre-existing global PHP installation **without requiring Administrator or root access**.
- 🛠️ **Built-in `php.ini` & Extension Manager**: Toggle extensions (`pvm ini enable curl`), inspect configuration directives (`pvm ini get memory_limit`), update limits (`pvm ini set memory_limit 1024M`), or open your active `php.ini` file in your favorite editor straight from the CLI.
- 🩺 **Self-Healing Doctor & PATH Hygiene**: Built-in system diagnostic engine (`pvm doctor --fix`) that detects duplicate PATH entries, cleans up external PHP shadowing conflicts, and verifies native C++ runtime requirements (`vcruntime140.dll`).
- 🔄 **In-Place Self-Updating & Uninstallation**: Upgrade your PVM binary (`pvm self-update`) or perform a complete, clean uninstallation (`pvm self-uninstall -y`) right from the command line anytime.

---

## 🌐 Cross-Platform Compatibility Matrix

| Operating System | Supported Architectures | Binary Name | Link Mechanism | Shell Profile Automation |
| :--- | :--- | :--- | :--- | :--- |
| **Windows 10 / 11** | `win-x64` | `pvm.exe` (`~34 MB`) | NTFS Directory Junction (`mklink /J`) | PowerShell `$PROFILE` + Windows Registry User `PATH` + `WM_SETTINGCHANGE` |
| **macOS (Darwin)** | `osx-x64`, `osx-arm64` *(Apple Silicon M1/M2/M3)* | `pvm` (`~32 MB`) | POSIX Symbolic Link (`ln -sfn`) | `~/.bashrc`, `~/.zshrc`, `~/.bash_profile` |
| **Linux (Debian/Ubuntu/RHEL)** | `linux-x64`, `linux-arm64` | `pvm` (`~32 MB`) | POSIX Symbolic Link (`ln -sfn`) | `~/.bashrc`, `~/.zshrc`, `~/.profile` |

---

## 📦 Installation Guide (All Platforms)

### Method 1: Standalone Windows Setup Wizard (`pvm-setup.exe`) — Recommended for Windows Desktop
If you prefer a classic, visual desktop installation wizard (`like nvm-setup.exe` or `git-setup.exe`):

1. **[Click Here to Download `pvm-setup.exe` Directly](https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-setup.exe)** *(or check out the [GitHub Releases Page](https://github.com/hasanhawary/phpvm/releases))*.
2. Double-click `pvm-setup.exe` (or run `pvm-setup.exe /S` from terminal for unattended silent installation).
3. The visual setup wizard checks if `pvm.exe` is running, lets you customize your installation directory, extracts the native binary atomically, registers your User `PATH`, and configures your PowerShell profiles automatically!

---

### Method 2: One-Line macOS & Linux Installer (`curl / wget`) — Recommended for macOS & Linux
For developers on **macOS (Intel & Apple Silicon)**, **Ubuntu**, **Debian**, **Cygwin**, or **Git Bash**, install PVM in seconds using our universal POSIX installer:

**Via cURL (macOS / Linux / Git Bash):**
```bash
curl -o- https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.sh | bash
```

**Or via Wget (Linux / Bash):**
```bash
wget -qO- https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.sh | bash
```
*Automatically detects your OS and CPU architecture (`x64` vs `arm64`), downloads the corresponding native binary to `$HOME/.pvm/bin/pvm`, and configures `~/.bashrc` / `~/.zshrc` so PVM works immediately across terminal sessions!*

---

### Method 3: One-Line Windows PowerShell Script (`install.ps1`)
If you work inside PowerShell and prefer terminal automation without GUI wizards (`like rustup` or `bun`):

```powershell
irm https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.ps1 | iex
```
*Downloads `pvm.exe` to `$HOME\.pvm\bin\pvm.exe`, configures `$PROFILE` for non-admin session prioritization, registers User `PATH`, and broadcasts environment updates right across Windows.*

---

### Method 4: Package Managers (`Scoop`, `Chocolatey`, `Homebrew`)

#### For Windows via Scoop:
```powershell
scoop bucket add phpvm https://github.com/hasanhawary/phpvm.git
scoop install pvm
```

#### For Windows via Chocolatey:
```powershell
choco install pvm
```

#### For macOS / Linux via Homebrew:
```bash
brew tap hasanhawary/phpvm
brew install pvm
```

---

### Method 5: Pre-built Binary Archives (`.zip` / `.tar.gz`) — Manual Installation
If you want to place `pvm` anywhere without running installation scripts:

1. Download your native binary package from **[GitHub Releases](https://github.com/hasanhawary/phpvm/releases/latest)**:
   - Windows (x64): [`pvm-win-x64.zip`](https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-win-x64.zip)
   - macOS (Intel x64): [`pvm-osx-x64.tar.gz`](https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-osx-x64.tar.gz)
   - macOS (Apple Silicon ARM64): [`pvm-osx-arm64.tar.gz`](https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-osx-arm64.tar.gz)
   - Linux (x64): [`pvm-linux-x64.tar.gz`](https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-linux-x64.tar.gz)
   - Linux (ARM64): [`pvm-linux-arm64.tar.gz`](https://github.com/hasanhawary/phpvm/releases/latest/download/pvm-linux-arm64.tar.gz)
2. Extract the binary into `~/.pvm/bin/pvm` (`~/.pvm/bin/pvm.exe` on Windows).
3. Add `~/.pvm/current` and `~/.pvm/bin` to your system `$PATH` (`export PATH="$HOME/.pvm/current:$HOME/.pvm/bin:$PATH"`).

---

### Method 6: Build Directly from Source (`git clone`)
To compile PVM from source using the .NET 8 SDK across any platform:

```powershell
git clone https://github.com/hasanhawary/phpvm.git
cd phpvm
.\build.ps1
```
*Runs all 57 unit tests, compiles single-file trimmed AOT-compatible binaries (`pvm.exe` and `pvm-setup.exe`), and installs globally.*

---

## ⚡ Quick Start Step-by-Step Guide

Once installed, follow these 5 quick steps to master your PHP environment across any OS:

### Step 1: Discover Available PHP Versions
Fetch and display all available PHP builds from official mirrors:
```powershell
pvm list --remote
```

### Step 2: Download & Install a PHP Version
Download and install any desired version (`PVM automatically verifies SHA256 cryptographic checksums and configures native Thread Safe/Non-Thread Safe architectures`):
```powershell
pvm install 8.4
```

### Step 3: Switch Active System PHP Version
Switch your active PHP version instantaneously across your device:
```powershell
pvm use 8.4
```

### Step 4: Verify Your Active PHP Environment
Verify both PVM status and the native `php -v` output:
```powershell
pvm current
php -v
```

### Step 5: Run Automated Health & Doctor Diagnostics
Inspect your environment for PATH duplicates, missing C++ redistributables, or conflicting external tools:
```powershell
pvm doctor --fix
```

---

## 🛠️ Advanced Feature Showcase

### 1. Interactive `php.ini` & Extension Manager (`pvm ini`)
You never need to hunt down or manually edit text files inside deep system directories again. PVM parses, edits, and manages your active `php.ini` directly from the command line:

```powershell
# List all PHP extensions and see which ones are Enabled vs Disabled
pvm ini ls

# Enable or disable an extension instantly across your active PHP runtime
pvm ini enable curl
pvm ini enable mbstring
pvm ini disable xdebug

# Inspect any configuration directive value
pvm ini get memory_limit

# Update a configuration directive cleanly
pvm ini set memory_limit 1024M
pvm ini set upload_max_filesize 64M

# Open the active php.ini directly in your default system text editor
pvm ini open
```

---

### 2. Semantic Version Aliases (`pvm alias`)
Create memorable shortcuts for specific project requirements or workflow defaults:

```powershell
# Create an alias pointing 'default' or 'legacy' to a specific PHP build
pvm alias default 8.4.23
pvm alias legacy 7.4.33

# Switch versions using your custom alias names!
pvm use default
pvm use legacy

# List all configured aliases or remove outdated ones
pvm alias
pvm alias legacy --remove
```

---

### 3. Automated Doctor & PATH Hygiene Diagnostics (`pvm doctor` & `pvm env`)
If your terminal ever executes the wrong `php.exe` because an old tool (like XAMPP, Laragon, or `C:\Program Files\PHP`) is shadowing PVM, PVM fixes it automatically:

```powershell
# Run system diagnostic checks
pvm doctor

# Automatically repair PATH duplicates and resolve junction problems
pvm doctor --fix

# Inspect PATH hygiene and conflict status
pvm env --check

# Clean duplicate PATH entries while preserving order
pvm env --clean
```

---

### 4. Shell Argument Autocompletion (`pvm completion`)
Generate native tab autocompletion scripts for your favorite shell:

```powershell
# PowerShell Tab Completion
pvm completion powershell >> $PROFILE

# Bash Tab Completion (Linux / macOS / Git Bash)
pvm completion bash >> ~/.bashrc
```

---

### 5. In-Place Self-Updating (`pvm self-update`)
Upgrade `pvm` (`pvm.exe`) itself to the latest release directly from GitHub without opening a browser:

```powershell
# Check if a new release version is available on GitHub
pvm self-update --check

# Download and atomically replace your active pvm binary in place
pvm self-update
```

---

### 6. Complete Uninstallation (`pvm self-uninstall`)
If you ever want to completely remove PVM, all downloaded PHP binaries, directory junctions, and shell `PATH` entries from your device without leaving a single trace:

#### Option A: Using the built-in PVM command (Any OS)
```powershell
pvm self-uninstall -y
```

#### Option B: One-Line PowerShell Uninstaller (`uninstall.ps1` for Windows)
```powershell
irm https://raw.githubusercontent.com/hasanhawary/phpvm/main/uninstall.ps1 | iex
```

#### Option C: One-Line Bash Uninstaller (`uninstall.sh` for macOS / Linux)
```bash
curl -o- https://raw.githubusercontent.com/hasanhawary/phpvm/main/uninstall.sh | bash
```

---

## 📖 Complete Command Reference (`pvm --help`)

| Command | Alias | Syntax | Description |
| :--- | :--- | :--- | :--- |
| **`list`** | `ls` | `pvm list [--remote]` | List installed local PHP versions or fetch available versions from official mirrors. |
| **`current`** | | `pvm current` | Display the active PHP version, binary path, and runtime status. |
| **`use`** | | `pvm use <version>` | Switch the active PHP version by atomically updating your system directory link. |
| **`install`** | | `pvm install <version> [--force]` | Download, verify SHA256 checksum, and install a PHP version from official mirrors. |
| **`uninstall`**| | `pvm uninstall <version>` | Remove an installed PHP version from your local disk (`~/.pvm/versions`). |
| **`env`** | | `pvm env [--check\|--clean\|--ps1]` | Inspect `PATH` hygiene (`--check`), clean duplicates (`--clean`), or output evaluation scripts (`--ps1`, `--cmd`). |
| **`ini`** | | `pvm ini <ls\|enable\|disable\|get\|set\|open>` | Inspect or modify the active `php.ini` file's extensions and configuration directives. |
| **`alias`** | | `pvm alias [name] [target] [--remove]` | Create, list, or delete (`--remove`) semantic version aliases. |
| **`doctor`** | | `pvm doctor [--fix]` | Run system health audits. Pass `--fix` to automatically remediate issues. |
| **`self-update`**| `update` | `pvm self-update [--check]` | Check for updates (`--check`) or self-update PVM directly from GitHub Releases. |
| **`self-uninstall`**| `uninstall-self` | `pvm self-uninstall [-y]` | Completely uninstall PVM, all PHP binaries, directory junctions, and shell `PATH` entries. |
| **`completion`** | | `pvm completion <powershell\|cmd\|bash>` | Output shell parameter argument completion scripts. |

---

## 📂 System Directory & File Structure

PVM stores all configuration and version binaries cleanly inside your user profile (`~/.pvm` or `%USERPROFILE%\.pvm`), ensuring zero permission conflicts across any OS:

```text
~/.pvm/
├── bin/
│   ├── pvm.exe          # Main compiled PVM CLI binary
│   └── pvm-setup.exe    # Standalone GUI installer wizard
├── current/             # Active directory link -> ~/.pvm/versions/<active_version>
├── versions/            # Extracted PHP version directories
│   ├── 8.2.15/
│   ├── 8.3.3/
│   └── 8.4.23/
└── aliases.json         # Semantic version shortcuts storage
```

---

## 🏗️ Clean Architecture & Development Specs

PVM is engineered using strict **Clean Architecture (Hexagonal / Ports & Adapters)** and **SOLID** domain-driven design principles:

```text
src/
├── Pvm.Core/            # Pure Domain Models, Value Objects, Port Interfaces (Zero OS/Infrastructure dependencies)
├── Pvm.Application/     # Use Case Orchestrators, Version Resolution, Ini Editing, PATH Management
├── Pvm.Infrastructure/  # Windows & POSIX Platform Adapters (Junctions, Symlinks, Registry, Web Downloads)
├── Pvm.Cli/             # Spectre.Console CLI Presentation Layer & Command Routing
└── Pvm.Setup/           # Windows GUI Setup Wizard & Transactional Installer Engine
```

### Running Unit Tests Locally
```powershell
dotnet test --configuration Release
```
*All 57 core, application, and infrastructure architectural unit tests execute in under 2 seconds.*

---

<div align="center">

**Engineered with ❤️ for PHP developers on Windows, macOS & Linux.**  
[Report an Issue](https://github.com/hasanhawary/phpvm/issues) • [Contribute to PVM](https://github.com/hasanhawary/phpvm/pulls) • [View Releases](https://github.com/hasanhawary/phpvm/releases)

</div>
