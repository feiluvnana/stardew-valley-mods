using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Tools;

namespace BetterQOL
{
    public static class MenuTooltipPatch
    {
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            var postfix = new HarmonyMethod(typeof(MenuTooltipPatch), nameof(DescriptionPostfix));

            // Only patch concrete item classes (never abstract classes like Item or Tool)
            Type[] concreteTypes = new[]
            {
                typeof(StardewValley.Object),
                typeof(Ring),
                typeof(Clothing),
                typeof(Hat),
                typeof(Boots),
                typeof(Furniture),
                typeof(Trinket),
                typeof(MeleeWeapon),
                typeof(Slingshot),
                typeof(Pickaxe),
                typeof(Axe),
                typeof(Hoe),
                typeof(WateringCan),
                typeof(FishingRod),
                typeof(Pan),
                typeof(Shears),
                typeof(MilkPail)
            };

            int patchedCount = 0;
            foreach (var type in concreteTypes)
            {
                try
                {
                    MethodInfo? method = AccessTools.DeclaredMethod(type, "getDescription", Type.EmptyTypes);
                    if (method != null && !method.IsAbstract)
                    {
                        harmony.Patch(method, postfix: postfix);
                        patchedCount++;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Could not patch getDescription on {type.Name}: {ex.Message}", LogLevel.Trace);
                }
            }

            monitor.Log($"Successfully applied Harmony description patches to {patchedCount} item classes for native in-menu tooltips.", LogLevel.Debug);
        }

        public static void DescriptionPostfix(Item? __instance, ref string __result)
        {
            if (__instance == null || Game1.activeClickableMenu == null)
                return;

            string extra = BuildItemExtraText(__instance);
            if (!string.IsNullOrEmpty(extra))
            {
                if (string.IsNullOrEmpty(__result))
                {
                    __result = extra;
                }
                else if (!__result.Contains(extra))
                {
                    __result = __result + "\n\n" + extra;
                }
            }
        }

        public static string BuildItemExtraText(Item item)
        {
            var lines = new List<string>();
            var config = ModEntry.Config;

            // 1. Sell Price
            if (config.ShowItemSellPriceOnHover)
            {
                int singlePrice = item.sellToStorePrice();
                if (singlePrice > 0)
                {
                    if (item.Stack > 1)
                    {
                        lines.Add(ModEntry.I18n.Get("hover.item.sell-price-stack", new { price = singlePrice, total = singlePrice * item.Stack, count = item.Stack }));
                    }
                    else
                    {
                        lines.Add(ModEntry.I18n.Get("hover.item.sell-price", new { price = singlePrice }));
                    }
                }
            }

            // 2. Community Center Bundles
            if (config.ShowBundleNeedOnHover)
            {
                var bundles = GetNeededBundles(item);
                if (bundles.Count > 0)
                {
                    lines.Add(ModEntry.I18n.Get("hover.item.bundle-needed", new { bundles = string.Join(", ", bundles) }));
                }
            }

            // 3. Museum Donation
            if (config.ShowMuseumNeedOnHover)
            {
                bool isMuseumItem = (item is StardewValley.Object obj && (obj.Type == "Arch" || obj.Type == "Minerals"))
                                 || item.Category == StardewValley.Object.mineralsCategory;
                if (isMuseumItem)
                {
                    bool isDonated = Game1.netWorldState.Value.MuseumPieces.Values.Any(v =>
                        v == item.ItemId ||
                        v == item.QualifiedItemId ||
                        (item is StardewValley.Object sObj && v == sObj.ParentSheetIndex.ToString()) ||
                        v == $"(O){item.ItemId}"
                    );

                    if (!isDonated)
                    {
                        lines.Add(ModEntry.I18n.Get("hover.item.museum-needed"));
                    }
                    else
                    {
                        lines.Add(ModEntry.I18n.Get("hover.item.museum-donated"));
                    }
                }
            }

            return string.Join("\n", lines);
        }

        private static List<string> GetNeededBundles(Item item)
        {
            var results = new List<string>();
            try
            {
                if (Game1.player.hasCompletedCommunityCenter() || Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
                    return results;

                var bundleData = DataLoader.Bundles(Game1.content);
                if (bundleData == null || Game1.netWorldState.Value.Bundles == null)
                    return results;

                foreach (var kvp in bundleData)
                {
                    string bundleKey = kvp.Key; // e.g. "Pantry/0"
                    string[] keyParts = bundleKey.Split('/');
                    if (keyParts.Length < 2 || !int.TryParse(keyParts[1], out int bundleId))
                        continue;

                    string bundleValue = kvp.Value;
                    string[] parts = bundleValue.Split('/');
                    if (parts.Length < 3)
                        continue;

                    string bundleName = parts.Length >= 6 && !string.IsNullOrEmpty(parts[5]) ? parts[5] : parts[0];
                    string[] reqParts = parts[2].Split(' ');

                    if (Game1.netWorldState.Value.Bundles.TryGetValue(bundleId, out bool[] ingredientSlots))
                    {
                        int itemsRequired = parts.Length > 4 && int.TryParse(parts[4], out int req) ? req : ingredientSlots.Length;
                        int filledCount = ingredientSlots.Count(b => b);
                        if (filledCount >= itemsRequired)
                            continue; // Bundle already finished

                        for (int k = 0; k < ingredientSlots.Length; k++)
                        {
                            if (!ingredientSlots[k]) // Slot not filled yet
                            {
                                int reqIndex = k * 3;
                                if (reqIndex + 2 >= reqParts.Length)
                                    break;

                                string reqId = reqParts[reqIndex];
                                int reqMinQuality = int.TryParse(reqParts[reqIndex + 2], out int q) ? q : 0;

                                bool idMatch = reqId == item.ItemId ||
                                               reqId == item.QualifiedItemId ||
                                               (item is StardewValley.Object obj && (reqId == obj.ParentSheetIndex.ToString() || reqId == obj.ItemId));
                                bool catMatch = int.TryParse(reqId, out int cat) && cat < 0 && item.Category == cat;
                                bool qualityMatch = item.Quality >= reqMinQuality;

                                if ((idMatch || catMatch) && qualityMatch)
                                {
                                    if (!results.Contains(bundleName))
                                    {
                                        results.Add(bundleName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return results;
        }
    }
}
