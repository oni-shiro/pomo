#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEST="${DEST:-/usr/local/bin}"

mkdir -p "${DEST}"

cd "${PROJECT_ROOT}"
dotnet publish -c Release -o "${PROJECT_ROOT}/build/install"

install -m 755 "${PROJECT_ROOT}/build/install/pomo" "${DEST}/pomo"

echo "Installed pomo to ${DEST}/pomo"
echo "Use: pomo reg"
