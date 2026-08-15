using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace BetterProduct
{
    public static class ArtisanBalancer
    {
        private static ModConfig Config = null!;
        private static IMonitor Monitor = null!;

        public static void Initialize(ModConfig config, IMonitor monitor)
        {
            Config = config;
            Monitor = monitor;
        }

        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;

                    // Caviar price buff
                    if (Config.CaviarPrice > 0 && data.TryGetValue("445", out var caviarData))
                    {
                        caviarData.Price = Config.CaviarPrice;
                    }
                }, AssetEditPriority.Late);
            }
        }

        public static int CalculatePreservePrice(StardewValley.Object obj, int originalPrice)
        {
            if (obj == null)
                return originalPrice;

            // Check preserve types
            if (obj.preserve.Value.HasValue)
            {
                var preserveType = obj.preserve.Value.Value;

                if (Config.EnableJuiceBuff && preserveType == StardewValley.Object.PreserveType.Juice)
                {
                    if (obj.Price > 0)
                    {
                        // In vanilla: 2.25 * base. Buff with Config.JuiceMultiplier / 2.25
                        float scale = Config.JuiceMultiplier / 2.25f;
                        return Math.Max(originalPrice, (int)Math.Round(originalPrice * scale));
                    }
                }
                else if (Config.EnablePickleBuff && preserveType == StardewValley.Object.PreserveType.Pickle)
                {
                    // In vanilla: 50 + 2 * base. Buff with Config.PickleMultiplier / 2.0
                    float scale = Config.PickleMultiplier / 2.0f;
                    return Math.Max(originalPrice, (int)Math.Round(originalPrice * scale));
                }
                else if (Config.EnableRoeBuff && preserveType == StardewValley.Object.PreserveType.AgedRoe)
                {
                    // In vanilla: 2 * base roe. Buff with Config.AgedRoeMultiplier / 2.0
                    float scale = Config.AgedRoeMultiplier / 2.0f;
                    return Math.Max(originalPrice, (int)Math.Round(originalPrice * scale));
                }
            }

            return originalPrice;
        }
    }
}