// "using" imports the game's own namespace so short names resolve:
//   StardewValley -> Game1 (global game state), Farmer, Utility, stats/mail APIs.
using StardewValley;

// ============================================================================
// ProgressionHelper answers "has the player earned X yet?" by reading the same
// save data Stardew Valley uses internally: mail flags (strings like
// "Visited_Island" that act as invisible achievement markers), per-player
// stats, and world state such as GoldenWalnuts. RewardGenerator consults these
// gates so late-game items (Qi goods, island items, Mastery rewards...) can't
// drop for a brand-new farm.
// Key concepts demonstrated: null-safe access with ?. and ??, C# switch
// expressions, and checking BOTH the local player and the multiplayer host.
// ============================================================================
namespace BetterChest
{
    // C# concept — STATIC UTILITY CLASS: never instantiated; simply a labeled
    // bag of stateless question methods, called like
    // ProgressionHelper.IsIslandUnlocked(). Perfect for pure true/false gates.
    /// <summary>
    /// Static utility class of progression gate checks used to keep late-game loot
    /// gated behind its in-game unlocks (island, Qi's room, Mastery, festivals...).
    /// </summary>
    public static class ProgressionHelper
    {
        /// <summary>
        /// Checks if the player has visited Ginger Island or collected golden walnuts.
        /// </summary>
        /// <returns>True if the island is unlocked for this farmer (checked on both the host "MasterPlayer" and the local player).</returns>
        public static bool IsIslandUnlocked()
        {
            // MasterPlayer is the save file's host farmer — in multiplayer their
            // progress often unlocks things for everyone, so check them first.
            if (Game1.MasterPlayer != null)
            {
                // hasOrWillReceiveMail covers mail scheduled but not yet seen; the
                // mailReceived list holds flags already granted. Either means "visited".
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

            // "?.Value" unwraps the net-synced world state only if it exists; walnuts
            // can only be > 0 once the island is reachable.
            if (Game1.netWorldState?.Value != null && Game1.netWorldState.Value.GoldenWalnuts > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Checks if Qi's Walnut Room (100 Golden Walnuts) is unlocked.
        /// </summary>
        /// <returns>True if the Qi walnut room door can be opened in this save.</returns>
        public static bool IsQiRoomUnlocked()
        {
            // 100+ walnuts in the shared world state = the room's requirement met.
            if (Game1.netWorldState?.Value != null && Game1.netWorldState.Value.GoldenWalnuts >= 100)
                return true;

            if (Game1.player != null)
            {
                // The "MineConditionsLocked" special-order rule and the per-player
                // walnut stat are extra signals used by different game versions/modes.
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
        /// <returns>True if mystery boxes are available in this save.</returns>
        public static bool IsMysteryBoxUnlocked()
        {
            if (Game1.player != null)
            {
                // Either the "QiMysteryBox" mail flag or a nonzero "MysteryBoxesOpened"
                // stat proves the player has encountered the feature.
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

            // Game1.stats is the global stat sheet shared across the whole farm.
            if (Game1.stats != null && Game1.stats.Get("MysteryBoxesOpened") > 0)
                return true;

            return false;
        }

        /// <summary>
        /// Gets the total number of mystery boxes opened across player stats.
        /// </summary>
        /// <returns>The highest mystery-box-opened count found on any stat sheet (local player, host, or global).</returns>
        public static uint GetMysteryBoxesOpened()
        {
            // uint = unsigned int (can't be negative) — the game uses it for counters.
            // Taking the MAX of all three sheets avoids undercounting in multiplayer
            // where stats may live on different farmers.
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
        /// <param name="specificSkill">Optional skill name ("Farming", "Fishing", ...). Null/empty checks ANY mastery.</param>
        /// <returns>True when the requested mastery has been claimed.</returns>
        public static bool IsMasteryUnlocked(string? specificSkill = null)
        {
            // "?? " falls back to the host farmer if there is no local player object.
            Farmer player = Game1.player ?? Game1.MasterPlayer;
            if (player == null)
                return false;

            // string? means the parameter is allowed to be null (nullable reference type).
            if (string.IsNullOrEmpty(specificSkill))
            {
                // Check if any mastery claimed or mastery bar active
                // The game stores each mastery claim as its own mail flag; string
                // interpolation ($"...{i}") builds names like "hasClaimedMastery_2".
                for (int i = 0; i < 5; i++)
                {
                    if (player.mailReceived.Contains($"Mastery_{i}") ||
                        player.hasOrWillReceiveMail($"Mastery_{i}") ||
                        player.mailReceived.Contains($"mastery_{i}") ||
                        player.hasOrWillReceiveMail($"mastery_{i}"))
                    {
                        return true;
                    }
                }

                if (player.stats != null && player.stats.Get("MasteryExp") > 0)
                    return true;

                return false;
            }

            // Switch expression: maps an input to a result arm-by-arm.
            // ToLowerInvariant makes matching case-insensitive ("fishing" == "Fishing");
            // "_ => -1" is the discard arm — the default for unmatched input.
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
                return player.mailReceived.Contains($"Mastery_{skillIndex}") ||
                       player.hasOrWillReceiveMail($"Mastery_{skillIndex}") ||
                       player.mailReceived.Contains($"mastery_{skillIndex}") ||
                       player.hasOrWillReceiveMail($"mastery_{skillIndex}");
            }

            return false;
        }

        /// <summary>
        /// Checks if the Desert Festival is currently happening or active.
        /// </summary>
        /// <returns>True while the Desert Festival is running.</returns>
        public static bool IsDesertFestivalActive()
        {
            // 1.6's passive-festival system knows the official schedule.
            if (Utility.IsPassiveFestivalDay("DesertFestival"))
                return true;

            // Fallback for Desert Festival days (15-17)
            return Game1.dayOfMonth >= 15 && Game1.dayOfMonth <= 17;
        }

        /// <summary>
        /// Checks if Community Center or Joja route is completed.
        /// </summary>
        /// <returns>True once either main-town storyline has been finished.</returns>
        public static bool IsCommunityCenterCompleted()
        {
            Farmer player = Game1.player ?? Game1.MasterPlayer;
            if (player == null)
                return false;

            // Covers both routes: ccIsComplete for the Community Center path,
            // JojaMember/jojaComplete for the JojaMart path.
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
        /// <returns>The deepest mine floor recorded on the save.</returns>
        public static int GetDeepestMineLevel()
        {
            Farmer player = Game1.player ?? Game1.MasterPlayer;
            if (player == null)
                return 0;

            // The game itself tracks this stat as you descend.
            return player.deepestMineLevel;
        }

        /// <summary>
        /// Checks if the Volcano Caldera exit shortcut on floor 10 has been unlocked with 5 Golden Walnuts.
        /// </summary>
        /// <returns>True if the Volcano Caldera shortcut is unlocked in this save.</returns>
        public static bool IsVolcanoShortcutUnlocked()
        {
            if (Game1.MasterPlayer != null)
            {
                if (Game1.MasterPlayer.hasOrWillReceiveMail("CalderaShortcut") ||
                    Game1.MasterPlayer.mailReceived.Contains("CalderaShortcut"))
                {
                    return true;
                }
            }

            if (Game1.player != null)
            {
                if (Game1.player.hasOrWillReceiveMail("CalderaShortcut") ||
                    Game1.player.mailReceived.Contains("CalderaShortcut"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the player's effective farming level.
        /// </summary>
        /// <returns>The highest farming level between local player and host.</returns>
        public static int GetFarmingLevel()
        {
            int level = 0;
            if (Game1.player != null)
                level = Math.Max(level, Game1.player.FarmingLevel);
            if (Game1.MasterPlayer != null)
                level = Math.Max(level, Game1.MasterPlayer.FarmingLevel);
            return level;
        }

        /// <summary>
        /// Gets the player's effective mining level.
        /// </summary>
        /// <returns>The highest mining level between local player and host.</returns>
        public static int GetMiningLevel()
        {
            int level = 0;
            if (Game1.player != null)
                level = Math.Max(level, Game1.player.MiningLevel);
            if (Game1.MasterPlayer != null)
                level = Math.Max(level, Game1.MasterPlayer.MiningLevel);
            return level;
        }
    }
}
