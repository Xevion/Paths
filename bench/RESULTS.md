# Results

What the rewrite has actually bought, measured against `BASELINE.md` on the same machine in the
same run. Ratios are the part worth reading.

## A* on the new model

Same grids, same walls, same heuristic. The old search keeps its state on shared node objects and
orders a sorted list; the new one uses flat arrays and the indexed heap.

| Size | Old | New | Ratio | Allocated, old to new |
|---|---:|---:|---:|---|
| 32x32 | 167 us | 34 us | 0.20 | 175 KB to 54 KB |
| 128x128 | 5,458 us | 849 us | 0.16 | 1,733 KB to 595 KB |
| 256x256 | 33,418 us | 3,280 us | 0.10 | 6,701 KB to 2,375 KB |

The gap widens with the grid, which is the point. Going from 128 to 256 cost the old search 6.1x
for 4x the cells, and costs the new one 3.9x. Superlinear against roughly linear, because the
frontier stopped being scanned end to end on every insert.

Allocation is down by about two thirds and no longer dominated by node objects. What is left is
the three per-search arrays and the recorded visits.

## Still to come

Dijkstra lands at 0.15 of the old A* at 256, and it is doing strictly more work than A* since it
has no heuristic to aim with. Jump Point Search and bidirectional have not been ported yet, so
they are still on the old model and still in the baseline numbers.
