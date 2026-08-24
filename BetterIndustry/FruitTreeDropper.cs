using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

// FruitTreeDropper automates harvesting: once per in-game day it scans every map and,
// whenever a mature fruit tree is holding the configured maximum number of fruit, knocks
// them all down as collectible debris so the player never has to click each fruit.
// It hooks SMAPI's DayStarted event instead of using a Harmony patch, because this is an
// extra daily action rather than a change to how existing game code behaves.
namespace BetterIndustry
{
    // "static class" refresher: cannot be instantiated with "new"; it's just a tidy
    // bundle of functions sharing state via ModEntry's static properties.
    /// <summary>
    /// Auto-drops fruit from fully-loaded fruit trees at the start of each day.
    /// </summary>
    public static class FruitTreeDropper
    {
        // Expression-bodied properties ("=>"): compact syntax for a read-only getter.
        // They just forward to the shared config/logger objects owned by ModEntry,
        // this mod's entry-point class.
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Day-start handler wired up in ModEntry: sweeps every location and drops
        /// fruit from any mature tree that has reached the trigger count.
        /// </summary>
        /// <param name="sender">Event source supplied by SMAPI (unused here).</param>
        /// <param name="e">Day-start details supplied by SMAPI (unused here).</param>
        public static void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Respect the config toggle. Context.IsMainPlayer is SMAPI's "is this client
            // the save host?" flag - only the host should spawn items, otherwise every
            // player in a multiplayer session would run this and duplicate the drops.
            if (!Config.EnableAutoFruitDrop || !Context.IsMainPlayer)
                return;

            // try/catch so one odd location can't crash SMAPI's whole day-start pipeline.
            try
            {
                // Math.Max clamps the setting so a bad value can never drop below 1.
                int threshold = Math.Max(1, Config.MaxFruitsBeforeDrop);
                int dropped = 0;

                // Game1.locations holds every map in the session (Farm, Town, interiors...).
                foreach (GameLocation location in Game1.locations)
                {
                    // "?." only continues when the left side isn't null, avoiding a
                    // NullReferenceException on sparse or unusual location entries.
                    if (location?.terrainFeatures == null)
                        continue;
                    dropped += DropExcessFruit(location, threshold);
                }

                // "$" marks an interpolated string: {dropped} is replaced at runtime.
                // LogLevel.Trace writes only to the log file - invisible in normal play.
                if (dropped > 0)
                    Monitor.Log($"Auto-dropped {dropped} fruit(s) from fully loaded fruit trees.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error auto-dropping fruit tree fruit: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Drops all fruit from every qualifying fruit tree on one map.
        /// </summary>
        /// <param name="location">The map whose trees are scanned.</param>
        /// <param name="threshold">Fruit count that triggers the drop.</param>
        /// <returns>Number of fruit items spawned as ground debris.</returns>
        private static int DropExcessFruit(GameLocation location, int threshold)
        {
            int dropped = 0;

            // terrainFeatures is a networked dictionary of tile-position -> feature
            // (trees, crops, grass, stones...). .Pairs enumerates its key/value pairs,
            // where pair.Key is the tile's grid coordinate.
            foreach (var pair in location.terrainFeatures.Pairs)
            {
                // Pattern matching: "is not FruitTree tree" type-tests AND assigns a
                // correctly-typed variable in one step. Non-fruit-trees are skipped.
                if (pair.Value is not FruitTree tree)
                    continue;
                // Skip chopped-down stumps and trees still maturing (daysUntilMature
                // counts down to 0 once the tree is fully grown).
                if (tree.stump.Value || tree.daysUntilMature.Value > 0)
                    continue;

                var fruits = tree.fruit;
                // Only fire once the tree carries at least the threshold number of fruit.
                if (fruits == null || fruits.Count < threshold)
                    continue;

                // Tiles are 64x64 pixels: tile coordinate * 64 gives pixels, and "+32"
                // aims at the tile centre so debris bursts out of the trunk.
                // Vector2 (imported from Microsoft.Xna.Framework at the top of this file)
                // is a simple X/Y floating-point struct used for all world positions.
                Vector2 origin = new Vector2(pair.Key.X * 64f + 32f, pair.Key.Y * 64f + 32f);
                // ToArray() snapshots the list because we Clear() it below while still in
                // this foreach - mutating a collection during iteration throws.
                // "Item" is the game's base class for anything inventory-storable; tree
                // fruits are held here as generic Item references.
                foreach (Item fruit in fruits.ToArray())
                {
                    if (fruit == null)
                        continue;
                    // Spawns the item as bouncing debris. Next(4) rolls 0-3 to pick a
                    // random fling direction; 'location' tells it which map to land on.
                    Game1.createItemDebris(fruit, origin, Game1.random.Next(4), location);
                    dropped++;
                }
                fruits.Clear();
            }

            return dropped;
        }
    }
}
