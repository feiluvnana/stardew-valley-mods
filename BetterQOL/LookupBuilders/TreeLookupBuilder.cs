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
    /// <remarks>
    /// BEGINNER NOTES:
    /// - Each public method turns one world object (wild tree, fruit tree, bush, resource clump,
    ///   giant crop) into a LookupSubject popup card made of LookupSection/LookupField rows.
    /// - Common pattern: ask a helper class (TreeHelper) for pre-computed info, then translate
    ///   that info into localized rows via ModEntry.I18n.Get("key").
    /// - C# SWITCH EXPRESSIONS ("x switch { ... }") map raw game ids onto readable text; the "_"
    ///   arm is the default case, and "or" lets one arm match several values.
    /// </remarks>
    public static partial class LookupDataManager
    {
        #region 5. Tree & Bush Lookup

        /// <summary>
        /// Builds the wild-tree card: growth stage, fertilizer/moss/seed extras, tapper countdown,
        /// and which artisan product a tapper produces on this tree species.
        /// </summary>
        public static LookupSubject BuildTreeSubject(Tree tree)
        {
            // Ask the shared helper for a packaged summary of this tree (name, stage, tapped...).
            TreeInfo treeInfo = TreeHelper.GetTreeInfo(tree);
            LookupSubject lookupSubject = new LookupSubject
            {
                // "??" fallback again: if the helper cannot identify the tree, say generic "Tree".
                Title = (treeInfo?.Name ?? ModEntry.I18n.Get("hover.tree.generic").ToString()),
                Subtitle = ModEntry.I18n.Get("hover.type.tree").ToString()
            };
            // Give the card its picture. The source rect says which piece of the texture ATLAS
            // (one big sheet holding many sprites) to crop out for this particular tree.
            if (treeInfo?.IconTexture != null)
            {
                lookupSubject.MainIcon = treeInfo.IconTexture;
                lookupSubject.MainIconSourceRect = treeInfo.IconSourceRect;
            }
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (treeInfo != null)
            {
                // IMMATURE TREE: show the current growth stage out of 5, plus tree fertilizer
                // status (fertilizer improves the odds of each daily stage-up succeeding).
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
                    // MATURE TREE: show extras that only exist on grown trees - green-rain moss
                    // (blocks tappers until scraped off) and the chance a seed/moss drop appears.
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
                // TAPPER STATUS. The default text assumes no tapper; the long condition below tries
                // to prove otherwise by chaining checks with && and using TryGetValue - the safe
                // dictionary lookup that outputs the found machine ('out var val') and returns
                // false instead of throwing when the tile has nothing on it.
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
                        // Machines count time in game-MINUTES. Convert to whole hours (/60), then
                        // split those into full days (/24) and leftover hours (% = remainder) so the
                        // card can print either "2d 5h" or just "7h".
                        int num = val.MinutesUntilReady / 60;
                        int num2 = num / 24;
                        int value3 = num % 24;
                        string time = (num2 > 0) ? $"{num2}d {value3}h" : $"{num}h";
                        value = ModEntry.I18n.Get("lookup.tree.tapper-producing", new { item = value2.DisplayName, time = time }).ToString();
                    }
                }
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.tapper"), value, treeInfo.IsTapped ? new Color(20, 110, 220) : Color.DarkSlateGray));
                // SWITCH EXPRESSION (C# 8+): a tidy alternative to long if/else-if chains. Each arm
                // maps one tree-type id to the product a tapper makes from that species.
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

        /// <summary>
        /// Builds the fruit-tree card: maturation countdown or fruit quality/count/season,
        /// plus whether nearby objects are blocking its 3x3 growth space.
        /// </summary>
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
                    // FRUIT QUALITY improves the longer a tree stays harvested: after maturity the
                    // game stores NEGATIVE day counts meaning "days since fully grown". The ternary
                    // captures that with Math.Abs (absolute value), then thresholds choose the tier:
                    // 28+ days silver, 56+ gold, 84+ iridium.
                    int num = (fruitTree.daysUntilMature.Value <= 0) ? Math.Abs(fruitTree.daysUntilMature.Value) : 0;
                    string value = (num >= 84) ? ModEntry.I18n.Get("lookup.common.iridium-quality").ToString() : (num >= 56) ? ModEntry.I18n.Get("lookup.common.gold-quality").ToString() : (num >= 28) ? ModEntry.I18n.Get("lookup.common.silver-quality").ToString() : ModEntry.I18n.Get("lookup.common.normal-quality").ToString();
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fruit-tree.fruit-quality"), value, new Color(180, 50, 180)));
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fruit-tree.fruit-count"), ModEntry.I18n.Get("lookup.fruit-tree.fruits-ready", new { count = fruitTreeInfo.FruitsOnTree }).ToString(), fruitTreeInfo.FruitsOnTree > 0 ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.harvest-seasons"), fruitTreeInfo.IsInSeason ? ModEntry.I18n.Get("hover.fruit-tree.in-season") : ModEntry.I18n.Get("hover.fruit-tree.out-of-season"), fruitTreeInfo.IsInSeason ? new Color(20, 110, 220) : Color.DarkSlateGray));
                }
                // GROWTH-SPACE CHECK: a fruit tree refuses to grow while any of the 8 tiles around
                // it is occupied. Two nested "for" loops sweep every offset pair (i,j) from -1..+1 -
                // the standard idiom for visiting a 3x3 grid around a point.
                if (fruitTree.Location != null)
                {
                    bool flag = false;
                    Vector2 tile = fruitTree.Tile;
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            // Skip the centre offset (0,0) - that tile IS the tree itself.
                            if (i != 0 || j != 0)
                            {
                                Vector2 checkTile = new Vector2(tile.X + i, tile.Y + j);
                                // Blocked when an OBJECT sits on the tile, or the terrain feature is
                                // anything other than plain farmland (HoeDirt). TryGetValue fetches
                                // the terrain feature safely so we can test its type.
                                if (fruitTree.Location.Objects.ContainsKey(checkTile) || (fruitTree.Location.terrainFeatures.TryGetValue(checkTile, out var tf) && !(tf is HoeDirt)))
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        // This second break exits the OUTER loop too once we know it's blocked.
                        if (flag) break;
                    }
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fruit-tree.surroundings"), flag ? ModEntry.I18n.Get("lookup.fruit-tree.surroundings-blocked").ToString() : ModEntry.I18n.Get("lookup.fruit-tree.surroundings-clear").ToString(), flag ? new Color(200, 60, 20) : new Color(0, 140, 0)));
                }
            }
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        /// <summary>
        /// Builds the bush card: tea bushes show a maturation countdown, other bushes only
        /// report whether they are currently harvestable ("in bloom").
        /// </summary>
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
                // Tea bushes are planted saplings: while immature, show their maturation countdown...
                if (bushInfo.IsTeaBush && !bushInfo.IsMature)
                {
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.maturation"), ModEntry.I18n.Get("hover.bush.tea-maturing", new { days = bushInfo.DaysUntilMature }), new Color(180, 100, 0)));
                }
                // ...otherwise an ordinary bush only matters while "in bloom" (harvestable).
                else if (bushInfo.IsInBloom)
                {
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.tapper"), ModEntry.I18n.Get("hover.bush.ready-to-harvest"), new Color(0, 140, 0)));
                }
            }
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        /// <summary>
        /// Builds the resource-clump card (stumps, hollow logs, meteorites, boulders): identifies
        /// the clump by sprite id and lists the required tool plus its loot drops.
        /// </summary>
        public static LookupSubject BuildResourceClumpSubject(ResourceClump clump)
        {
            // parentSheetIndex is the clump's sprite id in the object tilesheet; each switch
            // expression below translates that raw number into friendly text. Note "752 or 754"
            // style arms - one case can match several ids (all boulder variants share loot).
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

        /// <summary>
        /// Builds the giant-crop card: resolves the underlying crop item for name/icon, then lists
        /// harvest requirements (axe) and its special behaviour.
        /// </summary>
        public static LookupSubject BuildGiantCropSubject(GiantCrop giantCrop)
        {
            // ItemRegistry.GetData resolves ANY item id (vanilla or mod-added) into its definition.
            // The "?." / "??" pair keeps everything working if the id is somehow invalid, and
            // "MainIcon = data?.GetTexture()" shows null-conditional chaining: if 'data' is null
            // the property simply stays null instead of throwing.
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
