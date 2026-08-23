using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace BetterChest
{
    public static class ChestPatches
    {
        public const string RolledKeyPrefix = "feiluvnana.BetterChest/Rolled:";

        public static void Apply(Harmony harmony)
        {
            try
            {
                var checkForActionMethod = AccessTools.Method(
                    typeof(Chest),
                    nameof(Chest.checkForAction),
                    new[] { typeof(Farmer), typeof(bool) }
                );

                if (checkForActionMethod != null)
                {
                    harmony.Patch(
                        original: checkForActionMethod,
                        prefix: new HarmonyMethod(typeof(ChestPatches), nameof(CheckForAction_Prefix))
                    );
                    ModEntry.ModMonitor.Log("Hooked Chest.checkForAction for per-player reward rolls.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to patch Chest.checkForAction: {ex}", LogLevel.Error);
            }
        }

        public static void CheckForAction_Prefix(Chest __instance, Farmer who, bool justCheckingForActivity)
        {
            if (justCheckingForActivity || who == null || __instance == null)
                return;

            try
            {
                if (!__instance.modData.ContainsKey(ModEntry.GeneratedModDataKey))
                    return;

                string rolledKey = RolledKeyPrefix + who.UniqueMultiplayerID;
                if (__instance.modData.ContainsKey(rolledKey))
                    return;

                int mineLevel = (__instance.Location as MineShaft)?.mineLevel ?? 121;
                bool isSpecial = mineLevel == 220 || mineLevel == 320 || mineLevel == 420 || mineLevel == 520;

                if (ModEntry.Config.EnableCustomRewards)
                {
                    var rewards = RewardGenerator.GenerateRewards(ModEntry.Config, Game1.random, isSpecialChest: isSpecial, mineLevel: mineLevel);
                    __instance.Items.Clear();
                    foreach (var reward in rewards)
                    {
                        var leftover = __instance.addItem(reward);
                        if (leftover != null && leftover.Stack > 0)
                        {
                            __instance.Items.Add(leftover);
                        }
                    }
                }
                else if (ModEntry.Config.ExcludeCosmetics && __instance.Items != null)
                {
                    for (int i = __instance.Items.Count - 1; i >= 0; i--)
                    {
                        if (__instance.Items[i] != null && RewardGenerator.IsCosmeticItem(__instance.Items[i]))
                        {
                            __instance.Items.RemoveAt(i);
                        }
                    }

                    if (__instance.Items.Count == 0)
                    {
                        Item fallback = ItemRegistry.Create("(O)337", Game1.random.Next(3, 8)); // 3-7x Iridium Bar
                        __instance.addItem(fallback);
                    }
                }

                __instance.modData[rolledKey] = "true";
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error rolling per-player chest rewards: {ex}", LogLevel.Trace);
            }
        }
    }
}
