using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;

namespace BetterIndustry
{
    /// <summary>
    /// Harmony patches that apply the balanced Option 2 Quarter-Step quality matrix
    /// (75/25 & 50/25) to artisan machines and multi-harvest yields to Tree Tappers.
    /// </summary>
    public static class MachineQualityPatches
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Applies the Harmony patches on MachineDataUtility.GetOutputItem and Object.checkForAction.
        /// </summary>
        /// <param name="harmony">Harmony instance.</param>
        public static void Apply(Harmony harmony)
        {
            try
            {
                // 1. Hook MachineDataUtility.GetOutputItem for Option 2 quality matrix
                var method = AccessTools.Method(
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

                if (method != null)
                {
                    harmony.Patch(
                        original: method,
                        postfix: new HarmonyMethod(typeof(MachineQualityPatches), nameof(GetOutputItem_Postfix))
                    );
                    Monitor.Log("Hooked MachineDataUtility.GetOutputItem with Option 2 quality matrix successfully.", LogLevel.Trace);
                }
                else
                {
                    Monitor.Log("Could not locate MachineDataUtility.GetOutputItem for machine quality balancing.", LogLevel.Warn);
                }

                // 2. Hook Object.checkForAction for Tree Tapper multi-harvest yields
                var checkAction = AccessTools.Method(
                    typeof(StardewValley.Object),
                    nameof(StardewValley.Object.checkForAction),
                    new[] { typeof(Farmer), typeof(bool) }
                );

                if (checkAction != null)
                {
                    harmony.Patch(
                        original: checkAction,
                        prefix: new HarmonyMethod(typeof(MachineQualityPatches), nameof(CheckForAction_Prefix))
                    );
                    Monitor.Log("Hooked Object.checkForAction for Tapper multi-harvest yields successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply MachineQualityPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Prefix on Object.checkForAction: rolls multi-harvest yield stacks on ready Tree Tappers.
        /// </summary>
        public static void CheckForAction_Prefix(StardewValley.Object __instance, Farmer who, bool justCheckingForActivity)
        {
            if (justCheckingForActivity || !Config.EnableTapperMultiYield || __instance == null || who == null)
                return;

            try
            {
                if (__instance.readyForHarvest.Value && __instance.heldObject.Value != null)
                {
                    // Standard Tapper: 35% chance for 2x yield
                    if (__instance.ItemId == "105" || __instance.QualifiedItemId == "(BC)105" || __instance.Name.Contains("Tapper", StringComparison.OrdinalIgnoreCase))
                    {
                        if (__instance.ItemId != "264" && __instance.QualifiedItemId != "(BC)264" && !__instance.Name.Contains("Heavy", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Game1.random.NextDouble() <= Config.StandardTapperDoubleChance)
                            {
                                __instance.heldObject.Value.Stack = 2;
                            }
                        }
                    }

                    // Heavy Tapper: 100% 2x yield, 20% chance for 3x yield
                    if (__instance.ItemId == "264" || __instance.QualifiedItemId == "(BC)264" || __instance.Name.Contains("Heavy Tapper", StringComparison.OrdinalIgnoreCase))
                    {
                        __instance.heldObject.Value.Stack = Game1.random.NextDouble() <= Config.HeavyTapperTripleChance ? 3 : 2;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in Tapper CheckForAction_Prefix: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Harmony Postfix on MachineDataUtility.GetOutputItem: rolls the output quality using the
        /// Option 2 Quarter-Step probability matrix based on the input item's quality.
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
            // Do not alter during UI probes / tooltips, or when the feature is disabled, or if result is missing
            if (probe || !Config.EnableMachineQuality || inputItem == null || __result == null)
                return;

            try
            {
                // Casks manage their own internal aging progression, so do not override cask aging outputs
                if (machine != null && (machine.ItemId == "163" || machine.QualifiedItemId == "(BC)163"))
                    return;

                // Quality-preserving distribution matrix (60/25/15 with 0% Iridium floor):
                // Normal (0⭐)  -> 60% Normal, 25% Silver, 15% Gold
                // Silver (1⭐)  -> 25% Normal, 60% Silver, 15% Gold
                // Gold (2⭐)    -> 15% Normal, 25% Silver, 60% Gold
                // Iridium (4⭐) ->  0% Normal, 25% Silver, 75% Gold (Never Iridium from machines)
                double rateSilver;
                double rateGold;

                switch (inputItem.Quality)
                {
                    case 1: // Silver (1⭐)
                        rateSilver = 60.0;
                        rateGold = 15.0;
                        break;

                    case 2: // Gold (2⭐)
                        rateSilver = 25.0;
                        rateGold = 60.0;
                        break;

                    case 4: // Iridium (4⭐)
                        rateSilver = 25.0;
                        rateGold = 75.0;
                        break;

                    default: // Normal (0⭐)
                        rateSilver = 25.0;
                        rateGold = 15.0;
                        break;
                }

                // Large animal products act like Qi Seasoning: guarantee at least Gold tier floor (100% Gold)
                bool isLargeAnimalProduct = IsLargeAnimalProduct(inputItem);
                if (isLargeAnimalProduct)
                {
                    rateGold = 100.0;
                    rateSilver = 0.0;
                }

                // Roll quality from probability bands (0% Iridium)
                double roll = Game1.random.NextDouble() * 100.0;
                int quality;

                if (roll < rateGold)
                {
                    quality = 2; // Gold
                }
                else if (roll < rateGold + rateSilver)
                {
                    quality = 1; // Silver
                }
                else
                {
                    quality = 0; // Normal
                }

                __result.Quality = quality;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error calculating machine quality in GetOutputItem_Postfix: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Checks if an input item is a large or high-tier animal product that guarantees at least Gold quality.
        /// </summary>
        private static bool IsLargeAnimalProduct(Item item)
        {
            if (item == null) return false;

            string id = item.ItemId;
            string qid = item.QualifiedItemId;

            return id is "186" or "438" or "174" or "182" or "107" or "289" or "442"
                || qid is "(O)186" or "(O)438" or "(O)174" or "(O)182" or "(O)107" or "(O)289" or "(O)442"
                || string.Equals(id, "LargeMilk", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeGoatMilk", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeBrownEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "DinosaurEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "OstrichEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "DuckEgg", StringComparison.OrdinalIgnoreCase);
        }
    }
}
