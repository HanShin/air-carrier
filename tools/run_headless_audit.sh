#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
audit_binary="/tmp/aetherark-headless.exe"

cd "$project_root"
csc -nologo -langversion:latest \
  -out:"$audit_binary" \
  Assets/_Project/Scripts/Core/*.cs \
  Assets/_Project/Scripts/Content/*.cs \
  tools/HeadlessPlaythrough.cs

mono "$audit_binary" 100 Standard
mono "$audit_binary" 100 Story
mono "$audit_binary" 1 Story 32838
