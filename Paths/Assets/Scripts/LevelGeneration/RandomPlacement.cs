using System;
using System.Linq;
using Algorithms;
using UnityEngine;

namespace LevelGeneration {
    public class RandomPlacement : ILevelGenerator {
        private readonly float _percentFill;
        private readonly bool _fillTo;


        /// <summary>
        /// A completely random level generator, placing walls randomly with no pattern or point.
        /// Walls will be placed to meet a target percentage.
        /// </summary>
        /// <param name="percentFill">The target percentage to fill the graph to.</param>
        /// <param name="fillTo">If true, the graph will be filled to the target percentage. If false, it will simply add that percentage.</param>
        public RandomPlacement(float percentFill, bool fillTo) {
            _percentFill = percentFill;
            _fillTo = fillTo;
        }

        public NodeGrid Generate(NodeGrid nodeGrid) {
            // Calculate the number of walls to place
            int wallsLeft = (int) Math.Round(nodeGrid.CellCount * _percentFill);
            if (_fillTo) wallsLeft -= nodeGrid.Walls().Count();
            wallsLeft = Mathf.Clamp(wallsLeft, 0, nodeGrid.CellCount);

            // Begin adding walls
            while (wallsLeft > 0) {
                // Grab a node, skip if already a wall 
                Node node = nodeGrid.GetRandomNode();
                if (!node.Walkable) continue;

                // Node is empty, set to a wall
                node.Walkable = false;
                wallsLeft--;
            }

            return nodeGrid;
        }
    }
}