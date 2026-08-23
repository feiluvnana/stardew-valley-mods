using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.TokenizableStrings;

namespace BetterQOL
{
    public class MachineInfo
    {
        public string MachineName { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? HeldItemName { get; set; }
        public int HeldItemStack { get; set; } = 1;
        public int HeldItemQuality { get; set; } = 0;
        public Texture2D? HeldItemTexture { get; set; }
        public Rectangle? HeldItemSourceRect { get; set; }

        public bool IsReadyToHarvest { get; set; }
        public bool IsProcessing { get; set; }
        public int MinutesRemaining { get; set; }

        public string? TimeRemainingText { get; set; }
        public string? TargetFinishTimeText { get; set; }

        // Idle state
        public bool IsIdle { get; set; }
        public string? IdleStatusText { get; set; }

        // Special machine details
        public bool IsCask { get; set; }
        public int CaskCurrentQuality { get; set; }
        public int CaskDaysToNextQuality { get; set; }
        public int CaskDaysToIridium { get; set; }
        public int CaskNextQuality { get; set; }

        public bool IsCrabPot { get; set; }
        public bool CrabPotHasBait { get; set; }
        public string? CrabPotBaitName { get; set; }

        public bool IsAutoGrabber { get; set; }
        public int AutoGrabberItemCount { get; set; }
    }

    public class BuildingMachineInfo
    {
        public string BuildingName { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public Texture2D? IconTexture { get; set; }
        public Rectangle? IconSourceRect { get; set; }
        public List<TooltipLine> Lines { get; set; } = new();
    }

    public static class MachineHelper
    {
        public static MachineInfo? GetMachineInfo(StardewValley.Object obj)
        {
            if (obj == null)
                return null;

            // Handle Cask
            if (obj is Cask cask)
            {
                return GetCaskInfo(cask);
            }

            // Handle Crab Pot
            if (obj is CrabPot crabPot)
            {
                return GetCrabPotInfo(crabPot);
            }

            // Check if it's a Chest (containers, not machines, unless Auto-Grabber)
            if (obj is Chest chest && !chest.QualifiedItemId.Contains("165") && !chest.Name.Contains("Auto-Grabber", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Check if it's a Scarecrow
            if (obj.IsScarecrow())
            {
                return null;
            }

            string qualifiedId = obj.QualifiedItemId;
            string itemId = obj.ItemId;
            string name = obj.Name ?? string.Empty;
            MachineData? machineData = obj.GetMachineData();

            // Check special machines
            bool isAutoGrabber = qualifiedId.Contains("165") || name.Contains("Auto-Grabber", StringComparison.OrdinalIgnoreCase);
            bool isCoffeeMaker = qualifiedId.Contains("246") || name.Contains("Coffee Maker", StringComparison.OrdinalIgnoreCase);
            bool isWorkbench = qualifiedId.Contains("208") || name.Contains("Workbench", StringComparison.OrdinalIgnoreCase);
            bool isSewingMachine = qualifiedId.Contains("247") || qualifiedId.Contains("SewingMachine", StringComparison.OrdinalIgnoreCase) || name.Contains("Sewing Machine", StringComparison.OrdinalIgnoreCase);
            bool isAnvil = qualifiedId.Contains("Anvil", StringComparison.OrdinalIgnoreCase) || name.Contains("Anvil", StringComparison.OrdinalIgnoreCase);
            bool isMiniForge = qualifiedId.Contains("MiniForge", StringComparison.OrdinalIgnoreCase) || name.Contains("Mini-Forge", StringComparison.OrdinalIgnoreCase);
            bool isStatue = qualifiedId.Contains("160") || qualifiedId.Contains("StatueOf", StringComparison.OrdinalIgnoreCase) || name.Contains("Statue of", StringComparison.OrdinalIgnoreCase);

            bool isKnownMachine = machineData != null
                               || isAutoGrabber
                               || isCoffeeMaker
                               || isWorkbench
                               || isSewingMachine
                               || isAnvil
                               || isMiniForge
                               || isStatue
                               || obj.heldObject.Value != null
                               || obj.MinutesUntilReady > 0
                               || obj.readyForHarvest.Value;

            if (!isKnownMachine)
            {
                // If it's a generic bigCraftable that has no machine data and not a known machine, skip
                return null;
            }

            var info = new MachineInfo
            {
                MachineName = obj.DisplayName
            };

            // 1. Auto-Grabber
            if (isAutoGrabber)
            {
                info.IsAutoGrabber = true;
                if (obj.heldObject.Value is Chest agChest)
                {
                    int count = agChest.Items.Count(i => i != null);
                    info.AutoGrabberItemCount = count;
                    if (count > 0)
                    {
                        info.IsReadyToHarvest = true;
                        info.HeldItemName = ModEntry.I18n.Get("hover.autograbber.items-ready", new { count });
                    }
                    else
                    {
                        info.IsIdle = true;
                        info.IdleStatusText = ModEntry.I18n.Get("hover.autograbber.empty");
                    }
                }
                else
                {
                    info.IsIdle = true;
                    info.IdleStatusText = ModEntry.I18n.Get("hover.autograbber.empty");
                }
                return info;
            }

            // 2. Workbench
            if (isWorkbench)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.workbench.desc");
                return info;
            }

            // 3. Sewing Machine
            if (isSewingMachine)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.sewing.desc");
                return info;
            }

            // 4. Anvil
            if (isAnvil)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.anvil.desc");
                return info;
            }

            // 5. Mini-Forge
            if (isMiniForge)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.miniforge.desc");
                return info;
            }

            // Held item / output
            var held = obj.heldObject.Value;
            if (held != null)
            {
                info.HeldItemName = held.DisplayName;
                info.HeldItemStack = held.Stack;
                info.HeldItemQuality = held.Quality;

                var itemData = ItemRegistry.GetData(held.QualifiedItemId);
                if (itemData != null)
                {
                    try
                    {
                        info.HeldItemTexture = itemData.GetTexture();
                        info.HeldItemSourceRect = itemData.GetSourceRect();
                    }
                    catch
                    {
                        // Ignore texture failures
                    }
                }
            }

            // Ready state
            if (obj.readyForHarvest.Value || (held != null && obj.MinutesUntilReady <= 0))
            {
                info.IsReadyToHarvest = true;
                info.IsProcessing = false;
                return info;
            }

            // Processing countdown
            if (obj.MinutesUntilReady > 0)
            {
                info.IsProcessing = true;
                info.MinutesRemaining = obj.MinutesUntilReady;

                FormatMachineTime(obj.MinutesUntilReady, out string timeRemaining, out string finishTime);
                info.TimeRemainingText = timeRemaining;
                info.TargetFinishTimeText = finishTime;
                return info;
            }

            // Idle special cases (Coffee Maker, Statues, or regular idle machine)
            if (isCoffeeMaker)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.coffeemaker.desc");
                return info;
            }

            if (isStatue)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.statue.desc");
                return info;
            }

            // Generic idle data-driven machine
            if (machineData != null)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.machine.idle");
                return info;
            }

            return null;
        }

        public static BuildingMachineInfo? GetBuildingInfo(Building building)
        {
            if (building == null)
                return null;

            var info = new BuildingMachineInfo
            {
                BuildingName = building.buildingType.Value ?? ModEntry.I18n.Get("hover.building.generic")
            };

            // Custom Display Name if available
            try
            {
                var data = building.GetData();
                if (data != null && !string.IsNullOrEmpty(data.Name))
                {
                    info.BuildingName = TokenParser.ParseText(data.Name);
                }
            }
            catch { }

            // 1. Fish Pond
            if (building is FishPond fishPond)
            {
                string fishId = fishPond.fishType.Value;
                var fishData = !string.IsNullOrEmpty(fishId) ? (ItemRegistry.GetData(fishId) ?? ItemRegistry.GetData($"(O){fishId}")) : null;
                string fishName = fishData?.DisplayName ?? ModEntry.I18n.Get("hover.fishpond.generic-fish");
                info.BuildingName = $"{info.BuildingName} ({fishName})";

                if (fishData != null)
                {
                    try
                    {
                        info.IconTexture = fishData.GetTexture();
                        info.IconSourceRect = fishData.GetSourceRect();
                    }
                    catch { }
                }

                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.fishpond.population", new { count = fishPond.FishCount, max = fishPond.maxOccupants.Value }),
                    new Color(20, 110, 220)
                ));

                if (fishPond.output.Value != null)
                {
                    var outputData = ItemRegistry.GetData(fishPond.output.Value.QualifiedItemId);
                    string outName = outputData?.DisplayName ?? fishPond.output.Value.DisplayName;
                    string stackStr = fishPond.output.Value.Stack > 1 ? $" x{fishPond.output.Value.Stack}" : "";
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.fishpond.output-ready", new { item = $"{outName}{stackStr}" }),
                        new Color(0, 140, 0)
                    ));
                }
                else if (fishPond.neededItem.Value != null)
                {
                    string neededItemName = ModEntry.I18n.Get("hover.fishpond.default-item").ToString();
                    var neededItem = fishPond.neededItem.Value;
                    if (neededItem != null)
                    {
                        var itmData = ItemRegistry.GetData(neededItem.QualifiedItemId);
                        neededItemName = itmData?.DisplayName ?? neededItem.DisplayName;
                        int neededCount = fishPond.neededItemCount.Value;
                        if (neededCount > 1) neededItemName = $"{neededItemName} x{neededCount}";
                    }
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.fishpond.needs-quest-item", new { item = neededItemName }),
                        new Color(220, 20, 60)
                    ));
                }
                else
                {
                    if (fishPond.daysSinceSpawn.Value >= 0 && fishPond.FishCount < fishPond.maxOccupants.Value)
                    {
                        int spawnRate = fishPond.GetFishPondData()?.SpawnTime ?? 3;
                        int daysLeft = Math.Max(0, spawnRate - fishPond.daysSinceSpawn.Value);
                        if (daysLeft <= 1)
                        {
                            info.Lines.Add(new TooltipLine(
                                ModEntry.I18n.Get("hover.fishpond.spawning-tomorrow"),
                                new Color(0, 140, 0)
                            ));
                        }
                        else
                        {
                            info.Lines.Add(new TooltipLine(
                                ModEntry.I18n.Get("hover.fishpond.spawning-in", new { days = daysLeft }),
                                new Color(180, 100, 0)
                            ));
                        }
                    }
                    else
                    {
                        info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fishpond.producing"), Color.DarkSlateGray));
                    }
                }

                return info;
            }

            // 2. Mill
            if (building.buildingType.Value?.Equals("Mill", StringComparison.OrdinalIgnoreCase) == true)
            {
                var inputChest = building.GetBuildingChest("Input");
                var outputChest = building.GetBuildingChest("Output");

                int inputCount = inputChest?.Items.Count(i => i != null) ?? 0;
                int outputCount = outputChest?.Items.Count(i => i != null) ?? 0;

                if (outputCount > 0)
                {
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.mill.output-ready", new { count = outputCount }),
                        new Color(0, 140, 0)
                    ));
                }

                if (inputCount > 0)
                {
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.mill.processing-input", new { count = inputCount }),
                        new Color(180, 100, 0)
                    ));
                }
                else if (outputCount == 0)
                {
                    info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.mill.idle"), Color.DarkSlateGray));
                }

                return info;
            }

            // 3. Junimo Hut
            if (building is JunimoHut junimoHut)
            {
                var outputChest = junimoHut.GetOutputChest();
                int itemCount = outputChest?.Items.Count(i => i != null) ?? 0;

                bool isHarvesting = !junimoHut.noHarvest.Value && !Game1.IsRainingHere(junimoHut.GetParentLocation()) && Game1.season != Season.Winter;
                if (isHarvesting)
                {
                    info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.junimohut.active"), new Color(0, 140, 0)));
                }
                else
                {
                    info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.junimohut.paused"), Color.DarkSlateGray));
                }

                if (junimoHut.raisinDays.Value > 0)
                {
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.junimohut.raisins-active", new { days = junimoHut.raisinDays.Value }),
                        new Color(180, 50, 180)
                    ));
                }

                if (itemCount > 0)
                {
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.junimohut.items-stored", new { count = itemCount }),
                        new Color(20, 110, 220)
                    ));
                }

                return info;
            }

            // 4. Silo
            if (building.buildingType.Value?.Equals("Silo", StringComparison.OrdinalIgnoreCase) == true)
            {
                int hay = Game1.getFarm()?.piecesOfHay?.Value ?? 0;
                int siloCount = 0;
                if (Game1.getFarm() != null)
                {
                    foreach (var b in Game1.getFarm().buildings)
                    {
                        if (b.buildingType.Value?.Equals("Silo", StringComparison.OrdinalIgnoreCase) == true)
                            siloCount++;
                    }
                }
                int maxHay = Math.Max(1, siloCount) * 240;

                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.silo.hay-count", new { current = hay, max = maxHay }),
                    new Color(180, 100, 0)
                ));
                return info;
            }

            // 5. Shipping Bin
            if (building is ShippingBin || building.buildingType.Value?.Equals("Shipping Bin", StringComparison.OrdinalIgnoreCase) == true)
            {
                var farm = Game1.getFarm();
                int itemsCount = farm != null ? farm.getShippingBin(Game1.player).Count : 0;
                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.shippingbin.items", new { count = itemsCount }),
                    new Color(20, 110, 220)
                ));
                return info;
            }

            // 6. Pet Bowl (SDV 1.6)
            if (building is PetBowl petBowl || building.buildingType.Value?.Equals("Pet Bowl", StringComparison.OrdinalIgnoreCase) == true)
            {
                bool isWatered = false;
                if (building is PetBowl pb) isWatered = pb.watered.Value;
                info.Lines.Add(new TooltipLine(
                    isWatered ? ModEntry.I18n.Get("hover.petbowl.watered").ToString() : ModEntry.I18n.Get("hover.petbowl.unwatered").ToString(),
                    isWatered ? new Color(20, 110, 220) : new Color(200, 60, 20)
                ));
                return info;
            }

            // 7. Slime Hutch
            if (building.GetIndoors() is SlimeHutch || building.buildingType.Value?.Equals("Slime Hutch", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (building.GetIndoors() is SlimeHutch sh)
                {
                    int slimeCount = sh.characters.Count(c => c is StardewValley.Monsters.GreenSlime);
                    int troughsWatered = sh.waterSpots.Count(w => w);
                    int slimeBalls = sh.Objects.Pairs.Count(o => o.Value.QualifiedItemId == "(BC)56" || o.Value.Name == "Slime Ball");

                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.slimehutch.slimes-format", new { current = slimeCount, max = 20 }).ToString(),
                        slimeCount >= 20 ? new Color(0, 140, 0) : new Color(20, 110, 220)
                    ));
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.slimehutch.troughs-format", new { watered = troughsWatered, total = 4 }).ToString(),
                        troughsWatered == 4 ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                    if (slimeBalls > 0)
                    {
                        info.Lines.Add(new TooltipLine(
                            ModEntry.I18n.Get("hover.slimehutch.slimeballs-format", new { count = slimeBalls }).ToString(),
                            new Color(0, 140, 0)
                        ));
                    }
                }
                return info;
            }

            // 8. Stable
            if (building is Stable || building.buildingType.Value?.Equals("Stable", StringComparison.OrdinalIgnoreCase) == true)
            {
                string hName = Game1.player.horseName.Value ?? ModEntry.I18n.Get("hover.stable.horse").ToString();
                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.stable.horse-info", new { name = hName }).ToString(),
                    new Color(180, 100, 0)
                ));
                return info;
            }

            // 9. Animal Housing (Barn, Coop) - Only for actual AnimalHouse locations
            if (building.GetIndoors() is AnimalHouse animalHouse)
            {
                int current = animalHouse.animalsThatLiveHere.Count;
                int max = building.maxOccupants.Value > 0 ? building.maxOccupants.Value : animalHouse.animalLimit.Value;
                bool doorOpen = building.animalDoorOpen.Value;

                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.building.occupants", new { current = current, max = max }),
                    new Color(20, 110, 220)
                ));
                info.Lines.Add(new TooltipLine(
                    doorOpen ? ModEntry.I18n.Get("hover.animalhouse.door-open").ToString() : ModEntry.I18n.Get("hover.animalhouse.door-closed").ToString(),
                    doorOpen ? new Color(0, 140, 0) : Color.DarkSlateGray
                ));
                return info;
            }

            // 10. Shed / Big Shed
            if (building.GetIndoors() is Shed || building.buildingType.Value?.Contains("Shed", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (building.GetIndoors() is Shed shed)
                {
                    int objCount = shed.Objects.Pairs.Count();
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.shed.objects-count", new { count = objCount }).ToString(),
                        Color.DarkSlateGray
                    ));
                }
                return info;
            }

            // 11. Greenhouse
            if (building.buildingType.Value?.Equals("Greenhouse", StringComparison.OrdinalIgnoreCase) == true)
            {
                bool isRepaired = Game1.player.hasCompletedCommunityCenter()
                               || Game1.MasterPlayer.mailReceived.Contains("jojaPantry")
                               || Game1.MasterPlayer.mailReceived.Contains("ccPantry");
                info.Lines.Add(new TooltipLine(
                    isRepaired ? ModEntry.I18n.Get("hover.greenhouse.repaired").ToString() : ModEntry.I18n.Get("hover.greenhouse.needs-repair").ToString(),
                    isRepaired ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));
                return info;
            }

            // 12. FarmHouse / Cabin
            if (building.buildingType.Value?.Equals("FarmHouse", StringComparison.OrdinalIgnoreCase) == true || building.buildingType.Value?.Equals("Cabin", StringComparison.OrdinalIgnoreCase) == true)
            {
                int lvl = Game1.player.HouseUpgradeLevel;
                string lvlText = lvl switch
                {
                    0 => ModEntry.I18n.Get("hover.farmhouse.level-0").ToString(),
                    1 => ModEntry.I18n.Get("hover.farmhouse.level-1").ToString(),
                    2 => ModEntry.I18n.Get("hover.farmhouse.level-2").ToString(),
                    3 => ModEntry.I18n.Get("hover.farmhouse.level-3").ToString(),
                    _ => ModEntry.I18n.Get("hover.farmhouse.level-default", new { level = lvl }).ToString()
                };
                info.Lines.Add(new TooltipLine(lvlText, new Color(180, 100, 0)));
                return info;
            }

            // 13. Obelisks & Special Towers
            if (building.buildingType.Value?.Contains("Obelisk", StringComparison.OrdinalIgnoreCase) == true)
            {
                string bType = building.buildingType.Value.ToLower();
                string dest = bType switch
                {
                    var s when s.Contains("earth") => ModEntry.I18n.Get("hover.obelisk.destination-mountains").ToString(),
                    var s when s.Contains("water") => ModEntry.I18n.Get("hover.obelisk.destination-beach").ToString(),
                    var s when s.Contains("desert") => ModEntry.I18n.Get("hover.obelisk.destination-desert").ToString(),
                    var s when s.Contains("island") => ModEntry.I18n.Get("hover.obelisk.destination-island").ToString(),
                    _ => ModEntry.I18n.Get("hover.obelisk.warp-destination").ToString()
                };
                info.Lines.Add(new TooltipLine(dest, new Color(180, 50, 180)));
                return info;
            }

            if (building.buildingType.Value?.Equals("Gold Clock", StringComparison.OrdinalIgnoreCase) == true)
            {
                info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.goldclock.effect").ToString(), new Color(180, 100, 0)));
                return info;
            }

            if (building.buildingType.Value?.Equals("Well", StringComparison.OrdinalIgnoreCase) == true)
            {
                info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.well.water-source").ToString(), new Color(20, 110, 220)));
                return info;
            }

            if (info.Lines.Count == 0)
                return null;

            return info;
        }

        private static MachineInfo GetCaskInfo(Cask cask)
        {
            var info = new MachineInfo
            {
                MachineName = cask.DisplayName,
                IsCask = true
            };

            var held = cask.heldObject.Value;
            if (held == null)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.cask.empty");
                return info;
            }

            info.HeldItemName = held.DisplayName;
            info.HeldItemStack = held.Stack;
            info.HeldItemQuality = held.Quality;
            info.CaskCurrentQuality = held.Quality;

            var itemData = ItemRegistry.GetData(held.QualifiedItemId);
            if (itemData != null)
            {
                try
                {
                    info.HeldItemTexture = itemData.GetTexture();
                    info.HeldItemSourceRect = itemData.GetSourceRect();
                }
                catch
                {
                    // Ignore texture failures
                }
            }

            if (cask.readyForHarvest.Value || held.Quality >= 4 || cask.daysToMature.Value <= 0)
            {
                info.IsReadyToHarvest = true;
                info.IsProcessing = false;
                return info;
            }

            info.IsProcessing = true;
            float rawDaysRemaining = cask.daysToMature.Value;
            float rate = Math.Max(0.1f, cask.agingRate.Value);

            // In SDV Cask aging thresholds (raw units):
            // Normal (56..42) -> Silver at 42
            // Silver (42..28) -> Gold at 28
            // Gold (28..0) -> Iridium at 0
            if (held.Quality == 0) // Normal -> Silver
            {
                info.CaskNextQuality = 1; // Silver
                float days = Math.Max(0f, rawDaysRemaining - 42f) / rate;
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(days - 0.001f));
            }
            else if (held.Quality == 1) // Silver -> Gold
            {
                info.CaskNextQuality = 2; // Gold
                float days = Math.Max(0f, rawDaysRemaining - 28f) / rate;
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(days - 0.001f));
            }
            else // Gold (2) -> Iridium
            {
                info.CaskNextQuality = 4; // Iridium
                float days = Math.Max(0f, rawDaysRemaining) / rate;
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(days - 0.001f));
            }

            info.CaskDaysToIridium = Math.Max(1, (int)Math.Ceiling((Math.Max(0f, rawDaysRemaining) / rate) - 0.001f));

            return info;
        }

        private static MachineInfo GetCrabPotInfo(CrabPot crabPot)
        {
            var info = new MachineInfo
            {
                MachineName = crabPot.DisplayName,
                IsCrabPot = true
            };

            var held = crabPot.heldObject.Value;
            if (held != null)
            {
                info.HeldItemName = held.DisplayName;
                info.HeldItemStack = held.Stack;
                info.HeldItemQuality = held.Quality;
                info.IsReadyToHarvest = true;

                var itemData = ItemRegistry.GetData(held.QualifiedItemId);
                if (itemData != null)
                {
                    try
                    {
                        info.HeldItemTexture = itemData.GetTexture();
                        info.HeldItemSourceRect = itemData.GetSourceRect();
                    }
                    catch
                    {
                        // Ignore texture failures
                    }
                }
                return info;
            }

            var bait = crabPot.bait.Value;
            if (bait != null)
            {
                info.CrabPotHasBait = true;
                info.CrabPotBaitName = bait.DisplayName;
                info.IsProcessing = true;
            }
            else
            {
                info.CrabPotHasBait = false;
                info.IsProcessing = false;
            }

            return info;
        }

        public static void FormatMachineTime(int minutesRemaining, out string timeRemaining, out string finishTime)
        {
            int currentDayTime = Game1.timeOfDay;
            int curHours = currentDayTime / 100;
            int curMins = currentDayTime % 100;

            // In Stardew Valley, 6am = 600, 2am = 2600. Total 20 game hours (1200 mins) during the day.
            int minsPassedToday = (curHours - 6) * 60 + curMins;
            int minsLeftToday = Math.Max(0, (20 * 60) - minsPassedToday);

            if (minutesRemaining <= minsLeftToday)
            {
                // Completes today
                int hours = minutesRemaining / 60;
                int mins = minutesRemaining % 60;

                if (hours > 0 && mins > 0)
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.hours-minutes", new { hours, minutes = mins });
                }
                else if (hours > 0)
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.hours", new { hours });
                }
                else
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.minutes", new { minutes = Math.Max(1, mins) });
                }

                int targetTimeInt = Utility.ModifyTime(currentDayTime, minutesRemaining);
                string timeString = Game1.getTimeOfDayString(targetTimeInt);
                finishTime = ModEntry.I18n.Get("hover.time.today-at", new { time = timeString });
            }
            else
            {
                // Completes in future day
                int remAfterToday = minutesRemaining - minsLeftToday;
                // Full days are 1600 minutes in SDV's machine countdown logic (1200 day + 400 night)
                int daysAhead = 1 + (remAfterToday / 1600);
                int minsInFinalDay = remAfterToday % 1600;

                if (daysAhead == 1)
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.tomorrow");
                }
                else
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.days", new { days = daysAhead });
                }

                int targetTimeInt = minsInFinalDay <= 0 ? 600 : Utility.ModifyTime(600, minsInFinalDay);
                string timeString = Game1.getTimeOfDayString(targetTimeInt);

                if (daysAhead == 1)
                {
                    finishTime = ModEntry.I18n.Get("hover.time.tomorrow-at", new { time = timeString });
                }
                else
                {
                    finishTime = ModEntry.I18n.Get("hover.time.in-days-at", new { days = daysAhead, time = timeString });
                }
            }
        }
    }
}
