#!/usr/bin/env python3
"""Print per-file line coverage from a Cobertura report, least covered first.

coverlet's own console table only breaks down by module, and Core is one module, so it
reports a single number. The number is not the point: what is worth knowing is which
files nothing exercises. Pass --detail to also list the uncovered line numbers.
"""

import sys
import xml.etree.ElementTree as ET
from collections import defaultdict


def read(path):
    """Line hits per file. Partial classes appear once per class, so merge them."""
    files = defaultdict(dict)
    for cls in ET.parse(path).getroot().iter("class"):
        hits = files[cls.get("filename")]
        for line in cls.iter("line"):
            number = int(line.get("number"))
            hits[number] = hits.get(number, 0) + int(line.get("hits"))
    return files


def main(path, detail):
    files = read(path)
    rows = []
    for name, hits in files.items():
        covered = sum(1 for count in hits.values() if count > 0)
        rows.append((covered / len(hits), covered, len(hits), name, hits))
    rows.sort()

    total = sum(row[2] for row in rows)
    covered = sum(row[1] for row in rows)
    print(f"{'cover':>6}  {'lines':>9}  file")
    for rate, hit, count, name, hits in rows:
        print(f"{rate * 100:5.1f}%  {hit:4d}/{count:<4d}  {name}")
        if detail and hit < count:
            missed = sorted(n for n, c in hits.items() if c == 0)
            print(f"{'':>6}  {'':>9}  uncovered: {missed}")
    print(f"\n{covered / total * 100:5.1f}%  {covered:4d}/{total:<4d}  all files")


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("-")]
    if not args:
        sys.exit("usage: coverage.py <coverage.cobertura.xml> [--detail]")
    main(args[0], "--detail" in sys.argv)
