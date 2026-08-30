using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace BetterAnimal
{
    /// <summary>
    /// Manages data edits for Data/FarmAnimals, Data/Objects, and Data/Machines (Loom).
    /// </summary>
    public static class AnimalDataBalancer
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Asset requested event handler for game data edits.
        /// </summary>
        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/FarmAnimals"))
            {
                e.Edit(asset =>
                {
                    try
                    {
                        var data = asset.AsDictionary<string, FarmAnimalData>().Data;
                        ApplyFarmAnimalEdits(data);
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Error applying animal edits in AnimalDataBalancer: {ex}", LogLevel.Error);
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
                        ApplyObjectEdits(data);
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Error applying object edits in AnimalDataBalancer: {ex}", LogLevel.Error);
                    }
                }, AssetEditPriority.Late);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            {
                e.Edit(asset =>
                {
                    try
                    {
                        var data = asset.AsDictionary<string, MachineData>().Data;
                        ApplyLoomEdits(data);
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Error applying loom edits in AnimalDataBalancer: {ex}", LogLevel.Error);
                    }
                }, AssetEditPriority.Late);
            }
        }

        /// <summary>
        /// Edits Data/FarmAnimals: reduces rabbit production cooldown.
        /// </summary>
        private static void ApplyFarmAnimalEdits(IDictionary<string, FarmAnimalData> data)
        {
            if (!Config.EnableRabbitCooldownReduction)
                return;

            if (data.TryGetValue("Rabbit", out var rabbitData))
            {
                rabbitData.DaysToProduce = Config.RabbitDaysToProduce;
            }
        }

        /// <summary>
        /// Edits Data/Objects: rebalances base sell price for Rabbit's Foot.
        /// </summary>
        private static void ApplyObjectEdits(IDictionary<string, ObjectData> data)
        {
            if (!Config.EnableRabbitFootRebalance)
                return;

            if (data.TryGetValue("446", out var footData) || data.TryGetValue("(O)446", out footData))
            {
                footData.Price = Config.RabbitFootBasePrice;
            }
        }

        /// <summary>
        /// Edits Data/Machines: adds a Loom recipe allowing Duck Feathers to be spun into luxury Down Cloth.
        /// </summary>
        private static void ApplyLoomEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableDuckFeatherLoom)
                return;

            MachineData? loom = null;
            string[] loomKeys = { "(BC)17", "17", "Loom" };
            foreach (var key in loomKeys)
            {
                if (data.TryGetValue(key, out var machine) && machine != null)
                {
                    loom = machine;
                    break;
                }
            }

            if (loom?.OutputRules == null)
                return;

            bool hasFeatherRule = loom.OutputRules.Exists(r =>
                string.Equals(r.Id, "BetterAnimal_DuckFeather", StringComparison.OrdinalIgnoreCase) ||
                (r.Triggers != null && r.Triggers.Exists(t => t.RequiredItemId == "(O)444" || t.RequiredItemId == "444")));

            if (!hasFeatherRule)
            {
                var featherRule = new MachineOutputRule
                {
                    Id = "BetterAnimal_DuckFeather",
                    Triggers = new List<MachineOutputTriggerRule>
                    {
                        new()
                        {
                            Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                            RequiredItemId = "(O)444"
                        }
                    },
                    UseFirstValidOutput = true,
                    MinutesUntilReady = 240,
                    OutputItem = new List<MachineItemOutput>
                    {
                        new()
                        {
                            ItemId = "(O)428",
                            CopyPrice = true,
                            PriceModifiers = new List<QuantityModifier>
                            {
                                new()
                                {
                                    Modification = QuantityModifier.ModificationType.Multiply,
                                    Amount = 2.5f
                                }
                            }
                        }
                    }
                };

                loom.OutputRules.Add(featherRule);
            }
        }
    }
}
