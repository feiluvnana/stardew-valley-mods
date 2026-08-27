// =====================================================================================
// ArtisanBalancer.cs - rebalances ARTISAN GOODS: flower mead 2.0x value scaling and flavor
// retention, vegetable juice price buffs, truffle oil value scaling, and expanded cask aging.
//
// Machine quality is handled dynamically by MachineQualityPatches via Option 2 Quarter-Step matrix.
// This file manages data edits in "Data/Machines".
// =====================================================================================
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Machines;

namespace BetterIndustry
{
    /// <summary>
    /// Applies BetterIndustry's machine tweaks to the "Data/Machines" asset:
    /// flower-mead 2.0x pricing, vegetable juice buff, truffle-oil price scaling,
    /// and expanded cask aging for vegetable juice.
    /// </summary>
    public static class ArtisanBalancer
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// SMAPI event handler fired while ANY game asset loads. Filters for
        /// "Data/Machines" and queues our edits.
        /// </summary>
        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (!e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
                return;

            e.Edit(asset =>
            {
                try
                {
                    var data = asset.AsDictionary<string, MachineData>().Data;

                    ApplyKegEdits(data);
                    ApplyOilMakerEdits(data);
                    ApplyCaskEdits(data);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error applying machine balance in ArtisanBalancer: {ex}", LogLevel.Error);
                }
            }, AssetEditPriority.Late);
        }

        /// <summary>
        /// Finds a machine's data by trying several candidate IDs.
        /// </summary>
        private static MachineData? GetMachine(IDictionary<string, MachineData> data, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (data.TryGetValue(key, out var machine) && machine != null)
                    return machine;
            }
            return null;
        }

        /// <summary>
        /// Keg adjustments: (1) mead made from flower honey remembers WHICH flower honey
        /// was used and sells for 2x its price, (2) vegetable juice price multiplied by
        /// the configured buff factor (default 2.75x).
        /// </summary>
        private static void ApplyKegEdits(IDictionary<string, MachineData> data)
        {
            var keg = GetMachine(data, "(BC)12", "12", "Keg");
            if (keg?.OutputRules == null) return;

            foreach (var rule in keg.OutputRules)
            {
                if (rule.OutputItem == null) continue;

                foreach (var output in rule.OutputItem)
                {
                    // 1. Flower Honey Mead Fix
                    if (Config.EnableMeadFix && IsMeadOutput(rule, output))
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

                    // 2. Vegetable Juice Buff
                    if (Config.EnableJuiceBuff && IsJuiceOutput(rule, output))
                    {
                        output.PriceModifiers = new List<QuantityModifier>
                        {
                            new()
                            {
                                Modification = QuantityModifier.ModificationType.Multiply,
                                Amount = Config.JuiceMultiplier
                            }
                        };
                    }
                }
            }
        }

        /// <summary>
        /// True when this rule/output combination produces MEAD (item id 459).
        /// </summary>
        private static bool IsMeadOutput(MachineOutputRule rule, MachineItemOutput output)
        {
            return output.ItemId == "459"
                || output.ItemId == "(O)459"
                || string.Equals(output.Id, "Mead", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Mead", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Default_Mead", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when this output is JUICE (item id 350 or preserve type Juice).
        /// </summary>
        private static bool IsJuiceOutput(MachineOutputRule rule, MachineItemOutput output)
        {
            return string.Equals(output.PreserveType, "Juice", StringComparison.OrdinalIgnoreCase)
                || output.ItemId == "350"
                || output.ItemId == "(O)350"
                || string.Equals(output.Id, "Juice", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Juice", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Default_Juice", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Oil Maker adjustments: Truffle Oil price scales off the input Truffle value.
        /// </summary>
        private static void ApplyOilMakerEdits(IDictionary<string, MachineData> data)
        {
            var oilMaker = GetMachine(data, "(BC)19", "19", "OilMaker");
            if (oilMaker?.OutputRules == null) return;

            foreach (var rule in oilMaker.OutputRules)
            {
                if (rule.OutputItem == null) continue;

                foreach (var output in rule.OutputItem)
                {
                    bool isTruffleOil = output.ItemId == "432"
                        || output.ItemId == "(O)432"
                        || string.Equals(output.Id, "TruffleOil", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(rule.Id, "Truffle", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(rule.Id, "TruffleOil", StringComparison.OrdinalIgnoreCase);

                    if (isTruffleOil && Config.EnableTruffleOilFix)
                    {
                        output.CopyPrice = true;
                        output.PriceModifiers = new List<QuantityModifier>
                        {
                            new()
                            {
                                Modification = QuantityModifier.ModificationType.Multiply,
                                Amount = Config.TruffleOilMultiplier
                            }
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Cask adjustments: adds an aging rule so Vegetable Juice can age in cellar casks (AgingMultiplier = 4).
        /// </summary>
        private static void ApplyCaskEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableExpandedAging) return;

            var cask = GetMachine(data, "(BC)163", "163", "Cask");
            if (cask?.OutputRules == null) return;

            bool hasJuiceRule = cask.OutputRules.Exists(r =>
                string.Equals(r.Id, "BetterIndustry_Juice", StringComparison.OrdinalIgnoreCase) ||
                (r.Triggers != null && r.Triggers.Exists(t => t.RequiredItemId == "(O)350" || t.RequiredItemId == "350")));

            if (!hasJuiceRule)
            {
                var juiceRule = new MachineOutputRule
                {
                    Id = "BetterIndustry_Juice",
                    Triggers = new List<MachineOutputTriggerRule>
                    {
                        new()
                        {
                            Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                            RequiredItemId = "(O)350"
                        }
                    },
                    UseFirstValidOutput = true,
                    OutputItem = new List<MachineItemOutput>
                    {
                        new()
                        {
                            OutputMethod = "StardewValley.Objects.Cask, Stardew Valley:OutputCask",
                            CustomData = new Dictionary<string, string>
                            {
                                ["AgingMultiplier"] = "4"
                            }
                        }
                    }
                };

                cask.OutputRules.Add(juiceRule);
            }
        }
    }
}
