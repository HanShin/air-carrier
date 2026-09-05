#!/usr/bin/env python3
"""Small integration tests for the real headless binary and comparison report contracts."""
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.dont_write_bytecode = True
from compare_wing_policies import compile_audit, paired_outcomes, run_audit, summarize


class WingAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary = tempfile.TemporaryDirectory(prefix="aether-wing-tests-")
        cls.binary = Path(cls.temporary.name) / "audit.exe"
        compile_audit(cls.binary)

    @classmethod
    def tearDownClass(cls):
        cls.temporary.cleanup()

    def test_policy_self_tests(self):
        result = subprocess.run(["mono", str(self.binary), "--self-test"], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Wing policy tests passed: 9", result.stdout)

    def test_tutorial_seed_is_a_full_campaign_unless_explicitly_requested(self):
        campaign = run_audit(self.binary, ["1", "Story", "32838", "--flagship=ship_zephyr"])[0]
        tutorial = run_audit(self.binary, ["--tutorial"])[0]
        self.assertEqual(campaign["regions"], 6)
        self.assertEqual(tutorial["regions"], 1)
        self.assertEqual(tutorial["victory"], 1)
        self.assertEqual(tutorial["jumps"], 7)

    def test_repeated_campaign_records_are_identical(self):
        args = ["2", "Standard", "17000", "--flagship=ship_zephyr", "--wings=adaptive"]
        first = run_audit(self.binary, args)
        self.assertEqual(first, run_audit(self.binary, args))
        self.assertEqual([row["seed"] for row in first], [17000, 24919])
        self.assertTrue(all(row["regions"] == 6 and not row["stalemate"] for row in first))
        self.assertTrue(any(row["recon"] > 0 for row in first))

    def test_cautious_disables_sorties_even_with_adaptive_selected(self):
        row = run_audit(self.binary, ["1", "Standard", "17000", "--wings=adaptive", "--strategy=cautious"])[0]
        self.assertEqual(row["sorties"], 0)
        self.assertEqual(row["wing_ordnance"], 0)

    def test_invalid_modes_and_tutorial_cohorts_fail(self):
        for args in (["--wings=typo"], ["--flagship=ship_typo"], ["--unknown"], ["0"], ["2", "--tutorial"], ["--tutorial", "--flagship=ship_zephyr"]):
            result = subprocess.run(["mono", str(self.binary), *args], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("ArgumentException", result.stderr)
            self.assertNotIn("RUN ", result.stdout)

    def test_paired_report_distinguishes_gains_losses_and_mismatched_seeds(self):
        baseline = [{"seed": 1, "victory": 0}, {"seed": 2, "victory": 1}]
        candidate = [{"seed": 1, "victory": 1}, {"seed": 2, "victory": 0}]
        self.assertEqual(paired_outcomes(baseline, candidate), {"gained_wins": 1, "lost_wins": 1, "inconclusive_pairs": 0})
        with self.assertRaises(ValueError):
            paired_outcomes(baseline, list(reversed(candidate)))
        row = {"victory": 1, "battles": 2, "sorties": 6, "wing_ordnance": 8, "airframes_lost": 1,
               "raids": 1, "recon": 2, "dry_seconds": 4, "pilot_deaths": 0, "wings_destroyed": 0, "reached": 6}
        summary = summarize([row])
        self.assertEqual(summary["sorties_per_battle"], 3)
        self.assertEqual(summary["wing_ordnance_per_battle"], 4)
        self.assertEqual(summary["reached_region"], [1] * 6)
        self.assertEqual(summary["win_percent"], 100)

    def test_timeout_is_inconclusive_not_a_loss_or_paired_gain(self):
        args = ["1", "Standard", "17000", "--combat-cap=0.1"]
        with self.assertRaises(RuntimeError):
            run_audit(self.binary, args)
        rows = run_audit(self.binary, args, allow_timeouts=True)
        summary = summarize(rows)
        self.assertEqual(summary["timeouts"], 1)
        self.assertEqual(summary["losses"], 0)
        self.assertIsNone(summary["wilson_95_percent"])
        self.assertEqual(summary["win_percent_bounds"], [0, 100])
        self.assertEqual(paired_outcomes(rows, rows)["inconclusive_pairs"], 1)

    def test_known_slow_battle_finishes_under_a_longer_diagnostic_cap(self):
        args = ["1", "Story", "373355", "--flagship=ship_vanguard", "--wings=adaptive"]
        short = run_audit(self.binary, args, allow_timeouts=True)[0]
        longer = run_audit(self.binary, args + ["--combat-cap=600"])[0]
        self.assertEqual(short["stalemate"], 1)
        self.assertEqual(longer["stalemate"], 0)
        self.assertGreater(longer["battles"], short["battles"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
