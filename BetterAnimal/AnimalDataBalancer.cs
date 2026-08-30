using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Machines;

namespace BetterAnimal
{
    /// <summary>
    /// Manages data edits for Data/FarmAnimals and Data/Machines (Loom).
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
        /// Edits Data/FarmAnimals: reduces rabbit and dinosaur production cooldowns.
        /// </summary>
        private static void ApplyFarmAnimalEdits(IDictionary<string, FarmAnimalData> data)
        {
            if (Config.EnableRabbitCooldownReduction && data.TryGetValue("Rabbit", out var rabbitData))
            {
                rabbitData.DaysToProduce = Config.RabbitDaysToProduce;
            }

            if (Config.EnableDinosaurCooldownReduction && data.TryGetValue("Dinosaur", out var dinoData))
            {
                dinoData.DaysToProduce = Config.DinosaurDaysToProduce;
            }
        }

        /// <summary>
        /// Edits Data/Machines: adds a Loom recipe allowing Duck Feathers to be spun into luxury Down Cloth
        /// based on base feather value (250g * 1.5 = 375g base) without double-dipping.
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
                            PriceModifiers = new List<QuantityModifier>
                            {
                                new()
                                {
                                    Modification = QuantityModifier.ModificationType.Set,
                                    Amount = 375
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
