// =============================================================================
//  MapPatcher — the tile-surgery toolbox behind BetterMap.
//  Every method here is `static` (utility style: no objects are created; you
//  simply call the methods directly). Two jobs:
//    1. Delete the driftwood clutter blocking Ginger Island West's farm.
//    2. Widen farmhouse exit doorways from 1 tile to 3 tiles wide.
//  GAME BACKGROUND: Stardew maps are xTile "Map" objects built from stacked
//  LAYERS of 16x16-pixel cells ("tiles"). Each tile slot either holds a TILE
//  INDEX pointing into a TILE SHEET (one large PNG atlas the game slices the
//  picture from) or is empty. Editing those slots is how visuals and collision
//  change — no image editing required.
// =============================================================================
using StardewModdingAPI;
using xTile;
using xTile.Tiles;

namespace BetterMap
{
    /// <summary>
    /// Stateless collection of map-editing routines invoked from ModEntry's
    /// asset-requested handler: one cleanup routine for Island West, one patch
    /// per farmhouse variant, plus a shared exit-warp rewriting helper.
    /// </summary>
    /// <remarks>
    /// THE FOUR STANDARD LAYERS (bottom to top):
    ///   Back        : ground you walk on — dirt, floors, paths.
    ///   Buildings   : solid/colliding things (walls, furniture hitboxes) and
    ///                 interactive tiles (doors keep their "Action" property here).
    ///   Front       : decoration drawn OVER the player where appropriate —
    ///                 wall tops, counter faces, floor mats.
    ///   AlwaysFront : always drawn above the player — roof peaks, tree canopies.
    /// A tile index counts cells across a tilesheet image left-to-right,
    /// top-to-bottom; index 0 is the top-left 16x16 square.
    /// </remarks>
    public static class MapPatcher
    {
        /// <summary>Checks whether a tile index in island_tilesheet_1 belongs to the driftwood / log barrier or log piles.</summary>
        /// <remarks>
        /// These index ranges were identified by inspecting the island outdoor
        /// tilesheet: they cover driftwood fence segments, washed-up logs, log
        /// pile clusters, and a few lone prop tiles. This is a PURE FUNCTION:
        /// same input always gives the same answer, no state involved.
        /// </remarks>
        public static bool IsDriftwoodTile(int idx)
        {
            // `idx` is a tile index (position within the tilesheet atlas).
            // Each `(idx >= a && idx <= b)` is an inclusive RANGE check;
            // `||` (logical OR) chains them so ANY match returns true.
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
        /// <remarks>
        /// Walks every cell of the map and deletes any driftwood/log tile found
        /// on the three visible layers, backfilling bare ground with sand so no
        /// holes remain. Called from ModEntry whenever Maps/Island_W loads.
        /// </remarks>
        public static void PatchIslandWest(Map map, ModConfig config, IMonitor monitor)
        {
            // TRY/CATCH: if anything unexpected goes wrong we log an error and
            // keep playing instead of crashing the whole game.
            try
            {
                // Fetch each drawing layer BY NAME; `var` asks the compiler to
                // infer the type. Layers can be missing on odd/modified maps,
                // hence the null-safe (`?.`) usage further down.
                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var alwaysFront = map.GetLayer("AlwaysFront");
                var back = map.GetLayer("Back");

                // Grab the island outdoor tilesheet by its INTERNAL id — this
                // particular map file literally names it "untitled tile sheet"
                // even though its PNG file is island_tilesheet_1.png.
                var tsIsland = map.GetTileSheet("untitled tile sheet"); // island_tilesheet_1

                // 1. Remove the driftwood barrier fence and log piles across Ginger Island farm
                // Feature gate: only run when the user enabled it in config/GMCM.
                if (config.RemoveFarmDriftwoodBarrier)
                {
                    // Local counter just for the summary log line.
                    int driftwoodCount = 0;
                    // Scan EVERY cell: outer loop = rows (y), inner = columns (x).
                    // Layers[0]'s width/height define the grid all layers share.
                    for (int y = 0; y < map.Layers[0].LayerHeight; y++)
                    {
                        for (int x = 0; x < map.Layers[0].LayerWidth; x++)
                        {
                            // Check Buildings layer
                            // `?.` null-conditional: if the layer or tile is
                            // missing the result is simply null — no crash.
                            var bTile = buildings?.Tiles[x, y];
                            // Three-part test: tile exists, BELONGS to the island
                            // sheet (so other mods' tiles are never touched), and
                            // matches a known driftwood/log index.
                            if (bTile != null && bTile.TileSheet.ImageSource.Contains("island_tilesheet_1") && IsDriftwoodTile(bTile.TileIndex))
                            {
                                // Erase any map properties glued to the tile
                                // (e.g. "Action", "Shadow") so nothing invisible lingers.
                                buildings?.Tiles[x, y]?.Properties.Clear();
                                // Setting the slot to null DELETES the tile —
                                // its collision disappears along with it.
                                if (buildings != null) buildings.Tiles[x, y] = null;
                                driftwoodCount++;

                                // Ensure Back layer has solid sand underneath
                                // So the freed cell doesn't reveal void/blackness,
                                // stamp tile #101 (island sand) onto the ground layer.
                                if (back != null && back.Tiles[x, y] == null && tsIsland != null)
                                {
                                    // StaticTile = one fixed tile from a fixed sheet;
                                    // BlendMode.Alpha honors the PNG's transparent pixels.
                                    back.Tiles[x, y] = new StaticTile(back, tsIsland, BlendMode.Alpha, 101);
                                }
                            }

                            // Check Front layer
                            // Same test on the overlay layer; front tiles have no
                            // gameplay properties worth keeping, so just delete.
                            var fTile = front?.Tiles[x, y];
                            if (fTile != null && fTile.TileSheet.ImageSource.Contains("island_tilesheet_1") && IsDriftwoodTile(fTile.TileIndex))
                            {
                                if (front != null) front.Tiles[x, y] = null;
                            }

                            // Check AlwaysFront layer
                            // And finally the topmost layer, same pattern again.
                            var afTile = alwaysFront?.Tiles[x, y];
                            if (afTile != null && afTile.TileSheet.ImageSource.Contains("island_tilesheet_1") && IsDriftwoodTile(afTile.TileIndex))
                            {
                                if (alwaysFront != null) alwaysFront.Tiles[x, y] = null;
                            }
                        }
                    }
                    // `$"..."` is STRING INTERPOLATION: {driftwoodCount} gets
                    // replaced by the variable's value at runtime. Trace-level
                    // logs stay hidden unless verbose logging is switched on.
                    monitor.Log($"Successfully patched Island_W: Removed {driftwoodCount} driftwood fence/log tiles.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                // Report and continue; ex.Message holds only the error text,
                // not the full stack trace, keeping the log tidy.
                monitor.Log($"Error patching Island_W: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Rebuilds the map's "Warp" property so stepping onto the given doorway
        /// tiles teleports the player into the target map, while preserving all
        /// unrelated warps (cellar ladder, spouse-room door, etc.).
        /// </summary>
        /// <param name="map">The map whose Warp property will be rewritten.</param>
        /// <param name="targetMap">Destination map name our new warps lead to.</param>
        /// <param name="exitXCoords">X columns of each new doorway warp tile.</param>
        /// <param name="exitY">Shared Y row of the new doorway warp tiles.</param>
        /// <param name="targetX">Landing X inside the destination map.</param>
        /// <param name="targetY">Landing Y inside the destination map.</param>
        /// <remarks>
        /// THE WARP FORMAT: map.Properties["Warp"] is ONE long SPACE-separated
        /// string; every FIVE consecutive tokens describe one warp:
        ///     sourceX sourceY destinationMapName destinationX destinationY
        /// Extra warps are simply concatenated into the same string — which is
        /// why this method splits it apart, filters it, and rejoins it.
        /// </remarks>
        private static void UpdateExitWarps(Map map, string targetMap, int[] exitXCoords, int exitY, int targetX, int targetY)
        {
            // A resizable list collecting the warp strings we'll glue together
            // at the end (List<string> = growable array of text).
            var newWarpTokens = new List<string>();

            // TryGetValue fetches a property safely: `out var existingWarpObj`
            // both declares a variable and fills it; the condition is false and
            // the block skipped when the key doesn't exist at all.
            if (map.Properties.TryGetValue("Warp", out var existingWarpObj) && existingWarpObj != null)
            {
                // Chop the warp string into single tokens; RemoveEmptyEntries
                // drops stray double spaces so the token indexing stays aligned.
                string[] parts = existingWarpObj.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                // Step through FIVE tokens at a time (= exactly one warp).
                // The guard `i + 4 < parts.Length` skips a dangling partial warp
                // left behind by another mod, preventing out-of-range errors.
                for (int i = 0; i + 4 < parts.Length; i += 5)
                {
                    string xStr = parts[i];
                    string yStr = parts[i + 1];
                    string destMap = parts[i + 2];
                    string destX = parts[i + 3];
                    string destY = parts[i + 4];

                    // If this is an existing exit warp to the same destination map, skip it
                    // Drop OLD exits toward the same place — fresh ones for the
                    // widened door get appended below. OrdinalIgnoreCase makes
                    // the comparison case-insensitive ("farm" equals "Farm").
                    if (destMap.Equals(targetMap, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Keep all other warps (Cellar, Spouse Room, etc.)
                    // `$"{...}"` interpolation rebuilds one warp's five tokens.
                    newWarpTokens.Add($"{xStr} {yStr} {destMap} {destX} {destY}");
                }
            }

            // Append the 3 new exit doorway warps
            // `foreach` visits each doorway column stored in the array, in order.
            foreach (int x in exitXCoords)
            {
                newWarpTokens.Add($"{x} {exitY} {targetMap} {targetX} {targetY}");
            }

            // Join every collected warp with single spaces and write the
            // finished string back into the map property.
            map.Properties["Warp"] = string.Join(" ", newWarpTokens);
        }

        /// <summary>Applies seamless 3x1 exit widening to IslandFarmHouse, perfectly flush with the living room bottom wall.</summary>
        /// <remarks>
        /// STRATEGY: redraw the wall rows around the old 1-wide door so the
        /// opening spans columns x=13..15, remove collision between them,
        /// restamp floor mats/walkway tiles, then register three side-by-side
        /// exit warps. Indoor-sheet ids used: 162/163 = wall molding corner
        /// caps, 64/68 = vertical door-jamb posts (with collision), 96/130 =
        /// their decorative Front faces, 165 = floor mat overlay, and island
        /// sheet 181 = sandy walkway ground.
        /// </remarks>
        public static void PatchIslandFarmHouse(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                // Early bail-out when the user disabled this feature: `!` is
                // logical NOT, and a bare `return;` exits a void method.
                if (!config.WidenHouseExit) return;

                // Collect every layer and tilesheet this patch touches.
                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var back = map.GetLayer("Back");
                // The "indoor" sheet supplies interior walls/floors; the
                // untitled sheet provides sandy island flooring.
                var tsIndoor = map.GetTileSheet("indoor");
                var tsIsland = map.GetTileSheet("untitled tile sheet");

                // Guard clause: proceed ONLY when everything resolved — prevents
                // NullReferenceException on unusual or heavily-modded maps.
                if (buildings != null && front != null && back != null && tsIndoor != null && tsIsland != null)
                {
                    // Row 15 (Upper wall molding above doorway):
                    // Cap the widened opening with corner moldings; emptying
                    // (nulling) the three middle cells opens up the wall gap.
                    front.Tiles[12, 15] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[13, 15] = null;
                    front.Tiles[14, 15] = null;
                    front.Tiles[15, 15] = null;
                    front.Tiles[16, 15] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row 16 (Doorway opening & frames):
                    // Left/right jamb posts go on Buildings (they collide);
                    // their visible faces go on Front above them.
                    buildings.Tiles[12, 16] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[12, 16] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[16, 16] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[16, 16] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    // 3-wide doorway mats at x=13, 14, 15
                    // For the open middle: sandy ground underneath, NO collision
                    // in Buildings, and a mat overlay drawn on Front.
                    for (int x = 13; x <= 15; x++)
                    {
                        back.Tiles[x, 16] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                        buildings.Tiles[x, 16] = null;
                        front.Tiles[x, 16] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    }

                    // Row 17 (Exit pathway row below walls & doorway):
                    // Clear leftover wall fragments on both outer sides so the
                    // player isn't blocked stepping down.
                    back.Tiles[12, 17] = null;
                    buildings.Tiles[12, 17] = null;
                    front.Tiles[12, 17] = null;

                    back.Tiles[16, 17] = null;
                    buildings.Tiles[16, 17] = null;
                    front.Tiles[16, 17] = null;

                    // 3-wide exit pathway across x=13..15
                    // Lay the walkway strip across the full opening width.
                    for (int x = 13; x <= 15; x++)
                    {
                        back.Tiles[x, 17] = new StaticTile(back, tsIsland, BlendMode.Alpha, 181);
                        buildings.Tiles[x, 17] = null;
                        front.Tiles[x, 17] = null;
                    }

                    // Map Warps: allow exit by stepping down onto row 18
                    // Register warps at (13..15, 18): stepping on ANY of them
                    // teleports the player OUTSIDE to IslandWest tile (77, 40) —
                    // right in front of the island farmhouse door.
                    // `new[] { 13, 14, 15 }` is an implicitly-typed array literal.
                    UpdateExitWarps(map, "IslandWest", new[] { 13, 14, 15 }, 18, 77, 40);
                    monitor.Log("Successfully patched IslandFarmHouse: Applied seamless 3x1 exit doorway flush with bottom wall.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                // Never let a bad map take the game down with us.
                monitor.Log($"Error patching IslandFarmHouse: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies 3x1 exit widening and cleans upper surrounding wood moldings for FarmHouse (Starter Farmhouse).</summary>
        /// <remarks>
        /// Starter-cabin geometry differs from later upgrades: the door sits at
        /// columns x=2..4 with jambs at x=1/x=5, molding row y=10, doorway row
        /// y=11, and exit warps on row 12 leading to Farm tile (64, 15).
        /// </remarks>
        public static void PatchFarmHouse(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                // Respect the config toggle before touching anything.
                if (!config.WidenHouseExit) return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var tsIndoor = map.GetTileSheet("indoor");

                // Only edit when all needed pieces exist.
                if (buildings != null && front != null && tsIndoor != null)
                {
                    // Row above doorway (y=10): wood corner moldings at x=1 and x=5
                    // Re-cap the widened gap so the wall still looks framed.
                    front.Tiles[1, 10] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[2, 10] = null;
                    front.Tiles[3, 10] = null;
                    front.Tiles[4, 10] = null;
                    front.Tiles[5, 10] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row at doorway bottom (y=11):
                    // Jamb posts (colliding) plus their front faces frame the
                    // new 3-wide opening at columns 1 and 5.
                    buildings.Tiles[1, 11] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[1, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[5, 11] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[5, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    // Remove collision from the middle columns — that IS the
                    // actual widening; everything else is cosmetics.
                    buildings.Tiles[2, 11] = null;
                    buildings.Tiles[3, 11] = null;
                    buildings.Tiles[4, 11] = null;

                    // Draw doormats across all three open middle columns.
                    front.Tiles[2, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[3, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[4, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                }

                // Warps on row 12 at x=2..4 drop the player outside at Farm (64, 15).
                UpdateExitWarps(map, "Farm", new[] { 2, 3, 4 }, 12, 64, 15);
                monitor.Log("Successfully patched FarmHouse: Widened exit doorway to 3x1.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching FarmHouse: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies 3x1 exit widening and cleans upper surrounding wood moldings for FarmHouse1 (Level 1 Farmhouse).</summary>
        /// <remarks>
        /// Identical technique to PatchFarmHouse but shifted for this larger
        /// layout: jambs at x=7/x=11, molding row y=10, doorway row y=11, exit
        /// warps on row 12 leading to Farm tile (64, 15). Also used for the
        /// "_marriage" variant since the door position matches.
        /// </remarks>
        public static void PatchFarmHouse1(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                // Respect the config toggle before touching anything.
                if (!config.WidenHouseExit) return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var tsIndoor = map.GetTileSheet("indoor");

                // Only edit when all needed pieces exist.
                if (buildings != null && front != null && tsIndoor != null)
                {
                    // Row above doorway (y=10): wood corner moldings at x=7 and x=11
                    front.Tiles[7, 10] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[8, 10] = null;
                    front.Tiles[9, 10] = null;
                    front.Tiles[10, 10] = null;
                    front.Tiles[11, 10] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row at doorway bottom (y=11):
                    // Frame the widened opening with jamb posts + faces.
                    buildings.Tiles[7, 11] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[7, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[11, 11] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[11, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    // Open the middle columns by deleting their collision.
                    buildings.Tiles[8, 11] = null;
                    buildings.Tiles[9, 11] = null;
                    buildings.Tiles[10, 11] = null;

                    // Doormats across the three open columns.
                    front.Tiles[8, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[9, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[10, 11] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                }

                // Warps on row 12 at x=8..10 lead outside to Farm (64, 15).
                UpdateExitWarps(map, "Farm", new[] { 8, 9, 10 }, 12, 64, 15);
                monitor.Log("Successfully patched FarmHouse1: Widened exit doorway to 3x1.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error patching FarmHouse1: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>Applies 3x1 exit widening and cleans upper surrounding wood moldings for FarmHouse2 (Level 2 Farmhouse & Marriage Layout).</summary>
        /// <remarks>
        /// Largest upgrade layout: jambs at x=25/x=29, molding row y=29,
        /// doorway row y=30, plus an extra EXIT STEP row (y=31) that must be
        /// cleared of invisible blockers so the player can actually walk out
        /// across all three tiles. Exit warps sit on row 31 → Farm (64, 15).
        /// </remarks>
        public static void PatchFarmHouse2(Map map, ModConfig config, IMonitor monitor)
        {
            try
            {
                // Respect the config toggle before touching anything.
                if (!config.WidenHouseExit) return;

                var buildings = map.GetLayer("Buildings");
                var front = map.GetLayer("Front");
                var back = map.GetLayer("Back");
                var tsIndoor = map.GetTileSheet("indoor");

                // Only edit when all needed pieces exist.
                if (buildings != null && front != null && back != null && tsIndoor != null)
                {
                    // Upper door frame molding row (y=29):
                    front.Tiles[25, 29] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 162);
                    front.Tiles[26, 29] = null;
                    front.Tiles[27, 29] = null;
                    front.Tiles[28, 29] = null;
                    front.Tiles[29, 29] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 163);

                    // Row at doorway bottom (y=30):
                    // Jamb posts + faces frame the opening at columns 25 and 29.
                    buildings.Tiles[25, 30] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 64);
                    front.Tiles[25, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 96);
                    buildings.Tiles[29, 30] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[29, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 130);

                    // Delete collision across the three open middle columns.
                    buildings.Tiles[26, 30] = null;
                    buildings.Tiles[27, 30] = null;
                    buildings.Tiles[28, 30] = null;

                    // Doormats across the three open columns.
                    front.Tiles[26, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[27, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);
                    front.Tiles[28, 30] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 165);

                    // Row at exit step (y=31):
                    // Left vertical dividing wall continues down at x=25
                    // Keep the interior wall edge intact below the left jamb.
                    buildings.Tiles[25, 31] = new StaticTile(buildings, tsIndoor, BlendMode.Alpha, 68);
                    front.Tiles[25, 31] = new StaticTile(front, tsIndoor, BlendMode.Alpha, 68);

                    // Clear any void blocker tiles at x=26, 27, 28 so player can exit across full 3-tile width
                    // Nulling ALL layers here removes invisible "you shall not
                    // pass"/void tiles that would otherwise stop movement.
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

                // Warps on row 31 at x=26..28 drop the player at Farm (64, 15).
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
