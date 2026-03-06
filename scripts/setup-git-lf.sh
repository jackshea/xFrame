#!/usr/bin/env bash
set -euo pipefail

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "[ERROR] Not inside a Git repository." >&2
    exit 1
fi

if ! git config --local core.autocrlf false; then
    echo "[ERROR] Failed to write local Git configuration." >&2
    exit 1
fi

if ! git config --local core.eol lf; then
    echo "[ERROR] Failed to write local Git configuration." >&2
    exit 1
fi

if ! git config --local core.safecrlf true; then
    echo "[ERROR] Failed to write local Git configuration." >&2
    exit 1
fi

echo "[OK] Local Git line-ending settings applied:"
echo "  core.autocrlf ="
git config --local --get core.autocrlf
echo "  core.eol ="
git config --local --get core.eol
echo "  core.safecrlf ="
git config --local --get core.safecrlf

echo
echo 'Optional: run "git add --renormalize ." to normalize tracked files to LF.'
