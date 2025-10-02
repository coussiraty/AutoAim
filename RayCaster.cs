using System;
using System.Numerics;
using ImGuiNET;
using GameHelper;
using GameHelper.Plugin;
using GameHelper.RemoteEnums;
using GameHelper.RemoteObjects.Components;

namespace AutoAim
{
    /// <summary>
    /// Ray casting utilities for line of sight and pathfinding
    /// </summary>
    public static class RayCaster
    {
        /// <summary>
        /// Checks if there's a clear line of sight between two grid positions
        /// VERSÃO MELHORADA: mais inteligente sobre o que é "bloqueado"
        /// </summary>
        /// <param name="currentArea">Current area instance</param>
        /// <param name="fromX">Starting X position</param>
        /// <param name="fromY">Starting Y position</param>
        /// <param name="toX">Target X position</param>
        /// <param name="toY">Target Y position</param>
        /// <param name="allowLowWalls">If true, allows passage through tiles with value 1 (low walls)</param>
        /// <returns>True if there's a clear line of sight, false if blocked by walls</returns>
        public static bool HasLineOfSight(
            GameHelper.RemoteObjects.States.InGameStateObjects.AreaInstance currentArea,
            int fromX, int fromY, 
            int toX, int toY,
            bool allowLowWalls = true)
        {
            // Use Bresenham's line algorithm to trace the ray
            var points = GetLinePoints(fromX, fromY, toX, toY);
            
            int totalPoints = points.Length;
            
            if (totalPoints <= 3)
                return true;
            
            int solidBlockedCount = 0;
            int walkableCount = 0; 
            
            foreach (var point in points)
            {
                var walkableValue = GetWalkableValue(currentArea, point.X, point.Y);
                
                if (walkableValue == 5)
                    walkableCount++;
                else if (walkableValue <= 2 && walkableValue >= 0)
                    solidBlockedCount++;
            }
            
            // Very restrictive line-of-sight: any solid wall blocks targeting
            return solidBlockedCount == 0; 
        }

        /// <summary>
        /// Checks if a monster is targetable (not behind walls)
        /// </summary>
        /// <param name="currentArea">Current area instance</param>
        /// <param name="playerPos">Player position</param>
        /// <param name="monsterPos">Monster position</param>
        /// <param name="allowLowWalls">If true, allows targeting through low walls</param>
        /// <returns>True if monster is targetable, false if behind walls</returns>
        public static bool IsMonsterTargetable(
            GameHelper.RemoteObjects.States.InGameStateObjects.AreaInstance currentArea,
            Vector2 playerPos, 
            Vector2 monsterPos,
            bool allowLowWalls = true)
        {
          
            var playerGridX = (int)playerPos.X;
            var playerGridY = (int)playerPos.Y;
            var monsterGridX = (int)monsterPos.X;
            var monsterGridY = (int)monsterPos.Y;

            return HasLineOfSight(currentArea, playerGridX, playerGridY, monsterGridX, monsterGridY, allowLowWalls);
        }

        /// <summary>
        /// Finds the closest walkable position to a target (useful for pathfinding)
        /// </summary>
        /// <param name="currentArea">Current area instance</param>
        /// <param name="targetX">Target X position</param>
        /// <param name="targetY">Target Y position</param>
        /// <param name="searchRadius">Search radius around target</param>
        /// <returns>Closest walkable position or null if none found</returns>
        public static (int X, int Y)? FindClosestWalkablePosition(
            GameHelper.RemoteObjects.States.InGameStateObjects.AreaInstance currentArea,
            int targetX, int targetY, 
            int searchRadius = 5)
        {
            // If target is already walkable, return it
            if (GetWalkableValue(currentArea, targetX, targetY) > 0)
                return (targetX, targetY);

            // Search in expanding circles
            for (int radius = 1; radius <= searchRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        // Only check the perimeter of the current radius
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;

                        var checkX = targetX + dx;
                        var checkY = targetY + dy;

                        if (GetWalkableValue(currentArea, checkX, checkY) > 0)
                            return (checkX, checkY);
                    }
                }
            }

            return null; // No walkable position found
        }

        /// <summary>
        /// Gets distance to first obstacle in a direction (useful for skills with range)
        /// </summary>
        /// <param name="currentArea">Current area instance</param>
        /// <param name="fromX">Starting X position</param>
        /// <param name="fromY">Starting Y position</param>
        /// <param name="directionX">Direction X (-1, 0, or 1)</param>
        /// <param name="directionY">Direction Y (-1, 0, or 1)</param>
        /// <param name="maxDistance">Maximum distance to check</param>
        /// <returns>Distance to first obstacle or maxDistance if no obstacle found</returns>
        public static int GetDistanceToObstacle(
            GameHelper.RemoteObjects.States.InGameStateObjects.AreaInstance currentArea,
            int fromX, int fromY,
            int directionX, int directionY,
            int maxDistance = 50)
        {
            for (int distance = 1; distance <= maxDistance; distance++)
            {
                var checkX = fromX + (directionX * distance);
                var checkY = fromY + (directionY * distance);

                var walkableValue = GetWalkableValue(currentArea, checkX, checkY);
                
                // Found an obstacle
                if (walkableValue == 0)
                    return distance - 1; // Return distance to last walkable position
            }

            return maxDistance; // No obstacle found within range
        }

        /// <summary>
        /// Bresenham's line algorithm to get all points along a line
        /// </summary>
        public static (int X, int Y)[] GetLinePoints(int x0, int y0, int x1, int y1)
        {
            var points = new System.Collections.Generic.List<(int, int)>();
            
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;

            while (true)
            {
                points.Add((x, y));

                if (x == x1 && y == y1)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }

            return points.ToArray();
        }

        /// <summary>
        /// Gets the walkable value at a specific grid position
        /// </summary>
        public static int GetWalkableValue(
            GameHelper.RemoteObjects.States.InGameStateObjects.AreaInstance area, 
            int x, int y)
        {
            var mapWalkableData = area.GridWalkableData;
            var bytesPerRow = area.TerrainMetadata.BytesPerRow;
            
            if (mapWalkableData.Length == 0 || bytesPerRow <= 0)
                return 0;

            var totalRows = mapWalkableData.Length / bytesPerRow;
            var width = bytesPerRow * 2; // 2 nibbles per byte

            // Check bounds
            if (x < 0 || y < 0 || x >= width || y >= totalRows)
                return 0;

            // Calculate index in byte array
            var index = (y * bytesPerRow) + (x / 2);
            if (index >= mapWalkableData.Length)
                return 0;

            // Extract the correct nibble (4 bits)
            var data = mapWalkableData[index];
            var shiftAmount = (x % 2 == 0) ? 0 : 4; // First or second nibble
            
            return (data >> shiftAmount) & 0xF; // 4-bit mask (0xF = 1111)
        }
    }
}