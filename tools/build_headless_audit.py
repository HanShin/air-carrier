#!/usr/bin/env python3
"""Compile the shared audit sources and embed their fingerprint (Python 3 + Mono/csc, no packages)."""
import hashlib
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def sources():
    return sorted((ROOT / "Assets/_Project/Scripts/Core").glob("*.cs")) + sorted(
        (ROOT / "Assets/_Project/Scripts/Content").glob("*.cs")) + [
        ROOT / "Assets/_Project/Scripts/Runtime/CombatSnapshotFile.cs"] + [
        ROOT / "tools" / name for name in (
            "HeadlessPlaythrough.cs", "AuditWingPolicy.cs", "AuditWingPolicyTests.cs", "AuditCheckpointExport.cs")]


def source_digest():
    digest = hashlib.sha256()
    for path in sources() + [Path(__file__).resolve()]:
        digest.update(path.relative_to(ROOT).as_posix().encode("utf-8") + b"\0")
        digest.update(path.read_bytes() + b"\0")
    return digest.hexdigest()


def compile_audit(destination):
    with tempfile.TemporaryDirectory(prefix="aether-audit-build-") as directory:
        metadata = Path(directory) / "AuditBuild.cs"
        metadata.write_text('internal static class AuditBuild { public const string SourceSha256 = "' +
                            source_digest() + '"; }\n', encoding="utf-8")
        subprocess.run(["csc", "-nologo", "-langversion:latest", f"-out:{Path(destination).resolve()}",
                        *map(str, sources()), str(metadata)], check=True, cwd=ROOT)


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit("Usage: python3 tools/build_headless_audit.py /path/to/audit.exe")
    compile_audit(sys.argv[1])
