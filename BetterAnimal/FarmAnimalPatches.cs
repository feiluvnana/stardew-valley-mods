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
                        prefix: new HarmonyMethod(typeof(FarmAnimalPatches), nameof(DayUpdate_Prefix)),
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
                var removeAction = AccessTools.DeclaredMethod(
                    typeof(StardewValley.Object),
                    nameof(StardewValley.Object.performRemoveAction)
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

        public static void DayUpdate_Prefix(FarmAnimal __instance, out (string produce, int quality, int daysSinceLay, bool wasAdult) __state)
        {
            if (__instance != null)
            {
                __state = (
                    __instance.currentProduce?.Value ?? string.Empty,
                    __instance.produceQuality?.Value ?? 0,
                    __instance.daysSinceLastLay?.Value ?? 0,
                    __instance.isAdult()
                );
            }
            else
            {
                __state = (string.Empty, 0, 0, false);
            }
        }

        /// <summary>
        /// Postfix on FarmAnimal.dayUpdate: evaluates produce and grants multi-drops for
        /// ducks, rabbits, goats, dinosaurs, void chickens, and daily wool for happy sheep.
        /// </summary>
        public static void DayUpdate_Postfix(FarmAnimal __instance, GameLocation environment, (string produce, int quality, int daysSinceLay, bool wasAdult) __state)
        {
            if (__instance == null)
                return;

            try
            {
                string animalType = __instance.type?.Value ?? string.Empty;
                int hearts = __instance.friendshipTowardFarmer.Value / 200;
                bool producedToday = (__instance.daysSinceLastLay.Value == 0 && __state.wasAdult) || !string.IsNullOrEmpty(__instance.currentProduce?.Value);
                int quality = __instance.produceQuality?.Value ?? __state.quality;

                // 1. Duck Dual Drop: When a high-friendship duck lays/drops, grant bonus Duck Egg / Duck Feather
                if (Config.EnableDuckDualDrop && animalType.Contains("Duck", StringComparison.OrdinalIgnoreCase))
                {
                    if (producedToday && hearts >= Config.DuckDualDropMinHearts)
                    {
                        string currentProduce = __instance.currentProduce?.Value ?? __state.produce;
                        bool isFeather = currentProduce is "444" or "(O)444" || currentProduce.Contains("Feather", StringComparison.OrdinalIgnoreCase);

                        if (isFeather)
                        {
                            SpawnProduce(__instance, "442", quality); // Drop bonus Duck Egg alongside feather
                            Monitor.Log($"BetterAnimal: High-friendship duck '{__instance.Name}' dropped Duck Feather + bonus Duck Egg.", LogLevel.Trace);
                        }
                        else
                        {
                            double rollChance = hearts >= 5 ? Config.DuckDualDropChance : (Config.DuckDualDropChance * 0.75);
                            if (Game1.random.NextDouble() < rollChance)
                            {
                                SpawnProduce(__instance, "444", quality); // Drop bonus Duck Feather
                                Monitor.Log($"BetterAnimal: High-friendship duck '{__instance.Name}' produced bonus Duck Feather.", LogLevel.Trace);
                            }
                        }
                    }
                }

                // 2. Rabbit Multi-Drop: High-friendship rabbits have a chance to drop multiple wool or a lucky foot
                if (Config.EnableRabbitMultiDrop && animalType.Contains("Rabbit", StringComparison.OrdinalIgnoreCase))
                {
                    if (producedToday && hearts >= 3)
                    {
                        if (Game1.random.NextDouble() < Config.RabbitMultiDropChance)
                        {
                            string bonusItemId = (Game1.random.NextDouble() < (0.15 + (hearts * 0.05))) ? "446" : "440";
                            SpawnProduce(__instance, bonusItemId, quality);
                            Monitor.Log($"BetterAnimal: Rabbit '{__instance.Name}' produced bonus item ID '{bonusItemId}'.", LogLevel.Trace);
                        }
                    }
                }

                // 3. Goat Multi-Milk: High-friendship goats have a chance to produce bonus goat milk
                if (Config.EnableGoatMultiDrop && animalType.Contains("Goat", StringComparison.OrdinalIgnoreCase))
                {
                    string currentProduce = __instance.currentProduce?.Value ?? __state.produce;
                    bool isGoatMilk = currentProduce is "436" or "(O)436" or "438" or "(O)438" || currentProduce.Contains("GoatMilk", StringComparison.OrdinalIgnoreCase);
                    if (producedToday && isGoatMilk && hearts >= Config.GoatMultiDropMinHearts)
                    {
                        if (Game1.random.NextDouble() < Config.GoatMultiDropChance)
                        {
                            SpawnProduce(__instance, currentProduce, quality);
                            Monitor.Log($"BetterAnimal: Goat '{__instance.Name}' produced bonus Goat Milk.", LogLevel.Trace);
                        }
                    }
                }

                // 4. Dinosaur Multi-Egg: High-friendship dinosaurs have a chance to lay a bonus second egg
                if (Config.EnableDinosaurMultiDrop && animalType.Contains("Dino", StringComparison.OrdinalIgnoreCase))
                {
                    if (producedToday && hearts >= Config.DinosaurMultiDropMinHearts)
                    {
                        if (Game1.random.NextDouble() < Config.DinosaurMultiDropChance)
                        {
                            SpawnProduce(__instance, "107", quality);
                            Monitor.Log($"BetterAnimal: Dinosaur '{__instance.Name}' laid a bonus Dinosaur Egg.", LogLevel.Trace);
                        }
                    }
                }

                // 5. Void Chicken Multi-Egg: High-friendship void chickens have a chance to lay a bonus second void egg
                if (Config.EnableVoidChickenMultiDrop && animalType.Contains("Void", StringComparison.OrdinalIgnoreCase))
                {
                    if (producedToday && hearts >= Config.VoidChickenMultiDropMinHearts)
                    {
                        if (Game1.random.NextDouble() < Config.VoidChickenMultiDropChance)
                        {
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
                        __instance.daysSinceLastLay.Value = 2; // Sheep daysToLay is 3 in vanilla; set to 3-1=2 so next dayUpdate produces wool
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in FarmAnimalPatches DayUpdate_Postfix: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Postfix on SlimeHutch.dayUpdate: enhances daily slime ball spawn capacity up to SlimeHutchMaxBalls.
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
                if (currentBalls >= targetBalls || __instance.characters.Count < 5)
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
                        var slimeBall = ItemRegistry.Create<StardewValley.Object>("(BC)56");
                        if (slimeBall != null)
                        {
                            slimeBall.TileLocation = tile;
                            __instance.Objects.Add(tile, slimeBall);
                            spawned++;
                        }
                    }
                }

                if (spawned > 0)
                {
                    Monitor.Log($"BetterAnimal: Slime Hutch spawned {spawned} bonus Slime Balls (Total: {currentBalls + spawned}/{targetBalls}).", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in SlimeHutch_DayUpdate_Postfix: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Postfix on Object.performRemoveAction: drops bonus raw slimes when popping a Slime Ball.
        /// </summary>
        public static void PerformRemoveAction_Postfix(StardewValley.Object __instance)
        {
            if (!Config.EnableSlimeRanchingBalancing || __instance == null || __instance.Location == null)
                return;

            try
            {
                if (__instance.ItemId == "56" || __instance.QualifiedItemId == "(BC)56")
                {
                    // Spawn an extra 10 raw slimes (yielding ~20-30 total per ball)
                    Game1.createMultipleObjectDebris("(O)766", (int)__instance.TileLocation.X, (int)__instance.TileLocation.Y, 10, __instance.Location);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in PerformRemoveAction_Postfix: {ex}", LogLevel.Error);
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
                    if (Game1.random.NextDouble() < Config.SlimeEggPressDoubleChance)
                    {
                        __result.Stack = Math.Min(__result.Stack * 2, 999);
                        Monitor.Log("BetterAnimal: Slime Egg-Press produced 2x Slime Eggs.", LogLevel.Trace);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in GetOutputItem_Postfix for Slime Egg-Press: {ex}", LogLevel.Error);
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
            obj.IsSpawnedObject = true;

            if (animal.home?.indoors?.Value is AnimalHouse animalHouse)
            {
                // 1. Try depositing into an Auto-Grabber inside the building
                foreach (var placement in animalHouse.Objects.Values)
                {
                    if (placement.QualifiedItemId == "(BC)165" && placement.heldObject.Value is Chest grabber)
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
                    obj.TileLocation = originTile;
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
                            obj.TileLocation = candidate;
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
