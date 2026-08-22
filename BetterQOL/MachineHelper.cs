using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace BetterQOL
{
    public class MachineInfo
    {
        public string MachineName { get; set; } = string.Empty;
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

        // Special machine details
        public bool IsCask { get; set; }
        public int CaskCurrentQuality { get; set; }
        public int CaskDaysToNextQuality { get; set; }
        public int CaskDaysToIridium { get; set; }
        public int CaskNextQuality { get; set; }

        public bool IsCrabPot { get; set; }
        public bool CrabPotHasBait { get; set; }
        public string? CrabPotBaitName { get; set; }
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

            // Check if object is a machine (heldObject != null, minutesUntilReady > 0, readyForHarvest, or is a known machine)
            bool isMachine = obj.heldObject.Value != null
                          || obj.MinutesUntilReady > 0
                          || obj.readyForHarvest.Value
                          || obj.GetMachineData() != null
                          || obj.bigCraftable.Value;

            if (!isMachine)
                return null;

            var info = new MachineInfo
            {
                MachineName = obj.DisplayName
            };

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

            // If machine has no held item and minutes is 0, return null so we don't spam tooltips on empty chests/decorations
            if (held == null && obj.MinutesUntilReady <= 0 && !obj.readyForHarvest.Value)
            {
                return null;
            }

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
                return info;

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
