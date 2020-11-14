using System;
using System.Linq;
using Algorithms;
using UnityEngine;

namespace LevelGeneration {
    public class RandomPlacement : ILevelGenerator {
        private readonly float _percentFill;
        private readonly bool _fillTo;
        private readonly bool _add;


        /// <summary>
        /// A completely random level generator, placing walls randomly with no pattern or point.
        /// Walls will be placed to meet a target percentage.
        /// </summary>
        /// <param name="percentFill">The target percentage to fill the graph to.</param>
        /// <param name="fillTo">If true, the graph will be filled to the target percentage. If false, it will simply add that percentage.</param>
        /// <param name="add">If false, removes walls instead of adding walls.</param>
        public RandomPlacement(float percentFill, bool fillTo, bool add) {
            _percentFill = percentFill;
            _fillTo = fillTo;
            _add = add;
        }

        public NodeGrid Generate(NodeGrid nodeGrid) {
            // Calculate the number of walls to place
            int wallsLeft = (int) Math.Round(nodeGrid.CellCount * _percentFill);
            if (_fillTo) wallsLeft -= (_add ? nodeGrid.Walls() : nodeGrid.Empty()).Count();
            wallsLeft = Mathf.Clamp(wallsLeft, 0, nodeGrid.CellCount);

            // Begin adding walls
            while (wallsLeft > 0) {
                // Grab a node, skip if already a wall 
                Node node = nodeGrid.GetRandomNode();

                if (_add) {
                    if (!node.Walkable) continue;
                    node.Walkable = false;
                }
                else {
                    if (node.Walkable) continue;
                    node.Walkable = true;
                }

                wallsLeft--;
            }

            return nodeGrid;
        }
    }
}