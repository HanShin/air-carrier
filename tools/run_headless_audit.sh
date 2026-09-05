#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
audit_dir="$(mktemp -d "${TMPDIR:-/tmp}/aetherark-audit.XXXXXX")"
audit_binary="$audit_dir/audit.exe"
trap 'rm -f "$audit_binary"; rmdir "$audit_dir"' EXIT

cd "$project_root"
csc -nologo -langversion:latest \
  -out:"$audit_binary" \
  Assets/_Project/Scripts/Core/*.cs \
  Assets/_Project/Scripts/Content/*.cs \
  tools/HeadlessPlaythrough.cs tools/AuditWingPolicy.cs tools/AuditWingPolicyTests.cs

if [ "$#" -gt 0 ]; then
  mono "$audit_binary" "$@"
else
  mono "$audit_binary" --self-test
  mono "$audit_binary" 100 Standard
  mono "$audit_binary" 100 Story
  mono "$audit_binary" --tutorial
fi
