using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace BetterProduct
{
    public static class MeadPatches
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
                var salePriceMethod = AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.salePrice), new[] { typeof(bool) });
                if (salePriceMethod != null)
                {
                    harmony.Patch(
                        original: salePriceMethod,
                        postfix: new HarmonyMethod(typeof(MeadPatches), nameof(Object_salePrice_Postfix))
                    );
                }

                var performDropInMethod = AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performObjectDropInAction), new[] { typeof(Item), typeof(bool), typeof(Farmer), typeof(bool) });
                if (performDropInMethod != null)
                {
                    harmony.Patch(
                        original: performDropInMethod,
                        postfix: new HarmonyMethod(typeof(MeadPatches), nameof(Object_performObjectDropInAction_Postfix))
                    );
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply MeadPatches: {ex}", LogLevel.Error);
            }
        }

        public static void Object_salePrice_Postfix(StardewValley.Object __instance, bool ignoreProfitMargins, ref int __result)
        {
            if (__instance == null)
                return;

            // Apply artisan preserves bonus
            __result = ArtisanBalancer.CalculatePreservePrice(__instance, __result);

            // Apply Mead price fix
            if (Config.EnableMeadFix && (__instance.ItemId == "459" || __instance.QualifiedItemId == "(O)459"))
            {
                if (!string.IsNullOrEmpty(__instance.preservedParentSheetIndex.Value) && __instance.preservedParentSheetIndex.Value != "-1")
                {
                    if (ItemRegistry.Create(__instance.preservedParentSheetIndex.Value) is StardewValley.Object flowerObj)
                    {
                        // Flower Honey in vanilla is worth (100 + 2 * flower.Price)
                        int honeyBasePrice = (flowerObj.ItemId == "340" || flowerObj.QualifiedItemId == "(O)340")
                            ? flowerObj.Price
                            : 100 + flowerObj.Price * 2;

                        int meadPrice = (int)Math.Round(honeyBasePrice * Config.MeadMultiplier);
                        __result = Math.Max(__result, meadPrice);
                    }
                }
            }
        }

        public static void Object_performObjectDropInAction_Postfix(StardewValley.Object __instance, Item dropInItem, bool probe, Farmer who, ref bool __result)
        {
            if (!__result || probe || dropInItem == null || __instance == null)
                return;

            // If input was Honey into a Keg producing Mead (459), tag preservedParentSheetIndex
            if (Config.EnableMeadFix && __instance.heldObject.Value != null && __instance.heldObject.Value.ItemId == "459")
            {
                if (dropInItem is StardewValley.Object honeyObj && (honeyObj.ItemId == "340" || honeyObj.QualifiedItemId == "(O)340"))
                {
                    // Tag the flower type or honey parent item
                    if (!string.IsNullOrEmpty(honeyObj.preservedParentSheetIndex.Value))
                    {
                        __instance.heldObject.Value.preservedParentSheetIndex.Value = honeyObj.preservedParentSheetIndex.Value;
                    }
                }
            }
        }
    }
}