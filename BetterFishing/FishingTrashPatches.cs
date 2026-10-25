using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace BetterFishing
{
    /// <summary>
    /// Harmony patches on <see cref="GameLocation.getFish"/>, <see cref="MineShaft.getFish"/>, and <see cref="Farm.getFish"/>
    /// to ensure that when bait is used on a fishing rod, the chance of catching trash is 0%.
    /// </summary>
    public static class FishingTrashPatches
    {
        private static readonly HashSet<string> TrashItemIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "167", // Joja Cola
            "168", // Trash
            "169", // Driftwood
            "170", // Broken Glasses
            "171", // Broken CD
            "172"  // Soggy Newspaper
        };

        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        [ThreadStatic]
        private static bool isRerolling;

        /// <summary>
        /// Applies Harmony postfix patches to GameLocation.getFish, MineShaft.getFish, and Farm.getFish.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                // Patch GameLocation.getFish
                var baseMethod = AccessTools.DeclaredMethod(typeof(GameLocation), nameof(GameLocation.getFish));
                if (baseMethod != null)
                {
                    harmony.Patch(
                        original: baseMethod,
                        postfix: new HarmonyMethod(typeof(FishingTrashPatches), nameof(GetFish_Postfix))
                    );
                }

                // Patch MineShaft.getFish (MineShaft overrides getFish without calling base for floor 100 lava area)
                var mineShaftMethod = AccessTools.DeclaredMethod(typeof(MineShaft), nameof(MineShaft.getFish));
                if (mineShaftMethod != null)
                {
                    harmony.Patch(
                        original: mineShaftMethod,
                        postfix: new HarmonyMethod(typeof(FishingTrashPatches), nameof(GetFish_Postfix))
                    );
                }

                // Patch Farm.getFish (Farm overrides getFish)
                var farmMethod = AccessTools.DeclaredMethod(typeof(Farm), nameof(Farm.getFish));
                if (farmMethod != null)
                {
                    harmony.Patch(
                        original: farmMethod,
                        postfix: new HarmonyMethod(typeof(FishingTrashPatches), nameof(GetFish_Postfix))
                    );
                }

                Monitor.Log("Hooked GameLocation.getFish, MineShaft.getFish, and Farm.getFish with bait-prevents-trash postfix.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to patch getFish methods: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Checks whether a caught item represents trash or junk.
        /// </summary>
        public static bool IsTrash(Item? item)
        {
            if (item == null)
                return true;

            string cleanId = item.ItemId.StartsWith("(O)") ? item.ItemId[3..] : item.ItemId;
            if (TrashItemIds.Contains(cleanId) || item.Category == StardewValley.Object.junkCategory)
                return true;

            if (ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId)?.IsErrorItem == true)
                return true;

            return false;
        }

        /// <summary>
        /// Harmony Postfix on getFish methods: intercepts trash catches when bait is attached to the rod,
        /// rerolling or selecting a valid fish to ensure a 0% trash rate.
        /// </summary>
        public static void GetFish_Postfix(
            GameLocation __instance,
            float millisecondsAfterNibble,
            string bait,
            int waterDepth,
            Farmer who,
            double baitPotency,
            Vector2 bobberTile,
            string locationName,
            ref Item __result)
        {
            try
            {
                if (isRerolling)
                    return;

                if (Config == null || !Config.EnableBaitPreventsTrash)
                    return;

                // Check if bait is attached to the rod
                bool hasBait = !string.IsNullOrEmpty(bait) || ((who?.CurrentTool as FishingRod)?.GetBait() != null);
                if (!hasBait)
                    return;

                // If not trash, nothing to do
                if (!IsTrash(__result))
                    return;

                // Replace trash with a valid catch
                __result = RerollOrGetFish(__instance, millisecondsAfterNibble, bait, waterDepth, who, baitPotency, bobberTile, locationName);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in FishingTrashPatches.GetFish_Postfix: {ex}", LogLevel.Error);
            }
        }

        private static Item RerollOrGetFish(
            GameLocation location,
            float millisecondsAfterNibble,
            string bait,
            int waterDepth,
            Farmer who,
            double baitPotency,
            Vector2 bobberTile,
            string locationName)
        {
            try
            {
                isRerolling = true;

                // 1. Retry native location getFish in a loop
                int maxAttempts = location is MineShaft ? 200 : 100;
                for (int i = 0; i < maxAttempts; i++)
                {
                    Item candidate = location.getFish(millisecondsAfterNibble, bait, waterDepth, who, baitPotency, bobberTile, locationName);
                    if (!IsTrash(candidate))
                    {
                        return candidate;
                    }
                }

                // 2. If MineShaft didn't hit after maxAttempts, return the mine's signature fish
                if (location is MineShaft mine)
                {
                    int area = mine.getMineArea();
                    string mineFishId = area switch
                    {
                        80 => "(O)162",      // Lava Eel (Floor 100)
                        40 => "(O)161",      // Ice Pip (Floor 60)
                        10 or 0 => "(O)158", // Stonefish (Floor 20)
                        _ => "(O)156"        // Ghostfish
                    };
                    return ItemRegistry.Create(mineFishId, 1, 0, false);
                }

                // 3. Fallback for Farm ponds (standard farm layout has 0 fish in data) or locations with no active fish data
                bool isOcean = location.catchOceanCrabPotFishFromThisSpot((int)bobberTile.X, (int)bobberTile.Y)
                               || (location is Farm && Game1.whichFarm == Farm.beach_layout);

                if (isOcean)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Item candidate = GameLocation.GetFishFromLocationData("Beach", bobberTile, waterDepth, who, false, false, location);
                        if (!IsTrash(candidate))
                            return candidate;
                    }

                    Season season = Game1.GetSeasonForLocation(location);
                    string fallbackOceanFish = season switch
                    {
                        Season.Spring => "(O)147", // Herring
                        Season.Summer => "(O)130", // Tuna
                        Season.Fall => "(O)131",   // Sardine
                        Season.Winter => "(O)151", // Squid
                        _ => "(O)147"
                    };
                    return ItemRegistry.Create(fallbackOceanFish, 1, 0, false);
                }
                else
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Item candidate = GameLocation.GetFishFromLocationData("Forest", bobberTile, waterDepth, who, false, false, location);
                        if (!IsTrash(candidate))
                            return candidate;
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        Item candidate = GameLocation.GetFishFromLocationData("Town", bobberTile, waterDepth, who, false, false, location);
                        if (!IsTrash(candidate))
                            return candidate;
                    }

                    Season season = Game1.GetSeasonForLocation(location);
                    string fallbackFreshwaterFish = season switch
                    {
                        Season.Spring => "(O)145", // Sunfish
                        Season.Summer => "(O)138", // Rainbow Trout
                        Season.Fall => "(O)137",   // Smallmouth Bass
                        Season.Winter => "(O)702", // Chub
                        _ => "(O)142"              // Carp
                    };
                    return ItemRegistry.Create(fallbackFreshwaterFish, 1, 0, false);
                }
            }
            finally
            {
                isRerolling = false;
            }
        }
    }
}
