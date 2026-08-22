using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
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
                    $"{hearts}/{maxHearts} Hearts ({points} pts, {ptsInHeart}/250 to next)",
                    new Color(220, 20, 60)
                ));

                if (npc.currentLocation != null)
                {
                    string locName = npc.currentLocation.DisplayName ?? npc.currentLocation.Name;
                    relSection.Fields.Add(new LookupField("Current Location", $"{locName} (Tile: {(int)npc.Tile.X}, {(int)npc.Tile.Y})", new Color(20, 110, 220)));
                }

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

            // Section 4: Wild Forage Details (Spawn Locations & Seasons)
            AddForageDataSection(subject, item);

            // Section 5: Mineral & Artifact Finding Sources
            AddMineralAndArtifactLocationSection(subject, item);

            // Section 6: Weapons & Equipment (Damage, Crit, Defense, Forges, Enchantments)
            AddWeaponAndCombatSection(subject, item);

            // Section 7: 1.6 Trinkets & BetterForge Prismatic Ascension
            if (item is Trinket || item.QualifiedItemId.StartsWith("(TR)") || IsTrinketItem(item))
            {
                AddTrinketSection(subject, item);
            }

            // Section 8: 1.6 Skill & Power Books
            if (item.Category == -98 || item.ItemId.StartsWith("Book_") || item.QualifiedItemId.Contains("Book_") || item.Name.Contains("Book"))
            {
                AddSkillBookSection(subject, item);
            }

            // Section 9: Tool Details (Upgrade level, Enchantments, Attached Bait/Tackles)
            AddToolSection(subject, item);

            // Section 10: Animal Processing & Incubation (Eggs, Milk, Wool, Truffles)
            AddAnimalProductProcessingSection(subject, item);

            // Section 11: Geodes, Artifact Troves & Mystery Boxes
            AddGeodeAndMysteryBoxSection(subject, item);

            // Section 12: Fertilizer Details (Soil & Tree Growth Effects)
            AddFertilizerDetailsSection(subject, item);

            // Section 13: Recycling Machine Yields
            AddRecyclingSection(subject, item);

            // Section 14: Artisan Processing (Keg, Preserves Jar, Dehydrator, Cask, Fish Smoker, Oil, Mill)
            AddArtisanProductsSection(subject, item, baseSellPrice);

            // Section 15: Tailoring & Dyeing (Sewing Machine Product & Dye Pot Color)
            AddTailoringAndDyeSection(subject, item);

            // Section 16: Collections & Perfection Tracker (Shipped, Caught, Cooked, Crafted)
            AddCollectionAndPerfectionSection(subject, item);

            // Section 17: Museum & Bundles
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

            // Section 18: Gift Preferences (Interactive Tappable NPC Links)
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

            // Section 19: Recipes Using This Item (Cooking & Crafting)
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
                else if (item is Slingshot slingshot)
                {
                    var section = new LookupSection("Slingshot & Ammo");
                    bool isMaster = slingshot.ItemId == "33" || slingshot.Name.Contains("Master");
                    section.Fields.Add(new LookupField("Type", isMaster ? "Master Slingshot (2x Damage Multiplier)" : "Standard Slingshot (1x Multiplier)", new Color(180, 100, 0)));
                    section.Fields.Add(new LookupField("Compatible Ammo", "Stone (1x), Copper Ore (1.5x), Iron Ore (2x), Gold Ore (2.5x), Iridium Ore (3.5x), Explosive Ammo (AoE)", new Color(0, 140, 0)));
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

        private static bool IsTrinketItem(Item item)
        {
            if (item is Trinket || item.QualifiedItemId.StartsWith("(TR)"))
                return true;
            string id = item.ItemId.ToLowerInvariant();
            return id.Contains("fairybox") || id.Contains("frogegg") || id.Contains("magicquiver")
                || id.Contains("goldenspur") || id.Contains("iridiumspur") || id.Contains("icerod")
                || id.Contains("parrotegg") || id.Contains("basiliskpaw");
        }

        private static void AddTrinketSection(LookupSubject subject, Item item)
        {
            try
            {
                var section = new LookupSection("1.6 Trinket Stats & Mechanics");
                string cleanId = item.ItemId.Replace("(TR)", "").Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(cleanId))
                    cleanId = item.QualifiedItemId.Replace("(TR)", "").Trim().ToLowerInvariant();

                Trinket? trinket = item as Trinket;

                // 1. Current Stats / Active Roll
                string currentRollSummary = "Standard Trinket";
                string possibleRangeSummary = "";

                int seed = trinket?.generationSeed.Value ?? 0;
                Random r = Utility.CreateRandom(seed);

                if (cleanId.Contains("fairy"))
                {
                    possibleRangeSummary = "Levels 1–5 | Heal Interval: 3.5s–4.7s | Power: 0.8x–1.2x";
                    if (trinket != null)
                    {
                        int num = 1;
                        if (r.NextBool(0.45)) num = 2;
                        else if (r.NextBool(0.25)) num = 3;
                        else if (r.NextBool(0.125)) num = 4;
                        else if (r.NextBool(0.0675)) num = 5;
                        float interval = (5000 - num * 300) / 1000f;
                        float power = 0.7f + num * 0.1f;
                        currentRollSummary = $"Level {num}/5 (Heal Pulse: {interval:0.0}s, Power: {power:0.0}x)";
                    }
                    else
                    {
                        currentRollSummary = "Level 1–5 (Spawns a healing companion)";
                    }
                }
                else if (cleanId.Contains("quiver"))
                {
                    possibleRangeSummary = "Cooldown: 0.90s–2.00s | Damage: 10–40 (Normal, Rapid, Heavy, Perfect)";
                    if (trinket != null)
                    {
                        int minDmg, maxDmg;
                        float delay;
                        string style = "Normal";

                        if (r.NextBool(0.04))
                        {
                            style = "Perfect";
                            minDmg = 30;
                            maxDmg = 35;
                            delay = 900f;
                        }
                        else if (r.NextBool(0.1))
                        {
                            if (r.NextBool(0.5))
                            {
                                style = "Rapid";
                                minDmg = r.Next(10, 15) - 2;
                                maxDmg = minDmg + 5;
                                delay = 600 + r.Next(11) * 10;
                            }
                            else
                            {
                                style = "Heavy";
                                minDmg = r.Next(25, 41) - 2;
                                maxDmg = minDmg + 5;
                                delay = 1500 + r.Next(6) * 100;
                            }
                        }
                        else
                        {
                            minDmg = r.Next(15, 31) - 2;
                            maxDmg = minDmg + 5;
                            delay = 1100 + r.Next(11) * 100;
                        }

                        currentRollSummary = $"{style} Variant (Cooldown: {delay / 1000f:0.00}s, Damage: {minDmg}–{maxDmg})";
                    }
                    else
                    {
                        currentRollSummary = "Fires spectral arrows at nearby enemies";
                    }
                }
                else if (cleanId.Contains("ice") || cleanId.Contains("rod"))
                {
                    possibleRangeSummary = "Delay: 3.0s–5.0s | Freeze Duration: 2.0s–4.0s";
                    if (trinket != null)
                    {
                        float delay = r.Next(3000, 5001);
                        int freeze = r.Next(2000, 4001);
                        bool isPerfect = false;
                        if (r.NextDouble() < 0.05)
                        {
                            isPerfect = true;
                            delay = 3000f;
                            freeze = 4000;
                        }
                        currentRollSummary = $"Delay: {delay / 1000f:0.0}s | Freeze: {freeze / 1000f:0.0}s{(isPerfect ? " (Perfect Roll ★)" : "")}";
                    }
                    else
                    {
                        currentRollSummary = "Shoots ice orbs that freeze enemies";
                    }
                }
                else if (cleanId.Contains("spur") || cleanId.Contains("golden") || cleanId.Contains("iridium"))
                {
                    possibleRangeSummary = "Speed Boost Duration on Crit: 5s–10s";
                    if (trinket != null)
                    {
                        int duration = r.Next(5, 11);
                        currentRollSummary = $"Critical Strike Speed Boost: {duration} seconds{(duration == 10 ? " (Max Roll ★)" : "")}";
                    }
                    else
                    {
                        currentRollSummary = "Speed boost on Critical Strike (5s–10s)";
                    }
                }
                else if (cleanId.Contains("parrot"))
                {
                    possibleRangeSummary = "Levels 1–4 (10%–40% Gold Coin Drop Chance on monster kills)";
                    if (trinket != null)
                    {
                        int num = 1;
                        if (r.NextBool(0.4)) num = 2;
                        else if (r.NextBool(0.2)) num = 3;
                        else if (r.NextBool(0.1)) num = 4;
                        currentRollSummary = $"Level {num} / 4 ({num * 10}% Gold Coin Drop Chance)";
                    }
                    else
                    {
                        currentRollSummary = "Level 1–4 (Finds gold coins from defeated monsters)";
                    }
                }
                else if (cleanId.Contains("frog"))
                {
                    possibleRangeSummary = "Variants: Green, Yellow, Red, Blue, Void, Poison, Prismatic";
                    string variant = "Hungry Frog Companion";
                    if (trinket != null)
                    {
                        int frogType = r.Next(0, 8);
                        string vName = frogType switch
                        {
                            0 => "Green Frog",
                            1 => "Yellow Frog",
                            2 => "Red Frog",
                            3 => "Blue Frog",
                            4 => "Void Frog",
                            5 => "Poison Frog",
                            6 or 7 => "Prismatic Frog ★",
                            _ => "Frog"
                        };
                        variant = $"{vName} (Swallows nearby monsters)";
                    }
                    currentRollSummary = variant;
                }
                else if (cleanId.Contains("basilisk") || cleanId.Contains("paw"))
                {
                    possibleRangeSummary = "Fixed (Complete immunity to all combat debuffs)";
                    currentRollSummary = "Complete immunity to Slimed, Jinxed, Darkness, etc.";
                }

                section.Fields.Add(new LookupField("Active Trinket Stats", currentRollSummary, new Color(0, 140, 0)));
                if (!string.IsNullOrEmpty(possibleRangeSummary))
                {
                    section.Fields.Add(new LookupField("Possible Roll Ranges", possibleRangeSummary, Color.DarkSlateGray));
                }

                section.Fields.Add(new LookupField("Anvil Reforging", "Reroll stats at the Anvil using 3 Iridium Bars", new Color(180, 100, 0)));

                // 2. BetterForge Mod Integration & Prismatic Ascension
                bool isAscended = trinket != null && (trinket.modData.ContainsKey("feiluvnana.BetterForge/IsAscended") || trinket.modData.ContainsKey("feiluvnana.BetterTrinket/IsAscended"));
                bool hasBetterForge = ModEntry.ModHelper.ModRegistry.IsLoaded("feiluvnana.BetterForge");

                string ascensionPowerDesc = cleanId switch
                {
                    var s when s.Contains("frog") => "Swallowing monsters drops their full loot table with a 45% chance to immediately reset swallow cooldown.",
                    var s when s.Contains("fairy") => "Provides continuous passive pulse healing (even out of combat), heals nearby allies, and grants +1 Defense for 15s (Fairy Blessing).",
                    var s when s.Contains("parrot") => "Doubles gold coin value and grants a +35% chance for defeated monsters to drop bonus monster loot.",
                    var s when s.Contains("spur") || s.Contains("golden") || s.Contains("iridium") => "Increases Critical Strike Chance by +10%, and the critical speed boost provides +3 Attack (Spur Fury).",
                    var s when s.Contains("quiver") => "Spectral arrows pierce through all enemies and grant +15% Critical Strike Chance.",
                    var s when s.Contains("ice") || s.Contains("rod") => "Striking frozen enemies shatters the ice into a frost blast dealing 30% Attack damage and slowing nearby foes.",
                    var s when s.Contains("basilisk") || s.Contains("paw") => "Reflects 50% incoming damage back to attackers, and melee attacks have a 20% chance to lifesteal (heals 3–8 HP).",
                    _ => "Grants permanent +0.5 Luck and special enhanced combat abilities."
                };

                if (isAscended)
                {
                    section.Fields.Add(new LookupField("✦ Prismatic Ascension", "ACTIVE (Ascended at Anvil with 1 Prismatic Shard)", new Color(180, 50, 180)));
                    section.Fields.Add(new LookupField("Passive Luck Bonus", "+0.5 Permanent Luck (Endless Buff)", new Color(0, 140, 0)));
                    section.Fields.Add(new LookupField("Ascended Enhanced Power", ascensionPowerDesc, new Color(180, 50, 180)));
                    section.Fields.Add(new LookupField("Source Mod", "BetterForge Mod (Permanent Ascension)", Color.DarkSlateGray));
                }
                else
                {
                    string notice = hasBetterForge
                        ? "Not Ascended — (Forge with 1 Prismatic Shard at the Anvil to permanently unlock this power!)"
                        : "Not Ascended — (If using BetterForge mod: Forge with 1 Prismatic Shard at the Anvil to unlock this enhanced power + 0.5 Luck!)";

                    section.Fields.Add(new LookupField("✦ Prismatic Ascension (BetterForge)", notice, new Color(180, 100, 0)));
                    section.Fields.Add(new LookupField("Enhanced Ascension Power", $"{ascensionPowerDesc} (Includes +0.5 Permanent Luck)", Color.DarkSlateGray));
                }

                subject.Sections.Add(section);
            }
            catch { }
        }

        private static void AddSkillBookSection(LookupSubject subject, Item item)
        {
            try
            {
                string id = item.ItemId.ToLowerInvariant();
                string qId = item.QualifiedItemId.ToLowerInvariant();
                string name = item.Name.ToLowerInvariant();

                string bookStatKey = "";
                string bookMailKey = "";
                string powerDesc = "";
                string secondaryXpDesc = "";

                if (id.Contains("book_stars") || name.Contains("book of stars"))
                {
                    bookStatKey = "Book_Stars";
                    powerDesc = "Grants +250 Experience Points to all 5 skills (Farming, Mining, Foraging, Fishing, Combat).";
                    secondaryXpDesc = "+250 XP to all 5 skills on every reading.";
                }
                else if (id.Contains("book_defense") || name.Contains("safety manual"))
                {
                    bookStatKey = "Book_Defense";
                    bookMailKey = "DwarvishSafetyManual";
                    powerDesc = "Dwarvish Safety Manual: Take 25% less damage from bomb blasts.";
                    secondaryXpDesc = "+100 Combat XP on repeat readings.";
                }
                else if (id.Contains("book_woodcutting") || name.Contains("woodcutter"))
                {
                    bookStatKey = "Book_Woodcutting";
                    powerDesc = "Woodcutter's Weekly: Grants a 5% chance to gain extra wood from chopping trees.";
                    secondaryXpDesc = "+100 Foraging XP on repeat readings.";
                }
                else if (id.Contains("book_mining") || name.Contains("mining monthly"))
                {
                    bookStatKey = "Book_Mining";
                    powerDesc = "Mining Monthly: Permanently increases Mining experience gains.";
                    secondaryXpDesc = "+100 Mining XP on repeat readings.";
                }
                else if (id.Contains("book_friendship") || name.Contains("friendship 101"))
                {
                    bookStatKey = "Book_Friendship";
                    powerDesc = "Friendship 101: Friendship points with villagers decay significantly slower.";
                    secondaryXpDesc = "+100 Friendship XP on repeat readings.";
                }
                else if (id.Contains("book_speed2") || name.Contains("way of the wind pt 2"))
                {
                    bookStatKey = "Book_Speed2";
                    powerDesc = "Way of the Wind (Part 2): Permanently increases base walking speed by +0.25.";
                    secondaryXpDesc = "+100 Foraging XP on repeat readings.";
                }
                else if (id.Contains("book_speed") || name.Contains("way of the wind"))
                {
                    bookStatKey = "Book_Speed";
                    powerDesc = "Way of the Wind (Part 1): Permanently increases base walking speed by +0.25.";
                    secondaryXpDesc = "+100 Foraging XP on repeat readings.";
                }
                else if (id.Contains("book_wildseeds") || name.Contains("jack be nimble"))
                {
                    bookStatKey = "Book_WildSeeds";
                    powerDesc = "Jack Be Nimble, Jack Be Thick: Permanently increases Defense by +1.";
                    secondaryXpDesc = "+100 Foraging XP on repeat readings.";
                }
                else if (id.Contains("book_pricecatalogue") || name.Contains("price catalogue"))
                {
                    bookStatKey = "Book_PriceCatalogue";
                    powerDesc = "Price Catalogue: Permanently displays item sell prices in item tooltips.";
                    secondaryXpDesc = "+100 Experience on repeat readings.";
                }
                else if (id.Contains("book_mapping") || name.Contains("monster compendium"))
                {
                    bookStatKey = "Book_Mapping";
                    powerDesc = "Monster Compendium: Monsters have a 3% chance to drop double monster loot.";
                    secondaryXpDesc = "+100 Combat XP on repeat readings.";
                }
                else if (id.Contains("book_horse") || name.Contains("horse the book"))
                {
                    bookStatKey = "Book_Horse";
                    powerDesc = "Horse The Book: Permanently increases riding speed by +0.5.";
                    secondaryXpDesc = "+100 Foraging XP on repeat readings.";
                }
                else if (id.Contains("book_artifact") || name.Contains("treasure appraisal"))
                {
                    bookStatKey = "Book_Artifact";
                    powerDesc = "Treasure Appraisal Guide: Artifacts and dinosaur bones sell for 3x their normal price.";
                    secondaryXpDesc = "+100 Mining XP on repeat readings.";
                }
                else if (id.Contains("book_trash") || name.Contains("alleyway buffoon"))
                {
                    bookStatKey = "Book_Trash";
                    powerDesc = "Alleyway Buffoon: 20% greater chance to successfully find loot when searching garbage cans.";
                    secondaryXpDesc = "+100 Foraging XP on repeat readings.";
                }
                else if (id.Contains("book_grass") || name.Contains("slitherlegs"))
                {
                    bookStatKey = "Book_Grass";
                    powerDesc = "Ol' Slitherlegs: Move at full speed through tall grass without being slowed down.";
                    secondaryXpDesc = "+100 Foraging XP on repeat readings.";
                }
                else if (id.Contains("book_bait") || name.Contains("bait and bobber"))
                {
                    bookStatKey = "Book_Bait";
                    powerDesc = "Bait And Bobber: Grants +1 Fishing XP every time a fish is successfully caught.";
                    secondaryXpDesc = "+100 Fishing XP on repeat readings.";
                }
                else if (id.Contains("book_crab") || name.Contains("art of crabbing"))
                {
                    bookStatKey = "Book_Crab";
                    powerDesc = "Art of Crabbing: 25% chance for Crab Pots to produce double items.";
                    secondaryXpDesc = "+100 Fishing XP on repeat readings.";
                }
                else if (id.Contains("book_roe") || name.Contains("jewels of the sea"))
                {
                    bookStatKey = "Book_Roe";
                    powerDesc = "Jewels of the Sea: Fishing treasure chests have a 25% chance to contain wild Roe.";
                    secondaryXpDesc = "+100 Fishing XP on repeat readings.";
                }
                else if (id.Contains("book_diamonds") || name.Contains("diamond hunter"))
                {
                    bookStatKey = "Book_Diamonds";
                    powerDesc = "Diamond Hunter: Manual quarry rocks and stones have a chance to drop Diamonds.";
                    secondaryXpDesc = "+100 Mining XP on repeat readings.";
                }
                else if (id.Contains("book_mystery") || name.Contains("book of mysteries"))
                {
                    bookStatKey = "Book_Mystery";
                    powerDesc = "Book of Mysteries: Significantly increases the chance to find Mystery Boxes.";
                    secondaryXpDesc = "+100 Experience on repeat readings.";
                }
                else if (id.Contains("book_queenofsauce") || name.Contains("queen of sauce cookbook"))
                {
                    bookStatKey = "Book_QueenOfSauce";
                    powerDesc = "Queen of Sauce Cookbook: Instantly learns all cooking recipes from Queen of Sauce television broadcasts.";
                    secondaryXpDesc = "Instantly unlocks all missed cooking recipes.";
                }
                else if (id.Contains("book_animal") || name.Contains("animal catalogue"))
                {
                    bookStatKey = "Book_Animal";
                    powerDesc = "Animal Catalogue: Allows shopping at Marnie's Ranch even when Marnie is away from the counter.";
                    secondaryXpDesc = "+100 Farming XP on repeat readings.";
                }
                else
                {
                    return;
                }

                var section = new LookupSection("1.6 Skill & Power Book Details");

                bool isRead = false;
                if (!string.IsNullOrEmpty(bookStatKey) && Game1.player.stats.Get(bookStatKey) > 0)
                    isRead = true;
                if (!string.IsNullOrEmpty(bookMailKey) && (Game1.player.mailReceived.Contains(bookMailKey) || Game1.player.hasOrWillReceiveMail(bookMailKey)))
                    isRead = true;

                section.Fields.Add(new LookupField(
                    "Reading Status",
                    isRead ? "Read ✓ (Permanent Power Active)" : "Unread ✗ (Read book to unlock power)",
                    isRead ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                section.Fields.Add(new LookupField("Power Granted", powerDesc, new Color(180, 50, 180)));
                if (!string.IsNullOrEmpty(secondaryXpDesc))
                {
                    section.Fields.Add(new LookupField("Secondary Readings", secondaryXpDesc, Color.DarkSlateGray));
                }

                subject.Sections.Add(section);
            }
            catch { }
        }

        private static void AddCollectionAndPerfectionSection(LookupSubject subject, Item item)
        {
            try
            {
                var fields = new List<LookupField>();

                // 1. Shipped (Items Shipped collection)
                if (item.Category != StardewValley.Object.FishCategory && item.Category != StardewValley.Object.CookingCategory && item is StardewValley.Object obj && !obj.bigCraftable.Value)
                {
                    bool hasShipped = Game1.player.basicShipped.TryGetValue(item.ItemId, out int shipCount) && shipCount > 0;
                    fields.Add(new LookupField(
                        "Items Shipped",
                        hasShipped ? $"Shipped ✓ ({shipCount} shipped total)" : "Not Yet Shipped ✗ (Needed for Perfection)",
                        hasShipped ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                }

                // 2. Fish Caught
                if (item.Category == StardewValley.Object.FishCategory || IsFishItem(item))
                {
                    bool caught = Game1.player.fishCaught.TryGetValue(item.ItemId, out int[]? fishData) || Game1.player.fishCaught.TryGetValue($"(O){item.ItemId}", out fishData);
                    int count = caught && fishData != null && fishData.Length > 0 ? fishData[0] : 0;
                    int maxSize = caught && fishData != null && fishData.Length > 1 ? fishData[1] : 0;

                    fields.Add(new LookupField(
                        "Fish Caught",
                        caught && count > 0 ? $"Caught ✓ ({count} caught, Record: {maxSize} in.)" : "Not Yet Caught ✗ (Needed for Master Angler)",
                        caught && count > 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                }

                // 3. Recipes Cooked
                if (item.Category == StardewValley.Object.CookingCategory)
                {
                    bool cooked = Game1.player.recipesCooked.TryGetValue(item.ItemId, out int cookCount) && cookCount > 0;
                    fields.Add(new LookupField(
                        "Recipes Cooked",
                        cooked ? $"Cooked ✓ ({cookCount} cooked)" : "Not Yet Cooked ✗ (Needed for Gourmet Chef)",
                        cooked ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                }

                // 4. Crafting Recipes Crafted
                if (CraftingRecipe.craftingRecipes.ContainsKey(item.DisplayName) || CraftingRecipe.craftingRecipes.ContainsKey(item.Name))
                {
                    string recipeName = CraftingRecipe.craftingRecipes.ContainsKey(item.DisplayName) ? item.DisplayName : item.Name;
                    bool crafted = Game1.player.craftingRecipes.TryGetValue(recipeName, out int craftCount) && craftCount > 0;
                    fields.Add(new LookupField(
                        "Items Crafted",
                        crafted ? $"Crafted ✓ ({craftCount} crafted)" : "Not Yet Crafted ✗ (Needed for Craft Master)",
                        crafted ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                }

                if (fields.Count > 0)
                {
                    var section = new LookupSection("Collections & Perfection Tracker");
                    section.Fields.AddRange(fields);
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

                // 7. 1.6 Targeted Bait Maker
                section.Fields.Add(new LookupField("1.6 Bait Maker", $"Yields 5–10 {item.DisplayName} Bait (targets this fish!)", new Color(0, 140, 0)));

                // 8. Fish Pond Produce Highlights
                string fishName = item.Name.ToLowerInvariant();
                string pondHighlights = fishName switch
                {
                    var s when s.Contains("sturgeon") => "Sturgeon Roe (Aged in Preserves Jar into Caviar: 500g / 700g Artisan)",
                    var s when s.Contains("lava eel") => "Magma Geodes (x5), Gold Ore (x5), Spicy Eel (Population 9+)",
                    var s when s.Contains("blobfish") => "Pearls, Farm Warp Totems (Population 9+)",
                    var s when s.Contains("rainbow trout") => "Prismatic Shard (0.09% daily chance at Population 9+)",
                    var s when s.Contains("super cucumber") => "Iridium Ore (1–3), Amethyst (Population 9+)",
                    var s when s.Contains("midnight squid") || s.Contains("squid") => "Squid Ink (Common)",
                    var s when s.Contains("woodskip") => "Wood, Hardwood, Tree Seeds",
                    var s when s.Contains("slimejack") => "Slime, Petrified Slime",
                    var s when s.Contains("spook fish") => "Treasure Chest (Rare at Population 9+)",
                    var s when s.Contains("stingray") => "Dragon Tooth, Cinder Shards, Battery Pack (Population 9+)",
                    var s when s.Contains("lionfish") => "Taro Tuber (Population 9+)",
                    var s when s.Contains("eel") => "Gold Ore (Population 9+)",
                    _ => $"{item.DisplayName} Roe (Can be aged into Aged Roe in Preserves Jar)"
                };
                section.Fields.Add(new LookupField("Fish Pond Produce", pondHighlights, new Color(180, 50, 180)));

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

        private static string GetFriendlyForageLocationName(string locKey)
        {
            return locKey switch
            {
                "Town" => "Pelican Town",
                "Forest" => "Cindersap Forest",
                "Mountain" => "The Mountain",
                "BusStop" => "Bus Stop",
                "Railroad" => "Railroad",
                "Beach" => "The Beach",
                "Woods" => "Secret Woods",
                "Desert" => "Calico Desert",
                "IslandWest" => "Ginger Island (West Farm)",
                "IslandSouth" => "Ginger Island (South Beach)",
                "IslandNorth" => "Ginger Island (North / Dig Site)",
                "IslandSouthEast" => "Ginger Island (Pirate Cove)",
                "UndergroundMine" => "The Mines",
                "Backwoods" => "Backwoods",
                _ => locKey
            };
        }

        private static void AddForageDataSection(LookupSubject subject, Item item)
        {
            try
            {
                var locDict = DataLoader.Locations(Game1.content);
                if (locDict == null) return;

                string itemId = item.ItemId;
                string qId = item.QualifiedItemId;
                var foundLocations = new HashSet<string>();
                var foundSeasons = new HashSet<string>();

                foreach (var kvp in locDict)
                {
                    string locKey = kvp.Key;
                    var locData = kvp.Value;
                    if (locData.Forage == null) continue;

                    foreach (var forage in locData.Forage)
                    {
                        if (forage.ItemId == itemId || forage.ItemId == qId || forage.Id == itemId || forage.Id == qId)
                        {
                            string friendlyLoc = GetFriendlyForageLocationName(locKey);
                            if (!string.IsNullOrEmpty(friendlyLoc))
                            {
                                foundLocations.Add(friendlyLoc);
                            }

                            if (forage.Season.HasValue)
                            {
                                string sName = forage.Season.Value.ToString();
                                foundSeasons.Add(char.ToUpper(sName[0]) + sName.Substring(1));
                            }
                        }
                    }
                }

                // Special / Manual seasonal mapping for standard wild forage
                switch (item.ItemId)
                {
                    case "16": // Wild Horseradish
                    case "18": // Daffodil
                    case "20": // Leek
                    case "22": // Dandelion
                        foundSeasons.Add("Spring");
                        foundLocations.Add("Pelican Town");
                        foundLocations.Add("Cindersap Forest");
                        foundLocations.Add("The Mountain");
                        foundLocations.Add("Bus Stop");
                        break;
                    case "399": // Spring Onion
                        foundSeasons.Add("Spring");
                        foundLocations.Add("Cindersap Forest (Southeast Island)");
                        break;
                    case "257": // Morel
                        foundSeasons.Add("Spring");
                        foundLocations.Add("Secret Woods");
                        foundLocations.Add("Farm Cave (Mushroom)");
                        break;
                    case "396": // Spice Berry
                    case "398": // Grape
                    case "394": // Sweet Pea
                        foundSeasons.Add("Summer");
                        foundLocations.Add("Pelican Town");
                        foundLocations.Add("Cindersap Forest");
                        foundLocations.Add("The Mountain");
                        foundLocations.Add("Bus Stop");
                        break;
                    case "259": // Fiddlehead Fern
                        foundSeasons.Add("Summer");
                        foundLocations.Add("Secret Woods");
                        foundLocations.Add("Prehistoric Skull Cavern Floors");
                        break;
                    case "404": // Common Mushroom
                    case "406": // Wild Plum
                    case "408": // Hazelnut
                    case "410": // Blackberry
                        foundSeasons.Add("Fall");
                        foundLocations.Add("Pelican Town");
                        foundLocations.Add("Cindersap Forest");
                        foundLocations.Add("The Mountain");
                        foundLocations.Add("Bus Stop");
                        break;
                    case "281": // Chanterelle
                        foundSeasons.Add("Fall");
                        foundLocations.Add("Secret Woods");
                        foundLocations.Add("Farm Cave (Mushroom)");
                        break;
                    case "420": // Red Mushroom
                        foundSeasons.Add("Summer");
                        foundSeasons.Add("Fall");
                        foundLocations.Add("Secret Woods");
                        foundLocations.Add("The Mines");
                        break;
                    case "422": // Purple Mushroom
                        foundLocations.Add("The Mines (Floor 81+)");
                        foundLocations.Add("Skull Cavern");
                        break;
                    case "78": // Cave Carrot
                        foundLocations.Add("The Mines (Boxes, Barrels & Tilling Dirt)");
                        foundLocations.Add("Skull Cavern");
                        break;
                    case "372": // Clam
                    case "393": // Coral
                    case "397": // Sea Urchin
                    case "152": // Seaweed
                        foundLocations.Add("The Beach");
                        break;
                    case "88": // Coconut
                    case "90": // Cactus Fruit
                        foundLocations.Add("Calico Desert");
                        break;
                    case "829": // Ginger
                    case "830": // Taro Root
                    case "832": // Pineapple
                    case "834": // Mango
                        foundLocations.Add("Ginger Island");
                        break;
                    case "412": // Winter Root
                    case "414": // Crystal Fruit
                    case "416": // Snow Yam
                    case "418": // Crocus
                    case "283": // Holly
                        foundSeasons.Add("Winter");
                        foundLocations.Add("Pelican Town");
                        foundLocations.Add("Cindersap Forest");
                        foundLocations.Add("The Mountain");
                        foundLocations.Add("Bus Stop");
                        break;
                }

                if (foundLocations.Count > 0 || foundSeasons.Count > 0)
                {
                    var section = new LookupSection("Wild Forage Details");
                    if (foundSeasons.Count > 0)
                    {
                        section.Fields.Add(new LookupField("Seasons", string.Join(", ", foundSeasons), new Color(0, 140, 0)));
                    }
                    if (foundLocations.Count > 0)
                    {
                        section.Fields.Add(new LookupField("Spawn Locations", string.Join(", ", foundLocations), new Color(20, 110, 220)));
                    }
                    subject.Sections.Add(section);
                }
            }
            catch { }
        }

        private static void AddMineralAndArtifactLocationSection(LookupSubject subject, Item item)
        {
            try
            {
                if (item is StardewValley.Object obj && (obj.Type == "Arch" || obj.Type == "Minerals" || item.Category == StardewValley.Object.mineralsCategory))
                {
                    var sources = new List<string>();

                    if (obj.Type == "Arch")
                    {
                        sources.Add("Artifact Spots (Hoeing soil)");
                        sources.Add("Fishing Treasure Chests");
                        sources.Add("Artifact Troves (Opened at Clint's)");
                        if (item.ItemId == "107")
                        {
                            sources.Add("Pepper Rex Monster Drops (Prehistoric Floors)");
                        }
                    }
                    else if (obj.Type == "Minerals" || item.Category == StardewValley.Object.mineralsCategory)
                    {
                        if (item.ItemId == "74")
                        {
                            sources.Add("Iridium Nodes & Mystic Stones (Skull Cavern / Quarry / Volcano)");
                            sources.Add("Omni Geodes (0.4% chance)");
                            sources.Add("Monster Drops (Serpents, Mummies, Shadow Brutes)");
                            sources.Add("Rainbow Trout Fish Pond (rare)");
                        }
                        else if (item.ItemId == "72")
                        {
                            sources.Add("Diamond Nodes & Gem Nodes (The Mines Floor 50+)");
                            sources.Add("Monster Drops & Fishing Treasure Chests");
                        }
                        else if (item.ItemId == "60" || item.ItemId == "62" || item.ItemId == "64" || item.ItemId == "66" || item.ItemId == "68" || item.ItemId == "70")
                        {
                            sources.Add("Gem Nodes & Mining (The Mines, Skull Cavern, Volcano Dungeon)");
                            sources.Add("Geodes & Fishing Treasure Chests");
                        }
                        else
                        {
                            sources.Add("Mining in The Mines & Skull Cavern");
                            sources.Add("Cracking Geodes at Clint's Blacksmith");
                        }
                    }

                    if (sources.Count > 0)
                    {
                        var section = new LookupSection("Finding & Gathering Sources");
                        section.Fields.Add(new LookupField("Sources", string.Join(" | ", sources), new Color(20, 110, 220)));
                        subject.Sections.Add(section);
                    }
                }
            }
            catch { }
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
            string id = item.ItemId.ToLowerInvariant();
            string name = item.Name.ToLowerInvariant();

            // Fruits -> Wine, Jelly, Dried Fruit
            if (item.Category == StardewValley.Object.FruitsCategory)
            {
                int winePrice = basePrice * 3;
                var wineData = ItemRegistry.GetData("(O)348");
                artisanLinks.Add(new LookupLink(
                    text: $"{item.DisplayName} Wine ({winePrice}g)",
                    subtitle: "Keg (6.25d)",
                    textColor: new Color(180, 50, 180),
                    icon: wineData?.GetTexture(),
                    iconSourceRect: wineData?.GetSourceRect()
                ));

                int jellyPrice = basePrice * 2 + 50;
                var jellyData = ItemRegistry.GetData("(O)444");
                artisanLinks.Add(new LookupLink(
                    text: $"{item.DisplayName} Jelly ({jellyPrice}g)",
                    subtitle: "Preserves Jar (2–3d)",
                    textColor: new Color(200, 60, 20),
                    icon: jellyData?.GetTexture(),
                    iconSourceRect: jellyData?.GetSourceRect()
                ));

                int driedPrice = (int)(basePrice * 7.5) + 25;
                var driedData = ItemRegistry.GetData("(O)DriedFruit");
                if (driedData != null)
                {
                    artisanLinks.Add(new LookupLink(
                        text: $"Dried {item.DisplayName} ({driedPrice}g)",
                        subtitle: "Dehydrator (x5, 1d)",
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
                    subtitle: "Keg (4d)",
                    textColor: new Color(0, 140, 0),
                    icon: juiceData?.GetTexture(),
                    iconSourceRect: juiceData?.GetSourceRect()
                ));

                int picklePrice = basePrice * 2 + 50;
                var pickleData = ItemRegistry.GetData("(O)342");
                artisanLinks.Add(new LookupLink(
                    text: $"Pickled {item.DisplayName} ({picklePrice}g)",
                    subtitle: "Preserves Jar (2–3d)",
                    textColor: new Color(180, 100, 0),
                    icon: pickleData?.GetTexture(),
                    iconSourceRect: pickleData?.GetSourceRect()
                ));
            }
            // Mushrooms -> Dried Mushrooms
            else if (name.Contains("mushroom") || id == "404" || id == "420" || id == "422" || id == "281" || id == "257")
            {
                int driedPrice = (int)(basePrice * 7.5) + 25;
                var driedMushroom = ItemRegistry.GetData("(O)DriedMushrooms") ?? ItemRegistry.GetData("(O)DriedFruit");
                artisanLinks.Add(new LookupLink(
                    text: $"Dried {item.DisplayName} ({driedPrice}g)",
                    subtitle: "Dehydrator (x5, 1d)",
                    textColor: new Color(180, 100, 0),
                    icon: driedMushroom?.GetTexture(),
                    iconSourceRect: driedMushroom?.GetSourceRect()
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
                        text: $"Smoked {item.DisplayName} ({smokedPrice}g / {((int)(smokedPrice * 1.4))}g Artisan)",
                        subtitle: "Fish Smoker (1 Fish + 1 Coal)",
                        textColor: new Color(200, 60, 20),
                        icon: smokedData.GetTexture(),
                        iconSourceRect: smokedData.GetSourceRect()
                    ));
                }
            }

            // Specialty Goods
            if (id == "433" || name == "coffee bean")
            {
                var coffee = ItemRegistry.GetData("(O)395");
                artisanLinks.Add(new LookupLink("Coffee (150g, +1 Speed Buff)", "Keg (x5, 2h)", new Color(110, 40, 10), coffee?.GetTexture(), coffee?.GetSourceRect()));
            }
            else if (id == "815" || name == "tea leaves")
            {
                var greenTea = ItemRegistry.GetData("(O)614");
                var pickle = ItemRegistry.GetData("(O)342");
                artisanLinks.Add(new LookupLink("Green Tea (100g, +30 Max Energy)", "Keg (3h)", new Color(46, 125, 50), greenTea?.GetTexture(), greenTea?.GetSourceRect()));
                artisanLinks.Add(new LookupLink("Pickled Tea Leaves (150g)", "Preserves Jar (2–3d)", new Color(180, 100, 0), pickle?.GetTexture(), pickle?.GetSourceRect()));
            }
            else if (id == "304" || name == "hops")
            {
                var paleAle = ItemRegistry.GetData("(O)303");
                artisanLinks.Add(new LookupLink("Pale Ale (300g / 420g Artisan)", "Keg (1.5d) — Ages in Cask", new Color(180, 100, 0), paleAle?.GetTexture(), paleAle?.GetSourceRect()));
            }
            else if (id == "262" || name == "wheat")
            {
                var beer = ItemRegistry.GetData("(O)346");
                var flour = ItemRegistry.GetData("(O)246");
                artisanLinks.Add(new LookupLink("Beer (200g / 280g Artisan)", "Keg (1.5d) — Ages in Cask", new Color(180, 100, 0), beer?.GetTexture(), beer?.GetSourceRect()));
                artisanLinks.Add(new LookupLink("Wheat Flour (100g)", "Mill (Overnight)", Game1.textColor, flour?.GetTexture(), flour?.GetSourceRect()));
            }
            else if (id == "340" || name == "honey")
            {
                var mead = ItemRegistry.GetData("(O)459");
                artisanLinks.Add(new LookupLink("Mead (200g–400g / 560g Artisan)", "Keg (10h) — Ages in Cask", new Color(180, 100, 0), mead?.GetTexture(), mead?.GetSourceRect()));
            }
            else if (id == "270" || id == "421" || id == "431" || name.Contains("sunflower") || name == "corn")
            {
                var oil = ItemRegistry.GetData("(O)247");
                artisanLinks.Add(new LookupLink("Cooking Oil (100g)", "Oil Maker", new Color(180, 100, 0), oil?.GetTexture(), oil?.GetSourceRect()));
            }
            else if (id == "271" || name == "unmilled rice")
            {
                var rice = ItemRegistry.GetData("(O)423");
                artisanLinks.Add(new LookupLink("Milled Rice (100g)", "Mill (Overnight)", Game1.textColor, rice?.GetTexture(), rice?.GetSourceRect()));
            }
            else if (id == "284" || name == "beet")
            {
                var sugar = ItemRegistry.GetData("(O)245");
                artisanLinks.Add(new LookupLink("3x Sugar (3x 50g = 150g)", "Mill (Overnight)", Game1.textColor, sugar?.GetTexture(), sugar?.GetSourceRect()));
            }

            // Cask Aging (Wine, Cheese, Pale Ale, Beer, Mead)
            if (name.Contains("wine") || name.Contains("cheese") || name.Contains("pale ale") || name.Contains("beer") || name.Contains("mead"))
            {
                int iridiumVal = basePrice * 2;
                int days = name.Contains("wine") ? 56 : (name.Contains("cheese") ? 14 : 34);
                artisanLinks.Add(new LookupLink(
                    text: $"Iridium Quality ({iridiumVal}g)",
                    subtitle: $"Cask Aging ({days} days)",
                    textColor: new Color(180, 50, 180)
                ));
            }

            if (artisanLinks.Count > 0)
            {
                var section = new LookupSection("Artisan Processing & Value");
                section.Fields.Add(new LookupField("Products", artisanLinks));
                subject.Sections.Add(section);
            }
        }

        private static void AddTailoringAndDyeSection(LookupSubject subject, Item item)
        {
            try
            {
                var fields = new List<LookupField>();

                // 1. Sewing Machine Product
                var tailoringData = DataLoader.TailoringRecipes(Game1.content);
                if (tailoringData != null)
                {
                    foreach (var recipe in tailoringData)
                    {
                        if (recipe.SecondItemTags != null && recipe.SecondItemTags.Any(tag => item.HasContextTag(tag) || tag == item.ItemId || tag == item.QualifiedItemId || tag == $"(O){item.ItemId}"))
                        {
                            if (recipe.CraftedItemIds != null && recipe.CraftedItemIds.Count > 0)
                            {
                                string craftedId = recipe.CraftedItemIds[0];
                                var craftedData = ItemRegistry.GetData(craftedId);
                                if (craftedData != null)
                                {
                                    var link = new LookupLink(
                                        text: craftedData.DisplayName,
                                        subtitle: "Sewing Machine (Cloth + This)",
                                        textColor: new Color(180, 50, 180),
                                        icon: craftedData.GetTexture(),
                                        iconSourceRect: craftedData.GetSourceRect()
                                    );
                                    fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tailoring.sewing-product"), new List<LookupLink> { link }));
                                    break;
                                }
                            }
                        }
                    }
                }

                // 2. Dye Pot Color
                var tags = item.GetContextTags();
                if (tags != null)
                {
                    string? dyeColor = null;
                    if (tags.Contains("color_red")) dyeColor = "Red (Đỏ)";
                    else if (tags.Contains("color_orange")) dyeColor = "Orange (Cam)";
                    else if (tags.Contains("color_yellow")) dyeColor = "Yellow (Vàng)";
                    else if (tags.Contains("color_green")) dyeColor = "Green (Xanh Lá)";
                    else if (tags.Contains("color_blue") || tags.Contains("color_cyan") || tags.Contains("color_ocean_blue")) dyeColor = "Blue (Xanh Dương)";
                    else if (tags.Contains("color_purple")) dyeColor = "Purple (Tím)";
                    else if (tags.Contains("color_pink")) dyeColor = "Pink (Hồng)";
                    else if (tags.Contains("color_gray")) dyeColor = "Gray (Xám)";
                    else if (tags.Contains("color_brown")) dyeColor = "Brown (Nâu)";
                    else if (tags.Contains("color_black")) dyeColor = "Black (Đen)";

                    if (dyeColor != null)
                    {
                        Color c = dyeColor.StartsWith("Red") ? new Color(220, 20, 60)
                            : dyeColor.StartsWith("Orange") ? new Color(220, 100, 20)
                            : dyeColor.StartsWith("Yellow") ? new Color(200, 160, 0)
                            : dyeColor.StartsWith("Green") ? new Color(46, 125, 50)
                            : dyeColor.StartsWith("Blue") ? new Color(20, 110, 220)
                            : dyeColor.StartsWith("Purple") ? new Color(180, 50, 180)
                            : Color.DarkSlateGray;

                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tailoring.dye-color"), dyeColor, c));
                    }
                }

                if (fields.Count > 0)
                {
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.tailoring"));
                    section.Fields.AddRange(fields);
                    subject.Sections.Add(section);
                }
            }
            catch { }
        }

        private static void AddAnimalProductProcessingSection(LookupSubject subject, Item item)
        {
            try
            {
                string id = item.ItemId.ToLowerInvariant();
                string name = item.Name.ToLowerInvariant();
                var fields = new List<LookupField>();

                // Eggs & Incubation
                if (item.Category == StardewValley.Object.EggCategory || name.Contains("egg"))
                {
                    if (name.Contains("dinosaur"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), "Coop Incubator: Hatches a Dinosaur in 12.5 days (6.25 days with Coopmaster)", new Color(46, 125, 50)));
                        var mayo = ItemRegistry.GetData("(O)807");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink("Dinosaur Mayonnaise (800g)", "Mayonnaise Machine (3h)", new Color(46, 125, 50), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("ostrich"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), "Ostrich Incubator (Barn): Hatches an Ostrich in 9.5 days (4.75 days with Coopmaster)", new Color(46, 125, 50)));
                        var mayo = ItemRegistry.GetData("(O)306");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink("10x Mayonnaise (Matching quality, up to 10x 380g)", "Mayonnaise Machine (3h)", new Color(180, 100, 0), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("void"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), "Coop Incubator: Hatches a Void Chicken in ~5.5 days (2.75 days with Coopmaster)", new Color(180, 50, 180)));
                        var mayo = ItemRegistry.GetData("(O)308");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink("Void Mayonnaise (275g)", "Mayonnaise Machine (3h)", new Color(180, 50, 180), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("duck"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), "Coop Incubator: Hatches a Duck in ~5.5 days (2.75 days with Coopmaster)", new Color(20, 110, 220)));
                        var mayo = ItemRegistry.GetData("(O)307");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink("Duck Mayonnaise (375g)", "Mayonnaise Machine (3h)", new Color(20, 110, 220), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("golden"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), "Coop Incubator: Hatches a Golden Chicken in ~5.5 days (2.75 days with Coopmaster)", new Color(180, 100, 0)));
                        var mayo = ItemRegistry.GetData("(O)306");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink("3x Gold Quality Mayonnaise (3x 285g = 855g)", "Mayonnaise Machine (3h)", new Color(180, 100, 0), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("large"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), "Coop Incubator: Hatches a Chicken in ~5.5 days (2.75 days with Coopmaster)", new Color(0, 140, 0)));
                        var mayo = ItemRegistry.GetData("(O)306");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink("Gold Quality Mayonnaise (285g)", "Mayonnaise Machine (3h)", new Color(180, 100, 0), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), "Coop Incubator: Hatches a Chicken in ~5.5 days (2.75 days with Coopmaster)", new Color(0, 140, 0)));
                        var mayo = ItemRegistry.GetData("(O)306");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink("Normal Mayonnaise (190g)", "Mayonnaise Machine (3h)", Game1.textColor, mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                }

                // Milk & Cheese
                if (item.Category == StardewValley.Object.MilkCategory || name.Contains("milk"))
                {
                    if (name.Contains("goat"))
                    {
                        var cheese = ItemRegistry.GetData("(O)426");
                        string quality = name.Contains("large") ? "Gold Quality Goat Cheese (600g)" : "Regular Goat Cheese (400g)";
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.cheese"), new List<LookupLink> {
                            new LookupLink(quality, "Cheese Press (3.3h) — Ages in Cask", new Color(180, 100, 0), cheese?.GetTexture(), cheese?.GetSourceRect())
                        }));
                    }
                    else
                    {
                        var cheese = ItemRegistry.GetData("(O)424");
                        string quality = name.Contains("large") ? "Gold Quality Cheese (345g)" : "Regular Cheese (230g)";
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.cheese"), new List<LookupLink> {
                            new LookupLink(quality, "Cheese Press (3.3h) — Ages in Cask", new Color(180, 100, 0), cheese?.GetTexture(), cheese?.GetSourceRect())
                        }));
                    }
                }

                // Wool -> Cloth
                if (name.Contains("wool") || id == "440" || id == "(o)440")
                {
                    var cloth = ItemRegistry.GetData("(O)428");
                    fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.loom"), new List<LookupLink> {
                        new LookupLink("Cloth (470g, chance for 2x with Silver+ Wool)", "Loom (4h)", new Color(180, 50, 180), cloth?.GetTexture(), cloth?.GetSourceRect())
                    }));
                }

                // Truffle -> Truffle Oil
                if (name.Contains("truffle") && !name.Contains("oil"))
                {
                    var oil = ItemRegistry.GetData("(O)432");
                    fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.oil"), new List<LookupLink> {
                        new LookupLink("Truffle Oil (1,065g / 1,491g Artisan)", "Oil Maker (6h)", new Color(180, 100, 0), oil?.GetTexture(), oil?.GetSourceRect())
                    }));
                }

                if (fields.Count > 0)
                {
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.animal-processing"));
                    section.Fields.AddRange(fields);
                    subject.Sections.Add(section);
                }
            }
            catch { }
        }

        private static void AddRecyclingSection(LookupSubject subject, Item item)
        {
            try
            {
                string id = item.ItemId.ToLowerInvariant();
                string name = item.Name.ToLowerInvariant();

                string yieldDesc = "";
                if (id == "168" || name == "trash")
                {
                    yieldDesc = "Stone (1–3), Coal (1–3), or Iron Ore (1–3) [Recycling Machine (1h)]";
                }
                else if (id == "169" || name == "driftwood")
                {
                    yieldDesc = "Wood (1–3) or Coal (1–3) [Recycling Machine (1h)]";
                }
                else if (id == "170" || id == "broken glasses" || name.Contains("broken glasses"))
                {
                    yieldDesc = "Refined Quartz (100% guarantee) [Recycling Machine (1h)]";
                }
                else if (id == "171" || id == "broken cd" || name.Contains("broken cd"))
                {
                    yieldDesc = "Refined Quartz (100% guarantee) [Recycling Machine (1h)]";
                }
                else if (id == "172" || name == "soggy newspaper")
                {
                    yieldDesc = "Torches (x3, 90% chance) or Cloth (10% chance) [Recycling Machine (1h)]";
                }
                else if (id == "rotten plant" || name.Contains("rotten plant"))
                {
                    yieldDesc = "Trash [Recycling Machine (1h)]";
                }
                else
                {
                    return;
                }

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.recycling"));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recycling.yields"), yieldDesc, new Color(0, 140, 0)));
                subject.Sections.Add(section);
            }
            catch { }
        }

        private static void AddGeodeAndMysteryBoxSection(LookupSubject subject, Item item)
        {
            try
            {
                string id = item.ItemId.ToLowerInvariant();
                string name = item.Name.ToLowerInvariant();

                string crackInfo = "";
                string contentsInfo = "";

                if (id == "535" || name == "geode")
                {
                    crackInfo = "Clint breaks for 25g (or process in Geode Crusher with 1 Coal)";
                    contentsInfo = "Ores, Coal, Basic Minerals (Quartz, Earth Crystal), Stone";
                }
                else if (id == "536" || name == "frozen geode")
                {
                    crackInfo = "Clint breaks for 25g (or process in Geode Crusher with 1 Coal)";
                    contentsInfo = "Frozen Minerals (Frozen Tear, Aquamarine, Opal), Ores, Coal";
                }
                else if (id == "537" || name == "magma geode")
                {
                    crackInfo = "Clint breaks for 25g (or process in Geode Crusher with 1 Coal)";
                    contentsInfo = "Magma Minerals (Fire Quartz, Ruby, Emerald, Helvite), Gold Ore, Iridium Ore";
                }
                else if (id == "749" || name == "omni geode")
                {
                    crackInfo = "Clint breaks for 25g (or process in Geode Crusher with 1 Coal). Can trade at Desert Trader.";
                    contentsInfo = "All minerals, Prismatic Shard (0.4%), Artifacts, Ores, Geode Minerals";
                }
                else if (id == "275" || name == "artifact trove")
                {
                    crackInfo = "Clint breaks for 25g. Purchase from Desert Trader for 5 Omni Geodes.";
                    contentsInfo = "Rare Museum Artifacts, Golden Pumpkin, Pearl, Treasure Chest";
                }
                else if (id.Contains("mysterybox") || name.Contains("mystery box"))
                {
                    crackInfo = "Clint breaks open at Blacksmith (25g). 1.6 Special Box.";
                    contentsInfo = "High-tier items, Skill Books, Auto-Petters, Mega Bombs, Quality Fertilizer, Prismatic Shards";
                }
                else
                {
                    return;
                }

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.geode-info"));
                section.Fields.Add(new LookupField("Cracking Method", crackInfo, new Color(180, 100, 0)));
                section.Fields.Add(new LookupField("Potential Drops", contentsInfo, new Color(0, 140, 0)));
                subject.Sections.Add(section);
            }
            catch { }
        }

        private static void AddFertilizerDetailsSection(LookupSubject subject, Item item)
        {
            try
            {
                string id = item.ItemId.ToLowerInvariant();
                string name = item.Name.ToLowerInvariant();

                string effect = "";
                if (id == "368" || name == "basic fertilizer")
                    effect = "Slightly increases chance for Silver & Gold quality crops.";
                else if (id == "369" || name == "quality fertilizer")
                    effect = "Increases chance for Gold & Silver quality crops.";
                else if (id == "919" || name == "deluxe fertilizer")
                    effect = "Guarantees Gold and high chance for Iridium quality crops!";
                else if (id == "465" || name == "speed-gro")
                    effect = "Accelerates crop growth speed by 10%.";
                else if (id == "466" || name == "deluxe speed-gro")
                    effect = "Accelerates crop growth speed by 25%.";
                else if (id == "918" || name == "hyper speed-gro")
                    effect = "Accelerates crop growth speed by 33% (1.5x faster growth).";
                else if (id == "370" || name == "basic retaining soil")
                    effect = "33% chance to stay watered overnight.";
                else if (id == "371" || name == "quality retaining soil")
                    effect = "66% chance to stay watered overnight.";
                else if (id == "920" || name == "deluxe retaining soil")
                    effect = "100% guarantee to stay watered overnight forever!";
                else if (id == "805" || name == "tree fertilizer")
                    effect = "Guarantees non-fruit trees grow one stage every night (even in Winter).";
                else
                    return;

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.fertilizer-info"));
                section.Fields.Add(new LookupField("Soil Effect", effect, new Color(46, 125, 50)));
                subject.Sections.Add(section);
            }
            catch { }
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
                    $"{info.Hearts:0.0} / 5.0 Hearts ({info.FriendshipPoints} pts)",
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
                    $"{info.Hearts:0.0} / 5.0 Hearts ({info.FriendshipPoints} pts)",
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

                string tapperStatus = info.IsTapped ? ModEntry.I18n.Get("hover.tree.tapped").ToString() : ModEntry.I18n.Get("lookup.common.no").ToString();
                if (tree.tapped.Value && tree.Location != null)
                {
                    Vector2 tile = tree.Tile;
                    if (tree.Location.Objects.TryGetValue(tile, out var tapperObj) && tapperObj.heldObject.Value != null)
                    {
                        var held = tapperObj.heldObject.Value;
                        if (tapperObj.readyForHarvest.Value || tapperObj.MinutesUntilReady <= 0)
                        {
                            tapperStatus = $"{held.DisplayName} (★ Ready to Collect!)";
                        }
                        else
                        {
                            int hours = tapperObj.MinutesUntilReady / 60;
                            int days = hours / 24;
                            int remHours = hours % 24;
                            string timeText = days > 0 ? $"{days}d {remHours}h" : $"{hours}h";
                            tapperStatus = $"{held.DisplayName} (Producing: {timeText} remaining)";
                        }
                    }
                }

                section.Fields.Add(new LookupField(
                    "Tapper",
                    tapperStatus,
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
                    int ageDays = fruitTree.daysUntilMature.Value <= 0 ? Math.Abs(fruitTree.daysUntilMature.Value) : 0;
                    string quality = ageDays >= 84 ? "Iridium Quality (3+ Years Old)" : (ageDays >= 56 ? "Gold Quality (2 Years Old)" : (ageDays >= 28 ? "Silver Quality (1 Year Old)" : "Normal Quality (First Year)"));
                    section.Fields.Add(new LookupField("Fruit Quality", quality, new Color(180, 50, 180)));

                    section.Fields.Add(new LookupField(
                        "Fruit Count",
                        $"{info.FruitsOnTree} / 3 Ready",
                        info.FruitsOnTree > 0 ? new Color(0, 140, 0) : Color.DarkSlateGray
                    ));

                    section.Fields.Add(new LookupField(
                        "Season",
                        info.IsInSeason ? ModEntry.I18n.Get("hover.fruit-tree.in-season") : ModEntry.I18n.Get("hover.fruit-tree.out-of-season"),
                        info.IsInSeason ? new Color(20, 110, 220) : Color.DarkSlateGray
                    ));
                }

                // Check 8 surrounding tiles for growth obstruction
                if (fruitTree.Location != null)
                {
                    bool isBlocked = false;
                    Vector2 treeTile = fruitTree.Tile;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            Vector2 neighbor = new Vector2(treeTile.X + dx, treeTile.Y + dy);
                            if (fruitTree.Location.Objects.ContainsKey(neighbor) ||
                                (fruitTree.Location.terrainFeatures.TryGetValue(neighbor, out var tf) && tf is not HoeDirt))
                            {
                                isBlocked = true;
                                break;
                            }
                        }
                        if (isBlocked) break;
                    }

                    section.Fields.Add(new LookupField(
                        "Surrounding 8 Tiles",
                        isBlocked ? "Blocked by objects/trees ✗ (Growth may be stunted!)" : "Clear ✓ (Optimal growth conditions)",
                        isBlocked ? new Color(200, 60, 20) : new Color(0, 140, 0)
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

        public static LookupSubject BuildResourceClumpSubject(ResourceClump clump)
        {
            int index = clump.parentSheetIndex.Value;
            string name = index switch
            {
                600 => "Large Stump",
                602 => "Hollow Log",
                622 => "Meteorite",
                672 => "Giant Boulder",
                752 or 754 or 756 or 758 => "Mine Boulder",
                889 => "Fossil Rock",
                _ => "Resource Clump"
            };

            string toolReq = index switch
            {
                600 => "Copper Axe (or higher)",
                602 => "Steel Axe (or higher)",
                622 => "Gold Pickaxe (or higher)",
                672 => "Steel Pickaxe (or higher)",
                752 or 754 or 756 or 758 => "Steel Pickaxe",
                889 => "Any Pickaxe",
                _ => "Tool"
            };

            string drops = index switch
            {
                600 => "2 Hardwood, Foraging Experience (Chance for Mahogany Seed)",
                602 => "8 Hardwood, Foraging Experience (Chance for Secret Notes / Seeds)",
                622 => "6 Iridium Ore, 6 Stone, 2 Prismatic Shards (25% chance)",
                672 => "10 Stone, 1–3 Coal, Geodes",
                752 or 754 or 756 or 758 => "10 Stone, Ores, Geodes, Coal",
                889 => "Bone Fragments, Artifact Fossils, Clay",
                _ => "Resources"
            };

            var subject = new LookupSubject
            {
                Title = name,
                Subtitle = "Resource Clump"
            };

            var section = new LookupSection("Debris & Clearing Details");
            section.Fields.Add(new LookupField("Tool Required", toolReq, new Color(200, 60, 20)));
            section.Fields.Add(new LookupField("Hits Remaining", $"{clump.health.Value} hits remaining", new Color(180, 100, 0)));
            section.Fields.Add(new LookupField("Resource Drops", drops, new Color(0, 140, 0)));
            subject.Sections.Add(section);

            return subject;
        }

        public static LookupSubject BuildGiantCropSubject(GiantCrop giantCrop)
        {
            string cropName = giantCrop.Id switch
            {
                "190" or "Cauliflower" => "Cauliflower",
                "254" or "Melon" => "Melon",
                "276" or "Pumpkin" => "Pumpkin",
                "Powdermelon" => "Powdermelon",
                "QiFruit" => "Qi Fruit",
                _ => giantCrop.Id ?? "Crop"
            };

            var subject = new LookupSubject
            {
                Title = $"Giant {cropName}",
                Subtitle = "Giant Crop"
            };

            var section = new LookupSection("Giant Crop Details");
            section.Fields.Add(new LookupField("Tool Required", "Axe (Any quality)", new Color(20, 110, 220)));
            section.Fields.Add(new LookupField("Hits Remaining", $"{giantCrop.health.Value} axe hits remaining", new Color(180, 100, 0)));
            section.Fields.Add(new LookupField("Harvest Yield", "Yields 15–21 normal crops upon harvest", new Color(0, 140, 0)));
            subject.Sections.Add(section);

            return subject;
        }

        public static LookupSubject BuildBuildingSubject(Building building)
        {
            string bType = building.buildingType.Value;
            var subject = new LookupSubject
            {
                Title = !string.IsNullOrEmpty(bType) ? bType : "Farm Building",
                Subtitle = ModEntry.I18n.Get("lookup.type.building").ToString()
            };

            var section = new LookupSection("Building Status & Operations");

            // 1. Junimo Hut
            if (building is JunimoHut hut)
            {
                subject.Title = "Junimo Hut";
                bool harvesting = !hut.noHarvest.Value;
                section.Fields.Add(new LookupField(
                    "Harvesting State",
                    harvesting ? "Active ✓ (Junimos actively harvesting crops)" : "Paused ✗ (Junimos resting)",
                    harvesting ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                int raisinDays = hut.raisinDays.Value;
                section.Fields.Add(new LookupField(
                    "1.6 Raisins Boost",
                    raisinDays > 0 ? $"Active ✓ ({raisinDays} days left — 20% double crop chance!)" : "None fed ✗ (Place Raisins in hut for 20% 2x Harvests)",
                    raisinDays > 0 ? new Color(180, 50, 180) : Color.DarkSlateGray
                ));

                section.Fields.Add(new LookupField("Harvest Range", "17 x 17 tile radius around hut", new Color(20, 110, 220)));

                var hutChest = hut.GetOutputChest();
                if (hutChest != null && hutChest.Items.Any(i => i != null))
                {
                    var storedLinks = new List<LookupLink>();
                    foreach (var sItem in hutChest.Items.Where(i => i != null))
                    {
                        var itmData = ItemRegistry.GetData(sItem.QualifiedItemId);
                        storedLinks.Add(new LookupLink(
                            text: $"{sItem.DisplayName} (x{sItem.Stack})",
                            textColor: Game1.textColor,
                            icon: itmData?.GetTexture(),
                            iconSourceRect: itmData?.GetSourceRect(),
                            onClick: () => BuildItemSubject(sItem)
                        ));
                    }
                    section.Fields.Add(new LookupField("Stored Output Items", storedLinks));
                }
            }
            // 2. Barn / Coop (AnimalHouse)
            else if (building.indoors.Value is AnimalHouse animalHouse)
            {
                int occupants = animalHouse.animalsThatLiveHere.Count;
                int maxCap = animalHouse.animalLimit.Value;
                section.Fields.Add(new LookupField(
                    "Animal Capacity",
                    $"{occupants} / {maxCap} Occupants",
                    occupants >= maxCap ? new Color(0, 140, 0) : new Color(20, 110, 220)
                ));

                int hayCount = animalHouse.numberOfObjectsWithName("Hay");
                section.Fields.Add(new LookupField(
                    "Feed Troughs",
                    $"{hayCount} / {maxCap} Troughs filled with Hay",
                    hayCount >= occupants ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                // Animal list links
                var animalLinks = new List<LookupLink>();
                foreach (long id in animalHouse.animalsThatLiveHere)
                {
                    var a = Utility.getAnimal(id);
                    if (a != null)
                    {
                        var target = a;
                        animalLinks.Add(new LookupLink(
                            text: $"{target.Name} ({target.displayType})",
                            textColor: Game1.textColor,
                            onClick: () => BuildAnimalSubject(target)
                        ));
                    }
                }
                if (animalLinks.Count > 0)
                {
                    section.Fields.Add(new LookupField("Animals Inside", animalLinks));
                }

                // Egg Incubator Status
                foreach (var obj in animalHouse.objects.Values)
                {
                    if (obj.Name != null && obj.Name.Contains("Incubator") && obj.heldObject.Value != null)
                    {
                        var egg = obj.heldObject.Value;
                        int days = obj.MinutesUntilReady / 1000;
                        section.Fields.Add(new LookupField(
                            "Incubator",
                            $"{egg.DisplayName} incubating ({days} day(s) until hatch)",
                            new Color(180, 100, 0)
                        ));
                    }
                }
            }
            // 3. Silo
            else if (bType.Contains("Silo"))
            {
                var farm = Game1.getFarm();
                int hay = farm?.piecesOfHay.Value ?? 0;
                int maxHay = (farm?.buildings.Count(b => b.buildingType.Value.Contains("Silo")) ?? 1) * 240;
                section.Fields.Add(new LookupField("Hay Capacity", $"{hay} / {maxHay} Total Hay Stored", hay < maxHay / 4 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
            }
            // 4. Slime Hutch
            else if (building.indoors.Value is SlimeHutch slimeHutch)
            {
                int slimeCount = slimeHutch.characters.Count(c => c is StardewValley.Monsters.GreenSlime);
                section.Fields.Add(new LookupField("Slime Population", $"{slimeCount} / 20 Slimes", new Color(0, 140, 0)));

                int waterCount = slimeHutch.waterSpots.Count(w => w);
                section.Fields.Add(new LookupField("Water Troughs", $"{waterCount} / 4 Troughs Watered", waterCount == 4 ? new Color(0, 140, 0) : new Color(200, 60, 20)));
            }
            // 5. Stable
            else if (building is Stable stable)
            {
                string hName = Game1.player.horseName.Value ?? "Horse";
                section.Fields.Add(new LookupField("Horse", $"{hName}", new Color(180, 100, 0)));
            }
            // 6. Pet Bowl (1.6)
            else if (building is PetBowl petBowl)
            {
                bool watered = petBowl.watered.Value;
                section.Fields.Add(new LookupField(
                    "Water Status",
                    watered ? "Filled with Water today ✓ (+6 Friendship bonus)" : "Empty ✗ (Fill with Watering Can for +6 Friendship)",
                    watered ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));
            }

            subject.Sections.Add(section);
            return subject;
        }

        public static LookupSubject BuildChestSubject(Chest chest)
        {
            string chestName = chest.DisplayName ?? "Chest";
            int usedSlots = chest.Items.Count(i => i != null);
            int totalSlots = chest.GetActualCapacity();
            int totalItemCount = chest.Items.Where(i => i != null).Sum(i => i.Stack);

            var subject = new LookupSubject
            {
                Title = chestName,
                Subtitle = "Storage Container"
            };

            var section = new LookupSection("Storage Overview");
            section.Fields.Add(new LookupField(
                "Capacity",
                $"{usedSlots} / {totalSlots} Slots Used ({totalSlots - usedSlots} slots free)",
                usedSlots >= totalSlots ? new Color(200, 60, 20) : new Color(0, 140, 0)
            ));
            section.Fields.Add(new LookupField("Total Items", $"{totalItemCount:N0} items stored total", new Color(20, 110, 220)));

            if (usedSlots > 0)
            {
                var itemLinks = new List<LookupLink>();
                foreach (var item in chest.Items.Where(i => i != null).Take(36))
                {
                    var data = ItemRegistry.GetData(item.QualifiedItemId);
                    var target = item;
                    itemLinks.Add(new LookupLink(
                        text: $"{target.DisplayName} (x{target.Stack})",
                        textColor: Game1.textColor,
                        icon: data?.GetTexture(),
                        iconSourceRect: data?.GetSourceRect(),
                        onClick: () => BuildItemSubject(target)
                    ));
                }
                section.Fields.Add(new LookupField("Stored Items", itemLinks));
            }

            subject.Sections.Add(section);
            return subject;
        }

        public static LookupSubject BuildFarmerSubject(Farmer farmer)
        {
            var subject = new LookupSubject
            {
                Title = farmer.Name,
                Subtitle = $"{farmer.farmName.Value} Farm — {farmer.getTitle()}"
            };

            // 1. Active Buffs
            var buffSection = new LookupSection("Active Buffs & Effects");
            if (farmer.buffs != null && farmer.buffs.AppliedBuffs.Count > 0)
            {
                foreach (var kvp in farmer.buffs.AppliedBuffs)
                {
                    var buff = kvp.Value;
                    string bName = !string.IsNullOrEmpty(buff.displayName) ? buff.displayName : kvp.Key;
                    string durText = buff.millisecondsDuration > 0 && buff.millisecondsDuration < 9999999
                        ? $"{buff.millisecondsDuration / 60000}m {(buff.millisecondsDuration % 60000) / 1000}s left"
                        : "Permanent / Endless";

                    var effectParts = new List<string>();
                    var eff = buff.effects;
                    if (eff != null)
                    {
                        if (eff.Speed.Value != 0) effectParts.Add($"Speed +{eff.Speed.Value:0.#}");
                        if (eff.Attack.Value != 0) effectParts.Add($"Attack +{eff.Attack.Value:0.#}");
                        if (eff.Defense.Value != 0) effectParts.Add($"Defense +{eff.Defense.Value:0.#}");
                        if (eff.LuckLevel.Value != 0) effectParts.Add($"Luck +{eff.LuckLevel.Value:0.#}");
                        if (eff.FarmingLevel.Value != 0) effectParts.Add($"Farming +{eff.FarmingLevel.Value:0.#}");
                        if (eff.MiningLevel.Value != 0) effectParts.Add($"Mining +{eff.MiningLevel.Value:0.#}");
                        if (eff.FishingLevel.Value != 0) effectParts.Add($"Fishing +{eff.FishingLevel.Value:0.#}");
                        if (eff.ForagingLevel.Value != 0) effectParts.Add($"Foraging +{eff.ForagingLevel.Value:0.#}");
                        if (eff.MaxStamina.Value != 0) effectParts.Add($"Max Energy +{eff.MaxStamina.Value:0.#}");
                        if (eff.MagneticRadius.Value != 0) effectParts.Add($"Magnetism +{eff.MagneticRadius.Value:0.#}");
                    }
                    string effectsStr = effectParts.Count > 0 ? $" ({string.Join(", ", effectParts)})" : "";
                    buffSection.Fields.Add(new LookupField(bName, $"{durText}{effectsStr}", new Color(180, 50, 180)));
                }
            }
            else
            {
                buffSection.Fields.Add(new LookupField("Active Buffs", "No buffs currently active", Color.DarkSlateGray));
            }
            subject.Sections.Add(buffSection);

            // 2. Special Powers & Wallet
            var walletSection = new LookupSection("Special Powers & Wallet");
            walletSection.Fields.Add(new LookupField("Special Charm", farmer.hasSpecialCharm ? "Unlocked ✓ (+0.025 Permanent Luck)" : "Locked ✗", farmer.hasSpecialCharm ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Skull Key", farmer.hasSkullKey ? "Unlocked ✓ (Skull Cavern & Junimo Kart)" : "Locked ✗", farmer.hasSkullKey ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Club Card", farmer.hasClubCard ? "Unlocked ✓ (Oasis Casino Access)" : "Locked ✗", farmer.hasClubCard ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Magnifying Glass", farmer.hasMagnifyingGlass ? "Unlocked ✓ (Secret Notes Finding)" : "Locked ✗", farmer.hasMagnifyingGlass ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Dark Talisman", farmer.hasDarkTalisman ? "Unlocked ✓ (Witch's Swamp Access)" : "Locked ✗", farmer.hasDarkTalisman ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Magic Ink", farmer.hasMagicInk ? "Unlocked ✓ (Wizard Magical Buildings)" : "Locked ✗", farmer.hasMagicInk ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Bear's Knowledge", farmer.eventsSeen.Contains("2120303") || farmer.hasOrWillReceiveMail("BearKnowledge") ? "Unlocked ✓ (3x Blackberry & Salmonberry Value)" : "Locked ✗", farmer.eventsSeen.Contains("2120303") || farmer.hasOrWillReceiveMail("BearKnowledge") ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Spring Onion Mastery", farmer.eventsSeen.Contains("3910979") || farmer.hasOrWillReceiveMail("SpringOnionMastery") ? "Unlocked ✓ (5x Spring Onion Value)" : "Locked ✗", farmer.eventsSeen.Contains("3910979") || farmer.hasOrWillReceiveMail("SpringOnionMastery") ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Dwarvish Translation", farmer.canUnderstandDwarves ? "Unlocked ✓ (Speak with Dwarf Merchant)" : "Locked ✗", farmer.canUnderstandDwarves ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField("Key to the Town", farmer.HasTownKey ? "Unlocked ✓ (Enter all town buildings 24/7)" : "Locked ✗", farmer.HasTownKey ? new Color(0, 140, 0) : Color.DarkSlateGray));
            subject.Sections.Add(walletSection);

            // 3. Stats
            var statsSection = new LookupSection("Farmer Statistics");
            statsSection.Fields.Add(new LookupField("Current Gold", $"{farmer.Money:N0}g", new Color(180, 100, 0)));
            statsSection.Fields.Add(new LookupField("Total Earnings", $"{farmer.totalMoneyEarned:N0}g", new Color(0, 140, 0)));
            statsSection.Fields.Add(new LookupField("Qi Gems", $"{farmer.QiGems}", new Color(180, 50, 180)));
            statsSection.Fields.Add(new LookupField("Golden Walnuts", $"{Game1.netWorldState.Value.GoldenWalnutsFound} / 130 Found", new Color(180, 100, 0)));
            statsSection.Fields.Add(new LookupField("Daily Luck", $"{farmer.DailyLuck:F3}", farmer.DailyLuck >= 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)));
            subject.Sections.Add(statsSection);

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

            // 4. TV, Bookseller, Clint Upgrades & Quests
            var eventSec = BuildSpecialEventsSection();
            if (eventSec.Fields.Count > 0)
            {
                subject.Sections.Add(eventSec);
            }

            // 5. Farm & Chores Summary (Crops, Animals, Machines, Silo, Greenhouse, Island)
            subject.Sections.Add(BuildFarmSummarySection());

            // 6. Current Season Plantable Crops & Seeds Guide
            subject.Sections.Add(BuildSeasonalCropsSection());

            // 7. Current Season Wild Forage Guide
            subject.Sections.Add(BuildSeasonalForageSection());

            // 8. Player Skills, Level & 1.6 Mastery
            subject.Sections.Add(BuildSkillsAndMasterySection());

            // 9. Ginger Island & Qi Progress (if unlocked)
            var islandSec = BuildIslandProgressSection();
            if (islandSec != null && islandSec.Fields.Count > 0)
            {
                subject.Sections.Add(islandSec);
            }

            // 10. Tile Specifics (if clicked on tile)
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

        private static LookupSection BuildSeasonalCropsSection()
        {
            string seasonName = char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1);
            var section = new LookupSection($"Seasonal Crops & Seeds ({seasonName})");
            var cropLinks = new List<LookupLink>();

            try
            {
                var cropDict = DataLoader.Crops(Game1.content);
                if (cropDict != null)
                {
                    foreach (var kvp in cropDict)
                    {
                        var cropData = kvp.Value;
                        if (cropData.Seasons != null && cropData.Seasons.Any(s => s.ToString().Equals(Game1.currentSeason, StringComparison.OrdinalIgnoreCase)))
                        {
                            var harvestItem = ItemRegistry.GetData(cropData.HarvestItemId);
                            if (harvestItem != null && !cropLinks.Any(l => l.Text.StartsWith(harvestItem.DisplayName)))
                            {
                                int totalDays = cropData.DaysInPhase?.Sum() ?? 0;
                                string regrow = cropData.RegrowDays > 0 ? $", regrows {cropData.RegrowDays}d" : "";
                                string infoText = $"{harvestItem.DisplayName} ({totalDays}d{regrow})";

                                var item = ItemRegistry.Create(harvestItem.QualifiedItemId);
                                cropLinks.Add(new LookupLink(
                                    text: infoText,
                                    textColor: cropData.RegrowDays > 0 ? new Color(0, 140, 0) : Game1.textColor,
                                    icon: harvestItem.GetTexture(),
                                    iconSourceRect: harvestItem.GetSourceRect(),
                                    onClick: () => item != null ? BuildItemSubject(item) : null
                                ));
                            }
                        }
                    }
                }
            }
            catch { }

            if (cropLinks.Count > 0)
            {
                section.Fields.Add(new LookupField("Plantable Crops", cropLinks));
            }
            else
            {
                section.Fields.Add(new LookupField("Plantable Crops", "No outdoor crops available to plant in Winter (Use Greenhouse / Pots / Fiber Seeds)", Color.DarkSlateGray));
            }

            return section;
        }

        private static LookupSection BuildSeasonalForageSection()
        {
            string seasonName = char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1);
            var section = new LookupSection($"Seasonal Wild Forage ({seasonName})");
            var forageLinks = new List<LookupLink>();

            string[] forageIds = Game1.currentSeason.ToLower() switch
            {
                "spring" => new[] { "(O)16", "(O)18", "(O)20", "(O)22", "(O)399", "(O)257", "(O)296" },
                "summer" => new[] { "(O)396", "(O)398", "(O)394", "(O)259", "(O)402", "(O)393" },
                "fall" => new[] { "(O)404", "(O)406", "(O)408", "(O)410", "(O)281", "(O)420" },
                "winter" => new[] { "(O)412", "(O)414", "(O)416", "(O)418", "(O)283" },
                _ => Array.Empty<string>()
            };

            foreach (var id in forageIds)
            {
                var data = ItemRegistry.GetData(id);
                if (data != null && !forageLinks.Any(l => l.Text == data.DisplayName))
                {
                    var item = ItemRegistry.Create(data.QualifiedItemId);
                    forageLinks.Add(new LookupLink(
                        text: data.DisplayName,
                        textColor: Game1.textColor,
                        icon: data.GetTexture(),
                        iconSourceRect: data.GetSourceRect(),
                        onClick: () => item != null ? BuildItemSubject(item) : null
                    ));
                }
            }

            // Beach Forage
            var beachLinks = new List<LookupLink>();
            foreach (var bId in new[] { "(O)372", "(O)393", "(O)397", "(O)152" })
            {
                var data = ItemRegistry.GetData(bId);
                if (data != null)
                {
                    var item = ItemRegistry.Create(data.QualifiedItemId);
                    beachLinks.Add(new LookupLink(
                        text: data.DisplayName,
                        textColor: new Color(20, 110, 220),
                        icon: data.GetTexture(),
                        iconSourceRect: data.GetSourceRect(),
                        onClick: () => item != null ? BuildItemSubject(item) : null
                    ));
                }
            }

            if (forageLinks.Count > 0)
            {
                section.Fields.Add(new LookupField("Valley Forage", forageLinks));
            }
            if (beachLinks.Count > 0)
            {
                section.Fields.Add(new LookupField("Beach Forage", beachLinks));
            }

            return section;
        }

        private static LookupSection BuildSkillsAndMasterySection()
        {
            var section = new LookupSection("Skills, Level & 1.6 Mastery");

            int farmLvl = Game1.player.FarmingLevel;
            int mineLvl = Game1.player.MiningLevel;
            int forageLvl = Game1.player.ForagingLevel;
            int fishLvl = Game1.player.FishingLevel;
            int combatLvl = Game1.player.CombatLevel;

            section.Fields.Add(new LookupField(
                "Skill Levels",
                $"Farming: {farmLvl} | Mining: {mineLvl} | Foraging: {forageLvl} | Fishing: {fishLvl} | Combat: {combatLvl}",
                new Color(0, 140, 0)
            ));

            try
            {
                int totalLvl = farmLvl + mineLvl + forageLvl + fishLvl + combatLvl;
                if (totalLvl >= 50)
                {
                    int masteryExp = (int)Game1.stats.Get("MasteryExp");
                    section.Fields.Add(new LookupField("Mastery Progress", $"{masteryExp:N0} Mastery XP (Cave of Mastery)", new Color(180, 50, 180)));

                    bool combatM = Game1.player.stats.Get("Mastery_0") > 0;
                    bool forageM = Game1.player.stats.Get("Mastery_1") > 0;
                    bool farmM = Game1.player.stats.Get("Mastery_2") > 0;
                    bool fishM = Game1.player.stats.Get("Mastery_3") > 0;
                    bool mineM = Game1.player.stats.Get("Mastery_4") > 0;

                    section.Fields.Add(new LookupField("Combat Mastery", combatM ? "Claimed ✓ (Anvil, Mini-Forge, Trinket Slot)" : "Locked ✗", combatM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField("Foraging Mastery", forageM ? "Claimed ✓ (Mystic Tree Seed, Treasure Totem)" : "Locked ✗", forageM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField("Farming Mastery", farmM ? "Claimed ✓ (Iridium Scythe, Statue of Blessings)" : "Locked ✗", farmM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField("Fishing Mastery", fishM ? "Claimed ✓ (Advanced Iridium Rod, Challenge Bait)" : "Locked ✗", fishM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField("Mining Mastery", mineM ? "Claimed ✓ (Heavy Furnace, Statue of the Dwarf King)" : "Locked ✗", mineM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                }
            }
            catch { }

            return section;
        }

        private static LookupSection? BuildIslandProgressSection()
        {
            try
            {
                if (Game1.netWorldState.Value.GoldenWalnutsFound > 0 || Game1.player.hasOrWillReceiveMail("Visited_Island"))
                {
                    var section = new LookupSection("Ginger Island Exploration");
                    int walnuts = Game1.netWorldState.Value.GoldenWalnutsFound;
                    section.Fields.Add(new LookupField("Golden Walnuts", $"{walnuts} / 130 Found", walnuts >= 130 ? new Color(0, 140, 0) : new Color(180, 100, 0)));

                    if (Game1.player.hasOrWillReceiveMail("QiChallengeComplete") || Game1.player.QiGems > 0)
                    {
                        section.Fields.Add(new LookupField("Qi Gems", $"{Game1.player.QiGems} Qi Gems", new Color(180, 50, 180)));
                    }

                    return section;
                }
            }
            catch { }
            return null;
        }

        #endregion

        #region 7. Find Anything Query Engine (Live Search)

        public static List<LookupLink> SearchAll(string query, string category = "All")
        {
            var results = new List<LookupLink>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            string q = query.Trim().ToLower();
            string cat = category.Trim();

            bool includeVillagers = cat == "All" || cat == "Villagers" || cat == "NPCs";
            bool includeItems = cat == "All" || cat == "Items" || cat == "Fish" || cat == "Crops";
            bool includeMonsters = cat == "All" || cat == "Monsters";
            bool includeBuildings = cat == "All" || cat == "Buildings";

            // 1. Search Villagers
            if (includeVillagers)
            {
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
            }

            // 2. Search Items across all categories using typeDef.GetAllIds()
            if (includeItems)
            {
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
                                if (cat == "Fish" && itemData.Category != StardewValley.Object.FishCategory)
                                    continue;
                                if (cat == "Crops" && itemData.Category != StardewValley.Object.VegetableCategory && itemData.Category != StardewValley.Object.FruitsCategory && itemData.Category != StardewValley.Object.SeedsCategory)
                                    continue;

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

                                if (results.Count >= 50)
                                    break;
                            }
                        }
                    }

                    if (results.Count >= 50)
                        break;
                }
            }

            // 3. Search Monsters from DataLoader
            if (includeMonsters)
            {
                try
                {
                    var monsterDict = DataLoader.Monsters(Game1.content);
                    if (monsterDict != null)
                    {
                        foreach (var kvp in monsterDict)
                        {
                            string mName = kvp.Key;
                            if (mName.ToLower().Contains(q) && !results.Any(r => r.Text == mName))
                            {
                                string monsterData = kvp.Value;
                                results.Add(new LookupLink(
                                    text: mName,
                                    subtitle: "Monster",
                                    textColor: new Color(200, 60, 20),
                                    icon: null,
                                    iconSourceRect: null,
                                    onClick: () =>
                                    {
                                        var mSubject = new LookupSubject
                                        {
                                            Title = mName,
                                            Subtitle = "Monster"
                                        };
                                        var mSection = new LookupSection("Combat Stats");
                                        string[] parts = monsterData.Split('/');
                                        if (parts.Length > 0) mSection.Fields.Add(new LookupField("Health", parts[0], new Color(220, 20, 60)));
                                        if (parts.Length > 1) mSection.Fields.Add(new LookupField("Damage", parts[1], new Color(200, 60, 20)));
                                        if (parts.Length > 7) mSection.Fields.Add(new LookupField("Defense", parts[7], new Color(20, 110, 220)));
                                        if (parts.Length > 8) mSection.Fields.Add(new LookupField("Experience", parts[8], new Color(180, 100, 0)));
                                        mSubject.Sections.Add(mSection);
                                        return mSubject;
                                    }
                                ));

                                if (results.Count >= 50)
                                    break;
                            }
                        }
                    }
                }
                catch { }
            }

            // 4. Search Buildings & Farm Blueprints from DataLoader
            if (includeBuildings)
            {
                try
                {
                    var buildingDict = DataLoader.Buildings(Game1.content);
                    if (buildingDict != null)
                    {
                        foreach (var kvp in buildingDict)
                        {
                            var bData = kvp.Value;
                            string bName = bData.Name ?? kvp.Key;
                            if (bName.ToLower().Contains(q) && !results.Any(r => r.Text == bName))
                            {
                                results.Add(new LookupLink(
                                    text: bName,
                                    subtitle: "Farm Building",
                                    textColor: new Color(180, 100, 0),
                                    icon: null,
                                    iconSourceRect: null,
                                    onClick: () =>
                                    {
                                        var bSubject = new LookupSubject
                                        {
                                            Title = bName,
                                            Subtitle = "Farm Building"
                                        };
                                        var bSection = new LookupSection("Building Details");
                                        if (!string.IsNullOrEmpty(bData.Description)) bSection.Fields.Add(new LookupField("Description", bData.Description, Color.DarkSlateGray));
                                        if (bData.BuildCost > 0) bSection.Fields.Add(new LookupField("Build Cost", $"{bData.BuildCost}g", new Color(180, 100, 0)));
                                        if (bData.BuildMaterials != null && bData.BuildMaterials.Count > 0)
                                        {
                                            var matStrs = bData.BuildMaterials.Select(m => {
                                                var mItem = ItemRegistry.GetData(m.ItemId);
                                                return $"{mItem?.DisplayName ?? m.ItemId} (x{m.Amount})";
                                            });
                                            bSection.Fields.Add(new LookupField("Materials", string.Join(", ", matStrs), Game1.textColor));
                                        }
                                        bSubject.Sections.Add(bSection);
                                        return bSubject;
                                    }
                                ));

                                if (results.Count >= 50)
                                    break;
                            }
                        }
                    }
                }
                catch { }
            }

            return results;
        }

        #endregion
    }
}
