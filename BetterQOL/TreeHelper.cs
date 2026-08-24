using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BetterQOL
{
    /// <summary>
    /// Everything the hover tooltip shows about ONE tree or bush, packed into a
    /// plain data object. Three very different plants share this record - planted
    /// fruit trees, wild trees, and bushes - so the Is* flags tell the renderer
    /// which "mode" filled it in; fields belonging to other modes stay defaulted.
    /// </summary>
    public class TreeInfo
    {
        /// <summary>Display name, e.g. "Oak Tree" or the fruit tree's produce name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Small grey subtitle under the name ("Fruit Tree", "Wild Tree", "Bush"...).</summary>
        public string? Subtitle { get; set; }
        /// <summary>Texture atlas holding the icon image (null = couldn't load one).</summary>
        public Texture2D? IconTexture { get; set; }
        /// <summary>Pick-region inside that atlas locating this specific icon.</summary>
        public Rectangle? IconSourceRect { get; set; }

        /// <summary>True when this record describes a planted fruit tree.</summary>
        public bool IsFruitTree { get; set; }
        /// <summary>Fully grown (fruit trees) / final stage (wild trees) / producing (bushes).</summary>
        public bool IsMature { get; set; }
        /// <summary>Nights left until a still-growing plant reaches maturity.</summary>
        public int DaysUntilMature { get; set; }
        /// <summary>How many fruits are currently hanging on a mature fruit tree.</summary>
        public int FruitsOnTree { get; set; }
        /// <summary>Fruit quality code: 0 normal, 1 silver, 2 gold, 4 iridium.</summary>
        public int FruitQuality { get; set; }
        /// <summary>True when the fruit is in season here (fruit trees only produce in-season).</summary>
        public bool IsInSeason { get; set; }
        /// <summary>True while the tree is charred from a lightning strike.</summary>
        public bool StruckByLightning { get; set; }
        /// <summary>Nights until a lightning-struck tree recovers on its own.</summary>
        public int LightningDaysRemaining { get; set; }
        /// <summary>True when tree fertilizer was applied (speeds growth).</summary>
        public bool IsFertilized { get; set; }

        /// <summary>True when this record describes a wild (naturally spawned) tree.</summary>
        public bool IsWildTree { get; set; }
        /// <summary>Growth stage 0-5 (0 = seed/sprout, 5 = full-grown Tree.treeStage).</summary>
        public int GrowthStage { get; set; }
        /// <summary>True when moss grows on the trunk (from green-rain weather).</summary>
        public bool HasMoss { get; set; }
        /// <summary>True when a Tapper is attached collecting sap products.</summary>
        public bool IsTapped { get; set; }

        /// <summary>True when this record describes a bush (berry bush or tea bush).</summary>
        public bool IsBush { get; set; }
        /// <summary>True specifically for a plantable Green Tea bush (Stardew 1.6).</summary>
        public bool IsTeaBush { get; set; }
        /// <summary>True while the bush is blooming, i.e. harvestable right now.</summary>
        public bool IsInBloom { get; set; }
    }

    /// <summary>
    /// Static helper (no instances - call methods directly on the class name, e.g.
    /// TreeHelper.GetFruitTreeInfo(...)). Reads FruitTree / Tree / Bush objects and
    /// packs their state into a TreeInfo for the hover overlay. Strictly READ-ONLY:
    /// it never modifies the plant it inspects.
    /// </summary>
    public static class TreeHelper
    {
        /// <summary>
        /// Describes a planted FRUIT TREE (anything grown from a sapling: cherry,
        /// apple, pomegranate...). Fruit trees take 28 nights to mature, then drop
        /// one fruit per night while IN SEASON.
        /// </summary>
        /// <param name="fruitTree">The FruitTree terrain feature under the cursor.</param>
        /// <returns>A populated TreeInfo, or null when the input was null.</returns>
        public static TreeInfo? GetFruitTreeInfo(FruitTree fruitTree)
        {
            if (fruitTree == null)
                return null;

            // Fetch this tree's row from the game's Data/FruitTrees asset (display
            // name, fruit list...). "?." yields null instead of crashing on missing data.
            var data = fruitTree.GetData();
            // OBJECT INITIALIZER syntax: create the object and assign several listed
            // properties in one statement, separated by commas inside the braces.
            var info = new TreeInfo
            {
                IsFruitTree = true,
                // "??" supplies a fallback when the left side is null: unnamed/custom
                // trees fall back to a generic translated label.
                Name = data?.DisplayName ?? ModEntry.I18n.Get("hover.fruit-tree.generic"),
                Subtitle = ModEntry.I18n.Get("hover.type.fruit-tree")
            };

            // Check Fruit Tree Fertilizer (Ultimate Fertilizer mod support)
            // modData is a moddable string dictionary stapled onto any game object.
            // Mods store custom flags under "AuthorID/KeyName" keys; TryGetValue looks
            // up our key safely and passes the found string out via "out var fertVal".
            if (fruitTree.modData != null && fruitTree.modData.TryGetValue("fox_white25.ultimate_fertilizer/TreeFertilized", out var fertVal) && fertVal == "true")
            {
                info.IsFertilized = true;
            }

            // Fruit Icon
            // data.Fruit lists everything the mature tree drops; item [0] doubles as
            // the tooltip icon so players see what to expect.
            if (data?.Fruit != null && data.Fruit.Count > 0)
            {
                string fruitItemId = data.Fruit[0].ItemId;
                // Double lookup: try the id as-is, then retry with an "(O)" prefix
                // (the object-item category marker) for ids stored without it.
                var itemData = ItemRegistry.GetData(fruitItemId) ?? ItemRegistry.GetData($"(O){fruitItemId}");
                if (itemData != null)
                {
                    try
                    {
                        // Which texture atlas to draw from + which rectangle inside it:
                        // together these are all the game's SpriteBatch needs to stamp
                        // this item's picture onto the tooltip.
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
            // While growing, daysUntilMature counts down one per night. Report the
            // countdown and RETURN EARLY - an immature tree has no fruit to describe.
            if (fruitTree.daysUntilMature.Value > 0)
            {
                info.IsMature = false;
                info.DaysUntilMature = fruitTree.daysUntilMature.Value;
                return info;
            }

            info.IsMature = true;
            // fruit is the live list of pickable fruits currently on the tree.
            info.FruitsOnTree = fruitTree.fruit.Count;
            // False outside the fruit's season - no new growth happens then.
            info.IsInSeason = fruitTree.IsInSeasonHere();

            // Quality
            // Highest quality among current fruit: 0 normal, 1 silver, 2 gold, 4 iridium.
            info.FruitQuality = fruitTree.GetQuality();

            // Lightning
            // A struck tree turns charcoal-black and stops producing until this
            // countdown (in nights) reaches zero.
            if (fruitTree.struckByLightningCountdown.Value > 0)
            {
                info.StruckByLightning = true;
                info.LightningDaysRemaining = fruitTree.struckByLightningCountdown.Value;
            }

            return info;
        }

        /// <summary>
        /// Describes a WILD tree - the naturally spawning oaks, maples, pines,
        /// mushroom/mahogany/mystic trees and green-rain varieties. Wild trees don't
        /// fruit; the interesting facts are growth stage, moss, and tapper status.
        /// </summary>
        /// <param name="tree">The wild Tree terrain feature under the cursor.</param>
        /// <returns>A populated TreeInfo flagged IsWildTree, or null for null input.</returns>
        public static TreeInfo? GetTreeInfo(Tree tree)
        {
            if (tree == null)
                return null;

            // treeType stores a NUMBER-as-text id ("1" oak, "2" maple, "3" pine...).
            // The switch-expression helper at the bottom turns it into a readable name.
            string treeName = GetFallbackTreeName(tree.treeType.Value);

            var info = new TreeInfo
            {
                IsWildTree = true,
                Name = treeName,
                Subtitle = ModEntry.I18n.Get("hover.type.tree"),
                // NetField values are always read through their ".Value" wrapper.
                // Wild growth stages run 0 (seed/sprout) up to 5 (full tree).
                GrowthStage = tree.growthStage.Value,
                // Tree.treeStage is the game's constant for the FINAL stage (5), so
                // "stage >= 5" means fully grown: choppable, tappable, drops seeds.
                IsMature = tree.growthStage.Value >= Tree.treeStage,
                // Moss grew during green-rain weather; it lingers until cleared.
                HasMoss = tree.hasMoss.Value,
                // True once a Tapper has been attached (makes maple syrup, oak resin...).
                IsTapped = tree.tapped.Value,
                // True while Tree Fertilizer keeps growth instant each night.
                IsFertilized = tree.fertilized.Value
            };

            return info;
        }

        /// <summary>
        /// Describes a BUSH: either the wild berry bushes around town (harvestable
        /// only while blooming in their season) or the plantable Green Tea bush,
        /// which needs 20 days of growth before producing anything.
        /// </summary>
        /// <param name="bush">The Bush terrain feature under the cursor.</param>
        /// <returns>A populated TreeInfo flagged IsBush, or null for null input.</returns>
        public static TreeInfo? GetBushInfo(Bush bush)
        {
            if (bush == null)
                return null;

            var info = new TreeInfo
            {
                IsBush = true
            };

            // Bushes come in several sizes; size acts as their subtype id. This value
            // is the game's constant meaning "green tea bush" specifically.
            if (bush.size.Value == Bush.greenTeaBush)
            {
                info.IsTeaBush = true;
                info.Name = ModEntry.I18n.Get("hover.bush.tea");
                info.Subtitle = ModEntry.I18n.Get("hover.type.tea-bush");

                // getAge() reports days since planting; tea bushes mature at 20 days.
                int age = Math.Max(0, bush.getAge());
                if (age < 20)
                {
                    // Still growing: show how many days remain (at least 1).
                    info.IsMature = false;
                    info.DaysUntilMature = Math.Max(1, 20 - age);
                }
                else
                {
                    // Mature: inBloom() tells whether tea leaves are pickable today.
                    info.IsMature = true;
                    info.DaysUntilMature = 0;
                    info.IsInBloom = bush.inBloom();
                }
                return info;
            }
            else
            {
                // Any other bush is treated as a seasonal berry bush. Berry bushes
                // are born mature - the only question is whether they're harvestable now.
                info.Name = ModEntry.I18n.Get("hover.bush.berry");
                info.Subtitle = ModEntry.I18n.Get("hover.type.berry-bush");
                info.IsMature = true;
                info.DaysUntilMature = 0;
                info.IsInBloom = bush.inBloom();
                return info;
            }
        }

        /// <summary>
        /// Maps a wild tree's numeric type id (stored as TEXT in Tree.treeType) to a
        /// display name. Demonstrates a SWITCH EXPRESSION: compact, expression-style
        /// branching where each "pattern => result" arm returns a value.
        /// </summary>
        /// <param name="treeType">Raw type id, e.g. "1".</param>
        /// <returns>Translated tree name, or a generic label for unknown ids.</returns>
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
                // "or" merges several patterns into one arm (green-rain tree types).
                "10" or "11" or "12" => ModEntry.I18n.Get("hover.tree.green-rain"),
                // "_" is the discard pattern: matches ANYTHING else (the default case).
                _ => ModEntry.I18n.Get("hover.tree.generic")
            };
        }
    }
}
