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
    /// <summary>
    /// Implements "Prismatic Ascension": a permanent upgrade for trinkets purchased
    /// with a Prismatic Shard. Ascension is stored as a flag inside the item's
    /// <c>modData</c> (a per-item string dictionary that saves with the game), and each
    /// trinket family gains its own special power plus a shared +0.5 Luck passive.
    /// </summary>
    public static class TrinketAscensionLogic
    {
        // modData keys. modData is saved with the save file, so the ascension flag
        // survives quitting and reloading. The legacy key reads flags written by
        // this feature's older mod name ("BetterTrinket") so old saves keep working.

        /// <summary>modData key marking a trinket as ascended.</summary>
        public const string AscensionKey = "feiluvnana.BetterForge/IsAscended";

        /// <summary>Older modData key kept for migrating saves from the previous mod name.</summary>
        public const string LegacyAscensionKey = "feiluvnana.BetterTrinket/IsAscended";

        /// <summary>ID of the endless +0.5-luck-per-ascended-trinket buff.</summary>
        public const string BaseLuckBuffId = "feiluvnana.BetterForge/AscensionLuck";

        /// <summary>ID of the temporary +1 Defense buff granted by an ascended Fairy Box.</summary>
        public const string FairyDefenseBuffId = "feiluvnana.BetterForge/FairyDefense";

        /// <summary>ID of the speed+attack buff shared with vanilla's Iridium Spur.</summary>
        public const string SpurAttackBuffId = "iridiumspur";

        // Cached icon textures. Loading textures from disk is slow, so we load each
        // once and remember it ("?" means the field may hold null until first use).
        private static Microsoft.Xna.Framework.Graphics.Texture2D? _ascensionLuckIcon;
        private static Microsoft.Xna.Framework.Graphics.Texture2D? _fairyDefenseIcon;
        private static Microsoft.Xna.Framework.Graphics.Texture2D? _spurAttackIcon;

        /// <summary>
        /// Returns the HUD icon for the ascension luck buff, loading it from the mod
        /// folder on first call only. The "??=" operator means: if the field is still
        /// null, assign the right-hand side to it and return that value.
        /// </summary>
        public static Microsoft.Xna.Framework.Graphics.Texture2D GetAscensionLuckIcon()
        {
            return _ascensionLuckIcon ??= ModEntry.ModHelper.ModContent.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/ascension_luck.png");
        }

        /// <summary>Returns (and caches) the fairy defense buff icon texture.</summary>
        public static Microsoft.Xna.Framework.Graphics.Texture2D GetFairyDefenseIcon()
        {
            return _fairyDefenseIcon ??= ModEntry.ModHelper.ModContent.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/fairy_defense.png");
        }

        /// <summary>Returns (and caches) the golden spur attack buff icon texture.</summary>
        public static Microsoft.Xna.Framework.Graphics.Texture2D GetSpurAttackIcon()
        {
            return _spurAttackIcon ??= ModEntry.ModHelper.ModContent.Load<Microsoft.Xna.Framework.Graphics.Texture2D>("assets/spur_attack.png");
        }

        /// <summary>
        /// Checks whether a trinket carries the ascension flag (current or legacy key).
        /// </summary>
        /// <param name="trinket">The trinket to test; null safely counts as not ascended.</param>
        /// <returns>True if the trinket has been ascended at some point.</returns>
        public static bool IsAscended(Trinket? trinket)
        {
            if (trinket == null) return false;
            return trinket.modData.ContainsKey(AscensionKey) || trinket.modData.ContainsKey(LegacyAscensionKey);
        }

        /// <summary>
        /// Counts how many of the player's currently equipped trinkets are ascended.
        /// Used to size the passive luck buff (+0.5 Luck each).
        /// </summary>
        public static int CountAscendedTrinkets(Farmer? who)
        {
            // Early exit when there's nothing to check.
            if (who == null || who.trinketItems.Count == 0) return 0;

            int count = 0;
            for (int i = 0; i < who.trinketItems.Count; i++)
            {
                // "is Trinket trinket" is a pattern match: true only if the slot holds
                // a Trinket, and it also gives us a typed variable to use below.
                if (who.trinketItems[i] is Trinket trinket && IsAscended(trinket))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Searches the player's equipped trinkets for an ASCENDED one whose identity
        /// contains the given name fragment (e.g. "spur"). Identity text combines ID,
        /// display name, qualified ID and class name, so one substring can match any
        /// naming variant the game uses.
        /// </summary>
        public static bool HasAscendedTrinket(Farmer? who, string trinketName)
        {
            if (who == null || who.trinketItems.Count == 0) return false;

            for (int i = 0; i < who.trinketItems.Count; i++)
            {
                var item = who.trinketItems[i];
                if (item is Trinket trinket && IsAscended(trinket))
                {
                    // Build one lowercase "fingerprint" string of every identifier,
                    // then just do a substring search against the requested name.
                    string id = (trinket.ItemId + " " + trinket.Name + " " + trinket.QualifiedItemId + " " + trinket.GetType().Name).ToLowerInvariant();
                    if (id.Contains(trinketName.ToLowerInvariant()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the localized description of what ascension does for this specific
        /// trinket (used in tooltips). Returns null for unsupported trinkets.
        /// </summary>
        public static string? GetAscensionDescription(Trinket? trinket)
        {
            if (trinket == null) return null;
            string id = (trinket.ItemId + " " + trinket.Name + " " + trinket.QualifiedItemId + " " + trinket.GetType().Name).ToLowerInvariant();
            return GetAscensionDescriptionFromId(id);
        }

        /// <summary>Overload that looks up the ascension description from a raw item ID string.</summary>
        public static string? GetAscensionDescription(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            return GetAscensionDescriptionFromId(itemId.ToLowerInvariant());
        }

        /// <summary>
        /// Maps lowercase ID fragments to their translated ascension descriptions.
        /// Checked top-to-bottom; first match wins. Null means "no special power".
        /// </summary>
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

        /// <summary>
        /// Attempts to ascend a trinket the "paid" way: validates preconditions
        /// (not already ascended, player owns a Prismatic Shard), consumes the shard,
        /// then delegates to <see cref="AscendTrinketDirect"/> to do the real work.
        /// </summary>
        /// <returns>True if ascension succeeded.</returns>
        public static bool TryAscendTrinket(Trinket trinket, Farmer who)
        {
            // Null-safety: nothing to do without both a trinket and a player.
            if (trinket == null || who == null)
                return false;

            // Already ascended? Show an info HUD message ("2" = info style) and buzz.
            if (IsAscended(trinket))
            {
                Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("message.already-ascended"), 2));
                who.currentLocation?.playSound("cancel");
                return false;
            }

            // Require 1 Prismatic Shard. "(O)74": "O" = object category, 74 = shard ID.
            // ContainsId counts whether the inventory holds at least that many.
            if (!who.Items.ContainsId("(O)74", 1)) // Prismatic Shard
            {
                Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("message.need-prismatic"), 2));
                who.currentLocation?.playSound("cancel");
                return false;
            }

            // Payment accepted — remove exactly one shard from the inventory.
            who.Items.ReduceId("(O)74", 1);

            return AscendTrinketDirect(trinket, who);
        }

        /// <summary>
        /// Performs the actual ascension with no cost checks (used by both the paid
        /// path above and the Anvil's shard branch): flags the item, refreshes its
        /// tooltip, plays celebratory feedback, and reapplies passive buffs.
        /// </summary>
        public static bool AscendTrinketDirect(Trinket trinket, Farmer who)
        {
            if (trinket == null || who == null)
                return false;

            // Refuse double ascension silently here (callers show their own message).
            if (IsAscended(trinket))
            {
                return false;
            }

            // THE core step: write the flag into modData. This string dictionary is
            // saved inside the save file, so ascension is permanent for this trinket.
            trinket.modData[AscensionKey] = "true";

            // Force tooltips to rebuild so the ascension lines appear immediately.
            TrinketReforgeLogic.ResetCachedDescription(trinket, who);

            // Feedback: two cheerful sounds + a green success HUD message ("1").
            who.currentLocation?.playSound("yoba");
            who.currentLocation?.playSound("reward");

            Game1.addHUDMessage(new HUDMessage(
                ModEntry.I18n.Get("hud.ascension-success", new { item = trinket.DisplayName }),
                1
            ));

            // Recompute the passive luck buff now that one more slot may be ascended.
            UpdateAscensionLuckBuff(who);

            return true;
        }

        // --- Passive Base Ascension Luck Buff (+0.5 Luck per Ascended Trinket) ---

        /// <summary>
        /// Keeps the endless luck buff in sync with equipped ascended trinkets:
        /// applies/updates it when count > 0, removes it otherwise. Called twice a
        /// second from ModEntry's tick handler and right after any ascension.
        /// </summary>
        public static void UpdateAscensionLuckBuff(Farmer? who)
        {
            if (who == null) return;

            int count = CountAscendedTrinkets(who);
            if (count > 0)
            {
                // Each ascended trinket contributes half a point of daily luck.
                float luckAmount = count * 0.5f;

                // Look up any luck buff we previously applied, so we only rebuild it
                // when the amount actually changed (avoids churn every half-second).
                var existingBuff = who.buffs.AppliedBuffs.TryGetValue(BaseLuckBuffId, out var b) ? b : null;

                if (existingBuff == null || Math.Abs(existingBuff.effects.LuckLevel.Value - luckAmount) > 0.01f)
                {
                    // Build a fresh buff. Named arguments (id:, duration:) make this
                    // long constructor call readable. Buff.ENDLESS = never expires.
                    var luckBuff = new Buff(
                        id: BaseLuckBuffId,
                        displayName: ModEntry.I18n.Get("buff.ascension-luck.name"),
                        description: ModEntry.I18n.Get("buff.ascension-luck.desc"),
                        iconTexture: GetAscensionLuckIcon(),
                        iconSheetIndex: 0,
                        duration: Buff.ENDLESS,
                        effects: new BuffEffects()
                        {
                            LuckLevel = { luckAmount } // collection-initializer syntax adds the value
                        }
                    );
                    who.applyBuff(luckBuff);
                }
            }
            else
            {
                // No ascended trinkets equipped anymore — clean up our buff.
                if (who.buffs.AppliedBuffs.ContainsKey(BaseLuckBuffId))
                {
                    who.buffs.Remove(BaseLuckBuffId);
                }
            }
        }

        // 1. Frog Egg: Drop monster loot & 45% chance to immediately reset fullness
        // 1. Frog Egg: Drop monster loot & 45% chance to immediately reset fullness

        /// <summary>
        /// Ascended Frog Egg power, part 1: figures out which monster the hungry frog
        /// is currently targeting so the game can be made to drop its loot.
        /// </summary>
        public static Monster? GetFrogAttachedMonster(HungryFrogCompanion frog, GameLocation location)
        {
            if (frog == null || location == null) return null;

            try
            {
                // The target monster lives in a private field, so we read it with
                // reflection (AccessTools = Harmony's reflection helper). Reflection
                // reaches code the compiler normally hides from us.
                var targetField = AccessTools.Field(typeof(HungryFrogCompanion), "targetMonster");
                if (targetField != null)
                {
                    var netRef = targetField.GetValue(frog);
                    if (netRef != null)
                    {
                        // NetRef<T>.Value unwraps the networked reference to the actual
                        // Monster instance; "as Monster" casts or yields null safely.
                        var monster = AccessTools.Property(netRef.GetType(), "Value")?.GetValue(netRef) as Monster;
                        if (monster != null) return monster;
                    }
                }
            }
            catch { } // Any reflection failure just means "no target" — never crash.

            return null;
        }

        /// <summary>
        /// Ascended Frog Egg power, part 2: detects whether the frog's tongue has
        /// finished grabbing its prey (used to time the loot drop).
        /// </summary>
        public static bool IsFrogSwallowing(HungryFrogCompanion frog)
        {
            try
            {
                // tongueReturn == true means the tongue is heading back with a catch.
                var tongueReturn = AccessTools.Field(typeof(HungryFrogCompanion), "tongueReturn")?.GetValue(frog) as Netcode.NetBool;
                if (tongueReturn?.Value != true) return false;

                // Swallow completes when the tongue tip is back near the frog's body
                // (48 pixels = half a tile) OR its out-timer already ran out.
                var tonguePos = AccessTools.Field(typeof(HungryFrogCompanion), "tonguePosition")?.GetValue(frog) as StardewValley.Network.NetPosition;
                float dist = tonguePos != null ? Vector2.Distance(frog.Position, tonguePos.Value) : 999f;
                var timer = (float)(AccessTools.Field(typeof(HungryFrogCompanion), "tongueOutTimer")?.GetValue(frog) ?? 999f);

                return dist <= 48f || timer <= 0f;
            }
            catch
            {
                return false; // Reflection hiccup — treat as "not swallowing".
            }
        }

        /// <summary>
        /// Makes the game run its normal monster-drop routine for the frog's victim:
        /// coins, items and drop-table loot all spill out at the player's feet.
        /// </summary>
        public static void TriggerFrogLootDrop(Monster monster, Farmer who)
        {
            if (monster == null || who == null || who.currentLocation == null) return;

            try
            {
                var loc = who.currentLocation;
                Vector2 dropPos = who.Position;

                // Official monster drop table from game data (handles extra items and drop table)
                loc.monsterDrop(monster, (int)dropPos.X, (int)dropPos.Y, who);

                loc.playSound("coin");
                // Radial debris of sprite #12 = little golden sparks for flair.
                Game1.createRadialDebris(loc, 12, (int)dropPos.X + 32, (int)dropPos.Y + 32, 6, false);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor?.Log($"Error triggering frog loot drops: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Ascended Frog Egg bonus: skips most of the eating cooldown (45% chance,
        /// rolled by the caller) so the frog can hunt again right away.
        /// </summary>
        public static void TriggerFrogCooldownReset(HungryFrogCompanion frog, Farmer who)
        {
            if (frog == null || who == null) return;

            try
            {
                // Reset fullness to zero and push the next eat-check ~2 seconds out.
                frog.fullnessTime = 0f;
                AccessTools.Field(typeof(HungryFrogCompanion), "monsterEatCheckTimer")?.SetValue(frog, 2000f);
                who.currentLocation?.playSound("croak");
                frog.Hop(2.5f); // A happy little hop as feedback.
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor?.Log($"Error resetting frog cooldown: {ex}", LogLevel.Trace);
            }
        }

        // 2. Fairy Box: Guaranteed baseline heal for owner + nearby allies + grant +1 Defense for 15s

        /// <summary>
        /// Ascended Fairy Box power: every heal pulse now ALWAYS heals the owner,
        /// also heals co-op allies within 6 tiles, and grants everyone touched a
        /// short +1 Defense blessing.
        /// </summary>
        /// <param name="who">The fairy box owner receiving the pulse.</param>
        /// <param name="healAmount">Hit points restored per pulse.</param>
        public static void TriggerFairyAllyHealAndBlessing(Farmer who, int healAmount)
        {
            if (who?.currentLocation == null) return;

            // 1. Always guarantee heal for the owner (even out of combat)
            // Math.Min caps healing at maxHealth so we can't overheal past the bar.
            if (who.health < who.maxHealth)
            {
                who.health = Math.Min(who.maxHealth, who.health + healAmount);
                // Debris = the floating green "+N" number you see when healed.
                who.currentLocation.debris.Add(new Debris(healAmount, who.getStandingPosition(), Color.Lime, 1f, who));
                who.currentLocation.playSound("fairy_heal");
            }

            // 2. Apply defense buff to owner
            ApplyFairyDefenseBuff(who);

            // 3. Heal nearby allies + apply defense buff to them
            // currentLocation.farmers = every player currently in this map (co-op).
            foreach (var farmer in who.currentLocation.farmers)
            {
                // Skip the owner (already handled); 6f tiles ≈ generous aura radius.
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

        /// <summary>Applies (or refreshes) the 15-second +1 Defense blessing on one player.</summary>
        private static void ApplyFairyDefenseBuff(Farmer farmer)
        {
            if (farmer == null) return;

            // A short-lived buff: 15000 ms = 15 s, +1 flat Defense via BuffEffects.
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

        /// <summary>
        /// Ascended Parrot Egg power: on kill, 35% of the time an extra item from the
        /// monster's extra-drop list pops out, plus coin-sparkle feedback.
        /// </summary>
        public static void TriggerParrotBonusLoot(Monster monster, Farmer who)
        {
            if (monster == null || who?.currentLocation == null) return;

            // NextDouble() returns 0.0-1.0, so "< 0.35" is a literal 35% chance.
            if (Game1.random.NextDouble() < 0.35)
            {
                var extraDrops = monster.getExtraDropItems();
                if (extraDrops != null && extraDrops.Count > 0)
                {
                    // Pick one random entry from the drop list and spawn it in the world.
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

        /// <summary>
        /// Ascended Golden Spur power: when the player lands a critical hit while
        /// wearing it, grant a burst buff (+1 speed, +3 attack) whose duration mirrors
        /// the trinket's rolled dash duration.
        /// </summary>
        public static void TriggerGoldenSpurCritBonus(Farmer who, Monster monster, int baseDamage)
        {
            if (who == null) return;

            // Remove any spur buff still ticking so re-procs restart cleanly
            // instead of stacking multiple copies.
            if (who.buffs.AppliedBuffs.ContainsKey("iridiumspur"))
            {
                who.buffs.Remove("iridiumspur");
            }
            if (who.buffs.AppliedBuffs.ContainsKey("feiluvnana.BetterForge/SpurAttack"))
            {
                who.buffs.Remove("feiluvnana.BetterForge/SpurAttack");
            }

            // Default burst length is 8 s; read the trinket's actual rolled seconds
            // from its effect data ("GeneralStat" holds the duration in seconds).
            int duration = 8000;
            var spurTrinket = who.getFirstTrinketWithID("GoldenSpur");
            if (spurTrinket?.GetEffect() != null)
            {
                duration = spurTrinket.GetEffect().GeneralStat * 1000;
            }

            // Reuse vanilla's "iridiumspur" buff ID so its HUD behavior matches,
            // but supply our own icon texture and translated description.
            var unifiedBuff = new Buff(
                id: "iridiumspur",
                displayName: Game1.content.LoadString("Strings\\1_6_Strings:IridiumSpur_Name"),
                description: ModEntry.I18n.Get("buff.spur-attack.desc"),
                iconTexture: GetSpurAttackIcon(),
                iconSheetIndex: 0, // Custom 16x16 spur attack fury icon
                duration: duration,
                effects: new BuffEffects()
                {
                    Speed = { 1f },   // +1 movement speed
                    Attack = { 3f }   // +3 attack power
                }
            );
            who.applyBuff(unifiedBuff);
        }

        // 5. Ice Rod: Shatter ice + Frost Shockwave (30% Attack) + Frost Slow (Does not destroy rocks/ores)

        /// <summary>
        /// Ascended Ice Rod power: striking a frozen monster shatters the ice — big
        /// frost visuals plus an explosion that damages and slows every monster within
        /// 3.5 tiles of the impact.
        /// </summary>
        public static void TriggerIceShatterAndSlowNearby(Monster monster, Farmer who)
        {
            if (monster == null || who?.currentLocation == null) return;

            var location = who.currentLocation;

            // 1. Remove freeze effect and delete ice block/puddle sprite under monster
            // Un-stun immediately, then sweep the map's temporary sprite list BACKWARDS
            // (backwards iteration is required when removing items mid-loop) to delete
            // any ice sprite attached to this monster or stamped with its position hash.
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
            // Sprite #362 is the game's generic explosion animation, tinted icy cyan
            // via "Color * opacity". The main blast...
            location.temporarySprites.Add(new TemporaryAnimatedSprite(362, 50f, 6, 1, monster.Position, false, false)
            {
                color = new Color(130, 230, 255) * 0.65f,
                scale = 3.5f
            });

            // ...plus 3 smaller delayed puffs scattered around the impact point.
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
            // Start with a fallback value in case no weapon is held, then refine:
            int playerAttack = 40;
            if (who.CurrentTool is StardewValley.Tools.MeleeWeapon weapon)
            {
                playerAttack = (weapon.minDamage.Value + weapon.maxDamage.Value) / 2;
            }
            playerAttack += (int)(who.buffs.Attack * 5); // each attack buff point ≈ +5 raw
            int explosionDamage = Math.Max(15, (int)(playerAttack * 0.30f)); // never below 15

            // 5. Deal Explosion Damage + Display Cyan Damage Debris + Frost Slow to all nearby monsters (within 3.5 tiles)
            Vector2 centerTile = monster.Tile;
            foreach (var character in location.characters)
            {
                if (character is Monster nearbyMonster && nearbyMonster != monster)
                {
                    if (Vector2.Distance(centerTile, nearbyMonster.Tile) <= 3.5f)
                    {
                        // Deal the splash damage (no knockback, normal hit sound).
                        nearbyMonster.takeDamage(explosionDamage, 0, 0, false, 1.0, "hitEnemy");
                        location.debris.Add(new Debris(explosionDamage, nearbyMonster.getStandingPosition(), Color.Cyan, 1f, nearbyMonster));

                        // Frost slow: stun wave and splash visual
                        nearbyMonster.stunTime.Value = Math.Max(nearbyMonster.stunTime.Value, 1500);
                        Game1.createRadialDebris(location, 10, (int)nearbyMonster.Position.X + 32, (int)nearbyMonster.Position.Y + 32, 4, false);
                    }
                }
            }
        }

        // 6. Basilisk Paw: Reflect 50% damage & 20% lifesteal on hit (heal 3-8 HP)

        /// <summary>
        /// Ascended Basilisk Paw power, part 1: when the wearer takes damage, bounce
        /// half of it back onto the attacker with knockback away from the player.
        /// </summary>
        public static void TriggerDamageReflect(Monster attacker, Farmer victim, int incomingDamage)
        {
            if (attacker == null || victim == null || incomingDamage <= 0 || victim.currentLocation == null) return;

            var location = victim.currentLocation;
            // Always reflect at least 1 point, even for chip damage.
            int reflectDamage = Math.Max(1, (int)(incomingDamage * 0.5f));
            Vector2 trajectory = Utility.getAwayFromPlayerTrajectory(attacker.GetBoundingBox(), victim);
            attacker.takeDamage(reflectDamage, (int)trajectory.X, (int)trajectory.Y, false, 1.0, "hitEnemy");
            
            // Display visible orange floating damage number on attacker
            location.debris.Add(new Debris(reflectDamage, attacker.getStandingPosition(), Color.Orange, 1f, attacker));

            location.playSound("hitEnemy");
            Game1.createRadialDebris(location, 12, (int)attacker.Position.X + 32, (int)attacker.Position.Y + 32, 6, false);
        }

        /// <summary>
        /// Ascended Basilisk Paw power, part 2: 20% chance per hit to steal life,
        /// healing 3-8 HP scaled from the damage dealt (Math.Clamp bounds it).
        /// </summary>
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
