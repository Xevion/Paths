using NUnit.Framework;
using Paths.Core;
using Paths.Core.Generation;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    public class TerrainTests {
        [TestCase(Terrain.Plain, 1f)]
        [TestCase(Terrain.Road, 0.5f)]
        [TestCase(Terrain.Mud, 2f)]
        [TestCase(Terrain.Water, 4f)]
        public void EachTierHasItsWeight(Terrain terrain, float weight) {
            Assert.AreEqual(weight, Terrains.Weight(terrain), SearchCorpus.Eps);
        }

        [Test]
        public void EveryTierSurvivesTheRoundTrip() {
            foreach (Terrain terrain in Terrains.All)
                Assert.AreEqual(terrain, Terrains.Of(Terrains.Weight(terrain)));
        }

        [TestCase(0.4f, Terrain.Road)]
        [TestCase(0.9f, Terrain.Plain)]
        [TestCase(1.8f, Terrain.Mud)]
        [TestCase(9f, Terrain.Water)]
        public void AWeightFromElsewhereLandsOnTheNearestTier(float weight, Terrain expected) {
            Assert.AreEqual(expected, Terrains.Of(weight));
        }

        [Test]
        public void PaintingTerrainChangesWhatTheCellCosts() {
            var solver = new PathSolver(6, 6, 1, Generator.Empty, 0f);
            var cell = new Coord(2, 3);

            Assert.AreEqual(Terrain.Plain, solver.TerrainAt(cell));
            Assert.IsFalse(solver.HasTerrain);

            solver.SetTerrain(cell, Terrain.Water);

            Assert.AreEqual(Terrain.Water, solver.TerrainAt(cell));
            Assert.IsTrue(solver.HasTerrain);
            Assert.AreEqual(Terrain.Water, solver.TerrainField()[2, 3]);
            Assert.AreEqual(Terrain.Plain, solver.TerrainField()[0, 0]);
        }
    }
}
