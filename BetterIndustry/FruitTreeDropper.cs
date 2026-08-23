using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BetterIndustry
{
    public static class FruitTreeDropper
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        public static void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!Config.EnableAutoFruitDrop || !Context.IsMainPlayer)
                return;

            try
            {
                int threshold = Math.Max(1, Config.MaxFruitsBeforeDrop);
                int dropped = 0;

                foreach (GameLocation location in Game1.locations)
                {
                    if (location?.terrainFeatures == null)
                        continue;
                    dropped += DropExcessFruit(location, threshold);
                }

                if (dropped > 0)
                    Monitor.Log($"Auto-dropped {dropped} fruit(s) from fully loaded fruit trees.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error auto-dropping fruit tree fruit: {ex}", LogLevel.Error);
            }
        }

        private static int DropExcessFruit(GameLocation location, int threshold)
        {
            int dropped = 0;

            foreach (var pair in location.terrainFeatures.Pairs)
            {
                if (pair.Value is not FruitTree tree)
                    continue;
                if (tree.stump.Value || tree.daysUntilMature.Value > 0)
                    continue;

                var fruits = tree.fruit;
                if (fruits == null || fruits.Count < threshold)
                    continue;

                Vector2 origin = new Vector2(pair.Key.X * 64f + 32f, pair.Key.Y * 64f + 32f);
                foreach (Item fruit in fruits.ToArray())
                {
                    if (fruit == null)
                        continue;
                    Game1.createItemDebris(fruit, origin, Game1.random.Next(4), location);
                    dropped++;
                }
                fruits.Clear();
            }

            return dropped;
        }
    }
}
