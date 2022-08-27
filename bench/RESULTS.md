# Results

What the rewrite bought, against the numbers in `BASELINE.md`. Absolute times depend on the
machine, so the ratios and the shape of the growth are the parts worth reading.

Grids are a seeded 22% wall scatter, corner to corner, 8-connected. `make bench` reproduces.

## A* before and after

Same grids, same walls. The old search kept its state on shared node objects and ordered a sorted
list; this one uses flat arrays and the indexed heap.

| Size | Old | New | Ratio | Allocated, old to new |
|---|---:|---:|---:|---|
| 32x32 | 166 us | 28 us | 0.17 | 175 KB to 52 KB |
| 128x128 | 5,684 us | 562 us | 0.10 | 1,733 KB to 563 KB |
| 256x256 | 34,906 us | 2,420 us | 0.07 | 6,700 KB to 2,245 KB |

The gap widens with the grid, which is the point. Going from 128 to 256 cost the old search 6.1x
for 4x the cells, and costs this one 4.3x. Superlinear against roughly linear, because the frontier
stopped being scanned end to end on every insert.

## Where everything sits now

| Search | 32x32 | 128x128 | 256x256 |
|---|---:|---:|---:|
| A* (octile) | 28 us | 562 us | 2,420 us |
| Dijkstra | 30 us | 1,115 us | 5,615 us |
| Greedy | 9 us | 43 us | 247 us |
| Jump Point | 36 us | 662 us | 2,917 us |
| Bidirectional | 26 us | 1,038 us | 4,681 us |

Dijkstra costing about twice A* is expected: no heuristic to aim with means it spreads in every
direction and expands far more. Bidirectional lands near it because it is two Dijkstra waves, and
its saving only shows on longer routes than a corner-to-corner run on a small grid.

Jump Point is the surprise. It used to be 0.44x of A* and is now 1.2x, not because it got slower
but because A* got so much faster. Its cost was never the frontier, it is the jump recursion, so
replacing the frontier barely touched it. Halving its allocations bought 17%, and beyond that it
would need the jump scanning itself reworked.

## What the perf pass changed

Jump Point was allocating two neighbour lists per expanded cell. Reusing them across the search
cut its allocation by more than half and its time by about a fifth:

| | Before | After |
|---|---|---|
| 128x128 | 800 us, 990 KB | 662 us, 437 KB |
| 256x256 | 3,530 us, 3,844 KB | 2,917 us, 1,737 KB |

Pre-sizing the recorded trace shaved a little off the rest. It costs Greedy slightly, which expands
almost nothing and now reserves more than it uses, but that is a few hundred KB against a search
that is already an order of magnitude ahead of the others.

## Not done, on purpose

Pooling the per-search arrays would remove the fixed allocation a solve pays whatever it explores.
It is not worth it here. The app runs a 32x24 grid, where that fixed cost is a few KB, and it only
starts to matter at sizes the app never reaches. Buying it would mean handing a reusable workspace
into every search, giving up the property that a search holds nothing between calls, which is what
lets one instance be reused and two searches share a grid.
