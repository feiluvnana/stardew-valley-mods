// "using" directives import other libraries' namespaces so short names work:
//   HarmonyLib              -> the Harmony patching library (Patch, Prefix...)
//   StardewModdingAPI       -> SMAPI: logging types (IMonitor, LogLevel)
//   StardewValley           -> core game code (Farmer, Game1, Item, Random)
//   StardewValley.Locations -> MineShaft, the Skull Cavern location class
//   StardewValley.Objects   -> Chest and other placeable world objects
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

// ============================================================================
// ChestPatches gives every player their OWN roll from Skull Cavern treasure
// chests (in multiplayer, one chest would otherwise hold the same loot for
// everyone). It Harmony-patches Chest.checkForAction — the method the game
// runs whenever a player interacts with a chest — with a PREFIX (code that
// runs BEFORE the original). ModEntry.ProcessMineShaftChests tags eligible
// chests with modData when you warp onto a floor; this class notices the tag
// and fills the chest the first time THAT player opens it.
// Key concepts demonstrated: Harmony prefixes, __instance / original-method
// arguments, and storing per-player flags via the modData dictionary.
// ============================================================================
namespace BetterChest
{
    /// <summary>
    /// Harmony patches for <see cref="Chest.checkForAction"/> that roll custom,
    /// per-player rewards for tagged Skull Cavern treasure chests.
    /// </summary>
    public static class ChestPatches
    {
        // C# recap: "const" = a value frozen at compile time. This entire class
        // is "static" — it can NEVER be instantiated with "new"; it merely
        // groups these members under one accessible name (ChestPatches.Apply).
        /// <summary>
        /// Prefix of the per-player "already rolled" flag stored in a chest's
        /// modData dictionary (full key = this prefix + the player's multiplayer id).
        /// </summary>
        public const string RolledKeyPrefix = "feiluvnana.BetterChest/Rolled:";

        /// <summary>
        /// Locates the game's <c>Chest.checkForAction(Farmer, bool)</c> method and attaches
        /// <see cref="CheckForAction_Prefix"/> to it as a Harmony prefix. Called once at startup.
        /// </summary>
        /// <param name="harmony">The shared Harmony instance created by the mod entry point.</param>
        public static void Apply(Harmony harmony)
        {
            try
            {
                // Locate Chest.checkForAction via reflection. The third argument lists the
                // parameter types (Farmer, bool) because the game has overloaded versions
                // of checkForAction — this selects exactly the right one.
                // "var" = implicit typing: the compiler deduces the local's type
                // (here MethodInfo) from the right-hand side. It is still fully
                // statically typed — unlike JavaScript's dynamic var.
                var checkForActionMethod = AccessTools.Method(
                    typeof(Chest),
                    nameof(Chest.checkForAction),
                    new[] { typeof(Farmer), typeof(bool) }
                );

                // Null-check before patching (the method would be null if a game
                // update renamed or removed it).
                if (checkForActionMethod != null)
                {
                    // "prefix" = our method runs BEFORE the original interaction code.
                    // Because our prefix returns void, the original ALWAYS still runs
                    // afterwards (a prefix returning false would skip the original).
                    harmony.Patch(
                        original: checkForActionMethod,
                        prefix: new HarmonyMethod(typeof(ChestPatches), nameof(CheckForAction_Prefix))
                    );
                    // Trace is the lowest log verbosity — quiet diagnostics players rarely see.
                    ModEntry.ModMonitor.Log("Hooked Chest.checkForAction for per-player reward rolls.", LogLevel.Trace);
                }
            }
            // "catch" executes ONLY if something above threw an Exception (a
            // runtime error object). Absorbing the failure here keeps the game
            // running even if a future game update breaks the patch target.
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to patch Chest.checkForAction: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Harmony prefix for <c>Chest.checkForAction</c>: the first time THIS player
        /// opens a tagged chest, its contents are replaced with freshly rolled rewards.
        /// </summary>
        /// <param name="__instance">Harmony special: the Chest being interacted with.</param>
        /// <param name="who">The farmer performing the action (matches the original method's parameter).</param>
        /// <param name="justCheckingForActivity">True when the game is only probing whether the chest holds anything, not actually opening it.</param>
        public static void CheckForAction_Prefix(Chest __instance, Farmer who, bool justCheckingForActivity)
        {
            // C# note: in a "void" (returns-nothing) method, a bare "return;"
            // simply exits the method early.
            // Skip "peek" queries and null objects — only act on real openings.
            if (justCheckingForActivity || who == null || __instance == null)
                return;

            try
            {
                // modData is a persistent string-to-string dictionary attached to any
                // game object. Only chests that ModEntry tagged with GeneratedModDataKey qualify.
                if (!__instance.modData.ContainsKey(ModEntry.GeneratedModDataKey))
                    return;

                // Build a UNIQUE flag per player (UniqueMultiplayerID differs for everyone),
                // so each player rolls their own loot once instead of sharing one result.
                string rolledKey = RolledKeyPrefix + who.UniqueMultiplayerID;
                if (__instance.modData.ContainsKey(rolledKey))
                    return;

                // "as" casts Location to MineShaft (null if it isn't one);
                // "?." only reads mineLevel if the cast succeeded;
                // "??" falls back to 121 — the first Skull Cavern floor — otherwise.
                int mineLevel = (__instance.Location as MineShaft)?.mineLevel ?? 121;
                int relativeDepth = Math.Max(1, mineLevel > 120 ? mineLevel - 120 : mineLevel);
                bool isSpecial = (relativeDepth % 100 == 0);

                if (ModEntry.Config.EnableCustomRewards)
                {
                    // Roll this player's personal loot from the shared RewardGenerator.
                    var rewards = RewardGenerator.GenerateRewards(ModEntry.Config, Game1.random, isSpecialChest: isSpecial, mineLevel: mineLevel);
                    __instance.Items.Clear();
                    foreach (var reward in rewards)
                    {
                        // addItem tries to fit the item in; whatever did NOT fit comes back
                        // as "leftover", which we re-add so nothing is silently lost.
                        var leftover = __instance.addItem(reward);
                        if (leftover != null && leftover.Stack > 0)
                        {
                            __instance.Items.Add(leftover);
                        }
                    }
                }
                else if (ModEntry.Config.ExcludeCosmetics && __instance.Items != null)
                {
                    // Walk BACKWARDS (Count-1 down to 0) while removing: removing while
                    // iterating forward would shift later items down and skip some.
                    for (int i = __instance.Items.Count - 1; i >= 0; i--)
                    {
                        if (__instance.Items[i] != null && RewardGenerator.IsCosmeticItem(__instance.Items[i]))
                        {
                            __instance.Items.RemoveAt(i);
                        }
                    }

                    // If filtering emptied the chest entirely, guarantee a consolation prize.
                    if (__instance.modData.ContainsKey("BetterChest.Looted"))
                        return;

                    if (__instance.Items.Count == 0)
                    {
                        Item fallback = ItemRegistry.Create("(O)337", Game1.random.Next(3, 8)); // 3-7x Iridium Bar
                        __instance.addItem(fallback);
                    }
                    __instance.modData["BetterChest.Looted"] = "true";
                }

                // Mark this player/chest pair as done so the prefix does nothing next time.
                __instance.modData[rolledKey] = "true";
            }
            catch (Exception ex)
            {
                // Never let a loot bug break chest opening — log quietly and move on.
                ModEntry.ModMonitor.Log($"Error rolling per-player chest rewards: {ex}", LogLevel.Trace);
            }
        }
    }
}
