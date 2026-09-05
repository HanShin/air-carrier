#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
audit_dir="$(mktemp -d "${TMPDIR:-/tmp}/aetherark-audit.XXXXXX")"
audit_binary="$audit_dir/audit.exe"
trap 'rm -f "$audit_binary"; rmdir "$audit_dir"' EXIT

cd "$project_root"
python3 -B tools/build_headless_audit.py "$audit_binary"

if [ "$#" -gt 0 ]; then
  mono "$audit_binary" "$@"
else
  mono "$audit_binary" --self-test
  mono "$audit_binary" 100 Standard
  mono "$audit_binary" 100 Story
  mono "$audit_binary" --tutorial
fi
