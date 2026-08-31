using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace BetterFishing
{
    /// <summary>
    /// Harmony patches on <see cref="CrabPot"/> to reduce trash rates and provide tiered harvest experience (EXP).
    /// </summary>
    public static class CrabPotPatches
    {
        private static readonly HashSet<string> TrashItemIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "168", // Trash
            "169", // Driftwood
            "170", // Broken Glasses
            "171", // Broken CD
            "172"  // Soggy Newspaper
        };

        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Applies the Harmony patches to CrabPot.DayUpdate and CrabPot.checkForAction.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                // Patch DayUpdate to reduce trash rates and preserve Deluxe Bait bonus
                harmony.Patch(
                    original: AccessTools.Method(typeof(CrabPot), nameof(CrabPot.DayUpdate)),
                    prefix: new HarmonyMethod(typeof(CrabPotPatches), nameof(DayUpdate_Prefix)),
                    postfix: new HarmonyMethod(typeof(CrabPotPatches), nameof(DayUpdate_Postfix))
                );

                // Patch checkForAction to provide tiered harvesting experience
                harmony.Patch(
                    original: AccessTools.Method(typeof(CrabPot), nameof(CrabPot.checkForAction)),
                    prefix: new HarmonyMethod(typeof(CrabPotPatches), nameof(CheckForAction_Prefix)),
                    postfix: new HarmonyMethod(typeof(CrabPotPatches), nameof(CheckForAction_Postfix))
                );

                Monitor.Log("Hooked CrabPot.DayUpdate and CrabPot.checkForAction successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to patch CrabPot methods: {ex}", LogLevel.Error);
            }
        }

        public static void DayUpdate_Prefix(CrabPot __instance, out bool __state)
        {
            __state = false;
            if (__instance?.bait?.Value != null)
            {
                string baitId = __instance.bait.Value.QualifiedItemId ?? __instance.bait.Value.ItemId;
                __state = baitId is "(O)DeluxeBait" or "DeluxeBait" or "774" or "(O)774";
            }
        }

        /// <summary>
        /// Postfix on CrabPot.DayUpdate: intercepts trash generation and applies the trash reroll chance,
        /// converting trash catches into valid local fish and shellfish.
        /// </summary>
        public static void DayUpdate_Postfix(CrabPot __instance, bool __state)
        {
            try
            {
                if (!Config.EnableCrabPotTrashReduction)
                    return;

                var heldItem = __instance.heldObject.Value;
                if (heldItem == null)
                    return;

                string cleanId = heldItem.ItemId.StartsWith("(O)") ? heldItem.ItemId[3..] : heldItem.ItemId;
                if (!TrashItemIds.Contains(cleanId) && heldItem.Category != StardewValley.Object.junkCategory)
                    return;

                // Deterministic tile random for consistency with SDV DayUpdate
                Random r = Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.stats.DaysPlayed, (double)__instance.TileLocation.X * 77.0, (double)__instance.TileLocation.Y * 777.0);

                if (r.NextDouble() >= Config.CrabPotTrashRerollChance)
                    return;

                // Determine whether tile is ocean water or freshwater
                bool isOcean = IsOceanTile(__instance.Location, (int)__instance.TileLocation.X, (int)__instance.TileLocation.Y);
                bool hasDeluxeBait = __state;

                string chosenFishId = isOcean ? GetRandomOceanCatch(r, hasDeluxeBait) : GetRandomFreshwaterCatch(r);
                __instance.heldObject.Value = ItemRegistry.Create<StardewValley.Object>($"(O){chosenFishId}", 1);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in CrabPot.DayUpdate_Postfix: {ex}", LogLevel.Error);
            }
        }

        private static bool IsOceanTile(GameLocation? location, int x, int y)
        {
            if (location == null)
                return false;

            // Use vanilla API to correctly handle all locations including Beach Farm
            return location.catchOceanCrabPotFishFromThisSpot(x, y);
        }

        private static string GetRandomOceanCatch(Random r, bool deluxeBait)
        {
            // Ocean weighted pool: Lobster (3, 6 with Deluxe), Crab (6), Shrimp (5), Oyster (3), Mussel (14), Cockle (17), Clam (12)
            int lobsterWeight = deluxeBait ? 6 : 3;
            var candidates = new (string id, int weight)[]
            {
                ("715", lobsterWeight), // Lobster
                ("717", 6),             // Crab
                ("720", 5),             // Shrimp
                ("723", 3),             // Oyster
                ("719", 14),            // Mussel
                ("718", 17),            // Cockle
                ("372", 12)             // Clam
            };

            int totalWeight = candidates.Sum(c => c.weight);
            int roll = r.Next(totalWeight);
            int cumulative = 0;

            foreach (var (id, weight) in candidates)
            {
                cumulative += weight;
                if (roll < cumulative)
                    return id;
            }

            return "719"; // Mussel fallback
        }

        private static string GetRandomFreshwaterCatch(Random r)
        {
            // Freshwater weighted pool: Crayfish (28), Snail (20), Periwinkle (21)
            var candidates = new (string id, int weight)[]
            {
                ("716", 28), // Crayfish
                ("721", 20), // Snail
                ("722", 21)  // Periwinkle
            };

            int totalWeight = candidates.Sum(c => c.weight);
            int roll = r.Next(totalWeight);
            int cumulative = 0;

            foreach (var (id, weight) in candidates)
            {
                cumulative += weight;
                if (roll < cumulative)
                    return id;
            }

            return "722"; // Periwinkle fallback
        }

        public static void CheckForAction_Prefix(CrabPot __instance, bool justCheckingForActivity, out string? __state)
        {
            __state = null;

            if (!Config.EnableCrabPotExpBalancing || justCheckingForActivity)
                return;

            if (__instance.heldObject.Value != null)
            {
                __state = __instance.heldObject.Value.ItemId;
            }
        }

        public static void CheckForAction_Postfix(CrabPot __instance, Farmer who, bool __result, string? __state)
        {
            try
            {
                if (!__result || __state == null || who == null)
                    return;

                string cleanId = __state.StartsWith("(O)") ? __state[3..] : __state;
                int bonusExp = GetBonusExpForCatch(cleanId);

                if (bonusExp > 0)
                {
                    who.gainExperience(Farmer.fishingSkill, bonusExp);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in CrabPot.CheckForAction_Postfix: {ex}", LogLevel.Error);
            }
        }

        private static int GetBonusExpForCatch(string itemId)
        {
            // Vanilla already awarded flat 5 EXP. We award the difference:
            return itemId switch
            {
                "715" => Math.Max(0, Config.LobsterExp - 5), // Lobster
                "717" => Math.Max(0, Config.CrabExp - 5),    // Crab
                "716" or "720" or "721" or "723" => Math.Max(0, Config.Tier2CrabPotExp - 5), // Crayfish, Shrimp, Snail, Oyster
                "718" or "719" or "722" or "372" => Math.Max(0, Config.Tier1CrabPotExp - 5), // Cockle, Mussel, Periwinkle, Clam
                _ => 0
            };
        }
    }
}
