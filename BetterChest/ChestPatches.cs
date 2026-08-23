using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace BetterChest
{
    public static class ChestPatches
    {
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
                    ModEntry.ModMonitor.Log("Hooked Chest.checkForAction for reward summary notification.", LogLevel.Trace);
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
                if (__instance.modData.ContainsKey(ModEntry.GeneratedModDataKey) &&
                    !__instance.modData.ContainsKey("feiluvnana.BetterChest/Opened"))
                {
                    __instance.modData["feiluvnana.BetterChest/Opened"] = "true";

                    var validItems = __instance.Items.Where(i => i != null && i.Stack > 0).ToList();
                    if (validItems.Count > 0)
                    {
                        ShowChestRewardSummary(validItems);
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error showing chest reward summary: {ex}", LogLevel.Trace);
            }
        }

        public static void ShowChestRewardSummary(List<Item> items)
        {
            const int maxDisplayed = 4;
            var displayParts = new List<string>();

            for (int i = 0; i < Math.Min(items.Count, maxDisplayed); i++)
            {
                var item = items[i];
                string name = item.DisplayName;
                int stack = item.Stack;
                displayParts.Add(stack > 1 ? $"{stack}x {name}" : $"{name}");
            }

            string itemsText = string.Join(", ", displayParts);
            if (items.Count > maxDisplayed)
            {
                int remaining = items.Count - maxDisplayed;
                string moreText = ModEntry.I18n.Get("hud.chest-reward-more", new { count = remaining });
                itemsText += " " + moreText;
            }

            string fullMessage = ModEntry.I18n.Get("hud.chest-reward-summary", new { items = itemsText });
            Game1.addHUDMessage(new HUDMessage(fullMessage));
        }
    }
}
