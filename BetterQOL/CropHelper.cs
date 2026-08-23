using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;

namespace BetterQOL
{
    public class CropInfo
    {
        public string CropName { get; set; } = string.Empty;
        public Texture2D? IconTexture { get; set; }
        public Rectangle? IconSourceRect { get; set; }
        public bool IsDead { get; set; }
        public bool IsReadyToHarvest { get; set; }
        public int DaysRemaining { get; set; }
        public int CurrentStage { get; set; }
        public int TotalStages { get; set; }
        public bool IsRegrowable { get; set; }
        public int RegrowDays { get; set; }
        public bool IsWatered { get; set; }
        public string? FertilizerName { get; set; }
        public List<string> FertilizerNames { get; set; } = new();
        public bool IsFertilized => FertilizerNames.Count > 0;
        public bool IsPaddyCrop { get; set; }
        public bool IsPaddyWatered { get; set; }
        public bool IsHoeDirtOnly { get; set; }
    }

    public static class CropHelper
    {
        public static List<string> ParseFertilizerNames(string? fertilizerRaw)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(fertilizerRaw))
                return results;

            // Ultimate Fertilizer and multiple fertilizer mods separate IDs by '|'
            var tokens = fertilizerRaw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                return results;

            // Count occurrences to support stacking mode (e.g. Speed-Gro x3)
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var distinctIds = new List<string>();
            foreach (var token in tokens)
            {
                string id = token;
                if (!counts.ContainsKey(id))
                {
                    counts[id] = 0;
                    distinctIds.Add(id);
                }
                counts[id]++;
            }

            foreach (var id in distinctIds)
            {
                int count = counts[id];
                var fertData = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                string name = fertData?.DisplayName ?? id;
                if (count > 1)
                {
                    results.Add($"{name} x{count}");
                }
                else
                {
                    results.Add(name);
                }
            }

            return results;
        }

        public static CropInfo? GetCropInfo(HoeDirt hoeDirt)
        {
            if (hoeDirt == null)
                return null;

            var info = new CropInfo();

            // 1. Water status
            info.IsWatered = hoeDirt.state.Value == HoeDirt.watered;

            // 2. Fertilizer status
            string? fertilizerRaw = hoeDirt.fertilizer.Value;
            info.FertilizerNames = ParseFertilizerNames(fertilizerRaw);
            if (info.FertilizerNames.Count > 0)
            {
                info.FertilizerName = string.Join(", ", info.FertilizerNames);
            }

            Crop? crop = hoeDirt.crop;
            if (crop == null)
            {
                // Bare tilled soil or empty garden pot
                info.IsHoeDirtOnly = true;
                info.CropName = ModEntry.I18n.Get("hover.dirt.tilled");
                return info;
            }

            info.IsPaddyCrop = crop.isPaddyCrop();
            if (info.IsPaddyCrop)
            {
                info.IsPaddyWatered = hoeDirt.paddyWaterCheck();
                if (info.IsPaddyWatered)
                {
                    info.IsWatered = true;
                }
            }

            // 3. Dead crop check
            if (crop.dead.Value)
            {
                info.IsDead = true;
                info.CropName = ModEntry.I18n.Get("hover.crop.dead");
                return info;
            }

            // 4. Crop identity & Icon
            string harvestId = crop.indexOfHarvest.Value;
            ParsedItemData? harvestData = null;
            if (!string.IsNullOrEmpty(harvestId))
            {
                harvestData = ItemRegistry.GetData(harvestId) ?? ItemRegistry.GetData($"(O){harvestId}");
            }

            if (harvestData != null)
            {
                info.CropName = harvestData.DisplayName;
                try
                {
                    info.IconTexture = harvestData.GetTexture();
                    info.IconSourceRect = harvestData.GetSourceRect();
                }
                catch
                {
                    // Fallback if texture cannot be loaded
                }
            }
            else
            {
                info.CropName = ModEntry.I18n.Get("hover.crop.generic");
            }

            // 5. Growth stages & Days remaining
            int phaseCount = crop.phaseDays.Count;
            info.TotalStages = Math.Max(1, phaseCount > 0 ? phaseCount - 1 : 1);
            info.CurrentStage = Math.Min(crop.currentPhase.Value + 1, info.TotalStages);

            int regrow = crop.GetData()?.RegrowDays ?? -1;
            info.IsRegrowable = regrow > 0;
            info.RegrowDays = Math.Max(0, regrow);

            if (crop.fullyGrown.Value)
            {
                if (crop.dayOfCurrentPhase.Value <= 0)
                {
                    info.IsReadyToHarvest = true;
                    info.DaysRemaining = 0;
                }
                else
                {
                    info.IsReadyToHarvest = false;
                    info.DaysRemaining = Math.Max(0, crop.dayOfCurrentPhase.Value);
                }
            }
            else
            {
                if (crop.currentPhase.Value >= info.TotalStages)
                {
                    info.IsReadyToHarvest = true;
                    info.DaysRemaining = 0;
                }
                else if (crop.currentPhase.Value < crop.phaseDays.Count)
                {
                    int currentPhaseRemaining = Math.Max(0, crop.phaseDays[crop.currentPhase.Value] - crop.dayOfCurrentPhase.Value);
                    int remainingPhasesSum = 0;

                    for (int i = crop.currentPhase.Value + 1; i < crop.phaseDays.Count - 1; i++)
                    {
                        remainingPhasesSum += crop.phaseDays[i];
                    }

                    info.DaysRemaining = Math.Max(0, currentPhaseRemaining + remainingPhasesSum);
                    info.IsReadyToHarvest = info.DaysRemaining <= 0;
                }
            }

            return info;
        }
    }
}
