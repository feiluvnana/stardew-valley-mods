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
                            if (__instance.heldObject.Value.Stack == 1 && Game1.random.NextDouble() <= Config.StandardTapperDoubleChance)
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

                // Restrict quality preservation strictly to Artisan Goods (Category -26) from eligible artisan machines
                if (!IsEligibleArtisanProduct(__result, machine))
                    return;

                // Model A: Strict Quality Step-Down distribution matrix (0% Iridium from machines):
                // Normal (0⭐)  -> 100% Normal, 0% Silver, 0% Gold (No free upgrades)
                // Silver (1⭐)  ->  70% Normal, 30% Silver, 0% Gold
                // Gold (2⭐)    ->  40% Normal, 45% Silver, 15% Gold
                // Iridium (4⭐) ->  20% Normal, 50% Silver, 30% Gold
                double rateSilver;
                double rateGold;

                switch (inputItem.Quality)
                {
                    case 1: // Silver (1⭐)
                        rateSilver = 30.0;
                        rateGold = 0.0;
                        break;

                    case 2: // Gold (2⭐)
                        rateSilver = 45.0;
                        rateGold = 15.0;
                        break;

                    case 4: // Iridium (4⭐)
                        rateSilver = 50.0;
                        rateGold = 30.0;
                        break;

                    default: // Normal (0⭐)
                        rateSilver = 0.0;
                        rateGold = 0.0;
                        break;
                }

                // Large animal products (Large Milk, Large Egg) preserve vanilla guaranteed Gold tier output
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
        /// Checks if an input item is a vanilla Large animal product (Large Milk, Large Egg) that guarantees Gold quality.
        /// </summary>
        private static bool IsLargeAnimalProduct(Item item)
        {
            if (item == null) return false;

            string id = item.ItemId;
            string qid = item.QualifiedItemId;

            return id is "186" or "438" or "174" or "182"
                || qid is "(O)186" or "(O)438" or "(O)174" or "(O)182"
                || string.Equals(id, "LargeMilk", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeGoatMilk", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeBrownEgg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether an output item and machine represent a valid Artisan Good processing pipeline.
        /// Excludes non-artisan machines (furnaces, kilns, seed makers, recyclers, bone mills, geode crushers, etc.)
        /// and non-artisan item categories (metal bars, seeds, coal, fertilizer, minerals, ores, batteries, bait, etc.).
        /// </summary>
        private static bool IsEligibleArtisanProduct(Item result, StardewValley.Object machine)
        {
            if (result == null || machine == null)
                return false;

            string mId = machine.ItemId;
            string mQid = machine.QualifiedItemId;
            string mName = machine.Name ?? string.Empty;

            // 1. Explicitly exclude non-artisan machines
            if (mId is "13" or "182" or "25" or "20" or "90" or "114" or "9" or "231" or "211" or "154" or "156" or "158" or "21" or "246" or "105" or "264"
                || mQid is "(BC)13" or "(BC)182" or "(BC)25" or "(BC)20" or "(BC)90" or "(BC)114" or "(BC)9" or "(BC)231" or "(BC)211" or "(BC)154" or "(BC)156" or "(BC)158" or "(BC)21" or "(BC)246" or "(BC)105" or "(BC)264"
                || string.Equals(mId, "HeavyFurnace", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mQid, "(BC)HeavyFurnace", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mId, "BaitMaker", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mQid, "(BC)BaitMaker", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mId, "DeluxeWormBin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mQid, "(BC)DeluxeWormBin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mId, "MushroomLog", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mQid, "(BC)MushroomLog", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mId, "MiniForge", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mQid, "(BC)MiniForge", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Furnace", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Seed Maker", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Recycling", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Kiln", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Bone Mill", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Geode", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Solar Panel", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Lightning Rod", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Wood Chipper", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Bait", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Worm", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Slime", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Crystalarium", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Coffee Maker", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Statue", StringComparison.OrdinalIgnoreCase)
                || mName.Contains("Mushroom Log", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // 2. Output item must belong to Artisan Goods category (-26)
            if (result.Category == StardewValley.Object.artisanGoodsCategory)
                return true;

            // 3. Optional: Cooking Oil if configured as artisan good
            if (Config.EnableCookingOilArtisanCategory && (result.ItemId == "247" || result.QualifiedItemId == "(O)247"))
                return true;

            return false;
        }
    }
}
