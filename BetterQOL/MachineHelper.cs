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
                else
                {
                    if (fishPond.daysSinceSpawn.Value >= 0 && fishPond.FishCount < fishPond.maxOccupants.Value)
                    {
                        int spawnRate = fishPond.GetFishPondData()?.SpawnTime ?? 3;
                        int daysLeft = Math.Max(0, spawnRate - fishPond.daysSinceSpawn.Value);
                        info.Lines.Add(new TooltipLine(
                            ModEntry.I18n.Get("hover.fishpond.spawning-in", new { days = daysLeft }),
                            new Color(180, 100, 0)
                        ));
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
                int hay = Game1.getFarm()?.piecesOfHay.Value ?? 0;
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

            // 6. Generic Animals Housing / Occupants
            if (building.maxOccupants.Value > 0)
            {
                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.building.occupants", new { current = building.currentOccupants.Value, max = building.maxOccupants.Value }),
                    new Color(20, 110, 220)
                ));
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
            float daysRemaining = cask.daysToMature.Value;
            float totalAgingDays = 56f / Math.Max(0.1f, cask.agingRate.Value);

            // In SDV Cask math:
            // Normal -> Silver: 25% of totalAgingDays
            // Silver -> Gold: 25% of totalAgingDays
            // Gold -> Iridium: 50% of totalAgingDays
            float silverThreshold = totalAgingDays * 0.75f;
            float goldThreshold = totalAgingDays * 0.50f;

            if (held.Quality == 0) // Normal
            {
                info.CaskNextQuality = 1; // Silver
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(daysRemaining - silverThreshold));
            }
            else if (held.Quality == 1) // Silver
            {
                info.CaskNextQuality = 2; // Gold
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(daysRemaining - goldThreshold));
            }
            else // Gold (2)
            {
                info.CaskNextQuality = 4; // Iridium
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(daysRemaining));
            }

            info.CaskDaysToIridium = Math.Max(1, (int)Math.Ceiling(daysRemaining));

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
