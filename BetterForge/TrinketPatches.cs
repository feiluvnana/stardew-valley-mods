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
    /// <summary>
    /// Central Harmony patch collection for BetterForge. Each patched game method
    /// gets either a "prefix" (runs BEFORE the original; returning false skips it)
    /// or a "postfix" (runs AFTER the original, able to tweak its result).
    /// Together these add the Anvil trinket workflow, ascended-trinket combat
    /// powers, tooltip/name upgrades, and projectile piercing behaviors.
    /// </summary>
    public static class TrinketPatches
    {
        // Shared config + logger references, injected once by ModEntry at startup.
        // "null!" tells the compiler these are assigned before first use.
        public static ModConfig Config = null!;
        private static IMonitor Monitor = null!;

        /// <summary>Wires up config + SMAPI logger before any patch can fire.</summary>
        public static void Initialize(ModConfig config, IMonitor monitor)
        {
            Config = config;
            Monitor = monitor;
        }

        /// <summary>
        /// Applies every Harmony hook this mod needs. Grouped by feature so it's easy
        /// to see which game methods power which behavior:
        /// 1. Anvil interaction   2. Trinket tooltips/damage   3. Damage reflection
        /// 4. Quiver/Ice orbs     5. Frog companion            6. Fairy Box
        /// 7. Stacking/copying    8. Crit chance bonuses
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            // 1. Anvil check for action & drop-in
            PatchMethod(harmony, typeof(StardewValley.Object), nameof(StardewValley.Object.checkForAction),
                new[] { typeof(Farmer), typeof(bool) }, prefixMethodName: nameof(Object_checkForAction_Prefix), description: "Anvil");
            PatchMethod(harmony, typeof(StardewValley.Object), nameof(StardewValley.Object.performObjectDropInAction),
                new[] { typeof(Item), typeof(bool), typeof(Farmer), typeof(bool) }, prefixMethodName: nameof(Object_performObjectDropInAction_Prefix), description: "Anvil");

            // 2. Trinket display & damage
            PatchMethod(harmony, typeof(Trinket), nameof(Trinket.getDescription),
                postfixMethodName: nameof(Trinket_getDescription_Postfix));
            PatchMethod(harmony, typeof(Trinket), "loadDisplayName",
                postfixMethodName: nameof(Trinket_loadDisplayName_Postfix));
            PatchMethod(harmony, typeof(Trinket), nameof(Trinket.OnDamageMonster),
                new[] { typeof(Farmer), typeof(Monster), typeof(int), typeof(bool), typeof(bool) }, postfixMethodName: nameof(Trinket_OnDamageMonster_Postfix));

            // 3. Farmer damage reflection
            PatchMethod(harmony, typeof(Farmer), nameof(Farmer.takeDamage),
                new[] { typeof(int), typeof(bool), typeof(Monster) },
                prefixMethodName: nameof(Farmer_takeDamage_Prefix), postfixMethodName: nameof(Farmer_takeDamage_Postfix), description: "Damage Reflection");

            // 4. Quiver, Ice Orb, & Projectile updates
            PatchMethod(harmony, typeof(MagicQuiverTrinketEffect), nameof(MagicQuiverTrinketEffect.Update),
                new[] { typeof(Farmer), typeof(GameTime), typeof(GameLocation) }, postfixMethodName: nameof(MagicQuiverTrinketEffect_Update_Postfix));
            PatchMethod(harmony, typeof(IceOrbTrinketEffect), nameof(IceOrbTrinketEffect.Update),
                new[] { typeof(Farmer), typeof(GameTime), typeof(GameLocation) }, postfixMethodName: nameof(IceOrbTrinketEffect_Update_Postfix));
            PatchMethod(harmony, typeof(Projectile), nameof(Projectile.update),
                new[] { typeof(GameTime), typeof(GameLocation) }, postfixMethodName: nameof(Projectile_update_Postfix), description: "Magic Quiver Piercing");

            // 5. Frog Companion
            PatchMethod(harmony, typeof(HungryFrogCompanion), nameof(HungryFrogCompanion.tongueReachedMonster),
                new[] { typeof(Monster) }, postfixMethodName: nameof(HungryFrogCompanion_tongueReachedMonster_Postfix));
            PatchMethod(harmony, typeof(HungryFrogCompanion), "triggerFullnessTimer",
                prefixMethodName: nameof(HungryFrogCompanion_triggerFullnessTimer_Prefix), postfixMethodName: nameof(HungryFrogCompanion_triggerFullnessTimer_Postfix));

            // 6. Fairy Box
            PatchMethod(harmony, typeof(FairyBoxTrinketEffect), nameof(FairyBoxTrinketEffect.Update),
                new[] { typeof(Farmer), typeof(GameTime), typeof(GameLocation) },
                prefixMethodName: nameof(FairyBoxTrinketEffect_Update_Prefix), postfixMethodName: nameof(FairyBoxTrinketEffect_Update_Postfix));

            // 7. Item stacking & getOne
            PatchMethod(harmony, typeof(Item), nameof(Item.canStackWith),
                new[] { typeof(ISalable) }, postfixMethodName: nameof(Item_canStackWith_Postfix));
            PatchMethod(harmony, typeof(Item), nameof(Item.getOne),
                postfixMethodName: nameof(Item_getOne_Postfix));

            // 8. GameLocation damage monster for Spur Crit Chance
            PatchMethod(harmony, typeof(GameLocation), nameof(GameLocation.damageMonster),
                new[] {
                    typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float),
                    typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool)
                },
                prefixMethodName: nameof(GameLocation_damageMonster_Prefix), description: "Spur Crit Chance");
        }

        /// <summary>
        /// Small wrapper around harmony.Patch that finds a game method by name
        /// (optionally with exact parameter types, because many game classes have
        /// several methods sharing one name) and attaches the requested prefix
        /// and/or postfix. Failures are logged instead of crashing startup.
        /// </summary>
        private static void PatchMethod(
            Harmony harmony,
            Type type,
            string methodName,
            Type[]? parameters = null,
            string? prefixMethodName = null,
            string? postfixMethodName = null,
            string? description = null)
        {
            try
            {
                // The "? :" ternary picks the overload lookup with explicit parameter
                // types when provided, otherwise matches by name alone.
                var method = parameters != null
                    ? AccessTools.Method(type, methodName, parameters)
                    : AccessTools.Method(type, methodName);

                if (method != null)
                {
                    // Build only the hook kinds that were requested (null = skip).
                    var prefix = prefixMethodName != null ? new HarmonyMethod(typeof(TrinketPatches), prefixMethodName) : null;
                    var postfix = postfixMethodName != null ? new HarmonyMethod(typeof(TrinketPatches), postfixMethodName) : null;

                    harmony.Patch(original: method, prefix: prefix, postfix: postfix);
                    Monitor.Log($"Hooked {type.Name}.{methodName}{(description != null ? $" for {description}" : "")} successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error patching {type.Name}.{methodName}: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Recognizes the Anvil across game versions: it can appear under its 1.6 ID
        /// ("Anvil"), legacy numeric ID ("289"), or plain object Name.
        /// </summary>
        public static bool IsAnvil(StardewValley.Object obj)
        {
            if (obj == null) return false;
            return obj.QualifiedItemId == "(BC)Anvil"
                || obj.ItemId == "Anvil"
                || obj.QualifiedItemId == "(BC)289"
                || obj.ItemId == "289"
                || obj.Name == "Anvil";
        }

        /// <summary>
        /// The heart of the Anvil feature. Decides what the player wants to do based
        /// on their inventory:
        ///   both bars + shards -> refuse (ambiguous), warn;
        ///   bars only          -> paid reforge (never-downgrade tier up);
        ///   shard only         -> ascension;
        ///   neither            -> explain exactly what's missing.
        /// Costs scale with trinket stack size so whole stacks upgrade at once.
        /// </summary>
        /// <returns>True if an action was performed.</returns>
        public static bool ProcessAnvilInteraction(StardewValley.Object anvil, Trinket trinket, Farmer who)
        {
            // Null-safety first.
            if (anvil == null || trinket == null || who == null)
                return false;

            int stackSize = Math.Max(1, trinket.Stack);
            int iridiumCount = who.Items.CountId("(O)337");   // Iridium Bars owned
            int shardCount = who.Items.CountId("(O)74");      // Prismatic Shards owned
            int totalIridiumRequired = stackSize * Config.IridiumBarCost; // cost scales per item in stack
            int totalShardsRequired = stackSize * 1;                      // ascension costs 1 shard each

            // Case 1: Player has BOTH Iridium Bars and Prismatic Shards -> Prompt warning toast
            // Ambiguous intent — refuse and ask the player to pick a currency.
            if (iridiumCount >= totalIridiumRequired && shardCount >= totalShardsRequired)
            {
                who.currentLocation?.playSound("cancel");
                Game1.showRedMessage(ModEntry.I18n.Get("message.cannot-both"));
                return false;
            }

            // Case 2: Player has ONLY Iridium Bars -> Reforge / Level Up
            if (iridiumCount >= totalIridiumRequired)
            {
                // Simulate the current roll first: a maxed trinket can't be reforged.
                var eval = TrinketReforgeLogic.Evaluate(trinket.ItemId, trinket.generationSeed.Value);
                if (eval.IsMaxRoll)
                {
                    who.currentLocation?.playSound("cancel");
                    Game1.showRedMessage(ModEntry.I18n.Get("message.already-max-tier"));
                    return false;
                }

                who.Items.ReduceId("(O)337", totalIridiumRequired); // take payment...
                TrinketReforgeLogic.ProcessReforge(trinket, who, Config); // ...then upgrade

                // Forge feedback: furnace+hammer sounds and sparks at the anvil tile.
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

                who.Items.ReduceId("(O)74", totalShardsRequired); // take payment
                TrinketAscensionLogic.AscendTrinketDirect(trinket, who);

                Game1.createRadialDebris(who.currentLocation, 12, (int)anvil.TileLocation.X * 64 + 32, (int)anvil.TileLocation.Y * 64 + 32, 8, false);
                return true;
            }

            // Case 4: Not enough materials — explain precisely what's missing.
            who.currentLocation?.playSound("cancel");
            if (stackSize > 1)
            {
                // Stacked trinkets get tailored messages depending on how close
                // the player is to affording the full stack's cost.
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

        /// <summary>
        /// Harmony prefix on Object.checkForAction (right-clicking a placed object).
        /// If it's the Anvil: handle our custom menu instead of vanilla behavior.
        /// </summary>
        /// <param name="__instance">The object being clicked (Harmony injects this).</param>
        /// <param name="justCheckingForActivity">Game probes whether action exists
        /// without performing it — answer "yes" so the game shows interaction.</param>
        /// <param name="__result">We set the method's return value ourselves.</param>
        /// <returns>False = skip the original method entirely.</returns>
        public static bool Object_checkForAction_Prefix(
            StardewValley.Object __instance,
            Farmer who,
            bool justCheckingForActivity,
            ref bool __result)
        {
            // Not an anvil (or no player)? Let the original code run untouched.
            if (__instance == null || !IsAnvil(__instance) || who == null)
                return true;

            if (justCheckingForActivity)
            {
                __result = true;   // "yes, this object has an action"
                return false;      // ...but don't run vanilla's action
            }

            if (who.ActiveItem is Trinket trinket)
            {
                // Holding a trinket: run the reforge/ascend decision tree.
                ProcessAnvilInteraction(__instance, trinket, who);
                who.ignoreItemConsumptionThisFrame = true; // stop vanilla eating the item
                __result = true;
                return false;
            }
            else
            {
                // Not holding a trinket: tell the player what they need.
                who.currentLocation?.playSound("cancel");
                Game1.showRedMessage(ModEntry.I18n.Get("message.need-trinket-in-hand"));
                __result = true;
                return false;
            }
        }

        /// <summary>
        /// Harmony prefix on performObjectDropInAction — the path used when a player
        /// drops an item onto a machine. Lets trinkets be "dropped into" the Anvil.
        /// </summary>
        /// <param name="probe">True when the game only wants to know IF the item would
        /// be accepted, without actually doing it.</param>
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
                __result = dropInItem is Trinket;  // accept only trinkets
                return false;                      // and never let vanilla consume them
            }

            if (dropInItem is Trinket trinket)
            {
                ProcessAnvilInteraction(__instance, trinket, who);
                who.ignoreItemConsumptionThisFrame = true;
                __result = false; // false = item was NOT eaten by the machine
                return false;
            }

            __result = false;
            return false;
        }

        /// <summary>
        /// Postfix on Item.canStackWith: normally any two identical items merge, but
        /// two trinkets may look identical yet roll differently. Only allow stacking
        /// when ID, generation seed AND ascension status all match.
        /// </summary>
        public static void Item_canStackWith_Postfix(Item __instance, ISalable other, ref bool __result)
        {
            // "is not X" pattern: only act when BOTH operands are trinkets.
            if (__instance is not Trinket thisTrinket || other is not Trinket otherTrinket)
                return;

            bool sameSeed = thisTrinket.generationSeed.Value == otherTrinket.generationSeed.Value;
            bool sameAscension = TrinketAscensionLogic.IsAscended(thisTrinket) == TrinketAscensionLogic.IsAscended(otherTrinket);
            bool sameId = thisTrinket.QualifiedItemId == otherTrinket.QualifiedItemId;

            if (!sameId || !sameSeed || !sameAscension)
            {
                __result = false; // any difference -> refuse to merge stacks
            }
        }

        /// <summary>
        /// Postfix on Item.getOne (creates a copy of an item, used when splitting
        /// stacks). Vanilla copies don't carry modData or the seed override, so we
        /// manually transfer seed, ascension flag and reforge count to the clone.
        /// </summary>
        public static void Item_getOne_Postfix(Item __instance, ref Item __result)
        {
            if (__instance is not Trinket sourceTrinket || __result is not Trinket resultTrinket)
                return;

            // Copy the generation seed so the clone rolls identical stats.
            resultTrinket.generationSeed.Value = sourceTrinket.generationSeed.Value;

            // Preserve the ascension flag (modData is not copied by vanilla).
            if (TrinketAscensionLogic.IsAscended(sourceTrinket))
            {
                resultTrinket.modData[TrinketAscensionLogic.AscensionKey] = "true";
            }

            // Preserve the reforge counter too, checking both current and legacy keys.
            if (sourceTrinket.modData.TryGetValue(TrinketReforgeLogic.ReforgeCountKey, out string? count) || sourceTrinket.modData.TryGetValue(TrinketReforgeLogic.LegacyReforgeCountKey, out count))
            {
                resultTrinket.modData[TrinketReforgeLogic.ReforgeCountKey] = count;
            }
        }

        /// <summary>
        /// Postfix on Trinket.getDescription: appends the ascension block ("✦ ASCENDED"
        /// badge, base-luck line and the trinket's special power) to hover tooltips.
        /// parseText word-wraps the text to fit the tooltip width (320 px).
        /// </summary>
        public static void Trinket_getDescription_Postfix(Trinket __instance, ref string __result)
        {
            if (__instance == null)
                return;

            try
            {
                // Only ascended trinkets get the extra tooltip block.
                if (TrinketAscensionLogic.IsAscended(__instance))
                {
                    string badge = ModEntry.I18n.Get("tooltip.ascended-badge");
                    string baseLuck = ModEntry.I18n.Get("tooltip.ascended-base-luck");
                    string? desc = TrinketAscensionLogic.GetAscensionDescription(__instance);

                    if (!string.IsNullOrEmpty(desc))
                    {
                        // Contains() guards against appending twice when the game
                        // rebuilds tooltips repeatedly in one frame.
                        if (!__result.Contains(badge) && !__result.Contains(desc))
                        {
                            // Wrap the description to 320 px so it fits the box.
                            string wrappedDesc = Game1.smallFont != null
                                ? Game1.parseText("✦ " + desc, Game1.smallFont, 320)
                                : "✦ " + desc;

                            __result += $"\n\n{badge}\n{baseLuck}\n{wrappedDesc}";
                        }
                    }
                    else if (!__result.Contains(badge) && !__result.Contains(baseLuck))
                    {
                        // Trinket type has no unique power: show badge + luck only.
                        __result += $"\n\n{badge}\n{baseLuck}";
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error formatting trinket tooltip: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Postfix on the trinket's internal display-name loader. When an ascended
        /// trinket ALSO has a perfect max roll, rename it to the localized
        /// "Perfect ..." format so players can spot their best rolls instantly.
        /// </summary>
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
                        string baseName = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId).DisplayName;
                        __result = ModEntry.I18n.Get("trinket.perfect-name-format", new { name = baseName });
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error formatting trinket display name: {ex}", LogLevel.Trace);
            }
        }

        // Patch GameLocation.damageMonster for Spur +10% Crit Chance and Magic Quiver +15% Crit Chance

        /// <summary>
        /// Harmony prefix on GameLocation.damageMonster (every monster hit flows
        /// through it). Boosts the incoming critChance variable when the attacker
        /// wears an ascended Golden Spur or Magic Quiver.
        /// </summary>
        public static void GameLocation_damageMonster_Prefix(Farmer who, ref float critChance)
        {
            if (who == null) return;

            if (TrinketAscensionLogic.HasAscendedTrinket(who, "spur") || TrinketAscensionLogic.HasAscendedTrinket(who, "goldenspur") || TrinketAscensionLogic.HasAscendedTrinket(who, "iridiumspur") || TrinketAscensionLogic.HasAscendedTrinket(who, "iridium"))
            {
                critChance += 0.10f; // +10% Critical Strike Chance
            }

            if (TrinketAscensionLogic.HasAscendedTrinket(who, "quiver") || TrinketAscensionLogic.HasAscendedTrinket(who, "magicquiver"))
            {
                critChance += 0.15f; // +15% Critical Strike Chance
            }
        }

        // Hooked directly from Trinket.OnDamageMonster (called on weapon/tool hits)

        /// <summary>
        /// Postfix on Trinket.OnDamageMonster: dispatches each ascended trinket's
        /// unique combat power whenever its wearer damages a monster.
        /// </summary>
        public static void Trinket_OnDamageMonster_Postfix(Trinket __instance, Farmer farmer, Monster monster, int damageAmount, bool isBomb, bool isCriticalHit)
        {
            if (farmer == null || monster == null || damageAmount <= 0 || __instance == null) return;

            string trinketId = __instance.ItemId?.ToLowerInvariant() ?? "";

            // 1. Golden / Iridium Spur: Crit Damage & Attack Buff
            if (isCriticalHit && (trinketId.Contains("spur") || trinketId.Contains("golden")) && TrinketAscensionLogic.IsAscended(__instance))
            {
                TrinketAscensionLogic.TriggerGoldenSpurCritBonus(farmer, monster, damageAmount);
            }

            // 2. Ice Rod: Shatter Strike & Frost Slow Wave on Frozen Monsters
            // stunTime > 50 means the monster is currently frozen in ice.
            if (monster.stunTime.Value > 50 && (trinketId.Contains("ice") || trinketId.Contains("rod")) && TrinketAscensionLogic.IsAscended(__instance))
            {
                TrinketAscensionLogic.TriggerIceShatterAndSlowNearby(monster, farmer);
            }

            // 3. Basilisk Paw: 20% Lifesteal on Hit
            if ((trinketId.Contains("basilisk") || trinketId.Contains("paw")) && TrinketAscensionLogic.IsAscended(__instance))
            {
                TrinketAscensionLogic.TriggerBasiliskLifesteal(farmer, damageAmount);
            }

            // 4. Parrot Egg: 2x Gold Coins & 35% Chance for Extra Loot Drop
            // "Health <= damageAmount" predicts the killing blow.
            if ((monster.Health <= 0 || monster.Health <= damageAmount) && trinketId.Contains("parrot") && TrinketAscensionLogic.IsAscended(__instance))
            {
                farmer.Money += 25 + Game1.random.Next(25); // bonus 25-49 g
                Game1.playSound("money");
                TrinketAscensionLogic.TriggerParrotBonusLoot(monster, farmer);
            }
        }

        // Hooked from Farmer.takeDamage (void return type)

        /// <summary>
        /// Prefix capturing a "__state" snapshot: Harmony passes this value from the
        /// prefix to the postfix automatically. Here it records whether the damage
        /// event was "real" (player vulnerable + attacker valid) so the postfix only
        /// reflects genuine hits, not blocked or self-inflicted ones.
        /// </summary>
        public static void Farmer_takeDamage_Prefix(Farmer __instance, int damage, bool overrideParry, Monster damager, out bool __state)
        {
            __state = __instance != null && damager != null && __instance.CanBeDamaged() && !damager.isInvincible();
        }

        /// <summary>
        /// Postfix using __state from the prefix: if a real hit landed and the victim
        /// wears an ascended Basilisk Paw, reflect 50% of the damage back.
        /// </summary>
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
        // ConditionalWeakTable = a dictionary that doesn't prevent garbage collection:
        // entries vanish automatically when their projectile despawns, so no memory leak.
        // The HashSet per projectile remembers which monsters were already hit.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Projectile, System.Collections.Generic.HashSet<Monster>> _arrowHitMonsters = new();
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Projectile, System.Collections.Generic.HashSet<Monster>> _iceOrbHitMonsters = new();

        /// <summary>
        /// Ascended Magic Quiver: every frame, flag the player's arrows to ignore
        /// character collisions and pierce ~forever. Actual multi-hit damage is dealt
        /// by <see cref="Projectile_update_Postfix"/> below.
        /// </summary>
        public static void MagicQuiverTrinketEffect_Update_Postfix(MagicQuiverTrinketEffect __instance, Farmer farmer, GameLocation location)
        {
            if (__instance == null || farmer == null || location == null) return;

            if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "quiver") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "magicquiver"))
            {
                foreach (var proj in location.projectiles)
                {
                    // Projectile ID 14 = special arrow; only arrows owned by this player.
                    if (proj is BasicProjectile bp && bp.projectileID.Value == 14 && bp.theOneWhoFiredMe.Get(location) == farmer)
                    {
                        bp.ignoreCharacterCollisions.Value = true;
                        bp.piercesLeft.Value = 99999; // effectively infinite
                    }
                }
            }
        }

        /// <summary>Ascended Ice Orb: same piercing treatment for the player's ice orbs
        /// (DebuffingProjectile with the "frozen" debuff).</summary>
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

        /// <summary>
        /// Postfix on Projectile.update — runs for EVERY projectile every frame.
        /// Because ascended quiver/orb projectiles no longer collide on their own,
        /// this method manually sweeps their hitbox over nearby monsters and applies
        /// damage/freeze once per monster (tracked in the WeakTables above).
        /// </summary>
        public static void Projectile_update_Postfix(Projectile __instance, GameTime time, GameLocation location, ref bool __result)
        {
            if (__instance == null || location == null) return;

            // 1. Magic Quiver Arrow: Multi-target sweeping piercing
            if (__instance is BasicProjectile bp && bp.projectileID.Value == 14)
            {
                Farmer? farmer = bp.theOneWhoFiredMe.Get(location) as Farmer;
                if (farmer != null && (TrinketAscensionLogic.HasAscendedTrinket(farmer, "quiver") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "magicquiver")))
                {
                    bp.ignoreCharacterCollisions.Value = true;
                    bp.piercesLeft.Value = 99999;

                    Rectangle arrowBounds = bp.getBoundingBox();
                    var hitList = _arrowHitMonsters.GetOrCreateValue(bp); // per-arrow memory

                    // Check overlap against every monster currently on this map.
                    for (int i = 0; i < location.characters.Count; i++)
                    {
                        if (location.characters[i] is Monster monster && !monster.IsInvisible && arrowBounds.Intersects(monster.GetBoundingBox()))
                        {
                            // HashSet.Add returns false if already present: each monster
                            // is damaged at most ONCE per arrow pass.
                            if (hitList.Add(monster))
                            {
                                int dmg = Math.Max(1, bp.damageToFarmer.Value);
                                location.damageMonster(monster.GetBoundingBox(), dmg, dmg + 1, false, farmer, true);
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

                    // Inflate widens the rectangle by N px on all sides, making the
                    // sweeping orb slightly more forgiving than vanilla targeting.
                    Rectangle orbBounds = dp.getBoundingBox();
                    orbBounds.Inflate(12, 12);
                    var hitList = _iceOrbHitMonsters.GetOrCreateValue(dp);

                    for (int i = 0; i < location.characters.Count; i++)
                    {
                        if (location.characters[i] is Monster monster && !monster.IsInvisible && !monster.isInvincible() && orbBounds.Intersects(monster.GetBoundingBox()))
                        {
                            if (hitList.Add(monster)) // once per monster per orb
                            {
                                // Freeze duration comes from the orb's rolled intensity
                                // (milliseconds); fall back to a flat 4 s.
                                int freezeDuration = dp.debuffIntensity.Value > 0 ? dp.debuffIntensity.Value : 4000;
                                monster.stunTime.Value = freezeDuration;
                                location.playSound("frozen");

                                // Draw the ice-cube sprite (from the game's Cursors2 sheet)
                                // glued to the monster for as long as it stays frozen.
                                location.temporarySprites.Add(new TemporaryAnimatedSprite("LooseSprites\\Cursors2", new Rectangle(118, 227, 16, 13), new Vector2(0f, 0f), flipped: false, 0f, Color.White)
                                {
                                    layerDepth = (float)(monster.StandingPixel.Y + 2) / 10000f,
                                    animationLength = 1,
                                    interval = freezeDuration,
                                    scale = 4f,
                                    id = (int)(monster.position.X * 777f + monster.position.Y * 77777f), // unique hash for later cleanup
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
        // Same WeakTable trick as the projectiles: remember which monster each frog
        // caught so we can drop its loot when digestion finishes.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<HungryFrogCompanion, Monster> _frogAttachedMonsters = new();

        /// <summary>Records which monster the frog's tongue just grabbed.</summary>
        public static void HungryFrogCompanion_tongueReachedMonster_Postfix(HungryFrogCompanion __instance, Monster m)
        {
            if (__instance != null && m != null)
            {
                _frogAttachedMonsters.AddOrUpdate(__instance, m);
            }
        }

        /// <summary>
        /// Prefix on the frog's fullness timer: if the owner has an ascended Frog Egg,
        /// spill the swallowed monster's loot right before the normal digest begins.
        /// </summary>
        public static void HungryFrogCompanion_triggerFullnessTimer_Prefix(HungryFrogCompanion __instance)
        {
            if (__instance == null) return;

            Farmer? owner = __instance.Owner;
            if (owner != null && (TrinketAscensionLogic.HasAscendedTrinket(owner, "frog") || TrinketAscensionLogic.HasAscendedTrinket(owner, "frogegg")))
            {
                if (_frogAttachedMonsters.TryGetValue(__instance, out Monster? m) && m != null)
                {
                    TrinketAscensionLogic.TriggerFrogLootDrop(m, owner);
                    _frogAttachedMonsters.Remove(__instance); // consume the record
                }
            }
        }

        /// <summary>
        /// Postfix: ascended frogs have a 45% chance to skip their post-meal cooldown
        /// entirely and hop off hunting again immediately.
        /// </summary>
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

        /// <summary>
        /// Prefix snapshotting whether the fairy's heal pulse is about to fire this
        /// frame (HealTimer about to reach HealDelay). The postfix uses this to know
        /// when to add the ascended bonus heal — without double-counting vanilla's.
        /// </summary>
        public static void FairyBoxTrinketEffect_Update_Prefix(FairyBoxTrinketEffect __instance, Farmer farmer, GameTime time, out bool __state)
        {
            __state = false;
            if (__instance != null && farmer != null && time != null)
            {
                if (__instance.HealTimer + (float)time.ElapsedGameTime.TotalMilliseconds >= __instance.HealDelay)
                {
                    __state = true; // pulse fires in the original Update()
                }
            }
        }

        /// <summary>
        /// Postfix: on each vanilla heal pulse, an ascended Fairy Box additionally
        /// heals owner + nearby allies (guaranteed) and blesses them with +1 Defense,
        /// scaling the bonus with the trinket's power roll.
        /// </summary>
        public static void FairyBoxTrinketEffect_Update_Postfix(FairyBoxTrinketEffect __instance, Farmer farmer, bool __state)
        {
            if (__instance == null || farmer == null || !__state) return;

            if (TrinketAscensionLogic.HasAscendedTrinket(farmer, "fairy") || TrinketAscensionLogic.HasAscendedTrinket(farmer, "fairybox"))
            {
                // Bonus scales with max health (5% baseline) and the trinket's Power stat.
                int healAmount = Math.Max(4, (int)(farmer.maxHealth * 0.05f * __instance.Power));
                TrinketAscensionLogic.TriggerFairyAllyHealAndBlessing(farmer, healAmount);
                __instance.HealTimer = 0f; // restart the pulse cycle cleanly
            }
        }
    }
}
