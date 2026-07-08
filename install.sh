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
        ARCH="$(uname -m)"
        if [ "$OS_TYPE" = "Darwin" ]; then
            if [ "$ARCH" = "arm64" ]; then
                TARGET_ARCH="osx-arm64"
            else
                TARGET_ARCH="osx-x64"
            fi
        else
            if [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then
                TARGET_ARCH="linux-arm64"
            else
                TARGET_ARCH="linux-x64"
            fi
        fi

        TAR_URL="https://github.com/$REPO/releases/latest/download/pvm-$TARGET_ARCH.tar.gz"
        echo "[2/4] Downloading native cross-platform binary for $OS_TYPE ($TARGET_ARCH) from $TAR_URL..."
        TEMP_TAR="$HOME/.pvm/temp_pvm.tar.gz"

        if command -v curl >/dev/null 2>&1; then
            curl -fSL "$TAR_URL" -o "$TEMP_TAR" || {
                echo "Notice: Online release archive ($TAR_URL) not found yet. Checking local repository build..."
                if [ -f "./dist/pvm" ]; then cp "./dist/pvm" "$INSTALL_DIR/pvm"; chmod +x "$INSTALL_DIR/pvm"; fi
            }
        elif command -v wget >/dev/null 2>&1; then
            wget -q "$TAR_URL" -O "$TEMP_TAR" || {
                if [ -f "./dist/pvm" ]; then cp "./dist/pvm" "$INSTALL_DIR/pvm"; chmod +x "$INSTALL_DIR/pvm"; fi
            }
        fi

        if [ -f "$TEMP_TAR" ]; then
            echo "[3/4] Extracting native binary archive into $INSTALL_DIR..."
            tar -xzf "$TEMP_TAR" -C "$INSTALL_DIR" || true
            chmod +x "$INSTALL_DIR/pvm" 2>/dev/null || true
            rm -f "$TEMP_TAR"
        elif [ -f "$INSTALL_DIR/pvm" ]; then
            chmod +x "$INSTALL_DIR/pvm"
        else
            echo "Note: Release archive not found online. If compiling from source, run: dotnet publish src/Pvm.Cli/Pvm.Cli.csproj -c Release -r $TARGET_ARCH -o $INSTALL_DIR"
        fi
        ;;
    *)
        echo "Warning: Unsupported OS ($OS_TYPE). Attempting default installation..."
        ;;
esac

echo "[4/4] Configuring shell environment variables ($HOME/.bashrc, $HOME/.zshrc)..."
SHELL_CONFIGS=("$HOME/.bashrc" "$HOME/.zshrc" "$HOME/.bash_profile" "$HOME/.profile")
PATH_ENTRY='export PATH="$HOME/.pvm/current:$HOME/.pvm/bin:$PATH"'

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
