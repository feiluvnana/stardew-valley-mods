using System;
using System.Collections.Generic;
using System.Text.Json;
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
            if (!e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
                return;

            e.Edit(asset =>
            {
                try
                {
                    var data = asset.AsDictionary<string, MachineData>().Data;

                    ApplyKegEdits(data);
                    ApplyPreservesJarEdits(data);
                    ApplyCheesePressEdits(data);
                    ApplyMayonnaiseMachineEdits(data);
                    ApplyLoomEdits(data);
                    ApplyOilMakerEdits(data);
                    ApplyDehydratorEdits(data);
                    ApplyFishSmokerEdits(data);
                    ApplyCaskEdits(data);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error applying machine balance in ArtisanBalancer: {ex}", LogLevel.Error);
                }
            }, AssetEditPriority.Late);
        }

        private static MachineData? GetMachine(IDictionary<string, MachineData> data, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (data.TryGetValue(key, out var machine) && machine != null)
                    return machine;
            }
            return null;
        }

        private const string IridiumQualityTag = "quality_iridium";
        private const string IridiumRuleSuffix = "_BI_Iridium";

        private static void ApplyQualityPreservingToAllOutputs(MachineData machine)
        {
            if (machine.OutputRules == null || machine.OutputRules.Count == 0)
                return;

            var newOrder = new List<MachineOutputRule>();
            bool changed = false;

            foreach (var rule in machine.OutputRules)
            {
                // Vanilla caps large animal products at gold quality (e.g., Large Goat Milk -> Gold Goat Cheese).
                // Keep that floor for lower qualities, but let iridium inputs pass through via a higher-priority
                // duplicate rule gated on the "quality_iridium" context tag.
                bool hasFixedHighQualityOutput = false;
                if (rule.OutputItem != null)
                {
                    foreach (var output in rule.OutputItem)
                    {
                        if (output.Quality >= 2)
                            hasFixedHighQualityOutput = true;
                        else
                            output.CopyQuality = true;
                    }
                }

                if (
                    hasFixedHighQualityOutput
                    && rule.Triggers != null
                    && rule.Triggers.Count > 0
                    && !HasRule(machine.OutputRules, rule.Id + IridiumRuleSuffix)
                    && TryCreateIridiumPassthroughRule(rule, out var iridiumRule)
                )
                {
                    newOrder.Add(iridiumRule);
                    changed = true;
                }

                newOrder.Add(rule);
            }

            if (changed)
                machine.OutputRules = newOrder;
        }

        private static bool HasRule(List<MachineOutputRule> rules, string id)
        {
            foreach (var rule in rules)
            {
                if (string.Equals(rule.Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool TryCreateIridiumPassthroughRule(MachineOutputRule source, out MachineOutputRule clone)
        {
            clone = new MachineOutputRule();
            try
            {
                var json = JsonSerializer.Serialize(source);
                var cloned = JsonSerializer.Deserialize<MachineOutputRule>(json);
                if (cloned == null || cloned.Triggers == null || cloned.OutputItem == null)
                    return false;

                cloned.Id = source.Id + IridiumRuleSuffix;

                foreach (var trigger in cloned.Triggers)
                {
                    trigger.RequiredTags ??= new List<string>();
                    bool hasTag = false;
                    foreach (var tag in trigger.RequiredTags)
                    {
                        if (string.Equals(tag, IridiumQualityTag, StringComparison.OrdinalIgnoreCase))
                        {
                            hasTag = true;
                            break;
                        }
                    }
                    if (!hasTag)
                        trigger.RequiredTags.Add(IridiumQualityTag);
                }

                foreach (var output in cloned.OutputItem)
                {
                    output.Quality = -1;
                    output.CopyQuality = true;
                }

                clone = cloned;
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Could not create iridium passthrough rule for '{source.Id}': {ex}", LogLevel.Trace);
                return false;
            }
        }

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

                    // 3. Quality Preserving
                    if (Config.EnableQualityPreserving)
                    {
                        output.CopyQuality = true;
                    }
                }
            }
        }

        private static bool IsMeadOutput(MachineOutputRule rule, MachineItemOutput output)
        {
            return output.ItemId == "459"
                || output.ItemId == "(O)459"
                || string.Equals(output.Id, "Mead", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Mead", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Default_Mead", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsJuiceOutput(MachineOutputRule rule, MachineItemOutput output)
        {
            return string.Equals(output.PreserveType, "Juice", StringComparison.OrdinalIgnoreCase)
                || output.ItemId == "350"
                || output.ItemId == "(O)350"
                || string.Equals(output.Id, "Juice", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Juice", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Default_Juice", StringComparison.OrdinalIgnoreCase);
        }


        private static void ApplyPreservesJarEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var jar = GetMachine(data, "(BC)15", "15", "PreservesJar");
            if (jar != null)
            {
                ApplyQualityPreservingToAllOutputs(jar);
            }
        }

        private static void ApplyCheesePressEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var press = GetMachine(data, "(BC)16", "16", "CheesePress");
            if (press != null)
            {
                ApplyQualityPreservingToAllOutputs(press);
            }
        }

        private static void ApplyMayonnaiseMachineEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var mayo = GetMachine(data, "(BC)24", "24", "MayonnaiseMachine");
            if (mayo != null)
            {
                ApplyQualityPreservingToAllOutputs(mayo);
            }
        }

        private static void ApplyLoomEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var loom = GetMachine(data, "(BC)17", "17", "Loom");
            if (loom != null)
            {
                ApplyQualityPreservingToAllOutputs(loom);
            }
        }

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
                        output.CopyQuality = true;
                        output.PriceModifiers = new List<QuantityModifier>
                        {
                            new()
                            {
                                Modification = QuantityModifier.ModificationType.Multiply,
                                Amount = Config.TruffleOilMultiplier
                            }
                        };
                    }
                    else if (Config.EnableQualityPreserving)
                    {
                        output.CopyQuality = true;
                    }
                }
            }
        }

        private static void ApplyDehydratorEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var dehydrator = GetMachine(data, "(BC)Dehydrator", "Dehydrator", "(BC)272", "272");
            if (dehydrator != null)
            {
                ApplyQualityPreservingToAllOutputs(dehydrator);
            }
        }

        private static void ApplyFishSmokerEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var smoker = GetMachine(data, "(BC)FishSmoker", "FishSmoker", "(BC)274", "274");
            if (smoker != null)
            {
                ApplyQualityPreservingToAllOutputs(smoker);
            }
        }

        private static void ApplyCaskEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableExpandedAging) return;

            var cask = GetMachine(data, "(BC)163", "163", "Cask");
            if (cask?.OutputRules == null) return;

            // Check if Juice aging rule is already added
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

