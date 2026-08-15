using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Extensions;

namespace BetterForge
{
    public static class EnchantmentPatches
    {
        private static ModConfig Config = null!;
        private static IMonitor Monitor = null!;

        public static void Initialize(ModConfig config, IMonitor monitor)
        {
            Config = config;
            Monitor = monitor;
        }

        public static void Apply(Harmony harmony)
        {
            try
            {
                var method = AccessTools.Method(
                    typeof(BaseEnchantment),
                    nameof(BaseEnchantment.GetEnchantmentFromItem),
                    new[] { typeof(Item), typeof(Item) }
                );

                if (method != null)
                {
                    harmony.Patch(
                        original: method,
                        prefix: new HarmonyMethod(typeof(EnchantmentPatches), nameof(BaseEnchantment_GetEnchantmentFromItem_Prefix))
                    );
                    Monitor.Log("Hooked BaseEnchantment.GetEnchantmentFromItem for Uniform Enchantments successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply EnchantmentPatches: {ex}", LogLevel.Error);
            }
        }

        public static bool BaseEnchantment_GetEnchantmentFromItem_Prefix(Item base_item, Item item, ref BaseEnchantment? __result)
        {
            if (!Config.UniformEnchantmentChances)
                return true;

            if (item?.QualifiedItemId != "(O)74" && item?.ItemId != "74")
                return true;

            if (base_item is not Tool tool)
                return true;

            List<BaseEnchantment> available = BaseEnchantment.GetAvailableEnchantmentsForItem(tool);
            if (available == null || available.Count == 0)
                return true;

            // Filter out enchantments already applied on this item to prevent redundant rolls
            List<BaseEnchantment> candidates = new();
            foreach (var candidate in available)
            {
                bool alreadyHas = false;
                foreach (var activeEnch in tool.enchantments)
                {
                    if (activeEnch.GetType() == candidate.GetType())
                    {
                        alreadyHas = true;
                        break;
                    }
                }

                if (!alreadyHas)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                candidates = available;
            }

            // Pick with equal, uniform probability
            Random rng = Config.RandomizeEnchantmentSeed
                ? Game1.random
                : Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.stats.Get("timesEnchanted") * 777.0);

            __result = rng.ChooseFrom(candidates);
            return false;
        }
    }
}
