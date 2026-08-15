using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Objects.Trinkets;

namespace BetterTrinket
{
    public static class TrinketAscensionLogic
    {
        public const string AscensionKey = "feiluvnana.BetterTrinket/IsAscended";

        public static bool IsAscended(Trinket? trinket)
        {
            if (trinket == null) return false;
            return trinket.modData.ContainsKey(AscensionKey);
        }

        public static bool HasAscendedTrinket(Farmer? who, string trinketName)
        {
            if (who == null || who.trinketItems.Count == 0) return false;

            for (int i = 0; i < who.trinketItems.Count; i++)
            {
                var item = who.trinketItems[i];
                if (item is Trinket trinket && IsAscended(trinket))
                {
                    if (trinket.ItemId.Contains(trinketName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static string? GetAscensionDescription(string itemId)
        {
            string cleanId = itemId.Replace("(TR)", "").Trim().ToLowerInvariant();

            switch (cleanId)
            {
                case "frogegg":
                    return ModEntry.I18n.Get("ascension.frogegg.desc");
                case "fairybox":
                    return ModEntry.I18n.Get("ascension.fairybox.desc");
                case "parrotegg":
                    return ModEntry.I18n.Get("ascension.parrotegg.desc");
                case "goldenspur":
                    return ModEntry.I18n.Get("ascension.goldenspur.desc");
                case "magicquiver":
                    return ModEntry.I18n.Get("ascension.magicquiver.desc");
                case "icerod":
                    return ModEntry.I18n.Get("ascension.icerod.desc");
                case "basiliskpaw":
                    return ModEntry.I18n.Get("ascension.basiliskpaw.desc");
                default:
                    return null;
            }
        }

        public static bool TryAscendTrinket(Trinket trinket, Farmer who)
        {
            if (trinket == null || who == null)
                return false;

            if (IsAscended(trinket))
            {
                Game1.showRedMessage(ModEntry.I18n.Get("message.already-ascended"));
                who.currentLocation.playSound("cancel");
                return false;
            }

            if (!who.Items.ContainsId("(O)74", 1)) // Prismatic Shard
            {
                Game1.showRedMessage(ModEntry.I18n.Get("message.need-prismatic"));
                who.currentLocation.playSound("cancel");
                return false;
            }

            // Consume 1 Prismatic Shard
            who.Items.ReduceId("(O)74", 1);

            // Mark as Ascended
            trinket.modData[AscensionKey] = "true";

            // Optimize to max base stats
            Random rng = Game1.random;
            int bestSeed = trinket.generationSeed.Value;
            float bestScore = 0f;

            for (int i = 0; i < 500; i++)
            {
                int candSeed = rng.Next();
                var eval = TrinketReforgeLogic.Evaluate(trinket.ItemId, candSeed);
                if (eval.IsMaxRoll)
                {
                    bestSeed = candSeed;
                    break;
                }
                if (eval.Score > bestScore)
                {
                    bestScore = eval.Score;
                    bestSeed = candSeed;
                }
            }

            // Apply stats and clear cached tooltip
            trinket.RerollStats(bestSeed);
            TrinketReforgeLogic.ResetCachedDescription(trinket, who);

            who.currentLocation.playSound("yoba");
            who.currentLocation.playSound("reward");

            Game1.addHUDMessage(new HUDMessage(
                ModEntry.I18n.Get("hud.ascension-success", new { item = trinket.DisplayName })
            ));

            return true;
        }

        // --- In-Combat Ascension Effects ---

        public static void TriggerFairyAllyHeal(Farmer who, int healAmount)
        {
            if (who?.currentLocation == null) return;

            // Heal nearby co-op farmhands
            foreach (var farmer in who.currentLocation.farmers)
            {
                if (farmer != null && farmer != who && Vector2.Distance(who.Tile, farmer.Tile) <= 6f)
                {
                    farmer.health = Math.Min(farmer.maxHealth, farmer.health + healAmount);
                    who.currentLocation.playSound("healSound");
                }
            }
        }

        public static void TriggerIceShatter(Monster monster, Farmer who)
        {
            if (monster == null || who == null) return;

            int bonusDamage = 35;
            monster.takeDamage(bonusDamage, 0, 0, false, 1.0, who);
            who.currentLocation.playSound("glassBreak");
        }

        public static void TriggerDamageReflect(Monster attacker, Farmer who, int damageTaken)
        {
            if (attacker == null || who == null || damageTaken <= 0) return;

            int reflectDamage = Math.Max(5, (int)(damageTaken * 0.40f));
            attacker.takeDamage(reflectDamage, 0, 0, false, 1.0, who);
            who.currentLocation.playSound("parry");
        }

        public static void TriggerFrogFullDrop(Monster monster, Farmer who)
        {
            if (monster == null || who?.currentLocation == null) return;

            monster.currentLocation.monsterDrop(monster, (int)monster.Position.X, (int)monster.Position.Y, who);
        }
    }
}
