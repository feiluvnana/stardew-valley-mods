using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.Machines;

namespace BetterIndustry
{
    public static class ArtisanBalancer
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (!Config.EnableMeadFix)
                return;

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            {
                e.Edit(asset =>
                {
                    try
                    {
                        var data = asset.AsDictionary<string, MachineData>().Data;

                        // Keg edits ((BC)12) - Mead retains honey type with default 2.0x multiplier
                        if (data.TryGetValue("(BC)12", out var keg) && keg.OutputRules != null)
                        {
                            foreach (var rule in keg.OutputRules)
                            {
                                if (rule.OutputItem == null) continue;

                                foreach (var output in rule.OutputItem)
                                {
                                    if (output.ItemId == "459" || output.ItemId == "(O)459" || output.Id == "Mead")
                                    {
                                        output.PreserveId = "DROP_IN_PRESERVE_ID";
                                        output.CopyPrice = true;
                                        output.PriceModifiers = new List<QuantityModifier>
                                        {
                                            new()
                                            {
                                                Modification = QuantityModifier.ModificationType.Multiply,
                                                Amount = 2.0f
                                            }
                                        };
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Error applying mead fix in ArtisanBalancer: {ex}", LogLevel.Error);
                    }
                }, AssetEditPriority.Late);
            }
        }
    }
}
