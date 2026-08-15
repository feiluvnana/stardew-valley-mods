using System;
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
        public const string SpurAttackBuffId = "feiluvnana.BetterForge/SpurAttack";

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

            // Consume 1 Prismatic Shard
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

            // Mark as Ascended
            trinket.modData[AscensionKey] = "true";

            // Clear cached tooltip so description updates immediately
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
                        description: ModEntry.I18n.Get("buff.ascension-luck.desc", new { luck = luckAmount }),
                        iconTexture: Game1.buffsIcons,
                        iconSheetIndex: 4, // 4-leaf clover Luck icon
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

        // --- In-Combat Ascension Effects ---

        // 1. Frog Egg: 35% chance to immediately reset tongue / fullness cooldown
        public static void TriggerFrogCooldownReset(HungryFrogCompanion frog, Farmer who)
        {
            if (frog == null || who == null) return;

            if (Game1.random.NextDouble() < 0.35)
            {
                frog.fullnessTime = 0f;
                AccessTools.Field(typeof(HungryFrogCompanion), "monsterEatCheckTimer")?.SetValue(frog, 0f);
                who.currentLocation?.playSound("croak");
                frog.Hop(2.5f);
            }
        }

        // 2. Fairy Box: Heal nearby allies + grant +1 Defense for 15s
        public static void TriggerFairyAllyHealAndBlessing(Farmer who, int healAmount)
        {
            if (who?.currentLocation == null) return;

            // Apply +1 Defense to owner
            ApplyFairyDefenseBuff(who);

            // Heal nearby co-op farmhands and grant +1 Defense to them too
            foreach (var farmer in who.currentLocation.farmers)
            {
                if (farmer != null && farmer != who && Vector2.Distance(who.Tile, farmer.Tile) <= 6f)
                {
                    farmer.health = Math.Min(farmer.maxHealth, farmer.health + healAmount);
                    ApplyFairyDefenseBuff(farmer);
                    who.currentLocation?.playSound("healSound");
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
                iconTexture: Game1.buffsIcons,
                iconSheetIndex: 6, // Defense shield icon
                duration: 15000,   // 15 seconds
                effects: new BuffEffects()
                {
                    Defense = { 1f }
                }
            );
            farmer.applyBuff(defenseBuff);
        }

        // 3. Parrot Egg: 25% chance to drop extra monster loot
        public static void TriggerParrotBonusLoot(Monster monster, Farmer who)
        {
            if (monster == null || who?.currentLocation == null) return;

            if (Game1.random.NextDouble() < 0.25)
            {
                // Spawn bonus monster loot
                var extraDrops = monster.getExtraDropItems();
                if (extraDrops != null && extraDrops.Count > 0)
                {
                    Item drop = extraDrops[Game1.random.Next(extraDrops.Count)];
                    if (drop != null)
                    {
                        Game1.createItemDebris(drop, monster.Position, Game1.random.Next(4), who.currentLocation);
                    }
                }
                who.currentLocation?.playSound("coin");
                Game1.createRadialDebris(who.currentLocation, 12, (int)monster.Position.X + 32, (int)monster.Position.Y + 32, 6, false);
            }
        }

        // 4. Golden Spur: +25% Crit Damage & +3 Attack during speed boost
        public static void TriggerGoldenSpurCritBonus(Farmer who, Monster monster, int baseDamage)
        {
            if (who == null || monster == null || baseDamage <= 0) return;

            // Grant +25% bonus crit damage
            int bonusCritDmg = Math.Max(2, (int)(baseDamage * 0.25f));
            monster.takeDamage(bonusCritDmg, 0, 0, false, 1.0, who);

            // Apply +3 Attack buff alongside speed
            var attackBuff = new Buff(
                id: SpurAttackBuffId,
                displayName: ModEntry.I18n.Get("buff.spur-attack.name"),
                description: ModEntry.I18n.Get("buff.spur-attack.desc"),
                iconTexture: Game1.buffsIcons,
                iconSheetIndex: 0, // Attack sword icon
                duration: 8000,   // 8 seconds
                effects: new BuffEffects()
                {
                    Attack = { 3f }
                }
            );
            who.applyBuff(attackBuff);
        }

        // 5. Magic Quiver: Execute monsters below 15% HP or <= 25 HP
        public static bool TriggerQuiverExecute(Monster monster, Farmer who, GameLocation location)
        {
            if (monster == null || who == null || location == null) return false;

            if (monster.Health > 0)
            {
                bool isLowHp = monster.Health <= (int)(monster.MaxHealth * 0.15f) || monster.Health <= 25;
                if (isLowHp)
                {
                    int fatalDamage = monster.Health + 50;
                    monster.takeDamage(fatalDamage, 0, 0, false, 1.0, who);
                    location.playSound("shadowDie");
                    location.playSound("crit");
                    Game1.createRadialDebris(location, 12, (int)monster.Position.X + 32, (int)monster.Position.Y + 32, 8, false);
                    return true;
                }
            }
            return false;
        }

        // 6. Ice Rod: +35 Shatter damage & Frost wave slowing nearby monsters
        public static void TriggerIceShatterAndSlowNearby(Monster monster, Farmer who)
        {
            if (monster == null || who?.currentLocation == null) return;

            int bonusDamage = 35;
            monster.takeDamage(bonusDamage, 0, 0, false, 1.0, who);
            who.currentLocation.playSound("glassBreak");
            who.currentLocation.playSound("freeze");

            // Frost wave: slow/freeze nearby monsters in a 5-tile radius for 2 seconds
            Vector2 centerTile = monster.Tile;
            foreach (var character in who.currentLocation.characters)
            {
                if (character is Monster nearbyMonster && nearbyMonster != monster)
                {
                    if (Vector2.Distance(centerTile, nearbyMonster.Tile) <= 5f)
                    {
                        nearbyMonster.stunTime.Value = Math.Max(nearbyMonster.stunTime.Value, 2000);
                        Game1.createRadialDebris(who.currentLocation, 10, (int)nearbyMonster.Position.X + 32, (int)nearbyMonster.Position.Y + 32, 4, false);
                    }
                }
            }
        }

        // 7. Basilisk Paw: Reflect 50% damage & 20% lifesteal on hit (heal 3-8 HP)
        public static void TriggerDamageReflect(Monster attacker, Farmer victim, int incomingDamage)
        {
            if (attacker == null || victim == null || incomingDamage <= 0) return;

            int reflectDamage = Math.Max(1, incomingDamage / 2);
            attacker.takeDamage(reflectDamage, 0, 0, false, 1.0, victim);
            victim.currentLocation?.playSound("hitEnemy");
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
