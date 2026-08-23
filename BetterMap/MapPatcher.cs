using System;
using System.Collections.Generic;
using StardewModdingAPI;
using xTile;
using xTile.Layers;
using xTile.Tiles;

namespace BetterMap
{
    public static class MapPatcher
    {
        /// <summary>Checks whether a tile index in island_tilesheet_1 belongs to the driftwood / log barrier or log piles.</summary>
        public static bool IsDriftwoodTile(int idx)
        {
            return (idx >= 806 && idx <= 810) ||
                   (idx >= 838 && idx <= 842) ||
                   (idx >= 870 && idx <= 874) ||
                   (idx >= 902 && idx <= 906) ||
                   (idx >= 935 && idx <= 936) ||
                   (idx >= 967 && idx <= 968) ||
                   (idx >= 105 && idx <= 107) ||
                   (idx >= 137 && idx <= 140) ||
                   (idx >= 169 && idx <= 172) ||
                   (idx >= 495 && idx <= 496) ||
                   (idx == 527) ||
                   (idx == 204) ||
                   (idx == 464);
        }

        /// <summary>Applies map modifications to Island_W (Island West / Ginger Island Farm).</summary>
        public static void PatchIslandWest(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var alwaysFront = map.GetLayer("AlwaysFront");
                var back = map.GetLayer("Back");

                var tsIsland = map.GetTileSheet("untitled tile sheet"); // island_tilesheet_1

                // 1. Remove the driftwood barrier fence and log piles across Ginger Island farm
                if (config.RemoveFarmDriftwoodBarrier)
                {
                    int driftwoodCount = 0;
                    for (int y = 0; y < map.Layers[0].LayerHeight; y++)
                    {
                        for (int x = 0; x < map.Layers[0].LayerWidth; x++)
                        {
                            // Check Buildings layer
                            var bTile = buildings?.Tiles[x, y];
                            if (bTile != null && bTile.TileSheet.ImageSource.Contains("island_tilesheet_1") && IsDriftwoodTile(bTile.TileIndex))
                            {
                                buildings?.Tiles[x, y]?.Properties.Clear();
                                if (buildings != null) buildings.Tiles[x, y] = null;
                                driftwoodCount++;

                                // Ensure Back layer has solid sand underneath
                                if (back != null && back.Tiles[x, y] == null && tsIsland != null)
                                {
                                    back.Tiles[x, y] = new StaticTile(back, tsIsland, BlendMode.Alpha, 101);
                                }
                            }

                            // Check Front layer
                            var fTile = front?.Tiles[x, y];
                            if (fTile != null && fTile.TileSheet.ImageSource.Contains("island_tilesheet_1") && IsDriftwoodTile(fTile.TileIndex))
                            {
                                if (front != null) front.Tiles[x, y] = null;
                            }

                            // Check AlwaysFront layer
                            var afTile = alwaysFront?.Tiles[x, y];
                            if (afTile != null && afTile.TileSheet.ImageSource.Contains("island_tilesheet_1") && IsDriftwoodTile(afTile.TileIndex))
                            {
                                if (alwaysFront != null) alwaysFront.Tiles[x, y] = null;
                            }
                        }
                    }
                    monitor.Log($"Successfully patched Island_W: Removed {driftwoodCount} driftwood fence/log tiles.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching Island_W: {ex.Message}", LogLevel.Error);
            }
        }

        private static void UpdateExitWarps(Map map, string targetMap, int[] exitXCoords, int exitY, int targetX, int targetY)
        {
            var newWarpTokens = new List<string>();

            if (map.Properties.TryGetValue("Warp", out var existingWarpObj) && existingWarpObj != null)
            {
                string[] parts = existingWarpObj.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i + 4 < parts.Length; i += 5)
                {
                    string xStr = parts[i];
                    string yStr = parts[i + 1];
                    string destMap = parts[i + 2];
                    string destX = parts[i + 3];
                    string destY = parts[i + 4];

                    // If this is an existing exit warp to the same destination map, skip it
                    if (destMap.Equals(targetMap, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Keep all other warps (Cellar, Spouse Room, etc.)
                    newWarpTokens.Add($"{xStr} {yStr} {destMap} {destX} {destY}");
                }
            }

            // Append the 3 new exit doorway warps
            foreach (int x in exitXCoords)
            {
                newWarpTokens.Add($"{x} {exitY} {targetMap} {targetX} {targetY}");
            }

            map.Properties["Warp"] = string.Join(" ", newWarpTokens);
        }

        /// <summary>Applies seamless 3x1 exit widening to IslandFarmHouse, perfectly flush with the living room bottom wall.</summary>
        public static void PatchIslandFarmHouse(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                if (!config.WidenHouseExit) return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var back = map.GetLayer("Back");
                var tsIndoor = map.GetTileSheet("indoor");
                var tsIsland = map.GetTileSheet("untitled tile sheet");

                if (buildings != null && front != null && back != null && tsIndoor != null && tsIsland != null)
                {
                    // Row 15 (Upper wall molding above doorway):
                    front.Tiles[12, 15] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[13, 15] = null;
                    front.Tiles[14, 15] = null;
                    front.Tiles[15, 15] = null;
                    front.Tiles[16, 15] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row 16 (Doorway opening & frames):
                    buildings.Tiles[12, 16] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[12, 16] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[16, 16] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[16, 16] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    // 3-wide doorway mats at x=13, 14, 15
                    for (int x = 13; x <= 15; x++)
                    {
                        back.Tiles[x, 16] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                        buildings.Tiles[x, 16] = null;
                        front.Tiles[x, 16] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    }

                    // Row 17 (Exit pathway row below walls & doorway):
                    back.Tiles[12, 17] = null;
                    buildings.Tiles[12, 17] = null;
                    front.Tiles[12, 17] = null;

                    back.Tiles[16, 17] = null;
                    buildings.Tiles[16, 17] = null;
                    front.Tiles[16, 17] = null;

                    // 3-wide exit pathway across x=13..15
                    for (int x = 13; x <= 15; x++)
                    {
                        back.Tiles[x, 17] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                        buildings.Tiles[x, 17] = null;
                        front.Tiles[x, 17] = null;
                    }

                    // Map Warps: allow exit by stepping down onto row 18
                    UpdateExitWarps(map, "IslandWest", new[] { 13, 14, 15 }, 18, 77, 40);
                    monitor.Log("Successfully patched IslandFarmHouse: Applied seamless 3x1 exit doorway flush with bottom wall.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching IslandFarmHouse: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies 3x1 exit widening and cleans upper surrounding wood moldings for FarmHouse (Starter Farmhouse).</summary>
        public static void PatchFarmHouse(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                if (!config.WidenHouseExit) return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var tsIndoor = map.GetTileSheet("indoor");

                if (buildings != null && front != null && tsIndoor != null)
                {
                    // Row above doorway (y=10): wood corner moldings at x=1 and x=5
                    front.Tiles[1, 10] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[2, 10] = null;
                    front.Tiles[3, 10] = null;
                    front.Tiles[4, 10] = null;
                    front.Tiles[5, 10] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row at doorway bottom (y=11):
                    buildings.Tiles[1, 11] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[1, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[5, 11] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[5, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    buildings.Tiles[2, 11] = null;
                    buildings.Tiles[3, 11] = null;
                    buildings.Tiles[4, 11] = null;

                    front.Tiles[2, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[3, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[4, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                }

                UpdateExitWarps(map, "Farm", new[] { 2, 3, 4 }, 12, 64, 15);
                monitor.Log("Successfully patched FarmHouse: Widened exit doorway to 3x1.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching FarmHouse: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies 3x1 exit widening and cleans upper surrounding wood moldings for FarmHouse1 (Level 1 Farmhouse).</summary>
        public static void PatchFarmHouse1(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                if (!config.WidenHouseExit) return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var tsIndoor = map.GetTileSheet("indoor");

                if (buildings != null && front != null && tsIndoor != null)
                {
                    // Row above doorway (y=10): wood corner moldings at x=7 and x=11
                    front.Tiles[7, 10] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[8, 10] = null;
                    front.Tiles[9, 10] = null;
                    front.Tiles[10, 10] = null;
                    front.Tiles[11, 10] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row at doorway bottom (y=11):
                    buildings.Tiles[7, 11] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[7, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[11, 11] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[11, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    buildings.Tiles[8, 11] = null;
                    buildings.Tiles[9, 11] = null;
                    buildings.Tiles[10, 11] = null;

                    front.Tiles[8, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[9, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[10, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                }

                UpdateExitWarps(map, "Farm", new[] { 8, 9, 10 }, 12, 64, 15);
                monitor.Log("Successfully patched FarmHouse1: Widened exit doorway to 3x1.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching FarmHouse1: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies 3x1 exit widening and cleans upper surrounding wood moldings for FarmHouse2 (Level 2 Farmhouse & Marriage Layout).</summary>
        public static void PatchFarmHouse2(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                if (!config.WidenHouseExit) return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var back = map.GetLayer("Back");
                var tsIndoor = map.GetTileSheet("indoor");

                if (buildings != null && front != null && back != null && tsIndoor != null)
                {
                    // Upper door frame molding row (y=29):
                    front.Tiles[25, 29] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[26, 29] = null;
                    front.Tiles[27, 29] = null;
                    front.Tiles[28, 29] = null;
                    front.Tiles[29, 29] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row at doorway bottom (y=30):
                    buildings.Tiles[25, 30] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[25, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[29, 30] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[29, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    buildings.Tiles[26, 30] = null;
                    buildings.Tiles[27, 30] = null;
                    buildings.Tiles[28, 30] = null;

                    front.Tiles[26, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[27, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[28, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);

                    // Row at exit step (y=31):
                    // Left vertical dividing wall continues down at x=25
                    buildings.Tiles[25, 31] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[25, 31] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 68);

                    // Clear any void blocker tiles at x=26, 27, 28 so player can exit across full 3-tile width
                    for (int x = 26; x <= 28; x++)
                    {
                        back.Tiles[x, 31] = null;
                        buildings.Tiles[x, 31] = null;
                        front.Tiles[x, 31] = null;
                    }

                    // Clear below right door jamb base at (29, 31)
                    back.Tiles[29, 31] = null;
                    buildings.Tiles[29, 31] = null;
                    front.Tiles[29, 31] = null;
                }

                UpdateExitWarps(map, "Farm", new[] { 26, 27, 28 }, 31, 64, 15);
                monitor.Log("Successfully patched FarmHouse2: Widened exit doorway to 3x1.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching FarmHouse2: {ex.Message}", LogLevel.Error);
            }
        }
    }
}
