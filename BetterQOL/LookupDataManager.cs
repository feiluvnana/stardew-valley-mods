using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.GameData.Locations;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.SpecialOrders;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;

namespace BetterQOL
{
    public class LookupLink
    {
        public string Text { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public Texture2D? Icon { get; set; }
        public Rectangle? IconSourceRect { get; set; }
        public Color TextColor { get; set; } = Game1.textColor;
        public Func<LookupSubject?>? OnClick { get; set; }
        public Rectangle Bounds { get; set; }

        public LookupLink(string text, string? subtitle = null, Color? textColor = null, Texture2D? icon = null, Rectangle? iconSourceRect = null, Func<LookupSubject?>? onClick = null)
        {
            Text = text;
            Subtitle = subtitle;
            TextColor = textColor ?? Game1.textColor;
            Icon = icon;
            IconSourceRect = iconSourceRect;
            OnClick = onClick;
        }
    }

    public class LookupField
    {
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
        public Color ValueColor { get; set; } = Game1.textColor;
        public List<LookupLink> Links { get; set; } = new();

        public LookupField(string label, string value, Color? valueColor = null)
        {
            Label = label;
            Value = value;
            ValueColor = valueColor ?? Game1.textColor;
        }

        public LookupField(string label, List<LookupLink> links)
        {
            Label = label;
            Links = links;
        }
    }

    public class LookupSection
    {
        public string Title { get; set; } = string.Empty;
        public List<LookupField> Fields { get; set; } = new();

        public LookupSection(string title)
        {
            Title = title;
        }
    }

    public class LookupSubject
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public Texture2D? MainIcon { get; set; }
        public Rectangle? MainIconSourceRect { get; set; }
        public Texture2D? Portrait { get; set; }
        public Rectangle? PortraitSourceRect { get; set; }
        public List<LookupSection> Sections { get; set; } = new();
    }

    public static class LookupDataManager
    {
        #region 1. NPC / Villager Lookup

        public static LookupSubject BuildNPCSubject(NPC npc)
        {
            var subject = new LookupSubject
            {
                Title = npc.displayName ?? npc.Name,
                Portrait = npc.Portrait,
                PortraitSourceRect = new Rectangle(0, 0, 64, 64)
            };

            string birthdaySeason = ModEntry.I18n.Get($"season.{npc.Birthday_Season?.ToLower() ?? "spring"}").ToString();
            subject.Subtitle = ModEntry.I18n.Get("lookup.npc.subtitle", new { season = birthdaySeason, day = npc.Birthday_Day });

            // Section 1: Relationship
            var relSection = new LookupSection(ModEntry.I18n.Get("lookup.section.relationship"));
            if (Game1.player.friendshipData.TryGetValue(npc.Name, out var friendship))
            {
                int points = friendship.Points;
                int hearts = points / 250;
                int ptsInHeart = points % 250;
                int maxHearts = friendship.IsMarried() || friendship.IsRoommate() ? 14 : (friendship.IsDating() ? 10 : 8);

                relSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.npc.friendship"),
                    $"{hearts}/{maxHearts} ♥ ({points} pts, {ptsInHeart}/250 to next)",
                    new Color(220, 20, 60)
                ));

                bool talkedToday = Game1.player.hasPlayerTalkedToNPC(npc.Name);
                relSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.npc.talked-today"),
                    talkedToday ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"),
                    talkedToday ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                relSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.npc.gifts-this-week"),
                    $"{friendship.GiftsThisWeek}/2 (Today: {(friendship.GiftsToday > 0 ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"))})",
                    friendship.GiftsThisWeek >= 2 ? new Color(0, 140, 0) : Game1.textColor
                ));

                if (friendship.IsMarried())
                {
                    relSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.status"), ModEntry.I18n.Get("lookup.npc.status-married"), new Color(180, 50, 180)));
                }
                else if (friendship.IsRoommate())
                {
                    relSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.status"), ModEntry.I18n.Get("lookup.npc.status-roommate"), new Color(180, 50, 180)));
                }
                else if (friendship.IsDating())
                {
                    relSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.status"), ModEntry.I18n.Get("lookup.npc.status-dating"), new Color(220, 20, 60)));
                }
            }
            else
            {
                relSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.npc.friendship"),
                    ModEntry.I18n.Get("lookup.npc.unmet"),
                    Color.DarkSlateGray
                ));
            }
            subject.Sections.Add(relSection);

            // Section 2: Loved & Liked Gifts
            if (ModEntry.Config.ShowGiftTastes)
            {
                var giftSection = new LookupSection(ModEntry.I18n.Get("lookup.section.gifts"));
                var (lovedLinks, likedLinks, neutralLinks, dislikedLinks) = GetNPCAllGiftPreferenceLinks(npc);

                if (lovedLinks.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.loved-gifts"), lovedLinks));
                }

                if (likedLinks.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.liked-gifts"), likedLinks));
                }

                if (neutralLinks.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField("Neutral Gifts", neutralLinks));
                }

                if (dislikedLinks.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField("Disliked / Hated", dislikedLinks));
                }

                subject.Sections.Add(giftSection);
            }

            return subject;
        }

        private static (List<LookupLink> Loved, List<LookupLink> Liked, List<LookupLink> Neutral, List<LookupLink> Disliked) GetNPCAllGiftPreferenceLinks(NPC npc)
        {
            var loved = new List<LookupLink>();
            var liked = new List<LookupLink>();
            var neutral = new List<LookupLink>();
            var disliked = new List<LookupLink>();

            try
            {
                if (Game1.NPCGiftTastes != null && Game1.NPCGiftTastes.TryGetValue(npc.Name, out string? giftStr))
                {
                    string[] parts = giftStr.Split('/');

                    // Loved (index 1)
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        foreach (string id in parts[1].Split(' '))
                        {
                            var data = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                            if (data != null && !loved.Any(l => l.Text == data.DisplayName))
                            {
                                loved.Add(new LookupLink(data.DisplayName, null, new Color(180, 50, 180), data.GetTexture(), data.GetSourceRect(), () => {
                                    var itm = ItemRegistry.Create(data.QualifiedItemId);
                                    return itm != null ? BuildItemSubject(itm) : null;
                                }));
                            }
                        }
                    }

                    // Liked (index 3)
                    if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3]))
                    {
                        foreach (string id in parts[3].Split(' '))
                        {
                            var data = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                            if (data != null && !liked.Any(l => l.Text == data.DisplayName))
                            {
                                liked.Add(new LookupLink(data.DisplayName, null, new Color(0, 140, 0), data.GetTexture(), data.GetSourceRect(), () => {
                                    var itm = ItemRegistry.Create(data.QualifiedItemId);
                                    return itm != null ? BuildItemSubject(itm) : null;
                                }));
                            }
                        }
                    }

                    // Disliked (index 5)
                    if (parts.Length > 5 && !string.IsNullOrEmpty(parts[5]))
                    {
                        foreach (string id in parts[5].Split(' '))
                        {
                            var data = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                            if (data != null && !disliked.Any(l => l.Text == data.DisplayName))
                            {
                                disliked.Add(new LookupLink(data.DisplayName, null, new Color(200, 60, 20), data.GetTexture(), data.GetSourceRect(), () => {
                                    var itm = ItemRegistry.Create(data.QualifiedItemId);
                                    return itm != null ? BuildItemSubject(itm) : null;
                                }));
                            }
                        }
                    }

                    // Neutral (index 9)
                    if (parts.Length > 9 && !string.IsNullOrEmpty(parts[9]))
                    {
                        foreach (string id in parts[9].Split(' '))
                        {
                            var data = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                            if (data != null && !neutral.Any(l => l.Text == data.DisplayName))
                            {
                                neutral.Add(new LookupLink(data.DisplayName, null, Color.DarkSlateGray, data.GetTexture(), data.GetSourceRect(), () => {
                                    var itm = ItemRegistry.Create(data.QualifiedItemId);
                                    return itm != null ? BuildItemSubject(itm) : null;
                                }));
                            }
                        }
                    }
                }
            }
            catch { }

            return (loved.Take(12).ToList(), liked.Take(12).ToList(), neutral.Take(8).ToList(), disliked.Take(8).ToList());
        }

        #endregion

        #region 2. Item Lookup (Fish, Crops, Food, Artisan, Weapons, Progress)

        public static LookupSubject BuildItemSubject(Item item)
        {
            var subject = new LookupSubject
            {
                Title = item.DisplayName
            };

            var itemData = ItemRegistry.GetData(item.QualifiedItemId);
            if (itemData != null)
            {
                try
                {
                    subject.MainIcon = itemData.GetTexture();
                    subject.MainIconSourceRect = itemData.GetSourceRect();
                }
                catch { }
            }

            string categoryName = item.getCategoryName();
            subject.Subtitle = !string.IsNullOrEmpty(categoryName) ? categoryName : ModEntry.I18n.Get("lookup.type.item").ToString();

            // Section 1: Overview & Descriptions (Using unpatched raw description to prevent duplicates)
            var overviewSection = new LookupSection(ModEntry.I18n.Get("lookup.section.overview"));
            string desc = itemData?.Description ?? string.Empty;
            if (string.IsNullOrEmpty(desc) && item is not Tool)
            {
                desc = item.getDescription();
                int extraIdx = desc.IndexOf("\nSell Price:", StringComparison.Ordinal);
                if (extraIdx >= 0)
                {
                    desc = desc.Substring(0, extraIdx);
                }
                extraIdx = desc.IndexOf("\nNeeded for:", StringComparison.Ordinal);
                if (extraIdx >= 0)
                {
                    desc = desc.Substring(0, extraIdx);
                }
            }

            if (!string.IsNullOrEmpty(desc))
            {
                overviewSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.description"), desc.Trim(), Color.DarkSlateGray));
            }

            // Edibility (Health / Energy & Food Buffs)
            if (item is StardewValley.Object sObj && sObj.Edibility > -300)
            {
                int energy = sObj.staminaRecoveredOnConsumption();
                int health = sObj.healthRecoveredOnConsumption();
                string energyStr = energy >= 0 ? $"+{energy}" : $"{energy}";
                string healthStr = health >= 0 ? $"+{health}" : $"{health}";

                overviewSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.item.edibility"),
                    $"{energyStr} {ModEntry.I18n.Get("lookup.item.energy")}, {healthStr} {ModEntry.I18n.Get("lookup.item.health")}",
                    energy >= 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                // Food Buffs
                var buffs = GetFoodBuffs(item);
                if (buffs.Count > 0)
                {
                    overviewSection.Fields.Add(new LookupField("Buffs", string.Join(", ", buffs), new Color(180, 50, 180)));
                }
            }

            // Sell Prices by Quality (Using clean Silver/Gold/Iridium text instead of broken Unicode stars)
            int baseSellPrice = item.sellToStorePrice();
            if (baseSellPrice > 0)
            {
                int silverPrice = (int)(baseSellPrice * 1.25);
                int goldPrice = (int)(baseSellPrice * 1.5);
                int iridiumPrice = (int)(baseSellPrice * 2.0);

                overviewSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.item.sell-price"),
                    $"{baseSellPrice}g (Silver: {silverPrice}g, Gold: {goldPrice}g, Iridium: {iridiumPrice}g)",
                    new Color(180, 100, 0)
                ));
            }

            // Number Owned (in inventory + storage chests across the world)
            var (invCount, storageCount) = GetItemOwnedCounts(item);
            int totalOwned = invCount + storageCount;
            string ownedStr = totalOwned > 0
                ? $"{invCount} in inventory, {storageCount} in storage ({totalOwned} total)"
                : "0 owned (none in inventory or chests)";
            overviewSection.Fields.Add(new LookupField("Number Owned", ownedStr, totalOwned > 0 ? new Color(0, 140, 0) : Color.DarkSlateGray));

            subject.Sections.Add(overviewSection);

            // Section 2: Fish Details (Locations, Time, Seasons, Weather, Behavior)
            if (item.Category == StardewValley.Object.FishCategory || IsFishItem(item))
            {
                AddFishDataSection(subject, item);
            }

            // Section 3: Crop / Seed Data (Growth Time, Regrow, Harvest Seasons)
            AddCropDataSection(subject, item);

            // Section 4: Weapons & Equipment (Damage, Crit, Defense, Forges, Enchantments)
            AddWeaponAndCombatSection(subject, item);

            // Section 5: Tool Details (Upgrade level, Enchantments, Attached Bait/Tackles)
            AddToolSection(subject, item);

            // Section 6: Artisan Processing (Keg, Preserves Jar, Dehydrator, Cask, Fish Smoker)
            AddArtisanProductsSection(subject, item, baseSellPrice);

            // Section 7: Museum & Bundles
            if (ModEntry.Config.ShowBundleAndMuseumInfo)
            {
                var progressSection = new LookupSection(ModEntry.I18n.Get("lookup.section.progress"));

                // Museum
                bool isMuseumItem = (item is StardewValley.Object obj && (obj.Type == "Arch" || obj.Type == "Minerals"))
                                 || item.Category == StardewValley.Object.mineralsCategory;
                if (isMuseumItem)
                {
                    bool isDonated = Game1.netWorldState.Value.MuseumPieces.Values.Any(v =>
                        v == item.ItemId ||
                        v == item.QualifiedItemId ||
                        (item is StardewValley.Object s && v == s.ParentSheetIndex.ToString()) ||
                        v == $"(O){item.ItemId}"
                    );

                    progressSection.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.item.museum"),
                        isDonated ? ModEntry.I18n.Get("lookup.item.museum-donated") : ModEntry.I18n.Get("lookup.item.museum-needed"),
                        isDonated ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                }

                // Community Center Bundles
                var neededBundles = GetNeededBundles(item);
                if (neededBundles.Count > 0)
                {
                    progressSection.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.item.bundles"),
                        string.Join(", ", neededBundles),
                        new Color(180, 50, 180)
                    ));
                }

                if (progressSection.Fields.Count > 0)
                {
                    subject.Sections.Add(progressSection);
                }
            }

            // Section 8: Gift Preferences (Interactive Tappable NPC Links)
            if (ModEntry.Config.ShowGiftTastes)
            {
                var giftSection = new LookupSection(ModEntry.I18n.Get("lookup.section.gift-tastes"));
                var (lovers, likers) = GetItemGiftTastesLinks(item);

                if (lovers.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.loved-by"), lovers));
                }

                if (likers.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.liked-by"), likers));
                }

                if (giftSection.Fields.Count > 0)
                {
                    subject.Sections.Add(giftSection);
                }
            }

            // Section 9: Recipes Using This Item (Cooking & Crafting)
            if (ModEntry.Config.ShowItemRecipes)
            {
                var recipes = GetRecipesUsingItemLinks(item);
                if (recipes.Count > 0)
                {
                    var recipeSection = new LookupSection(ModEntry.I18n.Get("lookup.section.recipes"));
                    recipeSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.used-in-recipes"), recipes));
                    subject.Sections.Add(recipeSection);
                }
            }

            return subject;
        }

        private static (int InventoryCount, int StorageCount) GetItemOwnedCounts(Item item)
        {
            int inventory = 0;
            int storage = 0;
            string itemId = item.ItemId;
            string qId = item.QualifiedItemId;

            try
            {
                // 1. Inventory
                foreach (var invItem in Game1.player.Items)
                {
                    if (invItem != null && (invItem.ItemId == itemId || invItem.QualifiedItemId == qId))
                    {
                        inventory += invItem.Stack;
                    }
                }

                // 2. Storage across all locations (Chests, Fridges, Junimo Chests, Auto-Grabbers)
                foreach (var loc in Game1.locations)
                {
                    if (loc == null) continue;

                    foreach (var obj in loc.objects.Values)
                    {
                        if (obj is Chest chest && chest.Items != null)
                        {
                            foreach (var cItem in chest.Items)
                            {
                                if (cItem != null && (cItem.ItemId == itemId || cItem.QualifiedItemId == qId))
                                {
                                    storage += cItem.Stack;
                                }
                            }
                        }
                    }

                    if (loc is FarmHouse house && house.fridge.Value != null && house.fridge.Value.Items != null)
                    {
                        foreach (var fItem in house.fridge.Value.Items)
                        {
                            if (fItem != null && (fItem.ItemId == itemId || fItem.QualifiedItemId == qId))
                            {
                                storage += fItem.Stack;
                            }
                        }
                    }

                    if (loc.buildings.Count > 0)
                    {
                        foreach (var b in loc.buildings)
                        {
                            if (b.indoors.Value != null)
                            {
                                foreach (var obj in b.indoors.Value.objects.Values)
                                {
                                    if (obj is Chest chest && chest.Items != null)
                                    {
                                        foreach (var cItem in chest.Items)
                                        {
                                            if (cItem != null && (cItem.ItemId == itemId || cItem.QualifiedItemId == qId))
                                            {
                                                storage += cItem.Stack;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return (inventory, storage);
        }

        private static void AddWeaponAndCombatSection(LookupSubject subject, Item item)
        {
            try
            {
                if (item is MeleeWeapon weapon)
                {
                    var section = new LookupSection("Combat & Weapon Stats");
                    section.Fields.Add(new LookupField("Damage", $"{weapon.minDamage.Value} - {weapon.maxDamage.Value}", new Color(200, 60, 20)));

                    double critChance = weapon.critChance.Value * 100;
                    section.Fields.Add(new LookupField("Critical Strike", $"{critChance:0.#}% (x{weapon.critMultiplier.Value:0.#} dmg)", new Color(180, 50, 180)));

                    if (weapon.speed.Value != 0)
                        section.Fields.Add(new LookupField("Speed", $"{(weapon.speed.Value > 0 ? "+" : "")}{weapon.speed.Value}", weapon.speed.Value > 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)));

                    if (weapon.addedDefense.Value != 0)
                        section.Fields.Add(new LookupField("Defense", $"{(weapon.addedDefense.Value > 0 ? "+" : "")}{weapon.addedDefense.Value}", new Color(20, 110, 220)));

                    if (weapon.addedAreaOfEffect.Value != 0)
                        section.Fields.Add(new LookupField("Added Reach", $"+{weapon.addedAreaOfEffect.Value}", Color.DarkSlateGray));

                    if (weapon.knockback.Value != 1f)
                        section.Fields.Add(new LookupField("Knockback", $"{weapon.knockback.Value:0.0}", Color.DarkSlateGray));

                    int forges = weapon.GetTotalForgeLevels();
                    if (forges > 0)
                        section.Fields.Add(new LookupField("Volcano Forges", $"Level {forges} / 3", new Color(180, 100, 0)));

                    if (weapon.enchantments.Count > 0)
                    {
                        string enchants = string.Join(", ", weapon.enchantments.Select(e => e.GetName()));
                        section.Fields.Add(new LookupField("Enchantments", enchants, new Color(180, 50, 180)));
                    }

                    subject.Sections.Add(section);
                }
                else if (item is Boots boots)
                {
                    var section = new LookupSection("Equipment Stats");
                    if (boots.defenseBonus.Value > 0)
                        section.Fields.Add(new LookupField("Defense", $"+{boots.defenseBonus.Value}", new Color(20, 110, 220)));
                    if (boots.immunityBonus.Value > 0)
                        section.Fields.Add(new LookupField("Immunity", $"+{boots.immunityBonus.Value}", new Color(0, 140, 0)));
                    subject.Sections.Add(section);
                }
                else if (item is Ring ring)
                {
                    var section = new LookupSection("Ring Effects");
                    if (ring is CombinedRing combined && combined.combinedRings.Count > 0)
                    {
                        var ringLinks = new List<LookupLink>();
                        foreach (var subRing in combined.combinedRings)
                        {
                            var rData = ItemRegistry.GetData(subRing.QualifiedItemId);
                            ringLinks.Add(new LookupLink(subRing.DisplayName, null, Game1.textColor, rData?.GetTexture(), rData?.GetSourceRect(), () => BuildItemSubject(subRing)));
                        }
                        section.Fields.Add(new LookupField("Combined Rings", ringLinks));
                    }
                    subject.Sections.Add(section);
                }
            }
            catch { }
        }

        private static void AddToolSection(LookupSubject subject, Item item)
        {
            try
            {
                if (item is Tool tool && item is not MeleeWeapon)
                {
                    var section = new LookupSection("Tool Details");
                    string upgradeName = tool.UpgradeLevel switch
                    {
                        0 => "Basic (Standard)",
                        1 => "Copper Upgrade",
                        2 => "Steel Upgrade",
                        3 => "Gold Upgrade",
                        4 => "Iridium Upgrade",
                        _ => "Standard"
                    };
                    section.Fields.Add(new LookupField("Upgrade Level", upgradeName, new Color(180, 100, 0)));

                    if (tool.enchantments.Count > 0)
                    {
                        string enchants = string.Join(", ", tool.enchantments.Select(e => e.GetName()));
                        section.Fields.Add(new LookupField("Enchantments", enchants, new Color(180, 50, 180)));
                    }

                    if (tool is FishingRod rod)
                    {
                        var bait = rod.GetBait();
                        section.Fields.Add(new LookupField("Bait Attached", bait != null ? $"{bait.DisplayName} (x{bait.Stack})" : "None", bait != null ? new Color(0, 140, 0) : Color.DarkSlateGray));

                        var tackles = rod.GetTackle();
                        if (tackles != null && tackles.Count > 0)
                        {
                            var tackleNames = tackles.Where(t => t != null).Select(t => $"{t.DisplayName} ({t.uses.Value}/{FishingRod.maxTackleUses} uses left)");
                            section.Fields.Add(new LookupField("Tackles", string.Join(", ", tackleNames), new Color(20, 110, 220)));
                        }
                    }

                    subject.Sections.Add(section);
                }
            }
            catch { }
        }

        private static bool IsFishItem(Item item)
        {
            try
            {
                var fishData = DataLoader.Fish(Game1.content);
                return fishData != null && (fishData.ContainsKey(item.ItemId) || fishData.ContainsKey(item.QualifiedItemId));
            }
            catch { return false; }
        }

        private static void AddFishDataSection(LookupSubject subject, Item item)
        {
            try
            {
                var fishDict = DataLoader.Fish(Game1.content);
                if (fishDict == null || !fishDict.TryGetValue(item.ItemId, out string? fishRaw))
                    return;

                string[] parts = fishRaw.Split('/');
                if (parts.Length < 7)
                    return;

                var section = new LookupSection("Fishing Details");

                // 1. Difficulty & Behavior (parts[1] = difficulty, parts[2] = behavior)
                string diff = parts[1];
                string behavior = parts.Length > 2 && !string.IsNullOrEmpty(parts[2]) ? parts[2] : "mixed";
                string behaviorName = char.ToUpper(behavior[0]) + behavior.Substring(1);
                section.Fields.Add(new LookupField("Difficulty", $"{diff} ({behaviorName})", new Color(200, 60, 20)));

                // 2. Spawn Seasons (parts[6] in Data/Fish)
                if (parts.Length > 6 && !string.IsNullOrWhiteSpace(parts[6]))
                {
                    var seasonList = parts[6].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var seasonNames = seasonList.Select(s => {
                        string key = $"season.{s.ToLower()}";
                        var tr = ModEntry.I18n.Get(key);
                        return tr.HasValue() ? tr.ToString() : (char.ToUpper(s[0]) + s.Substring(1));
                    });
                    section.Fields.Add(new LookupField("Seasons", string.Join(", ", seasonNames), new Color(46, 125, 50)));
                }

                // 3. Spawn Times (parts[5] in Data/Fish)
                if (parts.Length > 5 && !string.IsNullOrWhiteSpace(parts[5]))
                {
                    string[] timeParts = parts[5].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var timeRanges = new List<string>();
                    for (int i = 0; i < timeParts.Length; i += 2)
                    {
                        if (i + 1 < timeParts.Length)
                        {
                            string start = FormatGameTime(timeParts[i]);
                            string end = FormatGameTime(timeParts[i + 1]);
                            timeRanges.Add($"{start} – {end}");
                        }
                    }
                    if (timeRanges.Count > 0)
                    {
                        section.Fields.Add(new LookupField("Time of Day", string.Join(", ", timeRanges), new Color(180, 100, 0)));
                    }
                }

                // 4. Weather (parts[7] in Data/Fish)
                if (parts.Length > 7)
                {
                    string weather = parts[7].ToLower() switch
                    {
                        "sunny" => "Sunny",
                        "rainy" => "Rainy",
                        _ => "Any Weather"
                    };
                    section.Fields.Add(new LookupField("Weather", weather, new Color(20, 110, 220)));
                }

                // 5. Min Skill
                if (parts.Length > 9 && int.TryParse(parts[9], out int minSkill) && minSkill > 0)
                {
                    section.Fields.Add(new LookupField("Min Fishing Skill", $"Level {minSkill}", Color.DarkSlateGray));
                }

                // 6. Spawn Locations (Extracted and mapped to friendly location names)
                var spawnLocations = GetFishSpawnLocations(item.ItemId);
                if (spawnLocations.Count > 0)
                {
                    section.Fields.Add(new LookupField("Locations", string.Join(", ", spawnLocations), new Color(20, 110, 220)));
                }

                subject.Sections.Add(section);
            }
            catch { }
        }

        private static List<string> GetFishSpawnLocations(string fishId)
        {
            var results = new List<string>();
            try
            {
                var locDict = DataLoader.Locations(Game1.content);
                if (locDict == null)
                    return results;

                foreach (var kvp in locDict)
                {
                    string locKey = kvp.Key;
                    if (locKey.Equals("fishingGame", StringComparison.OrdinalIgnoreCase) || locKey.Equals("Temp", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var locData = kvp.Value;
                    if (locData.Fish == null)
                        continue;

                    foreach (var fishEntry in locData.Fish)
                    {
                        if (fishEntry.ItemId == fishId || fishEntry.ItemId == $"(O){fishId}" || fishEntry.Id == fishId || fishEntry.Id == $"(O){fishId}")
                        {
                            string friendlyName = GetFriendlyLocationName(locKey, locData, fishEntry);
                            if (!results.Contains(friendlyName))
                            {
                                results.Add(friendlyName);
                            }
                        }
                    }
                }
            }
            catch { }
            return results;
        }

        private static string GetFriendlyLocationName(string locKey, LocationData locData, SpawnFishData fishEntry)
        {
            // Specific friendly names for standard Stardew fishing areas
            if (locKey.Equals("Forest", StringComparison.OrdinalIgnoreCase))
            {
                if (fishEntry.FishAreaId == "Pond") return "Forest Pond";
                if (fishEntry.FishAreaId == "River") return "Forest River";
                return "Cindersap Forest";
            }
            if (locKey.Equals("Town", StringComparison.OrdinalIgnoreCase)) return "Pelican Town (River)";
            if (locKey.Equals("Mountain", StringComparison.OrdinalIgnoreCase)) return "Mountain Lake";
            if (locKey.Equals("Beach", StringComparison.OrdinalIgnoreCase)) return "The Ocean (Beach)";
            if (locKey.Equals("Woods", StringComparison.OrdinalIgnoreCase)) return "Secret Woods";
            if (locKey.Equals("Desert", StringComparison.OrdinalIgnoreCase)) return "Calico Desert";
            if (locKey.Equals("UndergroundMine", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(fishEntry.FishAreaId))
                    return $"The Mines (Floor {fishEntry.FishAreaId})";
                return "The Mines";
            }
            if (locKey.Equals("Sewer", StringComparison.OrdinalIgnoreCase)) return "The Sewers";
            if (locKey.Equals("BugLand", StringComparison.OrdinalIgnoreCase)) return "Mutant Bug Lair";
            if (locKey.Equals("WitchSwamp", StringComparison.OrdinalIgnoreCase)) return "Witch's Swamp";
            if (locKey.Equals("Submarine", StringComparison.OrdinalIgnoreCase)) return "Night Market Submarine";
            if (locKey.Equals("IslandSouth", StringComparison.OrdinalIgnoreCase)) return "Ginger Island (South Ocean)";
            if (locKey.Equals("IslandWest", StringComparison.OrdinalIgnoreCase)) return "Ginger Island (West Ocean/River)";
            if (locKey.Equals("IslandNorth", StringComparison.OrdinalIgnoreCase)) return "Ginger Island (River)";
            if (locKey.Equals("IslandSouthEastCave", StringComparison.OrdinalIgnoreCase)) return "Pirate Cove";
            if (locKey.Equals("Caldera", StringComparison.OrdinalIgnoreCase)) return "Volcano Caldera";

            // Fallback 1: Resolve tokenized display names (e.g. [LocalizedText Strings\StringsFromCSFiles:...])
            if (!string.IsNullOrEmpty(locData.DisplayName))
            {
                string raw = locData.DisplayName;
                if (raw.StartsWith("[LocalizedText ") && raw.EndsWith("]"))
                {
                    try
                    {
                        string token = raw.Substring("[LocalizedText ".Length);
                        token = token.Substring(0, token.Length - 1).Trim();
                        string loaded = Game1.content.LoadString(token);
                        if (!string.IsNullOrEmpty(loaded))
                            return loaded;
                    }
                    catch { }
                }

                string parsed = TokenParser.ParseText(raw);
                if (!string.IsNullOrEmpty(parsed) && !parsed.StartsWith("["))
                {
                    return parsed;
                }
            }

            return locKey;
        }

        private static string FormatGameTime(string rawTime)
        {
            if (int.TryParse(rawTime, out int t))
            {
                int hours = t / 100;
                int mins = t % 100;
                string period = hours >= 12 && hours < 24 ? "PM" : "AM";
                if (hours > 12) hours -= 12;
                if (hours == 0) hours = 12;
                return $"{hours}:{(mins == 0 ? "00" : mins.ToString())} {period}";
            }
            return rawTime;
        }

        private static void AddCropDataSection(LookupSubject subject, Item item)
        {
            try
            {
                var crops = DataLoader.Crops(Game1.content);
                if (crops == null) return;

                // Check if this item is a seed or the harvested crop
                foreach (var kvp in crops)
                {
                    string seedId = kvp.Key;
                    var cropData = kvp.Value;
                    if (seedId == item.ItemId || seedId == item.QualifiedItemId || cropData.HarvestItemId == item.ItemId || cropData.HarvestItemId == item.QualifiedItemId)
                    {
                        var section = new LookupSection("Crop & Farming Info");

                        int totalDays = cropData.DaysInPhase != null ? cropData.DaysInPhase.Sum() : 0;
                        section.Fields.Add(new LookupField("Growth Time", $"{totalDays} days", new Color(0, 140, 0)));

                        if (cropData.RegrowDays > 0)
                        {
                            section.Fields.Add(new LookupField("Regrowth", $"Every {cropData.RegrowDays} days after first harvest", new Color(180, 100, 0)));
                        }

                        if (cropData.Seasons != null && cropData.Seasons.Count > 0)
                        {
                            string seasons = string.Join(", ", cropData.Seasons.Select(s => {
                                string key = $"season.{s.ToString().ToLower()}";
                                var tr = ModEntry.I18n.Get(key);
                                return tr.HasValue() ? tr.ToString() : s.ToString();
                            }));
                            section.Fields.Add(new LookupField("Harvest Seasons", seasons, new Color(46, 125, 50)));
                        }

                        if (cropData.IsRaised)
                        {
                            section.Fields.Add(new LookupField("Trellis Crop", "Yes (Cần Giàn - Không thể đi xuyên qua)", new Color(200, 60, 20)));
                        }

                        if (cropData.ExtraHarvestChance > 0)
                        {
                            section.Fields.Add(new LookupField("Extra Harvest Chance", $"{cropData.ExtraHarvestChance * 100:0.#}%", Color.DarkSlateGray));
                        }

                        subject.Sections.Add(section);
                        break;
                    }
                }
            }
            catch { }
        }

        private static void AddArtisanProductsSection(LookupSubject subject, Item item, int basePrice)
        {
            if (basePrice <= 0) return;

            var artisanLinks = new List<LookupLink>();

            // Fruits -> Wine, Jelly, Dried Fruit
            if (item.Category == StardewValley.Object.FruitsCategory)
            {
                int winePrice = basePrice * 3;
                var wineData = ItemRegistry.GetData("(O)348");
                artisanLinks.Add(new LookupLink(
                    text: $"{item.DisplayName} Wine ({winePrice}g)",
                    subtitle: "Keg",
                    textColor: new Color(180, 50, 180),
                    icon: wineData?.GetTexture(),
                    iconSourceRect: wineData?.GetSourceRect()
                ));

                int jellyPrice = basePrice * 2 + 50;
                var jellyData = ItemRegistry.GetData("(O)444");
                artisanLinks.Add(new LookupLink(
                    text: $"{item.DisplayName} Jelly ({jellyPrice}g)",
                    subtitle: "Preserves Jar",
                    textColor: new Color(200, 60, 20),
                    icon: jellyData?.GetTexture(),
                    iconSourceRect: jellyData?.GetSourceRect()
                ));

                int driedPrice = (int)(basePrice * 7.5);
                var driedData = ItemRegistry.GetData("(O)DriedFruit");
                if (driedData != null)
                {
                    artisanLinks.Add(new LookupLink(
                        text: $"Dried {item.DisplayName} ({driedPrice}g)",
                        subtitle: "Dehydrator (x5)",
                        textColor: new Color(180, 100, 0),
                        icon: driedData.GetTexture(),
                        iconSourceRect: driedData.GetSourceRect()
                    ));
                }
            }
            // Vegetables -> Juice, Pickles
            else if (item.Category == StardewValley.Object.VegetableCategory)
            {
                int juicePrice = (int)(basePrice * 2.25);
                var juiceData = ItemRegistry.GetData("(O)350");
                artisanLinks.Add(new LookupLink(
                    text: $"{item.DisplayName} Juice ({juicePrice}g)",
                    subtitle: "Keg",
                    textColor: new Color(0, 140, 0),
                    icon: juiceData?.GetTexture(),
                    iconSourceRect: juiceData?.GetSourceRect()
                ));

                int picklePrice = basePrice * 2 + 50;
                var pickleData = ItemRegistry.GetData("(O)342");
                artisanLinks.Add(new LookupLink(
                    text: $"Pickled {item.DisplayName} ({picklePrice}g)",
                    subtitle: "Preserves Jar",
                    textColor: new Color(180, 100, 0),
                    icon: pickleData?.GetTexture(),
                    iconSourceRect: pickleData?.GetSourceRect()
                ));
            }
            // Fish -> Smoked Fish
            else if (item.Category == StardewValley.Object.FishCategory || IsFishItem(item))
            {
                int smokedPrice = basePrice * 2;
                var smokedData = ItemRegistry.GetData("(O)SmokedFish");
                if (smokedData != null)
                {
                    artisanLinks.Add(new LookupLink(
                        text: $"Smoked {item.DisplayName} ({smokedPrice}g)",
                        subtitle: "Fish Smoker",
                        textColor: new Color(200, 60, 20),
                        icon: smokedData.GetTexture(),
                        iconSourceRect: smokedData.GetSourceRect()
                    ));
                }
            }

            if (artisanLinks.Count > 0)
            {
                var section = new LookupSection("Artisan Processing & Value");
                section.Fields.Add(new LookupField("Products", artisanLinks));
                subject.Sections.Add(section);
            }
        }

        private static List<string> GetFoodBuffs(Item item)
        {
            var buffs = new List<string>();
            try
            {
                if (Game1.objectData.TryGetValue(item.ItemId, out var data) && data.Buffs != null)
                {
                    foreach (var buff in data.Buffs)
                    {
                        var attrs = buff.CustomAttributes;
                        if (attrs != null)
                        {
                            if (attrs.FarmingLevel > 0) buffs.Add($"+{attrs.FarmingLevel} Farming");
                            if (attrs.MiningLevel > 0) buffs.Add($"+{attrs.MiningLevel} Mining");
                            if (attrs.FishingLevel > 0) buffs.Add($"+{attrs.FishingLevel} Fishing");
                            if (attrs.ForagingLevel > 0) buffs.Add($"+{attrs.ForagingLevel} Foraging");
                            if (attrs.CombatLevel > 0) buffs.Add($"+{attrs.CombatLevel} Combat");
                            if (attrs.LuckLevel > 0) buffs.Add($"+{attrs.LuckLevel} Luck");
                            if (attrs.Speed > 0) buffs.Add($"+{attrs.Speed} Speed");
                            if (attrs.Defense > 0) buffs.Add($"+{attrs.Defense} Defense");
                            if (attrs.Attack > 0) buffs.Add($"+{attrs.Attack} Attack");
                            if (attrs.MaxStamina > 0) buffs.Add($"+{attrs.MaxStamina} Max Energy");
                            if (attrs.MagneticRadius > 0) buffs.Add($"+{attrs.MagneticRadius} Magnetism");
                        }
                    }
                }
            }
            catch { }
            return buffs;
        }

        private static List<string> GetNeededBundles(Item item)
        {
            var results = new List<string>();
            try
            {
                if (Game1.player.hasCompletedCommunityCenter() || Game1.MasterPlayer.mailReceived.Contains("JojaMember"))
                    return results;

                var bundleData = DataLoader.Bundles(Game1.content);
                if (bundleData == null || Game1.netWorldState.Value.Bundles == null)
                    return results;

                foreach (var kvp in bundleData)
                {
                    string bundleKey = kvp.Key;
                    string[] keyParts = bundleKey.Split('/');
                    if (keyParts.Length < 2 || !int.TryParse(keyParts[1], out int bundleId))
                        continue;

                    string bundleValue = kvp.Value;
                    string[] parts = bundleValue.Split('/');
                    if (parts.Length < 3)
                        continue;

                    string bundleName = parts.Length >= 6 && !string.IsNullOrEmpty(parts[5]) ? parts[5] : parts[0];
                    string[] reqParts = parts[2].Split(' ');

                    if (Game1.netWorldState.Value.Bundles.TryGetValue(bundleId, out bool[] ingredientSlots))
                    {
                        int itemsRequired = parts.Length > 4 && int.TryParse(parts[4], out int req) ? req : ingredientSlots.Length;
                        int filledCount = ingredientSlots.Count(b => b);
                        if (filledCount >= itemsRequired)
                            continue;

                        for (int k = 0; k < ingredientSlots.Length; k++)
                        {
                            if (!ingredientSlots[k])
                            {
                                int reqIndex = k * 3;
                                if (reqIndex + 2 >= reqParts.Length)
                                    break;

                                string reqId = reqParts[reqIndex];
                                int reqMinQuality = int.TryParse(reqParts[reqIndex + 2], out int q) ? q : 0;

                                bool idMatch = reqId == item.ItemId ||
                                               reqId == item.QualifiedItemId ||
                                               (item is StardewValley.Object obj && (reqId == obj.ParentSheetIndex.ToString() || reqId == obj.ItemId));
                                bool catMatch = int.TryParse(reqId, out int cat) && cat < 0 && item.Category == cat;
                                bool qualityMatch = item.Quality >= reqMinQuality;

                                if ((idMatch || catMatch) && qualityMatch)
                                {
                                    if (!results.Contains(bundleName))
                                    {
                                        results.Add(bundleName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return results;
        }

        private static (List<LookupLink> Lovers, List<LookupLink> Likers) GetItemGiftTastesLinks(Item item)
        {
            var lovers = new List<LookupLink>();
            var likers = new List<LookupLink>();

            foreach (var npc in Utility.getAllCharacters())
            {
                if (npc == null || !npc.IsVillager || npc.IsMonster || string.IsNullOrEmpty(npc.Name))
                    continue;

                int taste = npc.getGiftTasteForThisItem(item);
                if (taste == NPC.gift_taste_love && !lovers.Any(l => l.Text == (npc.displayName ?? npc.Name)))
                {
                    var targetNPC = npc;
                    lovers.Add(new LookupLink(
                        text: targetNPC.displayName ?? targetNPC.Name,
                        textColor: new Color(180, 50, 180),
                        icon: targetNPC.Portrait,
                        iconSourceRect: new Rectangle(0, 0, 64, 64),
                        onClick: () => BuildNPCSubject(targetNPC)
                    ));
                }
                else if (taste == NPC.gift_taste_like && !likers.Any(l => l.Text == (npc.displayName ?? npc.Name)))
                {
                    var targetNPC = npc;
                    likers.Add(new LookupLink(
                        text: targetNPC.displayName ?? targetNPC.Name,
                        textColor: new Color(0, 140, 0),
                        icon: targetNPC.Portrait,
                        iconSourceRect: new Rectangle(0, 0, 64, 64),
                        onClick: () => BuildNPCSubject(targetNPC)
                    ));
                }
            }

            return (lovers.Take(12).ToList(), likers.Take(12).ToList());
        }

        private static List<LookupLink> GetRecipesUsingItemLinks(Item item)
        {
            var recipes = new List<LookupLink>();

            foreach (var kvp in CraftingRecipe.craftingRecipes)
            {
                string recipeName = kvp.Key;
                string recipeStr = kvp.Value;
                if (RecipeContainsItem(recipeStr, item) && !recipes.Any(r => r.Text == recipeName))
                {
                    var recipe = new CraftingRecipe(recipeName, isCookingRecipe: false);
                    var outputItem = recipe.createItem();
                    var itemData = outputItem != null ? ItemRegistry.GetData(outputItem.QualifiedItemId) : null;

                    recipes.Add(new LookupLink(
                        text: recipe.DisplayName,
                        textColor: Game1.textColor,
                        icon: itemData?.GetTexture(),
                        iconSourceRect: itemData?.GetSourceRect(),
                        onClick: () => outputItem != null ? BuildItemSubject(outputItem) : null
                    ));
                }
            }

            foreach (var kvp in CraftingRecipe.cookingRecipes)
            {
                string recipeName = kvp.Key;
                string recipeStr = kvp.Value;
                if (RecipeContainsItem(recipeStr, item) && !recipes.Any(r => r.Text == recipeName))
                {
                    var recipe = new CraftingRecipe(recipeName, isCookingRecipe: true);
                    var outputItem = recipe.createItem();
                    var itemData = outputItem != null ? ItemRegistry.GetData(outputItem.QualifiedItemId) : null;

                    recipes.Add(new LookupLink(
                        text: recipe.DisplayName,
                        textColor: Game1.textColor,
                        icon: itemData?.GetTexture(),
                        iconSourceRect: itemData?.GetSourceRect(),
                        onClick: () => outputItem != null ? BuildItemSubject(outputItem) : null
                    ));
                }
            }

            return recipes.Take(12).ToList();
        }

        private static bool RecipeContainsItem(string recipeStr, Item item)
        {
            string[] parts = recipeStr.Split('/');
            if (parts.Length < 1)
                return false;

            string[] ingredients = parts[0].Split(' ');
            for (int i = 0; i < ingredients.Length; i += 2)
            {
                string ingId = ingredients[i];
                if (ingId == item.ItemId || ingId == item.QualifiedItemId || ingId == item.Category.ToString())
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 3. Monster Lookup

        public static LookupSubject BuildMonsterSubject(Monster monster)
        {
            var subject = new LookupSubject
            {
                Title = monster.displayName ?? monster.Name,
                Subtitle = ModEntry.I18n.Get("lookup.type.monster").ToString()
            };

            var statsSection = new LookupSection(ModEntry.I18n.Get("lookup.section.combat"));
            statsSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.monster.health"),
                $"{monster.Health} / {monster.MaxHealth}",
                new Color(220, 20, 60)
            ));

            statsSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.monster.damage"),
                monster.DamageToFarmer.ToString(),
                new Color(200, 60, 20)
            ));

            statsSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.monster.defense"),
                monster.resilience.Value.ToString(),
                new Color(20, 110, 220)
            ));

            int xp = monster.ExperienceGained;
            statsSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.monster.experience"),
                xp.ToString(),
                new Color(180, 100, 0)
            ));
            subject.Sections.Add(statsSection);

            var dropsSection = new LookupSection(ModEntry.I18n.Get("lookup.section.drops"));
            var dropLinks = new List<LookupLink>();
            foreach (var dropId in monster.objectsToDrop)
            {
                string rawId = dropId;
                var dropData = ItemRegistry.GetData(rawId) ?? ItemRegistry.GetData($"(O){rawId}");
                if (dropData != null && !dropLinks.Any(l => l.Text == dropData.DisplayName))
                {
                    dropLinks.Add(new LookupLink(
                        text: dropData.DisplayName,
                        textColor: Game1.textColor,
                        icon: dropData.GetTexture(),
                        iconSourceRect: dropData.GetSourceRect(),
                        onClick: () =>
                        {
                            var item = ItemRegistry.Create(dropData.QualifiedItemId);
                            return item != null ? BuildItemSubject(item) : null;
                        }
                    ));
                }
            }

            if (dropLinks.Count > 0)
            {
                dropsSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.monster.possible-drops"),
                    dropLinks
                ));
            }
            else
            {
                dropsSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.monster.possible-drops"),
                    ModEntry.I18n.Get("lookup.common.none"),
                    Color.DarkSlateGray
                ));
            }
            subject.Sections.Add(dropsSection);

            return subject;
        }

        #endregion

        #region 4. Animal & Pet Lookup

        public static LookupSubject BuildAnimalSubject(FarmAnimal animal)
        {
            var info = AnimalHelper.GetFarmAnimalInfo(animal);
            var subject = new LookupSubject
            {
                Title = animal.Name,
                Subtitle = animal.displayType
            };

            var statusSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (info != null)
            {
                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.friendship"),
                    $"{info.Hearts:0.0} / 5.0 ♥ ({info.FriendshipPoints} pts)",
                    new Color(220, 20, 60)
                ));

                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.petted-today"),
                    info.WasPetToday ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"),
                    info.WasPetToday ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                int happiness = animal.happiness.Value;
                string mood = happiness >= 200 ? ModEntry.I18n.Get("lookup.animal.mood-very-happy").ToString()
                            : happiness >= 100 ? ModEntry.I18n.Get("lookup.animal.mood-happy").ToString()
                            : ModEntry.I18n.Get("lookup.animal.mood-unhappy").ToString();

                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.happiness"),
                    $"{happiness}/255 ({mood})",
                    happiness >= 100 ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                if (info.HasProduceReady && !string.IsNullOrEmpty(info.ProduceName))
                {
                    statusSection.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.animal.produce"),
                        $"{info.ProduceName} ({ModEntry.I18n.Get("lookup.animal.ready")})",
                        new Color(0, 140, 0)
                    ));
                }
            }
            subject.Sections.Add(statusSection);

            return subject;
        }

        public static LookupSubject BuildPetSubject(Pet pet)
        {
            var info = AnimalHelper.GetPetInfo(pet);
            var subject = new LookupSubject
            {
                Title = pet.Name,
                Subtitle = pet.petType.Value ?? ModEntry.I18n.Get("hover.type.pet").ToString()
            };

            var statusSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (info != null)
            {
                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.friendship"),
                    $"{info.Hearts:0.0} / 5.0 ♥ ({info.FriendshipPoints} pts)",
                    new Color(220, 20, 60)
                ));

                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.petted-today"),
                    info.WasPetToday ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"),
                    info.WasPetToday ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));
            }
            subject.Sections.Add(statusSection);

            return subject;
        }

        #endregion

        #region 5. Tree & Bush Lookup

        public static LookupSubject BuildTreeSubject(Tree tree)
        {
            var info = TreeHelper.GetTreeInfo(tree);
            var subject = new LookupSubject
            {
                Title = info?.Name ?? ModEntry.I18n.Get("hover.tree.generic").ToString(),
                Subtitle = ModEntry.I18n.Get("hover.type.tree").ToString()
            };

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (info != null)
            {
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("hover.tree.stage", new { stage = info.GrowthStage + 1, total = 5 }),
                    info.IsMature ? ModEntry.I18n.Get("hover.tree.fully-grown") : $"Stage {info.GrowthStage + 1}/5",
                    info.IsMature ? new Color(0, 140, 0) : new Color(180, 100, 0)
                ));

                section.Fields.Add(new LookupField(
                    "Moss",
                    info.HasMoss ? ModEntry.I18n.Get("hover.tree.has-moss") : ModEntry.I18n.Get("lookup.common.no"),
                    info.HasMoss ? new Color(46, 125, 50) : Color.DarkSlateGray
                ));

                section.Fields.Add(new LookupField(
                    "Tapper",
                    info.IsTapped ? ModEntry.I18n.Get("hover.tree.tapped") : ModEntry.I18n.Get("lookup.common.no"),
                    info.IsTapped ? new Color(20, 110, 220) : Color.DarkSlateGray
                ));
            }
            subject.Sections.Add(section);
            return subject;
        }

        public static LookupSubject BuildFruitTreeSubject(FruitTree fruitTree)
        {
            var info = TreeHelper.GetFruitTreeInfo(fruitTree);
            var subject = new LookupSubject
            {
                Title = info?.Name ?? ModEntry.I18n.Get("hover.fruit-tree.generic").ToString(),
                Subtitle = ModEntry.I18n.Get("hover.type.fruit-tree").ToString()
            };

            if (info?.IconTexture != null)
            {
                subject.MainIcon = info.IconTexture;
                subject.MainIconSourceRect = info.IconSourceRect;
            }

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (info != null)
            {
                if (!info.IsMature)
                {
                    section.Fields.Add(new LookupField(
                        "Maturation",
                        ModEntry.I18n.Get("hover.fruit-tree.maturing", new { days = info.DaysUntilMature }),
                        new Color(180, 100, 0)
                    ));
                }
                else
                {
                    section.Fields.Add(new LookupField(
                        "Fruit Count",
                        $"{info.FruitsOnTree} / 3",
                        info.FruitsOnTree > 0 ? new Color(0, 140, 0) : Color.DarkSlateGray
                    ));

                    section.Fields.Add(new LookupField(
                        "Season",
                        info.IsInSeason ? ModEntry.I18n.Get("hover.fruit-tree.in-season") : ModEntry.I18n.Get("hover.fruit-tree.out-of-season"),
                        info.IsInSeason ? new Color(20, 110, 220) : Color.DarkSlateGray
                    ));
                }
            }
            subject.Sections.Add(section);
            return subject;
        }

        public static LookupSubject BuildBushSubject(Bush bush)
        {
            var info = TreeHelper.GetBushInfo(bush);
            var subject = new LookupSubject
            {
                Title = info?.Name ?? ModEntry.I18n.Get("hover.bush.generic").ToString(),
                Subtitle = ModEntry.I18n.Get("hover.bush.generic").ToString()
            };

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (info != null)
            {
                if (info.IsTeaBush && !info.IsMature)
                {
                    section.Fields.Add(new LookupField(
                        "Maturation",
                        ModEntry.I18n.Get("hover.bush.tea-maturing", new { days = info.DaysUntilMature }),
                        new Color(180, 100, 0)
                    ));
                }
                else if (info.IsInBloom)
                {
                    section.Fields.Add(new LookupField(
                        "Harvest",
                        ModEntry.I18n.Get("hover.bush.ready-to-harvest"),
                        new Color(0, 140, 0)
                    ));
                }
            }
            subject.Sections.Add(section);
            return subject;
        }

        #endregion

        #region 6. Fish Pond / Building Lookup

        public static LookupSubject BuildFishPondSubject(FishPond pond)
        {
            string fishId = pond.fishType.Value;
            var fishData = ItemRegistry.GetData(fishId) ?? ItemRegistry.GetData($"(O){fishId}");
            string fishName = fishData?.DisplayName ?? ModEntry.I18n.Get("lookup.building.fish").ToString();

            var subject = new LookupSubject
            {
                Title = ModEntry.I18n.Get("lookup.building.fish-pond-title", new { fish = fishName }),
                Subtitle = ModEntry.I18n.Get("lookup.type.building").ToString()
            };

            if (fishData != null)
            {
                try
                {
                    subject.MainIcon = fishData.GetTexture();
                    subject.MainIconSourceRect = fishData.GetSourceRect();
                }
                catch { }
            }

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            section.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.building.population"),
                $"{pond.FishCount} / {pond.maxOccupants.Value}",
                new Color(20, 110, 220)
            ));

            if (pond.hasSpawnedFish.Value)
            {
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.building.next-spawn"),
                    ModEntry.I18n.Get("lookup.common.ready"),
                    new Color(0, 140, 0)
                ));
            }

            if (pond.neededItem.Value != null)
            {
                var reqData = ItemRegistry.GetData(pond.neededItem.Value.QualifiedItemId);
                var reqLink = new LookupLink(
                    text: $"{pond.neededItemCount.Value}x {pond.neededItem.Value.DisplayName}",
                    textColor: new Color(200, 60, 20),
                    icon: reqData?.GetTexture(),
                    iconSourceRect: reqData?.GetSourceRect(),
                    onClick: () => BuildItemSubject(pond.neededItem.Value)
                );

                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.building.quest-item"),
                    new List<LookupLink> { reqLink }
                ));
            }

            subject.Sections.Add(section);
            return subject;
        }

        public static LookupSubject BuildTileSubject(GameLocation location, Vector2 tilePos)
        {
            return BuildWorldOverviewSubject(location, tilePos);
        }

        public static LookupSubject BuildWorldOverviewSubject(GameLocation? location = null, Vector2? tilePos = null)
        {
            string locName = location != null ? (location.DisplayName ?? location.Name) : $"{Game1.player.farmName.Value} Farm";
            string title = tilePos.HasValue ? $"{locName} ({tilePos.Value.X}, {tilePos.Value.Y})" : $"{locName} - Daily Almanac";

            var subject = new LookupSubject
            {
                Title = title,
                Subtitle = GetFullDateString()
            };

            // 1. Calendar, Birthdays & Festivals
            subject.Sections.Add(BuildCalendarSection());

            // 2. Daily Luck & Fortune
            subject.Sections.Add(BuildDailyLuckSection());

            // 3. Weather Forecast (Today & Tomorrow)
            subject.Sections.Add(BuildWeatherSection(location));

            // 4. TV & Special Events (Queen of Sauce & Traveling Merchant)
            var eventSec = BuildSpecialEventsSection();
            if (eventSec.Fields.Count > 0)
            {
                subject.Sections.Add(eventSec);
            }

            // 5. Farm & Chores Summary
            subject.Sections.Add(BuildFarmSummarySection());

            // 6. Tile Specifics (if clicked on tile)
            if (location != null && tilePos.HasValue)
            {
                subject.Sections.Add(BuildTileDetailsSection(location, tilePos.Value));
            }

            return subject;
        }

        private static string GetFullDateString()
        {
            int day = Game1.dayOfMonth;
            string dayOfWeek = ((day - 1) % 7) switch
            {
                0 => "Monday",
                1 => "Tuesday",
                2 => "Wednesday",
                3 => "Thursday",
                4 => "Friday",
                5 => "Saturday",
                6 => "Sunday",
                _ => string.Empty
            };
            string seasonKey = $"season.{Game1.currentSeason.ToLower()}";
            var tr = ModEntry.I18n.Get(seasonKey);
            string season = tr.HasValue() ? tr.ToString() : (char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1));
            return $"{dayOfWeek}, {season} {day}, Year {Game1.year}";
        }

        private static LookupSection BuildCalendarSection()
        {
            var section = new LookupSection("Calendar & Events");

            // Today's Birthday
            var todayBirthdayNpcs = new List<NPC>();
            var upcomingBirthdays = new List<(NPC Npc, int DaysUntil)>();

            foreach (var npc in Utility.getAllCharacters())
            {
                if (npc == null || !npc.IsVillager || npc.IsMonster || string.IsNullOrEmpty(npc.Name))
                    continue;

                if (string.Equals(npc.Birthday_Season, Game1.currentSeason, StringComparison.OrdinalIgnoreCase))
                {
                    if (npc.Birthday_Day == Game1.dayOfMonth)
                    {
                        todayBirthdayNpcs.Add(npc);
                    }
                    else if (npc.Birthday_Day > Game1.dayOfMonth && npc.Birthday_Day <= Game1.dayOfMonth + 7)
                    {
                        upcomingBirthdays.Add((npc, npc.Birthday_Day - Game1.dayOfMonth));
                    }
                }
            }

            if (todayBirthdayNpcs.Count > 0)
            {
                var bdayLinks = new List<LookupLink>();
                foreach (var npc in todayBirthdayNpcs)
                {
                    var target = npc;
                    bdayLinks.Add(new LookupLink(
                        text: $"{target.displayName ?? target.Name} (Birthday Today!)",
                        textColor: new Color(180, 50, 180),
                        icon: target.Portrait,
                        iconSourceRect: new Rectangle(0, 0, 64, 64),
                        onClick: () => BuildNPCSubject(target)
                    ));
                }
                section.Fields.Add(new LookupField("Today's Birthday", bdayLinks));
            }
            else
            {
                section.Fields.Add(new LookupField("Today's Birthday", "None", Color.DarkSlateGray));
            }

            if (upcomingBirthdays.Count > 0)
            {
                var upLinks = new List<LookupLink>();
                foreach (var (npc, days) in upcomingBirthdays.OrderBy(u => u.DaysUntil))
                {
                    var target = npc;
                    string dayText = days == 1 ? "Tomorrow" : $"In {days} days";
                    upLinks.Add(new LookupLink(
                        text: $"{target.displayName ?? target.Name} ({dayText})",
                        textColor: Game1.textColor,
                        icon: target.Portrait,
                        iconSourceRect: new Rectangle(0, 0, 64, 64),
                        onClick: () => BuildNPCSubject(target)
                    ));
                }
                section.Fields.Add(new LookupField("Upcoming Birthdays", upLinks));
            }

            // Festivals / Special Days
            string? festivalToday = GetFestivalName(Game1.currentSeason, Game1.dayOfMonth);
            if (!string.IsNullOrEmpty(festivalToday))
            {
                section.Fields.Add(new LookupField("Festival Today", festivalToday, new Color(200, 60, 20)));
            }

            // Upcoming Festival
            for (int d = Game1.dayOfMonth + 1; d <= Math.Min(28, Game1.dayOfMonth + 7); d++)
            {
                string? fest = GetFestivalName(Game1.currentSeason, d);
                if (!string.IsNullOrEmpty(fest))
                {
                    int daysAway = d - Game1.dayOfMonth;
                    section.Fields.Add(new LookupField("Upcoming Festival", $"{fest} (in {daysAway} days)", new Color(180, 100, 0)));
                    break;
                }
            }

            return section;
        }

        private static string? GetFestivalName(string season, int day)
        {
            string s = season.ToLower();
            if (s == "spring")
            {
                if (day == 13) return "Egg Festival (Town Square, 9:00 AM - 2:00 PM)";
                if (day == 24) return "Flower Dance (Cindersap Forest, 9:00 AM - 2:00 PM)";
                if (day >= 15 && day <= 17) return "Desert Festival (Calico Desert)";
            }
            else if (s == "summer")
            {
                if (day == 11) return "Luau (The Beach, 9:00 AM - 2:00 PM)";
                if (day == 28) return "Dance of the Moonlight Jellies (The Beach, 10:00 PM - 12:00 AM)";
                if (day == 20 || day == 21) return "Trout Derby (Cindersap Forest)";
            }
            else if (s == "fall")
            {
                if (day == 16) return "Stardew Valley Fair (Town Square, 9:00 AM - 3:00 PM)";
                if (day == 27) return "Spirit's Eve (Town Square, 10:00 PM - 11:50 PM)";
            }
            else if (s == "winter")
            {
                if (day == 8) return "Festival of Ice (Cindersap Forest, 9:00 AM - 2:00 PM)";
                if (day >= 15 && day <= 17) return "Night Market (The Beach, 5:00 PM - 2:00 AM)";
                if (day == 25) return "Feast of the Winter Star (Town Square, 9:00 AM - 2:00 PM)";
                if (day == 12 || day == 13) return "SquidFest (The Beach)";
            }
            return null;
        }

        private static LookupSection BuildDailyLuckSection()
        {
            var section = new LookupSection("Daily Luck & Fortune");
            double luck = Game1.player.DailyLuck;

            string fortuneText;
            Color fortuneColor;

            if (luck > 0.07)
            {
                fortuneText = "The spirits are very happy today! They will do their best to shower everyone with good fortune. (Very Lucky)";
                fortuneColor = new Color(0, 140, 0);
            }
            else if (luck > 0.02)
            {
                fortuneText = "The spirits are in good humor today. I think you'll have a little extra luck. (Lucky)";
                fortuneColor = new Color(46, 125, 50);
            }
            else if (luck >= -0.02)
            {
                fortuneText = "The spirits feel neutral today. The day is in your hands. (Neutral)";
                fortuneColor = Color.DarkSlateGray;
            }
            else if (luck >= -0.07)
            {
                fortuneText = "This is not your day. The spirits are somewhat displeased. (Unlucky)";
                fortuneColor = new Color(200, 100, 20);
            }
            else
            {
                fortuneText = "The spirits are very displeased today. They will do their best to make your life difficult. (Very Unlucky)";
                fortuneColor = new Color(220, 20, 60);
            }

            section.Fields.Add(new LookupField("Spirits Forecast", fortuneText, fortuneColor));

            string luckSign = luck >= 0 ? $"+{luck:F3}" : $"{luck:F3}";
            section.Fields.Add(new LookupField("Daily Luck Modifier", luckSign, luck >= 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)));

            if (Game1.player.hasSpecialCharm)
            {
                section.Fields.Add(new LookupField("Special Charm", "Active (+0.025 permanent luck bonus)", new Color(180, 50, 180)));
            }

            return section;
        }

        private static LookupSection BuildWeatherSection(GameLocation? location)
        {
            var section = new LookupSection("Weather Forecast");

            // Today's Weather
            string todayWeather = Game1.isGreenRain ? "Green Rain"
                                : Game1.isLightning ? "Lightning Storm"
                                : Game1.isSnowing ? "Snowing"
                                : Game1.isRaining ? "Raining"
                                : Game1.isDebrisWeather ? "Windy / Spring Debris"
                                : "Sunny & Clear";

            Color todayColor = (Game1.isRaining || Game1.isLightning || Game1.isGreenRain) ? new Color(20, 110, 220) : new Color(180, 100, 0);
            section.Fields.Add(new LookupField("Today's Weather", todayWeather, todayColor));

            // Tomorrow's Weather Forecast
            string tomorrowKey = Game1.weatherForTomorrow;
            string tomorrowWeather = tomorrowKey switch
            {
                Game1.weather_rain => "Rainy",
                Game1.weather_lightning => "Lightning Storm",
                Game1.weather_snow => "Snowy",
                Game1.weather_green_rain => "Green Rain",
                Game1.weather_debris => "Windy / Debris",
                _ => "Sunny"
            };

            Color tomorrowColor = (tomorrowKey == Game1.weather_rain || tomorrowKey == Game1.weather_lightning || tomorrowKey == Game1.weather_green_rain)
                ? new Color(20, 110, 220)
                : new Color(180, 100, 0);

            section.Fields.Add(new LookupField("Tomorrow's Forecast", tomorrowWeather, tomorrowColor));

            // Ginger Island Forecast (if unlocked)
            if (Game1.netWorldState.Value.IslandVisitors.Count > 0 || Game1.player.hasOrWillReceiveMail("Visited_Island"))
            {
                var islandLoc = Game1.getLocationFromName("IslandSouth");
                if (islandLoc != null)
                {
                    string islandToday = islandLoc.IsRainingHere() ? "Rainy" : "Sunny";
                    section.Fields.Add(new LookupField("Ginger Island Weather", islandToday, islandLoc.IsRainingHere() ? new Color(20, 110, 220) : new Color(180, 100, 0)));
                }
            }

            return section;
        }

        private static LookupSection BuildSpecialEventsSection()
        {
            var section = new LookupSection("Events, TV & Quests");

            // Bookseller (1.6 Feature)
            bool isBookseller = Game1.getLocationFromName("Town")?.characters.Any(c => c.Name.Equals("Bookseller", StringComparison.OrdinalIgnoreCase)) == true;
            section.Fields.Add(new LookupField("Bookseller", isBookseller ? "Visiting Town today! (Behind JojaMart)" : "Not visiting today", isBookseller ? new Color(180, 50, 180) : Color.DarkSlateGray));

            // Tool Upgrade at Clint's
            if (Game1.player.daysLeftForToolUpgrade.Value > 0)
            {
                int days = Game1.player.daysLeftForToolUpgrade.Value;
                string readyText = days == 1 ? "Ready tomorrow!" : $"Ready in {days} days";
                section.Fields.Add(new LookupField("Tool Upgrade", $"Clint is upgrading a tool ({readyText})", new Color(180, 100, 0)));
            }

            // Active Quests & Special Orders
            int billboardQuests = Game1.player.questLog.Count;
            int specialOrders = Game1.player.team.specialOrders.Count;
            if (billboardQuests > 0 || specialOrders > 0)
            {
                section.Fields.Add(new LookupField("Active Quests", $"{billboardQuests} billboard quest(s), {specialOrders} special order(s) active", new Color(20, 110, 220)));
            }

            // Queen of Sauce
            int day = Game1.dayOfMonth;
            int dayOfWeek = (day - 1) % 7; // 6 = Sun, 2 = Wed
            if (dayOfWeek == 6)
            {
                var qos = GetQueenOfSauceRecipe(isSunday: true);
                if (qos.HasValue)
                {
                    string status = qos.Value.Known ? "Already Known" : "New Recipe! (Watch TV to Learn)";
                    Color statusColor = qos.Value.Known ? Color.DarkSlateGray : new Color(0, 140, 0);
                    section.Fields.Add(new LookupField("Queen of Sauce (Sunday)", $"{qos.Value.RecipeName} - {status}", statusColor));
                }
            }
            else if (dayOfWeek == 2)
            {
                var qos = GetQueenOfSauceRecipe(isSunday: false);
                if (qos.HasValue)
                {
                    string status = qos.Value.Known ? "Already Known" : "New Recipe! (Watch TV to Learn)";
                    Color statusColor = qos.Value.Known ? Color.DarkSlateGray : new Color(0, 140, 0);
                    section.Fields.Add(new LookupField("Queen of Sauce (Rerun)", $"{qos.Value.RecipeName} - {status}", statusColor));
                }
            }

            // Traveling Merchant
            bool isCartDay = (dayOfWeek == 4 || dayOfWeek == 6) || (Game1.currentSeason == "winter" && day >= 15 && day <= 17);
            if (isCartDay)
            {
                section.Fields.Add(new LookupField("Traveling Merchant", "Visiting Cindersap Forest today (Open 6:00 AM - 8:00 PM)", new Color(180, 50, 180)));
            }
            else
            {
                int daysToFri = ((4 - dayOfWeek) + 7) % 7;
                if (daysToFri == 0) daysToFri = 7;
                section.Fields.Add(new LookupField("Traveling Merchant", $"Next visit in {daysToFri} days (Friday)", Color.DarkSlateGray));
            }

            return section;
        }

        private static (string RecipeName, bool Known)? GetQueenOfSauceRecipe(bool isSunday)
        {
            try
            {
                var recipes = DataLoader.Tv_CookingChannel(Game1.content);
                if (recipes == null) return null;

                if (isSunday)
                {
                    int seasonOffset = Game1.currentSeason switch
                    {
                        "summer" => 28,
                        "fall" => 56,
                        "winter" => 84,
                        _ => 0
                    };
                    int recipeNum = ((Game1.year - 1) * 112 + Game1.dayOfMonth + seasonOffset) / 7;
                    if (recipes.TryGetValue(recipeNum.ToString(), out string? data))
                    {
                        string[] parts = data.Split('/');
                        if (parts.Length > 0)
                        {
                            string recipeName = parts[0];
                            bool known = Game1.player.cookingRecipes.ContainsKey(recipeName);
                            return (recipeName, known);
                        }
                    }
                }
                else
                {
                    // Wednesday Rerun
                    foreach (var kvp in recipes)
                    {
                        string[] parts = kvp.Value.Split('/');
                        if (parts.Length > 0 && !Game1.player.cookingRecipes.ContainsKey(parts[0]))
                        {
                            return (parts[0], false);
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static LookupSection BuildFarmSummarySection()
        {
            var section = new LookupSection("Farm & Daily Chores");

            int unwateredCrops = 0;
            int readyCrops = 0;
            int deadCrops = 0;
            int unpettedAnimals = 0;
            int readyProduce = 0;
            int readyMachines = 0;

            try
            {
                var farm = Game1.getFarm();
                if (farm != null)
                {
                    // Crops
                    foreach (var pair in farm.terrainFeatures.Pairs)
                    {
                        if (pair.Value is HoeDirt dirt && dirt.crop != null)
                        {
                            if (dirt.crop.dead.Value) deadCrops++;
                            else if (dirt.readyForHarvest()) readyCrops++;
                            else if (dirt.needsWatering() && dirt.state.Value != HoeDirt.watered) unwateredCrops++;
                        }
                    }

                    // Animals
                    foreach (var animal in farm.getAllFarmAnimals())
                    {
                        if (!animal.wasPet.Value) unpettedAnimals++;
                        if (animal.currentProduce.Value != null) readyProduce++;
                    }

                    // Machines on Farm & Indoors
                    foreach (var obj in farm.objects.Values)
                    {
                        if (obj.readyForHarvest.Value) readyMachines++;
                    }
                    foreach (var building in farm.buildings)
                    {
                        if (building.indoors.Value != null)
                        {
                            foreach (var obj in building.indoors.Value.objects.Values)
                            {
                                if (obj.readyForHarvest.Value) readyMachines++;
                            }
                        }
                    }
                }
            }
            catch { }

            // Crops Field
            if (unwateredCrops == 0 && readyCrops == 0 && deadCrops == 0)
            {
                section.Fields.Add(new LookupField("Crops", "All crops watered & taken care of!", new Color(0, 140, 0)));
            }
            else
            {
                var cropParts = new List<string>();
                if (unwateredCrops > 0) cropParts.Add($"{unwateredCrops} unwatered");
                if (readyCrops > 0) cropParts.Add($"{readyCrops} ready to harvest");
                if (deadCrops > 0) cropParts.Add($"{deadCrops} dead (clear with scythe)");
                section.Fields.Add(new LookupField("Crops", string.Join(", ", cropParts), unwateredCrops > 0 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
            }

            // Animals Field
            if (unpettedAnimals == 0 && readyProduce == 0)
            {
                section.Fields.Add(new LookupField("Animals", "All animals loved & petted today!", new Color(0, 140, 0)));
            }
            else
            {
                var animalParts = new List<string>();
                if (unpettedAnimals > 0) animalParts.Add($"{unpettedAnimals} need petting");
                if (readyProduce > 0) animalParts.Add($"{readyProduce} produce ready");
                section.Fields.Add(new LookupField("Animals", string.Join(", ", animalParts), unpettedAnimals > 0 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
            }

            // Machines Field
            if (readyMachines > 0)
            {
                section.Fields.Add(new LookupField("Machines", $"{readyMachines} machines ready to collect", new Color(0, 140, 0)));
            }
            else
            {
                section.Fields.Add(new LookupField("Machines", "No machines ready to collect", Color.DarkSlateGray));
            }

            // Silo Hay
            try
            {
                var farm = Game1.getFarm();
                if (farm != null)
                {
                    int hay = farm.piecesOfHay.Value;
                    int maxHay = farm.buildings.Count(b => b.buildingType.Value.Contains("Silo")) * 240;
                    if (maxHay > 0)
                    {
                        section.Fields.Add(new LookupField("Silo Hay", $"{hay} / {maxHay} Hay", hay < maxHay / 4 ? new Color(200, 60, 20) : Game1.textColor));
                    }
                }
            }
            catch { }

            // Greenhouse Crops
            try
            {
                var greenhouse = Game1.getLocationFromName("Greenhouse");
                if (greenhouse != null)
                {
                    int ghUnwatered = 0;
                    int ghReady = 0;
                    foreach (var pair in greenhouse.terrainFeatures.Pairs)
                    {
                        if (pair.Value is HoeDirt dirt && dirt.crop != null && !dirt.crop.dead.Value)
                        {
                            if (dirt.readyForHarvest()) ghReady++;
                            else if (dirt.needsWatering() && dirt.state.Value != HoeDirt.watered) ghUnwatered++;
                        }
                    }
                    if (ghUnwatered > 0 || ghReady > 0)
                    {
                        section.Fields.Add(new LookupField("Greenhouse", $"{ghUnwatered} unwatered, {ghReady} ready to harvest", ghUnwatered > 0 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
                    }
                }
            }
            catch { }

            // Ginger Island Farm (if unlocked)
            try
            {
                var islandFarm = Game1.getLocationFromName("IslandWest");
                if (islandFarm != null)
                {
                    int islUnwatered = 0;
                    int islReady = 0;
                    foreach (var pair in islandFarm.terrainFeatures.Pairs)
                    {
                        if (pair.Value is HoeDirt dirt && dirt.crop != null && !dirt.crop.dead.Value)
                        {
                            if (dirt.readyForHarvest()) islReady++;
                            else if (dirt.needsWatering() && dirt.state.Value != HoeDirt.watered) islUnwatered++;
                        }
                    }
                    if (islUnwatered > 0 || islReady > 0)
                    {
                        section.Fields.Add(new LookupField("Island Farm", $"{islUnwatered} unwatered, {islReady} ready to harvest", islUnwatered > 0 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
                    }
                }
            }
            catch { }

            return section;
        }

        private static LookupSection BuildTileDetailsSection(GameLocation location, Vector2 tilePos)
        {
            var section = new LookupSection("Tile Location Details");
            section.Fields.Add(new LookupField("Location", location.DisplayName ?? location.Name, new Color(20, 110, 220)));
            section.Fields.Add(new LookupField("Tile Position", $"X: {tilePos.X}, Y: {tilePos.Y}", Game1.textColor));

            bool isWater = location.isWaterTile((int)tilePos.X, (int)tilePos.Y);
            bool isPassable = location.isTilePassable(new xTile.Dimensions.Location((int)tilePos.X, (int)tilePos.Y), Game1.viewport);

            section.Fields.Add(new LookupField("Tile Type", isWater ? "Water Tile" : (isPassable ? "Walkable Ground" : "Obstacle / Blocked"), isWater ? new Color(20, 110, 220) : (isPassable ? new Color(0, 140, 0) : new Color(200, 60, 20))));

            return section;
        }

        #endregion

        #region 7. Find Anything Query Engine (Live Search)

        public static List<LookupLink> SearchAll(string query)
        {
            var results = new List<LookupLink>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            string q = query.Trim().ToLower();

            // 1. Search Villagers
            foreach (var npc in Utility.getAllCharacters())
            {
                if (npc != null && npc.IsVillager && !npc.IsMonster && !string.IsNullOrEmpty(npc.Name))
                {
                    string name = npc.displayName ?? npc.Name;
                    if (name.ToLower().Contains(q) && !results.Any(r => r.Text == name))
                    {
                        var target = npc;
                        results.Add(new LookupLink(
                            text: name,
                            subtitle: "Villager",
                            textColor: new Color(180, 50, 180),
                            icon: target.Portrait,
                            iconSourceRect: new Rectangle(0, 0, 64, 64),
                            onClick: () => BuildNPCSubject(target)
                        ));
                    }
                }
            }

            // 2. Search Items across all categories using typeDef.GetAllIds()
            foreach (var typeDef in ItemRegistry.ItemTypes)
            {
                if (typeDef == null)
                    continue;

                foreach (string id in typeDef.GetAllIds())
                {
                    var itemData = typeDef.GetData(id);
                    if (itemData != null && !string.IsNullOrEmpty(itemData.DisplayName))
                    {
                        if (itemData.DisplayName.ToLower().Contains(q) && !results.Any(r => r.Text == itemData.DisplayName))
                        {
                            var data = itemData;
                            string catName = !string.IsNullOrEmpty(data.ObjectType) ? data.ObjectType : "Item";
                            results.Add(new LookupLink(
                                text: data.DisplayName,
                                subtitle: catName,
                                textColor: Game1.textColor,
                                icon: data.GetTexture(),
                                iconSourceRect: data.GetSourceRect(),
                                onClick: () =>
                                {
                                    var item = ItemRegistry.Create(data.QualifiedItemId);
                                    return item != null ? BuildItemSubject(item) : null;
                                }
                            ));

                            if (results.Count >= 40)
                                break;
                        }
                    }
                }

                if (results.Count >= 40)
                    break;
            }

            return results;
        }

        #endregion
    }
}
