using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Objects;

namespace BetterAnimal
{
    /// <summary>
    /// Harmony patches on <see cref="FarmAnimal"/>, <see cref="SlimeHutch"/>, and <see cref="StardewValley.Object"/>
    /// to implement high-friendship multi-drops (ducks, rabbits, goats, dinosaurs, void chickens),
    /// sheep daily shearing, and Slime Hutch quantity scaling.
    /// </summary>
    public static class FarmAnimalPatches
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Applies Harmony patches across FarmAnimal, SlimeHutch, Object, and MachineDataUtility.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                // 1. Hook FarmAnimal.dayUpdate
                var animalDayUpdate = AccessTools.Method(
                    typeof(FarmAnimal),
                    nameof(FarmAnimal.dayUpdate),
                    new[] { typeof(GameLocation) }
                );
                if (animalDayUpdate != null)
                {
                    harmony.Patch(
                        original: animalDayUpdate,
                        postfix: new HarmonyMethod(typeof(FarmAnimalPatches), nameof(DayUpdate_Postfix))
                    );
                    Monitor.Log("Hooked FarmAnimal.dayUpdate successfully.", LogLevel.Trace);
                }

                // 2. Hook SlimeHutch.DayUpdate
                var slimeHutchDayUpdate = AccessTools.Method(
                    typeof(SlimeHutch),
                    nameof(SlimeHutch.DayUpdate),
                    new[] { typeof(int) }
                );
                if (slimeHutchDayUpdate != null)
                {
                    harmony.Patch(
                        original: slimeHutchDayUpdate,
                        postfix: new HarmonyMethod(typeof(FarmAnimalPatches), nameof(SlimeHutch_DayUpdate_Postfix))
                    );
                    Monitor.Log("Hooked SlimeHutch.DayUpdate successfully.", LogLevel.Trace);
                }

                // 3. Hook Object.performRemoveAction (for Slime Ball pop bonus)
                var removeAction = AccessTools.Method(
                    typeof(StardewValley.Object),
                    nameof(StardewValley.Object.performRemoveAction),
                    new[] { typeof(GameLocation) }
                );
                if (removeAction != null)
                {
                    harmony.Patch(
                        original: removeAction,
                        postfix: new HarmonyMethod(typeof(FarmAnimalPatches), nameof(PerformRemoveAction_Postfix))
                    );
                    Monitor.Log("Hooked Object.performRemoveAction successfully.", LogLevel.Trace);
                }

                // 4. Hook MachineDataUtility.GetOutputItem (for Slime Egg-Press 2x multi-yield)
                var machineOutput = AccessTools.Method(
                    typeof(MachineDataUtility),
                    nameof(MachineDataUtility.GetOutputItem),
                    new[]
                    {
                        typeof(StardewValley.Object),
                        typeof(MachineItemOutput),
                        typeof(Item),
                        typeof(Farmer),
                        typeof(bool),
                        typeof(int?).MakeByRefType()
                    }
                );
                if (machineOutput != null)
                {
                    harmony.Patch(
                        original: machineOutput,
                        postfix: new HarmonyMethod(typeof(FarmAnimalPatches), nameof(GetOutputItem_Postfix))
                    );
                    Monitor.Log("Hooked MachineDataUtility.GetOutputItem for Slime Egg-Press successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply FarmAnimalPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Postfix on FarmAnimal.dayUpdate: evaluates produce and grants multi-drops for
        /// ducks, rabbits, goats, dinosaurs, void chickens, and daily wool for happy sheep.
        /// </summary>
        public static void DayUpdate_Postfix(FarmAnimal __instance, GameLocation environtment)
        {
            if (__instance == null)
                return;

            try
            {
                string animalType = __instance.type?.Value ?? string.Empty;
                int hearts = __instance.friendshipTowardFarmer.Value / 200;
                string currentProduce = __instance.currentProduce?.Value ?? string.Empty;

                // 1. Duck Dual Drop: When a high-friendship duck rolls a Feather, grant the Duck Egg as well
                if (Config.EnableDuckDualDrop && animalType.Contains("Duck", StringComparison.OrdinalIgnoreCase))
                {
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
                    if (!string.IsNullOrEmpty(currentProduce) && hearts >= 3)
                    {
                        if (Game1.random.NextDouble() <= Config.RabbitMultiDropChance)
                        {
                            string bonusItemId = (Game1.random.NextDouble() < (0.15 + (hearts * 0.05))) ? "446" : "440";
                            int quality = __instance.produceQuality?.Value ?? 0;
                            SpawnProduce(__instance, bonusItemId, quality);
                            Monitor.Log($"BetterAnimal: Rabbit '{__instance.Name}' produced bonus item ID '{bonusItemId}'.", LogLevel.Trace);
                        }
                    }
                }

                // 3. Goat Multi-Milk: High-friendship goats have a chance to produce bonus goat milk
                if (Config.EnableGoatMultiDrop && animalType.Contains("Goat", StringComparison.OrdinalIgnoreCase))
                {
                    bool isGoatMilk = currentProduce is "436" or "(O)436" or "438" or "(O)438" || currentProduce.Contains("GoatMilk", StringComparison.OrdinalIgnoreCase);
                    if (isGoatMilk && hearts >= Config.GoatMultiDropMinHearts)
                    {
                        if (Game1.random.NextDouble() <= Config.GoatMultiDropChance)
                        {
                            int quality = __instance.produceQuality?.Value ?? 0;
                            SpawnProduce(__instance, currentProduce, quality);
                            Monitor.Log($"BetterAnimal: Goat '{__instance.Name}' produced bonus Goat Milk.", LogLevel.Trace);
                        }
                    }
                }

                // 4. Dinosaur Multi-Egg: High-friendship dinosaurs have a chance to lay a bonus second egg
                if (Config.EnableDinosaurMultiDrop && animalType.Contains("Dino", StringComparison.OrdinalIgnoreCase))
                {
                    bool isDinoEgg = currentProduce is "107" or "(O)107" || currentProduce.Contains("DinosaurEgg", StringComparison.OrdinalIgnoreCase);
                    if (isDinoEgg && hearts >= Config.DinosaurMultiDropMinHearts)
                    {
                        if (Game1.random.NextDouble() <= Config.DinosaurMultiDropChance)
                        {
                            int quality = __instance.produceQuality?.Value ?? 0;
                            SpawnProduce(__instance, "107", quality);
                            Monitor.Log($"BetterAnimal: Dinosaur '{__instance.Name}' laid a bonus Dinosaur Egg.", LogLevel.Trace);
                        }
                    }
                }

                // 5. Void Chicken Multi-Egg: High-friendship void chickens have a chance to lay a bonus second void egg
                if (Config.EnableVoidChickenMultiDrop && animalType.Contains("Void", StringComparison.OrdinalIgnoreCase))
                {
                    bool isVoidEgg = currentProduce is "305" or "(O)305" || currentProduce.Contains("VoidEgg", StringComparison.OrdinalIgnoreCase);
                    if (isVoidEgg && hearts >= Config.VoidChickenMultiDropMinHearts)
                    {
                        if (Game1.random.NextDouble() <= Config.VoidChickenMultiDropChance)
                        {
                            int quality = __instance.produceQuality?.Value ?? 0;
                            SpawnProduce(__instance, "305", quality);
                            Monitor.Log($"BetterAnimal: Void Chicken '{__instance.Name}' laid a bonus Void Egg.", LogLevel.Trace);
                        }
                    }
                }

                // 6. Sheep Daily Shearing: Ready to shear daily at 5 hearts
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
        /// Postfix on SlimeHutch.DayUpdate: enhances daily slime ball spawn capacity up to SlimeHutchMaxBalls.
        /// </summary>
        public static void SlimeHutch_DayUpdate_Postfix(SlimeHutch __instance)
        {
            if (!Config.EnableSlimeRanchingBalancing || __instance == null)
                return;

            try
            {
                // Count current slime balls inside the hutch
                int currentBalls = 0;
                foreach (var obj in __instance.Objects.Values)
                {
                    if (obj.ItemId == "56" || obj.QualifiedItemId == "(BC)56")
                        currentBalls++;
                }

                int targetBalls = Config.SlimeHutchMaxBalls;
                if (currentBalls >= targetBalls || __instance.characters.Count < 10)
                    return;

                int extraNeeded = targetBalls - currentBalls;
                int spawned = 0;

                for (int attempts = 0; attempts < 50 && spawned < extraNeeded; attempts++)
                {
                    int x = Game1.random.Next(2, 16);
                    int y = Game1.random.Next(4, 11);
                    Vector2 tile = new(x, y);

                    if (__instance.isTileOnMap(tile) && !__instance.Objects.ContainsKey(tile) && __instance.CanItemBePlacedHere(tile))
                    {
                        var slimeBall = new StardewValley.Object(tile, "56");
                        __instance.Objects.Add(tile, slimeBall);
                        spawned++;
                    }
                }

                if (spawned > 0)
                {
                    Monitor.Log($"BetterAnimal: Slime Hutch spawned {spawned} bonus Slime Balls (Total: {currentBalls + spawned}/{targetBalls}).", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in SlimeHutch_DayUpdate_Postfix: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Postfix on Object.performRemoveAction: drops bonus raw slimes when popping a Slime Ball.
        /// </summary>
        public static void PerformRemoveAction_Postfix(StardewValley.Object __instance, GameLocation location)
        {
            if (!Config.EnableSlimeRanchingBalancing || __instance == null || location == null)
                return;

            try
            {
                if (__instance.ItemId == "56" || __instance.QualifiedItemId == "(BC)56")
                {
                    // Spawn an extra 10 raw slimes (yielding ~20-30 total per ball)
                    Game1.createMultipleObjectDebris("(O)766", (int)__instance.TileLocation.X, (int)__instance.TileLocation.Y, 10, location);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in PerformRemoveAction_Postfix: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Postfix on MachineDataUtility.GetOutputItem: grants a chance for 2x Slime Eggs in the Slime Egg-Press.
        /// </summary>
        public static void GetOutputItem_Postfix(
            StardewValley.Object machine,
            MachineItemOutput outputData,
            Item inputItem,
            Farmer who,
            bool probe,
            ref int? overrideMinutesUntilReady,
            ref Item? __result)
        {
            if (probe || !Config.EnableSlimeEggPressMultiYield || machine == null || __result == null)
                return;

            try
            {
                if (machine.ItemId == "158" || machine.QualifiedItemId == "(BC)158" || machine.Name.Contains("Egg-Press", StringComparison.OrdinalIgnoreCase))
                {
                    if (Game1.random.NextDouble() <= Config.SlimeEggPressDoubleChance)
                    {
                        __result.Stack = Math.Min(__result.Stack * 2, 999);
                        Monitor.Log("BetterAnimal: Slime Egg-Press produced 2x Slime Eggs.", LogLevel.Trace);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in GetOutputItem_Postfix for Slime Egg-Press: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Spawns a secondary animal product in the animal's building (depositing into Auto-Grabber if present, or on the floor).
        /// </summary>
        private static void SpawnProduce(FarmAnimal animal, string itemId, int quality)
        {
            if (animal == null)
                return;

            string cleanId = itemId.StartsWith("(O)") ? itemId.Substring(3) : itemId;
            var obj = new StardewValley.Object(cleanId, 1)
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
