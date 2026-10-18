"""Extract measured Homework 2 evidence from opt-in Unity process traces.

Usage: python3 tools/summarize_homework2.py /path/to/raw-traces
Keeps short original trace windows and fails if the claimed movement checks fail.
"""
import json
import math
from pathlib import Path
import sys

source = Path(sys.argv[1])
destination = Path(__file__).resolve().parents[1] / 'verification/week04'
destination.mkdir(parents=True, exist_ok=True)


def read(name):
    return [json.loads(line) for line in (source / (name + '.jsonl')).read_text().splitlines() if line]


def paddle(sample, side):
    return next((p for p in sample['paddles'] if p['side'] == side), None)


server = read('server')
bursts = []
maximum_speed = 0
for before, after in zip(server, server[1:]):
    for side in (0, 1):
        a, b = paddle(before, side), paddle(after, side)
        if a is None or b is None or a['owner'] != b['owner']:
            continue
        ticks = after['tick'] - before['tick']
        if ticks <= 0:
            continue
        dz = b['position']['z'] - a['position']['z']
        maximum_speed = max(maximum_speed, abs(dz) / (ticks * after['tickDelta']))
        if side == 0 and b['rejected'] - a['rejected'] == 19:
            other_a, other_b = paddle(before, 1), paddle(after, 1)
            bursts.append({
                'serverTick': after['tick'], 'rejectedDelta': 19,
                'leftBefore': a['position']['z'], 'leftAfter': b['position']['z'],
                'otherPlayerDelta': None if other_a is None or other_b is None else
                other_b['position']['z'] - other_a['position']['z'],
            })

assert maximum_speed <= 8.0001, maximum_speed
assert bursts and abs(bursts[0]['leftAfter'] - bursts[0]['leftBefore'] - 8 / 30) < 0.0001
independent = next(b for b in bursts if b['otherPlayerDelta'] is not None and abs(b['otherPlayerDelta']) > .2)

# The native X press followed the first C press: z=3.2 -> z=3.4666667.
x_pair = next((a, b) for a, b in zip(server, server[1:])
              if paddle(a, 0) and paddle(b, 0)
              and abs(paddle(a, 0)['position']['z'] - bursts[0]['leftAfter']) < .0001
              and .26 < paddle(b, 0)['position']['z'] - paddle(a, 0)['position']['z'] < .27
              and paddle(b, 0)['rejected'] == paddle(a, 0)['rejected'])

left = read('left')
jump_index = next(i for i, s in enumerate(left) if s['localSide'] == 0 and s['tick'] > x_pair[1]['tick'] and paddle(s, 0)
                  and abs(paddle(s, 0)['position']['x']) < 1)
jump = left[jump_index]
corrected = next(s for s in left[jump_index + 1:] if paddle(s, 0)
                 and abs(paddle(s, 0)['position']['x'] + 7) < .01)
correction_ms = (corrected['time'] - jump['time']) * 1000
assert correction_ms < 1000 / 30
assert all(abs(paddle(s, 0)['position']['x'] + 7) < .01 for s in server if paddle(s, 0))

assert all(math.isfinite(value) for s in server for p in s['paddles'] for value in p['position'].values())
assert all(abs(p['position']['z']) <= 4.0001 for s in server for p in s['paddles'])

right = read('right')
spectator = read('spectator')
assert all(p['position'] == {'x': 0.0, 'y': 0.0, 'z': 0.0} and p['rejected'] == 0
           for s in server for p in s['paddles'] if p['side'] == 2)
shared = [next(s for s in values if s['left'] == 1 and s['right'] == 0 and len(s['paddles']) >= 3)
          for values in (server, left, right, spectator)]
assert {s['localSide'] for s in shared} == {-1, 0, 1, 2}

summary = {
    'date': '2026-10-18', 'unity': '6000.3.2f1', 'tickRate': 30,
    'scope': 'Local processes on one Mac; no physical second laptop or cloud deployment',
    'mppmStep2': {'serverAndClients': 3, 'leftServerIntermediateZ': .8000001,
                  'leftFinalZ': 4, 'rightFinalZ': 0, 'observedRttMs': 0},
    'nativeC': bursts[0], 'independentPaddles': independent,
    'nativeX': {'serverTick': x_pair[1]['tick'],
                'before': paddle(x_pair[0], 0)['position']['z'],
                'after': paddle(x_pair[1], 0)['position']['z'], 'sentAxis': 50},
    'nativeT': {'clientTick': jump['tick'], 'rttMs': jump['rtt'],
                'jumpPosition': paddle(jump, 0)['position'],
                'correctedPosition': paddle(corrected, 0)['position'],
                'observedCorrectionMs': correction_ms, 'serverXNeverChanged': True},
    'maximumObservedServerSpeed': maximum_speed,
    'allServerPositionsFinite': True, 'allServerPaddlesInsideCourt': True,
    'nanRequest': 'Sent through owner RPC by opt-in probe; no non-finite server position',
    'spectatorSpam': 'Sent through owned RPC by opt-in probe; no movement, counter stays zero',
    'sharedGoalSnapshots': shared,
    'importantLimit': 'Instrumented load and Editor MCP pauses also batch normal input into one server tick; cumulative rejection totals are not a cheat count.',
}
(destination / 'results.json').write_text(json.dumps(summary, indent=2) + '\n')

ticks = [bursts[0]['serverTick'], independent['serverTick'], x_pair[1]['tick'], jump['tick']]
selected_server = [s for s in server if any(abs(s['tick'] - tick) <= 10 for tick in ticks)]
(destination / 'server-selected.jsonl').write_text(''.join(json.dumps(s) + '\n' for s in selected_server))
(destination / 'client-teleport.jsonl').write_text(''.join(json.dumps(s) + '\n' for s in left[max(0, jump_index-3):jump_index+10]))
print(json.dumps({k: summary[k] for k in ('nativeC', 'nativeX', 'nativeT', 'independentPaddles', 'maximumObservedServerSpeed')}, indent=2))
