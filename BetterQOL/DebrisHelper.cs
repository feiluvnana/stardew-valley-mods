using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace BetterQOL
{
    /// <summary>
    /// Information model for destructible ground debris (stones, ore nodes, twigs, weeds, resource clumps).
    /// </summary>
    public class DebrisInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string ToolHitsText { get; set; } = string.Empty;
        public Color ToolHitsColor { get; set; } = Game1.textColor;
        public Texture2D? IconTexture { get; set; }
        public Rectangle? IconSourceRect { get; set; }
    }

    /// <summary>
    /// Helper for calculating remaining tool hits/chops to break objects, trees, and resource clumps.
    /// </summary>
    public static class DebrisHelper
    {
        /// <summary>
        /// Gets the upgrade level of the player's best Axe (current tool first, then inventory).
        /// </summary>
        public static int GetBestAxeUpgradeLevel()
        {
            if (Game1.player?.CurrentTool is Axe currentAxe)
                return currentAxe.UpgradeLevel;

            int best = -1;
            if (Game1.player?.Items != null)
            {
                foreach (var item in Game1.player.Items)
                {
                    if (item is Axe axe && axe.UpgradeLevel > best)
                    {
                        best = axe.UpgradeLevel;
                    }
                }
            }
            return best >= 0 ? best : 0;
        }

        /// <summary>
        /// Gets the upgrade level of the player's best Pickaxe (current tool first, then inventory).
        /// </summary>
        public static int GetBestPickaxeUpgradeLevel()
        {
            if (Game1.player?.CurrentTool is Pickaxe currentPick)
                return currentPick.UpgradeLevel;

            int best = -1;
            if (Game1.player?.Items != null)
            {
                foreach (var item in Game1.player.Items)
                {
                    if (item is Pickaxe pick && pick.UpgradeLevel > best)
                    {
                        best = pick.UpgradeLevel;
                    }
                }
            }
            return best >= 0 ? best : 0;
        }

        /// <summary>
        /// Computes remaining chops for a wild tree.
        /// </summary>
        public static (int totalChops, int trunkChops, int stumpChops) GetTreeChopHits(Tree tree)
        {
            int axeLevel = GetBestAxeUpgradeLevel();
            float damage = axeLevel switch
            {
                0 => 1.0f,
                1 => 1.25f,
                2 => 1.67f,
                3 => 2.5f,
                4 => 5.0f,
                _ => axeLevel + 1
            };

            if (tree.growthStage.Value >= 5)
            {
                if (tree.stump.Value)
                {
                    float stumpHealth = tree.health.Value > 0f ? tree.health.Value : 5f;
                    int stumpHits = Math.Max(1, (int)Math.Ceiling(stumpHealth / damage));
                    return (stumpHits, 0, stumpHits);
                }
                else
                {
                    float trunkHealth = tree.health.Value > 0f ? tree.health.Value : 5f;
                    int trunkHits = Math.Max(1, (int)Math.Ceiling(trunkHealth / damage));
                    int stumpHits = Math.Max(1, (int)Math.Ceiling(5f / damage));
                    return (trunkHits + stumpHits, trunkHits, stumpHits);
                }
            }
            else
            {
                return (1, 1, 0);
            }
        }

        /// <summary>
        /// Computes remaining chops for a fruit tree.
        /// </summary>
        public static (int totalChops, int trunkChops, int stumpChops) GetFruitTreeChopHits(FruitTree fruitTree)
        {
            int axeLevel = GetBestAxeUpgradeLevel();
            float damage = axeLevel switch
            {
                0 => 1.0f,
                1 => 1.25f,
                2 => 1.67f,
                3 => 2.5f,
                4 => 5.0f,
                _ => axeLevel + 1
            };

            if (fruitTree.growthStage.Value >= 4)
            {
                if (fruitTree.stump.Value)
                {
                    float stumpHealth = fruitTree.health.Value > 0f ? fruitTree.health.Value : 5f;
                    int stumpHits = Math.Max(1, (int)Math.Ceiling(stumpHealth / damage));
                    return (stumpHits, 0, stumpHits);
                }
                else
                {
                    float trunkHealth = fruitTree.health.Value > 0f ? fruitTree.health.Value : 5f;
                    int trunkHits = Math.Max(1, (int)Math.Ceiling(trunkHealth / damage));
                    int stumpHits = Math.Max(1, (int)Math.Ceiling(5f / damage));
                    return (trunkHits + stumpHits, trunkHits, stumpHits);
                }
            }
            else
            {
                return (1, 1, 0);
            }
        }

        /// <summary>
        /// Analyzes a breakable ground Object (stone, ore node, twig, weed) and produces tooltip info.
        /// </summary>
        public static DebrisInfo? GetDebrisInfo(StardewValley.Object obj)
        {
            if (obj == null)
                return null;

            // 1. Twigs / Wood on the ground
            if (obj.IsTwig() || obj.ItemId is "30" or "294" or "295")
            {
                return new DebrisInfo
                {
                    Name = obj.DisplayName ?? ModEntry.I18n.Get("hover.debris.twig").ToString(),
                    ToolHitsText = ModEntry.I18n.Get("hover.debris.axe-chops", new { count = 1 }).ToString(),
                    ToolHitsColor = new Color(0, 140, 0),
                    IconTexture = obj.bigCraftable.Value ? null : Game1.objectSpriteSheet,
                    IconSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, 16, 16)
                };
            }

            // 2. Weeds
            if (obj.IsWeeds() || obj.Name.Contains("Weeds", StringComparison.OrdinalIgnoreCase))
            {
                return new DebrisInfo
                {
                    Name = obj.DisplayName ?? ModEntry.I18n.Get("hover.debris.weeds").ToString(),
                    ToolHitsText = ModEntry.I18n.Get("hover.debris.any-tool-hit", new { count = 1 }).ToString(),
                    ToolHitsColor = new Color(0, 140, 0),
                    IconTexture = obj.bigCraftable.Value ? null : Game1.objectSpriteSheet,
                    IconSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, 16, 16)
                };
            }

            // 3. Breakable Stones & Ore Nodes
            if (obj.IsBreakableStone())
            {
                int pickLevel = GetBestPickaxeUpgradeLevel();
                string name = obj.DisplayName ?? ModEntry.I18n.Get("hover.debris.stone").ToString();
                string qid = obj.QualifiedItemId;
                string id = obj.ItemId;

                // Check tool level requirements according to Stardew Valley Object.performToolAction:
                // Iridium node (12): Pickaxe level >= 2 (Steel Pickaxe+) required
                // Mystic stone (14): Pickaxe level >= 1 (Copper Pickaxe+) required
                if ((qid == "(O)12" || id == "12") && pickLevel < 2)
                {
                    return new DebrisInfo
                    {
                        Name = name,
                        ToolHitsText = ModEntry.I18n.Get("hover.debris.requires-tool", new { tool = ModEntry.I18n.Get("tool.steel-pickaxe") }).ToString(),
                        ToolHitsColor = new Color(200, 60, 20),
                        IconTexture = Game1.objectSpriteSheet,
                        IconSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, 16, 16)
                    };
                }

                if ((qid == "(O)14" || id == "14") && pickLevel < 1)
                {
                    return new DebrisInfo
                    {
                        Name = name,
                        ToolHitsText = ModEntry.I18n.Get("hover.debris.requires-tool", new { tool = ModEntry.I18n.Get("tool.copper-pickaxe") }).ToString(),
                        ToolHitsColor = new Color(200, 60, 20),
                        IconTexture = Game1.objectSpriteSheet,
                        IconSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, 16, 16)
                    };
                }

                int baseHealth = obj.MinutesUntilReady > 0 ? obj.MinutesUntilReady : GetDefaultStoneHealth(id);
                int damagePerHit = pickLevel + 1;
                int hitsRemaining = Math.Max(1, (int)Math.Ceiling((double)baseHealth / damagePerHit));

                return new DebrisInfo
                {
                    Name = name,
                    ToolHitsText = ModEntry.I18n.Get("hover.debris.pickaxe-hits", new { count = hitsRemaining }).ToString(),
                    ToolHitsColor = hitsRemaining == 1 ? new Color(0, 140, 0) : new Color(20, 110, 220),
                    IconTexture = Game1.objectSpriteSheet,
                    IconSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, 16, 16)
                };
            }

            // 4. Supply Crates
            if (obj.Name.Contains("SupplyCrate", StringComparison.OrdinalIgnoreCase))
            {
                int pickLevel = GetBestPickaxeUpgradeLevel();
                int baseHealth = obj.MinutesUntilReady > 0 ? obj.MinutesUntilReady : 3;
                int damagePerHit = pickLevel + 1;
                int hitsRemaining = Math.Max(1, (int)Math.Ceiling((double)baseHealth / damagePerHit));

                return new DebrisInfo
                {
                    Name = obj.DisplayName ?? "Supply Crate",
                    ToolHitsText = ModEntry.I18n.Get("hover.debris.any-tool-hit", new { count = hitsRemaining }).ToString(),
                    ToolHitsColor = new Color(20, 110, 220),
                    IconTexture = Game1.objectSpriteSheet,
                    IconSourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, 16, 16)
                };
            }

            return null;
        }

        /// <summary>
        /// Analyzes a large ResourceClump (large stump, hollow log, boulder, meteorite) and produces tooltip info.
        /// </summary>
        public static DebrisInfo? GetResourceClumpInfo(ResourceClump clump)
        {
            if (clump == null)
                return null;

            int index = clump.parentSheetIndex.Value;
            int axeLevel = GetBestAxeUpgradeLevel();
            int pickLevel = GetBestPickaxeUpgradeLevel();

            string name;
            string toolLine;
            Color color;

            switch (index)
            {
                case 600: // Large Stump
                    name = ModEntry.I18n.Get("lookup.clump.large-stump").ToString();
                    if (axeLevel < 1)
                    {
                        toolLine = ModEntry.I18n.Get("hover.debris.requires-tool", new { tool = ModEntry.I18n.Get("tool.copper-axe") }).ToString();
                        color = new Color(200, 60, 20);
                    }
                    else
                    {
                        float damage = Math.Max(1f, (axeLevel + 1) * 0.75f);
                        float curHealth = clump.health.Value > 0f ? clump.health.Value : 10f;
                        int hits = Math.Max(1, (int)Math.Ceiling(curHealth / damage));
                        toolLine = ModEntry.I18n.Get("hover.debris.axe-chops", new { count = hits }).ToString();
                        color = new Color(20, 110, 220);
                    }
                    break;

                case 602: // Hollow Log
                    name = ModEntry.I18n.Get("lookup.clump.hollow-log").ToString();
                    if (axeLevel < 2)
                    {
                        toolLine = ModEntry.I18n.Get("hover.debris.requires-tool", new { tool = ModEntry.I18n.Get("tool.steel-axe") }).ToString();
                        color = new Color(200, 60, 20);
                    }
                    else
                    {
                        float damage = Math.Max(1f, (axeLevel + 1) * 0.75f);
                        float curHealth = clump.health.Value > 0f ? clump.health.Value : 20f;
                        int hits = Math.Max(1, (int)Math.Ceiling(curHealth / damage));
                        toolLine = ModEntry.I18n.Get("hover.debris.axe-chops", new { count = hits }).ToString();
                        color = new Color(20, 110, 220);
                    }
                    break;

                case 672: // Meteorite
                    name = ModEntry.I18n.Get("lookup.clump.meteorite").ToString();
                    if (pickLevel < 3)
                    {
                        toolLine = ModEntry.I18n.Get("hover.debris.requires-tool", new { tool = ModEntry.I18n.Get("tool.gold-pickaxe") }).ToString();
                        color = new Color(200, 60, 20);
                    }
                    else
                    {
                        float damage = Math.Max(1f, (pickLevel + 1) * 0.75f);
                        float curHealth = clump.health.Value > 0f ? clump.health.Value : 10f;
                        int hits = Math.Max(1, (int)Math.Ceiling(curHealth / damage));
                        toolLine = ModEntry.I18n.Get("hover.debris.pickaxe-hits", new { count = hits }).ToString();
                        color = new Color(20, 110, 220);
                    }
                    break;

                case 752:
                case 754:
                case 756:
                case 758: // Boulders
                    name = ModEntry.I18n.Get("lookup.clump.boulder").ToString();
                    if (pickLevel < 2)
                    {
                        toolLine = ModEntry.I18n.Get("hover.debris.requires-tool", new { tool = ModEntry.I18n.Get("tool.steel-pickaxe") }).ToString();
                        color = new Color(200, 60, 20);
                    }
                    else
                    {
                        float damage = Math.Max(1f, (pickLevel + 1) * 0.75f);
                        float curHealth = clump.health.Value > 0f ? clump.health.Value : 10f;
                        int hits = Math.Max(1, (int)Math.Ceiling(curHealth / damage));
                        toolLine = ModEntry.I18n.Get("hover.debris.pickaxe-hits", new { count = hits }).ToString();
                        color = new Color(20, 110, 220);
                    }
                    break;

                case 148:
                case 622: // Mine Boulders
                    name = ModEntry.I18n.Get("lookup.clump.boulder").ToString();
                    if (pickLevel < 3)
                    {
                        toolLine = ModEntry.I18n.Get("hover.debris.requires-tool", new { tool = ModEntry.I18n.Get("tool.gold-pickaxe") }).ToString();
                        color = new Color(200, 60, 20);
                    }
                    else
                    {
                        float damage = Math.Max(1f, (pickLevel + 1) * 0.75f);
                        float curHealth = clump.health.Value > 0f ? clump.health.Value : 20f;
                        int hits = Math.Max(1, (int)Math.Ceiling(curHealth / damage));
                        toolLine = ModEntry.I18n.Get("hover.debris.pickaxe-hits", new { count = hits }).ToString();
                        color = new Color(20, 110, 220);
                    }
                    break;

                default:
                    name = ModEntry.I18n.Get("lookup.clump.resource-clump").ToString();
                    float defDamage = Math.Max(1f, (axeLevel + 1) * 0.75f);
                    float defHealth = clump.health.Value > 0f ? clump.health.Value : 10f;
                    int defHits = Math.Max(1, (int)Math.Ceiling(defHealth / defDamage));
                    toolLine = ModEntry.I18n.Get("hover.debris.axe-chops", new { count = defHits }).ToString();
                    color = new Color(20, 110, 220);
                    break;
            }

            return new DebrisInfo
            {
                Name = name,
                ToolHitsText = toolLine,
                ToolHitsColor = color
            };
        }

        private static int GetDefaultStoneHealth(string id)
        {
            return id switch
            {
                "8" => 4,
                "10" => 8,
                "12" => 16,
                "14" => 12,
                "25" => 8,
                "751" => 2, // Copper Node
                "290" or "843" or "844" => 4, // Iron Node
                "764" => 8, // Gold Node
                "765" => 16, // Iridium Node
                _ => 1
            };
        }
    }
}