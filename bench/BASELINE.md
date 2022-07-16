# Baseline

Where the searches stand before any of the core rewrite lands. Recorded after the relaxation and
`ChangeController` fixes, so this is correct behaviour being measured, not the old wrong behaviour.

Absolute times depend on the machine, so they only mean anything next to the numbers directly
beside them. The ratio column and the shape of the growth are the parts worth holding onto.

Grids are a seeded 22% wall scatter, corner to corner, 8-connected. `make bench` reproduces.

## One full solve

Build the search, find the path, clean the grid back up.

| Search | 32x32 | 128x128 | 256x256 |
|---|---:|---:|---:|
| A* (Euclidean) | 166 us | 5,684 us | 34,906 us |
| Dijkstra | 150 us | 5,022 us | 30,491 us |
| Greedy | 26 us | 151 us | 539 us |
| Jump Point | 93 us | 2,601 us | 14,387 us |
| Bidirectional | 139 us | 6,343 us | 40,021 us |

Allocation per solve, same order:

| Search | 32x32 | 128x128 | 256x256 |
|---|---:|---:|---:|
| A* | 175 KB | 1,733 KB | 6,700 KB |
| Dijkstra | 198 KB | 3,173 KB | 12,691 KB |
| Greedy | 47 KB | 240 KB | 764 KB |
| Jump Point | 109 KB | 1,168 KB | 4,513 KB |
| Bidirectional | 180 KB | 3,018 KB | 12,039 KB |

## Neighbour enumeration

Walking every cell's neighbours once, on a 128x128 grid.

| Connectivity | Mean | Allocated |
|---|---:|---:|
| Orthogonal | 531 us | 1 MB |
| Diagonal | 856 us | 2 MB |

## What stands out

A* costs 4x more going from 128 to 256 in cells but 6x in time, and Dijkstra does the same. That
is the sorted-list frontier: every insert scans it linearly and calls `Priority` per comparison,
so the frontier growing hurts twice over. It is the first thing worth replacing.

Bidirectional being the slowest is the same story amplified, since it runs two frontiers.

Greedy is two orders of magnitude ahead of everything at 256 because it charges straight at the
goal and expands almost nothing. It also does not find the shortest path, so it is not really in
the same competition.

Allocation tracks node count rather than grid size, which is expected while search state lives on
the `Node` objects themselves.
