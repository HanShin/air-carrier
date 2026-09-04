#!/usr/bin/env python3
"""Fast repository validation that does not require the Unity editor."""

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message: str) -> None:
    print(f"ERROR: {message}")
    raise SystemExit(1)


def validate_json() -> None:
    paths = [ROOT / "Packages/manifest.json"] + list(ROOT.glob("Assets/**/*.asmdef"))
    for path in paths:
        try:
            json.loads(path.read_text(encoding="utf-8"))
        except Exception as exc:
            fail(f"invalid JSON in {path.relative_to(ROOT)}: {exc}")
    print(f"OK: {len(paths)} JSON project files")


def strip_csharp(source: str) -> str:
    source = re.sub(r'@?"(?:""|\\.|[^"\\])*"', '""', source)
    source = re.sub(r"//.*?$", "", source, flags=re.MULTILINE)
    source = re.sub(r"/\*.*?\*/", "", source, flags=re.DOTALL)
    return source


def validate_braces() -> None:
    paths = list(ROOT.glob("Assets/**/*.cs"))
    for path in paths:
        source = strip_csharp(path.read_text(encoding="utf-8"))
        for opening, closing in [("{", "}"), ("(", ")"), ("[", "]")]:
            depth = 0
            for char in source:
                if char == opening:
                    depth += 1
                elif char == closing:
                    depth -= 1
                if depth < 0:
                    fail(f"unbalanced {opening}{closing} in {path.relative_to(ROOT)}")
            if depth != 0:
                fail(f"unbalanced {opening}{closing} in {path.relative_to(ROOT)}")
    print(f"OK: delimiter balance across {len(paths)} C# files")


def validate_localization() -> None:
    localization = (ROOT / "Assets/_Project/Scripts/Content/LocalizationService.cs").read_text(encoding="utf-8")
    defined = set(re.findall(r'Add\("([a-z0-9_.]+)"\s*,', localization))
    used = set()
    for path in ROOT.glob("Assets/_Project/Scripts/**/*.cs"):
        if path.name == "LocalizationService.cs":
            continue
        source = path.read_text(encoding="utf-8")
        used.update(re.findall(r'"((?:command|ui|log|system|squadron|node|weather|encounter|choice|result|tutorial)\.[a-z0-9_.]+)"', source))
    missing = sorted(used - defined)
    if missing:
        fail("missing localization keys: " + ", ".join(missing))
    print(f"OK: {len(used)} referenced localization keys ({len(defined)} defined)")


def validate_assets() -> None:
    required = [
        ROOT / "Assets/Scenes/Main.unity",
        ROOT / "Assets/_Project/Resources/Art/sky_storm_background.png",
        ROOT / "ProjectSettings/EditorBuildSettings.asset",
    ]
    for path in required:
        if not path.exists() or path.stat().st_size == 0:
            fail(f"missing required asset: {path.relative_to(ROOT)}")
    ship_art = ROOT / "Assets/_Project/Resources/Art/Ships"
    expected_silhouettes = {
        "enemy_cutter", "enemy_carrier", "enemy_scout", "enemy_boarder", "enemy_lancer",
        "enemy_minelayer", "enemy_firebrand", "enemy_cruiser", "enemy_monitor",
        "enemy_dreadnought", "enemy_hive", "enemy_wraith", "enemy_warden",
    }
    present = {path.stem for path in ship_art.glob("*.png") if path.stat().st_size > 0}
    missing = sorted(expected_silhouettes - present)
    if missing:
        fail("missing enemy silhouette art: " + ", ".join(missing))
    expected_icons = {
        "Modules": {"hull", "core", "weapons", "deck", "sensors", "engineering", "bridge", "marines"},
        "Weapons": {"cannon", "lance", "piercer", "missile", "flak", "incendiary", "breacher"},
        "Wings": {"interceptor", "bomber", "escort", "recon", "assault"},
    }
    for folder, expected in expected_icons.items():
        icon_folder = ROOT / "Assets/_Project/Resources/Art/Icons" / folder
        present_icons = {path.stem for path in icon_folder.glob("*.png") if path.stat().st_size > 0}
        missing_icons = sorted(expected - present_icons)
        if missing_icons:
            fail(f"missing {folder.lower()} icons: " + ", ".join(missing_icons))
    print(f"OK: required scene, settings, background, {len(expected_silhouettes)} ship silhouettes, and 20 equipment icons")


def main() -> None:
    validate_json()
    validate_braces()
    validate_localization()
    validate_assets()
    print("Repository validation passed.")


if __name__ == "__main__":
    main()
