using System;
using StardewModdingAPI;
using xTile;
using xTile.Layers;
using xTile.Tiles;

namespace BetterMap
{
    public static class MapPatcher
    {
        /// <summary>Applies map modifications to Island_S (Island South / Beach).</summary>
        public static void PatchIslandSouth(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                if (!config.RemoveBeachFarmWreck)
                    return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var alwaysFront = map.GetLayer("AlwaysFront");
                var back = map.GetLayer("Back");

                var tsIsland = map.GetTileSheet("untitled tile sheet"); // island_tilesheet_1

                // Remove the wreckage and obstacles along the western passage (x: 0..3, y: 11..16)
                for (int x = 0; x <= 3; x++)
                {
                    for (int y = 11; y <= 16; y++)
                    {
                        if (buildings != null && buildings.Tiles[x, y] != null)
                        {
                            buildings.Tiles[x, y]?.Properties.Clear();
                            buildings.Tiles[x, y] = null;
                        }

                        if (front != null && front.Tiles[x, y] != null)
                        {
                            front.Tiles[x, y] = null;
                        }

                        if (alwaysFront != null && alwaysFront.Tiles[x, y] != null)
                        {
                            alwaysFront.Tiles[x, y] = null;
                        }

                        // Set walkable clean sand tile on Back layer
                        if (back != null && tsIsland != null)
                        {
                            back.Tiles[x, y] = new StaticTile(back, tsIsland, BlendMode.Alpha, 72);
                        }
                    }
                }

                monitor.Log("Successfully patched Island_S: Removed beach-farm passage wreck.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching Island_S: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies map modifications to Island_W (Island West / Farm).</summary>
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

                // 1. Remove beach-farm passage wreckage on the east border of Island West (x: 101..106, y: 41..46)
                if (config.RemoveBeachFarmWreck)
                {
                    for (int x = 101; x <= 106; x++)
                    {
                        for (int y = 41; y <= 46; y++)
                        {
                            if (buildings != null && buildings.Tiles[x, y] != null)
                            {
                                buildings.Tiles[x, y]?.Properties.Clear();
                                buildings.Tiles[x, y] = null;
                            }

                            if (front != null && front.Tiles[x, y] != null)
                            {
                                front.Tiles[x, y] = null;
                            }

                            if (alwaysFront != null && alwaysFront.Tiles[x, y] != null)
                            {
                                alwaysFront.Tiles[x, y] = null;
                            }

                            if (back != null && tsOutdoors != null)
                            {
                                back.Tiles[x, y] = new StaticTile(back, tsOutdoors, BlendMode.Alpha, 201);
                            }
                        }
                    }
                    monitor.Log("Successfully patched Island_W: Removed east border transition obstacles.", LogLevel.Trace);
                }

                // 2. Remove southern beach shipwreck (x: 57..78, y: 88..98)
                if (config.RemoveIslandWestShipwreck)
                {
                    for (int x = 57; x <= 78; x++)
                    {
                        for (int y = 88; y <= 98; y++)
                        {
                            if (buildings != null && buildings.Tiles[x, y] != null)
                            {
                                buildings.Tiles[x, y]?.Properties.Clear();
                                buildings.Tiles[x, y] = null;
                            }

                            if (front != null && front.Tiles[x, y] != null)
                            {
                                front.Tiles[x, y] = null;
                            }

                            if (alwaysFront != null && alwaysFront.Tiles[x, y] != null)
                            {
                                alwaysFront.Tiles[x, y] = null;
                            }

                            // If Back layer has ship floor/wood tiles (indices >= 1500 in island_tilesheet_1), replace with sand
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
    }
}
