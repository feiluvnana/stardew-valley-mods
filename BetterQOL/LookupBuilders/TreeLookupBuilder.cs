using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using SObject = StardewValley.Object;

namespace BetterQOL
{
    /// <summary>
    /// Lookup builder for wild trees, fruit trees, tea bushes, giant crops, and resource clumps.
    /// </summary>
    public static partial class LookupDataManager
    {
        #region 5. Tree & Bush Lookup

        public static LookupSubject BuildTreeSubject(Tree tree)
        {
            TreeInfo treeInfo = TreeHelper.GetTreeInfo(tree);
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = (treeInfo?.Name ?? ModEntry.I18n.Get("hover.tree.generic").ToString()),
                Subtitle = ModEntry.I18n.Get("hover.type.tree").ToString()
            };
            if (treeInfo?.IconTexture != null)
            {
                lookupSubject.MainIcon = treeInfo.IconTexture;
                lookupSubject.MainIconSourceRect = treeInfo.IconSourceRect;
            }
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (treeInfo != null)
            {
                if (!treeInfo.IsMature)
                {
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.growth-stage"), ModEntry.I18n.Get("hover.tree.stage", new { stage = treeInfo.GrowthStage, max = 5 }), new Color(180, 100, 0)));
                    if (tree.fertilized.Value)
                    {
                        lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.fertilized"), ModEntry.I18n.Get("lookup.tree.fertilized-status").ToString(), new Color(0, 140, 0)));
                    }
                }
                else
                {
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.growth-stage"), ModEntry.I18n.Get("hover.tree.fully-grown").ToString(), new Color(0, 140, 0)));
                    if (treeInfo.HasMoss)
                    {
                        lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.moss"), ModEntry.I18n.Get("hover.tree.covered-in-moss").ToString(), new Color(46, 125, 50)));
                    }
                    if (tree.hasSeed.Value)
                    {
                        lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.seed"), ModEntry.I18n.Get("hover.tree.has-seed").ToString(), new Color(180, 100, 0)));
                    }
                }
                string value = ModEntry.I18n.Get("lookup.tree.no-tapper").ToString();
                if (treeInfo.IsTapped && tree.Location != null && tree.Location.Objects.TryGetValue(tree.Tile, out var val) && val?.heldObject.Value != null)
                {
                    SObject value2 = val.heldObject.Value;
                    if (val.readyForHarvest.Value || val.MinutesUntilReady <= 0)
                    {
                        value = ModEntry.I18n.Get("lookup.tree.tapper-ready", new { item = value2.DisplayName }).ToString();
                    }
                    else
                    {
                        int num = val.MinutesUntilReady / 60;
                        int num2 = num / 24;
                        int value3 = num % 24;
                        string time = (num2 > 0) ? $"{num2}d {value3}h" : $"{num}h";
                        value = ModEntry.I18n.Get("lookup.tree.tapper-producing", new { item = value2.DisplayName, time = time }).ToString();
                    }
                }
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.tapper"), value, treeInfo.IsTapped ? new Color(20, 110, 220) : Color.DarkSlateGray));
                string value4 = tree.treeType.Value;
                string text = value4 switch
                {
                    "1" => ModEntry.I18n.Get("lookup.tree.oak-resin").ToString(),
                    "2" => ModEntry.I18n.Get("lookup.tree.maple-syrup").ToString(),
                    "3" => ModEntry.I18n.Get("lookup.tree.pine-tar").ToString(),
                    "8" => ModEntry.I18n.Get("lookup.tree.sap").ToString(),
                    "7" => ModEntry.I18n.Get("lookup.tree.mushroom").ToString(),
                    "mysticTree" => ModEntry.I18n.Get("lookup.tree.mystic-syrup").ToString(),
                    _ => ModEntry.I18n.Get("lookup.tree.standard-wood").ToString()
                };
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.products"), text, new Color(180, 100, 0)));
            }
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        public static LookupSubject BuildFruitTreeSubject(FruitTree fruitTree)
        {
            TreeInfo fruitTreeInfo = TreeHelper.GetFruitTreeInfo(fruitTree);
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = (fruitTreeInfo?.Name ?? ModEntry.I18n.Get("hover.fruit-tree.generic").ToString()),
                Subtitle = ModEntry.I18n.Get("hover.type.fruit-tree").ToString()
            };
            if (fruitTreeInfo?.IconTexture != null)
            {
                lookupSubject.MainIcon = fruitTreeInfo.IconTexture;
                lookupSubject.MainIconSourceRect = fruitTreeInfo.IconSourceRect;
            }
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (fruitTreeInfo != null)
            {
                if (!fruitTreeInfo.IsMature)
                {
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.maturation"), ModEntry.I18n.Get("hover.fruit-tree.maturing", new { days = fruitTreeInfo.DaysUntilMature }), new Color(180, 100, 0)));
                    if (fruitTreeInfo.IsFertilized)
                    {
                        lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.fertilized"), ModEntry.I18n.Get("lookup.tree.fertilized-status").ToString(), new Color(0, 140, 0)));
                    }
                }
                else
                {
                    int num = (fruitTree.daysUntilMature.Value <= 0) ? Math.Abs(fruitTree.daysUntilMature.Value) : 0;
                    string value = (num >= 84) ? ModEntry.I18n.Get("lookup.common.iridium-quality").ToString() : (num >= 56) ? ModEntry.I18n.Get("lookup.common.gold-quality").ToString() : (num >= 28) ? ModEntry.I18n.Get("lookup.common.silver-quality").ToString() : ModEntry.I18n.Get("lookup.common.normal-quality").ToString();
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fruit-tree.fruit-quality"), value, new Color(180, 50, 180)));
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fruit-tree.fruit-count"), ModEntry.I18n.Get("lookup.fruit-tree.fruits-ready", new { count = fruitTreeInfo.FruitsOnTree }).ToString(), fruitTreeInfo.FruitsOnTree > 0 ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.harvest-seasons"), fruitTreeInfo.IsInSeason ? ModEntry.I18n.Get("hover.fruit-tree.in-season") : ModEntry.I18n.Get("hover.fruit-tree.out-of-season"), fruitTreeInfo.IsInSeason ? new Color(20, 110, 220) : Color.DarkSlateGray));
                }
                if (fruitTree.Location != null)
                {
                    bool flag = false;
                    Vector2 tile = fruitTree.Tile;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            if (i != 0 || j != 0)
                            {
                                Vector2 checkTile = new Vector2(tile.X + i, tile.Y + j);
                                if (fruitTree.Location.Objects.ContainsKey(checkTile) || (fruitTree.Location.terrainFeatures.TryGetValue(checkTile, out var tf) && !(tf is HoeDirt)))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (flag) break;
                    }
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fruit-tree.surroundings"), flag ? ModEntry.I18n.Get("lookup.fruit-tree.surroundings-blocked").ToString() : ModEntry.I18n.Get("lookup.fruit-tree.surroundings-clear").ToString(), flag ? new Color(200, 60, 20) : new Color(0, 140, 0)));
                }
            }
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        public static LookupSubject BuildBushSubject(Bush bush)
        {
            TreeInfo bushInfo = TreeHelper.GetBushInfo(bush);
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = (bushInfo?.Name ?? ModEntry.I18n.Get("hover.bush.generic").ToString()),
                Subtitle = ModEntry.I18n.Get("hover.bush.generic").ToString()
            };
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (bushInfo != null)
            {
                if (bushInfo.IsTeaBush && !bushInfo.IsMature)
                {
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.maturation"), ModEntry.I18n.Get("hover.bush.tea-maturing", new { days = bushInfo.DaysUntilMature }), new Color(180, 100, 0)));
                }
                else if (bushInfo.IsInBloom)
                {
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.tapper"), ModEntry.I18n.Get("hover.bush.ready-to-harvest"), new Color(0, 140, 0)));
                }
            }
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        public static LookupSubject BuildResourceClumpSubject(ResourceClump clump)
        {
            int value = clump.parentSheetIndex.Value;
            string text = value switch
            {
                600 => ModEntry.I18n.Get("lookup.clump.large-stump").ToString(),
                602 => ModEntry.I18n.Get("lookup.clump.hollow-log").ToString(),
                672 => ModEntry.I18n.Get("lookup.clump.meteorite").ToString(),
                752 or 754 or 756 or 758 => ModEntry.I18n.Get("lookup.clump.boulder").ToString(),
                _ => ModEntry.I18n.Get("lookup.clump.resource-clump").ToString()
            };
            string sub = value switch
            {
                600 or 602 => ModEntry.I18n.Get("lookup.clump.hardwood-source").ToString(),
                672 => ModEntry.I18n.Get("lookup.clump.rare-minerals").ToString(),
                _ => ModEntry.I18n.Get("lookup.clump.stone-ore-source").ToString()
            };
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = text,
                Subtitle = sub
            };
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.requirements"));
            string reqTool = value switch
            {
                600 => ModEntry.I18n.Get("lookup.clump.copper-axe").ToString(),
                602 => ModEntry.I18n.Get("lookup.clump.steel-axe").ToString(),
                672 => ModEntry.I18n.Get("lookup.clump.gold-pickaxe").ToString(),
                752 or 754 => ModEntry.I18n.Get("lookup.clump.steel-pickaxe").ToString(),
                _ => ModEntry.I18n.Get("lookup.clump.pickaxe").ToString()
            };
            lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.clump.required-tool"), reqTool, new Color(20, 110, 220)));
            string drops = value switch
            {
                600 => ModEntry.I18n.Get("lookup.clump.hardwood-stump-drops").ToString(),
                602 => ModEntry.I18n.Get("lookup.clump.hardwood-log-drops").ToString(),
                672 => ModEntry.I18n.Get("lookup.clump.meteorite-drops").ToString(),
                _ => ModEntry.I18n.Get("lookup.clump.boulder-drops").ToString()
            };
            lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.clump.yields"), drops, new Color(0, 140, 0)));
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        public static LookupSubject BuildGiantCropSubject(GiantCrop giantCrop)
        {
            var data = ItemRegistry.GetData(giantCrop.Id);
            string cropName = data?.DisplayName ?? ModEntry.I18n.Get("lookup.giant-crop.generic").ToString();
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = ModEntry.I18n.Get("lookup.giant-crop.title", new { name = cropName }).ToString(),
                Subtitle = ModEntry.I18n.Get("lookup.giant-crop.subtitle").ToString(),
                MainIcon = data?.GetTexture(),
                MainIconSourceRect = data?.GetSourceRect()
            };
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.harvest"));
            lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.giant-crop.tool"), ModEntry.I18n.Get("lookup.giant-crop.tool-axe").ToString(), new Color(20, 110, 220)));
            lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.giant-crop.yields"), ModEntry.I18n.Get("lookup.giant-crop.yields-amount", new { name = cropName }).ToString(), new Color(0, 140, 0)));
            lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.giant-crop.special"), ModEntry.I18n.Get("lookup.giant-crop.special-info").ToString(), new Color(180, 50, 180)));
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        #endregion
    }
}
