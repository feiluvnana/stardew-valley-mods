using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Objects.Trinkets;
using StardewValley.Projectiles;

namespace BetterTrinket
{
    public static class TrinketPatches
    {
        private static ModConfig Config = null!;
        private static IMonitor Monitor = null!;

        public static void Initialize(ModConfig config, IMonitor monitor)
        {
            Config = config;
            Monitor = monitor;
        }

        public static void Apply(Harmony harmony)
        {
            try
            {
                // Patch Anvil drop-in action
                var dropInMethod = AccessTools.Method(
                    typeof(StardewValley.Object),
                    nameof(StardewValley.Object.performObjectDropInAction),
                    new[] { typeof(Item), typeof(bool), typeof(Farmer), typeof(bool) }
                );

                if (dropInMethod != null)
                {
                    harmony.Patch(
                        original: dropInMethod,
                        prefix: new HarmonyMethod(typeof(TrinketPatches), nameof(Object_performObjectDropInAction_Prefix))
                    );
                    Monitor.Log("Hooked Object.performObjectDropInAction for Anvil successfully.", LogLevel.Trace);
                }

                // Patch Trinket.getDescription
                var descMethod = AccessTools.Method(typeof(Trinket), nameof(Trinket.getDescription));
                if (descMethod != null)
                {
                    harmony.Patch(
                        original: descMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Trinket_getDescription_Postfix))
                    );
                    Monitor.Log("Hooked Trinket.getDescription successfully.", LogLevel.Trace);
                }

                // Patch Monster.takeDamage for Ice Shatter
                var monsterDamageMethod = AccessTools.Method(
                    typeof(Monster),
                    nameof(Monster.takeDamage),
                    new[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(double), typeof(Farmer) }
                );

                if (monsterDamageMethod != null)
                {
                    harmony.Patch(
                        original: monsterDamageMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Monster_takeDamage_Postfix))
                    );
                    Monitor.Log("Hooked Monster.takeDamage for Ice Shatter successfully.", LogLevel.Trace);
                }

                // Patch Farmer.takeDamage for Basilisk Reflection
                var farmerDamageMethod = AccessTools.Method(
                    typeof(Farmer),
                    nameof(Farmer.takeDamage),
                    new[] { typeof(int), typeof(bool), typeof(Monster) }
                );

                if (farmerDamageMethod != null)
                {
                    harmony.Patch(
                        original: farmerDamageMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Farmer_takeDamage_Postfix))
                    );
                    Monitor.Log("Hooked Farmer.takeDamage for Damage Reflection successfully.", LogLevel.Trace);
                }

                // Patch Projectile collision for Magic Quiver Pierce
                var projectileCollisionMethod = AccessTools.Method(
                    typeof(BasicProjectile),
                    nameof(BasicProjectile.behaviorOnCollisionWithMonster),
                    new[] { typeof(NPC), typeof(GameLocation) }
                );

                if (projectileCollisionMethod != null)
                {
                    harmony.Patch(
                        original: projectileCollisionMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(BasicProjectile_behaviorOnCollisionWithMonster_Postfix))
                    );
                    Monitor.Log("Hooked BasicProjectile for Magic Quiver Piercing successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply TrinketPatches: {ex}", LogLevel.Error);
            }
        }

        public static bool IsAnvil(StardewValley.Object obj)
        {
            if (obj == null) return false;
            return obj.QualifiedItemId == "(BC)Anvil"
                || obj.ItemId == "Anvil"
                || obj.QualifiedItemId == "(BC)289"
                || obj.ItemId == "289"
                || obj.Name == "Anvil";
        }

        public static bool Object_performObjectDropInAction_Prefix(
            StardewValley.Object __instance,
            Item dropInItem,
            bool probe,
            Farmer who,
            ref bool __result)
        {
            if (__instance == null || !IsAnvil(__instance) || who == null)
                return true;

            // 1. Check Prismatic Ascension Drop-in (holding Prismatic Shard or targeting Trinket)
            if (dropInItem is Trinket trinket)
            {
                // Check if player has Prismatic Shard in inventory and wants Ascension
                bool holdingPrismatic = who.ActiveItem?.QualifiedItemId == "(O)74" || who.ActiveItem?.ItemId == "74";

                if (holdingPrismatic || (!TrinketAscensionLogic.IsAscended(trinket) && who.Items.ContainsId("(O)74", 1) && !who.Items.ContainsId("(O)337", Config.IridiumBarCost)))
                {
                    if (probe)
                    {
                        __result = !TrinketAscensionLogic.IsAscended(trinket) && who.Items.ContainsId("(O)74", 1);
                        return false;
                    }

                    if (TrinketAscensionLogic.TryAscendTrinket(trinket, who))
                    {
                        // Spawn rainbow sparkle on anvil
                        who.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(
                            "TileSheets\\animations",
                            new Rectangle(0, 640, 64, 64),
                            80f,
                            8,
                            0,
                            new Vector2(__instance.TileLocation.X * 64f, (__instance.TileLocation.Y - 1) * 64f),
                            flicker: false,
                            flipped: false
                        ));
                        __result = true;
                        return false;
                    }
                }

                // 2. Standard Smart Reforge with Iridium Bars
                int requiredBars = Math.Max(1, Config.IridiumBarCost);

                if (probe)
                {
                    __result = who.Items.ContainsId("(O)337", requiredBars);
                    return false;
                }

                if (!who.Items.ContainsId("(O)337", requiredBars))
                {
                    Game1.showRedMessage(ModEntry.I18n.Get("message.need-iridium", new { count = requiredBars }));
                    who.currentLocation.playSound("cancel");
                    __result = false;
                    return false;
                }

                // Consume Iridium Bars
                who.Items.ReduceId("(O)337", requiredBars);

                // Execute smart reforge
                TrinketReforgeLogic.ProcessReforge(trinket, who, Config);

                who.currentLocation.playSound("anvil");

                // Spawn sparkle animation
                who.currentLocation.TemporarySprites.Add(new TemporaryAnimatedSprite(
                    "TileSheets\\animations",
                    new Rectangle(0, 640, 64, 64),
                    100f,
                    8,
                    0,
                    new Vector2(__instance.TileLocation.X * 64f, (__instance.TileLocation.Y - 1) * 64f),
                    flicker: false,
                    flipped: false
                ));

                __result = true;
                return false;
            }

            return true;
        }

        public static void Trinket_getDescription_Postfix(Trinket __instance, ref string __result)
        {
            if (__instance == null)
                return;

            try
            {
                var eval = TrinketReforgeLogic.Evaluate(__instance.ItemId, __instance.generationSeed.Value);
                string extra = string.Empty;

                if (Config.ShowStatRangesInTooltips)
                {
                    extra += $"\n\nTier: {eval.StarString}\n{eval.Summary}";

                    int count = 0;
                    if (__instance.modData.TryGetValue(TrinketReforgeLogic.ReforgeCountKey, out string? countStr) && int.TryParse(countStr, out int parsedCount))
                    {
                        count = parsedCount;
                    }

                    if (eval.IsMaxRoll)
                    {
                        extra += $"\n{ModEntry.I18n.Get("tooltip.perfect-roll")}";
                    }
                    else if (Config.EnablePitySystem)
                    {
                        int max = Config.RollsForGuaranteedUpgrade;
                        int remaining = Math.Max(1, max - count);

                        if (count >= max)
                        {
                            extra += $"\n{ModEntry.I18n.Get("tooltip.pity-guaranteed", new { count = count, max = max })}";
                        }
                        else
                        {
                            extra += $"\n{ModEntry.I18n.Get("tooltip.pity-counter", new { count = count, max = max, remaining = remaining })}";
                        }
                    }
                }

                // Ascension Display
                if (TrinketAscensionLogic.IsAscended(__instance))
                {
                    string? desc = TrinketAscensionLogic.GetAscensionDescription(__instance.ItemId);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        extra += $"\n\n{ModEntry.I18n.Get("tooltip.ascended-badge")}";
                        extra += $"\n• {desc}";
                    }
                }

                __result += extra;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error formatting trinket tooltip: {ex}", LogLevel.Trace);
            }
        }

        public static void Monster_takeDamage_Postfix(Monster __instance, int damage, Farmer who, int __result)
        {
            if (__instance == null || who == null || __result <= 0) return;

            // Ice Rod Shatter Strike
            if (TrinketAscensionLogic.HasAscendedTrinket(who, "IceRod"))
            {
                if (__instance.stunTime.Value > 0 || __instance.isInvincible())
                {
                    TrinketAscensionLogic.TriggerIceShatter(__instance, who);
                }
            }
        }

        public static void Farmer_takeDamage_Postfix(Farmer __instance, int damage, Monster damager)
        {
            if (__instance == null || damager == null || damage <= 0) return;

            // Basilisk Paw Damage Reflection
            if (TrinketAscensionLogic.HasAscendedTrinket(__instance, "BasiliskPaw"))
            {
                TrinketAscensionLogic.TriggerDamageReflect(damager, __instance, damage);
            }
        }

        public static void BasicProjectile_behaviorOnCollisionWithMonster_Postfix(BasicProjectile __instance, NPC n, GameLocation location)
        {
            if (__instance == null || location == null) return;

            if (__instance.theOneWhoFiredMe.Get(location) is Farmer farmer)
            {
                // Magic Quiver Penetration
                if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "MagicQuiver"))
                {
                    __instance.destroyMe = false; // Arrow pierces through monsters!
                }
            }
        }
    }
}
