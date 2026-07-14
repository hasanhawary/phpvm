#!/usr/bin/env bash
# ==============================================================================
# Universal PVM Automated Test Suite (POSIX / Bash)
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

pass_count=0
fail_count=0

log_test() { printf "\n${C_CYAN}[TEST]${C_RESET} ${C_BOLD}%s${C_RESET}\n" "$1"; }
log_pass() { printf "${C_GREEN}✔ PASS: %s${C_RESET}\n" "$1"; pass_count=$((pass_count + 1)); }
log_fail() { printf "${C_RED}✖ FAIL: %s${C_RESET}\n" "$1"; fail_count=$((fail_count + 1)); }

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PVM_BIN="$SCRIPT_DIR/pvm"

if [ ! -f "$PVM_BIN" ]; then
    log_fail "pvm script not found at $PVM_BIN"
    exit 1
fi

log_test "1. Verify --version command"
if "$PVM_BIN" --version | grep -q "1.1.0-shell"; then
    log_pass "--version reported expected version"
else
    log_fail "--version check failed"
fi

log_test "2. Verify --help command"
if "$PVM_BIN" --help | grep -q "Usage:"; then
    log_pass "--help output is valid"
else
    log_fail "--help output invalid"
fi

log_test "3. Verify list command"
if "$PVM_BIN" list | grep -q "8."; then
    log_pass "list command listed installed PHP versions"
else
    log_fail "list command did not return expected versions"
fi

log_test "4. Verify current command"
if "$PVM_BIN" current | grep -q "Active PVM Runtime Status:"; then
    log_pass "current command displayed runtime status and CLI output"
else
    log_fail "current command check failed"
fi

log_test "5. Verify alias creation, lookup, and removal"
"$PVM_BIN" alias testalias 8.4 >/dev/null
if "$PVM_BIN" alias | grep -q "testalias"; then
    log_pass "Created and listed 'testalias'"
else
    log_fail "Failed to verify alias creation"
fi
"$PVM_BIN" alias -r testalias >/dev/null
if ! "$PVM_BIN" alias | grep -q "testalias"; then
    log_pass "Successfully removed 'testalias'"
else
    log_fail "Alias removal failed"
fi

log_test "6. Verify ini ls command"
if "$PVM_BIN" ini ls | grep -q "curl\|mbstring\|openssl"; then
    log_pass "ini ls successfully enumerated extensions"
else
    log_fail "ini ls check failed"
fi

log_test "7. Verify env --check command"
if "$PVM_BIN" env --check | grep -q "PVM PATH Environment Audit:"; then
    log_pass "env --check successfully audited environment"
else
    log_fail "env --check check failed"
fi

log_test "8. Verify doctor health check"
if "$PVM_BIN" doctor | grep -q "Check 1: PVM Directory Structure is intact"; then
    log_pass "doctor health check completed successfully"
else
    log_fail "doctor health check failed"
fi

printf "\n${C_BOLD}==============================================================================${C_RESET}\n"
if [ "$fail_count" -eq 0 ]; then
    printf "${C_GREEN}✔ All %d Bash tests PASSED cleanly with zero errors!${C_RESET}\n" "$pass_count"
    printf "${C_BOLD}==============================================================================${C_RESET}\n\n"
    exit 0
else
    printf "${C_RED}✖ Test Suite completed with %d failed test(s) (%d passed).${C_RESET}\n" "$fail_count" "$pass_count"
    printf "${C_BOLD}==============================================================================${C_RESET}\n\n"
    exit 1
fi
