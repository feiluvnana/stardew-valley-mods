using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace BetterChest
{
    public static class ProgressionHelper
    {
        /// <summary>
        /// Checks if the player has visited Ginger Island or collected golden walnuts.
        /// </summary>
        public static bool IsIslandUnlocked()
        {
            if (Game1.MasterPlayer != null)
            {
                if (Game1.MasterPlayer.hasOrWillReceiveMail("Visited_Island") ||
                    Game1.MasterPlayer.mailReceived.Contains("hasVisitedIsland") ||
                    Game1.MasterPlayer.mailReceived.Contains("Visited_Island"))
                {
                    return true;
                }
            }

            if (Game1.player != null)
            {
                if (Game1.player.hasOrWillReceiveMail("Visited_Island") ||
                    Game1.player.mailReceived.Contains("hasVisitedIsland") ||
                    Game1.player.mailReceived.Contains("Visited_Island"))
                {
                    return true;
                }
            }

            if (Game1.netWorldState?.Value != null && Game1.netWorldState.Value.GoldenWalnuts > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Checks if Qi's Walnut Room (100 Golden Walnuts) is unlocked.
        /// </summary>
        public static bool IsQiRoomUnlocked()
        {
            if (Game1.netWorldState?.Value != null && Game1.netWorldState.Value.GoldenWalnuts >= 100)
                return true;

            if (Game1.player != null)
            {
                if (Game1.player.hasOrWillReceiveMail("QiNutDoor") ||
                    Game1.player.mailReceived.Contains("QiNutDoor") ||
                    (Game1.player.team != null && Game1.player.team.SpecialOrderRuleActive("MineConditionsLocked")) ||
                    (Game1.player.stats != null && Game1.player.stats.Get("WalnutsCollected") >= 100))
                {
                    return true;
                }
            }

            if (Game1.MasterPlayer != null)
            {
                if (Game1.MasterPlayer.hasOrWillReceiveMail("QiNutDoor") ||
                    Game1.MasterPlayer.mailReceived.Contains("QiNutDoor") ||
                    (Game1.MasterPlayer.stats != null && Game1.MasterPlayer.stats.Get("WalnutsCollected") >= 100))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if Mr. Qi's Mystery Box event has been triggered or mystery boxes opened.
        /// </summary>
        public static bool IsMysteryBoxUnlocked()
        {
            if (Game1.player != null)
            {
                if (Game1.player.hasOrWillReceiveMail("QiMysteryBox") ||
                    Game1.player.mailReceived.Contains("QiMysteryBox") ||
                    (Game1.player.stats != null && Game1.player.stats.Get("MysteryBoxesOpened") > 0))
                {
                    return true;
                }
            }

            if (Game1.MasterPlayer != null)
            {
                if (Game1.MasterPlayer.hasOrWillReceiveMail("QiMysteryBox") ||
                    Game1.MasterPlayer.mailReceived.Contains("QiMysteryBox") ||
                    (Game1.MasterPlayer.stats != null && Game1.MasterPlayer.stats.Get("MysteryBoxesOpened") > 0))
                {
                    return true;
                }
            }

            if (Game1.stats != null && Game1.stats.Get("MysteryBoxesOpened") > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the total number of mystery boxes opened across player stats.
        /// </summary>
        public static uint GetMysteryBoxesOpened()
        {
            uint count = 0;
            if (Game1.player?.stats != null)
                count = Math.Max(count, Game1.player.stats.Get("MysteryBoxesOpened"));
            if (Game1.MasterPlayer?.stats != null)
                count = Math.Max(count, Game1.MasterPlayer.stats.Get("MysteryBoxesOpened"));
            if (Game1.stats != null)
                count = Math.Max(count, Game1.stats.Get("MysteryBoxesOpened"));
            return count;
        }

        /// <summary>
        /// Checks if the player has unlocked any Mastery or a specific mastery skill in 1.6.
        /// Skill indices: 0 = Farming, 1 = Fishing, 2 = Foraging, 3 = Mining, 4 = Combat
        /// </summary>
        public static bool IsMasteryUnlocked(string? specificSkill = null)
        {
            Farmer player = Game1.player ?? Game1.MasterPlayer;
            if (player == null)
                return false;

            if (string.IsNullOrEmpty(specificSkill))
            {
                // Check if any mastery claimed or mastery bar active
                for (int i = 0; i < 5; i++)
                {
                    if (player.mailReceived.Contains($"hasClaimedMastery_{i}") ||
                        player.hasOrWillReceiveMail($"hasClaimedMastery_{i}"))
                    {
                        return true;
                    }
                }

                if (player.stats != null && player.stats.Get("MasteryExp") > 0)
                    return true;

                return false;
            }

            int skillIndex = specificSkill.ToLowerInvariant() switch
            {
                "farming" => 0,
                "fishing" => 1,
                "foraging" => 2,
                "mining" => 3,
                "combat" => 4,
                _ => -1
            };

            if (skillIndex >= 0)
            {
                return player.mailReceived.Contains($"hasClaimedMastery_{skillIndex}") ||
                       player.hasOrWillReceiveMail($"hasClaimedMastery_{skillIndex}");
            }

            return false;
        }

        /// <summary>
        /// Checks if the Desert Festival is currently happening or active.
        /// </summary>
        public static bool IsDesertFestivalActive()
        {
            if (Utility.IsPassiveFestivalDay("DesertFestival"))
                return true;

            return Game1.dayOfMonth >= 15 && Game1.dayOfMonth <= 17;
        }

        /// <summary>
        /// Checks if Community Center or Joja route is completed.
        /// </summary>
        public static bool IsCommunityCenterCompleted()
        {
            Farmer player = Game1.player ?? Game1.MasterPlayer;
            if (player == null)
                return false;

            if (player.hasOrWillReceiveMail("ccIsComplete") ||
                player.hasOrWillReceiveMail("JojaMember") ||
                player.hasOrWillReceiveMail("jojaComplete") ||
                player.hasCompletedCommunityCenter())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the player's deepest mine level reached in the regular mines (0 to 120+).
        /// </summary>
        public static int GetDeepestMineLevel()
        {
            Farmer player = Game1.player ?? Game1.MasterPlayer;
            if (player == null)
                return 0;

            return player.deepestMineLevel;
        }
    }
}
