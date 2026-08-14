#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
output="/tmp/aetherark-runtime-smoke.dll"
cd "$project_root"

csc -nologo -langversion:latest -target:library -out:"$output" \
  tools/UnityCompileStubs.cs \
  Assets/_Project/Scripts/Core/*.cs \
  Assets/_Project/Scripts/Content/*.cs \
  Assets/_Project/Scripts/Runtime/*.cs

echo "Runtime compile smoke passed: $output"
