using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    public class CoordTests {
        [Test]
        public void AddsAndSubtractsComponentwise() {
            Assert.AreEqual(new Coord(4, 6), new Coord(1, 2) + new Coord(3, 4));
            Assert.AreEqual(new Coord(-2, -2), new Coord(1, 2) - new Coord(3, 4));
        }

        [Test]
        public void EqualsComparesByValue() {
            Assert.IsTrue(new Coord(2, 3) == new Coord(2, 3));
            Assert.IsFalse(new Coord(2, 3) == new Coord(3, 2));
            Assert.IsTrue(new Coord(2, 3) != new Coord(3, 2));
            Assert.AreEqual(new Coord(2, 3), new Coord(2, 3));
        }

        // the searches key sets and dictionaries off positions, so equal coords have to collide and
        // (x,y) has to hash differently from (y,x)
        [Test]
        public void HashesConsistentlyWithEquality() {
            Assert.AreEqual(new Coord(2, 3).GetHashCode(), new Coord(2, 3).GetHashCode());
            Assert.AreNotEqual(new Coord(2, 3).GetHashCode(), new Coord(3, 2).GetHashCode());

            var seen = new HashSet<Coord> { new Coord(2, 3) };
            Assert.IsTrue(seen.Contains(new Coord(2, 3)));
            Assert.IsFalse(seen.Contains(new Coord(3, 2)));
        }

        [Test]
        public void ToStringReadsAsAPair() {
            Assert.AreEqual("(2, 3)", new Coord(2, 3).ToString());
        }
    }
}
