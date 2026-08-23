using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;

namespace BetterQOL
{
    public class TreeInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public Texture2D? IconTexture { get; set; }
        public Rectangle? IconSourceRect { get; set; }

        public bool IsFruitTree { get; set; }
        public bool IsMature { get; set; }
        public int DaysUntilMature { get; set; }
        public int FruitsOnTree { get; set; }
        public int FruitQuality { get; set; }
        public bool IsInSeason { get; set; }
        public bool StruckByLightning { get; set; }
        public int LightningDaysRemaining { get; set; }
        public bool IsFertilized { get; set; }

        public bool IsWildTree { get; set; }
        public int GrowthStage { get; set; }
        public bool HasMoss { get; set; }
        public bool IsTapped { get; set; }

        public bool IsBush { get; set; }
        public bool IsTeaBush { get; set; }
        public bool IsInBloom { get; set; }
    }

    public static class TreeHelper
    {
        public static TreeInfo? GetFruitTreeInfo(FruitTree fruitTree)
        {
            if (fruitTree == null)
                return null;

            var data = fruitTree.GetData();
            var info = new TreeInfo
            {
                IsFruitTree = true,
                Name = data?.DisplayName ?? ModEntry.I18n.Get("hover.fruit-tree.generic"),
                Subtitle = ModEntry.I18n.Get("hover.type.fruit-tree")
            };

            // Check Fruit Tree Fertilizer (Ultimate Fertilizer mod support)
            if (fruitTree.modData != null && fruitTree.modData.TryGetValue("fox_white25.ultimate_fertilizer/TreeFertilized", out var fertVal) && fertVal == "true")
            {
                info.IsFertilized = true;
            }

            // Fruit Icon
            if (data?.Fruit != null && data.Fruit.Count > 0)
            {
                string fruitItemId = data.Fruit[0].ItemId;
                var itemData = ItemRegistry.GetData(fruitItemId) ?? ItemRegistry.GetData($"(O){fruitItemId}");
                if (itemData != null)
                {
                    try
                    {
                        info.IconTexture = itemData.GetTexture();
                        info.IconSourceRect = itemData.GetSourceRect();
                    }
                    catch
                    {
                        // Ignore texture errors
                    }
                }
            }

            // Maturation
            if (fruitTree.daysUntilMature.Value > 0)
            {
                info.IsMature = false;
                info.DaysUntilMature = fruitTree.daysUntilMature.Value;
                return info;
            }

            info.IsMature = true;
            info.FruitsOnTree = fruitTree.fruit.Count;
            info.IsInSeason = fruitTree.IsInSeasonHere();

            // Quality
            info.FruitQuality = fruitTree.GetQuality();

            // Lightning
            if (fruitTree.struckByLightningCountdown.Value > 0)
            {
                info.StruckByLightning = true;
                info.LightningDaysRemaining = fruitTree.struckByLightningCountdown.Value;
            }

            return info;
        }

        public static TreeInfo? GetTreeInfo(Tree tree)
        {
            if (tree == null)
                return null;

            string treeName = GetFallbackTreeName(tree.treeType.Value);

            var info = new TreeInfo
            {
                IsWildTree = true,
                Name = treeName,
                Subtitle = ModEntry.I18n.Get("hover.type.tree"),
                GrowthStage = tree.growthStage.Value,
                IsMature = tree.growthStage.Value >= Tree.treeStage,
                HasMoss = tree.hasMoss.Value,
                IsTapped = tree.tapped.Value,
                IsFertilized = tree.fertilized.Value
            };

            return info;
        }

        public static TreeInfo? GetBushInfo(Bush bush)
        {
            if (bush == null)
                return null;

            var info = new TreeInfo
            {
                IsBush = true
            };

            if (bush.size.Value == Bush.greenTeaBush)
            {
                info.IsTeaBush = true;
                info.Name = ModEntry.I18n.Get("hover.bush.tea");
                info.Subtitle = ModEntry.I18n.Get("hover.type.tea-bush");

                int age = Math.Max(0, bush.getAge());
                if (age < 20)
                {
                    info.IsMature = false;
                    info.DaysUntilMature = Math.Max(1, 20 - age);
                }
                else
                {
                    info.IsMature = true;
                    info.DaysUntilMature = 0;
                    info.IsInBloom = bush.inBloom();
                }
                return info;
            }
            else
            {
                info.Name = ModEntry.I18n.Get("hover.bush.berry");
                info.Subtitle = ModEntry.I18n.Get("hover.type.berry-bush");
                info.IsMature = true;
                info.DaysUntilMature = 0;
                info.IsInBloom = bush.inBloom();
                return info;
            }
        }

        private static string GetFallbackTreeName(string treeType)
        {
            return treeType switch
            {
                "1" => ModEntry.I18n.Get("hover.tree.oak"),
                "2" => ModEntry.I18n.Get("hover.tree.maple"),
                "3" => ModEntry.I18n.Get("hover.tree.pine"),
                "7" => ModEntry.I18n.Get("hover.tree.mushroom"),
                "8" => ModEntry.I18n.Get("hover.tree.mahogany"),
                "9" => ModEntry.I18n.Get("hover.tree.mystic"),
                "10" or "11" or "12" => ModEntry.I18n.Get("hover.tree.green-rain"),
                _ => ModEntry.I18n.Get("hover.tree.generic")
            };
        }
    }
}
