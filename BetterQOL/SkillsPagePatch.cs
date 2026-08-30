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
        /// Postfix on SkillsPage.draw to draw exact XP numbers on the panel and rich hover tooltips.
        /// </summary>
        public static void DrawPostfix(SkillsPage __instance, SpriteBatch b)
        {
            if (!Context.IsWorldReady || !ModEntry.Config.ShowExactExperienceInSkillsPage)
                return;

            if (__instance.skillBars == null || __instance.skillBars.Count < 50)
                return;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            int hoveredSkillIndex = -1;

            // 5 Skills: 0 = Farming, 1 = Fishing, 2 = Foraging, 3 = Mining, 4 = Combat
            for (int skillIndex = 0; skillIndex < 5; skillIndex++)
            {
                int firstBarIndex = skillIndex * 10;
                int lastBarIndex = skillIndex * 10 + 9;

                if (firstBarIndex >= __instance.skillBars.Count || lastBarIndex >= __instance.skillBars.Count)
                    continue;

                var firstBar = __instance.skillBars[firstBarIndex];
                var lastBar = __instance.skillBars[lastBarIndex];

                int currentXp = Game1.player.experiencePoints.Length > skillIndex ? Game1.player.experiencePoints[skillIndex] : 0;
                int baseLevel = Game1.player.GetUnmodifiedSkillLevel(skillIndex);
                int buffedLevel = Game1.player.GetSkillLevel(skillIndex);

                // 1. Direct on-panel text display next to the skill bar & profession icons
                int textX = lastBar.bounds.Right + 56;
                int textY = lastBar.bounds.Y - 2;

                string onPanelText;
                Color textColor;

                if (baseLevel < 10)
                {
                    int nextLevelXp = ExpPointsPerLevel[Math.Clamp(baseLevel, 0, ExpPointsPerLevel.Length - 1)];
                    onPanelText = $"{currentXp:N0} / {nextLevelXp:N0} XP";
                    textColor = Game1.textColor;
                }
                else
                {
                    onPanelText = $"{currentXp:N0} XP (Max)";
                    textColor = new Color(140, 50, 160);
                }

                Utility.drawTextWithShadow(b, onPanelText, Game1.smallFont, new Vector2(textX, textY), textColor);

                // 2. Row hover detection for detailed tooltip
                int rowLeft = __instance.xPositionOnScreen + IClickableMenu.spaceToClearSideBorder + 16;
                int rowWidth = __instance.width - (IClickableMenu.spaceToClearSideBorder * 2) - 32;
                int rowTop = firstBar.bounds.Y - 10;
                int rowHeight = firstBar.bounds.Height + 20;

                var rowRect = new Rectangle(rowLeft, rowTop, rowWidth, rowHeight);
                if (rowRect.Contains(mouseX, mouseY))
                {
                    hoveredSkillIndex = skillIndex;
                }
            }

            // 3. Render Hover Tooltip on top of the menu if a skill row is hovered
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
