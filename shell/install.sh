#!/usr/bin/env bash
# ==============================================================================
# Universal PVM Installer (Bash / POSIX / macOS / Linux / Git Bash)
# Repository: https://github.com/hasanhawary/phpvm
# License: MIT
# ==============================================================================

set -e

# Colors
C_RESET='\033[0m'
C_BOLD='\033[1m'
C_GREEN='\033[0;32m'
C_CYAN='\033[0;36m'
C_YELLOW='\033[1;33m'
C_RED='\033[0;31m'

print_info() { printf "${C_CYAN}[INFO]${C_RESET} %s\n" "$1"; }
print_ok()   { printf "${C_GREEN}[OK]${C_RESET}   %s\n" "$1"; }
print_warn() { printf "${C_YELLOW}[WARN]${C_RESET} %s\n" "$1"; }
print_err()  { printf "${C_RED}[ERR]${C_RESET}  %s\n" "$1" >&2; }

# Paths
PVM_HOME="${PVM_HOME:-$HOME/.pvm}"
PVM_BIN="$PVM_HOME/bin"
PVM_VERSIONS="$PVM_HOME/versions"
PVM_CURRENT="$PVM_HOME/current"

RAW_BASE_URL="https://raw.githubusercontent.com/hasanhawary/phpvm/main"

print_info "Installing Universal PVM (PHP Version Manager) into ${C_BOLD}$PVM_HOME${C_RESET}..."

mkdir -p "$PVM_BIN" "$PVM_VERSIONS"

# Download or copy from local repo if running from cloned dir
SCRIPT_DIR=""
if [ -n "$0" ] && [ -f "$0" ] && [ "$0" != "bash" ] && [ "$0" != "sh" ] && [ "$0" != "-bash" ] && [ "$0" != "-sh" ]; then
    SCRIPT_DIR="$(cd "$(dirname "$0")" 2>/dev/null && pwd || true)"
fi

if [ -n "$SCRIPT_DIR" ] && [ -f "$SCRIPT_DIR/pvm" ]; then
    print_info "Copying local shell scripts to $PVM_BIN..."
    cp -f "$SCRIPT_DIR/pvm" "$PVM_BIN/pvm"
    chmod +x "$PVM_BIN/pvm"
    if [ -f "$SCRIPT_DIR/pvm.ps1" ]; then cp -f "$SCRIPT_DIR/pvm.ps1" "$PVM_BIN/pvm.ps1"; fi
    if [ -f "$SCRIPT_DIR/pvm.cmd" ]; then cp -f "$SCRIPT_DIR/pvm.cmd" "$PVM_BIN/pvm.cmd"; fi
else
    print_info "Downloading latest PVM scripts from GitHub..."
    if command -v curl >/dev/null 2>&1; then
        curl -fsSL "$RAW_BASE_URL/pvm" -o "$PVM_BIN/pvm"
        curl -fsSL "$RAW_BASE_URL/pvm.ps1" -o "$PVM_BIN/pvm.ps1" 2>/dev/null || true
        curl -fsSL "$RAW_BASE_URL/pvm.cmd" -o "$PVM_BIN/pvm.cmd" 2>/dev/null || true
    elif command -v wget >/dev/null 2>&1; then
        wget -q "$RAW_BASE_URL/pvm" -O "$PVM_BIN/pvm"
        wget -q "$RAW_BASE_URL/pvm.ps1" -O "$PVM_BIN/pvm.ps1" 2>/dev/null || true
        wget -q "$RAW_BASE_URL/pvm.cmd" -O "$PVM_BIN/pvm.cmd" 2>/dev/null || true
    else
        print_err "Neither curl nor wget found. Cannot download PVM."
        exit 1
    fi
    chmod +x "$PVM_BIN/pvm"
fi

# Clean any legacy .NET executables so shell scripts take precedence
rm -f "$PVM_BIN"/pvm*.exe* 2>/dev/null || true

print_ok "PVM binary installed to ${C_BOLD}$PVM_BIN/pvm${C_RESET}"

# Setup PATH configuration in shell profiles
setup_profile() {
    local profile_file="$1"
    if [ -f "$profile_file" ]; then
        if ! grep -q "PVM_HOME" "$profile_file"; then
            print_info "Adding PVM environment configuration to $profile_file..."
            cat >> "$profile_file" << 'EOF'

# PVM (PHP Version Manager) Environment
export PVM_HOME="$HOME/.pvm"
export PATH="$PVM_HOME/current:$PVM_HOME/bin:$PATH"
EOF
            print_ok "Updated $profile_file"
        else
            print_ok "PVM already configured in $profile_file"
        fi
    fi
}

# Detect active shell profiles
if [ -n "$BASH_VERSION" ] || [ -f "$HOME/.bashrc" ] || [ -f "$HOME/.bash_profile" ]; then
    setup_profile "$HOME/.bashrc"
    setup_profile "$HOME/.bash_profile"
fi

if [ -n "$ZSH_VERSION" ] || [ -f "$HOME/.zshrc" ]; then
    setup_profile "$HOME/.zshrc"
fi

setup_profile "$HOME/.profile"

# Summary
printf "\n${C_BOLD}==============================================================================${C_RESET}\n"
printf "${C_GREEN}✔ PVM Installation Complete!${C_RESET}\n"
printf "${C_BOLD}==============================================================================${C_RESET}\n\n"
printf "To get started right now in your current terminal session, run:\n"
printf "  ${C_BOLD}export PVM_HOME=\"\$HOME/.pvm\" && export PATH=\"\$PVM_HOME/current:\$PVM_HOME/bin:\$PATH\"${C_RESET}\n\n"
printf "Or restart your terminal and run:\n"
printf "  ${C_CYAN}pvm list --remote${C_RESET}      (View available PHP versions to install)\n"
printf "  ${C_CYAN}pvm install 8.4${C_RESET}        (Install PHP 8.4)\n"
printf "  ${C_CYAN}pvm use 8.4${C_RESET}            (Switch active PHP runtime globally)\n\n"
