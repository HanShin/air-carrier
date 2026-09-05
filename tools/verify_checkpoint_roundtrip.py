#!/usr/bin/env python3
"""Compare all Mono fixture fields with optional Unity EditMode round-trip artifacts."""
import argparse
import json
from pathlib import Path
import struct

FIXTURES = Path(__file__).resolve().parents[1] / "Assets/_Project/Tests/EditMode/Fixtures/audit_v1"


def compare(expected, actual, path="payload"):
    if isinstance(expected, dict):
        assert isinstance(actual, dict) and expected.keys() == actual.keys(), f"{path}: fields differ"
        return sum(compare(value, actual[key], f"{path}.{key}") for key, value in expected.items())
    if isinstance(expected, list):
        assert isinstance(actual, list) and len(expected) == len(actual), f"{path}: list differs"
        return sum(compare(a, b, f"{path}[{index}]") for index, (a, b) in enumerate(zip(expected, actual)))
    if isinstance(expected, (int, float)) and not isinstance(expected, bool):
        assert isinstance(actual, (int, float)) and not isinstance(actual, bool), f"{path}: numeric type differs"
        if isinstance(expected, float) or isinstance(actual, float):
            assert struct.pack("<f", expected) == struct.pack("<f", actual), f"{path}: float32 bits differ: {expected} / {actual}"
        else:
            assert expected == actual, f"{path}: integer differs: {expected} / {actual}"
    elif expected is None and actual == "":
        pass  # JsonUtility normalizes null string fields to empty strings; neither contains data.
    else:
        assert expected == actual, f"{path}: {expected!r} != {actual!r}"
    return 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output_directory", type=Path, help="AETHER_AUDIT_ROUNDTRIP_DIR supplied to Unity EditMode tests")
    args = parser.parse_args()
    for fixture in sorted(FIXTURES.glob("*.json")):
        expected = json.loads(json.loads(fixture.read_text())["payloadJson"])
        actual = json.loads((args.output_directory / fixture.name).read_text())
        print(f"{fixture.name}: {compare(expected, actual)} scalar fields preserved (float32 bit exact)")
