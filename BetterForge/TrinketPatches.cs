using System;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Companions;
using StardewValley.Monsters;
using StardewValley.Objects.Trinkets;
using StardewValley.Projectiles;

namespace BetterForge
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
            // 1. Patch Anvil check for action (right-click / activate)
            try
            {
                var checkForActionMethod = AccessTools.Method(
                    typeof(StardewValley.Object),
                    nameof(StardewValley.Object.checkForAction),
                    new[] { typeof(Farmer), typeof(bool) }
                );

                if (checkForActionMethod != null)
                {
                    harmony.Patch(
                        original: checkForActionMethod,
                        prefix: new HarmonyMethod(typeof(TrinketPatches), nameof(Object_checkForAction_Prefix))
                    );
                    Monitor.Log("Hooked Object.checkForAction for Anvil successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Object.checkForAction: {ex}", LogLevel.Error);
            }

            // 2. Patch Anvil drop-in action
            try
            {
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
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Object.performObjectDropInAction: {ex}", LogLevel.Error);
            }

            // 3. Patch Trinket.getDescription
            try
            {
                var descMethod = AccessTools.Method(typeof(Trinket), nameof(Trinket.getDescription));
                if (descMethod != null)
                {
                    harmony.Patch(
                        original: descMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Trinket_getDescription_Postfix))
                    );
                    Monitor.Log("Hooked Trinket.getDescription successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Trinket.getDescription: {ex}", LogLevel.Error);
            }

            // 4. Patch Trinket.loadDisplayName for Maxed + Ascended "Perfect" name
            try
            {
                var loadDisplayNameMethod = AccessTools.Method(typeof(Trinket), "loadDisplayName");
                if (loadDisplayNameMethod != null)
                {
                    harmony.Patch(
                        original: loadDisplayNameMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Trinket_loadDisplayName_Postfix))
                    );
                    Monitor.Log("Hooked Trinket.loadDisplayName successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Trinket.loadDisplayName: {ex}", LogLevel.Error);
            }

            // 5. Patch Trinket.OnDamageMonster (called on weapon/tool hits)
            try
            {
                var onDamageMonsterMethod = AccessTools.Method(
                    typeof(Trinket),
                    nameof(Trinket.OnDamageMonster),
                    new[] { typeof(Farmer), typeof(Monster), typeof(int), typeof(bool), typeof(bool) }
                );

                if (onDamageMonsterMethod != null)
                {
                    harmony.Patch(
                        original: onDamageMonsterMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Trinket_OnDamageMonster_Postfix))
                    );
                    Monitor.Log("Hooked Trinket.OnDamageMonster successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Trinket.OnDamageMonster: {ex}", LogLevel.Error);
            }

            // 6. Patch Farmer.takeDamage for Basilisk Reflection
            try
            {
                var farmerDamageMethod = AccessTools.Method(
                    typeof(Farmer),
                    nameof(Farmer.takeDamage),
                    new[] { typeof(int), typeof(bool), typeof(Monster) }
                );

                if (farmerDamageMethod != null)
                {
                    harmony.Patch(
                        original: farmerDamageMethod,
                        prefix: new HarmonyMethod(typeof(TrinketPatches), nameof(Farmer_takeDamage_Prefix)),
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Farmer_takeDamage_Postfix))
                    );
                    Monitor.Log("Hooked Farmer.takeDamage for Damage Reflection successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Farmer.takeDamage: {ex}", LogLevel.Error);
            }

            // 7. Patch MagicQuiverTrinketEffect.Update for Arrow Infinite Piercing
            try
            {
                var quiverUpdateMethod = AccessTools.Method(
                    typeof(MagicQuiverTrinketEffect),
                    nameof(MagicQuiverTrinketEffect.Update),
                    new[] { typeof(Farmer), typeof(GameTime), typeof(GameLocation) }
                );

                if (quiverUpdateMethod != null)
                {
                    harmony.Patch(
                        original: quiverUpdateMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(MagicQuiverTrinketEffect_Update_Postfix))
                    );
                    Monitor.Log("Hooked MagicQuiverTrinketEffect.Update successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching MagicQuiverTrinketEffect.Update: {ex}", LogLevel.Error);
            }

            // 8. Patch IceOrbTrinketEffect.Update for Ice Orb Multi-Target Collision
            try
            {
                var iceOrbUpdateMethod = AccessTools.Method(
                    typeof(IceOrbTrinketEffect),
                    nameof(IceOrbTrinketEffect.Update),
                    new[] { typeof(Farmer), typeof(GameTime), typeof(GameLocation) }
                );

                if (iceOrbUpdateMethod != null)
                {
                    harmony.Patch(
                        original: iceOrbUpdateMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(IceOrbTrinketEffect_Update_Postfix))
                    );
                    Monitor.Log("Hooked IceOrbTrinketEffect.Update successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching IceOrbTrinketEffect.Update: {ex}", LogLevel.Error);
            }

            // 8. Patch Projectile.update for Magic Quiver Pierce & Execution
            try
            {
                var projectileUpdateMethod = AccessTools.Method(
                    typeof(Projectile),
                    nameof(Projectile.update),
                    new[] { typeof(GameTime), typeof(GameLocation) }
                );

                if (projectileUpdateMethod != null)
                {
                    harmony.Patch(
                        original: projectileUpdateMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Projectile_update_Postfix))
                    );
                    Monitor.Log("Hooked Projectile.update for Magic Quiver Piercing & Execute successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Projectile.update: {ex}", LogLevel.Error);
            }

            // 9. Patch HungryFrogCompanion for Loot Drop and Cooldown Reset
            try
            {
                var tongueReachedMethod = AccessTools.Method(
                    typeof(HungryFrogCompanion),
                    nameof(HungryFrogCompanion.tongueReachedMonster),
                    new[] { typeof(Monster) }
                );
                if (tongueReachedMethod != null)
                {
                    harmony.Patch(
                        original: tongueReachedMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(HungryFrogCompanion_tongueReachedMonster_Postfix))
                    );
                    Monitor.Log("Hooked HungryFrogCompanion.tongueReachedMonster successfully.", LogLevel.Trace);
                }

                var fullnessTimerMethod = AccessTools.Method(
                    typeof(HungryFrogCompanion),
                    "triggerFullnessTimer"
                );
                if (fullnessTimerMethod != null)
                {
                    harmony.Patch(
                        original: fullnessTimerMethod,
                        prefix: new HarmonyMethod(typeof(TrinketPatches), nameof(HungryFrogCompanion_triggerFullnessTimer_Prefix)),
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(HungryFrogCompanion_triggerFullnessTimer_Postfix))
                    );
                    Monitor.Log("Hooked HungryFrogCompanion.triggerFullnessTimer successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching HungryFrogCompanion: {ex}", LogLevel.Error);
            }

            // 10. Patch FairyBoxTrinketEffect.Update for Ally Heal & +1 Defense Blessing
            try
            {
                var fairyUpdateMethod = AccessTools.Method(
                    typeof(FairyBoxTrinketEffect),
                    nameof(FairyBoxTrinketEffect.Update),
                    new[] { typeof(Farmer), typeof(GameTime), typeof(GameLocation) }
                );

                if (fairyUpdateMethod != null)
                {
                    harmony.Patch(
                        original: fairyUpdateMethod,
                        prefix: new HarmonyMethod(typeof(TrinketPatches), nameof(FairyBoxTrinketEffect_Update_Prefix)),
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(FairyBoxTrinketEffect_Update_Postfix))
                    );
                    Monitor.Log("Hooked FairyBoxTrinketEffect.Update successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching FairyBoxTrinketEffect.Update: {ex}", LogLevel.Error);
            }

            // 11. Patch Item.canStackWith to protect Ascension and Stats
            try
            {
                var canStackMethod = AccessTools.Method(
                    typeof(Item),
                    nameof(Item.canStackWith),
                    new[] { typeof(ISalable) }
                );

                if (canStackMethod != null)
                {
                    harmony.Patch(
                        original: canStackMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Item_canStackWith_Postfix))
                    );
                    Monitor.Log("Hooked Item.canStackWith successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Item.canStackWith: {ex}", LogLevel.Error);
            }

            // 12. Patch Item.getOne to preserve Ascension and ModData
            try
            {
                var getOneMethod = AccessTools.Method(
                    typeof(Item),
                    nameof(Item.getOne)
                );

                if (getOneMethod != null)
                {
                    harmony.Patch(
                        original: getOneMethod,
                        postfix: new HarmonyMethod(typeof(TrinketPatches), nameof(Item_getOne_Postfix))
                    );
                    Monitor.Log("Hooked Item.getOne successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching Item.getOne: {ex}", LogLevel.Error);
            }

            // 13. Patch GameLocation.damageMonster for Spur +10% Crit Chance
            try
            {
                var damageMonsterMethod = AccessTools.Method(
                    typeof(GameLocation),
                    nameof(GameLocation.damageMonster),
                    new[] {
                        typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float),
                        typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool)
                    }
                );

                if (damageMonsterMethod != null)
                {
                    harmony.Patch(
                        original: damageMonsterMethod,
                        prefix: new HarmonyMethod(typeof(TrinketPatches), nameof(GameLocation_damageMonster_Prefix))
                    );
                    Monitor.Log("Hooked GameLocation.damageMonster for Spur Crit Chance successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching GameLocation.damageMonster: {ex}", LogLevel.Error);
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

        public static bool ProcessAnvilInteraction(StardewValley.Object anvil, Trinket trinket, Farmer who)
        {
            if (anvil == null || trinket == null || who == null)
                return false;

            int stackSize = Math.Max(1, trinket.Stack);
            int iridiumCount = who.Items.CountId("(O)337") + who.Items.CountId("337");
            int shardCount = who.Items.CountId("(O)74") + who.Items.CountId("74");
            int totalIridiumRequired = stackSize * Config.IridiumBarCost;
            int totalShardsRequired = stackSize * 1;

            // Case 1: Player has BOTH Iridium Bars and Prismatic Shards -> Prompt warning toast
            if (iridiumCount >= totalIridiumRequired && shardCount >= totalShardsRequired)
            {
                who.currentLocation?.playSound("cancel");
                Game1.showRedMessage(ModEntry.I18n.Get("message.cannot-both"));
                return false;
            }

            // Case 2: Player has ONLY Iridium Bars -> Reforge / Level Up
            if (iridiumCount >= totalIridiumRequired)
            {
                var eval = TrinketReforgeLogic.Evaluate(trinket.ItemId, trinket.generationSeed.Value);
                if (eval.IsMaxRoll)
                {
                    who.currentLocation?.playSound("cancel");
                    Game1.showRedMessage(ModEntry.I18n.Get("message.already-max-tier"));
                    return false;
                }

                who.Items.ReduceId("(O)337", totalIridiumRequired);
                TrinketReforgeLogic.ProcessReforge(trinket, who, Config);

                who.currentLocation?.playSound("furnace");
                who.currentLocation?.playSound("hammer");
                Game1.createRadialDebris(who.currentLocation, 12, (int)anvil.TileLocation.X * 64 + 32, (int)anvil.TileLocation.Y * 64 + 32, 6, false);
                return true;
            }

            // Case 3: Player has ONLY Prismatic Shards -> Ascension
            if (shardCount >= totalShardsRequired)
            {
                if (TrinketAscensionLogic.IsAscended(trinket))
                {
                    who.currentLocation?.playSound("cancel");
                    Game1.showRedMessage(ModEntry.I18n.Get("message.already-ascended"));
                    return false;
                }

                who.Items.ReduceId("(O)74", totalShardsRequired);
                TrinketAscensionLogic.AscendTrinketDirect(trinket, who);

                Game1.createRadialDebris(who.currentLocation, 12, (int)anvil.TileLocation.X * 64 + 32, (int)anvil.TileLocation.Y * 64 + 32, 8, false);
                return true;
            }

            // Case 4: Not enough materials
            who.currentLocation?.playSound("cancel");
            if (stackSize > 1)
            {
                if (iridiumCount >= Config.IridiumBarCost && iridiumCount < totalIridiumRequired)
                {
                    Game1.showRedMessage(ModEntry.I18n.Get("message.need-stack-iridium", new { count = totalIridiumRequired, stack = stackSize }));
                }
                else if (shardCount >= 1 && shardCount < totalShardsRequired)
                {
                    Game1.showRedMessage(ModEntry.I18n.Get("message.need-stack-prismatic", new { count = totalShardsRequired, stack = stackSize }));
                }
                else
                {
                    Game1.showRedMessage(ModEntry.I18n.Get("message.need-stack-materials", new { count = totalIridiumRequired, shards = totalShardsRequired, stack = stackSize }));
                }
            }
            else
            {
                Game1.showRedMessage(ModEntry.I18n.Get("message.need-materials", new { count = totalIridiumRequired }));
            }
            return false;
        }

        public static bool Object_checkForAction_Prefix(
            StardewValley.Object __instance,
            Farmer who,
            bool justCheckingForActivity,
            ref bool __result)
        {
            if (__instance == null || !IsAnvil(__instance) || who == null)
                return true;

            if (justCheckingForActivity)
            {
                __result = true;
                return false;
            }

            if (who.ActiveItem is Trinket trinket)
            {
                ProcessAnvilInteraction(__instance, trinket, who);
                who.ignoreItemConsumptionThisFrame = true;
                __result = true;
                return false;
            }
            else
            {
                who.currentLocation?.playSound("cancel");
                Game1.showRedMessage(ModEntry.I18n.Get("message.need-trinket-in-hand"));
                __result = true;
                return false;
            }
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

            if (probe)
            {
                __result = dropInItem is Trinket;
                return false;
            }

            if (dropInItem is Trinket trinket)
            {
                ProcessAnvilInteraction(__instance, trinket, who);
                who.ignoreItemConsumptionThisFrame = true;
                __result = false;
                return false;
            }

            __result = false;
            return false;
        }

        public static void Item_canStackWith_Postfix(Item __instance, ISalable other, ref bool __result)
        {
            if (__instance is not Trinket thisTrinket || other is not Trinket otherTrinket)
                return;

            bool sameSeed = thisTrinket.generationSeed.Value == otherTrinket.generationSeed.Value;
            bool sameAscension = TrinketAscensionLogic.IsAscended(thisTrinket) == TrinketAscensionLogic.IsAscended(otherTrinket);
            bool sameId = thisTrinket.QualifiedItemId == otherTrinket.QualifiedItemId;

            if (!sameId || !sameSeed || !sameAscension)
            {
                __result = false;
            }
        }

        public static void Item_getOne_Postfix(Item __instance, ref Item __result)
        {
            if (__instance is not Trinket sourceTrinket || __result is not Trinket resultTrinket)
                return;

            resultTrinket.generationSeed.Value = sourceTrinket.generationSeed.Value;

            if (TrinketAscensionLogic.IsAscended(sourceTrinket))
            {
                resultTrinket.modData[TrinketAscensionLogic.AscensionKey] = "true";
            }

            if (sourceTrinket.modData.TryGetValue(TrinketReforgeLogic.ReforgeCountKey, out string? count) || sourceTrinket.modData.TryGetValue(TrinketReforgeLogic.LegacyReforgeCountKey, out count))
            {
                resultTrinket.modData[TrinketReforgeLogic.ReforgeCountKey] = count;
            }
        }

        public static void Trinket_getDescription_Postfix(Trinket __instance, ref string __result)
        {
            if (__instance == null)
                return;

            try
            {
                if (TrinketAscensionLogic.IsAscended(__instance))
                {
                    string badge = ModEntry.I18n.Get("tooltip.ascended-badge");
                    string baseLuck = ModEntry.I18n.Get("tooltip.ascended-base-luck");
                    string? desc = TrinketAscensionLogic.GetAscensionDescription(__instance);

                    if (!string.IsNullOrEmpty(desc))
                    {
                        if (!__result.Contains(badge) && !__result.Contains(desc))
                        {
                            string wrappedDesc = Game1.smallFont != null
                                ? Game1.parseText("✦ " + desc, Game1.smallFont, 320)
                                : "✦ " + desc;

                            __result += $"\n\n{badge}\n{baseLuck}\n{wrappedDesc}";
                        }
                    }
                    else if (!__result.Contains(badge) && !__result.Contains(baseLuck))
                    {
                        __result += $"\n\n{badge}\n{baseLuck}";
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error formatting trinket tooltip: {ex}", LogLevel.Trace);
            }
        }

        public static void Trinket_loadDisplayName_Postfix(Trinket __instance, ref string __result)
        {
            if (__instance == null || string.IsNullOrEmpty(__result))
                return;

            try
            {
                // When trinket is both Maxed (Max Tier / Perfect Stats) AND Ascended, add "Perfect" prefix/suffix
                if (TrinketAscensionLogic.IsAscended(__instance))
                {
                    var eval = TrinketReforgeLogic.Evaluate(__instance.ItemId, __instance.generationSeed.Value);
                    if (eval.IsMaxRoll)
                    {
                        string baseName = __result;
                        if (baseName.StartsWith("Perfect ", StringComparison.OrdinalIgnoreCase))
                        {
                            baseName = baseName.Substring(8).Trim();
                        }
                        if (baseName.EndsWith(" Hoàn Hảo", StringComparison.OrdinalIgnoreCase))
                        {
                            baseName = baseName.Substring(0, baseName.Length - 9).Trim();
                        }

                        __result = ModEntry.I18n.Get("trinket.perfect-name-format", new { name = baseName });
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error formatting trinket display name: {ex}", LogLevel.Trace);
            }
        }

        // Patch GameLocation.damageMonster to grant +10% Crit Chance for Ascended Spur
        public static void GameLocation_damageMonster_Prefix(Farmer who, ref float critChance)
        {
            if (who != null && (TrinketAscensionLogic.HasAscendedTrinket(who, "spur") || TrinketAscensionLogic.HasAscendedTrinket(who, "goldenspur") || TrinketAscensionLogic.HasAscendedTrinket(who, "iridiumspur") || TrinketAscensionLogic.HasAscendedTrinket(who, "iridium")))
            {
                critChance += 0.10f; // +10% Critical Strike Chance
            }
        }

        // Hooked directly from Trinket.OnDamageMonster (called on weapon/tool hits)
        public static void Trinket_OnDamageMonster_Postfix(Trinket __instance, Farmer farmer, Monster monster, int damageAmount, bool isBomb, bool isCriticalHit)
        {
            if (farmer == null || monster == null || damageAmount <= 0) return;

            // 1. Golden / Iridium Spur: Crit Damage & Attack Buff
            if (isCriticalHit && (TrinketAscensionLogic.HasAscendedTrinket(farmer, "spur") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "golden") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "iridium")))
            {
                TrinketAscensionLogic.TriggerGoldenSpurCritBonus(farmer, monster, damageAmount);
            }

            // 2. Ice Rod: Shatter Strike & Frost Slow Wave on Frozen Monsters
            if (monster.stunTime.Value > 50 && (TrinketAscensionLogic.HasAscendedTrinket(farmer, "ice") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "rod")))
            {
                TrinketAscensionLogic.TriggerIceShatterAndSlowNearby(monster, farmer);
            }

            // 3. Basilisk Paw: 20% Lifesteal on Hit
            if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "basilisk") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "paw"))
            {
                TrinketAscensionLogic.TriggerBasiliskLifesteal(farmer, damageAmount);
            }

            // 4. Parrot Egg: 2x Gold Coins & 35% Chance for Extra Loot Drop
            if (monster.Health <= 0 || monster.Health <= damageAmount)
            {
                if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "parrot"))
                {
                    monster.objectsToDrop.Add("GoldCoin");
                    monster.objectsToDrop.Add("GoldCoin");
                    TrinketAscensionLogic.TriggerParrotBonusLoot(monster, farmer);
                }
            }
        }

        // Hooked from Farmer.takeDamage (void return type)
        public static void Farmer_takeDamage_Prefix(Farmer __instance, int damage, bool overrideParry, Monster damager, out bool __state)
        {
            __state = __instance != null && damager != null && __instance.CanBeDamaged() && !damager.isInvincible();
        }

        public static void Farmer_takeDamage_Postfix(Farmer __instance, int damage, bool overrideParry, Monster damager, bool __state)
        {
            if (!__state || __instance == null || damager == null || damage <= 0) return;

            // Basilisk Paw: Reflect 50% damage back to attacking monster
            if (TrinketAscensionLogic.HasAscendedTrinket(__instance, "basilisk") || TrinketAscensionLogic.HasAscendedTrinket(__instance, "paw"))
            {
                TrinketAscensionLogic.TriggerDamageReflect(damager, __instance, damage);
            }
        }

        // Magic Quiver arrow infinite piercing (each enemy damaged at most once per arrow)
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Projectile, System.Collections.Generic.HashSet<Monster>> _arrowHitMonsters = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Projectile, System.Collections.Generic.HashSet<Monster>> _iceOrbHitMonsters = new();

        public static void MagicQuiverTrinketEffect_Update_Postfix(MagicQuiverTrinketEffect __instance, Farmer farmer, GameLocation location)
        {
            if (__instance == null || farmer == null || location == null) return;

            if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "quiver") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "magicquiver"))
            {
                foreach (var proj in location.projectiles)
                {
                    if (proj is BasicProjectile bp && bp.projectileID.Value == 14 && bp.theOneWhoFiredMe.Get(location) == farmer)
                    {
                        bp.ignoreCharacterCollisions.Value = true;
                        bp.piercesLeft.Value = 99999;
                    }
                }
            }
        }

        public static void IceOrbTrinketEffect_Update_Postfix(IceOrbTrinketEffect __instance, Farmer farmer, GameLocation location)
        {
            if (__instance == null || farmer == null || location == null) return;

            if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "icerod") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "iceorb"))
            {
                foreach (var proj in location.projectiles)
                {
                    if (proj is DebuffingProjectile dp && dp.debuff.Value == "frozen" && dp.theOneWhoFiredMe.Get(location) == farmer)
                    {
                        dp.ignoreCharacterCollisions.Value = true;
                        dp.piercesLeft.Value = 99999;
                    }
                }
            }
        }

        public static void Projectile_update_Postfix(Projectile __instance, GameTime time, GameLocation location, ref bool __result)
        {
            if (__instance == null || location == null) return;

            // 1. Magic Quiver Arrow: Multi-target sweeping piercing & execution
            if (__instance is BasicProjectile bp && bp.projectileID.Value == 14)
            {
                Farmer? farmer = bp.GetPlayerWhoFiredMe(location);
                if (farmer != null && (TrinketAscensionLogic.HasAscendedTrinket(farmer, "quiver") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "magicquiver")))
                {
                    bp.ignoreCharacterCollisions.Value = true;
                    bp.piercesLeft.Value = 99999;

                    Rectangle arrowBounds = bp.getBoundingBox();
                    var hitList = _arrowHitMonsters.GetOrCreateValue(bp);

                    for (int i = 0; i < location.characters.Count; i++)
                    {
                        if (location.characters[i] is Monster monster && !monster.IsInvisible && arrowBounds.Intersects(monster.GetBoundingBox()))
                        {
                            if (hitList.Add(monster))
                            {
                                location.damageMonster(monster.GetBoundingBox(), bp.damageToFarmer.Value, bp.damageToFarmer.Value + 1, false, farmer, true);
                                TrinketAscensionLogic.TriggerQuiverExecute(monster, farmer, location);
                                location.playSound("hitEnemy");
                                Game1.createRadialDebris(location, 12, (int)monster.Position.X + 32, (int)monster.Position.Y + 32, 4, false);
                            }
                        }
                    }
                }
            }

            // 2. Ice Rod Orb: Multi-target sweeping freeze with expanded hitbox
            if (__instance is DebuffingProjectile dp && dp.debuff.Value == "frozen")
            {
                Farmer? farmer = dp.theOneWhoFiredMe.Get(location) as Farmer;
                if (farmer != null && (TrinketAscensionLogic.HasAscendedTrinket(farmer, "icerod") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "iceorb")))
                {
                    dp.ignoreCharacterCollisions.Value = true;
                    dp.piercesLeft.Value = 99999;

                    Rectangle orbBounds = dp.getBoundingBox();
                    orbBounds.Inflate(12, 12);
                    var hitList = _iceOrbHitMonsters.GetOrCreateValue(dp);

                    for (int i = 0; i < location.characters.Count; i++)
                    {
                        if (location.characters[i] is Monster monster && !monster.IsInvisible && !monster.isInvincible() && orbBounds.Intersects(monster.GetBoundingBox()))
                        {
                            if (hitList.Add(monster))
                            {
                                int freezeDuration = dp.debuffIntensity.Value > 0 ? dp.debuffIntensity.Value : 4000;
                                monster.stunTime.Value = freezeDuration;
                                location.playSound("frozen");

                                location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(118, 227, 16, 13), new Vector2(0f, 0f), flipped: false, 0f, Color.White)
                                {
                                    layerDepth = (float)(monster.StandingPixel.Y + 2) / 10000f,
                                    animationLength = 1,
                                    interval = freezeDuration,
                                    scale = 4f,
                                    id = (int)(monster.position.X * 777f + monster.position.Y * 77777f),
                                    positionFollowsAttachedCharacter = true,
                                    attachedCharacter = monster
                                });

                                Game1.createRadialDebris(location, 10, (int)monster.Position.X + 32, (int)monster.Position.Y + 32, 6, false);
                            }
                        }
                    }
                }
            }
        }

        // Frog Companion Swallow Loot & Cooldown Reset
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<HungryFrogCompanion, Monster> _frogAttachedMonsters = new();

        public static void HungryFrogCompanion_tongueReachedMonster_Postfix(HungryFrogCompanion __instance, Monster m)
        {
            if (__instance != null && m != null)
            {
                _frogAttachedMonsters.AddOrUpdate(__instance, m);
            }
        }

        public static void HungryFrogCompanion_triggerFullnessTimer_Prefix(HungryFrogCompanion __instance)
        {
            if (__instance == null) return;

            Farmer? owner = __instance.Owner;
            if (owner != null && (TrinketAscensionLogic.HasAscendedTrinket(owner, "frog") || TrinketAscensionLogic.HasAscendedTrinket(owner, "frogegg")))
            {
                if (_frogAttachedMonsters.TryGetValue(__instance, out Monster? m) && m != null)
                {
                    TrinketAscensionLogic.TriggerFrogLootDrop(m, owner);
                    _frogAttachedMonsters.Remove(__instance);
                }
            }
        }

        public static void HungryFrogCompanion_triggerFullnessTimer_Postfix(HungryFrogCompanion __instance)
        {
            if (__instance == null) return;

            Farmer? owner = __instance.Owner;
            if (owner != null && (TrinketAscensionLogic.HasAscendedTrinket(owner, "frog") || TrinketAscensionLogic.HasAscendedTrinket(owner, "frogegg")))
            {
                // 45% chance to immediately reset fullness cooldown
                if (Game1.random.NextDouble() < 0.45)
                {
                    TrinketAscensionLogic.TriggerFrogCooldownReset(__instance, owner);
                }
            }
        }

        // Fairy Box Ally Heal & Defense Blessing
        public static void FairyBoxTrinketEffect_Update_Prefix(FairyBoxTrinketEffect __instance, Farmer farmer, GameTime time, out bool __state)
        {
            __state = false;
            if (__instance != null && farmer != null && time != null)
            {
                if (__instance.HealTimer + (float)time.ElapsedGameTime.TotalMilliseconds >= __instance.HealDelay)
                {
                    __state = true;
                }
            }
        }

        public static void FairyBoxTrinketEffect_Update_Postfix(FairyBoxTrinketEffect __instance, Farmer farmer, bool __state)
        {
            if (__instance == null || farmer == null || !__state) return;

            if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "fairy") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "fairybox"))
            {
                int healAmount = Math.Max(4, (int)(farmer.maxHealth * 0.05f * __instance.Power));
                TrinketAscensionLogic.TriggerFairyAllyHealAndBlessing(farmer, healAmount);
            }
        }
    }
}
