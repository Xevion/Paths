<div align="center">
    <a href=""><img src="./.media/banner.png" alt="Paths"></a>
    <br>
    <sub>
        Built with ❤︎ by <a href="https://github.com/Xevion/">Xevion</a>
    </sub>
</div>

Paths is a Unity application for watching pathfinding algorithms work. You draw walls, drag the
start and end around, pick an algorithm, and it animates the search cell by cell — what it's seen,
what it's expanded, and the path it settles on — with a scrubber so you can step through it.

It started as an A\* visualizer and grew into a handful of algorithms behind a shared interface,
a few heuristics, and an editable grid you can resize and reframe on the fly.

## Algorithms

- **A\*** — informed, the original
- **Dijkstra** — uniform cost, no heuristic
- **Greedy best-first** — heuristic only, not optimal
- **Breadth-first / Depth-first** — the uninformed baselines
- **Jump Point Search** — A\* with symmetry-breaking jumps on a uniform grid

A\*, Dijkstra and Greedy share an expand loop with a sorted frontier; the heuristic (Manhattan,
Euclidean or Chebyshev) and 8-direction movement are toggled in the UI. JPS does its own thing —
it's 8-connected by design, so the diagonal toggle doesn't apply to it.

## Controls

| | |
|---|---|
| **Space** | play / pause |
| **Click + drag** | draw / erase walls |
| **Drag green / red** | move the start / end |
| **Drag the bar** | scrub the search |
| **- / =** | shrink / grow the grid |
| **Scroll / right-drag** | zoom / pan |
| **H** | hide the help overlay |

Editing while it's playing recomputes the search and keeps your place in the timeline, so you can
watch a wall reshape the path without losing where you were.

## Running it

Built and developed on **Unity 2020.3.18f1**. There's a `Makefile` for building without opening
the editor (point `UNITY` at your editor binary in a `local.mk`):

```sh
make demo   # build + run the desktop player
make web    # build the WebGL demo and serve it at http://localhost:8000
make test   # EditMode tests
```

`make web` produces a self-contained WebGL build under `Build/WebGL` — it loads off any static
host, so dropping that folder somewhere is the whole deploy.

## Roadmap

What's done and what's still on the list:

- Algorithms
    - [x] Dijkstra
    - [x] Depth-First Search
    - [x] Breadth-First Search
    - [x] Jump Point Search
    - [ ] IDA\*
    - [ ] Orthogonal Jump Point Search
    - [ ] Trace
    - [ ] Bellman-Ford
    - [ ] D\* or D\*-Lite
- Configuration Options
    - Search Options
        - [x] Uninformed (BFS / DFS / Dijkstra)
        - [x] Informed (A\* / Greedy / JPS)
        - Heuristics
            - [x] Manhattan
            - [x] Euclidean
            - [x] Chebyshev
- Application Elements
    - [x] UI Toolbar / Tool Selection
    - [x] Algorithm Configuration Area
    - [x] Statistics Report
- Miscellaneous
    - [x] Repository graphics
