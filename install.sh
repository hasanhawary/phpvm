#!/usr/bin/env bash

# pvm — PHP Version Manager One-Line Bash Installer
# Usage:
#   curl -o- https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.sh | bash
#   wget -qO- https://raw.githubusercontent.com/hasanhawary/phpvm/main/install.sh | bash

set -e

REPO="hasanhawary/phpvm"
INSTALL_DIR="$HOME/.pvm/bin"
ZIP_URL="https://github.com/$REPO/releases/latest/download/pvm-win-x64.zip"

echo "========================================================"
echo "      PVM (PHP Version Manager) Bash Installer          "
echo "========================================================"

OS_TYPE="$(uname -s)"
echo "[1/4] Detected Operating System: $OS_TYPE"

mkdir -p "$INSTALL_DIR"
mkdir -p "$HOME/.pvm/versions"
mkdir -p "$HOME/.pvm/current"

case "$OS_TYPE" in
    MINGW*|MSYS*|CYGWIN*|Windows_NT)
        echo "[2/4] Downloading Windows x64 standalone binary ($ZIP_URL)..."
        TEMP_ZIP="$HOME/.pvm/temp_pvm.zip"
        if command -v curl >/dev/null 2>&1; then
            curl -fSL "$ZIP_URL" -o "$TEMP_ZIP" || {
                echo "Notice: Online release zip not found. Looking for local build in ./dist/pvm.exe..."
                if [ -f "./dist/pvm.exe" ]; then
                    cp "./dist/pvm.exe" "$INSTALL_DIR/pvm.exe"
                else
                    echo "Error: Could not download release archive and no local ./dist/pvm.exe exists."
                    exit 1
                fi
            }
        elif command -v wget >/dev/null 2>&1; then
            wget -q "$ZIP_URL" -O "$TEMP_ZIP" || {
                if [ -f "./dist/pvm.exe" ]; then
                    cp "./dist/pvm.exe" "$INSTALL_DIR/pvm.exe"
                else
                    echo "Error: Could not download release archive and no local ./dist/pvm.exe exists."
                    exit 1
                fi
            }
        else
            echo "Error: Neither curl nor wget was found. Please install one and try again."
            exit 1
        fi

        if [ -f "$TEMP_ZIP" ]; then
            echo "[3/4] Extracting PVM archive into $INSTALL_DIR..."
            if command -v unzip >/dev/null 2>&1; then
                unzip -o -q "$TEMP_ZIP" -d "$INSTALL_DIR"
            elif command -v powershell >/dev/null 2>&1; then
                powershell -Command "Expand-Archive -Path '$TEMP_ZIP' -DestinationPath '$INSTALL_DIR' -Force"
            fi
            rm -f "$TEMP_ZIP"
        fi
        ;;
    Darwin|Linux)
        echo "[2/4] Cross-platform environment ($OS_TYPE) detected."
        echo "Checking for local .NET 8 SDK / binary build..."
        if [ -f "./dist/pvm" ]; then
            cp "./dist/pvm" "$INSTALL_DIR/pvm"
            chmod +x "$INSTALL_DIR/pvm"
        elif [ -f "./dist/pvm.exe" ]; then
            # WSL or Wine support
            cp "./dist/pvm.exe" "$INSTALL_DIR/pvm.exe"
            chmod +x "$INSTALL_DIR/pvm.exe"
        else
            echo "Note: To compile natively for $OS_TYPE, run: dotnet publish -c Release -r $(uname -s | tr '[:upper:]' '[:lower:]')-x64"
        fi
        ;;
    *)
        echo "Warning: Unsupported OS ($OS_TYPE). Attempting default installation..."
        ;;
esac

echo "[4/4] Configuring shell environment variables ($HOME/.bashrc, $HOME/.zshrc)..."
SHELL_CONFIGS=("$HOME/.bashrc" "$HOME/.zshrc" "$HOME/.bash_profile" "$HOME/.profile")
PATH_ENTRY='export PATH="$HOME/.pvm/bin:$PATH"'

for CONFIG in "${SHELL_CONFIGS[@]}"; do
    if [ -f "$CONFIG" ]; then
        if ! grep -q "$HOME/.pvm/bin" "$CONFIG"; then
            echo "" >> "$CONFIG"
            echo "# PVM (PHP Version Manager)" >> "$CONFIG"
            echo "$PATH_ENTRY" >> "$CONFIG"
            echo "Added $INSTALL_DIR to $CONFIG"
        else
            echo "$INSTALL_DIR already present in $CONFIG"
        fi
    fi
done

echo ""
echo "========================================================"
echo " INSTALLATION SUCCESSFUL! 🎉"
echo " PVM binaries installed in: $INSTALL_DIR"
echo "========================================================"
echo ""
echo "Please restart your terminal or run the following command to update your PATH:"
echo "  export PATH=\"\$HOME/.pvm/bin:\$PATH\""
echo ""
echo "Verify installation by running: pvm --help"
