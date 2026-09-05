#!/usr/bin/env python3
"""Export through the real audit CLI; no Unity install or Python packages required."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

sys.dont_write_bytecode = True
from build_headless_audit import compile_audit, source_digest


class AuditCheckpointTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build = tempfile.TemporaryDirectory(prefix="aether-checkpoint-tests-")
        cls.binary = Path(cls.build.name) / "audit.exe"
        compile_audit(cls.binary)

    @classmethod
    def tearDownClass(cls):
        cls.build.cleanup()

    def setUp(self):
        self.output = tempfile.TemporaryDirectory(prefix="aether-checkpoint-output-")
        self.addCleanup(self.output.cleanup)
        self.directory = Path(self.output.name) / "captures"

    def invoke(self, args):
        return subprocess.run(["mono", str(self.binary), *args], capture_output=True, text=True, timeout=30)

    def export(self, battle=21, tick=100, extra=(), seed=17000):
        args = ["1", "Standard", str(seed), "--flagship=ship_zephyr", "--wings=adaptive",
                f"--snapshot-dir={self.directory}", f"--snapshot-battle={battle}", f"--snapshot-tick={tick}", *extra]
        result = self.invoke(args)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("RUN ", result.stdout)
        path = Path(next(line.removeprefix("SNAPSHOT ") for line in result.stdout.splitlines() if line.startswith("SNAPSHOT ")))
        envelope = json.loads(path.read_text())
        self.assertEqual(envelope["format"], "aether-ark-combat-snapshot")
        self.assertEqual(envelope["formatVersion"], 1)
        self.assertEqual(envelope["sha256"], hashlib.sha256(envelope["payloadJson"].encode()).hexdigest())
        self.assertTrue(envelope["unityVersion"].startswith("headless-mono/"))
        payload = json.loads(envelope["payloadJson"])
        self.assertEqual(payload["audit"]["sourceSha256"], source_digest())
        return path, payload

    def test_late_battle_keeps_accumulated_campaign_and_all_rng_streams(self):
        _, payload = self.export()
        run = payload["run"]
        self.assertEqual((run["seed"], run["regionIndex"], run["totalTravelCount"]), (17000, 6, 41))
        self.assertEqual(run["random"], {"route": 94922169, "combat": 2838187044, "events": 1977682864})
        self.assertGreater(len(run["installedModules"]), 0)
        self.assertGreater(len(run["crew"]), 6)
        self.assertAlmostEqual(run["combatElapsed"], 10, places=4)
        self.assertEqual(payload["audit"]["boundary"], "before-next-orders")
        self.assertEqual(payload["audit"]["completedTicks"], 100)

    def test_repeat_capture_is_same_payload_and_never_overwrites(self):
        first, a = self.export()
        before = first.read_bytes()
        second, b = self.export()
        self.assertNotEqual(first, second)
        self.assertEqual(a, b)
        self.assertEqual(first.read_bytes(), before)
        self.assertEqual(len(list(self.directory.glob("*.json"))), 2)
        self.assertEqual(list(self.directory.glob("*.tmp")), [])

    def test_tick_zero_is_before_initial_orders_and_one_is_after_exactly_one_tick(self):
        _, start = self.export(battle=1, tick=0)
        _, after = self.export(battle=1, tick=1)
        self.assertEqual(start["run"]["combatElapsed"], 0)
        self.assertAlmostEqual(after["run"]["combatElapsed"], .1, places=7)
        self.assertTrue(all(not crew["movement"]["path"] for crew in start["run"]["crew"]))
        self.assertTrue(any(crew["movement"]["path"] for crew in after["run"]["crew"]))
        self.assertEqual(start["run"]["random"]["route"], after["run"]["random"]["route"])
        self.assertEqual(start["run"]["random"]["events"], after["run"]["random"]["events"])

    def test_unreached_battle_ended_battle_and_earlier_timeout_write_nothing(self):
        for battle, tick, cap in ((99, 0, 420), (1, 36001, 420), (21, 100, .1), (1, 2, .1)):
            result = self.invoke(["1", "Standard", "17000", "--flagship=ship_zephyr", "--wings=adaptive",
                                  f"--snapshot-dir={self.directory}", f"--snapshot-battle={battle}",
                                  f"--snapshot-tick={tick}", f"--combat-cap={cap}"])
            self.assertEqual(result.returncode, 4, result.stderr)
            self.assertIn("CHECKPOINT UNREACHED", result.stderr)
            self.assertNotIn("SNAPSHOT ", result.stdout)
            self.assertFalse(self.directory.exists())

    def test_active_checkpoint_at_cap_is_exportable(self):
        _, payload = self.export(battle=1, tick=1, extra=("--combat-cap=0.1",))
        self.assertEqual(payload["audit"]["completedTicks"], 1)

    def test_invalid_partial_ambiguous_or_multi_run_requests_fail_before_writing(self):
        output = f"--snapshot-dir={self.directory}"
        for args in (["1", "Standard", "17000", output],
                     ["1", "Standard", "17000", output, "--snapshot-battle=0", "--snapshot-tick=0"],
                     ["1", "Standard", "17000", output, "--snapshot-battle=1", "--snapshot-tick=-1"],
                     ["1", "Standard", "17000", output, "--snapshot-battle=1", "--snapshot-tick=0", "--records"],
                     ["2", "Standard", "17000", output, "--snapshot-battle=1", "--snapshot-tick=0"],
                     ["1", output, "--snapshot-battle=1", "--snapshot-tick=0"]):
            result = self.invoke(args)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("ArgumentException", result.stderr)
            self.assertFalse(self.directory.exists())

    def test_signed_seed_and_explicit_tutorial_are_preserved(self):
        for seed in (-2147483648, -1, 0, 2147483647, 32838):
            _, payload = self.export(battle=1, tick=0, seed=seed)
            self.assertEqual(payload["run"]["seed"], seed)
            self.assertEqual(payload["run"]["regionCount"], 6)
        result = self.invoke(["--tutorial", f"--snapshot-dir={self.directory}", "--snapshot-battle=1", "--snapshot-tick=0"])
        self.assertEqual(result.returncode, 0, result.stderr)
        path = Path(result.stdout.splitlines()[0][len("SNAPSHOT "):])
        payload = json.loads(json.loads(path.read_text())["payloadJson"])
        self.assertEqual(payload["run"]["regionCount"], 1)
        self.assertTrue(payload["run"]["isFirstExpedition"])

    def test_forced_enemy_and_cautious_strategy_provenance(self):
        _, payload = self.export(battle=1, tick=1, extra=("--strategy=cautious", "--enemy=enemy_cruiser"))
        self.assertEqual(payload["audit"]["strategy"], "cautious")
        self.assertEqual(payload["audit"]["forcedEnemy"], "enemy_cruiser")
        self.assertFalse(any(crew["onSortie"] for crew in payload["run"]["crew"]))


if __name__ == "__main__":
    unittest.main(verbosity=2)
