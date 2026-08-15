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
            try
            {
                // Patch Anvil check for action (right-click / activate)
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

                // Patch Trinket.OnDamageMonster (called directly by GameLocation.damageMonster on every hit!)
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

                // Patch Projectile collision for Magic Quiver Pierce & Execution
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
                    Monitor.Log("Hooked BasicProjectile for Magic Quiver Piercing & Execute successfully.", LogLevel.Trace);
                }

                // Patch HungryFrogCompanion.Update for Loot Drop and Cooldown Reset
                var frogUpdateMethod = AccessTools.Method(
                    typeof(HungryFrogCompanion),
                    nameof(HungryFrogCompanion.Update),
                    new[] { typeof(GameTime), typeof(GameLocation) }
                );

                if (frogUpdateMethod != null)
                {
                    harmony.Patch(
                        original: frogUpdateMethod,
                        prefix: new HarmonyMethod(typeof(TrinketPatches), nameof(HungryFrogCompanion_Update_Prefix))
                    );
                    Monitor.Log("Hooked HungryFrogCompanion.Update successfully.", LogLevel.Trace);
                }

                // Patch FairyBoxTrinketEffect.Update for Ally Heal & +1 Defense Blessing
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

                // Patch Item.canStackWith to protect Ascension and Stats
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

                // Patch Item.getOne to preserve Ascension and ModData
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
                        if (!__result.Contains(desc))
                        {
                            __result += $"\n\n{badge}\n{baseLuck}\n✦ {desc}";
                        }
                    }
                    else if (!__result.Contains(baseLuck))
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

        // Hooked directly from Trinket.OnDamageMonster (called on every weapon/tool hit against monsters)
        public static void Trinket_OnDamageMonster_Postfix(Trinket __instance, Farmer farmer, Monster monster, int damageAmount, bool isBomb, bool isCriticalHit)
        {
            if (farmer == null || monster == null || damageAmount <= 0) return;

            // 1. Golden Spur: Crit Damage & Attack Buff
            if (isCriticalHit && (TrinketAscensionLogic.HasAscendedTrinket(farmer, "GoldenSpur") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "spur")))
            {
                TrinketAscensionLogic.TriggerGoldenSpurCritBonus(farmer, monster, damageAmount);
            }

            // 2. Ice Rod: Shatter Strike & Frost Slow Wave on Frozen Monsters
            if (monster.stunTime.Value > 0 && (TrinketAscensionLogic.HasAscendedTrinket(farmer, "IceRod") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "ice")))
            {
                TrinketAscensionLogic.TriggerIceShatterAndSlowNearby(monster, farmer);
            }

            // 3. Basilisk Paw: 20% Lifesteal on Hit
            if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "BasiliskPaw") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "basilisk"))
            {
                TrinketAscensionLogic.TriggerBasiliskLifesteal(farmer, damageAmount);
            }

            // 4. Parrot Egg: 25% Chance for Extra Loot Drop on Monster Kill
            if (monster.Health <= 0 || monster.Health <= damageAmount)
            {
                if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "ParrotEgg") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "parrot"))
                {
                    TrinketAscensionLogic.TriggerParrotBonusLoot(monster, farmer);
                }
            }
        }

        public static void Farmer_takeDamage_Postfix(Farmer __instance, int damage, bool fromBomb, Monster damager, int __result)
        {
            if (__instance == null || damager == null || damage <= 0) return;

            // Basilisk Paw: Reflect 50% damage back to attacking monster
            if (TrinketAscensionLogic.HasAscendedTrinket(__instance, "BasiliskPaw") || TrinketAscensionLogic.HasAscendedTrinket(__instance, "basilisk"))
            {
                TrinketAscensionLogic.TriggerDamageReflect(damager, __instance, damage);
            }
        }

        public static void BasicProjectile_behaviorOnCollisionWithMonster_Postfix(BasicProjectile __instance, NPC n, GameLocation location)
        {
            if (__instance == null || location == null) return;

            Farmer? farmer = __instance.GetPlayerWhoFiredMe(location) ?? Game1.player;
            if (farmer != null && (TrinketAscensionLogic.HasAscendedTrinket(farmer, "MagicQuiver") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "quiver")))
            {
                // Guarantee arrow pierces infinitely through all monsters
                __instance.piercesLeft.Value = 99999;

                // Execute low-HP monsters below 15% HP or <= 30 HP
                if (n is Monster monster)
                {
                    TrinketAscensionLogic.TriggerQuiverExecute(monster, farmer, location);
                }
            }
        }

        public static void HungryFrogCompanion_Update_Prefix(HungryFrogCompanion __instance, GameTime time, GameLocation location)
        {
            if (__instance == null || location == null) return;

            Farmer? owner = __instance.Owner;
            if (owner == null) return;

            Monster? attached = TrinketAscensionLogic.GetFrogAttachedMonster(__instance, location);
            if (attached != null)
            {
                var tongueReturn = AccessTools.Field(typeof(HungryFrogCompanion), "tongueReturn")?.GetValue(__instance) as Netcode.NetBool;
                if (tongueReturn?.Value == true)
                {
                    var tonguePos = AccessTools.Field(typeof(HungryFrogCompanion), "tonguePosition")?.GetValue(__instance) as StardewValley.Network.NetPosition;
                    float dist = tonguePos != null ? Vector2.Distance(__instance.Position, tonguePos.Value) : 0f;
                    if (dist <= 48f)
                    {
                        if (TrinketAscensionLogic.HasAscendedTrinket(owner, "FrogEgg") || TrinketAscensionLogic.HasAscendedTrinket(owner, "frog"))
                        {
                            TrinketAscensionLogic.TriggerFrogLootDrop(attached, owner);
                            TrinketAscensionLogic.TriggerFrogCooldownReset(__instance, owner);
                        }
                    }
                }
            }
        }

        private static float _prevFairyTimer = 0f;
        private static int _prevFairyHealth = 0;

        public static void FairyBoxTrinketEffect_Update_Prefix(FairyBoxTrinketEffect __instance, Farmer farmer)
        {
            if (__instance != null && farmer != null)
            {
                _prevFairyTimer = __instance.HealTimer;
                _prevFairyHealth = farmer.health;
            }
        }

        public static void FairyBoxTrinketEffect_Update_Postfix(FairyBoxTrinketEffect __instance, Farmer farmer)
        {
            if (__instance == null || farmer == null) return;

            // Detect if a heal pulse just fired (HealTimer reset or health restored)
            bool healTriggered = (_prevFairyTimer <= 100f && __instance.HealTimer >= __instance.HealDelay - 150f)
                              || (farmer.health > _prevFairyHealth && _prevFairyHealth > 0);

            if (healTriggered)
            {
                if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "FairyBox") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "fairy"))
                {
                    int healAmount = Math.Max(2, (int)(farmer.maxHealth * 0.05f * __instance.Power));
                    TrinketAscensionLogic.TriggerFairyAllyHealAndBlessing(farmer, healAmount);
                }
            }
        }
    }
}
