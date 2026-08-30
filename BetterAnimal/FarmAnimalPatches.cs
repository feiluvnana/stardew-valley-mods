using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace BetterAnimal
{
    /// <summary>
    /// Harmony patches on <see cref="FarmAnimal"/> to implement high-friendship duck dual drops,
    /// rabbit multi-drops, and sheep daily shearing at max friendship.
    /// </summary>
    public static class FarmAnimalPatches
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Applies Harmony patches to FarmAnimal.dayUpdate.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var method = AccessTools.Method(
                    typeof(FarmAnimal),
                    nameof(FarmAnimal.dayUpdate),
                    new[] { typeof(GameLocation) }
                );

                if (method != null)
                {
                    harmony.Patch(
                        original: method,
                        postfix: new HarmonyMethod(typeof(FarmAnimalPatches), nameof(DayUpdate_Postfix))
                    );
                    Monitor.Log("Hooked FarmAnimal.dayUpdate successfully.", LogLevel.Trace);
                }
                else
                {
                    Monitor.Log("Could not locate FarmAnimal.dayUpdate method.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply FarmAnimalPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Postfix on FarmAnimal.dayUpdate: evaluates produce and grants dual drops for ducks,
        /// bonus yields for rabbits, and daily wool readiness for happy sheep.
        /// </summary>
        public static void DayUpdate_Postfix(FarmAnimal __instance, GameLocation environtment)
        {
            if (__instance == null)
                return;

            try
            {
                string animalType = __instance.type?.Value ?? string.Empty;
                int hearts = __instance.friendshipTowardFarmer.Value / 200;

                // 1. Duck Dual Drop: When a high-friendship duck rolls a Feather, grant the Duck Egg as well
                if (Config.EnableDuckDualDrop && animalType.Contains("Duck", StringComparison.OrdinalIgnoreCase))
                {
                    string currentProduce = __instance.currentProduce?.Value ?? string.Empty;
                    bool isDuckFeather = currentProduce is "444" or "(O)444" || currentProduce.Equals("DuckFeather", StringComparison.OrdinalIgnoreCase);

                    if (isDuckFeather && hearts >= Config.DuckDualDropMinHearts)
                    {
                        double rollChance = hearts >= 5 ? Config.DuckDualDropChance : (Config.DuckDualDropChance * 0.75);
                        if (Game1.random.NextDouble() <= rollChance)
                        {
                            int quality = __instance.produceQuality?.Value ?? 0;
                            SpawnProduce(__instance, "442", quality); // Duck Egg (442)
                            Monitor.Log($"BetterAnimal: High-friendship duck '{__instance.Name}' dropped bonus Duck Egg alongside Duck Feather.", LogLevel.Trace);
                        }
                    }
                }

                // 2. Rabbit Multi-Drop: High-friendship rabbits have a chance to drop multiple wool or a lucky foot
                if (Config.EnableRabbitMultiDrop && animalType.Contains("Rabbit", StringComparison.OrdinalIgnoreCase))
                {
                    string currentProduce = __instance.currentProduce?.Value ?? string.Empty;
                    if (!string.IsNullOrEmpty(currentProduce) && hearts >= 3)
                    {
                        if (Game1.random.NextDouble() <= Config.RabbitMultiDropChance)
                        {
                            // 25% chance for bonus Lucky Foot at high hearts, otherwise bonus Wool
                            string bonusItemId = (Game1.random.NextDouble() < (0.15 + (hearts * 0.05))) ? "446" : "440";
                            int quality = __instance.produceQuality?.Value ?? 0;
                            SpawnProduce(__instance, bonusItemId, quality);
                            Monitor.Log($"BetterAnimal: Rabbit '{__instance.Name}' produced bonus item ID '{bonusItemId}'.", LogLevel.Trace);
                        }
                    }
                }

                // 3. Sheep Daily Shearing: Ready to shear daily at 5 hearts
                if (Config.EnableSheepDailyShearAtMaxHearts && animalType.Contains("Sheep", StringComparison.OrdinalIgnoreCase))
                {
                    if (hearts >= 5)
                    {
                        __instance.daysSinceLastLay.Value = 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in FarmAnimalPatches DayUpdate_Postfix: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Spawns a secondary animal product in the animal's building (depositing into Auto-Grabber if present, or on the floor).
        /// </summary>
        private static void SpawnProduce(FarmAnimal animal, string itemId, int quality)
        {
            if (animal == null)
                return;

            var obj = new StardewValley.Object(itemId, 1)
            {
                Quality = quality
            };

            if (animal.home?.indoors?.Value is AnimalHouse animalHouse)
            {
                // 1. Try depositing into an Auto-Grabber inside the building
                foreach (var placement in animalHouse.Objects.Values)
                {
                    if (placement is Chest grabber && (placement.ItemId == "165" || placement.QualifiedItemId == "(BC)165" || placement.Name.Contains("Auto-Grabber")))
                    {
                        Item? remaining = grabber.addItem(obj);
                        if (remaining == null)
                            return; // Successfully deposited into Auto-Grabber
                    }
                }

                // 2. Otherwise place on the floor
                Point tilePoint = animal.TilePoint;
                Vector2 originTile = new(tilePoint.X, tilePoint.Y);

                if (!animalHouse.Objects.ContainsKey(originTile))
                {
                    animalHouse.Objects.Add(originTile, obj);
                    return;
                }

                // Search nearby tiles for an open spot
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        Vector2 candidate = new(originTile.X + dx, originTile.Y + dy);
                        if (animalHouse.isTileOnMap(candidate) && !animalHouse.Objects.ContainsKey(candidate))
                        {
                            animalHouse.Objects.Add(candidate, obj);
                            return;
                        }
                    }
                }

                // Fallback: spawn as debris
                Game1.createItemDebris(obj, new Vector2(originTile.X * 64, originTile.Y * 64), 0, animalHouse);
            }
        }
    }
}
