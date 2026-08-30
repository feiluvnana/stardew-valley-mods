// =====================================================================================
// CookingPatches.cs - implements the Food Quality and Star Level system.
//
// In vanilla Stardew Valley, cooking always produces normal (0-star) dishes, regardless
// of whether top-tier Iridium ingredients were used, and Qi Seasoning only upgrades meals
// to Gold.
//
// This file uses Harmony to:
// 1. Intercept CraftingPage.clickCraftingRecipe when cooking to calculate the dish's
//    quality (Silver, Gold, Iridium) from consumed ingredients and enhanced Qi Seasoning.
// 2. Select ingredients intelligently based on the configured priority (HighestQuality,
//    LowestQuality, or InventoryOrder).
// 3. Amplify stat buffs (+2) and duration (2.0x) for Iridium-quality culinary dishes.
// =====================================================================================
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Quests;

namespace BetterIndustry
{
    /// <summary>
    /// Harmony patches managing food quality calculation, ingredient consumption priority,
    /// Qi Seasoning synergy, and high-quality meal stat buff enhancements.
    /// </summary>
    public static class CookingPatches
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Registers all cooking-related Harmony patches.
        /// </summary>
        /// <param name="harmony">The mod's Harmony instance.</param>
        public static void Apply(Harmony harmony)
        {
            try
            {
                // 1. CraftingPage.clickCraftingRecipe (Prefix)
                harmony.Patch(
                    original: AccessTools.Method(typeof(CraftingPage), "clickCraftingRecipe"),
                    prefix: new HarmonyMethod(typeof(CookingPatches), nameof(ClickCraftingRecipe_Prefix))
                );

                // 2. Object.ModifyItemBuffs (Postfix)
                harmony.Patch(
                    original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.ModifyItemBuffs), new[] { typeof(BuffEffects) }),
                    postfix: new HarmonyMethod(typeof(CookingPatches), nameof(ModifyItemBuffs_Postfix))
                );

                // 3. Object.GetFoodOrDrinkBuffs (Postfix)
                harmony.Patch(
                    original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.GetFoodOrDrinkBuffs)),
                    postfix: new HarmonyMethod(typeof(CookingPatches), nameof(GetFoodOrDrinkBuffs_Postfix))
                );

                Monitor.Log("CookingPatches applied successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply CookingPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Helper representing a candidate ingredient item in inventory or container.
        /// </summary>
        private class CandidateItem
        {
            public Item Item { get; }
            public int ContainerIndex { get; } // -1 for player inventory, >= 0 for materialContainers
            public int SlotIndex { get; }
            public int Quality => Item.Quality;
            public int OriginalOrder { get; }

            public CandidateItem(Item item, int containerIndex, int slotIndex, int originalOrder)
            {
                Item = item;
                ContainerIndex = containerIndex;
                SlotIndex = slotIndex;
                OriginalOrder = originalOrder;
            }
        }

        /// <summary>
        /// Helper representing a planned item consumption.
        /// </summary>
        private class ConsumptionPlan
        {
            public int ContainerIndex { get; }
            public int SlotIndex { get; }
            public int Count { get; }
            public int Quality { get; }

            public ConsumptionPlan(int containerIndex, int slotIndex, int count, int quality)
            {
                ContainerIndex = containerIndex;
                SlotIndex = slotIndex;
                Count = count;
                Quality = quality;
            }
        }

        /// <summary>
        /// Harmony Prefix on CraftingPage.clickCraftingRecipe: calculates food quality from
        /// ingredients, consumes items according to priority, applies enhanced Qi Seasoning,
        /// and awards achievements/quests.
        /// </summary>
        public static bool ClickCraftingRecipe_Prefix(
            CraftingPage __instance,
            ClickableTextureComponent c,
            bool playSound,
            bool ___cooking,
            List<Dictionary<ClickableTextureComponent, CraftingRecipe>> ___pagesOfCraftingRecipes,
            int ___currentCraftingPage,
            ref Item? ___heldItem,
            List<IInventory>? ____materialContainers)
        {
            // Only intercept cooking when food quality feature is enabled
            if (!___cooking || !Config.EnableFoodQuality)
                return true;

            try
            {
                var recipeDict = ___pagesOfCraftingRecipes[___currentCraftingPage];
                if (!recipeDict.TryGetValue(c, out var recipe) || recipe == null)
                    return true;

                IList<Item>? containerContents = GetContainerContents(____materialContainers);
                if (!recipe.doesFarmerHaveIngredientsInInventory(containerContents))
                    return false;

                // Determine ingredients to consume and calculate rates
                var plans = PlanIngredientConsumption(recipe, ____materialContainers, out var rates);
                if (plans == null)
                    return true; // Fallback to vanilla if planning fails

                double rateNormal = rates.Normal;
                double rateSilver = rates.Silver;
                double rateGold = rates.Gold;
                double rateIridium = rates.Iridium;

                // Check Qi Seasoning (917)
                List<KeyValuePair<string, int>>? seasoningList = null;
                var testList = new List<KeyValuePair<string, int>> { new("917", 1) };
                if (CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(testList, containerContents))
                {
                    seasoningList = testList;
                    // Qi Seasoning turns all Normal and Silver weights directly to Gold (100% Gold floor).
                    rateGold += rateNormal + rateSilver;
                    rateNormal = 0.0;
                    rateSilver = 0.0;
                    rateIridium = 0.0;
                }

                // Roll final quality tier based on cumulative rates
                double roll = Game1.random.NextDouble() * 100.0;
                int calculatedQuality = 0;
                if (roll < rateIridium)
                {
                    calculatedQuality = 4; // Iridium
                }
                else if (roll < rateIridium + rateGold)
                {
                    calculatedQuality = 2; // Gold
                }
                else if (roll < rateIridium + rateGold + rateSilver)
                {
                    calculatedQuality = 1; // Silver
                }
                else
                {
                    calculatedQuality = 0; // Regular
                }

                // Create the crafted item
                Item crafted = recipe.createItem();
                if (crafted is StardewValley.Object craftedObj)
                {
                    craftedObj.Quality = calculatedQuality;
                }
                else if (crafted != null)
                {
                    crafted.Quality = calculatedQuality;
                }

                // Handle heldItem stacking
                if (___heldItem == null)
                {
                    ExecuteConsumption(plans, ____materialContainers);
                    ___heldItem = crafted;
                    if (playSound)
                        Game1.playSound("coin");
                }
                else
                {
                    if (___heldItem.Name == crafted.Name && ___heldItem.getOne().canStackWith(crafted.getOne()) && ___heldItem.Stack + recipe.numberProducedPerCraft - 1 < ___heldItem.maximumStackSize())
                    {
                        ___heldItem.Stack += recipe.numberProducedPerCraft;
                        ExecuteConsumption(plans, ____materialContainers);
                        if (playSound)
                            Game1.playSound("coin");
                    }
                    else
                    {
                        // If items cannot stack (e.g. star quality mismatch), auto-deposit the currently held item into inventory
                        if (Game1.player.addItemToInventoryBool(___heldItem))
                        {
                            ExecuteConsumption(plans, ____materialContainers);
                            ___heldItem = crafted;
                            if (playSound)
                                Game1.playSound("coin");
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                // Consume Qi Seasoning if used
                if (seasoningList != null)
                {
                    if (playSound)
                        Game1.playSound("breathin");
                    CraftingRecipe.ConsumeAdditionalIngredients(seasoningList, ____materialContainers);
                    if (!CraftingRecipe.DoesFarmerHaveAdditionalIngredientsInInventory(seasoningList, containerContents))
                    {
                        Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Seasoning_UsedLast"));
                    }
                }

                // Quests, achievements, and stats
                Game1.player.NotifyQuests((Quest quest) => quest.OnRecipeCrafted(recipe, crafted));
                Game1.player.cookedRecipe(___heldItem?.ItemId ?? crafted.ItemId);
                Game1.stats.checkForCookingAchievements();

                if (Game1.options.gamepadControls && ___heldItem != null && Game1.player.couldInventoryAcceptThisItem(___heldItem))
                {
                    Game1.player.addItemToInventoryBool(___heldItem);
                    ___heldItem = null;
                }

                return false; // Skip vanilla method
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in ClickCraftingRecipe_Prefix: {ex}", LogLevel.Error);
                return true; // Fallback to vanilla
            }
        }

        /// <summary>
        /// Plans which ingredient items to consume from player inventory and fridge containers,
        /// sorting candidates by the configured IngredientQualityPriority.
        /// Accumulates the 4-level weight contribution across all consumed ingredients.
        /// </summary>
        private static List<ConsumptionPlan>? PlanIngredientConsumption(
            CraftingRecipe recipe,
            List<IInventory>? materialContainers,
            out (double Normal, double Silver, double Gold, double Iridium) rates)
        {
            rates = (60.0, 25.0, 15.0, 0.0);
            var plans = new List<ConsumptionPlan>();
            double totalWeightNormal = 0.0;
            double totalWeightSilver = 0.0;
            double totalWeightGold = 0.0;
            double totalWeightIridium = 0.0;
            double totalWeightSum = 0.0;

            // Gather all available items from player inventory and material containers
            // Snapshot current available stack counts to avoid over-allocating
            var playerSlots = new List<CandidateItem>();
            int orderCounter = 0;

            // Player inventory (scanned in vanilla reverse order)
            for (int i = Game1.player.Items.Count - 1; i >= 0; i--)
            {
                Item item = Game1.player.Items[i];
                if (item != null)
                {
                    playerSlots.Add(new CandidateItem(item, -1, i, orderCounter++));
                }
            }

            // Fridge containers (scanned in vanilla order)
            var containerSlots = new List<CandidateItem>();
            if (materialContainers != null)
            {
                for (int c = 0; c < materialContainers.Count; c++)
                {
                    IInventory container = materialContainers[c];
                    if (container == null) continue;

                    for (int i = container.Count - 1; i >= 0; i--)
                    {
                        Item item = container[i];
                        if (item != null)
                        {
                            containerSlots.Add(new CandidateItem(item, c, i, orderCounter++));
                        }
                    }
                }
            }

            // Track remaining stacks in each slot during planning
            var slotRemaining = new Dictionary<(int container, int slot), int>();
            foreach (var slot in playerSlots.Concat(containerSlots))
            {
                slotRemaining[(slot.ContainerIndex, slot.SlotIndex)] = slot.Item.Stack;
            }

            // For each required ingredient in recipeList
            foreach (var (reqId, reqCount) in recipe.recipeList)
            {
                int remainingNeeded = reqCount;

                // Find all slots matching this requirement
                var matchingCandidates = new List<CandidateItem>();
                foreach (var cand in playerSlots.Concat(containerSlots))
                {
                    if (slotRemaining[(cand.ContainerIndex, cand.SlotIndex)] > 0 && CraftingRecipe.ItemMatchesForCrafting(cand.Item, reqId))
                    {
                        matchingCandidates.Add(cand);
                    }
                }

                // Sort candidates based on configured priority
                string priority = Config.IngredientQualityPriority ?? "HighestQuality";
                if (string.Equals(priority, "HighestQuality", StringComparison.OrdinalIgnoreCase))
                {
                    // Highest quality first, then vanilla order
                    matchingCandidates.Sort((a, b) =>
                    {
                        int qComp = b.Quality.CompareTo(a.Quality);
                        if (qComp != 0) return qComp;
                        return a.OriginalOrder.CompareTo(b.OriginalOrder);
                    });
                }
                else if (string.Equals(priority, "LowestQuality", StringComparison.OrdinalIgnoreCase))
                {
                    // Lowest quality first, then vanilla order
                    matchingCandidates.Sort((a, b) =>
                    {
                        int qComp = a.Quality.CompareTo(b.Quality);
                        if (qComp != 0) return qComp;
                        return a.OriginalOrder.CompareTo(b.OriginalOrder);
                    });
                }
                else
                {
                    // InventoryOrder (Vanilla scan order)
                    matchingCandidates.Sort((a, b) => a.OriginalOrder.CompareTo(b.OriginalOrder));
                }

                // Consume from sorted candidates
                foreach (var cand in matchingCandidates)
                {
                    int available = slotRemaining[(cand.ContainerIndex, cand.SlotIndex)];
                    if (available <= 0) continue;

                    int take = Math.Min(remainingNeeded, available);
                    plans.Add(new ConsumptionPlan(cand.ContainerIndex, cand.SlotIndex, take, cand.Quality));
                    slotRemaining[(cand.ContainerIndex, cand.SlotIndex)] -= take;
                    remainingNeeded -= take;

                    var (wNorm, wSil, wGold, wIri) = GetIngredientWeights(cand.Item);
                    totalWeightNormal += wNorm * take;
                    totalWeightSilver += wSil * take;
                    totalWeightGold += wGold * take;
                    totalWeightIridium += wIri * take;
                    totalWeightSum += 100.0 * take;

                    if (remainingNeeded <= 0)
                        break;
                }

                if (remainingNeeded > 0)
                {
                    // Not enough ingredients found
                    return null;
                }
            }

            if (totalWeightSum > 0)
            {
                rates = (
                    (totalWeightNormal / totalWeightSum) * 100.0,
                    (totalWeightSilver / totalWeightSum) * 100.0,
                    (totalWeightGold / totalWeightSum) * 100.0,
                    (totalWeightIridium / totalWeightSum) * 100.0
                );
            }

            return plans;
        }

        /// <summary>
        /// Returns the 4-level weight distribution (Normal, Silver, Gold, Iridium) contributed by an ingredient.
        /// Follows the deterministic 60/25/15 quality matrix with 0% Iridium output.
        /// </summary>
        private static (double Normal, double Silver, double Gold, double Iridium) GetIngredientWeights(Item item)
        {
            if (item == null || IsNonQualityStaple(item) || item.Quality == 0)
            {
                return (60.0, 25.0, 15.0, 0.0);
            }

            return item.Quality switch
            {
                1 => (25.0, 60.0, 15.0, 0.0), // Silver (1⭐)
                2 => (15.0, 25.0, 60.0, 0.0), // Gold (2⭐)
                4 => (0.0, 25.0, 75.0, 0.0),  // Iridium (4⭐) -> Max Gold
                _ => (60.0, 25.0, 15.0, 0.0)
            };
        }

        /// <summary>
        /// Determines whether an item is a non-quality cooking staple (e.g. Flour, Sugar, Oil, Vinegar, Rice)
        /// that cannot naturally have star quality.
        /// </summary>
        private static bool IsNonQualityStaple(Item item)
        {
            if (item == null) return false;

            // If the item actually carries a quality star (e.g. from mods or special goods), treat as quality-eligible.
            if (item.Quality > 0) return false;

            string id = item.ItemId;
            string qid = item.QualifiedItemId;

            return id is "245" or "246" or "247" or "419" or "423"
                || qid is "(O)245" or "(O)246" or "(O)247" or "(O)419" or "(O)423"
                || string.Equals(id, "Sugar", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "WheatFlour", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "Oil", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "Vinegar", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "Rice", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Executes planned ingredient consumption from player inventory and material containers.
        /// </summary>
        private static void ExecuteConsumption(List<ConsumptionPlan> plans, List<IInventory>? materialContainers)
        {
            var dirtyContainers = new HashSet<int>();

            foreach (var plan in plans)
            {
                if (plan.ContainerIndex == -1)
                {
                    // Player inventory
                    if (plan.SlotIndex >= 0 && plan.SlotIndex < Game1.player.Items.Count)
                    {
                        Item item = Game1.player.Items[plan.SlotIndex];
                        if (item != null)
                        {
                            Game1.player.Items[plan.SlotIndex] = item.ConsumeStack(plan.Count);
                        }
                    }
                }
                else if (materialContainers != null && plan.ContainerIndex >= 0 && plan.ContainerIndex < materialContainers.Count)
                {
                    // Material container (fridge)
                    IInventory container = materialContainers[plan.ContainerIndex];
                    if (container != null && plan.SlotIndex >= 0 && plan.SlotIndex < container.Count)
                    {
                        Item item = container[plan.SlotIndex];
                        if (item != null)
                        {
                            container[plan.SlotIndex] = item.ConsumeStack(plan.Count);
                            if (container[plan.SlotIndex] == null)
                            {
                                dirtyContainers.Add(plan.ContainerIndex);
                            }
                        }
                    }
                }
            }

            // Clean up empty slots in modified fridge inventories
            if (materialContainers != null)
            {
                foreach (int cIndex in dirtyContainers)
                {
                    if (cIndex >= 0 && cIndex < materialContainers.Count)
                    {
                        materialContainers[cIndex]?.RemoveEmptySlots();
                    }
                }
            }
        }

        /// <summary>
        /// Harmony Postfix on Object.ModifyItemBuffs: awards +2 stat bonus (instead of +1)
        /// for Iridium-quality cooked meals and drinks.
        /// </summary>
        public static void ModifyItemBuffs_Postfix(StardewValley.Object __instance, BuffEffects effects)
        {
            try
            {
                if (effects == null || !Config.EnableEnhancedFoodBuffs)
                    return;

                // Category -7 is StardewValley.Object.CookingCategory (Cooking)
                if (__instance.Category == -7 && __instance.Quality == 4)
                {
                    // Vanilla already added +1 for Quality != 0.
                    // For Iridium quality, add an additional +1 so total bonus is +2.
                    Netcode.NetFloat[] statFields = new[]
                    {
                        effects.FarmingLevel,
                        effects.FishingLevel,
                        effects.MiningLevel,
                        effects.LuckLevel,
                        effects.ForagingLevel,
                        effects.MaxStamina,
                        effects.MagneticRadius,
                        effects.Defense,
                        effects.Attack
                    };

                    foreach (var stat in statFields)
                    {
                        if (stat.Value != 0f)
                        {
                            stat.Value += 1f;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in ModifyItemBuffs_Postfix: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Harmony Postfix on Object.GetFoodOrDrinkBuffs: scales duration for Iridium-quality
        /// cooked dishes up to the configured multiplier (default 2.0x).
        /// </summary>
        public static void GetFoodOrDrinkBuffs_Postfix(StardewValley.Object __instance, ref IEnumerable<Buff> __result)
        {
            try
            {
                if (!Config.EnableEnhancedFoodBuffs || __instance.Category != -7 || __instance.Quality != 4)
                    return;

                float targetMultiplier = Config.IridiumBuffDurationMultiplier;
                // Vanilla already multiplied by 1.5x for non-zero quality.
                if (Math.Abs(targetMultiplier - 1.5f) < 0.01f)
                    return;

                float scaleRatio = targetMultiplier / 1.5f;
                var adjustedBuffs = new List<Buff>();

                foreach (var buff in __result)
                {
                    if (buff != null && buff.millisecondsDuration > 0)
                    {
                        buff.millisecondsDuration = (int)(buff.millisecondsDuration * scaleRatio);
                        buff.totalMillisecondsDuration = buff.millisecondsDuration;
                    }
                    adjustedBuffs.Add(buff);
                }

                __result = adjustedBuffs;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in GetFoodOrDrinkBuffs_Postfix: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Combines all items across the provided material containers into a single list.
        /// </summary>
        private static IList<Item>? GetContainerContents(List<IInventory>? materialContainers)
        {
            if (materialContainers == null)
                return null;

            var list = new List<Item>();
            foreach (var container in materialContainers)
            {
                if (container != null)
                {
                    list.AddRange(container);
                }
            }
            return list;
        }
    }
}
