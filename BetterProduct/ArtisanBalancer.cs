using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
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
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, MachineData>().Data;

                    // Keg edits ((BC)12)
                    if (data.TryGetValue("(BC)12", out var keg) && keg.OutputRules != null)
                    {
                        foreach (var rule in keg.OutputRules)
                        {
                            if (rule.OutputItem == null) continue;

                            foreach (var output in rule.OutputItem)
                            {
                                // Mead rebalancing from Honey
                                if (Config.EnableMeadFix && (output.ItemId == "459" || output.ItemId == "(O)459" || output.Id == "Mead"))
                                {
                                    output.PreserveId = "DROP_IN_PRESERVE_ID";
                                    output.CopyPrice = true;
                                    output.PriceModifiers = new List<QuantityModifier>
                                    {
                                        new()
                                        {
                                            Modification = QuantityModifier.ModificationType.Multiply,
                                            Amount = Config.MeadMultiplier
                                        }
                                    };
                                }

                                // Juice rebalancing
                                if (Config.EnableJuiceBuff && (output.ItemId == "350" || output.ItemId == "(O)350" || output.PreserveType == "Juice"))
                                {
                                    output.PriceModifiers ??= new List<QuantityModifier>();
                                    var multMod = output.PriceModifiers.Find(m => m.Modification == QuantityModifier.ModificationType.Multiply);
                                    if (multMod != null)
                                    {
                                        multMod.Amount = Config.JuiceMultiplier;
                                    }
                                    else
                                    {
                                        output.PriceModifiers.Add(new QuantityModifier
                                        {
                                            Modification = QuantityModifier.ModificationType.Multiply,
                                            Amount = Config.JuiceMultiplier
                                        });
                                    }
                                }
                            }
                        }
                    }

                    // Preserves Jar edits ((BC)15)
                    if (data.TryGetValue("(BC)15", out var preservesJar) && preservesJar.OutputRules != null)
                    {
                        foreach (var rule in preservesJar.OutputRules)
                        {
                            if (rule.OutputItem == null) continue;

                            foreach (var output in rule.OutputItem)
                            {
                                // Pickles rebalancing
                                if (Config.EnablePickleBuff && (output.ItemId == "342" || output.ItemId == "(O)342" || output.PreserveType == "Pickle"))
                                {
                                    if (output.PriceModifiers != null)
                                    {
                                        var multMod = output.PriceModifiers.Find(m => m.Modification == QuantityModifier.ModificationType.Multiply);
                                        if (multMod != null)
                                        {
                                            multMod.Amount = Config.PickleMultiplier;
                                        }
                                    }
                                }

                                // Aged Roe rebalancing
                                if (Config.EnableRoeBuff && (output.ItemId == "447" || output.ItemId == "(O)447" || output.PreserveType == "AgedRoe"))
                                {
                                    if (output.PriceModifiers != null)
                                    {
                                        var multMod = output.PriceModifiers.Find(m => m.Modification == QuantityModifier.ModificationType.Multiply);
                                        if (multMod != null)
                                        {
                                            multMod.Amount = Config.AgedRoeMultiplier;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }, AssetEditPriority.Late);
            }
        }
    }
}