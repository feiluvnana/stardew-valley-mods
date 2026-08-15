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
    }
}
