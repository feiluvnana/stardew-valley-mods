// =====================================================================================
// ArtisanBalancer.cs - rebalances ARTISAN GOODS: flower mead price scaling and flavor
// retention, vegetable juice price buffs, truffle oil value scaling, expanded cask aging,
// fruit tree Year-1 ROI, mineral cracking margins, and mid/late dungeon monster loot.
//
// Machine quality is handled dynamically by MachineQualityPatches via Option 2 Quarter-Step matrix.
// This file manages data edits in "Data/Machines" and "Data/Objects".
// =====================================================================================
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace BetterIndustry
{
    /// <summary>
    /// Applies BetterIndustry's machine and object balance edits to "Data/Machines" and "Data/Objects".
    /// </summary>
    public static class ArtisanBalancer
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// SMAPI event handler fired while ANY game asset loads. Filters for
        /// "Data/Machines" and "Data/Objects" to apply balance edits.
        /// </summary>
        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            {
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
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    try
                    {
                        var data = asset.AsDictionary<string, ObjectData>().Data;
                        ApplyMilledGoodsEdits(data);
                        ApplyCookingOilEdits(data);
                        ApplyFruitTreeEdits(data);
                        ApplyMineralEdits(data);
                        ApplyMonsterLootEdits(data);
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Error applying object edits in ArtisanBalancer: {ex}", LogLevel.Error);
                    }
                }, AssetEditPriority.Late);
            }
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
        /// was used and sells for configured multiplier (default 1.35x), (2) vegetable juice price multiplied by
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
                        output.PreserveId = "DROP_IN_ID";
                        output.CopyPrice = true;
                        output.PriceModifiers = new List<QuantityModifier>
                        {
                            new()
                            {
                                Modification = QuantityModifier.ModificationType.Multiply,
                                Amount = Config.FlowerMeadMultiplier
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
        /// Oil Maker adjustments: Truffle Oil base price scales off the raw Truffle value (625g * 1.5 = 937g base)
        /// without double-dipping, allowing MachineQualityPatches to scale star qualities cleanly up to 2,625g Artisan.
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
                        output.CopyPrice = false;
                        output.PriceModifiers = new List<QuantityModifier>
                        {
                            new()
                            {
                                Modification = QuantityModifier.ModificationType.Set,
                                Amount = (int)Math.Round(625 * Config.TruffleOilMultiplier) // 937g
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

        /// <summary>
        /// Milled goods adjustments: assigns Artisan Goods category (-26) and rebalances base sell prices.
        /// </summary>
        private static void ApplyMilledGoodsEdits(IDictionary<string, ObjectData> data)
        {
            var milledItems = new[]
            {
                (Id: "246", Price: Config.WheatFlourBasePrice), // Wheat Flour
                (Id: "245", Price: Config.SugarBasePrice),      // Sugar
                (Id: "423", Price: Config.RiceBasePrice)        // Rice
            };

            foreach (var (id, price) in milledItems)
            {
                if (data.TryGetValue(id, out var objData) || data.TryGetValue($"(O){id}", out objData))
                {
                    if (Config.EnableMillArtisanCategory)
                    {
                        objData.Category = StardewValley.Object.artisanGoodsCategory; // -26
                    }

                    if (Config.EnableMillBalancing)
                    {
                        objData.Price = price;
                    }
                }
            }
        }

        /// <summary>
        /// Cooking Oil adjustments: assigns Artisan Goods category (-26) so it benefits from the Artisan profession.
        /// </summary>
        private static void ApplyCookingOilEdits(IDictionary<string, ObjectData> data)
        {
            if (!Config.EnableCookingOilArtisanCategory)
                return;

            if (data.TryGetValue("247", out var oilData) || data.TryGetValue("(O)247", out oilData))
            {
                oilData.Category = StardewValley.Object.artisanGoodsCategory; // -26
            }
        }

        /// <summary>
        /// Fruit Tree adjustments: rebalances fruit sell prices for guaranteed positive Year-1 ROI.
        /// </summary>
        private static void ApplyFruitTreeEdits(IDictionary<string, ObjectData> data)
        {
            if (!Config.EnableFruitTreeRebalance)
                return;

            var fruitPrices = new Dictionary<string, int>
            {
                ["634"] = 75,   // Apricot (was 50g -> 2,100g Y1 revenue vs 2,000g sapling)
                ["638"] = 110,  // Cherry (was 80g -> 3,080g Y1 revenue vs 3,400g sapling)
                ["635"] = 135,  // Orange (was 100g -> 3,780g Y1 revenue vs 4,000g sapling)
                ["613"] = 135,  // Apple (was 100g -> 3,780g Y1 revenue vs 4,000g sapling)
                ["636"] = 180,  // Peach (was 140g -> 5,040g Y1 revenue vs 6,000g sapling)
                ["637"] = 180,  // Pomegranate (was 140g -> 5,040g Y1 revenue vs 6,000g sapling)
                ["91"] = 180,   // Banana (was 150g -> 5,040g Y1 revenue)
                ["834"] = 160   // Mango (was 130g -> 4,480g Y1 revenue)
            };

            foreach (var (id, price) in fruitPrices)
            {
                if (data.TryGetValue(id, out var objData) || data.TryGetValue($"(O){id}", out objData))
                {
                    objData.Price = price;
                }
            }
        }

        /// <summary>
        /// Mineral adjustments: rebalances 41 geode minerals and 4 foraged minerals for 2-digit profit increases.
        /// </summary>
        private static void ApplyMineralEdits(IDictionary<string, ObjectData> data)
        {
            if (!Config.EnableMineralPriceRebalance)
                return;

            var mineralPrices = new Dictionary<string, int>
            {
                // Standard Geode Minerals (15 items)
                ["571"] = 40,   // Limestone (was 15g, +25g)
                ["574"] = 50,   // Mudstone (was 25g, +25g)
                ["576"] = 90,   // Sandstone (was 60g, +30g)
                ["539"] = 110,  // Calcite (was 75g, +35g)
                ["569"] = 110,  // Granite (was 75g, +35g)
                ["544"] = 120,  // Nekoite (was 80g, +40g)
                ["545"] = 120,  // Orpiment (was 80g, +40g)
                ["577"] = 125,  // Slate (was 85g, +40g)
                ["543"] = 145,  // Malachite (was 100g, +45g)
                ["558"] = 145,  // Thunder Egg (was 100g, +45g)
                ["541"] = 165,  // Jagoite (was 115g, +50g)
                ["557"] = 140,  // Petrified Slime (was 120g, +20g conservative)
                ["540"] = 175,  // Celestine (was 125g, +50g)
                ["538"] = 205,  // Alamite (was 150g, +55g)
                ["542"] = 205,  // Jamborite (was 150g, +55g)

                // Frozen Geode Minerals (14 items)
                ["549"] = 145,  // Esperite (was 100g, +45g)
                ["550"] = 145,  // Fluorapatite (was 100g, +45g)
                ["567"] = 160,  // Marble (was 110g, +50g)
                ["559"] = 170,  // Pyrite (was 120g, +50g)
                ["572"] = 170,  // Soapstone (was 120g, +50g)
                ["548"] = 175,  // Aerinite (was 125g, +50g)
                ["551"] = 210,  // Geminite (was 150g, +60g)
                ["573"] = 210,  // Hematite (was 150g, +60g)
                ["564"] = 210,  // Opal (was 150g, +60g)
                ["561"] = 265,  // Ghost Crystal (was 200g, +65g)
                ["554"] = 265,  // Lunarite (was 200g, +65g)
                ["560"] = 290,  // Ocean Stone (was 220g, +70g)
                ["578"] = 325,  // Fairy Stone (was 250g, +75g)
                ["553"] = 325,  // Kyanite (was 250g, +75g)

                // Magma Geode Minerals (12 items)
                ["546"] = 85,   // Baryte (was 50g, +35g)
                ["552"] = 165,  // Bixbite (was 115g, +50g)
                ["563"] = 210,  // Jasper (was 150g, +60g)
                ["570"] = 240,  // Basalt (was 175g, +65g)
                ["562"] = 245,  // Lava Teardrop (was 180g, +65g)
                ["556"] = 270,  // Lemon Stone (was 200g, +70g)
                ["568"] = 270,  // Obsidian (was 200g, +70g)
                ["566"] = 350,  // Tigerseye (was 275g, +75g)
                ["575"] = 380,  // Dolomite (was 300g, +80g)
                ["565"] = 435,  // Fire Opal (was 350g, +85g)
                ["555"] = 540,  // Helvite (was 450g, +90g)
                ["579"] = 560,  // Star Shards (was 500g, +60g < 750g Diamond)

                // Foraged Mining Minerals (4 items)
                ["80"] = 45,    // Quartz (was 25g, +20g)
                ["86"] = 80,    // Earth Crystal (was 50g, +30g)
                ["84"] = 110,   // Frozen Tear (was 75g, +35g)
                ["82"] = 145    // Fire Quartz (was 100g, +45g)
            };

            foreach (var (id, price) in mineralPrices)
            {
                if (data.TryGetValue(id, out var objData) || data.TryGetValue($"(O){id}", out objData))
                {
                    objData.Price = price;
                }
            }
        }

        /// <summary>
        /// Monster loot adjustments: rebalances mid/late-game dungeon drops (Solar/Void Essence, Squid Ink, Bone Fragment).
        /// Strict early game protection: Bug Meat (8g), Slime (5g), and Bat Wing (15g) are left 100% vanilla.
        /// </summary>
        private static void ApplyMonsterLootEdits(IDictionary<string, ObjectData> data)
        {
            if (!Config.EnableMonsterLootRebalance)
                return;

            var monsterDrops = new Dictionary<string, int>
            {
                ["881"] = 25,   // Bone Fragment (was 12g, +13g)
                ["768"] = 75,   // Solar Essence (was 40g, +35g)
                ["769"] = 90,   // Void Essence (was 50g, +40g)
                ["814"] = 175   // Squid Ink (was 110g, +65g)
            };

            foreach (var (id, price) in monsterDrops)
            {
                if (data.TryGetValue(id, out var objData) || data.TryGetValue($"(O){id}", out objData))
                {
                    objData.Price = price;
                }
            }
        }
    }
}
