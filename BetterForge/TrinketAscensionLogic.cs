using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Companions;
using StardewValley.Monsters;
using StardewValley.Objects.Trinkets;

namespace BetterForge
{
    public static class TrinketAscensionLogic
    {
        public const string AscensionKey = "feiluvnana.BetterForge/IsAscended";
        public const string LegacyAscensionKey = "feiluvnana.BetterTrinket/IsAscended";
        public const string BaseLuckBuffId = "feiluvnana.BetterForge/AscensionLuck";
        public const string FairyDefenseBuffId = "feiluvnana.BetterForge/FairyDefense";
        public const string SpurAttackBuffId = "iridiumspur";

        private static Microsoft.Xna.Framework.Graphics.Texture2D? _ascensionLuckIcon;
        private static Microsoft.Xna.Framework.Graphics.Texture2D? _fairyDefenseIcon;
        private static Microsoft.Xna.Framework.Graphics.Texture2D? _spurAttackIcon;

        public static Microsoft.Xna.Framework.Graphics.Texture2D GetAscensionLuckIcon()
        {
            return _ascensionLuckIcon ??= ModEntry.ModHelper.ModContent.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/ascension_luck.png");
        }

        public static Microsoft.Xna.Framework.Graphics.Texture2D GetFairyDefenseIcon()
        {
            return _fairyDefenseIcon ??= ModEntry.ModHelper.ModContent.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/fairy_defense.png");
        }

        public static Microsoft.Xna.Framework.Graphics.Texture2D GetSpurAttackIcon()
        {
            return _spurAttackIcon ??= ModEntry.ModHelper.ModContent.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/spur_attack.png");
        }

        public static bool IsAscended(Trinket? trinket)
        {
            if (trinket == null) return false;
            return trinket.modData.ContainsKey(AscensionKey) || trinket.modData.ContainsKey(LegacyAscensionKey);
        }

        public static int CountAscendedTrinkets(Farmer? who)
        {
            if (who == null || who.trinketItems.Count == 0) return 0;

            int count = 0;
            for (int i = 0; i < who.trinketItems.Count; i++)
            {
                if (who.trinketItems[i] is Trinket trinket && IsAscended(trinket))
                {
                    count++;
                }
            }
            return count;
        }

        public static bool HasAscendedTrinket(Farmer? who, string trinketName)
        {
            if (who == null || who.trinketItems.Count == 0) return false;

            for (int i = 0; i < who.trinketItems.Count; i++)
            {
                var item = who.trinketItems[i];
                if (item is Trinket trinket && IsAscended(trinket))
                {
                    string id = (trinket.ItemId + " " + trinket.Name + " " + trinket.QualifiedItemId + " " + trinket.GetType().Name).ToLowerInvariant();
                    if (id.Contains(trinketName.ToLowerInvariant()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string? GetAscensionDescription(Trinket? trinket)
        {
            if (trinket == null) return null;
            string id = (trinket.ItemId + " " + trinket.Name + " " + trinket.QualifiedItemId + " " + trinket.GetType().Name).ToLowerInvariant();
            return GetAscensionDescriptionFromId(id);
        }

        public static string? GetAscensionDescription(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            return GetAscensionDescriptionFromId(itemId.ToLowerInvariant());
        }

        private static string? GetAscensionDescriptionFromId(string cleanId)
        {
            if (cleanId.Contains("frog"))
                return ModEntry.I18n.Get("ascension.frogegg.desc");
            if (cleanId.Contains("fairy"))
                return ModEntry.I18n.Get("ascension.fairybox.desc");
            if (cleanId.Contains("parrot"))
                return ModEntry.I18n.Get("ascension.parrotegg.desc");
            if (cleanId.Contains("spur") || cleanId.Contains("golden") || cleanId.Contains("iridium"))
                return ModEntry.I18n.Get("ascension.goldenspur.desc");
            if (cleanId.Contains("quiver"))
                return ModEntry.I18n.Get("ascension.magicquiver.desc");
            if (cleanId.Contains("ice") || cleanId.Contains("rod"))
                return ModEntry.I18n.Get("ascension.icerod.desc");
            if (cleanId.Contains("basilisk") || cleanId.Contains("paw"))
                return ModEntry.I18n.Get("ascension.basiliskpaw.desc");

            return null;
        }

        public static bool TryAscendTrinket(Trinket trinket, Farmer who)
        {
            if (trinket == null || who == null)
                return false;

            if (IsAscended(trinket))
            {
                Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("message.already-ascended"), 2));
                who.currentLocation?.playSound("cancel");
                return false;
            }

            if (!who.Items.ContainsId("(O)74", 1)) // Prismatic Shard
            {
                Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("message.need-prismatic"), 2));
                who.currentLocation?.playSound("cancel");
                return false;
            }

            who.Items.ReduceId("(O)74", 1);

            return AscendTrinketDirect(trinket, who);
        }

        public static bool AscendTrinketDirect(Trinket trinket, Farmer who)
        {
            if (trinket == null || who == null)
                return false;

            if (IsAscended(trinket))
            {
                return false;
            }

            trinket.modData[AscensionKey] = "true";

            TrinketReforgeLogic.ResetCachedDescription(trinket, who);

            who.currentLocation?.playSound("yoba");
            who.currentLocation?.playSound("reward");

            Game1.addHUDMessage(new HUDMessage(
                ModEntry.I18n.Get("hud.ascension-success", new { item = trinket.DisplayName }),
                1
            ));

            UpdateAscensionLuckBuff(who);

            return true;
        }

        // --- Passive Base Ascension Luck Buff (+0.5 Luck per Ascended Trinket) ---

        public static void UpdateAscensionLuckBuff(Farmer? who)
        {
            if (who == null) return;

            int count = CountAscendedTrinkets(who);
            if (count > 0)
            {
                float luckAmount = count * 0.5f;
                var existingBuff = who.buffs.AppliedBuffs.TryGetValue(BaseLuckBuffId, out var b) ? b : null;

                if (existingBuff == null || Math.Abs(existingBuff.effects.LuckLevel.Value - luckAmount) > 0.01f)
                {
                    var luckBuff = new Buff(
                        id: BaseLuckBuffId,
                        displayName: ModEntry.I18n.Get("buff.ascension-luck.name"),
                        description: ModEntry.I18n.Get("buff.ascension-luck.desc"),
                        iconTexture: GetAscensionLuckIcon(),
                        iconSheetIndex: 0,
                        duration: Buff.ENDLESS,
                        effects: new BuffEffects()
                        {
                            LuckLevel = { luckAmount }
                        }
                    );
                    who.applyBuff(luckBuff);
                }
            }
            else
            {
                if (who.buffs.AppliedBuffs.ContainsKey(BaseLuckBuffId))
                {
                    who.buffs.Remove(BaseLuckBuffId);
                }
            }
        }

        // 1. Frog Egg: Drop monster loot & 45% chance to immediately reset fullness
        public static Monster? GetFrogAttachedMonster(HungryFrogCompanion frog, GameLocation location)
        {
            if (frog == null || location == null) return null;

            try
            {
                var targetField = AccessTools.Field(typeof(HungryFrogCompanion), "targetMonster");
                if (targetField != null)
                {
                    var netRef = targetField.GetValue(frog);
                    if (netRef != null)
                    {
                        var monster = AccessTools.Property(netRef.GetType(), "Value")?.GetValue(netRef) as Monster;
                        if (monster != null) return monster;
                    }
                }
            }
            catch { }

            return null;
        }

        public static bool IsFrogSwallowing(HungryFrogCompanion frog)
        {
            try
            {
                var tongueReturn = AccessTools.Field(typeof(HungryFrogCompanion), "tongueReturn")?.GetValue(frog) as Netcode.NetBool;
                if (tongueReturn?.Value != true) return false;

                var tonguePos = AccessTools.Field(typeof(HungryFrogCompanion), "tonguePosition")?.GetValue(frog) as StardewValley.Network.NetPosition;
                float dist = tonguePos != null ? Vector2.Distance(frog.Position, tonguePos.Value) : 999f;
                var timer = (float)(AccessTools.Field(typeof(HungryFrogCompanion), "tongueOutTimer")?.GetValue(frog) ?? 999f);

                return dist <= 48f || timer <= 0f;
            }
            catch
            {
                return false;
            }
        }

        public static void TriggerFrogLootDrop(Monster monster, Farmer? who)
        {
            if (monster == null || who?.currentLocation == null) return;

            try
            {
                var loc = who.currentLocation;
                Vector2 dropPos = who.Position;

                // 1. Extra drops from monster instance
                var extraDrops = monster.getExtraDropItems();
                if (extraDrops != null)
                {
                    foreach (var item in extraDrops)
                    {
                        if (item != null)
                        {
                            Game1.createItemDebris(item, dropPos, Game1.random.Next(4), loc);
                        }
                    }
                }

                // 2. Objects queued to drop
                if (monster.objectsToDrop.Count > 0)
                {
                    for (int i = 0; i < monster.objectsToDrop.Count; i++)
                    {
                        string dropId = monster.objectsToDrop[i];
                        if (!string.IsNullOrEmpty(dropId))
                        {
                            Item dropItem = ItemRegistry.Create(dropId);
                            if (dropItem != null)
                            {
                                Game1.createItemDebris(dropItem, dropPos, Game1.random.Next(4), loc);
                            }
                        }
                    }
                }

                // 3. Official monster drop table from game data
                loc.monsterDrop(monster, (int)dropPos.X, (int)dropPos.Y, who);

                loc.playSound("coin");
                Game1.createRadialDebris(loc, 12, (int)dropPos.X + 32, (int)dropPos.Y + 32, 6, false);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor?.Log($"Error triggering frog loot drops: {ex}", LogLevel.Trace);
            }
        }

        public static void TriggerFrogCooldownReset(HungryFrogCompanion frog, Farmer who)
        {
            if (frog == null || who == null) return;

            try
            {
                frog.fullnessTime = 0f;
                AccessTools.Field(typeof(HungryFrogCompanion), "monsterEatCheckTimer")?.SetValue(frog, 2000f);
                who.currentLocation?.playSound("croak");
                frog.Hop(2.5f);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor?.Log($"Error resetting frog cooldown: {ex}", LogLevel.Trace);
            }
        }

        // 2. Fairy Box: Guaranteed baseline heal for owner + nearby allies + grant +1 Defense for 15s
        public static void TriggerFairyAllyHealAndBlessing(Farmer who, int healAmount)
        {
            if (who?.currentLocation == null) return;

            // 1. Always guarantee heal for the owner (even out of combat)
            if (who.health < who.maxHealth)
            {
                who.health = Math.Min(who.maxHealth, who.health + healAmount);
                who.currentLocation.debris.Add(new Debris(healAmount, who.getStandingPosition(), Color.Lime, 1f, who));
                who.currentLocation.playSound("fairy_heal");
            }

            // 2. Apply defense buff to owner
            ApplyFairyDefenseBuff(who);

            // 3. Heal nearby allies + apply defense buff to them
            foreach (var farmer in who.currentLocation.farmers)
            {
                if (farmer != null && farmer != who && Vector2.Distance(who.Tile, farmer.Tile) <= 6f)
                {
                    if (farmer.health < farmer.maxHealth)
                    {
                        farmer.health = Math.Min(farmer.maxHealth, farmer.health + healAmount);
                        who.currentLocation.debris.Add(new Debris(healAmount, farmer.getStandingPosition(), Color.Lime, 1f, farmer));
                    }
                    ApplyFairyDefenseBuff(farmer);
                    who.currentLocation.playSound("healSound");
                }
            }
        }

        private static void ApplyFairyDefenseBuff(Farmer farmer)
        {
            if (farmer == null) return;

            var defenseBuff = new Buff(
                id: FairyDefenseBuffId,
                displayName: ModEntry.I18n.Get("buff.fairy-defense.name"),
                description: ModEntry.I18n.Get("buff.fairy-defense.desc"),
                iconTexture: GetFairyDefenseIcon(),
                iconSheetIndex: 0, // Custom 16x16 fairy defense icon
                duration: 15000,   // 15 seconds
                effects: new BuffEffects()
                {
                    Defense = { 1f }
                }
            );
            farmer.applyBuff(defenseBuff);
        }

        // 3. Parrot Egg: 35% chance to drop extra monster loot on defeat
        public static void TriggerParrotBonusLoot(Monster monster, Farmer who)
        {
            if (monster == null || who?.currentLocation == null) return;

            if (Game1.random.NextDouble() < 0.35)
            {
                var extraDrops = monster.getExtraDropItems();
                if (extraDrops != null && extraDrops.Count > 0)
                {
                    Item drop = extraDrops[Game1.random.Next(extraDrops.Count)];
                    if (drop != null)
                    {
                        Game1.createItemDebris(drop, monster.Position, Game1.random.Next(4), who.currentLocation);
                    }
                }
                who.currentLocation.playSound("coin");
                Game1.createRadialDebris(who.currentLocation, 12, (int)monster.Position.X + 32, (int)monster.Position.Y + 32, 6, false);
            }
        }

        // 4. Golden Spur: +10% Crit Chance & +3 Attack during speed boost (Unified single Spur buff)
        public static void TriggerGoldenSpurCritBonus(Farmer who, Monster monster, int baseDamage)
        {
            if (who == null) return;

            if (who.buffs.AppliedBuffs.ContainsKey("feiluvnana.BetterForge/SpurAttack"))
            {
                who.buffs.Remove("feiluvnana.BetterForge/SpurAttack");
            }

            int duration = 8000;
            var spurTrinket = who.getFirstTrinketWithID("IridiumSpur");
            if (spurTrinket?.GetEffect() != null)
            {
                duration = spurTrinket.GetEffect().GeneralStat * 1000;
            }

            var unifiedBuff = new Buff(
                id: "iridiumspur",
                displayName: Game1.content.LoadString("Strings\\1_6_Strings:IridiumSpur_Name"),
                description: ModEntry.I18n.Get("buff.spur-attack.desc"),
                iconTexture: GetSpurAttackIcon(),
                iconSheetIndex: 0, // Custom 16x16 spur attack fury icon
                duration: duration,
                effects: new BuffEffects()
                {
                    Speed = { 1f },
                    Attack = { 3f }
                }
            );
            who.applyBuff(unifiedBuff);
        }

        // 5. Ice Rod: Shatter ice + Frost Shockwave (30% Attack) + Frost Slow (Does not destroy rocks/ores)
        public static void TriggerIceShatterAndSlowNearby(Monster monster, Farmer who)
        {
            if (monster == null || who?.currentLocation == null) return;

            var location = who.currentLocation;

            // 1. Remove freeze effect and delete ice block/puddle sprite under monster
            monster.stunTime.Value = 0;
            for (int i = location.temporarySprites.Count - 1; i >= 0; i--)
            {
                var sprite = location.temporarySprites[i];
                if (sprite.attachedCharacter == monster || sprite.id == (int)(monster.position.X * 777f + monster.position.Y * 77777f))
                {
                    location.temporarySprites.RemoveAt(i);
                }
            }

            // 2. Audio & Ice Debris Visual
            location.playSound("glassBreak");
            location.playSound("frozen");
            location.playSound("explosion");
            Rumble.rumbleAndFade(0.6f, 300f);

            Game1.createRadialDebris(location, 10, (int)monster.Position.X + 32, (int)monster.Position.Y + 32, 10, false);

            // 3. Ice-Colored Explosion Visual (Cyan / Frost Blue Shockwave & Blasts)
            location.temporarySprites.Add(new TemporaryAnimatedSprite(362, 50f, 6, 1, monster.Position, false, false)
            {
                color = new Color(130, 230, 255) * 0.65f,
                scale = 3.5f
            });

            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = Utility.getRandom360degreeVector(Game1.random.Next(20, 48));
                location.temporarySprites.Add(new TemporaryAnimatedSprite(362, Game1.random.Next(45, 75), 6, 1, monster.Position + offset, false, Game1.random.Next(2) == 0)
                {
                    color = new Color(160, 240, 255) * 0.45f,
                    scale = 2.4f,
                    delayBeforeAnimationStart = Game1.random.Next(30, 100)
                });
            }

            // 4. Calculate Explosion Damage = 30% Player Attack Power
            int playerAttack = 40;
            if (who.CurrentTool is StardewValley.Tools.MeleeWeapon weapon)
            {
                playerAttack = (weapon.minDamage.Value + weapon.maxDamage.Value) / 2;
            }
            playerAttack += (int)(who.buffs.Attack * 5);
            int explosionDamage = Math.Max(15, (int)(playerAttack * 0.30f));

            // 5. Deal Explosion Damage + Display Cyan Damage Debris + Frost Slow to all nearby monsters (within 3.5 tiles)
            Vector2 centerTile = monster.Tile;
            foreach (var character in location.characters)
            {
                if (character is Monster nearbyMonster && nearbyMonster != monster)
                {
                    if (Vector2.Distance(centerTile, nearbyMonster.Tile) <= 3.5f)
                    {
                        nearbyMonster.takeDamage(explosionDamage, 0, 0, false, 1.0, "hitEnemy");
                        location.debris.Add(new Debris(explosionDamage, nearbyMonster.getStandingPosition(), Color.Cyan, 1f, nearbyMonster));

                        nearbyMonster.addedSpeed = -2;
                        nearbyMonster.stunTime.Value = Math.Max(nearbyMonster.stunTime.Value, 1200);
                        Game1.createRadialDebris(location, 10, (int)nearbyMonster.Position.X + 32, (int)nearbyMonster.Position.Y + 32, 4, false);
                    }
                }
            }
        }

        // 7. Basilisk Paw: Reflect 50% damage & 20% lifesteal on hit (heal 3-8 HP)
        public static void TriggerDamageReflect(Monster attacker, Farmer victim, int incomingDamage)
        {
            if (attacker == null || victim == null || incomingDamage <= 0 || victim.currentLocation == null) return;

            var location = victim.currentLocation;
            int reflectDamage = Math.Max(1, (int)(incomingDamage * 0.5f));
            Vector2 trajectory = Utility.getAwayFromPlayerTrajectory(attacker.GetBoundingBox(), victim);
            attacker.takeDamage(reflectDamage, (int)trajectory.X, (int)trajectory.Y, false, 1.0, "hitEnemy");
            
            // Display visible orange floating damage number on attacker
            location.debris.Add(new Debris(reflectDamage, attacker.getStandingPosition(), Color.Orange, 1f, attacker));

            location.playSound("hitEnemy");
            Game1.createRadialDebris(location, 12, (int)attacker.Position.X + 32, (int)attacker.Position.Y + 32, 6, false);
        }

        public static void TriggerBasiliskLifesteal(Farmer who, int damageDealt)
        {
            if (who == null || damageDealt <= 0) return;

            if (Game1.random.NextDouble() < 0.20)
            {
                int healAmount = Math.Clamp((int)(damageDealt * 0.08f), 3, 8);
                if (who.health < who.maxHealth)
                {
                    who.health = Math.Min(who.maxHealth, who.health + healAmount);
                    who.currentLocation?.playSound("healSound");
                }
            }
        }
    }
}
