#!/usr/bin/env python3
"""Compare audit-only wing policies on identical campaign seeds. Requires Mono/csc, no packages.

Writes per-seed records plus aggregate and paired outcomes as JSON. This is not a human playtest.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
import hashlib
import json
import math
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
POLICIES = ("legacy", "once", "always", "healthy", "adaptive")
FLAGSHIPS = ("ship_vanguard", "ship_bastion", "ship_zephyr")


def sources():
    return sorted((ROOT / "Assets/_Project/Scripts/Core").glob("*.cs")) + sorted(
        (ROOT / "Assets/_Project/Scripts/Content").glob("*.cs")) + [
        ROOT / "tools" / name for name in ("HeadlessPlaythrough.cs", "AuditWingPolicy.cs", "AuditWingPolicyTests.cs")]


def compile_audit(destination):
    subprocess.run(["csc", "-nologo", "-langversion:latest", f"-out:{destination}", *map(str, sources())], check=True, cwd=ROOT)


def records_from(stdout):
    records = []
    for line in stdout.splitlines():
        if not line.startswith("RUN "):
            continue
        row = {}
        for part in line[4:].split():
            key, value = part.split("=", 1)
            row[key] = float(value) if "." in value else int(value)
        records.append(row)
    return records


def run_audit(binary, args, allow_timeouts=False):
    result = subprocess.run(["mono", str(binary), *args, "--records"], capture_output=True, text=True, cwd=ROOT, timeout=300)
    # 1 = no wins, 2 = below the historical 25% threshold; both are valid measurement outcomes.
    if result.returncode not in ((0, 1, 2, 3) if allow_timeouts else (0, 1, 2)):
        raise RuntimeError(f"Audit failed ({result.returncode}): {' '.join(args)}\n{result.stdout}\n{result.stderr}")
    records = records_from(result.stdout)
    if not records:
        raise RuntimeError(f"Audit returned no records: {' '.join(args)}\n{result.stderr}")
    if result.returncode == 3 and not any(row["stalemate"] for row in records):
        raise RuntimeError("Timeout exit code without a flagged record")
    return records


def summarize(records):
    n = len(records)
    wins = sum(row["victory"] for row in records)
    battles = sum(row["battles"] for row in records)
    # Wilson interval describes this sample, not model validity or a human success-rate estimate.
    z = 1.96
    center = (wins / n + z * z / (2 * n)) / (1 + z * z / n)
    radius = z * math.sqrt((wins / n * (1 - wins / n) + z * z / (4 * n)) / n) / (1 + z * z / n)
    result = {"wins": wins, "runs": n, "win_percent": round(100 * wins / n, 2),
              "wilson_95_percent": [round(100 * (center - radius), 2), round(100 * (center + radius), 2)], "battles": battles}
    result["timeouts"] = sum(row.get("stalemate", 0) for row in records)
    result["losses"] = n - wins - result["timeouts"]
    if result["timeouts"]:
        result["wilson_95_percent"] = None  # Do not present an interval that counts unfinished runs as losses.
        result["win_percent_bounds"] = [round(100 * wins / n, 2), round(100 * (wins + result["timeouts"]) / n, 2)]
    for key in ("sorties", "wing_ordnance", "airframes_lost", "raids", "recon", "dry_seconds"):
        result[key + "_per_battle"] = round(sum(row[key] for row in records) / max(1, battles), 3)
    for key in ("pilot_deaths", "wings_destroyed"):
        result[key] = sum(row[key] for row in records)
    result["reached_region"] = [sum(row["reached"] >= region for row in records) for region in range(1, 7)]
    return result


def compare(binary, runs, difficulty, base_seed, flagship, policy, combat_cap=420):
    records = run_audit(binary, [str(runs), difficulty, str(base_seed), f"--flagship={flagship}", f"--wings={policy}", f"--combat-cap={combat_cap}"], allow_timeouts=True)
    expected = [base_seed + i * 7919 for i in range(runs)]
    if [row["seed"] for row in records] != expected or any(row["regions"] != 6 for row in records):
        raise RuntimeError("Invalid campaign cohort: wrong seeds or tutorial contamination")
    return {"flagship": flagship, "policy": policy, "summary": summarize(records), "records": records}


def paired_outcomes(baseline, candidate):
    if [row["seed"] for row in baseline] != [row["seed"] for row in candidate]:
        raise ValueError("Paired outcomes require identical ordered seeds")
    decided = [(a, b) for a, b in zip(baseline, candidate) if not a.get("stalemate", 0) and not b.get("stalemate", 0)]
    return {"gained_wins": sum(not a["victory"] and bool(b["victory"]) for a, b in decided),
            "lost_wins": sum(bool(a["victory"]) and not b["victory"] for a, b in decided),
            "inconclusive_pairs": len(baseline) - len(decided)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--runs", type=int, default=100)
    parser.add_argument("--base-seed", type=int, default=17000)
    parser.add_argument("--difficulty", choices=("Story", "Standard", "Harsh"), default="Standard")
    parser.add_argument("--combat-cap", type=float, default=420)
    parser.add_argument("--flagships", choices=FLAGSHIPS, nargs="+", default=list(FLAGSHIPS))
    parser.add_argument("--policies", choices=POLICIES, nargs="+", default=list(POLICIES))
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if not 1 <= args.runs <= 10000:
        parser.error("--runs must be 1..10000")
    if not math.isfinite(args.combat_cap) or not 0.1 <= args.combat_cap <= 3600:
        parser.error("--combat-cap must be 0.1..3600 seconds")
    if args.base_seed < -2147483648 or args.base_seed + (args.runs - 1) * 7919 > 2147483647:
        parser.error("seed range must fit signed 32-bit integers")
    if len(set(args.flagships)) != len(args.flagships) or len(set(args.policies)) != len(args.policies):
        parser.error("duplicate flagships/policies would bias the comparison")
    digest = hashlib.sha256()
    for path in sources() + [Path(__file__).resolve()]:
        digest.update(str(path.relative_to(ROOT)).encode())
        digest.update(path.read_bytes())
    groups = []
    with tempfile.TemporaryDirectory(prefix="aether-wing-audit-") as temporary:
        binary = Path(temporary) / "audit.exe"
        compile_audit(binary)
        with ThreadPoolExecutor(max_workers=3) as pool:
            futures = [pool.submit(compare, binary, args.runs, args.difficulty, args.base_seed, flagship, policy, args.combat_cap)
                       for flagship in args.flagships for policy in args.policies]
            for future in as_completed(futures):
                group = future.result()
                groups.append(group)
                s = group["summary"]
                print(f"{group['flagship']:14} {group['policy']:8} {s['wins']:3}/{s['runs']} wins, "
                      f"sorties/battle {s['sorties_per_battle']:.2f}, ammo/battle {s['wing_ordnance_per_battle']:.2f}, "
                      f"timeouts {s['timeouts']}", flush=True)
    groups.sort(key=lambda group: (FLAGSHIPS.index(group["flagship"]), POLICIES.index(group["policy"])))
    for group in groups:
        baseline = next((other for other in groups if other["flagship"] == group["flagship"] and other["policy"] == "legacy"), None)
        if baseline:
            group["paired_vs_legacy"] = paired_outcomes(baseline["records"], group["records"])
    report = {"schema_version": 1, "source_sha256": digest.hexdigest(), "difficulty": args.difficulty,
              "base_seed": args.base_seed, "seed_stride": 7919, "runs_per_group": args.runs,
              "tutorial": False, "strategy": "standard", "combat_time_cap": args.combat_cap,
              "note": "Autoplayer comparison, not human win rates. Ship values are unchanged. Timeouts are inconclusive, not losses.",
              "groups": groups}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Report: {args.output}")
    if any(group["summary"]["timeouts"] for group in groups):
        raise SystemExit(3)  # Keep the evidence, but never make a timed-out comparison look like a clean pass.


if __name__ == "__main__":
    main()
