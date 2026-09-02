using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace BetterQOL
{
    /// <summary>
    /// Harmony patch for Stardew Valley's in-game Skills tab (SkillsPage).
    /// Displays exact experience points (XP) directly on the skills panel next to each skill bar,
    /// and renders a detailed hover tooltip showing remaining XP, progress percentage, and buffs.
    /// </summary>
    public static class SkillsPagePatch
    {
        /// <summary>Standard XP milestones for Skill levels 1 to 10 in Stardew Valley.</summary>
        public static readonly int[] ExpPointsPerLevel = new[]
        {
            100,    // Level 1
            380,    // Level 2 (+280)
            770,    // Level 3 (+390)
            1300,   // Level 4 (+530)
            2150,   // Level 5 (+850)
            3300,   // Level 6 (+1150)
            4800,   // Level 7 (+1500)
            6900,   // Level 8 (+2100)
            10000,  // Level 9 (+3100)
            15000   // Level 10 (+5000)
        };

        /// <summary>1.6 Mastery total experience milestones for Mastery Levels 1 to 5.</summary>
        public static readonly int[] MasteryExpGoals = new[]
        {
            10000,  // Point 1
            25000,  // Point 2
            45000,  // Point 3
            70000,  // Point 4
            100000  // Point 5
        };

        /// <summary>
        /// Registers Harmony postfix on SkillsPage.draw.
        /// </summary>
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            try
            {
                var drawMethod = AccessTools.Method(typeof(SkillsPage), nameof(SkillsPage.draw), new[] { typeof(SpriteBatch) });
                if (drawMethod != null)
                {
                    var drawPostfix = new HarmonyMethod(typeof(SkillsPagePatch), nameof(DrawPostfix));
                    harmony.Patch(drawMethod, postfix: drawPostfix);
                    monitor.Log("Successfully applied SkillsPage draw patch for direct exact experience display and hover tooltips.", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed to patch SkillsPage.draw: {ex.Message}", LogLevel.Warn);
            }
        }

        /// <summary>
        /// Postfix on SkillsPage.draw to render rich hover tooltips with exact experience details.
        /// </summary>
        public static void DrawPostfix(SkillsPage __instance, SpriteBatch b)
        {
            if (!Context.IsWorldReady || !ModEntry.Config.ShowExactExperienceInSkillsPage)
                return;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            // If the mouse is hovering over an unlocked profession icon (in skillBars),
            // let vanilla draw the profession tooltip without occlusion.
            if (__instance.skillBars != null)
            {
                foreach (var sb in __instance.skillBars)
                {
                    if (sb != null && sb.containsPoint(mouseX, mouseY) && !string.IsNullOrEmpty(sb.hoverText))
                        return;
                }
            }

            // Also check CC tracker buttons if any
            if (__instance.ccTrackerButtons != null)
            {
                foreach (var ccb in __instance.ccTrackerButtons)
                {
                    if (ccb != null && ccb.containsPoint(mouseX, mouseY))
                        return;
                }
            }

            // Calculate start coordinates matching vanilla SkillsPage.draw layout
            int startX = (LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ru || LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.it)
                ? (__instance.xPositionOnScreen + __instance.width - 448 - 48)
                : (__instance.xPositionOnScreen + IClickableMenu.borderWidth + IClickableMenu.spaceToClearTopBorder + 256 - 8);

            int startY = __instance.yPositionOnScreen + IClickableMenu.spaceToClearTopBorder + IClickableMenu.borderWidth - 8;

            int hoveredSkillIndex = -1;

            // 5 visual skill rows in vanilla SkillsPage:
            // Row 0: Farming (0)
            // Row 1: Fishing (1)
            // Row 2: Foraging (2)
            // Row 3: Mining (3)
            // Row 4: Combat (4)
            for (int row = 0; row < 5; row++)
            {
                int skillIndex = row switch
                {
                    0 => Farmer.farmingSkill,
                    1 => Farmer.fishingSkill,
                    2 => Farmer.foragingSkill,
                    3 => Farmer.miningSkill,
                    4 => Farmer.combatSkill,
                    _ => row
                };

                int rowY = startY + row * 68;
                // Hitbox spanning from the left skill icon/title to the rightmost level number
                int rowLeft = startX - 176;
                int rowWidth = 630;
                int rowHeight = 56;

                var rowRect = new Rectangle(rowLeft, rowY - 4, rowWidth, rowHeight);
                if (rowRect.Contains(mouseX, mouseY))
                {
                    hoveredSkillIndex = skillIndex;
                    break;
                }
            }

            // Render Hover Tooltip for hovered skill row
            if (hoveredSkillIndex >= 0)
            {
                int currentXp = Game1.player.experiencePoints.Length > hoveredSkillIndex ? Game1.player.experiencePoints[hoveredSkillIndex] : 0;
                int baseLevel = Game1.player.GetUnmodifiedSkillLevel(hoveredSkillIndex);
                int buffedLevel = Game1.player.GetSkillLevel(hoveredSkillIndex);

                string skillName = GetLocalizedSkillName(hoveredSkillIndex);
                string hoverTitle;
                string hoverText;

                if (baseLevel < 10)
                {
                    int nextLevel = baseLevel + 1;
                    int nextLevelXp = ExpPointsPerLevel[Math.Clamp(baseLevel, 0, ExpPointsPerLevel.Length - 1)];
                    int prevLevelXp = baseLevel > 0 ? ExpPointsPerLevel[baseLevel - 1] : 0;
                    int xpRemaining = Math.Max(0, nextLevelXp - currentXp);
                    float progress = Math.Clamp((float)(currentXp - prevLevelXp) / Math.Max(1, nextLevelXp - prevLevelXp) * 100f, 0f, 100f);

                    string buffText = buffedLevel > baseLevel ? $" (+{buffedLevel - baseLevel})" : "";
                    hoverTitle = ModEntry.I18n.Get("skills.hover.title", new
                    {
                        skill = skillName,
                        level = $"{baseLevel}{buffText}"
                    }).ToString();

                    hoverText = ModEntry.I18n.Get("skills.hover.body", new
                    {
                        current = $"{currentXp:N0}",
                        next = $"{nextLevelXp:N0}",
                        needed = $"{xpRemaining:N0}",
                        targetLevel = nextLevel,
                        percent = $"{progress:0.0}"
                    }).ToString();
                }
                else
                {
                    string buffText = buffedLevel > 10 ? $" (+{buffedLevel - 10})" : "";
                    hoverTitle = ModEntry.I18n.Get("skills.hover.max-title", new
                    {
                        skill = skillName,
                        level = $"10{buffText}"
                    }).ToString();

                    hoverText = ModEntry.I18n.Get("skills.hover.max-body", new
                    {
                        current = $"{currentXp:N0}"
                    }).ToString();
                }

                IClickableMenu.drawToolTip(b, hoverText, hoverTitle, null);
                return;
            }

            // 1.6 Mastery Bar Hover Detection
            if ((int)Game1.stats.Get("MasteryExp") > 0)
            {
                var masteryRect = new Rectangle(__instance.xPositionOnScreen + 240, __instance.yPositionOnScreen + 485, 570, 50);
                if (masteryRect.Contains(mouseX, mouseY))
                {
                    int masteryExp = (int)Game1.stats.Get("MasteryExp");
                    int currentMasteryLevel = MasteryTrackerMenu.getCurrentMasteryLevel();
                    int spentMasteryLevels = (int)Game1.stats.Get("masteryLevelsSpent");
                    int claimablePoints = Math.Max(0, currentMasteryLevel - spentMasteryLevels);

                    string hoverTitle;
                    string hoverText;

                    if (currentMasteryLevel < 5)
                    {
                        int expForCurrent = MasteryTrackerMenu.getMasteryExpNeededForLevel(currentMasteryLevel);
                        int expForNext = MasteryTrackerMenu.getMasteryExpNeededForLevel(currentMasteryLevel + 1);
                        int needed = Math.Max(0, expForNext - masteryExp);
                        float progress = Math.Clamp((float)(masteryExp - expForCurrent) / Math.Max(1, expForNext - expForCurrent) * 100f, 0f, 100f);

                        hoverTitle = ModEntry.I18n.Get("skills.hover.mastery-title", new
                        {
                            level = $"{currentMasteryLevel} / 5"
                        }).ToString();

                        hoverText = ModEntry.I18n.Get("skills.hover.mastery-body", new
                        {
                            current = $"{masteryExp:N0}",
                            next = $"{expForNext:N0}",
                            needed = $"{needed:N0}",
                            percent = $"{progress:0.0}",
                            claimable = $"{claimablePoints}"
                        }).ToString();
                    }
                    else
                    {
                        hoverTitle = ModEntry.I18n.Get("skills.hover.mastery-max-title").ToString();
                        hoverText = ModEntry.I18n.Get("skills.hover.mastery-max-body", new
                        {
                            current = $"{masteryExp:N0}",
                            claimable = $"{claimablePoints}"
                        }).ToString();
                    }

                    IClickableMenu.drawToolTip(b, hoverText, hoverTitle, null);
                }
            }
        }

        /// <summary>
        /// Gets the localized display name for a skill index.
        /// </summary>
        public static string GetLocalizedSkillName(int skillIndex)
        {
            return skillIndex switch
            {
                Farmer.farmingSkill => ModEntry.I18n.Get("lookup.skills.farming").ToString(),
                Farmer.fishingSkill => ModEntry.I18n.Get("lookup.skills.fishing").ToString(),
                Farmer.foragingSkill => ModEntry.I18n.Get("lookup.skills.foraging").ToString(),
                Farmer.miningSkill => ModEntry.I18n.Get("lookup.skills.mining").ToString(),
                Farmer.combatSkill => ModEntry.I18n.Get("lookup.skills.combat").ToString(),
                _ => ModEntry.I18n.Get("lookup.type.item").ToString()
            };
        }
    }
}
