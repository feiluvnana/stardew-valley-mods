using System;
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
                var tsOutdoors = map.GetTileSheet("untitled tile sheet2"); // summer_outdoorsTileSheet

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

                // 2. Optional: Remove southern beach shipwreck (x: 57..78, y: 88..98)
                if (config.RemoveIslandWestShipwreck)
                {
                    for (int x = 57; x <= 78; x++)
                    {
                        for (int y = 88; y <= 98; y++)
                        {
                            if (buildings?.Tiles[x, y] != null)
                            {
                                buildings.Tiles[x, y]?.Properties.Clear();
                                buildings.Tiles[x, y] = null;
                            }

                            if (front?.Tiles[x, y] != null)
                            {
                                front.Tiles[x, y] = null;
                            }

                            if (alwaysFront?.Tiles[x, y] != null)
                            {
                                alwaysFront.Tiles[x, y] = null;
                            }

                            if (back != null && tsOutdoors != null)
                            {
                                var curTile = back.Tiles[x, y];
                                if (curTile != null && curTile.TileSheet.ImageSource.Contains("island_tilesheet_1") && curTile.TileIndex >= 1500)
                                {
                                    back.Tiles[x, y] = new StaticTile(back, tsOutdoors, BlendMode.Alpha, 201);
                                }
                            }
                        }
                    }
                    monitor.Log("Successfully patched Island_W: Removed southern beach shipwreck.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching Island_W: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies 3x1 exit widening and cleans upper surrounding wood moldings for IslandFarmHouse.</summary>
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

                if (buildings != null && front != null && tsIndoor != null)
                {
                    // Row above doorway (y=15): remove wood moldings from x=13, 14, 15 and place at outer edges x=12, 16
                    front.Tiles[12, 15] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[13, 15] = null;
                    front.Tiles[14, 15] = null;
                    front.Tiles[15, 15] = null;
                    front.Tiles[16, 15] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row at doorway top (y=16):
                    buildings.Tiles[12, 16] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[12, 16] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 64);
                    buildings.Tiles[16, 16] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[16, 16] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 68);

                    buildings.Tiles[13, 16] = null;
                    front.Tiles[13, 16] = null;
                    buildings.Tiles[14, 16] = null;
                    front.Tiles[14, 16] = null;
                    buildings.Tiles[15, 16] = null;
                    front.Tiles[15, 16] = null;

                    // Row at doorway bottom (y=17):
                    buildings.Tiles[12, 17] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[12, 17] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[16, 17] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[16, 17] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    buildings.Tiles[13, 17] = null;
                    buildings.Tiles[14, 17] = null;
                    buildings.Tiles[15, 17] = null;

                    front.Tiles[13, 17] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[14, 17] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[15, 17] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                }

                // Ensure Back floor tiles on x=13..15 at y=16..17
                if (back != null && tsIsland != null)
                {
                    back.Tiles[13, 16] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                    back.Tiles[14, 16] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                    back.Tiles[15, 16] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                    back.Tiles[13, 17] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                    back.Tiles[14, 17] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                    back.Tiles[15, 17] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                }

                // Update Map Warp property to 3x1
                map.Properties["Warp"] = "13 18 IslandWest 77 40 14 18 IslandWest 77 40 15 18 IslandWest 77 40";
                monitor.Log("Successfully patched IslandFarmHouse: Widened exit doorway to 3x1 and cleaned upper wood molding.", LogLevel.Trace);
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
                    // Row above doorway (y=10): remove wood moldings from x=2, 3, 4 and move to x=1, 5
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

                map.Properties["Warp"] = "2 12 Farm 64 15 3 12 Farm 64 15 4 12 Farm 64 15";
                monitor.Log("Successfully patched FarmHouse: Widened exit doorway to 3x1 and cleaned upper wood molding.", LogLevel.Trace);
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
                    // Row above doorway (y=10): remove wood moldings from x=8, 9, 10 and move to x=7, 11
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

                map.Properties["Warp"] = "8 12 Farm 64 15 9 12 Farm 64 15 10 12 Farm 64 15";
                monitor.Log("Successfully patched FarmHouse1: Widened exit doorway to 3x1 and cleaned upper wood molding.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching FarmHouse1: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies 3x1 exit widening and cleans upper surrounding wood moldings for FarmHouse2 (Level 2 Farmhouse).</summary>
        public static void PatchFarmHouse2(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                if (!config.WidenHouseExit) return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var tsIndoor = map.GetTileSheet("indoor");

                if (buildings != null && front != null && tsIndoor != null)
                {
                    // Row above doorway (y=29): remove wood moldings from x=26, 27, 28 and move to x=25, 29
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
                }

                map.Properties["Warp"] = "26 31 Farm 64 15 27 31 Farm 64 15 28 31 Farm 64 15";
                monitor.Log("Successfully patched FarmHouse2: Widened exit doorway to 3x1 and cleaned upper wood molding.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching FarmHouse2: {ex.Message}", LogLevel.Error);
            }
        }
    }
}
