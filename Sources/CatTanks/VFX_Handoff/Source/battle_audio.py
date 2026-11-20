"""Synthesize a small stereo sound bed for the offline VFX preview."""
from array import array
from math import exp, pi, sin, sqrt
from random import Random
import wave


RATE = 44100


def write_battle_audio(path, seconds, cues):
    frames = int(seconds * RATE)
    left = [0.0] * frames
    right = [0.0] * frames
    for index, (time, kind, x) in enumerate(cues):
        length, pitch, weight, volume = {
            "rapid": (.086, 136, .28, .29),
            "shell": (.57, 69, .72, .56),
            "rocket": (.39, 104, .58, .35),
            "hit": (.075, 230, .15, .16),
            "blast": (.69, 51, .78, .62),
            "rocketblast": (.43, 65, .66, .38),
            "arc": (.24, 194, .18, .29),
            "frost": (.42, 470, .14, .23),
        }[kind]
        rng = Random(410 + index)
        pan = min(.88, max(.12, x))
        a, b = sqrt(1 - pan) * volume, sqrt(pan) * volume
        noise_state = 0.0
        start = round(time * RATE)
        for sample in range(min(int(length * RATE), frames - start)):
            t = sample / RATE
            p = t / length
            noise = rng.uniform(-1, 1)
            noise_state += .19 * (noise - noise_state)
            body = sin(2 * pi * pitch * t * (1 - .54 * p))
            attack = min(1, t / .003)
            tone = body * weight * exp(-7 * p) + (
                .60 * noise_state + .22 * noise
            ) * (1 - weight * .34) * exp(-4 * p)
            if kind in ("arc", "frost"):
                tone += .28 * sin(2 * pi * pitch * t * (2.3 + p * 1.8)) * exp(-5 * p)
            left[start + sample] += tone * attack * a
            right[start + sample] += tone * attack * b

    pcm = array("h")
    for i, (a, b) in enumerate(zip(left, right)):
        t = i / RATE
        room = .010 * sin(2 * pi * 51 * t) * min(1, t * 2, (seconds - t) * 2)
        for value in (a + room, b + room):
            rounded = round(30000 * value / (1 + .28 * abs(value)))
            pcm.append(max(-32768, min(32767, rounded)))
    with wave.open(str(path), "wb") as stream:
        stream.setparams((2, 2, RATE, frames, "NONE", "not compressed"))
        stream.writeframes(pcm.tobytes())
