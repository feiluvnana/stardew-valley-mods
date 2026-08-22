using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace BetterQOL
{
    public static class MenuTooltipPatch
    {
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            try
            {
                MethodInfo? drawToolTipMethod = AccessTools.Method(
                    typeof(IClickableMenu),
                    nameof(IClickableMenu.drawToolTip),
                    new Type[]
                    {
                        typeof(SpriteBatch),
                        typeof(string),
                        typeof(string),
                        typeof(Item),
                        typeof(bool),
                        typeof(int),
                        typeof(int),
                        typeof(string),
                        typeof(int),
                        typeof(CraftingRecipe),
                        typeof(int)
                    }
                );

                if (drawToolTipMethod != null)
                {
                    harmony.Patch(
                        original: drawToolTipMethod,
                        prefix: new HarmonyMethod(typeof(MenuTooltipPatch), nameof(Prefix))
                    );
                    monitor.Log("Successfully applied Harmony patch to IClickableMenu.drawToolTip.", LogLevel.Debug);
                }
                else
                {
                    monitor.Log("Could not find IClickableMenu.drawToolTip method to patch.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed to patch IClickableMenu.drawToolTip: {ex}", LogLevel.Error);
            }
        }

        public static void Prefix(ref string hoverText, Item? hoveredItem)
        {
            if (hoveredItem == null)
                return;

            string extra = BuildItemExtraText(hoveredItem);
            if (!string.IsNullOrEmpty(extra))
            {
                if (string.IsNullOrEmpty(hoverText))
                {
                    hoverText = extra;
                }
                else if (!hoverText.Contains(extra))
                {
                    hoverText = hoverText + "\n" + extra;
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
                    bool isDonated = Game1.netWorldState.Value.MuseumPieces.Values.Contains(item.ItemId)
                                  || Game1.netWorldState.Value.MuseumPieces.Values.Contains(item.QualifiedItemId);
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
                if (Game1.netWorldState.Value.Bundles == null || Game1.netWorldState.Value.BundleData == null)
                    return results;

                foreach (var kvp in Game1.netWorldState.Value.BundleData)
                {
                    string[] parts = kvp.Value.Split('/');
                    if (parts.Length < 3)
                        continue;

                    string bundleName = parts[0];
                    string[] reqs = parts[2].Split(' ');

                    for (int i = 0; i < reqs.Length; i += 3)
                    {
                        if (i >= reqs.Length) break;
                        string reqId = reqs[i];
                        if (reqId == item.ItemId || reqId == item.QualifiedItemId)
                        {
                            if (!results.Contains(bundleName))
                            {
                                results.Add(bundleName);
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
