#!/usr/bin/env python3
"""Original Aether Ark prototype score/SFX. Standard library only; no samples or external music.

Generate: python3 tools/gen_audio.py
Check committed WAVs: python3 tools/gen_audio.py --validate
Reproduce byte-for-byte: python3 tools/gen_audio.py --check
"""
import argparse
from array import array
import hashlib
import io
import math
from pathlib import Path
import random
import sys
import wave

ROOT = Path(__file__).resolve().parents[1] / "Assets/_Project/Resources/Audio"
RATE = 22050
TAU = math.tau
MUSIC = {"voyage": 76, "port": 90, "encounter": 68, "combat": 118, "finale": 132}
EFFECTS = ("confirm", "reject", "cannon", "impact", "flyby", "launch", "recover", "resonance",
           "warning", "critical", "pause", "resume", "victory", "defeat")


class Mix:
    def __init__(self, seconds, loop=False):
        self.frames = round(seconds * RATE)
        self.left = array("f", [0]) * self.frames
        self.right = array("f", [0]) * self.frames
        self.loop = loop

    def add(self, start, duration, frequency, gain, voice="bell", pan=0, end_frequency=None):
        offset = round(start * RATE)
        count = round(duration * RATE)
        left = math.sqrt((1 - pan) / 2) * gain
        right = math.sqrt((1 + pan) / 2) * gain
        rng = random.Random(offset + count + round(frequency * 13))
        noise = 0.0
        for n in range(count):
            index = offset + n
            if self.loop:
                index %= self.frames  # Release/reverb tails wrap into the next seamless loop.
            elif index >= self.frames:
                break
            t = n / RATE
            u = n / max(1, count - 1)
            phase = TAU * (frequency * t + 0.5 * ((end_frequency or frequency) - frequency) * t * u)
            edge = min(1, t / 0.008, (duration - t) / 0.025)
            if voice == "pad":
                envelope = math.sin(math.pi * u) ** 1.5
                sample = (math.sin(phase) + 0.18 * math.sin(phase * 2) + 0.07 * math.sin(phase * 3)) * envelope
            elif voice == "bass":
                sample = (math.sin(phase) + 0.22 * math.sin(phase * 2)) * math.exp(-4 * u) * edge
            elif voice == "noise":
                noise = noise * 0.78 + rng.uniform(-1, 1) * 0.22
                sample = (noise * 1.6 + math.sin(phase) * 0.3) * math.exp(-6 * u) * edge
            elif voice == "air":
                noise = noise * 0.65 + rng.uniform(-1, 1) * 0.35
                sample = (noise * 0.4 + math.sin(phase) * 0.35) * math.sin(math.pi * u) ** 2
            else:
                sample = (math.sin(phase) * math.exp(-5 * u)
                          + 0.3 * math.sin(phase * 2.002) * math.exp(-11 * u)
                          + 0.12 * math.sin(phase * 3.01) * math.exp(-18 * u)) * edge
            self.left[index] += sample * left
            self.right[index] += sample * right

    def note(self, beat, length, midi, gain, voice, tempo, pan=0):
        self.add(beat * tempo, length * tempo, 440 * 2 ** ((midi - 69) / 12), gain, voice, pan)

    def encode(self, peak):
        if not self.loop:
            for n in range(self.frames):
                fade = min(1, n / (RATE * 0.005), (self.frames - 1 - n) / (RATE * 0.03))
                self.left[n] *= fade
                self.right[n] *= fade
        scale = peak * 32767 / max(0.001, max(map(abs, self.left)), max(map(abs, self.right)))
        samples = array("h")
        for left, right in zip(self.left, self.right):
            samples.append(round(left * scale))
            samples.append(round(right * scale))
        if sys.byteorder != "little":
            samples.byteswap()
        buffer = io.BytesIO()
        with wave.open(buffer, "wb") as output:
            output.setnchannels(2)
            output.setsampwidth(2)
            output.setframerate(RATE)
            output.writeframes(samples.tobytes())
        return buffer.getvalue()


def score(name, bpm):
    beat = 60 / bpm
    mix = Mix(32 * beat, loop=True)
    roots = [50, 46, 53, 48, 50, 46, 43, 45]
    tense = name in ("combat", "finale")
    # A newly authored rising/falling six-note motif, reharmonized across eight bars.
    motif = [12, 19, 15, 14, 7, 10, 12, 7]
    for bar, root in enumerate(roots):
        third = 3 if bar in (0, 4, 6) else 4
        for voice_index, interval in enumerate((0, third, 7)):
            mix.note(bar * 4, 5, root + interval, 0.11 if tense else 0.14, "pad", beat, (voice_index - 1) * 0.5)
        count = 8 if tense or name == "port" else 4
        for step in range(count):
            interval = (0, 7, 12, third + 12)[step % 4]
            mix.note(bar * 4 + step * 4 / count, 1.8, root + interval + 12,
                     0.09 if name == "encounter" else 0.15, "bell", beat, -0.45 if step % 2 else 0.45)
        if name != "encounter":
            mix.note(bar * 4 + 0.5, 2.5, root + motif[bar], 0.18, "bell", beat, 0.1)
            mix.note(bar * 4 + 2.5, 2.2, root + motif[(bar + 3) % 8], 0.12, "bell", beat, -0.2)
        for step in range(4 if tense else 2):
            mix.note(bar * 4 + step * (1 if tense else 2), 1.5, root - 12, 0.26, "bass", beat)
        if tense:
            for step in (0, 2, 3.5):
                mix.add((bar * 4 + step) * beat, 0.22, 130, 0.4, "bass", end_frequency=38)
            for step in (1, 3):
                mix.add((bar * 4 + step) * beat, 0.15, 150, 0.20, "noise", 0.15)
            if name == "finale":
                for step in range(8):
                    mix.add((bar * 4 + step / 2) * beat, 0.07, 2200, 0.065, "noise", -0.4)
        elif name == "port":
            for step in (1, 3):
                mix.add((bar * 4 + step) * beat, 0.10, 850, 0.055, "noise", -0.3)
    return mix.encode(0.5)


def effect(name):
    durations = {"cannon": 0.9, "impact": 0.75, "flyby": 0.6, "launch": 1.4, "recover": 1.1,
                 "resonance": 1.6, "warning": 0.9, "critical": 1.2, "victory": 3.2, "defeat": 3.2}
    mix = Mix(durations.get(name, 0.5))
    if name in ("cannon", "impact"):
        mix.add(0, 0.7, 150 if name == "cannon" else 85, 0.85, "noise", end_frequency=28)
        mix.add(0, 0.35, 170, 0.8, "bass", end_frequency=35)
    elif name in ("flyby", "launch"):
        mix.add(0, mix.frames / RATE, 170 if name == "launch" else 700, 0.7, "air",
                end_frequency=900 if name == "launch" else 140)
    elif name == "resonance":
        mix.add(0, 1.55, 90, 0.6, "air", end_frequency=660)
        mix.add(0.65, 0.8, 523, 0.25, "bell")
    elif name in ("warning", "critical"):
        for i in range(2 if name == "warning" else 3):
            mix.add(i * 0.3, 0.23, 660 if i % 2 else 440, 0.6, "bell")
    elif name in ("victory", "defeat"):
        notes = (62, 65, 69, 74) if name == "victory" else (62, 60, 57, 50)
        for i, note in enumerate(notes):
            mix.note(i * 0.32, 2, note, 0.6, "bell", 1, (i - 1.5) * 0.2)
            mix.note(i * 0.32, 2, note - 12, 0.18, "pad", 1)
    else:
        notes = {"confirm": (74, 81), "reject": (57, 53), "recover": (69, 74, 81),
                 "pause": (74, 69), "resume": (69, 74)}[name]
        for i, note in enumerate(notes):
            mix.note(i * 0.12, 0.4, note, 0.5, "bell", 1)
    return mix.encode(0.72)


def validate():
    paths = [ROOT / "Music" / f"{name}.wav" for name in MUSIC]
    paths += [ROOT / "Effects" / f"{name}.wav" for name in EFFECTS]
    for path in paths:
        with wave.open(str(path), "rb") as audio:
            assert (audio.getnchannels(), audio.getsampwidth(), audio.getframerate()) == (2, 2, RATE), path
            duration = audio.getnframes() / RATE
            samples = array("h", audio.readframes(audio.getnframes()))
        if sys.byteorder != "little":
            samples.byteswap()
        peak = max(map(abs, samples)) / 32768
        rms = math.sqrt(sum(sample * sample for sample in samples) / len(samples)) / 32768
        assert 0.3 < peak < 0.8 and 0.015 < rms < 0.3, (path, peak, rms)
        if path.parent.name == "Music":
            assert abs(duration - 32 * 60 / MUSIC[path.stem]) < 1 / RATE, path
            seam = max(abs(samples[0] - samples[-2]), abs(samples[1] - samples[-1])) / 32768
            assert seam < 0.06, (path, "loop seam", seam)
        else:
            assert max(abs(samples[0]), abs(samples[-1])) < 100, (path, "edge click")
        print(f"OK {path.parent.name}/{path.name}: {duration:.2f}s, peak {peak:.2f}, RMS {rms:.3f}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--validate", action="store_true")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    if args.validate:
        validate()
        return
    for folder, names in (("Music", MUSIC), ("Effects", EFFECTS)):
        for name in names:
            data = score(name, MUSIC[name]) if folder == "Music" else effect(name)
            path = ROOT / folder / f"{name}.wav"
            if args.check:
                if not path.exists() or hashlib.sha256(path.read_bytes()).digest() != hashlib.sha256(data).digest():
                    raise SystemExit(f"Stale audio: {path}")
            else:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)
            print(f"{'Checked' if args.check else 'Generated'} {folder}/{name}.wav", flush=True)
    validate()


if __name__ == "__main__":
    main()
