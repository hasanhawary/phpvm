#!/usr/bin/env bash

# PVM (PHP Version Manager) One-Line Bash Uninstaller
# Usage:
#   curl -o- https://raw.githubusercontent.com/hasanhawary/phpvm/main/uninstall.sh | bash
#   wget -qO- https://raw.githubusercontent.com/hasanhawary/phpvm/main/uninstall.sh | bash

set -e

echo "========================================================"
echo "       PVM (PHP Version Manager) Uninstaller            "
echo "========================================================"

INSTALL_DIR="$HOME/.pvm/bin"
PVM_ROOT="$HOME/.pvm"

echo "[1/3] Removing PVM exports from shell config files..."
SHELL_CONFIGS=("$HOME/.bashrc" "$HOME/.zshrc" "$HOME/.bash_profile" "$HOME/.profile")

for CONFIG in "${SHELL_CONFIGS[@]}"; do
    if [ -f "$CONFIG" ]; then
        # Remove lines containing .pvm/bin
        if command -v sed >/dev/null 2>&1; then
            sed -i.bak '/\.pvm\/bin/d' "$CONFIG" 2>/dev/null || sed -i '' '/\.pvm\/bin/d' "$CONFIG" 2>/dev/null || true
            sed -i.bak '/# PVM (PHP Version Manager)/d' "$CONFIG" 2>/dev/null || sed -i '' '/# PVM (PHP Version Manager)/d' "$CONFIG" 2>/dev/null || true
            rm -f "$CONFIG.bak"
            echo "Cleaned $CONFIG"
        fi
    fi
done

echo "[2/3] Terminating active PVM/PHP instances..."
if command -v pkill >/dev/null 2>&1; then
    pkill -f "$PVM_ROOT" 2>/dev/null || true
fi

echo "[3/3] Deleting $PVM_ROOT..."
if [ -d "$PVM_ROOT" ]; then
    rm -rf "$PVM_ROOT"
    echo "Removed $PVM_ROOT successfully."
else
    echo "$PVM_ROOT already removed."
fi

echo ""
echo "========================================================"
echo " UNINSTALLATION SUCCESSFUL! PVM has been removed.      "
echo "========================================================"
echo ""
