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
                    ModEntry.I18n.Get("lookup.npc.friendship-format", new { hearts, maxHearts, points, ptsInHeart }).ToString(),
                    new Color(220, 20, 60)
                ));

                if (npc.currentLocation != null)
                {
                    string locName = npc.currentLocation.DisplayName ?? npc.currentLocation.Name;
                    relSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.current-location"), ModEntry.I18n.Get("lookup.npc.current-location-format", new { location = locName, x = (int)npc.Tile.X, y = (int)npc.Tile.Y }).ToString(), new Color(20, 110, 220)));
                }

                bool talkedToday = Game1.player.hasPlayerTalkedToNPC(npc.Name);
                relSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.npc.talked-today"),
                    talkedToday ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"),
                    talkedToday ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                relSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.npc.gifts-this-week"),
                    ModEntry.I18n.Get("lookup.npc.gifts-this-week-format", new { count = friendship.GiftsThisWeek, today = (friendship.GiftsToday > 0 ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no")) }).ToString(),
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
                    giftSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.neutral-gifts"), neutralLinks));
                }

                if (dislikedLinks.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.disliked-gifts"), dislikedLinks));
                }

                subject.Sections.Add(giftSection);
            }

            // Section 3: Daily Schedule
            try
            {
                var schedSection = new LookupSection(ModEntry.I18n.Get("lookup.section.schedule", "Daily Schedule"));
                bool isVisitingIsland = Game1.netWorldState.Value.IslandVisitors != null && Game1.netWorldState.Value.IslandVisitors.Contains(npc.Name);
                if (isVisitingIsland)
                {
                    schedSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.today-schedule"), ModEntry.I18n.Get("lookup.npc.schedule-island").ToString(), new Color(20, 110, 220)));
                }
                else if (npc.Schedule != null && npc.Schedule.Count > 0)
                {
                    var sorted = npc.Schedule.OrderBy(kv => kv.Key).ToList();
                    for (int i = 0; i < sorted.Count; i++)
                    {
                        var kvp = sorted[i];
                        int schedTime = kvp.Key;
                        var path = kvp.Value;
                        string timeFormatted = FormatGameTime(schedTime.ToString());

                        string locName = path.targetLocationName ?? (npc.currentLocation?.DisplayName ?? ModEntry.I18n.Get("lookup.schedule.unknown-location").ToString());
                        var locObj = Game1.getLocationFromName(locName);
                        string displayLoc = locObj?.DisplayName ?? locName;

                        bool isCurrent = false;
                        int nextTime = (i + 1 < sorted.Count) ? sorted[i + 1].Key : 2600;
                        if (Game1.timeOfDay >= schedTime && Game1.timeOfDay < nextTime)
                        {
                            isCurrent = true;
                        }

                        string actionDesc = $"{displayLoc} (Tile: {path.targetTile.X}, {path.targetTile.Y})";
                        if (!string.IsNullOrEmpty(path.endOfRouteBehavior))
                        {
                            actionDesc += $" — {path.endOfRouteBehavior}";
                        }

                        string fieldKey = timeFormatted + (isCurrent ? ModEntry.I18n.Get("lookup.schedule.current-tag").ToString() : "");
                        schedSection.Fields.Add(new LookupField(
                            fieldKey,
                            actionDesc,
                            isCurrent ? new Color(0, 140, 0) : Game1.textColor
                        ));
                    }
                }
                else
                {
                    string currLoc = npc.currentLocation != null ? (npc.currentLocation.DisplayName ?? npc.currentLocation.Name) : ModEntry.I18n.Get("lookup.schedule.unknown-location").ToString();
                    schedSection.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.section.schedule"),
                        ModEntry.I18n.Get("lookup.schedule.no-departures", new { location = currLoc, x = (int)npc.Tile.X, y = (int)npc.Tile.Y }).ToString(),
                        Color.DarkSlateGray
                    ));
                }
                subject.Sections.Add(schedSection);
            }
            catch { }

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
                    overviewSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.buffs"), string.Join(", ", buffs), new Color(180, 50, 180)));
                }
            }

            // Sell Prices by Quality (Using localized Silver/Gold/Iridium labels)
            int baseSellPrice = item.sellToStorePrice();
            if (baseSellPrice > 0)
            {
                int silverPrice = (int)(baseSellPrice * 1.25);
                int goldPrice = (int)(baseSellPrice * 1.5);
                int iridiumPrice = (int)(baseSellPrice * 2.0);

                string silverLabel = ModEntry.I18n.Get("hover.quality.silver");
                string goldLabel = ModEntry.I18n.Get("hover.quality.gold");
                string iridiumLabel = ModEntry.I18n.Get("hover.quality.iridium");

                overviewSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.item.sell-price"),
                    $"{baseSellPrice}g ({silverLabel}: {silverPrice}g, {goldLabel}: {goldPrice}g, {iridiumLabel}: {iridiumPrice}g)",
                    new Color(180, 100, 0)
                ));
            }

            // Number Owned (in inventory + storage chests across the world)
            var (invCount, storageCount) = GetItemOwnedCounts(item);
            int totalOwned = invCount + storageCount;
            string ownedStr = totalOwned > 0
                ? ModEntry.I18n.Get("hover.number-owned-format", new { inv = invCount, storage = storageCount, total = totalOwned }).ToString()
                : ModEntry.I18n.Get("hover.number-owned-none").ToString();
            overviewSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.number-owned"), ownedStr, totalOwned > 0 ? new Color(0, 140, 0) : Color.DarkSlateGray));

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

            // Section 10: Machine Operations & Farm Equipment
            AddMachineItemSection(subject, item);

            // Section 11: Fruit Tree Sapling Guide
            AddFruitTreeSaplingSection(subject, item);

            // Section 12: Special Item Details & Lore (Currencies, Consumables, 1.6 Items)
            AddSpecialItemLoreSection(subject, item);

            // Section 13: Animal Processing & Incubation (Eggs, Milk, Wool, Truffles)
            AddAnimalProductProcessingSection(subject, item);

            // Section 14: Geodes, Artifact Troves & Mystery Boxes
            AddGeodeAndMysteryBoxSection(subject, item);

            // Section 15: Fertilizer Details (Soil & Tree Growth Effects)
            AddFertilizerDetailsSection(subject, item);

            // Section 16: Recycling Machine Yields
            AddRecyclingSection(subject, item);

            // Section 17: Artisan Processing (Keg, Preserves Jar, Dehydrator, Cask, Fish Smoker, Oil, Mill, Seed Maker)
            AddArtisanProductsSection(subject, item, baseSellPrice);

            // Section 18: Tailoring & Dyeing (Sewing Machine Product & Dye Pot Color)
            AddTailoringAndDyeSection(subject, item);

            // Section 19: Collections & Perfection Tracker (Shipped, Caught, Cooked, Crafted)
            AddCollectionAndPerfectionSection(subject, item);

            // Section 20: Museum & Bundles
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
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.weapon-combat"));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.damage"), $"{weapon.minDamage.Value} - {weapon.maxDamage.Value}", new Color(200, 60, 20)));

                    double critChance = weapon.critChance.Value * 100;
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.crit-strike"), ModEntry.I18n.Get("lookup.weapon.crit-multiplier", new { chance = $"{critChance:0.#}", mult = $"{weapon.critMultiplier.Value:0.#}" }).ToString(), new Color(180, 50, 180)));

                    if (weapon.speed.Value != 0)
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.speed"), $"{(weapon.speed.Value > 0 ? "+" : "")}{weapon.speed.Value}", weapon.speed.Value > 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)));

                    if (weapon.addedDefense.Value != 0)
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.defense"), $"{(weapon.addedDefense.Value > 0 ? "+" : "")}{weapon.addedDefense.Value}", new Color(20, 110, 220)));

                    if (weapon.addedAreaOfEffect.Value != 0)
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.reach"), $"+{weapon.addedAreaOfEffect.Value}", Color.DarkSlateGray));

                    if (weapon.knockback.Value != 1f)
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.knockback"), $"{weapon.knockback.Value:0.0}", Color.DarkSlateGray));

                    int forges = weapon.GetTotalForgeLevels();
                    if (forges > 0)
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.volcano-forges"), ModEntry.I18n.Get("lookup.weapon.volcano-forges-level", new { level = forges }).ToString(), new Color(180, 100, 0)));

                    if (weapon.enchantments.Count > 0)
                    {
                        foreach (var ench in weapon.enchantments)
                        {
                            string eName = ench.GetName();
                            string eDesc = GetEnchantmentDescription(eName);
                            string val = !string.IsNullOrEmpty(eDesc) ? $"{eName} — {eDesc}" : eName;
                            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.enchantment"), val, new Color(180, 50, 180)));
                        }
                    }

                    subject.Sections.Add(section);
                }
                else if (item is Slingshot slingshot)
                {
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.slingshot"));
                    bool isMaster = slingshot.ItemId == "33" || slingshot.Name.Contains("Master");
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.slingshot.type"), isMaster ? ModEntry.I18n.Get("lookup.slingshot.type.master").ToString() : ModEntry.I18n.Get("lookup.slingshot.type.standard").ToString(), new Color(180, 100, 0)));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.slingshot.compatible-ammo"), ModEntry.I18n.Get("lookup.slingshot.compatible-ammo-desc").ToString(), new Color(0, 140, 0)));
                    subject.Sections.Add(section);
                }
                else if (item is Boots boots)
                {
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.equipment-stats"));
                    if (boots.defenseBonus.Value > 0)
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.defense"), $"+{boots.defenseBonus.Value}", new Color(20, 110, 220)));
                    if (boots.immunityBonus.Value > 0)
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.equipment.immunity"), $"+{boots.immunityBonus.Value}", new Color(0, 140, 0)));
                    subject.Sections.Add(section);
                }
                else if (item is Ring ring)
                {
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.ring-effects"));
                    string ringEffect = GetRingEffectDescription(ring.ItemId, ring.DisplayName);
                    if (!string.IsNullOrEmpty(ringEffect))
                    {
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.ring.special-effect"), ringEffect, new Color(180, 50, 180)));
                    }

                    if (ring is CombinedRing combined && combined.combinedRings.Count > 0)
                    {
                        var ringLinks = new List<LookupLink>();
                        foreach (var subRing in combined.combinedRings)
                        {
                            var rData = ItemRegistry.GetData(subRing.QualifiedItemId);
                            string subEffect = GetRingEffectDescription(subRing.ItemId, subRing.DisplayName);
                            string subText = !string.IsNullOrEmpty(subEffect) ? $"{subRing.DisplayName} ({subEffect})" : subRing.DisplayName;
                            ringLinks.Add(new LookupLink(subText, null, Game1.textColor, rData?.GetTexture(), rData?.GetSourceRect(), () => BuildItemSubject(subRing)));
                        }
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.ring.combined-rings"), ringLinks));
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
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.tool-details"));
                    string upgradeName = tool.UpgradeLevel switch
                    {
                        0 => ModEntry.I18n.Get("lookup.tool.level.basic").ToString(),
                        1 => ModEntry.I18n.Get("lookup.tool.level.copper").ToString(),
                        2 => ModEntry.I18n.Get("lookup.tool.level.steel").ToString(),
                        3 => ModEntry.I18n.Get("lookup.tool.level.gold").ToString(),
                        4 => ModEntry.I18n.Get("lookup.tool.level.iridium").ToString(),
                        _ => ModEntry.I18n.Get("lookup.tool.level.basic").ToString()
                    };
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tool.upgrade-level"), upgradeName, new Color(180, 100, 0)));

                    if (tool.enchantments.Count > 0)
                    {
                        foreach (var ench in tool.enchantments)
                        {
                            string eName = ench.GetName();
                            string eDesc = GetEnchantmentDescription(eName);
                            string val = !string.IsNullOrEmpty(eDesc) ? $"{eName} — {eDesc}" : eName;
                            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.enchantment"), val, new Color(180, 50, 180)));
                        }
                    }

                    if (tool is FishingRod rod)
                    {
                        var bait = rod.GetBait();
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tool.bait-attached"), bait != null ? $"{bait.DisplayName} (x{bait.Stack})" : ModEntry.I18n.Get("lookup.common.none").ToString(), bait != null ? new Color(0, 140, 0) : Color.DarkSlateGray));

                        var tackles = rod.GetTackle();
                        if (tackles != null && tackles.Count > 0)
                        {
                            var tackleNames = tackles.Where(t => t != null).Select(t => ModEntry.I18n.Get("lookup.tool.tackle-uses", new { name = t.DisplayName, uses = t.uses.Value, max = FishingRod.maxTackleUses }).ToString());
                            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tool.tackles"), string.Join(", ", tackleNames), new Color(20, 110, 220)));
                        }
                    }

                    subject.Sections.Add(section);
                }
            }
            catch { }
        }

        private static string GetEnchantmentDescription(string enchantmentName)
        {
            string e = enchantmentName.ToLower();
            if (e.Contains("crusader")) return ModEntry.I18n.Get("lookup.enchantment.crusader").ToString();
            if (e.Contains("vampiric")) return ModEntry.I18n.Get("lookup.enchantment.vampiric").ToString();
            if (e.Contains("haymaker")) return ModEntry.I18n.Get("lookup.enchantment.haymaker").ToString();
            if (e.Contains("artful")) return ModEntry.I18n.Get("lookup.enchantment.artful").ToString();
            if (e.Contains("bug killer")) return ModEntry.I18n.Get("lookup.enchantment.bug-killer").ToString();
            if (e.Contains("auto-hook")) return ModEntry.I18n.Get("lookup.enchantment.auto-hook").ToString();
            if (e.Contains("master")) return ModEntry.I18n.Get("lookup.enchantment.master").ToString();
            if (e.Contains("preserving")) return ModEntry.I18n.Get("lookup.enchantment.preserving").ToString();
            if (e.Contains("reaching")) return ModEntry.I18n.Get("lookup.enchantment.reaching").ToString();
            if (e.Contains("bottomless")) return ModEntry.I18n.Get("lookup.enchantment.bottomless").ToString();
            if (e.Contains("efficient")) return ModEntry.I18n.Get("lookup.enchantment.efficient").ToString();
            if (e.Contains("generous")) return ModEntry.I18n.Get("lookup.enchantment.generous").ToString();
            if (e.Contains("archaeologist")) return ModEntry.I18n.Get("lookup.enchantment.archaeologist").ToString();
            return string.Empty;
        }

        private static string GetRingEffectDescription(string ringId, string ringName)
        {
            string r = ringName.ToLower();
            if (r.Contains("glow")) return ModEntry.I18n.Get("lookup.ring.effect.glow").ToString();
            if (r.Contains("magnet")) return ModEntry.I18n.Get("lookup.ring.effect.magnet").ToString();
            if (r.Contains("iridium band")) return ModEntry.I18n.Get("lookup.ring.effect.iridium-band").ToString();
            if (r.Contains("burglar")) return ModEntry.I18n.Get("lookup.ring.effect.burglar").ToString();
            if (r.Contains("slime charmer")) return ModEntry.I18n.Get("lookup.ring.effect.slime-charmer").ToString();
            if (r.Contains("savage")) return ModEntry.I18n.Get("lookup.ring.effect.savage").ToString();
            if (r.Contains("vampire")) return ModEntry.I18n.Get("lookup.ring.effect.vampire").ToString();
            if (r.Contains("crabshell")) return ModEntry.I18n.Get("lookup.ring.effect.crabshell").ToString();
            if (r.Contains("napalm")) return ModEntry.I18n.Get("lookup.ring.effect.napalm").ToString();
            if (r.Contains("hot java")) return ModEntry.I18n.Get("lookup.ring.effect.hot-java").ToString();
            if (r.Contains("lucky")) return ModEntry.I18n.Get("lookup.ring.effect.lucky").ToString();
            if (r.Contains("phoenix")) return ModEntry.I18n.Get("lookup.ring.effect.phoenix").ToString();
            if (r.Contains("ruby")) return ModEntry.I18n.Get("lookup.ring.effect.ruby").ToString();
            if (r.Contains("aquamarine")) return ModEntry.I18n.Get("lookup.ring.effect.aquamarine").ToString();
            if (r.Contains("emerald")) return ModEntry.I18n.Get("lookup.ring.effect.emerald").ToString();
            if (r.Contains("jade")) return ModEntry.I18n.Get("lookup.ring.effect.jade").ToString();
            if (r.Contains("amethyst")) return ModEntry.I18n.Get("lookup.ring.effect.amethyst").ToString();
            if (r.Contains("topaz")) return ModEntry.I18n.Get("lookup.ring.effect.topaz").ToString();
            if (r.Contains("warrior")) return ModEntry.I18n.Get("lookup.ring.effect.warrior").ToString();
            if (r.Contains("yoba")) return ModEntry.I18n.Get("lookup.ring.effect.yoba").ToString();
            if (r.Contains("thorns")) return ModEntry.I18n.Get("lookup.ring.effect.thorns").ToString();
            if (r.Contains("immunity")) return ModEntry.I18n.Get("lookup.ring.effect.immunity").ToString();
            if (r.Contains("sturdy")) return ModEntry.I18n.Get("lookup.ring.effect.sturdy").ToString();
            return string.Empty;
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
                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.trinket-stats"));
                string cleanId = item.ItemId.Replace("(TR)", "").Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(cleanId))
                    cleanId = item.QualifiedItemId.Replace("(TR)", "").Trim().ToLowerInvariant();

                Trinket? trinket = item as Trinket;

                // 1. Current Stats / Active Roll
                string currentRollSummary = ModEntry.I18n.Get("lookup.type.item").ToString();
                string possibleRangeSummary = "";

                int seed = trinket?.generationSeed.Value ?? 0;
                Random r = Utility.CreateRandom(seed);

                if (cleanId.Contains("fairy"))
                {
                    possibleRangeSummary = ModEntry.I18n.Get("lookup.trinket.fairy-box.range").ToString();
                    if (trinket != null)
                    {
                        int num = 1;
                        if (r.NextBool(0.45)) num = 2;
                        else if (r.NextBool(0.25)) num = 3;
                        else if (r.NextBool(0.125)) num = 4;
                        else if (r.NextBool(0.0675)) num = 5;
                        float interval = (5000 - num * 300) / 1000f;
                        float power = 0.7f + num * 0.1f;
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.fairy-box.current", new { level = num, interval = $"{interval:0.0}", power = $"{power:0.0}" }).ToString();
                    }
                    else
                    {
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.fairy-box.desc").ToString();
                    }
                }
                else if (cleanId.Contains("quiver"))
                {
                    possibleRangeSummary = ModEntry.I18n.Get("lookup.trinket.quiver.range").ToString();
                    if (trinket != null)
                    {
                        int minDmg, maxDmg;
                        float delay;
                        string style = ModEntry.I18n.Get("lookup.trinket.variant.normal").ToString();

                        if (r.NextBool(0.04))
                        {
                            style = ModEntry.I18n.Get("lookup.trinket.variant.perfect").ToString();
                            minDmg = 30;
                            maxDmg = 35;
                            delay = 900f;
                        }
                        else if (r.NextBool(0.1))
                        {
                            if (r.NextBool(0.5))
                            {
                                style = ModEntry.I18n.Get("lookup.trinket.variant.rapid").ToString();
                                minDmg = r.Next(10, 15) - 2;
                                maxDmg = minDmg + 5;
                                delay = 600 + r.Next(11) * 10;
                            }
                            else
                            {
                                style = ModEntry.I18n.Get("lookup.trinket.variant.heavy").ToString();
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

                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.quiver.current", new { variant = style, cooldown = $"{delay / 1000f:0.00}", minDmg, maxDmg }).ToString();
                    }
                    else
                    {
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.quiver.desc").ToString();
                    }
                }
                else if (cleanId.Contains("ice") || cleanId.Contains("rod"))
                {
                    possibleRangeSummary = ModEntry.I18n.Get("lookup.trinket.ice-rod.range").ToString();
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
                        string perfectTag = isPerfect ? ModEntry.I18n.Get("lookup.trinket.ice-rod.perfect-tag").ToString() : "";
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.ice-rod.current", new { delay = $"{delay / 1000f:0.0}", freeze = $"{freeze / 1000f:0.0}", perfect = perfectTag }).ToString();
                    }
                    else
                    {
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.ice-rod.desc").ToString();
                    }
                }
                else if (cleanId.Contains("spur") || cleanId.Contains("golden") || cleanId.Contains("iridium"))
                {
                    possibleRangeSummary = ModEntry.I18n.Get("lookup.trinket.spur.range").ToString();
                    if (trinket != null)
                    {
                        int duration = r.Next(5, 11);
                        string maxTag = duration == 10 ? ModEntry.I18n.Get("lookup.trinket.spur.max-tag").ToString() : "";
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.spur.current", new { duration, maxTag }).ToString();
                    }
                    else
                    {
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.spur.desc").ToString();
                    }
                }
                else if (cleanId.Contains("parrot"))
                {
                    possibleRangeSummary = ModEntry.I18n.Get("lookup.trinket.parrot.range").ToString();
                    if (trinket != null)
                    {
                        int num = 1;
                        if (r.NextBool(0.4)) num = 2;
                        else if (r.NextBool(0.2)) num = 3;
                        else if (r.NextBool(0.1)) num = 4;
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.parrot.current", new { level = num, chance = num * 10 }).ToString();
                    }
                    else
                    {
                        currentRollSummary = ModEntry.I18n.Get("lookup.trinket.parrot.desc").ToString();
                    }
                }
                else if (cleanId.Contains("frog"))
                {
                    possibleRangeSummary = ModEntry.I18n.Get("lookup.trinket.frog.range").ToString();
                    string variant = ModEntry.I18n.Get("lookup.trinket.frog.green").ToString();
                    if (trinket != null)
                    {
                        int frogType = r.Next(0, 8);
                        string vName = frogType switch
                        {
                            0 => ModEntry.I18n.Get("lookup.trinket.frog.green").ToString(),
                            1 => ModEntry.I18n.Get("lookup.trinket.frog.yellow").ToString(),
                            2 => ModEntry.I18n.Get("lookup.trinket.frog.red").ToString(),
                            3 => ModEntry.I18n.Get("lookup.trinket.frog.blue").ToString(),
                            4 => ModEntry.I18n.Get("lookup.trinket.frog.void").ToString(),
                            5 => ModEntry.I18n.Get("lookup.trinket.frog.poison").ToString(),
                            6 or 7 => ModEntry.I18n.Get("lookup.trinket.frog.prismatic").ToString(),
                            _ => ModEntry.I18n.Get("lookup.trinket.frog.green").ToString()
                        };
                        variant = $"{vName}{ModEntry.I18n.Get("lookup.trinket.frog.swallows")}";
                    }
                    currentRollSummary = variant;
                }
                else if (cleanId.Contains("basilisk") || cleanId.Contains("paw"))
                {
                    possibleRangeSummary = ModEntry.I18n.Get("lookup.trinket.basilisk.range").ToString();
                    currentRollSummary = ModEntry.I18n.Get("lookup.trinket.basilisk.desc").ToString();
                }

                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.trinket.active-stats"), currentRollSummary, new Color(0, 140, 0)));
                if (!string.IsNullOrEmpty(possibleRangeSummary))
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.trinket.possible-ranges"), possibleRangeSummary, Color.DarkSlateGray));
                }

                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.trinket.anvil-reforging"), ModEntry.I18n.Get("lookup.trinket.reforge-desc").ToString(), new Color(180, 100, 0)));

                // 2. BetterForge Mod Integration & Prismatic Ascension
                bool isAscended = trinket != null && (trinket.modData.ContainsKey("feiluvnana.BetterForge/IsAscended") || trinket.modData.ContainsKey("feiluvnana.BetterTrinket/IsAscended"));
                bool hasBetterForge = ModEntry.ModHelper.ModRegistry.IsLoaded("feiluvnana.BetterForge");

                string ascensionPowerDesc = cleanId switch
                {
                    var s when s.Contains("frog") => ModEntry.I18n.Get("lookup.ascension.desc.frog").ToString(),
                    var s when s.Contains("fairy") => ModEntry.I18n.Get("lookup.ascension.desc.fairy").ToString(),
                    var s when s.Contains("parrot") => ModEntry.I18n.Get("lookup.ascension.desc.parrot").ToString(),
                    var s when s.Contains("spur") || s.Contains("golden") || s.Contains("iridium") => ModEntry.I18n.Get("lookup.ascension.desc.spur").ToString(),
                    var s when s.Contains("quiver") => ModEntry.I18n.Get("lookup.ascension.desc.quiver").ToString(),
                    var s when s.Contains("ice") || s.Contains("rod") => ModEntry.I18n.Get("lookup.ascension.desc.ice").ToString(),
                    var s when s.Contains("basilisk") || s.Contains("paw") => ModEntry.I18n.Get("lookup.ascension.desc.basilisk").ToString(),
                    _ => ModEntry.I18n.Get("lookup.ascension.desc.default").ToString()
                };

                if (isAscended)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.ascension.status-label"), ModEntry.I18n.Get("lookup.ascension.active-desc").ToString(), new Color(180, 50, 180)));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.ascension.luck-label"), ModEntry.I18n.Get("lookup.ascension.luck-desc").ToString(), new Color(0, 140, 0)));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.ascension.enhanced-power"), ascensionPowerDesc, new Color(180, 50, 180)));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.ascension.source-mod"), ModEntry.I18n.Get("lookup.ascension.source-desc").ToString(), Color.DarkSlateGray));
                }
                else
                {
                    string notice = hasBetterForge
                        ? ModEntry.I18n.Get("lookup.ascension.notice-forge").ToString()
                        : ModEntry.I18n.Get("lookup.ascension.notice-info").ToString();

                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.ascension.section-label"), notice, new Color(180, 100, 0)));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.ascension.power-label"), ModEntry.I18n.Get("lookup.ascension.power-format", new { desc = ascensionPowerDesc }).ToString(), Color.DarkSlateGray));
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

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.skill-books"));

                bool isRead = false;
                if (!string.IsNullOrEmpty(bookStatKey) && Game1.player.stats.Get(bookStatKey) > 0)
                    isRead = true;
                if (!string.IsNullOrEmpty(bookMailKey) && (Game1.player.mailReceived.Contains(bookMailKey) || Game1.player.hasOrWillReceiveMail(bookMailKey)))
                    isRead = true;

                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.book.reading-status"),
                    isRead ? ModEntry.I18n.Get("lookup.book.read-done").ToString() : ModEntry.I18n.Get("lookup.book.read-needed").ToString(),
                    isRead ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.book.power-granted"), powerDesc, new Color(180, 50, 180)));
                if (!string.IsNullOrEmpty(secondaryXpDesc))
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.book.secondary-readings"), secondaryXpDesc, Color.DarkSlateGray));
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
                        ModEntry.I18n.Get("lookup.section.items-shipped"),
                        hasShipped ? ModEntry.I18n.Get("lookup.collection.shipped-done", new { count = shipCount }).ToString() : ModEntry.I18n.Get("lookup.collection.shipped-needed").ToString(),
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
                        ModEntry.I18n.Get("lookup.section.fish-caught"),
                        caught && count > 0 ? ModEntry.I18n.Get("lookup.collection.fish-caught-done", new { count, size = maxSize }).ToString() : ModEntry.I18n.Get("lookup.collection.fish-caught-needed").ToString(),
                        caught && count > 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                }

                // 3. Recipes Cooked
                if (item.Category == StardewValley.Object.CookingCategory)
                {
                    bool cooked = Game1.player.recipesCooked.TryGetValue(item.ItemId, out int cookCount) && cookCount > 0;
                    fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.section.recipes-cooked"),
                        cooked ? ModEntry.I18n.Get("lookup.collection.cooked-done", new { count = cookCount }).ToString() : ModEntry.I18n.Get("lookup.collection.cooked-needed").ToString(),
                        cooked ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                }

                // 4. Crafting Recipes Crafted
                if (CraftingRecipe.craftingRecipes.ContainsKey(item.DisplayName) || CraftingRecipe.craftingRecipes.ContainsKey(item.Name))
                {
                    string recipeName = CraftingRecipe.craftingRecipes.ContainsKey(item.DisplayName) ? item.DisplayName : item.Name;
                    bool crafted = Game1.player.craftingRecipes.TryGetValue(recipeName, out int craftCount) && craftCount > 0;
                    fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.section.items-crafted"),
                        crafted ? ModEntry.I18n.Get("lookup.collection.crafted-done", new { count = craftCount }).ToString() : ModEntry.I18n.Get("lookup.collection.crafted-needed").ToString(),
                        crafted ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                }

                if (fields.Count > 0)
                {
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.collections-perfection"));
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

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.fishing-details"));

                // 1. Difficulty & Behavior (parts[1] = difficulty, parts[2] = behavior)
                string diff = parts[1];
                string behavior = parts.Length > 2 && !string.IsNullOrEmpty(parts[2]) ? parts[2] : "mixed";
                string behaviorKey = behavior.ToLowerInvariant();
                string behaviorName = ModEntry.I18n.Get($"lookup.fish.behavior.{behaviorKey}").ToString();
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish.difficulty"), ModEntry.I18n.Get("lookup.fish.difficulty-format", new { diff, behavior = behaviorName }).ToString(), new Color(200, 60, 20)));

                // 2. Spawn Seasons (parts[6] in Data/Fish)
                if (parts.Length > 6 && !string.IsNullOrWhiteSpace(parts[6]))
                {
                    var seasonList = parts[6].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var seasonNames = seasonList.Select(s => {
                        string key = $"season.{s.ToLower()}";
                        var tr = ModEntry.I18n.Get(key);
                        return tr.HasValue() ? tr.ToString() : (char.ToUpper(s[0]) + s.Substring(1));
                    });
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.forage.seasons"), string.Join(", ", seasonNames), new Color(46, 125, 50)));
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
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish.time-of-day"), string.Join(", ", timeRanges), new Color(180, 100, 0)));
                    }
                }

                // 4. Weather (parts[7] in Data/Fish)
                if (parts.Length > 7)
                {
                    string weather = parts[7].ToLower() switch
                    {
                        "sunny" => ModEntry.I18n.Get("lookup.weather.sunny").ToString(),
                        "rainy" => ModEntry.I18n.Get("lookup.weather.rainy").ToString(),
                        _ => ModEntry.I18n.Get("lookup.common.all-weather", "Any Weather").ToString()
                    };
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish.weather"), weather, new Color(20, 110, 220)));
                }

                // 5. Min Skill
                if (parts.Length > 9 && int.TryParse(parts[9], out int minSkill) && minSkill > 0)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish.min-skill"), ModEntry.I18n.Get("lookup.fish.min-skill-level", new { level = minSkill }).ToString(), Color.DarkSlateGray));
                }

                // 6. Spawn Locations (Extracted and mapped to friendly location names)
                var spawnLocations = GetFishSpawnLocations(item.ItemId);
                if (spawnLocations.Count > 0)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish.locations"), string.Join(", ", spawnLocations), new Color(20, 110, 220)));
                }

                // 7. 1.6 Targeted Bait Maker
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish.bait-maker"), ModEntry.I18n.Get("lookup.fish.bait-maker-yield", new { name = item.DisplayName }).ToString(), new Color(0, 140, 0)));

                // 8. Fish Pond Produce Highlights
                string fishName = item.Name.ToLowerInvariant();
                string pondHighlights = fishName switch
                {
                    var s when s.Contains("sturgeon") => ModEntry.I18n.Get("lookup.pond.sturgeon").ToString(),
                    var s when s.Contains("lava eel") => ModEntry.I18n.Get("lookup.pond.lava-eel").ToString(),
                    var s when s.Contains("blobfish") => ModEntry.I18n.Get("lookup.pond.blobfish").ToString(),
                    var s when s.Contains("rainbow trout") => ModEntry.I18n.Get("lookup.pond.rainbow-trout").ToString(),
                    var s when s.Contains("super cucumber") => ModEntry.I18n.Get("lookup.pond.super-cucumber").ToString(),
                    var s when s.Contains("midnight squid") || s.Contains("squid") => ModEntry.I18n.Get("lookup.pond.squid").ToString(),
                    var s when s.Contains("woodskip") => ModEntry.I18n.Get("lookup.pond.woodskip").ToString(),
                    var s when s.Contains("slimejack") => ModEntry.I18n.Get("lookup.pond.slimejack").ToString(),
                    var s when s.Contains("spook fish") => ModEntry.I18n.Get("lookup.pond.stonefish").ToString(),
                    var s when s.Contains("stingray") => ModEntry.I18n.Get("lookup.pond.stingray").ToString(),
                    var s when s.Contains("lionfish") => ModEntry.I18n.Get("lookup.pond.lionfish").ToString(),
                    var s when s.Contains("eel") => ModEntry.I18n.Get("lookup.pond.dorado").ToString(),
                    _ => ModEntry.I18n.Get("lookup.pond.regular-roe", new { fish = item.DisplayName }).ToString()
                };
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish.pond-produce"), pondHighlights, new Color(180, 50, 180)));

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
                if (fishEntry.FishAreaId == "Pond") return ModEntry.I18n.Get("lookup.location.forest-pond").ToString();
                if (fishEntry.FishAreaId == "River") return ModEntry.I18n.Get("lookup.location.forest-river").ToString();
                return ModEntry.I18n.Get("lookup.location.cindersap-forest").ToString();
            }
            if (locKey.Equals("Town", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.pelican-town-river").ToString();
            if (locKey.Equals("Mountain", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.mountain-lake").ToString();
            if (locKey.Equals("Beach", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.ocean-beach").ToString();
            if (locKey.Equals("Woods", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.secret-woods").ToString();
            if (locKey.Equals("Desert", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.calico-desert").ToString();
            if (locKey.Equals("UndergroundMine", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(fishEntry.FishAreaId))
                    return ModEntry.I18n.Get("lookup.location.mines-floor", new { floor = fishEntry.FishAreaId }).ToString();
                return ModEntry.I18n.Get("lookup.location.the-mines").ToString();
            }
            if (locKey.Equals("Sewer", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.the-sewers").ToString();
            if (locKey.Equals("BugLand", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.mutant-bug-lair").ToString();
            if (locKey.Equals("WitchSwamp", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.witchs-swamp").ToString();
            if (locKey.Equals("Submarine", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.night-market-submarine").ToString();
            if (locKey.Equals("IslandSouth", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.ginger-island-south").ToString();
            if (locKey.Equals("IslandWest", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.ginger-island-west").ToString();
            if (locKey.Equals("IslandNorth", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.ginger-island-river").ToString();
            if (locKey.Equals("IslandSouthEastCave", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.pirate-cove").ToString();
            if (locKey.Equals("Caldera", StringComparison.OrdinalIgnoreCase)) return ModEntry.I18n.Get("lookup.location.volcano-caldera").ToString();

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
                "Town" => ModEntry.I18n.Get("lookup.location.pelican-town-river").ToString(),
                "Forest" => ModEntry.I18n.Get("lookup.location.cindersap-forest").ToString(),
                "Mountain" => ModEntry.I18n.Get("lookup.location.the-mountain").ToString(),
                "BusStop" => ModEntry.I18n.Get("lookup.location.bus-stop").ToString(),
                "Railroad" => ModEntry.I18n.Get("lookup.location.railroad").ToString(),
                "Beach" => ModEntry.I18n.Get("lookup.location.the-beach").ToString(),
                "Woods" => ModEntry.I18n.Get("lookup.location.secret-woods").ToString(),
                "Desert" => ModEntry.I18n.Get("lookup.location.calico-desert").ToString(),
                "IslandWest" => ModEntry.I18n.Get("lookup.spawn.ginger-island-volcano").ToString(),
                "IslandSouth" => ModEntry.I18n.Get("lookup.location.ginger-island-south").ToString(),
                "IslandNorth" => ModEntry.I18n.Get("lookup.location.ginger-island-river").ToString(),
                "IslandSouthEast" => ModEntry.I18n.Get("lookup.location.pirate-cove").ToString(),
                "UndergroundMine" => ModEntry.I18n.Get("lookup.location.the-mines").ToString(),
                "Backwoods" => ModEntry.I18n.Get("lookup.location.backwoods").ToString(),
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

                string seasonSpring = ModEntry.I18n.Get("season.spring").ToString();
                string seasonSummer = ModEntry.I18n.Get("season.summer").ToString();
                string seasonFall = ModEntry.I18n.Get("season.fall").ToString();
                string seasonWinter = ModEntry.I18n.Get("season.winter").ToString();

                string locTown = ModEntry.I18n.Get("lookup.location.pelican-town-river").ToString();
                string locForest = ModEntry.I18n.Get("lookup.location.cindersap-forest").ToString();
                string locMountain = ModEntry.I18n.Get("lookup.location.the-mountain").ToString();
                string locBusStop = ModEntry.I18n.Get("lookup.location.bus-stop").ToString();
                string locIslandEast = ModEntry.I18n.Get("lookup.location.cindersap-island").ToString();
                string locSecretWoods = ModEntry.I18n.Get("lookup.location.secret-woods").ToString();
                string locFarmCave = ModEntry.I18n.Get("lookup.location.farm-cave-mushroom").ToString();
                string locPrehistoric = ModEntry.I18n.Get("lookup.location.prehistoric-skull-cavern").ToString();
                string locMines = ModEntry.I18n.Get("lookup.location.the-mines").ToString();
                string locMines81 = ModEntry.I18n.Get("lookup.location.the-mines-81").ToString();
                string locSkull = ModEntry.I18n.Get("lookup.location.skull-cavern").ToString();
                string locMinesBoxes = ModEntry.I18n.Get("lookup.location.the-mines-boxes").ToString();
                string locBeach = ModEntry.I18n.Get("lookup.location.the-beach").ToString();
                string locDesert = ModEntry.I18n.Get("lookup.location.calico-desert").ToString();
                string locGinger = ModEntry.I18n.Get("lookup.location.ginger-island-south").ToString();

                // Special / Manual seasonal mapping for standard wild forage
                switch (item.ItemId)
                {
                    case "16": // Wild Horseradish
                    case "18": // Daffodil
                    case "20": // Leek
                    case "22": // Dandelion
                        foundSeasons.Add(seasonSpring);
                        foundLocations.Add(locTown);
                        foundLocations.Add(locForest);
                        foundLocations.Add(locMountain);
                        foundLocations.Add(locBusStop);
                        break;
                    case "399": // Spring Onion
                        foundSeasons.Add(seasonSpring);
                        foundLocations.Add(locIslandEast);
                        break;
                    case "257": // Morel
                        foundSeasons.Add(seasonSpring);
                        foundLocations.Add(locSecretWoods);
                        foundLocations.Add(locFarmCave);
                        break;
                    case "396": // Spice Berry
                    case "398": // Grape
                    case "394": // Sweet Pea
                        foundSeasons.Add(seasonSummer);
                        foundLocations.Add(locTown);
                        foundLocations.Add(locForest);
                        foundLocations.Add(locMountain);
                        foundLocations.Add(locBusStop);
                        break;
                    case "259": // Fiddlehead Fern
                        foundSeasons.Add(seasonSummer);
                        foundLocations.Add(locSecretWoods);
                        foundLocations.Add(locPrehistoric);
                        break;
                    case "404": // Common Mushroom
                    case "406": // Wild Plum
                    case "408": // Hazelnut
                    case "410": // Blackberry
                        foundSeasons.Add(seasonFall);
                        foundLocations.Add(locTown);
                        foundLocations.Add(locForest);
                        foundLocations.Add(locMountain);
                        foundLocations.Add(locBusStop);
                        break;
                    case "281": // Chanterelle
                        foundSeasons.Add(seasonFall);
                        foundLocations.Add(locSecretWoods);
                        foundLocations.Add(locFarmCave);
                        break;
                    case "420": // Red Mushroom
                        foundSeasons.Add(seasonSummer);
                        foundSeasons.Add(seasonFall);
                        foundLocations.Add(locSecretWoods);
                        foundLocations.Add(locMines);
                        break;
                    case "422": // Purple Mushroom
                        foundLocations.Add(locMines81);
                        foundLocations.Add(locSkull);
                        break;
                    case "78": // Cave Carrot
                        foundLocations.Add(locMinesBoxes);
                        foundLocations.Add(locSkull);
                        break;
                    case "372": // Clam
                    case "393": // Coral
                    case "397": // Sea Urchin
                    case "152": // Seaweed
                        foundLocations.Add(locBeach);
                        break;
                    case "88": // Coconut
                    case "90": // Cactus Fruit
                        foundLocations.Add(locDesert);
                        break;
                    case "829": // Ginger
                    case "830": // Taro Root
                    case "832": // Pineapple
                    case "834": // Mango
                        foundLocations.Add(locGinger);
                        break;
                    case "412": // Winter Root
                    case "414": // Crystal Fruit
                    case "416": // Snow Yam
                    case "418": // Crocus
                    case "283": // Holly
                        foundSeasons.Add(seasonWinter);
                        foundLocations.Add(locTown);
                        foundLocations.Add(locForest);
                        foundLocations.Add(locMountain);
                        foundLocations.Add(locBusStop);
                        break;
                }

                if (foundLocations.Count > 0 || foundSeasons.Count > 0)
                {
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.wild-forage"));
                    if (foundSeasons.Count > 0)
                    {
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.forage.seasons"), string.Join(", ", foundSeasons), new Color(0, 140, 0)));
                    }
                    if (foundLocations.Count > 0)
                    {
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.forage.spawn-locations"), string.Join(", ", foundLocations), new Color(20, 110, 220)));
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
                        sources.Add(ModEntry.I18n.Get("lookup.mineral.source.artifact-spots").ToString());
                        sources.Add(ModEntry.I18n.Get("lookup.mineral.source.fishing-chests").ToString());
                        sources.Add(ModEntry.I18n.Get("lookup.mineral.source.artifact-troves").ToString());
                        if (item.ItemId == "107")
                        {
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.pepper-rex").ToString());
                        }
                    }
                    else if (obj.Type == "Minerals" || item.Category == StardewValley.Object.mineralsCategory)
                    {
                        if (item.ItemId == "74")
                        {
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.iridium-nodes").ToString());
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.omni-geodes").ToString());
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.monster-drops").ToString());
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.rainbow-trout-pond").ToString());
                        }
                        else if (item.ItemId == "72")
                        {
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.diamond-nodes").ToString());
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.fishing-chests").ToString());
                        }
                        else if (item.ItemId == "60" || item.ItemId == "62" || item.ItemId == "64" || item.ItemId == "66" || item.ItemId == "68" || item.ItemId == "70")
                        {
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.gem-nodes").ToString());
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.fishing-chests").ToString());
                        }
                        else
                        {
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.mining-mines").ToString());
                            sources.Add(ModEntry.I18n.Get("lookup.mineral.source.cracking-geodes").ToString());
                        }
                    }

                    if (sources.Count > 0)
                    {
                        var section = new LookupSection(ModEntry.I18n.Get("lookup.section.gathering-sources"));
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mineral.sources"), string.Join(" | ", sources), new Color(20, 110, 220)));
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
                        var section = new LookupSection(ModEntry.I18n.Get("lookup.section.crop-info"));

                        int totalDays = cropData.DaysInPhase != null ? cropData.DaysInPhase.Sum() : 0;
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.growth-time"), ModEntry.I18n.Get("lookup.crop.growth-days", new { days = totalDays }).ToString(), new Color(0, 140, 0)));

                        if (cropData.RegrowDays > 0)
                        {
                            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.regrowth"), ModEntry.I18n.Get("lookup.crop.regrowth-days", new { days = cropData.RegrowDays }).ToString(), new Color(180, 100, 0)));
                        }

                        if (cropData.Seasons != null && cropData.Seasons.Count > 0)
                        {
                            string seasons = string.Join(", ", cropData.Seasons.Select(s => {
                                string key = $"season.{s.ToString().ToLower()}";
                                var tr = ModEntry.I18n.Get(key);
                                return tr.HasValue() ? tr.ToString() : s.ToString();
                            }));
                            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.harvest-seasons"), seasons, new Color(46, 125, 50)));
                        }

                        if (cropData.IsRaised)
                        {
                            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.trellis"), ModEntry.I18n.Get("lookup.crop.trellis-yes").ToString(), new Color(200, 60, 20)));
                        }

                        if (cropData.ExtraHarvestChance > 0)
                        {
                            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.extra-harvest"), $"{cropData.ExtraHarvestChance * 100:0.#}%", Color.DarkSlateGray));
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
                    text: ModEntry.I18n.Get("lookup.artisan.wine", new { name = item.DisplayName, price = winePrice }).ToString(),
                    subtitle: ModEntry.I18n.Get("lookup.artisan.keg-time-wine").ToString(),
                    textColor: new Color(180, 50, 180),
                    icon: wineData?.GetTexture(),
                    iconSourceRect: wineData?.GetSourceRect()
                ));

                int jellyPrice = basePrice * 2 + 50;
                var jellyData = ItemRegistry.GetData("(O)444");
                artisanLinks.Add(new LookupLink(
                    text: ModEntry.I18n.Get("lookup.artisan.jelly", new { name = item.DisplayName, price = jellyPrice }).ToString(),
                    subtitle: ModEntry.I18n.Get("lookup.artisan.jar-time-jelly").ToString(),
                    textColor: new Color(200, 60, 20),
                    icon: jellyData?.GetTexture(),
                    iconSourceRect: jellyData?.GetSourceRect()
                ));

                int driedPrice = (int)(basePrice * 7.5) + 25;
                var driedData = ItemRegistry.GetData("(O)DriedFruit");
                if (driedData != null)
                {
                    artisanLinks.Add(new LookupLink(
                        text: ModEntry.I18n.Get("lookup.artisan.dried", new { name = item.DisplayName, price = driedPrice }).ToString(),
                        subtitle: ModEntry.I18n.Get("lookup.artisan.dehydrator-time").ToString(),
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
                    text: ModEntry.I18n.Get("lookup.artisan.juice", new { name = item.DisplayName, price = juicePrice }).ToString(),
                    subtitle: ModEntry.I18n.Get("lookup.artisan.keg-time-juice").ToString(),
                    textColor: new Color(0, 140, 0),
                    icon: juiceData?.GetTexture(),
                    iconSourceRect: juiceData?.GetSourceRect()
                ));

                int picklePrice = basePrice * 2 + 50;
                var pickleData = ItemRegistry.GetData("(O)342");
                artisanLinks.Add(new LookupLink(
                    text: ModEntry.I18n.Get("lookup.artisan.pickled", new { name = item.DisplayName, price = picklePrice }).ToString(),
                    subtitle: ModEntry.I18n.Get("lookup.artisan.jar-time-jelly").ToString(),
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
                    text: ModEntry.I18n.Get("lookup.artisan.dried", new { name = item.DisplayName, price = driedPrice }).ToString(),
                    subtitle: ModEntry.I18n.Get("lookup.artisan.dehydrator-time").ToString(),
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
                        text: ModEntry.I18n.Get("lookup.artisan.smoked", new { name = item.DisplayName, price = smokedPrice, artisanPrice = (int)(smokedPrice * 1.4) }).ToString(),
                        subtitle: ModEntry.I18n.Get("lookup.artisan.smoker-time").ToString(),
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
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.coffee").ToString(), ModEntry.I18n.Get("lookup.artisan.keg-time-coffee").ToString(), new Color(110, 40, 10), coffee?.GetTexture(), coffee?.GetSourceRect()));
            }
            else if (id == "815" || name == "tea leaves")
            {
                var greenTea = ItemRegistry.GetData("(O)614");
                var pickle = ItemRegistry.GetData("(O)342");
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.green-tea").ToString(), ModEntry.I18n.Get("lookup.artisan.keg-time-tea").ToString(), new Color(46, 125, 50), greenTea?.GetTexture(), greenTea?.GetSourceRect()));
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.pickled-tea").ToString(), ModEntry.I18n.Get("lookup.artisan.jar-time-jelly").ToString(), new Color(180, 100, 0), pickle?.GetTexture(), pickle?.GetSourceRect()));
            }
            else if (id == "304" || name == "hops")
            {
                var paleAle = ItemRegistry.GetData("(O)303");
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.pale-ale").ToString(), ModEntry.I18n.Get("lookup.artisan.keg-cask-ale").ToString(), new Color(180, 100, 0), paleAle?.GetTexture(), paleAle?.GetSourceRect()));
            }
            else if (id == "262" || name == "wheat")
            {
                var beer = ItemRegistry.GetData("(O)346");
                var flour = ItemRegistry.GetData("(O)246");
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.beer").ToString(), ModEntry.I18n.Get("lookup.artisan.keg-cask-ale").ToString(), new Color(180, 100, 0), beer?.GetTexture(), beer?.GetSourceRect()));
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.wheat-flour").ToString(), ModEntry.I18n.Get("lookup.artisan.mill-overnight").ToString(), Game1.textColor, flour?.GetTexture(), flour?.GetSourceRect()));
            }
            else if (id == "340" || name == "honey")
            {
                var mead = ItemRegistry.GetData("(O)459");
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.mead").ToString(), ModEntry.I18n.Get("lookup.artisan.keg-cask-mead").ToString(), new Color(180, 100, 0), mead?.GetTexture(), mead?.GetSourceRect()));
            }
            else if (id == "270" || id == "421" || id == "431" || name.Contains("sunflower") || name == "corn")
            {
                var oil = ItemRegistry.GetData("(O)247");
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.cooking-oil").ToString(), ModEntry.I18n.Get("lookup.artisan.oil-maker-source").ToString(), new Color(180, 100, 0), oil?.GetTexture(), oil?.GetSourceRect()));
            }
            else if (id == "271" || name == "unmilled rice")
            {
                var rice = ItemRegistry.GetData("(O)423");
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.milled-rice").ToString(), ModEntry.I18n.Get("lookup.artisan.mill-overnight").ToString(), Game1.textColor, rice?.GetTexture(), rice?.GetSourceRect()));
            }
            else if (id == "284" || name == "beet")
            {
                var sugar = ItemRegistry.GetData("(O)245");
                artisanLinks.Add(new LookupLink(ModEntry.I18n.Get("lookup.artisan.sugar-yield").ToString(), ModEntry.I18n.Get("lookup.artisan.mill-overnight").ToString(), Game1.textColor, sugar?.GetTexture(), sugar?.GetSourceRect()));
            }

            // Seed Maker (Crops & Fruits)
            if (item.Category == StardewValley.Object.FruitsCategory || item.Category == StardewValley.Object.VegetableCategory)
            {
                artisanLinks.Add(new LookupLink(
                    text: ModEntry.I18n.Get("lookup.artisan.seeds-yield").ToString(),
                    subtitle: ModEntry.I18n.Get("lookup.artisan.seed-maker-time").ToString(),
                    textColor: new Color(46, 125, 50)
                ));
            }

            // Cask Aging (Wine, Cheese, Pale Ale, Beer, Mead)
            if (name.Contains("wine") || name.Contains("cheese") || name.Contains("pale ale") || name.Contains("beer") || name.Contains("mead"))
            {
                int iridiumVal = basePrice * 2;
                int days = name.Contains("wine") ? 56 : (name.Contains("cheese") ? 14 : 34);
                artisanLinks.Add(new LookupLink(
                    text: $"{ModEntry.I18n.Get("hover.quality.iridium")} ({iridiumVal}g)",
                    subtitle: ModEntry.I18n.Get("lookup.building.cask-aging", new { days }).ToString(),
                    textColor: new Color(180, 50, 180)
                ));
            }

            if (artisanLinks.Count > 0)
            {
                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.artisan-products"));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.artisan.products"), artisanLinks));
                subject.Sections.Add(section);
            }
        }

        private static void AddMachineItemSection(LookupSubject subject, Item item)
        {
            try
            {
                string id = item.ItemId.ToLowerInvariant();
                string name = item.Name.ToLowerInvariant();
                string func = "";

                if (name == "furnace" || id == "13") func = ModEntry.I18n.Get("lookup.machine.furnace").ToString();
                else if (name.Contains("heavy furnace") || id == "heavyfurnace" || id == "278") func = ModEntry.I18n.Get("lookup.machine.heavy-furnace").ToString();
                else if (name.Contains("charcoal kiln") || id == "114") func = ModEntry.I18n.Get("lookup.machine.charcoal-kiln").ToString();
                else if (name == "crystalarium" || id == "21") func = ModEntry.I18n.Get("lookup.machine.crystalarium").ToString();
                else if (name == "seed maker" || id == "25") func = ModEntry.I18n.Get("lookup.machine.seed-maker").ToString();
                else if (name == "cheese press" || id == "16") func = ModEntry.I18n.Get("lookup.machine.cheese-press").ToString();
                else if (name == "mayonnaise machine" || id == "24") func = ModEntry.I18n.Get("lookup.machine.mayo-machine").ToString();
                else if (name == "oil maker" || id == "19") func = ModEntry.I18n.Get("lookup.machine.oil-maker").ToString();
                else if (name == "loom" || id == "17") func = ModEntry.I18n.Get("lookup.machine.loom").ToString();
                else if (name == "keg" || id == "12") func = ModEntry.I18n.Get("lookup.machine.keg").ToString();
                else if (name == "preserves jar" || id == "15") func = ModEntry.I18n.Get("lookup.machine.preserves-jar").ToString();
                else if (name == "cask" || id == "163") func = ModEntry.I18n.Get("lookup.machine.cask").ToString();
                else if (name.Contains("dehydrator")) func = ModEntry.I18n.Get("lookup.machine.dehydrator").ToString();
                else if (name.Contains("fish smoker") || name.Contains("smoker")) func = ModEntry.I18n.Get("lookup.machine.fish-smoker").ToString();
                else if (name.Contains("bait maker")) func = ModEntry.I18n.Get("lookup.machine.bait-maker").ToString();
                else if (name.Contains("deluxe worm bin")) func = ModEntry.I18n.Get("lookup.machine.deluxe-worm-bin").ToString();
                else if (name.Contains("worm bin") || id == "154") func = ModEntry.I18n.Get("lookup.machine.worm-bin").ToString();
                else if (name == "bone mill" || id == "90") func = ModEntry.I18n.Get("lookup.machine.bone-mill").ToString();
                else if (name == "geode crusher" || id == "182") func = ModEntry.I18n.Get("lookup.machine.geode-crusher").ToString();
                else if (name == "solar panel" || id == "231") func = ModEntry.I18n.Get("lookup.machine.solar-panel").ToString();
                else if (name == "mini-forge" || id == "230") func = ModEntry.I18n.Get("lookup.machine.mini-forge").ToString();
                else if (name == "anvil") func = ModEntry.I18n.Get("lookup.machine.anvil").ToString();
                else if (name == "auto-grabber" || id == "165") func = ModEntry.I18n.Get("lookup.machine.auto-grabber").ToString();
                else if (name == "auto-petter" || id == "272") func = ModEntry.I18n.Get("lookup.machine.auto-petter").ToString();
                else if (name.Contains("statue of perfection")) func = ModEntry.I18n.Get("lookup.machine.statue-perfection").ToString();
                else if (name.Contains("statue of true perfection")) func = ModEntry.I18n.Get("lookup.machine.statue-true-perfection").ToString();
                else if (name.Contains("statue of blessings")) func = ModEntry.I18n.Get("lookup.machine.statue-blessings").ToString();
                else if (name.Contains("statue of the dwarf king")) func = ModEntry.I18n.Get("lookup.machine.statue-dwarf-king").ToString();
                else return;

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.machine-info"));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.machine.processing"), func, new Color(0, 140, 0)));
                subject.Sections.Add(section);
            }
            catch { }
        }

        private static void AddFruitTreeSaplingSection(LookupSubject subject, Item item)
        {
            try
            {
                string name = item.Name.ToLowerInvariant();
                if (!name.Contains("sapling") && !name.Contains("tree"))
                    return;

                string season = "";
                if (name.Contains("cherry")) { season = ModEntry.I18n.Get("season.spring").ToString(); }
                else if (name.Contains("apricot")) { season = ModEntry.I18n.Get("season.spring").ToString(); }
                else if (name.Contains("orange")) { season = ModEntry.I18n.Get("season.summer").ToString(); }
                else if (name.Contains("peach")) { season = ModEntry.I18n.Get("season.summer").ToString(); }
                else if (name.Contains("banana")) { season = ModEntry.I18n.Get("lookup.fruit-tree.summer-greenhouse").ToString(); }
                else if (name.Contains("mango")) { season = ModEntry.I18n.Get("lookup.fruit-tree.summer-greenhouse").ToString(); }
                else if (name.Contains("apple")) { season = ModEntry.I18n.Get("season.fall").ToString(); }
                else if (name.Contains("pomegranate")) { season = ModEntry.I18n.Get("season.fall").ToString(); }
                else if (name.Contains("mystic")) { season = ModEntry.I18n.Get("lookup.fruit-tree.all-seasons-mystic").ToString(); }
                else return;

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.sapling-info"));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.sapling.harvest-season"), season, new Color(46, 125, 50)));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.sapling.maturation-time"), ModEntry.I18n.Get("lookup.sapling.maturation-desc").ToString(), new Color(180, 100, 0)));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.sapling.spacing"), ModEntry.I18n.Get("lookup.sapling.spacing-desc").ToString(), new Color(200, 60, 20)));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.sapling.quality-aging"), ModEntry.I18n.Get("lookup.sapling.quality-desc").ToString(), new Color(180, 50, 180)));
                subject.Sections.Add(section);
            }
            catch { }
        }

        private static void AddSpecialItemLoreSection(LookupSubject subject, Item item)
        {
            try
            {
                string id = item.ItemId.ToLowerInvariant();
                string name = item.Name.ToLowerInvariant();

                string desc = "";
                if (name == "stardrop tea" || id == "stardroptea") desc = ModEntry.I18n.Get("lookup.lore.stardrop-tea").ToString();
                else if (name == "prize ticket" || id == "prizeticket") desc = ModEntry.I18n.Get("lookup.lore.prize-ticket").ToString();
                else if (name == "calico egg" || id == "calicoegg") desc = ModEntry.I18n.Get("lookup.lore.calico-egg").ToString();
                else if (name == "golden walnut" || id == "73") desc = ModEntry.I18n.Get("lookup.lore.golden-walnut").ToString();
                else if (name == "qi gem" || id == "858") desc = ModEntry.I18n.Get("lookup.lore.qi-gem").ToString();
                else if (name == "cinder shard" || id == "848") desc = ModEntry.I18n.Get("lookup.lore.cinder-shard").ToString();
                else if (name == "magic rock candy" || id == "279") desc = ModEntry.I18n.Get("lookup.lore.magic-rock-candy").ToString();
                else if (name == "tent kit" || id == "tentkit") desc = ModEntry.I18n.Get("lookup.lore.tent-kit").ToString();
                else if (name == "sonar bobber" || id == "sonarbobber") desc = ModEntry.I18n.Get("lookup.lore.sonar-bobber").ToString();
                else if (name == "challenge bait" || id == "challengebait") desc = ModEntry.I18n.Get("lookup.lore.challenge-bait").ToString();
                else if (name == "deluxe bait" || id == "deluxebait") desc = ModEntry.I18n.Get("lookup.lore.deluxe-bait").ToString();
                else if (name.Contains("faraway") || id == "farawaystone") desc = ModEntry.I18n.Get("lookup.lore.far-away-stone").ToString();
                else if (name.Contains("crab pot") || id == "710" || id == "(o)710") desc = ModEntry.I18n.Get("lookup.lore.crab-pot").ToString();
                else return;

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.special-item"));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.special-item.function-lore"), desc, new Color(180, 50, 180)));
                subject.Sections.Add(section);
            }
            catch { }
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
                                        subtitle: ModEntry.I18n.Get("lookup.tailoring.sewing-product").ToString(),
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
                    if (tags.Contains("color_red")) dyeColor = "Red";
                    else if (tags.Contains("color_orange")) dyeColor = "Orange";
                    else if (tags.Contains("color_yellow")) dyeColor = "Yellow";
                    else if (tags.Contains("color_green")) dyeColor = "Green";
                    else if (tags.Contains("color_blue") || tags.Contains("color_cyan") || tags.Contains("color_ocean_blue")) dyeColor = "Blue";
                    else if (tags.Contains("color_purple")) dyeColor = "Purple";
                    else if (tags.Contains("color_pink")) dyeColor = "Pink";
                    else if (tags.Contains("color_gray")) dyeColor = "Gray";
                    else if (tags.Contains("color_brown")) dyeColor = "Brown";
                    else if (tags.Contains("color_black")) dyeColor = "Black";

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
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), ModEntry.I18n.Get("lookup.animal-processing.dino-egg").ToString(), new Color(46, 125, 50)));
                        var mayo = ItemRegistry.GetData("(O)807");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.dino-mayo").ToString(), ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time").ToString(), new Color(46, 125, 50), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("ostrich"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), ModEntry.I18n.Get("lookup.animal-processing.ostrich-egg").ToString(), new Color(46, 125, 50)));
                        var mayo = ItemRegistry.GetData("(O)306");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.ostrich-mayo").ToString(), ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time").ToString(), new Color(180, 100, 0), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("void"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), ModEntry.I18n.Get("lookup.animal-processing.void-egg").ToString(), new Color(180, 50, 180)));
                        var mayo = ItemRegistry.GetData("(O)308");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.void-mayo").ToString(), ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time").ToString(), new Color(180, 50, 180), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("duck"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), ModEntry.I18n.Get("lookup.animal-processing.duck-egg").ToString(), new Color(20, 110, 220)));
                        var mayo = ItemRegistry.GetData("(O)307");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.duck-mayo").ToString(), ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time").ToString(), new Color(20, 110, 220), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("golden"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), ModEntry.I18n.Get("lookup.animal-processing.golden-egg").ToString(), new Color(180, 100, 0)));
                        var mayo = ItemRegistry.GetData("(O)306");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.gold-mayo-3x").ToString(), ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time").ToString(), new Color(180, 100, 0), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else if (name.Contains("large"))
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), ModEntry.I18n.Get("lookup.animal-processing.chicken-egg").ToString(), new Color(0, 140, 0)));
                        var mayo = ItemRegistry.GetData("(O)306");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.gold-mayo").ToString(), ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time").ToString(), new Color(180, 100, 0), mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                    else
                    {
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.incubation"), ModEntry.I18n.Get("lookup.animal-processing.chicken-egg").ToString(), new Color(0, 140, 0)));
                        var mayo = ItemRegistry.GetData("(O)306");
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.mayo"), new List<LookupLink> {
                            new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.normal-mayo").ToString(), ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time").ToString(), Game1.textColor, mayo?.GetTexture(), mayo?.GetSourceRect())
                        }));
                    }
                }

                // Milk & Cheese
                if (item.Category == StardewValley.Object.MilkCategory || name.Contains("milk"))
                {
                    if (name.Contains("goat"))
                    {
                        var cheese = ItemRegistry.GetData("(O)426");
                        string quality = name.Contains("large") ? ModEntry.I18n.Get("lookup.animal-prod.gold-goat-cheese").ToString() : ModEntry.I18n.Get("lookup.animal-prod.regular-goat-cheese").ToString();
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.cheese"), new List<LookupLink> {
                            new LookupLink(quality, ModEntry.I18n.Get("lookup.animal-prod.cheese-press-time").ToString(), new Color(180, 100, 0), cheese?.GetTexture(), cheese?.GetSourceRect())
                        }));
                    }
                    else
                    {
                        var cheese = ItemRegistry.GetData("(O)424");
                        string quality = name.Contains("large") ? ModEntry.I18n.Get("lookup.animal-prod.gold-cheese").ToString() : ModEntry.I18n.Get("lookup.animal-prod.regular-cheese").ToString();
                        fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.cheese"), new List<LookupLink> {
                            new LookupLink(quality, ModEntry.I18n.Get("lookup.animal-prod.cheese-press-time").ToString(), new Color(180, 100, 0), cheese?.GetTexture(), cheese?.GetSourceRect())
                        }));
                    }
                }

                // Wool -> Cloth
                if (name.Contains("wool") || id == "440" || id == "(o)440")
                {
                    var cloth = ItemRegistry.GetData("(O)428");
                    fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.loom"), new List<LookupLink> {
                        new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.cloth").ToString(), ModEntry.I18n.Get("lookup.animal-prod.loom-time").ToString(), new Color(180, 50, 180), cloth?.GetTexture(), cloth?.GetSourceRect())
                    }));
                }

                // Truffle -> Truffle Oil
                if (name.Contains("truffle") && !name.Contains("oil"))
                {
                    var oil = ItemRegistry.GetData("(O)432");
                    fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal-processing.oil"), new List<LookupLink> {
                        new LookupLink(ModEntry.I18n.Get("lookup.animal-prod.truffle-oil").ToString(), ModEntry.I18n.Get("lookup.animal-prod.oil-maker-time").ToString(), new Color(180, 100, 0), oil?.GetTexture(), oil?.GetSourceRect())
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
                    yieldDesc = ModEntry.I18n.Get("lookup.recycling.trash").ToString();
                }
                else if (id == "169" || name == "driftwood")
                {
                    yieldDesc = ModEntry.I18n.Get("lookup.recycling.driftwood").ToString();
                }
                else if (id == "170" || id == "broken glasses" || name.Contains("broken glasses"))
                {
                    yieldDesc = ModEntry.I18n.Get("lookup.recycling.refined-quartz").ToString();
                }
                else if (id == "171" || id == "broken cd" || name.Contains("broken cd"))
                {
                    yieldDesc = ModEntry.I18n.Get("lookup.recycling.refined-quartz").ToString();
                }
                else if (id == "172" || name == "soggy newspaper")
                {
                    yieldDesc = ModEntry.I18n.Get("lookup.recycling.newspaper").ToString();
                }
                else if (id == "rotten plant" || name.Contains("rotten plant"))
                {
                    yieldDesc = ModEntry.I18n.Get("lookup.recycling.rotten-plant").ToString();
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
                    crackInfo = ModEntry.I18n.Get("lookup.geode.crack.clint-or-crusher").ToString();
                    contentsInfo = ModEntry.I18n.Get("lookup.geode.drops.regular").ToString();
                }
                else if (id == "536" || name == "frozen geode")
                {
                    crackInfo = ModEntry.I18n.Get("lookup.geode.crack.clint-or-crusher").ToString();
                    contentsInfo = ModEntry.I18n.Get("lookup.geode.drops.frozen").ToString();
                }
                else if (id == "537" || name == "magma geode")
                {
                    crackInfo = ModEntry.I18n.Get("lookup.geode.crack.clint-or-crusher").ToString();
                    contentsInfo = ModEntry.I18n.Get("lookup.geode.drops.magma").ToString();
                }
                else if (id == "749" || name == "omni geode")
                {
                    crackInfo = ModEntry.I18n.Get("lookup.geode.crack.omni").ToString();
                    contentsInfo = ModEntry.I18n.Get("lookup.geode.drops.omni").ToString();
                }
                else if (id == "275" || name == "artifact trove")
                {
                    crackInfo = ModEntry.I18n.Get("lookup.geode.crack.trove").ToString();
                    contentsInfo = ModEntry.I18n.Get("lookup.geode.drops.trove").ToString();
                }
                else if (id.Contains("mysterybox") || name.Contains("mystery box"))
                {
                    crackInfo = ModEntry.I18n.Get("lookup.geode.crack.mystery-box").ToString();
                    contentsInfo = ModEntry.I18n.Get("lookup.geode.drops.mystery-box").ToString();
                }
                else
                {
                    return;
                }

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.geode-info"));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.geode.cracking-method"), crackInfo, new Color(180, 100, 0)));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.geode.potential-drops"), contentsInfo, new Color(0, 140, 0)));
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
                    effect = ModEntry.I18n.Get("lookup.fertilizer.basic").ToString();
                else if (id == "369" || name == "quality fertilizer")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.quality").ToString();
                else if (id == "919" || name == "deluxe fertilizer")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.deluxe").ToString();
                else if (id == "465" || name == "speed-gro")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.speed-gro").ToString();
                else if (id == "466" || name == "deluxe speed-gro")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.deluxe-speed-gro").ToString();
                else if (id == "918" || name == "hyper speed-gro")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.hyper-speed-gro").ToString();
                else if (id == "370" || name == "basic retaining soil")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.basic-retaining").ToString();
                else if (id == "371" || name == "quality retaining soil")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.quality-retaining").ToString();
                else if (id == "920" || name == "deluxe retaining soil")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.deluxe-retaining").ToString();
                else if (id == "805" || name == "tree fertilizer")
                    effect = ModEntry.I18n.Get("lookup.fertilizer.tree").ToString();
                else
                    return;

                var section = new LookupSection(ModEntry.I18n.Get("lookup.section.fertilizer-info"));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fertilizer.soil-effect"), effect, new Color(46, 125, 50)));
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
                            if (attrs.FarmingLevel > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.farming", new { level = attrs.FarmingLevel }).ToString());
                            if (attrs.MiningLevel > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.mining", new { level = attrs.MiningLevel }).ToString());
                            if (attrs.FishingLevel > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.fishing", new { level = attrs.FishingLevel }).ToString());
                            if (attrs.ForagingLevel > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.foraging", new { level = attrs.ForagingLevel }).ToString());
                            if (attrs.CombatLevel > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.combat", new { level = attrs.CombatLevel }).ToString());
                            if (attrs.LuckLevel > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.luck", new { level = attrs.LuckLevel }).ToString());
                            if (attrs.Speed > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.speed", new { level = attrs.Speed }).ToString());
                            if (attrs.Defense > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.defense", new { level = attrs.Defense }).ToString());
                            if (attrs.Attack > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.attack", new { level = attrs.Attack }).ToString());
                            if (attrs.MaxStamina > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.max-energy", new { level = attrs.MaxStamina }).ToString());
                            if (attrs.MagneticRadius > 0) buffs.Add(ModEntry.I18n.Get("lookup.buff.magnetism", new { level = attrs.MagneticRadius }).ToString());
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
            string mName = monster.displayName ?? monster.Name;
            var subject = new LookupSubject
            {
                Title = mName,
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

            statsSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.monster.speed"),
                monster.Speed.ToString(),
                monster.Speed >= 4 ? new Color(200, 60, 20) : Game1.textColor
            ));

            if (monster.missChance.Value > 0)
            {
                statsSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.monster.miss-chance"),
                    $"{monster.missChance.Value * 100:0.#}%",
                    new Color(180, 100, 0)
                ));
            }

            int xp = monster.ExperienceGained;
            statsSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.monster.experience"),
                xp.ToString(),
                new Color(180, 100, 0)
            ));

            // Monster Slayer Goal
            var (category, kills, goal, completed) = GetMonsterSlayerProgress(mName);
            if (!string.IsNullOrEmpty(category) && goal > 0)
            {
                string localizedCat = GetLocalizedMonsterCategory(category);
                string goalText = completed
                    ? ModEntry.I18n.Get("lookup.monster.slayer-completed", new { kills, goal, category = localizedCat }).ToString()
                    : ModEntry.I18n.Get("lookup.monster.slayer-remaining", new { kills, goal, remaining = goal - kills, category = localizedCat }).ToString();
                statsSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.monster.slayer-goal"),
                    goalText,
                    completed ? new Color(0, 140, 0) : new Color(180, 50, 180)
                ));
            }

            // Spawn locations
            string spawnLocs = GetMonsterSpawnLocations(mName);
            if (!string.IsNullOrEmpty(spawnLocs))
            {
                statsSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.monster.spawn-locations"),
                    spawnLocs,
                    new Color(20, 110, 220)
                ));
            }

            subject.Sections.Add(statsSection);

            // Drops with drop probabilities
            var dropsSection = new LookupSection(ModEntry.I18n.Get("lookup.section.drops"));
            var dropLinks = GetMonsterDropLinks(monster);

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

        private static string GetLocalizedMonsterCategory(string category)
        {
            return category switch
            {
                "Slimes" => ModEntry.I18n.Get("lookup.monster.category.slimes").ToString(),
                "Void Spirits" => ModEntry.I18n.Get("lookup.monster.category.void-spirits").ToString(),
                "Bats" => ModEntry.I18n.Get("lookup.monster.category.bats").ToString(),
                "Skeletons" => ModEntry.I18n.Get("lookup.monster.category.skeletons").ToString(),
                "Cave Insects" => ModEntry.I18n.Get("lookup.monster.category.cave-insects").ToString(),
                "Duggies" => ModEntry.I18n.Get("lookup.monster.category.duggies").ToString(),
                "Dust Sprites" => ModEntry.I18n.Get("lookup.monster.category.dust-sprites").ToString(),
                "Rock Crabs" => ModEntry.I18n.Get("lookup.monster.category.rock-crabs").ToString(),
                "Mummies" => ModEntry.I18n.Get("lookup.monster.category.mummies").ToString(),
                "Pepper Rex" => ModEntry.I18n.Get("lookup.monster.category.pepper-rex").ToString(),
                "Serpents" => ModEntry.I18n.Get("lookup.monster.category.serpents").ToString(),
                "Magma Sprites" => ModEntry.I18n.Get("lookup.monster.category.magma-sprites").ToString(),
                _ => category
            };
        }

        private static (string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) GetMonsterSlayerProgress(string monsterName)
        {
            try
            {
                string m = monsterName.ToLower();
                if (m.Contains("magma sprite") || m.Contains("magma sparker") || m.Contains("sparker"))
                {
                    int kills = Game1.stats.getMonstersKilled("Magma Sprite")
                              + Game1.stats.getMonstersKilled("Magma Sparker");
                    return ("Magma Sprites", kills, 150, kills >= 150);
                }
                if (m.Contains("dust spirit") || m.Contains("dust sprite") || m.Contains("dust"))
                {
                    int kills = Game1.stats.getMonstersKilled("Dust Spirit");
                    return ("Dust Sprites", kills, 500, kills >= 500);
                }
                if (m.Contains("slime") || m.Contains("jelly") || m.Contains("sludge"))
                {
                    int kills = Game1.stats.getMonstersKilled("Green Slime")
                              + Game1.stats.getMonstersKilled("Frost Jelly")
                              + Game1.stats.getMonstersKilled("Sludge")
                              + Game1.stats.getMonstersKilled("Tiger Slime");
                    return ("Slimes", kills, 1000, kills >= 1000);
                }
                if (m.Contains("shadow") || m.Contains("void spirit") || m.Contains("shaman") || m.Contains("brute") || m.Contains("sniper"))
                {
                    int kills = Game1.stats.getMonstersKilled("Shadow Brute")
                              + Game1.stats.getMonstersKilled("Shadow Shaman")
                              + Game1.stats.getMonstersKilled("Shadow Sniper");
                    return ("Void Spirits", kills, 150, kills >= 150);
                }
                if (m.Contains("bat"))
                {
                    int kills = Game1.stats.getMonstersKilled("Bat")
                              + Game1.stats.getMonstersKilled("Frost Bat")
                              + Game1.stats.getMonstersKilled("Lava Bat")
                              + Game1.stats.getMonstersKilled("Iridium Bat");
                    return ("Bats", kills, 200, kills >= 200);
                }
                if (m.Contains("skeleton"))
                {
                    int kills = Game1.stats.getMonstersKilled("Skeleton")
                              + Game1.stats.getMonstersKilled("Skeleton Mage");
                    return ("Skeletons", kills, 50, kills >= 50);
                }
                if (m.Contains("bug") || m.Contains("fly") || m.Contains("grub"))
                {
                    int kills = Game1.stats.getMonstersKilled("Cave Fly")
                              + Game1.stats.getMonstersKilled("Grub")
                              + Game1.stats.getMonstersKilled("Bug")
                              + Game1.stats.getMonstersKilled("Mutant Fly")
                              + Game1.stats.getMonstersKilled("Mutant Grub");
                    return ("Cave Insects", kills, 125, kills >= 125);
                }
                if (m.Contains("duggy"))
                {
                    int kills = Game1.stats.getMonstersKilled("Duggy")
                              + Game1.stats.getMonstersKilled("Magma Duggy");
                    return ("Duggies", kills, 30, kills >= 30);
                }
                if (m.Contains("crab"))
                {
                    int kills = Game1.stats.getMonstersKilled("Rock Crab")
                              + Game1.stats.getMonstersKilled("Lava Crab")
                              + Game1.stats.getMonstersKilled("Iridium Crab");
                    return ("Rock Crabs", kills, 60, kills >= 60);
                }
                if (m.Contains("mummy"))
                {
                    int kills = Game1.stats.getMonstersKilled("Mummy");
                    return ("Mummies", kills, 100, kills >= 100);
                }
                if (m.Contains("pepper") || m.Contains("rex") || m.Contains("dinosaur"))
                {
                    int kills = Game1.stats.getMonstersKilled("Pepper Rex");
                    return ("Pepper Rex", kills, 50, kills >= 50);
                }
                if (m.Contains("serpent"))
                {
                    int kills = Game1.stats.getMonstersKilled("Serpent")
                              + Game1.stats.getMonstersKilled("Royal Serpent");
                    return ("Serpents", kills, 250, kills >= 250);
                }
            }
            catch { }

            int genericKills = Game1.stats.getMonstersKilled(monsterName);
            return (monsterName, genericKills, 0, false);
        }

        private static string GetMonsterSpawnLocations(string monsterName)
        {
            string m = monsterName.ToLower();
            if (m.Contains("magma sprite") || m.Contains("sparker")) return ModEntry.I18n.Get("lookup.spawn.volcano-dungeon").ToString();
            if (m.Contains("green slime")) return ModEntry.I18n.Get("lookup.spawn.mines-1-39-secret").ToString();
            if (m.Contains("frost jelly")) return ModEntry.I18n.Get("lookup.spawn.mines-41-79").ToString();
            if (m.Contains("sludge")) return ModEntry.I18n.Get("lookup.spawn.mines-81-119-skull").ToString();
            if (m.Contains("tiger slime")) return ModEntry.I18n.Get("lookup.spawn.ginger-island-volcano").ToString();
            if (m.Contains("slime")) return ModEntry.I18n.Get("lookup.spawn.mines-all-skull-island").ToString();
            if (m.Contains("bat") && m.Contains("frost")) return ModEntry.I18n.Get("lookup.spawn.mines-41-79").ToString();
            if (m.Contains("bat") && m.Contains("lava")) return ModEntry.I18n.Get("lookup.spawn.mines-81-119").ToString();
            if (m.Contains("bat") && m.Contains("iridium")) return ModEntry.I18n.Get("lookup.spawn.skull-deep").ToString();
            if (m.Contains("bat")) return ModEntry.I18n.Get("lookup.spawn.mines-31-119-skull").ToString();
            if (m.Contains("dust")) return ModEntry.I18n.Get("lookup.spawn.mines-41-79-ice").ToString();
            if (m.Contains("skeleton")) return ModEntry.I18n.Get("lookup.spawn.mines-71-79").ToString();
            if (m.Contains("shadow")) return ModEntry.I18n.Get("lookup.spawn.mines-81-119").ToString();
            if (m.Contains("ghost") && m.Contains("carbon")) return ModEntry.I18n.Get("lookup.spawn.skull-carbon").ToString();
            if (m.Contains("ghost")) return ModEntry.I18n.Get("lookup.spawn.mines-51-79").ToString();
            if (m.Contains("rock crab")) return ModEntry.I18n.Get("lookup.spawn.mines-1-29").ToString();
            if (m.Contains("lava crab")) return ModEntry.I18n.Get("lookup.spawn.mines-81-119").ToString();
            if (m.Contains("iridium crab")) return ModEntry.I18n.Get("lookup.spawn.skull-carbon").ToString();
            if (m.Contains("cave fly") || m.Contains("grub") || m.Contains("bug")) return ModEntry.I18n.Get("lookup.spawn.mines-1-29-bug").ToString();
            if (m.Contains("duggy") && m.Contains("magma")) return ModEntry.I18n.Get("lookup.spawn.volcano-dungeon").ToString();
            if (m.Contains("duggy")) return ModEntry.I18n.Get("lookup.spawn.mines-1-29-dirt").ToString();
            if (m.Contains("squid")) return ModEntry.I18n.Get("lookup.spawn.mines-81-119").ToString();
            if (m.Contains("serpent")) return ModEntry.I18n.Get("lookup.spawn.skull-all").ToString();
            if (m.Contains("mummy")) return ModEntry.I18n.Get("lookup.spawn.skull-mummy").ToString();
            if (m.Contains("pepper") || m.Contains("rex")) return ModEntry.I18n.Get("lookup.spawn.skull-prehistoric").ToString();
            if (m.Contains("lava lurk") || m.Contains("dwarvish sentry")) return ModEntry.I18n.Get("lookup.spawn.volcano-lava-pools").ToString();
            return string.Empty;
        }

        private static List<LookupLink> GetMonsterDropLinks(Monster monster)
        {
            var dropLinks = new List<LookupLink>();
            var dropProbabilities = new Dictionary<string, double>();

            try
            {
                var monsterDict = DataLoader.Monsters(Game1.content);
                if (monsterDict != null && monsterDict.TryGetValue(monster.Name, out string? mData) && !string.IsNullOrEmpty(mData))
                {
                    string[] parts = mData.Split('/');
                    if (parts.Length > 6 && !string.IsNullOrEmpty(parts[6]))
                    {
                        string[] dropTokens = parts[6].Split(' ');
                        for (int i = 0; i + 1 < dropTokens.Length; i += 2)
                        {
                            string dId = dropTokens[i];
                            if (double.TryParse(dropTokens[i + 1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double prob))
                            {
                                dropProbabilities[dId] = prob;
                            }
                        }
                    }
                    if (parts.Length > 14 && !string.IsNullOrEmpty(parts[14]))
                    {
                        string[] extraTokens = parts[14].Split(' ');
                        for (int i = 0; i + 1 < extraTokens.Length; i += 2)
                        {
                            string dId = extraTokens[i];
                            if (double.TryParse(extraTokens[i + 1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double prob))
                            {
                                dropProbabilities[dId] = prob;
                            }
                        }
                    }
                }
            }
            catch { }

            var processedIds = new HashSet<string>();
            foreach (var dropId in monster.objectsToDrop)
            {
                string rawId = dropId;
                if (!processedIds.Add(rawId)) continue;

                var dropData = ItemRegistry.GetData(rawId) ?? ItemRegistry.GetData($"(O){rawId}");
                if (dropData != null)
                {
                    string probText = "";
                    if (dropProbabilities.TryGetValue(rawId, out double p))
                    {
                        probText = p >= 1.0 ? " (100%)" : p >= 0.01 ? $" ({p * 100:0.#}%)" : $" ({p * 100:0.00}%)";
                    }

                    dropLinks.Add(new LookupLink(
                        text: $"{dropData.DisplayName}{probText}",
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

            foreach (var kvp in dropProbabilities)
            {
                string rawId = kvp.Key;
                if (!processedIds.Add(rawId)) continue;

                var dropData = ItemRegistry.GetData(rawId) ?? ItemRegistry.GetData($"(O){rawId}");
                if (dropData != null)
                {
                    double p = kvp.Value;
                    string probText = p >= 1.0 ? " (100%)" : p >= 0.01 ? $" ({p * 100:0.#}%)" : $" ({p * 100:0.00}%)";
                    dropLinks.Add(new LookupLink(
                        text: $"{dropData.DisplayName}{probText}",
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

            return dropLinks;
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
                    ModEntry.I18n.Get("lookup.animal.hearts-points-format", new { hearts = $"{info.Hearts:0.0}", max = "5.0", points = info.FriendshipPoints }).ToString(),
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

                // Age
                int ageDays = animal.age.Value;
                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.age"),
                    ModEntry.I18n.Get("lookup.animal.days-old", new { days = ageDays }).ToString(),
                    Color.DarkSlateGray
                ));

                // Home building
                string homeName = animal.home?.buildingType?.Value ?? animal.buildingTypeILiveIn.Value;
                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.home"),
                    homeName,
                    new Color(180, 100, 0)
                ));

                // Fed Today
                bool isFed = animal.fullness.Value >= 200;
                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.fed-today"),
                    isFed ? ModEntry.I18n.Get("lookup.animal.fed-yes").ToString() : ModEntry.I18n.Get("lookup.animal.fed-no").ToString(),
                    isFed ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                // Expected Produce Quality
                float qualityScore = (animal.friendshipTowardFarmer.Value / 1000f) * ((animal.happiness.Value + 100) / 355f);
                string qualityEst = qualityScore >= 0.85f ? ModEntry.I18n.Get("lookup.common.iridium-quality-highest").ToString()
                                  : qualityScore >= 0.60f ? ModEntry.I18n.Get("lookup.common.gold-quality").ToString()
                                  : qualityScore >= 0.35f ? ModEntry.I18n.Get("lookup.common.silver-quality").ToString()
                                  : ModEntry.I18n.Get("lookup.common.normal-quality").ToString();
                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.quality-forecast"),
                    qualityEst,
                    qualityScore >= 0.85f ? new Color(180, 50, 180) : qualityScore >= 0.60f ? new Color(180, 100, 0) : Game1.textColor
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
                    ModEntry.I18n.Get("lookup.animal.hearts-points-format", new { hearts = $"{info.Hearts:0.0}", max = "5.0", points = info.FriendshipPoints }).ToString(),
                    new Color(220, 20, 60)
                ));

                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.animal.petted-today"),
                    info.WasPetToday ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"),
                    info.WasPetToday ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                // Water Bowl Status
                bool bowlWatered = false;
                try
                {
                    var farm = Game1.getFarm();
                    if (farm != null)
                    {
                        foreach (var b in farm.buildings)
                        {
                            if (b is PetBowl pb && pb.watered.Value)
                            {
                                bowlWatered = true;
                                break;
                            }
                        }
                    }
                }
                catch { }

                statusSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.pet.water-bowl"),
                    bowlWatered ? ModEntry.I18n.Get("lookup.petbowl.water-status-filled").ToString() : ModEntry.I18n.Get("lookup.petbowl.water-status-empty").ToString(),
                    bowlWatered ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                if (pet.friendshipTowardFarmer.Value >= 1000)
                {
                    statusSection.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.pet.love-milestone"),
                        ModEntry.I18n.Get("lookup.pet.loves-you", new { name = pet.Name }).ToString(),
                        new Color(180, 50, 180)
                    ));
                }
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
                    info.IsMature ? ModEntry.I18n.Get("hover.tree.fully-grown") : ModEntry.I18n.Get("hover.tree.stage", new { stage = info.GrowthStage + 1, total = 5 }).ToString(),
                    info.IsMature ? new Color(0, 140, 0) : new Color(180, 100, 0)
                ));

                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.tree.moss"),
                    info.HasMoss ? ModEntry.I18n.Get("hover.tree.has-moss") : ModEntry.I18n.Get("lookup.common.no"),
                    info.HasMoss ? new Color(46, 125, 50) : Color.DarkSlateGray
                ));

                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.tree.fertilized"),
                    tree.fertilized.Value ? ModEntry.I18n.Get("lookup.tree.fertilized-status").ToString() : ModEntry.I18n.Get("lookup.common.no").ToString(),
                    tree.fertilized.Value ? new Color(0, 140, 0) : Color.DarkSlateGray
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
                            tapperStatus = ModEntry.I18n.Get("lookup.tree.tapper-ready", new { item = held.DisplayName }).ToString();
                        }
                        else
                        {
                            int hours = tapperObj.MinutesUntilReady / 60;
                            int days = hours / 24;
                            int remHours = hours % 24;
                            string timeText = days > 0 ? $"{days}d {remHours}h" : $"{hours}h";
                            tapperStatus = ModEntry.I18n.Get("lookup.tree.tapper-producing", new { item = held.DisplayName, time = timeText }).ToString();
                        }
                    }
                }

                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.tree.tapper"),
                    tapperStatus,
                    info.IsTapped ? new Color(20, 110, 220) : Color.DarkSlateGray
                ));

                // Tree Produce Guide
                string treeTypeStr = tree.treeType.Value;
                string produceInfo = treeTypeStr switch
                {
                    Tree.bushyTree => ModEntry.I18n.Get("lookup.tree.oak-resin").ToString(),
                    Tree.leafyTree => ModEntry.I18n.Get("lookup.tree.maple-syrup").ToString(),
                    Tree.pineTree => ModEntry.I18n.Get("lookup.tree.pine-tar").ToString(),
                    Tree.mahoganyTree => ModEntry.I18n.Get("lookup.tree.sap").ToString(),
                    Tree.mushroomTree => ModEntry.I18n.Get("lookup.tree.mushroom").ToString(),
                    "7" or "mysticTree" => ModEntry.I18n.Get("lookup.tree.mystic-syrup").ToString(),
                    _ => ModEntry.I18n.Get("lookup.tree.standard-wood").ToString()
                };
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tree.products"), produceInfo, new Color(180, 100, 0)));
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
                        ModEntry.I18n.Get("lookup.tree.maturation"),
                        ModEntry.I18n.Get("hover.fruit-tree.maturing", new { days = info.DaysUntilMature }),
                        new Color(180, 100, 0)
                    ));

                    if (info.IsFertilized)
                    {
                        section.Fields.Add(new LookupField(
                            ModEntry.I18n.Get("lookup.tree.fertilized"),
                            ModEntry.I18n.Get("lookup.tree.fertilized-status").ToString(),
                            new Color(0, 140, 0)
                        ));
                    }
                }
                else
                {
                    int ageDays = fruitTree.daysUntilMature.Value <= 0 ? Math.Abs(fruitTree.daysUntilMature.Value) : 0;
                    string quality = ageDays >= 84 ? ModEntry.I18n.Get("lookup.common.iridium-quality").ToString() : (ageDays >= 56 ? ModEntry.I18n.Get("lookup.common.gold-quality").ToString() : (ageDays >= 28 ? ModEntry.I18n.Get("lookup.common.silver-quality").ToString() : ModEntry.I18n.Get("lookup.common.normal-quality").ToString()));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fruit-tree.fruit-quality"), quality, new Color(180, 50, 180)));

                    section.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.fruit-tree.fruit-count"),
                        ModEntry.I18n.Get("lookup.fruit-tree.fruits-ready", new { count = info.FruitsOnTree }).ToString(),
                        info.FruitsOnTree > 0 ? new Color(0, 140, 0) : Color.DarkSlateGray
                    ));

                    section.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.crop.harvest-seasons"),
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
                        ModEntry.I18n.Get("lookup.fruit-tree.surroundings"),
                        isBlocked ? ModEntry.I18n.Get("lookup.fruit-tree.surroundings-blocked").ToString() : ModEntry.I18n.Get("lookup.fruit-tree.surroundings-clear").ToString(),
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
                        ModEntry.I18n.Get("lookup.tree.maturation"),
                        ModEntry.I18n.Get("hover.bush.tea-maturing", new { days = info.DaysUntilMature }),
                        new Color(180, 100, 0)
                    ));
                }
                else if (info.IsInBloom)
                {
                    section.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.tree.tapper"),
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
                600 => ModEntry.I18n.Get("lookup.clump.large-stump").ToString(),
                602 => ModEntry.I18n.Get("lookup.clump.hollow-log").ToString(),
                622 => ModEntry.I18n.Get("lookup.clump.meteorite").ToString(),
                672 => ModEntry.I18n.Get("lookup.clump.giant-boulder").ToString(),
                752 or 754 or 756 or 758 => ModEntry.I18n.Get("lookup.clump.mine-boulder").ToString(),
                889 => ModEntry.I18n.Get("lookup.clump.fossil-rock").ToString(),
                _ => ModEntry.I18n.Get("lookup.type.resource-clump").ToString()
            };

            string toolReq = index switch
            {
                600 => ModEntry.I18n.Get("lookup.clump.tool.copper-axe").ToString(),
                602 => ModEntry.I18n.Get("lookup.clump.tool.steel-axe").ToString(),
                622 => ModEntry.I18n.Get("lookup.clump.tool.gold-pickaxe").ToString(),
                672 => ModEntry.I18n.Get("lookup.clump.tool.steel-pickaxe-higher").ToString(),
                752 or 754 or 756 or 758 => ModEntry.I18n.Get("lookup.clump.tool.steel-pickaxe").ToString(),
                889 => ModEntry.I18n.Get("lookup.clump.tool.any-pickaxe").ToString(),
                _ => ModEntry.I18n.Get("lookup.slot.tool").ToString()
            };

            string drops = index switch
            {
                600 => ModEntry.I18n.Get("lookup.clump.drops.600").ToString(),
                602 => ModEntry.I18n.Get("lookup.clump.drops.602").ToString(),
                622 => ModEntry.I18n.Get("lookup.clump.drops.622").ToString(),
                672 => ModEntry.I18n.Get("lookup.clump.drops.672").ToString(),
                752 or 754 or 756 or 758 => ModEntry.I18n.Get("lookup.clump.drops.752").ToString(),
                889 => ModEntry.I18n.Get("lookup.clump.drops.889").ToString(),
                _ => ModEntry.I18n.Get("lookup.clump.drops.default").ToString()
            };

            var subject = new LookupSubject
            {
                Title = name,
                Subtitle = ModEntry.I18n.Get("lookup.type.resource-clump").ToString()
            };

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.debris-clearing"));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.clump.tool-required"), toolReq, new Color(200, 60, 20)));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.clump.hits-remaining"), ModEntry.I18n.Get("lookup.clump.hits-remaining-format", new { health = clump.health.Value }).ToString(), new Color(180, 100, 0)));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.clump.resource-drops"), drops, new Color(0, 140, 0)));
            subject.Sections.Add(section);

            return subject;
        }

        public static LookupSubject BuildGiantCropSubject(GiantCrop giantCrop)
        {
            var itemData = ItemRegistry.GetData(giantCrop.Id) ?? ItemRegistry.GetData($"(O){giantCrop.Id}");
            string cropName = itemData?.DisplayName ?? (giantCrop.Id switch
            {
                "190" or "Cauliflower" => "Cauliflower",
                "254" or "Melon" => "Melon",
                "276" or "Pumpkin" => "Pumpkin",
                "Powdermelon" => "Powdermelon",
                "QiFruit" => "Qi Fruit",
                _ => giantCrop.Id ?? ModEntry.I18n.Get("lookup.crop.default-crop").ToString()
            });

            var subject = new LookupSubject
            {
                Title = ModEntry.I18n.Get("hover.giant-crop.title", new { name = cropName }).ToString(),
                Subtitle = ModEntry.I18n.Get("lookup.type.giant-crop").ToString()
            };

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.giant-crop-details"));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.clump.tool-required"), ModEntry.I18n.Get("lookup.giant-crop.tool-desc").ToString(), new Color(20, 110, 220)));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.clump.hits-remaining"), ModEntry.I18n.Get("lookup.clump.axe-hits-format", new { health = giantCrop.health.Value }).ToString(), new Color(180, 100, 0)));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.giant-crop.harvest-yield"), ModEntry.I18n.Get("lookup.giant-crop.yield-desc").ToString(), new Color(0, 140, 0)));
            subject.Sections.Add(section);

            return subject;
        }

        public static LookupSubject BuildBuildingSubject(Building building)
        {
            string bType = building.buildingType.Value;
            var subject = new LookupSubject
            {
                Title = !string.IsNullOrEmpty(bType) ? bType : ModEntry.I18n.Get("lookup.building.default-farm-building").ToString(),
                Subtitle = ModEntry.I18n.Get("lookup.type.building").ToString()
            };

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.building-status"));

            // 1. Junimo Hut
            if (building is JunimoHut hut)
            {
                subject.Title = ModEntry.I18n.Get("lookup.building.junimo-hut").ToString();
                bool harvesting = !hut.noHarvest.Value;
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.building.harvesting-state"),
                    harvesting ? ModEntry.I18n.Get("lookup.building.harvesting-active").ToString() : ModEntry.I18n.Get("lookup.building.harvesting-paused").ToString(),
                    harvesting ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));

                int raisinDays = hut.raisinDays.Value;
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.building.raisins-boost"),
                    raisinDays > 0 ? ModEntry.I18n.Get("lookup.building.raisins-active", new { days = raisinDays }).ToString() : ModEntry.I18n.Get("lookup.building.raisins-none").ToString(),
                    raisinDays > 0 ? new Color(180, 50, 180) : Color.DarkSlateGray
                ));

                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.junimo.harvest-range"), ModEntry.I18n.Get("lookup.junimo.radius-desc").ToString(), new Color(20, 110, 220)));

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
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.junimo.stored-output"), storedLinks));
                }
            }
            // 2. Barn / Coop (AnimalHouse)
            else if (building.indoors.Value is AnimalHouse animalHouse)
            {
                int occupants = animalHouse.animalsThatLiveHere.Count;
                int maxCap = animalHouse.animalLimit.Value;
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.building.animal-capacity"),
                    ModEntry.I18n.Get("lookup.building.animal-capacity-format", new { count = occupants, max = maxCap }).ToString(),
                    occupants >= maxCap ? new Color(0, 140, 0) : new Color(20, 110, 220)
                ));

                int hayCount = animalHouse.numberOfObjectsWithName("Hay");
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.building.feed-troughs"),
                    ModEntry.I18n.Get("lookup.building.feed-troughs-format", new { count = hayCount, max = maxCap }).ToString(),
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
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.building.animals-inside"), animalLinks));
                }

                // Egg Incubator Status
                foreach (var obj in animalHouse.objects.Values)
                {
                    if (obj.Name != null && obj.Name.Contains("Incubator") && obj.heldObject.Value != null)
                    {
                        var egg = obj.heldObject.Value;
                        int days = obj.MinutesUntilReady / 1000;
                        section.Fields.Add(new LookupField(
                            ModEntry.I18n.Get("lookup.building.incubator"),
                            ModEntry.I18n.Get("lookup.incubator.hatching-format", new { egg = egg.DisplayName, days }).ToString(),
                            new Color(180, 100, 0)
                        ));
                    }
                }
            }
            // 3. Mill (Windmill)
            else if (bType.Contains("Mill"))
            {
                subject.Title = ModEntry.I18n.Get("lookup.building.mill").ToString();
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.building.processing-rules"),
                    ModEntry.I18n.Get("lookup.mill.rules").ToString(),
                    new Color(180, 100, 0)
                ));

                var inputChest = building.GetBuildingChest("Input");
                if (inputChest != null && inputChest.Items.Any(i => i != null))
                {
                    var inLinks = new List<LookupLink>();
                    foreach (var item in inputChest.Items.Where(i => i != null))
                    {
                        var itmData = ItemRegistry.GetData(item.QualifiedItemId);
                        inLinks.Add(new LookupLink(
                            text: ModEntry.I18n.Get("lookup.building.mill-processing", new { name = item.DisplayName, stack = item.Stack }).ToString(),
                            textColor: Game1.textColor,
                            icon: itmData?.GetTexture(),
                            iconSourceRect: itmData?.GetSourceRect(),
                            onClick: () => BuildItemSubject(item)
                        ));
                    }
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mill.input-label"), inLinks));
                }

                var outputChest = building.GetBuildingChest("Output");
                if (outputChest != null && outputChest.Items.Any(i => i != null))
                {
                    var outLinks = new List<LookupLink>();
                    foreach (var item in outputChest.Items.Where(i => i != null))
                    {
                        var itmData = ItemRegistry.GetData(item.QualifiedItemId);
                        outLinks.Add(new LookupLink(
                            text: ModEntry.I18n.Get("lookup.building.mill-ready", new { name = item.DisplayName, stack = item.Stack }).ToString(),
                            textColor: new Color(0, 140, 0),
                            icon: itmData?.GetTexture(),
                            iconSourceRect: itmData?.GetSourceRect(),
                            onClick: () => BuildItemSubject(item)
                        ));
                    }
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mill.output-label"), outLinks));
                }
            }
            // 4. Shipping Bin
            else if (bType.Contains("Shipping"))
            {
                subject.Title = ModEntry.I18n.Get("lookup.building.shipping-bin").ToString();
                var farm = Game1.getFarm();
                if (farm != null)
                {
                    var shippedItems = farm.getShippingBin(Game1.player);
                    int count = shippedItems?.Count ?? 0;
                    int estTotal = shippedItems != null ? shippedItems.Sum(i => i.sellToStorePrice() * i.Stack) : 0;

                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.shipping.pending-items"), ModEntry.I18n.Get("lookup.shipping.pending-format", new { count }).ToString(), new Color(20, 110, 220)));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.shipping.revenue"), ModEntry.I18n.Get("lookup.shipping.revenue-format", new { revenue = $"{estTotal:N0}" }).ToString(), new Color(0, 140, 0)));

                    if (shippedItems != null && shippedItems.Count > 0)
                    {
                        var shipLinks = new List<LookupLink>();
                        foreach (var item in shippedItems.Take(36))
                        {
                            var itmData = ItemRegistry.GetData(item.QualifiedItemId);
                            shipLinks.Add(new LookupLink(
                                text: $"{item.DisplayName} (x{item.Stack})",
                                textColor: Game1.textColor,
                                icon: itmData?.GetTexture(),
                                iconSourceRect: itmData?.GetSourceRect(),
                                onClick: () => BuildItemSubject(item)
                            ));
                        }
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.shipping.contents"), shipLinks));
                    }
                }
            }
            // 5. Silo
            else if (bType.Contains("Silo"))
            {
                var farm = Game1.getFarm();
                int hay = farm?.piecesOfHay.Value ?? 0;
                int maxHay = (farm?.buildings.Count(b => b.buildingType.Value.Contains("Silo")) ?? 1) * 240;
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.silo.capacity"), ModEntry.I18n.Get("lookup.silo.hay-format", new { current = hay, max = maxHay }).ToString(), hay < maxHay / 4 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
            }
            // 6. Slime Hutch
            else if (building.indoors.Value is SlimeHutch slimeHutch)
            {
                int slimeCount = slimeHutch.characters.Count(c => c is StardewValley.Monsters.GreenSlime);
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.slime-hutch.population"), ModEntry.I18n.Get("lookup.slime-hutch.population-format", new { current = slimeCount, max = 20 }).ToString(), new Color(0, 140, 0)));

                int waterCount = slimeHutch.waterSpots.Count(w => w);
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.slime-hutch.water-troughs"), ModEntry.I18n.Get("lookup.slime-hutch.troughs-format", new { watered = waterCount, total = 4 }).ToString(), waterCount == 4 ? new Color(0, 140, 0) : new Color(200, 60, 20)));
            }
            // 7. Stable
            else if (building is Stable stable)
            {
                string hName = Game1.player.horseName.Value ?? ModEntry.I18n.Get("hover.stable.horse").ToString();
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.stable.horse"), $"{hName}", new Color(180, 100, 0)));
            }
            // 8. Pet Bowl (1.6)
            else if (building is PetBowl petBowl)
            {
                bool watered = petBowl.watered.Value;
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.petbowl.water-status"),
                    watered ? ModEntry.I18n.Get("lookup.petbowl.water-status-filled").ToString() : ModEntry.I18n.Get("lookup.petbowl.water-status-empty").ToString(),
                    watered ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));
            }

            subject.Sections.Add(section);
            return subject;
        }

        public static LookupSubject BuildChestSubject(Chest chest)
        {
            string chestName = chest.DisplayName ?? ModEntry.I18n.Get("lookup.chest.default-name").ToString();
            int usedSlots = chest.Items.Count(i => i != null);
            int totalSlots = chest.GetActualCapacity();
            int totalItemCount = chest.Items.Where(i => i != null).Sum(i => i.Stack);

            var subject = new LookupSubject
            {
                Title = chestName,
                Subtitle = ModEntry.I18n.Get("lookup.type.storage-container").ToString()
            };

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.storage-overview"));
            section.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.building.storage-capacity"),
                ModEntry.I18n.Get("lookup.building.storage-capacity-format", new { used = usedSlots, total = totalSlots, free = totalSlots - usedSlots }).ToString(),
                usedSlots >= totalSlots ? new Color(200, 60, 20) : new Color(0, 140, 0)
            ));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chest.total-items"), ModEntry.I18n.Get("lookup.chest.total-items-format", new { count = $"{totalItemCount:N0}" }).ToString(), new Color(20, 110, 220)));

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
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chest.stored-items"), itemLinks));
            }

            subject.Sections.Add(section);
            return subject;
        }

        public static LookupSubject BuildFarmerSubject(Farmer farmer)
        {
            var subject = new LookupSubject
            {
                Title = farmer.Name,
                Subtitle = ModEntry.I18n.Get("lookup.farmer.farm-subtitle", new { farm = farmer.farmName.Value, title = farmer.getTitle() }).ToString()
            };

            // 1. Health, Energy & Active Buffs
            var statusSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            statusSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.farmer.health"),
                ModEntry.I18n.Get("lookup.farmer.hp-format", new { current = farmer.health, max = farmer.maxHealth }).ToString(),
                new Color(220, 20, 60)
            ));
            statusSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.farmer.energy"),
                ModEntry.I18n.Get("lookup.farmer.energy-format", new { current = (int)farmer.Stamina, max = farmer.MaxStamina }).ToString(),
                new Color(0, 140, 0)
            ));

            // Stardrops found (Max energy starts at 270, each stardrop adds 34 up to 508 for 7 stardrops)
            int stardropsCount = Math.Clamp((farmer.MaxStamina - 270) / 34, 0, 7);
            statusSection.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.farmer.stardrops"),
                ModEntry.I18n.Get("lookup.perfection.stardrops-found-format", new { count = stardropsCount }).ToString(),
                stardropsCount == 7 ? new Color(0, 140, 0) : new Color(180, 50, 180)
            ));
            subject.Sections.Add(statusSection);

            // 2. Active Buffs & Effects
            var buffSection = new LookupSection(ModEntry.I18n.Get("lookup.section.active-buffs"));
            if (farmer.buffs != null && farmer.buffs.AppliedBuffs.Count > 0)
            {
                foreach (var kvp in farmer.buffs.AppliedBuffs)
                {
                    var buff = kvp.Value;
                    string bName = !string.IsNullOrEmpty(buff.displayName) ? buff.displayName : kvp.Key;
                    string durText = buff.millisecondsDuration > 0 && buff.millisecondsDuration < 9999999
                        ? ModEntry.I18n.Get("lookup.buff.duration-left", new { m = buff.millisecondsDuration / 60000, s = (buff.millisecondsDuration % 60000) / 1000 }).ToString()
                        : ModEntry.I18n.Get("lookup.buff.permanent").ToString();

                    var effectParts = new List<string>();
                    var eff = buff.effects;
                    if (eff != null)
                    {
                        if (eff.Speed.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.speed", new { level = $"{eff.Speed.Value:0.#}" }).ToString());
                        if (eff.Attack.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.attack", new { level = $"{eff.Attack.Value:0.#}" }).ToString());
                        if (eff.Defense.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.defense", new { level = $"{eff.Defense.Value:0.#}" }).ToString());
                        if (eff.LuckLevel.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.luck", new { level = $"{eff.LuckLevel.Value:0.#}" }).ToString());
                        if (eff.FarmingLevel.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.farming", new { level = $"{eff.FarmingLevel.Value:0.#}" }).ToString());
                        if (eff.MiningLevel.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.mining", new { level = $"{eff.MiningLevel.Value:0.#}" }).ToString());
                        if (eff.FishingLevel.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.fishing", new { level = $"{eff.FishingLevel.Value:0.#}" }).ToString());
                        if (eff.ForagingLevel.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.foraging", new { level = $"{eff.ForagingLevel.Value:0.#}" }).ToString());
                        if (eff.MaxStamina.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.max-energy", new { level = $"{eff.MaxStamina.Value:0.#}" }).ToString());
                        if (eff.MagneticRadius.Value != 0) effectParts.Add(ModEntry.I18n.Get("lookup.buff.magnetism", new { level = $"{eff.MagneticRadius.Value:0.#}" }).ToString());
                    }
                    string effectsStr = effectParts.Count > 0 ? $" ({string.Join(", ", effectParts)})" : "";
                    buffSection.Fields.Add(new LookupField(bName, $"{durText}{effectsStr}", new Color(180, 50, 180)));
                }
            }
            else
            {
                buffSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.buff.active-label"), ModEntry.I18n.Get("lookup.buff.none-active").ToString(), Color.DarkSlateGray));
            }
            subject.Sections.Add(buffSection);

            // 3. Equipped Gear & Combat Stats
            var gearSection = new LookupSection(ModEntry.I18n.Get("lookup.farmer.gear"));
            var gearLinks = new List<LookupLink>();

            void AddGearLink(string slotName, Item? gItem)
            {
                if (gItem != null)
                {
                    var gData = ItemRegistry.GetData(gItem.QualifiedItemId);
                    gearLinks.Add(new LookupLink(
                        text: $"{slotName}: {gItem.DisplayName}",
                        textColor: Game1.textColor,
                        icon: gData?.GetTexture(),
                        iconSourceRect: gData?.GetSourceRect(),
                        onClick: () => BuildItemSubject(gItem)
                    ));
                }
            }

            AddGearLink(ModEntry.I18n.Get("lookup.slot.tool").ToString(), farmer.CurrentTool);
            AddGearLink(ModEntry.I18n.Get("lookup.slot.hat").ToString(), farmer.hat.Value);
            AddGearLink(ModEntry.I18n.Get("lookup.slot.shirt").ToString(), farmer.shirtItem.Value);
            AddGearLink(ModEntry.I18n.Get("lookup.slot.pants").ToString(), farmer.pantsItem.Value);
            AddGearLink(ModEntry.I18n.Get("lookup.slot.boots").ToString(), farmer.boots.Value);
            AddGearLink(ModEntry.I18n.Get("lookup.slot.left-ring").ToString(), farmer.leftRing.Value);
            AddGearLink(ModEntry.I18n.Get("lookup.slot.right-ring").ToString(), farmer.rightRing.Value);
            if (farmer.trinketItems.Count > 0 && farmer.trinketItems[0] != null)
            {
                AddGearLink(ModEntry.I18n.Get("lookup.slot.trinket").ToString(), farmer.trinketItems[0]);
            }

            if (gearLinks.Count > 0)
            {
                gearSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.gear.equipped-items"), gearLinks));
            }

            // Aggregate Gear Combat Stats
            int totalDef = 0;
            int totalImm = 0;
            if (farmer.boots.Value is Boots b)
            {
                totalDef += b.defenseBonus.Value;
                totalImm += b.immunityBonus.Value;
            }
            if (farmer.leftRing.Value != null)
            {
                string id = farmer.leftRing.Value.ItemId;
                if (id == "524") totalDef += 5; // Crabshell Ring
                if (id == "517") totalDef += 1; // Topaz Ring
                if (id == "525") totalImm += 4; // Immunity Band
            }
            if (farmer.rightRing.Value != null)
            {
                string id = farmer.rightRing.Value.ItemId;
                if (id == "524") totalDef += 5;
                if (id == "517") totalDef += 1;
                if (id == "525") totalImm += 4;
            }

            gearSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.gear.total-def-imm"), ModEntry.I18n.Get("lookup.gear.total-def-imm-format", new { def = totalDef, imm = totalImm }).ToString(), new Color(20, 110, 220)));
            subject.Sections.Add(gearSection);

            // 4. Chosen Professions
            var profSection = new LookupSection(ModEntry.I18n.Get("lookup.farmer.professions"));
            if (farmer.professions.Count > 0)
            {
                foreach (int profId in farmer.professions)
                {
                    string profName = GetProfessionName(profId);
                    profSection.Fields.Add(new LookupField("•", profName, new Color(0, 140, 0)));
                }
            }
            else
            {
                profSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.professions-label"), ModEntry.I18n.Get("lookup.farmer.no-professions").ToString(), Color.DarkSlateGray));
            }
            subject.Sections.Add(profSection);

            // 5. Special Powers & Wallet
            var walletSection = new LookupSection(ModEntry.I18n.Get("lookup.section.wallet-powers"));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.special-charm"), farmer.hasSpecialCharm ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.special-charm-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.hasSpecialCharm ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.skull-key"), farmer.hasSkullKey ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.skull-key-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.hasSkullKey ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.club-card"), farmer.hasClubCard ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.club-card-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.hasClubCard ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.magnifying-glass"), farmer.hasMagnifyingGlass ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.magnifying-glass-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.hasMagnifyingGlass ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.dark-talisman"), farmer.hasDarkTalisman ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.dark-talisman-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.hasDarkTalisman ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.magic-ink"), farmer.hasMagicInk ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.magic-ink-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.hasMagicInk ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.bears-knowledge"), farmer.eventsSeen.Contains("2120303") || farmer.hasOrWillReceiveMail("BearKnowledge") ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.bears-knowledge-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.eventsSeen.Contains("2120303") || farmer.hasOrWillReceiveMail("BearKnowledge") ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.spring-onion-mastery"), farmer.eventsSeen.Contains("3910979") || farmer.hasOrWillReceiveMail("SpringOnionMastery") ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.spring-onion-mastery-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.eventsSeen.Contains("3910979") || farmer.hasOrWillReceiveMail("SpringOnionMastery") ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.dwarvish-translation"), farmer.canUnderstandDwarves ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.dwarvish-translation-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.canUnderstandDwarves ? new Color(0, 140, 0) : Color.DarkSlateGray));
            walletSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.key-to-town"), farmer.HasTownKey ? ModEntry.I18n.Get("lookup.wallet.unlocked", new { desc = ModEntry.I18n.Get("lookup.wallet.key-to-town-desc") }).ToString() : ModEntry.I18n.Get("lookup.wallet.locked").ToString(), farmer.HasTownKey ? new Color(0, 140, 0) : Color.DarkSlateGray));
            subject.Sections.Add(walletSection);

            // 6. Stats
            var statsSection = new LookupSection(ModEntry.I18n.Get("lookup.section.farmer-statistics"));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.current-gold"), $"{farmer.Money:N0}g", new Color(180, 100, 0)));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.total-earnings"), $"{farmer.totalMoneyEarned:N0}g", new Color(0, 140, 0)));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.qi-gems"), $"{farmer.QiGems}", new Color(180, 50, 180)));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.island.walnuts"), ModEntry.I18n.Get("lookup.island.walnuts-format", new { count = Game1.netWorldState.Value.GoldenWalnutsFound }).ToString(), new Color(180, 100, 0)));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.daily-luck"), $"{farmer.DailyLuck:F3}", farmer.DailyLuck >= 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)));
            subject.Sections.Add(statsSection);

            return subject;
        }

        private static string GetProfessionName(int id) => id switch
        {
            0 => ModEntry.I18n.Get("lookup.profession.0").ToString(),
            1 => ModEntry.I18n.Get("lookup.profession.1").ToString(),
            2 => ModEntry.I18n.Get("lookup.profession.2").ToString(),
            3 => ModEntry.I18n.Get("lookup.profession.3").ToString(),
            4 => ModEntry.I18n.Get("lookup.profession.4").ToString(),
            5 => ModEntry.I18n.Get("lookup.profession.5").ToString(),
            6 => ModEntry.I18n.Get("lookup.profession.6").ToString(),
            7 => ModEntry.I18n.Get("lookup.profession.7").ToString(),
            8 => ModEntry.I18n.Get("lookup.profession.8").ToString(),
            9 => ModEntry.I18n.Get("lookup.profession.9").ToString(),
            10 => ModEntry.I18n.Get("lookup.profession.10").ToString(),
            11 => ModEntry.I18n.Get("lookup.profession.11").ToString(),
            12 => ModEntry.I18n.Get("lookup.profession.12").ToString(),
            13 => ModEntry.I18n.Get("lookup.profession.13").ToString(),
            14 => ModEntry.I18n.Get("lookup.profession.14").ToString(),
            15 => ModEntry.I18n.Get("lookup.profession.15").ToString(),
            16 => ModEntry.I18n.Get("lookup.profession.16").ToString(),
            17 => ModEntry.I18n.Get("lookup.profession.17").ToString(),
            18 => ModEntry.I18n.Get("lookup.profession.18").ToString(),
            19 => ModEntry.I18n.Get("lookup.profession.19").ToString(),
            20 => ModEntry.I18n.Get("lookup.profession.20").ToString(),
            21 => ModEntry.I18n.Get("lookup.profession.21").ToString(),
            22 => ModEntry.I18n.Get("lookup.profession.22").ToString(),
            23 => ModEntry.I18n.Get("lookup.profession.23").ToString(),
            24 => ModEntry.I18n.Get("lookup.profession.24").ToString(),
            25 => ModEntry.I18n.Get("lookup.profession.25").ToString(),
            26 => ModEntry.I18n.Get("lookup.profession.26").ToString(),
            27 => ModEntry.I18n.Get("lookup.profession.27").ToString(),
            28 => ModEntry.I18n.Get("lookup.profession.28").ToString(),
            29 => ModEntry.I18n.Get("lookup.profession.29").ToString(),
            _ => ModEntry.I18n.Get("lookup.profession.unknown", new { id }).ToString()
        };

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

            // Spawn countdown
            int maxCap = pond.maxOccupants.Value;
            if (pond.FishCount < maxCap)
            {
                int spawnRate = 3;
                try
                {
                    var pondData = pond.GetFishPondData();
                    if (pondData != null && pondData.SpawnTime > 0)
                    {
                        spawnRate = pondData.SpawnTime;
                    }
                }
                catch { }

                int daysLeft = Math.Max(0, spawnRate - pond.daysSinceSpawn.Value);
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.fish-pond.spawn-countdown"),
                    daysLeft == 0 ? ModEntry.I18n.Get("hover.fishpond.spawning-tomorrow").ToString() : ModEntry.I18n.Get("lookup.fish-pond.spawn-days-format", new { days = daysLeft }).ToString(),
                    daysLeft == 0 ? new Color(0, 140, 0) : new Color(180, 100, 0)
                ));
            }
            else
            {
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.fish-pond.spawn-countdown"),
                    ModEntry.I18n.Get("lookup.fish-pond.max-capacity").ToString(),
                    Color.DarkSlateGray
                ));
            }

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

            // Roe & Produce Drop Rates Section
            var dropSection = new LookupSection(ModEntry.I18n.Get("lookup.section.fish-pond-drops"));

            // Roe Sell Values
            int fishPrice = 0;
            if (fishData != null && int.TryParse(fishData.RawData?.ToString(), out int fp))
            {
                fishPrice = fp;
            }
            else
            {
                var itemObj = ItemRegistry.Create(fishData?.QualifiedItemId ?? fishId);
                fishPrice = itemObj?.sellToStorePrice() ?? 50;
            }

            int roeBase = 30 + (fishPrice / 2);
            int agedRoePrice = roeBase * 2;
            if (fishName.Contains("Sturgeon") || fishId == "698" || fishId == "(O)698")
            {
                dropSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish-pond.caviar-label"), ModEntry.I18n.Get("lookup.fish-pond.caviar-desc").ToString(), new Color(180, 50, 180)));
            }
            else
            {
                dropSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish-pond.roe-value"), ModEntry.I18n.Get("lookup.fish-pond.roe-format", new { roe = roeBase, aged = agedRoePrice }).ToString(), new Color(180, 100, 0)));
            }

            // Pond Reward Items from FishPondData
            try
            {
                var pondData = pond.GetFishPondData();
                if (pondData != null && pondData.ProducedItems != null && pondData.ProducedItems.Count > 0)
                {
                    var produceLinks = new List<LookupLink>();
                    foreach (var reward in pondData.ProducedItems)
                    {
                        var rData = ItemRegistry.GetData(reward.ItemId);
                        if (rData != null)
                        {
                            string probStr = reward.Chance >= 1.0f ? "100%" : $"{reward.Chance * 100:0.#}%";
                            string countStr = reward.MinStack == reward.MaxStack ? (reward.MinStack > 1 ? $"{reward.MinStack}x " : "") : $"{reward.MinStack}-{reward.MaxStack}x ";
                            string label = ModEntry.I18n.Get("lookup.fish-pond.produce-label", new { count = countStr, item = rData.DisplayName, chance = probStr, pop = reward.RequiredPopulation }).ToString();

                            produceLinks.Add(new LookupLink(
                                text: label,
                                textColor: reward.RequiredPopulation <= pond.FishCount ? new Color(0, 140, 0) : Color.DarkSlateGray,
                                icon: rData.GetTexture(),
                                iconSourceRect: rData.GetSourceRect(),
                                onClick: () =>
                                {
                                    var itm = ItemRegistry.Create(rData.QualifiedItemId);
                                    return itm != null ? BuildItemSubject(itm) : null;
                                }
                            ));
                        }
                    }
                    if (produceLinks.Count > 0)
                    {
                        dropSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fish-pond.possible-produce"), produceLinks));
                    }
                }
            }
            catch { }

            subject.Sections.Add(dropSection);
            return subject;
        }

        public static LookupSubject BuildTileSubject(GameLocation location, Vector2 tilePos)
        {
            return BuildWorldOverviewSubject(location, tilePos);
        }

        public static LookupSubject BuildWorldOverviewSubject(GameLocation? location = null, Vector2? tilePos = null)
        {
            string farmWord = ModEntry.I18n.Get("lookup.world.farm").ToString();
            string locName = location != null ? (location.DisplayName ?? location.Name) : $"{Game1.player.farmName.Value} {farmWord}";
            string almanacWord = ModEntry.I18n.Get("lookup.world.daily-almanac").ToString();
            string title = tilePos.HasValue ? $"{locName} ({tilePos.Value.X}, {tilePos.Value.Y})" : $"{locName} - {almanacWord}";

            string timeStr = Game1.getTimeOfDayString(Game1.timeOfDay);
            int daysLeftInSeason = 28 - Game1.dayOfMonth;
            string daysLeftStr = ModEntry.I18n.Get("lookup.world.days-left", new { days = daysLeftInSeason }).ToString();

            var subject = new LookupSubject
            {
                Title = title,
                Subtitle = $"{GetFullDateString()} — {timeStr} ({daysLeftStr})"
            };

            // 1. Daily Overview Highlights
            subject.Sections.Add(BuildDailyOverviewSummarySection());

            // 2. Calendar, Birthdays & Festivals
            subject.Sections.Add(BuildCalendarSection());

            // 3. Daily Luck & Fortune
            subject.Sections.Add(BuildDailyLuckSection());

            // 4. Weather Forecast (Today & Tomorrow)
            subject.Sections.Add(BuildWeatherSection(location));

            // 5. TV, Bookseller, Clint Upgrades & Quests
            var eventSec = BuildSpecialEventsSection();
            if (eventSec.Fields.Count > 0)
            {
                subject.Sections.Add(eventSec);
            }

            // 6. Farm & Chores Summary (Crops, Animals, Machines, Silo, Greenhouse, Island)
            subject.Sections.Add(BuildFarmSummarySection());

            // 7. Community Center / Joja Progress
            if (ModEntry.Config.ShowCommunityCenterProgress)
            {
                var ccSec = BuildCommunityCenterSection();
                if (ccSec.Fields.Count > 0)
                {
                    subject.Sections.Add(ccSec);
                }
            }

            // 8. Friendship Overview
            if (ModEntry.Config.ShowFriendshipOverview)
            {
                var friendSec = BuildFriendshipOverviewSection();
                if (friendSec.Fields.Count > 0)
                {
                    subject.Sections.Add(friendSec);
                }
            }

            // 9. Progress & Perfection Tracker
            if (ModEntry.Config.ShowProgressAndPerfection)
            {
                var perfSec = BuildProgressAndPerfectionSection();
                if (perfSec.Fields.Count > 0)
                {
                    subject.Sections.Add(perfSec);
                }
            }

            // 10. Museum Donation Progress
            if (ModEntry.Config.ShowMuseumProgress)
            {
                var musSec = BuildMuseumProgressSection();
                if (musSec.Fields.Count > 0)
                {
                    subject.Sections.Add(musSec);
                }
            }

            // 11. Mines & Adventurer's Guild Progress
            if (ModEntry.Config.ShowMineAndGuildProgress)
            {
                var mineSec = BuildMineAndGuildProgressSection();
                if (mineSec.Fields.Count > 0)
                {
                    subject.Sections.Add(mineSec);
                }
            }

            // 12. Current Season Plantable Crops & Seeds Guide
            subject.Sections.Add(BuildSeasonalCropsSection());

            // 13. Current Season Wild Forage Guide
            subject.Sections.Add(BuildSeasonalForageSection());

            // 14. Player Skills, Level & 1.6 Mastery
            subject.Sections.Add(BuildSkillsAndMasterySection());

            // 15. Ginger Island & Qi Progress (if unlocked)
            var islandSec = BuildIslandProgressSection();
            if (islandSec != null && islandSec.Fields.Count > 0)
            {
                subject.Sections.Add(islandSec);
            }

            // 16. Tile Specifics (if clicked on tile)
            if (location != null && tilePos.HasValue)
            {
                subject.Sections.Add(BuildTileDetailsSection(location, tilePos.Value));
            }

            return subject;
        }

        private static LookupSection BuildDailyOverviewSummarySection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.almanac-highlights"));

            try
            {
                // 1. Weather & Luck
                string weatherKey = Game1.isRaining ? (Game1.isLightning ? "stormy" : (Game1.isSnowing ? "snowy" : "rainy")) : (Game1.isGreenRain ? "green-rain" : (Game1.isDebrisWeather ? "debris" : "sunny"));
                string weatherToday = ModEntry.I18n.Get($"lookup.weather.{weatherKey}").ToString();
                double luck = Game1.player.DailyLuck;
                string luckKey = luck switch
                {
                    > 0.07 => "very-lucky",
                    > 0.02 => "good-luck",
                    >= -0.02 => "neutral",
                    >= -0.07 => "bad-luck",
                    _ => "very-bad-luck"
                };
                string luckBrief = ModEntry.I18n.Get($"lookup.luck.{luckKey}").ToString();
                string outlookText = ModEntry.I18n.Get("lookup.world.outlook-format", new { weather = weatherToday, luck = luckBrief }).ToString();
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.world.outlook"), outlookText, luck >= 0.02 ? new Color(0, 140, 0) : (luck <= -0.02 ? new Color(200, 60, 20) : Color.DarkSlateGray)));

                // 2. Chores Highlights
                var farm = Game1.getFarm();
                if (farm != null)
                {
                    int unwatered = 0, readyCrops = 0, unpet = 0, readyProduce = 0, readyMach = 0;
                    foreach (var pair in farm.terrainFeatures.Pairs)
                    {
                        if (pair.Value is HoeDirt dirt && dirt.crop != null && !dirt.crop.dead.Value)
                        {
                            if (dirt.readyForHarvest()) readyCrops++;
                            else if (dirt.needsWatering() && dirt.state.Value != HoeDirt.watered) unwatered++;
                        }
                    }
                    foreach (var animal in farm.getAllFarmAnimals())
                    {
                        if (!animal.wasPet.Value) unpet++;
                        if (animal.currentProduce.Value != null) readyProduce++;
                    }
                    foreach (var obj in farm.objects.Values)
                    {
                        if (obj.heldObject.Value != null && (obj.MinutesUntilReady <= 0 || obj.readyForHarvest.Value))
                            readyMach++;
                    }

                    string choresText = (readyCrops > 0 || unwatered > 0 || unpet > 0 || readyProduce > 0 || readyMach > 0)
                        ? ModEntry.I18n.Get("lookup.world.chores-summary", new { readyCrops, unwatered, unpet, readyProduce, readyMach }).ToString()
                        : ModEntry.I18n.Get("lookup.world.chores-all-done").ToString();

                    section.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.section.farm-chores"),
                        choresText,
                        (readyCrops > 0 || unwatered > 0 || unpet > 0 || readyMach > 0) ? new Color(180, 100, 0) : new Color(0, 140, 0)
                    ));
                }

                // 3. Social / Events Highlights
                var bdayNPCs = Utility.getAllCharacters().Where(c => c != null && c.IsVillager && string.Equals(c.Birthday_Season, Game1.currentSeason, StringComparison.OrdinalIgnoreCase) && c.Birthday_Day == Game1.dayOfMonth).ToList();
                string bdayStr = bdayNPCs.Count > 0 
                    ? string.Join(", ", bdayNPCs.Select(n => ModEntry.I18n.Get("lookup.calendar.npc-birthday", new { name = n.displayName ?? n.Name }).ToString())) 
                    : ModEntry.I18n.Get("lookup.calendar.no-birthdays").ToString();

                bool isBookseller = Game1.getLocationFromName("Town")?.characters.Any(c => c.Name.Equals("Bookseller", StringComparison.OrdinalIgnoreCase)) == true;
                int day = Game1.dayOfMonth;
                int dayOfWeek = (day - 1) % 7;
                bool isCart = (dayOfWeek == 4 || dayOfWeek == 6) || (Game1.currentSeason == "winter" && day >= 15 && day <= 17);

                var highlights = new List<string>();
                if (bdayNPCs.Count > 0) highlights.Add(bdayStr);
                if (isBookseller) highlights.Add(ModEntry.I18n.Get("lookup.events.bookseller-in-town").ToString());
                if (isCart) highlights.Add(ModEntry.I18n.Get("lookup.events.cart-in-forest").ToString());

                string? fest = GetFestivalName(Game1.currentSeason, Game1.dayOfMonth);
                if (!string.IsNullOrEmpty(fest)) highlights.Add(fest);

                if (highlights.Count > 0)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.world.events-today"), string.Join(" | ", highlights), new Color(180, 50, 180)));
                }

                // 4. Progress Highlights
                bool isJoja = Game1.MasterPlayer.mailReceived.Contains("JojaMember");
                if (isJoja)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.world.progress-label"), ModEntry.I18n.Get("lookup.world.joja-active").ToString(), new Color(20, 110, 220)));
                }
                else if (Game1.player.hasCompletedCommunityCenter())
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.world.progress-label"), ModEntry.I18n.Get("lookup.world.cc-restored").ToString(), new Color(0, 140, 0)));
                }
                else
                {
                    int doneRooms = 0;
                    if (Game1.MasterPlayer.mailReceived.Contains("ccBoilerRoom")) doneRooms++;
                    if (Game1.MasterPlayer.mailReceived.Contains("ccCraftsRoom")) doneRooms++;
                    if (Game1.MasterPlayer.mailReceived.Contains("ccPantry")) doneRooms++;
                    if (Game1.MasterPlayer.mailReceived.Contains("ccVault")) doneRooms++;
                    if (Game1.MasterPlayer.mailReceived.Contains("ccFishTank")) doneRooms++;
                    if (Game1.MasterPlayer.mailReceived.Contains("ccBulletin")) doneRooms++;
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.world.progress-label"), ModEntry.I18n.Get("lookup.world.cc-rooms-done", new { count = doneRooms }).ToString(), new Color(180, 100, 0)));
                }
            }
            catch { }

            return section;
        }

        private static string GetFullDateString()
        {
            int day = Game1.dayOfMonth;
            string dayKey = ((day - 1) % 7) switch
            {
                0 => "monday",
                1 => "tuesday",
                2 => "wednesday",
                3 => "thursday",
                4 => "friday",
                5 => "saturday",
                6 => "sunday",
                _ => "monday"
            };
            string dayOfWeek = ModEntry.I18n.Get($"day.{dayKey}").ToString();
            string seasonKey = $"season.{Game1.currentSeason.ToLower()}";
            var tr = ModEntry.I18n.Get(seasonKey);
            string season = tr.HasValue() ? tr.ToString() : (char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1));
            return ModEntry.I18n.Get("lookup.world.full-date", new { dayOfWeek, season, day, year = Game1.year }).ToString();
        }

        private static LookupSection BuildCalendarSection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.calendar-events"));

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
                        text: ModEntry.I18n.Get("lookup.calendar.birthday-today-format", new { name = target.displayName ?? target.Name }).ToString(),
                        textColor: new Color(180, 50, 180),
                        icon: target.Portrait,
                        iconSourceRect: new Rectangle(0, 0, 64, 64),
                        onClick: () => BuildNPCSubject(target)
                    ));
                }
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.calendar.todays-birthday"), bdayLinks));
            }
            else
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.calendar.todays-birthday"), ModEntry.I18n.Get("lookup.common.none").ToString(), Color.DarkSlateGray));
            }

            if (upcomingBirthdays.Count > 0)
            {
                var upLinks = new List<LookupLink>();
                foreach (var (npc, days) in upcomingBirthdays.OrderBy(u => u.DaysUntil))
                {
                    var target = npc;
                    string dayText = days == 1 ? ModEntry.I18n.Get("lookup.calendar.tomorrow").ToString() : ModEntry.I18n.Get("lookup.calendar.in-days-format", new { days }).ToString();
                    upLinks.Add(new LookupLink(
                        text: $"{target.displayName ?? target.Name} ({dayText})",
                        textColor: Game1.textColor,
                        icon: target.Portrait,
                        iconSourceRect: new Rectangle(0, 0, 64, 64),
                        onClick: () => BuildNPCSubject(target)
                    ));
                }
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.calendar.upcoming-birthdays"), upLinks));
            }

            // Festivals / Special Days
            string? festivalToday = GetFestivalName(Game1.currentSeason, Game1.dayOfMonth);
            if (!string.IsNullOrEmpty(festivalToday))
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.calendar.festival-today"), festivalToday, new Color(200, 60, 20)));
            }

            // Upcoming Festival
            for (int d = Game1.dayOfMonth + 1; d <= Math.Min(28, Game1.dayOfMonth + 7); d++)
            {
                string? fest = GetFestivalName(Game1.currentSeason, d);
                if (!string.IsNullOrEmpty(fest))
                {
                    int daysAway = d - Game1.dayOfMonth;
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.calendar.upcoming-festival"), ModEntry.I18n.Get("lookup.calendar.in-days", new { festival = fest, days = daysAway }).ToString(), new Color(180, 100, 0)));
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
                if (day == 13) return ModEntry.I18n.Get("lookup.calendar.festival-egg").ToString();
                if (day == 24) return ModEntry.I18n.Get("lookup.calendar.festival-flower").ToString();
                if (day >= 15 && day <= 17) return ModEntry.I18n.Get("lookup.calendar.festival-desert").ToString();
            }
            else if (s == "summer")
            {
                if (day == 11) return ModEntry.I18n.Get("lookup.calendar.festival-luau").ToString();
                if (day == 28) return ModEntry.I18n.Get("lookup.calendar.festival-jellies").ToString();
                if (day == 20 || day == 21) return ModEntry.I18n.Get("lookup.calendar.festival-trout").ToString();
            }
            else if (s == "fall")
            {
                if (day == 16) return ModEntry.I18n.Get("lookup.calendar.festival-fair").ToString();
                if (day == 27) return ModEntry.I18n.Get("lookup.calendar.festival-spirits-eve").ToString();
            }
            else if (s == "winter")
            {
                if (day == 8) return ModEntry.I18n.Get("lookup.calendar.festival-ice").ToString();
                if (day >= 15 && day <= 17) return ModEntry.I18n.Get("lookup.calendar.festival-night-market").ToString();
                if (day == 25) return ModEntry.I18n.Get("lookup.calendar.festival-winter-star").ToString();
                if (day == 12 || day == 13) return ModEntry.I18n.Get("lookup.calendar.festival-squid").ToString();
            }
            return null;
        }

        private static LookupSection BuildDailyLuckSection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.daily-luck"));
            double luck = Game1.player.DailyLuck;

            string fortuneText;
            Color fortuneColor;

            if (luck > 0.07)
            {
                fortuneText = ModEntry.I18n.Get("lookup.fortune.very-lucky-text").ToString();
                fortuneColor = new Color(0, 140, 0);
            }
            else if (luck > 0.02)
            {
                fortuneText = ModEntry.I18n.Get("lookup.fortune.lucky-text").ToString();
                fortuneColor = new Color(46, 125, 50);
            }
            else if (luck >= -0.02)
            {
                fortuneText = ModEntry.I18n.Get("lookup.fortune.neutral-text").ToString();
                fortuneColor = Color.DarkSlateGray;
            }
            else if (luck >= -0.07)
            {
                fortuneText = ModEntry.I18n.Get("lookup.fortune.unlucky-text").ToString();
                fortuneColor = new Color(200, 100, 20);
            }
            else
            {
                fortuneText = ModEntry.I18n.Get("lookup.fortune.very-unlucky-text").ToString();
                fortuneColor = new Color(220, 20, 60);
            }

            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fortune.spirits-forecast"), fortuneText, fortuneColor));

            string luckSign = luck >= 0 ? $"+{luck:F3}" : $"{luck:F3}";
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.fortune.modifier"), luckSign, luck >= 0 ? new Color(0, 140, 0) : new Color(200, 60, 20)));

            if (Game1.player.hasSpecialCharm)
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.wallet.special-charm"), ModEntry.I18n.Get("lookup.wallet.special-charm-active").ToString(), new Color(180, 50, 180)));
            }

            return section;
        }

        private static LookupSection BuildWeatherSection(GameLocation? location)
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.weather-forecast"));

            // Today's Weather
            string todayWeather = Game1.isGreenRain ? ModEntry.I18n.Get("lookup.weather.green-rain-text").ToString()
                                : Game1.isLightning ? ModEntry.I18n.Get("lookup.weather.lightning-storm").ToString()
                                : Game1.isSnowing ? ModEntry.I18n.Get("lookup.weather.snowing").ToString()
                                : Game1.isRaining ? ModEntry.I18n.Get("lookup.weather.rainy-text").ToString()
                                : Game1.isDebrisWeather ? ModEntry.I18n.Get("lookup.weather.windy-debris").ToString()
                                : ModEntry.I18n.Get("lookup.weather.clear").ToString();

            Color todayColor = (Game1.isRaining || Game1.isLightning || Game1.isGreenRain) ? new Color(20, 110, 220) : new Color(180, 100, 0);
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weather.today-label"), todayWeather, todayColor));

            // Tomorrow's Weather Forecast
            string tomorrowKey = Game1.weatherForTomorrow;
            string tomorrowWeather = tomorrowKey switch
            {
                Game1.weather_rain => ModEntry.I18n.Get("lookup.weather.rainy-text").ToString(),
                Game1.weather_lightning => ModEntry.I18n.Get("lookup.weather.lightning-storm").ToString(),
                Game1.weather_snow => ModEntry.I18n.Get("lookup.weather.snowing").ToString(),
                Game1.weather_green_rain => ModEntry.I18n.Get("lookup.weather.green-rain-text").ToString(),
                Game1.weather_debris => ModEntry.I18n.Get("lookup.weather.windy-debris").ToString(),
                _ => ModEntry.I18n.Get("lookup.weather.sunny").ToString()
            };

            Color tomorrowColor = (tomorrowKey == Game1.weather_rain || tomorrowKey == Game1.weather_lightning || tomorrowKey == Game1.weather_green_rain)
                ? new Color(20, 110, 220)
                : new Color(180, 100, 0);

            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weather.tomorrow-label"), tomorrowWeather, tomorrowColor));

            // Ginger Island Forecast (if unlocked)
            if (Game1.netWorldState.Value.IslandVisitors.Count > 0 || Game1.player.hasOrWillReceiveMail("Visited_Island"))
            {
                var islandLoc = Game1.getLocationFromName("IslandSouth");
                if (islandLoc != null)
                {
                    string islandToday = islandLoc.IsRainingHere() ? ModEntry.I18n.Get("lookup.weather.rainy-text").ToString() : ModEntry.I18n.Get("lookup.weather.sunny").ToString();
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weather.island-weather"), islandToday, islandLoc.IsRainingHere() ? new Color(20, 110, 220) : new Color(180, 100, 0)));
                }
            }

            return section;
        }

        private static LookupSection BuildSpecialEventsSection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.events-tv-quests"));

            // Bookseller (1.6 Feature)
            bool isBookseller = Game1.getLocationFromName("Town")?.characters.Any(c => c.Name.Equals("Bookseller", StringComparison.OrdinalIgnoreCase)) == true;
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.events.bookseller"), isBookseller ? ModEntry.I18n.Get("lookup.events.bookseller-visiting").ToString() : ModEntry.I18n.Get("lookup.events.bookseller-not-today").ToString(), isBookseller ? new Color(180, 50, 180) : Color.DarkSlateGray));

            // Tool Upgrade at Clint's
            if (Game1.player.daysLeftForToolUpgrade.Value > 0)
            {
                int days = Game1.player.daysLeftForToolUpgrade.Value;
                string readyText = days == 1 ? ModEntry.I18n.Get("lookup.events.tool-ready-tomorrow").ToString() : ModEntry.I18n.Get("lookup.events.tool-ready-in-days", new { days }).ToString();
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.events.tool-upgrade"), ModEntry.I18n.Get("lookup.events.tool-upgrading", new { ready = readyText }).ToString(), new Color(180, 100, 0)));
            }

            // Active Quests & Special Orders
            int billboardQuests = Game1.player.questLog.Count;
            int specialOrders = Game1.player.team.specialOrders.Count;
            if (billboardQuests > 0 || specialOrders > 0)
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.events.active-quests"), ModEntry.I18n.Get("lookup.events.quests-active-format", new { billboard = billboardQuests, special = specialOrders }).ToString(), new Color(20, 110, 220)));
            }

            // Queen of Sauce
            int day = Game1.dayOfMonth;
            int dayOfWeek = (day - 1) % 7; // 6 = Sun, 2 = Wed
            if (dayOfWeek == 6)
            {
                var qos = GetQueenOfSauceRecipe(isSunday: true);
                if (qos.HasValue)
                {
                    string status = qos.Value.Known ? ModEntry.I18n.Get("lookup.tv.recipe-known").ToString() : ModEntry.I18n.Get("lookup.tv.recipe-new").ToString();
                    Color statusColor = qos.Value.Known ? Color.DarkSlateGray : new Color(0, 140, 0);
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tv.qos-sunday"), $"{qos.Value.RecipeName} - {status}", statusColor));
                }
            }
            else if (dayOfWeek == 2)
            {
                var qos = GetQueenOfSauceRecipe(isSunday: false);
                if (qos.HasValue)
                {
                    string status = qos.Value.Known ? ModEntry.I18n.Get("lookup.tv.recipe-known").ToString() : ModEntry.I18n.Get("lookup.tv.recipe-new").ToString();
                    Color statusColor = qos.Value.Known ? Color.DarkSlateGray : new Color(0, 140, 0);
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tv.qos-rerun"), $"{qos.Value.RecipeName} - {status}", statusColor));
                }
            }

            // Traveling Merchant
            bool isCartDay = (dayOfWeek == 4 || dayOfWeek == 6) || (Game1.currentSeason == "winter" && day >= 15 && day <= 17);
            if (isCartDay)
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.events.cart"), ModEntry.I18n.Get("lookup.events.cart-schedule").ToString(), new Color(180, 50, 180)));
            }
            else
            {
                int daysToFri = ((4 - dayOfWeek) + 7) % 7;
                if (daysToFri == 0) daysToFri = 7;
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.events.cart"), ModEntry.I18n.Get("lookup.events.cart-next", new { days = daysToFri, dayOfWeek = ModEntry.I18n.Get("day.friday") }).ToString(), Color.DarkSlateGray));
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
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.farm-chores"));

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
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.crops"), ModEntry.I18n.Get("lookup.chores.crops-done").ToString(), new Color(0, 140, 0)));
            }
            else
            {
                var cropParts = new List<string>();
                if (unwateredCrops > 0) cropParts.Add(ModEntry.I18n.Get("lookup.chores.crop-unwatered-part", new { count = unwateredCrops }).ToString());
                if (readyCrops > 0) cropParts.Add(ModEntry.I18n.Get("lookup.chores.crop-ready-part", new { count = readyCrops }).ToString());
                if (deadCrops > 0) cropParts.Add(ModEntry.I18n.Get("lookup.chores.crop-dead-part", new { count = deadCrops }).ToString());
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.crops"), string.Join(", ", cropParts), unwateredCrops > 0 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
            }

            // Animals Field
            if (unpettedAnimals == 0 && readyProduce == 0)
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.animals"), ModEntry.I18n.Get("lookup.chores.animals-done").ToString(), new Color(0, 140, 0)));
            }
            else
            {
                var animalParts = new List<string>();
                if (unpettedAnimals > 0) animalParts.Add(ModEntry.I18n.Get("lookup.chores.animal-unpetted-part", new { count = unpettedAnimals }).ToString());
                if (readyProduce > 0) animalParts.Add(ModEntry.I18n.Get("lookup.chores.animal-produce-part", new { count = readyProduce }).ToString());
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.animals"), string.Join(", ", animalParts), unpettedAnimals > 0 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
            }

            // Machines Field
            if (readyMachines > 0)
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.machines"), ModEntry.I18n.Get("lookup.chores.machines-ready-format", new { count = readyMachines }).ToString(), new Color(0, 140, 0)));
            }
            else
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.machines"), ModEntry.I18n.Get("lookup.chores.no-machines").ToString(), Color.DarkSlateGray));
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
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.silo-hay"), ModEntry.I18n.Get("lookup.chores.silo-hay-format", new { current = hay, max = maxHay }).ToString(), hay < maxHay / 4 ? new Color(200, 60, 20) : Game1.textColor));
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
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.greenhouse"), ModEntry.I18n.Get("lookup.chores.greenhouse-format", new { unwatered = ghUnwatered, ready = ghReady }).ToString(), ghUnwatered > 0 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
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
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.island-farm"), ModEntry.I18n.Get("lookup.chores.island-farm-format", new { unwatered = islUnwatered, ready = islReady }).ToString(), islUnwatered > 0 ? new Color(200, 60, 20) : new Color(0, 140, 0)));
                    }
                }
            }
            catch { }

            return section;
        }

        private static LookupSection BuildTileDetailsSection(GameLocation location, Vector2 tilePos)
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.tile-details"));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tile.location"), location.DisplayName ?? location.Name, new Color(20, 110, 220)));
            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tile.position"), $"X: {tilePos.X}, Y: {tilePos.Y}", Game1.textColor));

            bool isWater = location.isWaterTile((int)tilePos.X, (int)tilePos.Y);
            bool isPassable = location.isTilePassable(new xTile.Dimensions.Location((int)tilePos.X, (int)tilePos.Y), Game1.viewport);

            string tileTypeStr = isWater ? ModEntry.I18n.Get("lookup.tile.water").ToString()
                               : (isPassable ? ModEntry.I18n.Get("lookup.tile.walkable").ToString() : ModEntry.I18n.Get("lookup.tile.obstacle").ToString());

            section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.tile.type"), tileTypeStr, isWater ? new Color(20, 110, 220) : (isPassable ? new Color(0, 140, 0) : new Color(200, 60, 20))));

            return section;
        }

        private static LookupSection BuildSeasonalCropsSection()
        {
            string sKey = $"season.{Game1.currentSeason.ToLower()}";
            var tr = ModEntry.I18n.Get(sKey);
            string seasonName = tr.HasValue() ? tr.ToString() : (char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1));
            var section = new LookupSection(ModEntry.I18n.Get("lookup.seasonal.crops-title", new { season = seasonName }).ToString());
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
                                string infoText = cropData.RegrowDays > 0
                                    ? ModEntry.I18n.Get("lookup.seasonal.crop-info-regrow", new { name = harvestItem.DisplayName, days = totalDays, regrow = cropData.RegrowDays }).ToString()
                                    : ModEntry.I18n.Get("lookup.seasonal.crop-info-single", new { name = harvestItem.DisplayName, days = totalDays }).ToString();

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
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.plantable"), cropLinks));
            }
            else
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.crop.plantable"), ModEntry.I18n.Get("lookup.crop.winter-none").ToString(), Color.DarkSlateGray));
            }

            return section;
        }

        private static LookupSection BuildSeasonalForageSection()
        {
            string sKey = $"season.{Game1.currentSeason.ToLower()}";
            var tr = ModEntry.I18n.Get(sKey);
            string seasonName = tr.HasValue() ? tr.ToString() : (char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1));
            var section = new LookupSection(ModEntry.I18n.Get("lookup.seasonal.forage-title", new { season = seasonName }).ToString());
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
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.forage.valley"), forageLinks));
            }
            if (beachLinks.Count > 0)
            {
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.forage.beach"), beachLinks));
            }

            return section;
        }

        private static LookupSection BuildSkillsAndMasterySection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.skills-mastery"));

            int farmLvl = Game1.player.FarmingLevel;
            int mineLvl = Game1.player.MiningLevel;
            int forageLvl = Game1.player.ForagingLevel;
            int fishLvl = Game1.player.FishingLevel;
            int combatLvl = Game1.player.CombatLevel;

            section.Fields.Add(new LookupField(
                ModEntry.I18n.Get("lookup.skills.levels-label"),
                ModEntry.I18n.Get("lookup.skills.levels-breakdown", new { farming = farmLvl, mining = mineLvl, foraging = forageLvl, fishing = fishLvl, combat = combatLvl }).ToString(),
                new Color(0, 140, 0)
            ));

            try
            {
                int totalLvl = farmLvl + mineLvl + forageLvl + fishLvl + combatLvl;
                if (totalLvl >= 50)
                {
                    int masteryExp = (int)Game1.stats.Get("MasteryExp");
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mastery.progress"), ModEntry.I18n.Get("lookup.mastery.exp-format", new { exp = $"{masteryExp:N0}" }).ToString(), new Color(180, 50, 180)));

                    bool combatM = Game1.player.stats.Get("Mastery_0") > 0;
                    bool forageM = Game1.player.stats.Get("Mastery_1") > 0;
                    bool farmM = Game1.player.stats.Get("Mastery_2") > 0;
                    bool fishM = Game1.player.stats.Get("Mastery_3") > 0;
                    bool mineM = Game1.player.stats.Get("Mastery_4") > 0;

                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mastery.combat"), combatM ? ModEntry.I18n.Get("lookup.mastery.claimed-combat").ToString() : ModEntry.I18n.Get("lookup.mastery.locked").ToString(), combatM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mastery.foraging"), forageM ? ModEntry.I18n.Get("lookup.mastery.claimed-foraging").ToString() : ModEntry.I18n.Get("lookup.mastery.locked").ToString(), forageM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mastery.farming"), farmM ? ModEntry.I18n.Get("lookup.mastery.claimed-farming").ToString() : ModEntry.I18n.Get("lookup.mastery.locked").ToString(), farmM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mastery.fishing"), fishM ? ModEntry.I18n.Get("lookup.mastery.claimed-fishing").ToString() : ModEntry.I18n.Get("lookup.mastery.locked").ToString(), fishM ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mastery.mining"), mineM ? ModEntry.I18n.Get("lookup.mastery.claimed-mining").ToString() : ModEntry.I18n.Get("lookup.mastery.locked").ToString(), mineM ? new Color(0, 140, 0) : Color.DarkSlateGray));
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
                    var section = new LookupSection(ModEntry.I18n.Get("lookup.section.ginger-island"));
                    int walnuts = Game1.netWorldState.Value.GoldenWalnutsFound;
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.island.walnuts"), ModEntry.I18n.Get("lookup.island.walnuts-format", new { count = walnuts }).ToString(), walnuts >= 130 ? new Color(0, 140, 0) : new Color(180, 100, 0)));

                    if (Game1.player.hasOrWillReceiveMail("QiChallengeComplete") || Game1.player.QiGems > 0)
                    {
                        section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.qi-gems"), ModEntry.I18n.Get("lookup.farmer.qi-gems-format", new { count = Game1.player.QiGems }).ToString(), new Color(180, 50, 180)));
                    }

                    return section;
                }
            }
            catch { }
            return null;
        }

        private static LookupSection BuildCommunityCenterSection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.community-center"));

            try
            {
                bool isJoja = Game1.MasterPlayer.mailReceived.Contains("JojaMember");
                if (isJoja)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.section.status"), ModEntry.I18n.Get("lookup.joja.active-desc").ToString(), new Color(20, 110, 220)));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.joja.minecarts"), Game1.MasterPlayer.mailReceived.Contains("jojaBoilerRoom") ? ModEntry.I18n.Get("lookup.joja.completed").ToString() : ModEntry.I18n.Get("lookup.joja.cost-format", new { cost = "5,000" }).ToString(), Game1.MasterPlayer.mailReceived.Contains("jojaBoilerRoom") ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.joja.bridge-repair"), Game1.MasterPlayer.mailReceived.Contains("jojaCraftsRoom") ? ModEntry.I18n.Get("lookup.joja.completed").ToString() : ModEntry.I18n.Get("lookup.joja.cost-format", new { cost = "25,000" }).ToString(), Game1.MasterPlayer.mailReceived.Contains("jojaCraftsRoom") ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.chores.greenhouse"), Game1.MasterPlayer.mailReceived.Contains("jojaPantry") ? ModEntry.I18n.Get("lookup.joja.completed").ToString() : ModEntry.I18n.Get("lookup.joja.cost-format", new { cost = "35,000" }).ToString(), Game1.MasterPlayer.mailReceived.Contains("jojaPantry") ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.joja.bus-repair"), Game1.MasterPlayer.mailReceived.Contains("jojaVault") ? ModEntry.I18n.Get("lookup.joja.completed").ToString() : ModEntry.I18n.Get("lookup.joja.cost-format", new { cost = "40,000" }).ToString(), Game1.MasterPlayer.mailReceived.Contains("jojaVault") ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.joja.panning"), Game1.MasterPlayer.mailReceived.Contains("jojaFishTank") ? ModEntry.I18n.Get("lookup.joja.completed").ToString() : ModEntry.I18n.Get("lookup.joja.cost-format", new { cost = "20,000" }).ToString(), Game1.MasterPlayer.mailReceived.Contains("jojaFishTank") ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.joja.movie-theater"), Game1.MasterPlayer.mailReceived.Contains("ccMovieTheater") ? ModEntry.I18n.Get("lookup.joja.completed").ToString() : ModEntry.I18n.Get("lookup.joja.cost-format", new { cost = "500,000" }).ToString(), Game1.MasterPlayer.mailReceived.Contains("ccMovieTheater") ? new Color(0, 140, 0) : Color.DarkSlateGray));
                    return section;
                }

                if (Game1.player.hasCompletedCommunityCenter())
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.world.cc-status"), ModEntry.I18n.Get("lookup.cc.restored-all").ToString(), new Color(0, 140, 0)));
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.cc.abandoned-jojamart"), Game1.MasterPlayer.mailReceived.Contains("ccMovieTheater") ? ModEntry.I18n.Get("lookup.cc.theater-built").ToString() : ModEntry.I18n.Get("lookup.cc.theater-missing").ToString(), Game1.MasterPlayer.mailReceived.Contains("ccMovieTheater") ? new Color(0, 140, 0) : new Color(180, 50, 180)));
                    return section;
                }

                var bundlesDict = DataLoader.Bundles(Game1.content);
                var worldBundles = Game1.netWorldState.Value.Bundles;

                if (bundlesDict == null)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.section.status"), ModEntry.I18n.Get("lookup.world.cc-in-progress").ToString(), new Color(0, 140, 0)));
                    return section;
                }

                var roomNames = new Dictionary<string, string>
                {
                    { "Pantry", ModEntry.I18n.Get("lookup.cc.room.pantry").ToString() },
                    { "CraftsRoom", ModEntry.I18n.Get("lookup.cc.room.crafts").ToString() },
                    { "FishTank", ModEntry.I18n.Get("lookup.cc.room.fish-tank").ToString() },
                    { "BoilerRoom", ModEntry.I18n.Get("lookup.cc.room.boiler").ToString() },
                    { "Vault", ModEntry.I18n.Get("lookup.cc.room.vault").ToString() },
                    { "BulletinBoard", ModEntry.I18n.Get("lookup.cc.room.bulletin").ToString() },
                    { "AbandonedJojaMart", ModEntry.I18n.Get("lookup.cc.room.abandoned-joja").ToString() }
                };

                var roomBundles = new Dictionary<string, List<(string Key, string RawData)>>();
                foreach (var kvp in bundlesDict)
                {
                    string room = kvp.Key.Split('/')[0];
                    if (!roomBundles.ContainsKey(room))
                        roomBundles[room] = new List<(string Key, string RawData)>();
                    roomBundles[room].Add((kvp.Key, kvp.Value));
                }

                foreach (var roomKvp in roomBundles)
                {
                    string roomKey = roomKvp.Key;
                    string roomTitle = roomNames.GetValueOrDefault(roomKey, roomKey);
                    var bList = roomKvp.Value;

                    int roomCompleted = 0;
                    var missingLinks = new List<LookupLink>();

                    foreach (var (bKey, bData) in bList)
                    {
                        string[] parts = bData.Split('/');
                        if (parts.Length < 3) continue;

                        string bundleName = parts[0];
                        string ingStr = parts[2];
                        int pickCount = parts.Length > 4 && int.TryParse(parts[4], out int pc) ? pc : -1;

                        int bIndex = 0;
                        string[] keyParts = bKey.Split('/');
                        if (keyParts.Length > 1 && int.TryParse(keyParts[1], out int bi)) bIndex = bi;

                        bool isDone = false;
                        if (worldBundles.TryGetValue(bIndex, out bool[]? slots) && slots != null)
                        {
                            int filled = slots.Count(s => s);
                            string[] ingTokens = ingStr.Split(' ');
                            int totalSlots = ingTokens.Length / 3;
                            int needed = pickCount > 0 ? pickCount : totalSlots;
                            if (filled >= needed)
                            {
                                isDone = true;
                            }
                        }

                        if (isDone)
                        {
                            roomCompleted++;
                        }
                        else
                        {
                            string[] tokens = ingStr.Split(' ');
                            for (int i = 0; i + 2 < tokens.Length; i += 3)
                            {
                                string itemId = tokens[i];
                                int slotIndex = i / 3;

                                bool slotFilled = slots != null && slotIndex < slots.Length && slots[slotIndex];
                                if (!slotFilled)
                                {
                                    var itemData = ItemRegistry.GetData(itemId) ?? ItemRegistry.GetData($"(O){itemId}");
                                    if (itemData != null && !missingLinks.Any(l => l.Text.StartsWith(itemData.DisplayName)))
                                    {
                                        missingLinks.Add(new LookupLink(
                                            text: $"{itemData.DisplayName} ({bundleName})",
                                            textColor: Game1.textColor,
                                            icon: itemData.GetTexture(),
                                            iconSourceRect: itemData.GetSourceRect(),
                                            onClick: () =>
                                            {
                                                var itm = ItemRegistry.Create(itemData.QualifiedItemId);
                                                return itm != null ? BuildItemSubject(itm) : null;
                                            }
                                        ));
                                        if (missingLinks.Count >= 8) break;
                                    }
                                }
                            }
                        }
                    }

                    if (roomCompleted == bList.Count)
                    {
                        section.Fields.Add(new LookupField(roomTitle, ModEntry.I18n.Get("lookup.world.room-completed").ToString(), new Color(0, 140, 0)));
                    }
                    else
                    {
                        string status = ModEntry.I18n.Get("lookup.cc.bundles-progress-format", new { completed = roomCompleted, total = bList.Count }).ToString();
                        if (missingLinks.Count > 0)
                        {
                            section.Fields.Add(new LookupField($"{roomTitle} ({status})", missingLinks));
                        }
                        else
                        {
                            section.Fields.Add(new LookupField(roomTitle, status, new Color(180, 100, 0)));
                        }
                    }
                }
            }
            catch { }

            return section;
        }

        private static LookupSection BuildFriendshipOverviewSection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.friendship-overview"));

            try
            {
                var unspokenLinks = new List<LookupLink>();
                var giftLinks = new List<LookupLink>();
                int maxHeartsCount = 0;
                int totalHearts = 0;
                int villagerCount = 0;

                foreach (var npc in Utility.getAllCharacters())
                {
                    if (npc == null || !npc.IsVillager || npc.IsMonster || string.IsNullOrEmpty(npc.Name))
                        continue;

                    if (Game1.player.friendshipData.TryGetValue(npc.Name, out var friendship))
                    {
                        villagerCount++;
                        int hearts = friendship.Points / 250;
                        totalHearts += hearts;
                        int maxH = (npc.datable.Value && !friendship.IsDating()) ? 8 : 10;
                        if (hearts >= maxH) maxHeartsCount++;

                        var target = npc;
                        if (!Game1.player.hasPlayerTalkedToNPC(npc.Name))
                        {
                            unspokenLinks.Add(new LookupLink(
                                text: $"{target.displayName ?? target.Name} ({hearts}♥)",
                                textColor: Game1.textColor,
                                icon: target.Portrait,
                                iconSourceRect: new Rectangle(0, 0, 64, 64),
                                onClick: () => BuildNPCSubject(target)
                            ));
                        }

                        if (friendship.GiftsThisWeek < 2 && friendship.GiftsToday == 0)
                        {
                            giftLinks.Add(new LookupLink(
                                text: ModEntry.I18n.Get("lookup.friendship.gifts-left-format", new { target = target.displayName ?? target.Name, count = 2 - friendship.GiftsThisWeek }).ToString(),
                                textColor: new Color(0, 140, 0),
                                icon: target.Portrait,
                                iconSourceRect: new Rectangle(0, 0, 64, 64),
                                onClick: () => BuildNPCSubject(target)
                            ));
                        }
                    }
                }

                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.friendship.summary-label"),
                    ModEntry.I18n.Get("lookup.friendship.summary-format", new { maxFriends = maxHeartsCount, totalVillagers = villagerCount, totalHearts }).ToString(),
                    new Color(180, 50, 180)
                ));

                if (unspokenLinks.Count > 0)
                {
                    section.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.friendship.unspoken-format", new { count = unspokenLinks.Count }).ToString(),
                        unspokenLinks.Take(12).ToList()
                    ));
                }
                else
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.npc.talked-today"), ModEntry.I18n.Get("lookup.npc.talked-all").ToString(), new Color(0, 140, 0)));
                }

                if (giftLinks.Count > 0)
                {
                    section.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.friendship.gifts-available-format", new { count = giftLinks.Count }).ToString(),
                        giftLinks.Take(12).ToList()
                    ));
                }
            }
            catch { }

            return section;
        }

        private static LookupSection BuildProgressAndPerfectionSection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.progress-perfection"));

            try
            {
                // 1. Produce Shipped (15%)
                int totalToShip = 0;
                int totalShipped = 0;
                var allObjs = DataLoader.Objects(Game1.content);
                if (allObjs != null)
                {
                    foreach (var kvp in allObjs)
                    {
                        string oId = kvp.Key;
                        var oData = kvp.Value;
                        var pData = ItemRegistry.GetData(oId) ?? ItemRegistry.GetData($"(O){oId}");
                        if (pData != null && !pData.ObjectType.Equals("Arch", StringComparison.OrdinalIgnoreCase) && !pData.ObjectType.Equals("Minerals", StringComparison.OrdinalIgnoreCase) && !pData.ObjectType.Equals("Fish", StringComparison.OrdinalIgnoreCase) && pData.Category != -75 && pData.Category != -79 && pData.Category != -80 && pData.Category != -81 && pData.Category != -999 && pData.Category != 0)
                        {
                            if (oData.Type == "Basic" || pData.Category == StardewValley.Object.VegetableCategory || pData.Category == StardewValley.Object.FruitsCategory || pData.Category == StardewValley.Object.flowersCategory || pData.Category == StardewValley.Object.EggCategory || pData.Category == StardewValley.Object.MilkCategory || pData.Category == StardewValley.Object.artisanGoodsCategory || pData.Category == StardewValley.Object.meatCategory || pData.Category == StardewValley.Object.syrupCategory || pData.Category == StardewValley.Object.GreensCategory)
                            {
                                totalToShip++;
                                if (Game1.player.basicShipped.ContainsKey(oId) || Game1.player.basicShipped.ContainsKey($"(O){oId}"))
                                {
                                    totalShipped++;
                                }
                            }
                        }
                    }
                }
                if (totalToShip == 0) totalToShip = 145;
                float shippedPct = Math.Min(15f, (float)totalShipped / Math.Max(1, totalToShip) * 15f);
                float shippedDisplayPct = (float)totalShipped / Math.Max(1, totalToShip) * 100f;
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.shipped-summary"), ModEntry.I18n.Get("lookup.perfection.shipped-format", new { shipped = totalShipped, total = totalToShip, percent = $"{Math.Min(100f, shippedDisplayPct):0.0}" }).ToString(), shippedPct >= 15f ? new Color(0, 140, 0) : Game1.textColor));

                // 2. Obelisks Built (4%)
                var allBuildings = new List<Building>();
                if (Game1.getFarm() != null) allBuildings.AddRange(Game1.getFarm().buildings);
                var islandFarm = Game1.getLocationFromName("IslandWest");
                if (islandFarm != null) allBuildings.AddRange(islandFarm.buildings);

                int obeliskCount = 0;
                if (allBuildings.Any(b => b.buildingType.Value.Contains("Earth Obelisk"))) obeliskCount++;
                if (allBuildings.Any(b => b.buildingType.Value.Contains("Water Obelisk"))) obeliskCount++;
                if (allBuildings.Any(b => b.buildingType.Value.Contains("Desert Obelisk"))) obeliskCount++;
                if (allBuildings.Any(b => b.buildingType.Value.Contains("Island Obelisk"))) obeliskCount++;
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.obelisks"), ModEntry.I18n.Get("lookup.perfection.obelisks-built-format", new { count = obeliskCount }).ToString(), obeliskCount == 4 ? new Color(0, 140, 0) : Color.DarkSlateGray));

                // 3. Gold Clock Built (10%)
                bool hasGoldClock = allBuildings.Any(b => b.buildingType.Value.Contains("Gold Clock"));
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.gold-clock"), hasGoldClock ? ModEntry.I18n.Get("lookup.perfection.gold-clock-built").ToString() : ModEntry.I18n.Get("lookup.perfection.gold-clock-not-built").ToString(), hasGoldClock ? new Color(0, 140, 0) : Color.DarkSlateGray));

                // 4. Monster Slayer Goals (10%)
                var (slimesCat, sKills, sGoal, sComp) = GetMonsterSlayerProgress("Green Slime");
                var (batsCat, bKills, bGoal, bComp) = GetMonsterSlayerProgress("Bat");
                var (skelCat, skKills, skGoal, skComp) = GetMonsterSlayerProgress("Skeleton");
                var (voidCat, vKills, vGoal, vComp) = GetMonsterSlayerProgress("Shadow Brute");
                var (caveCat, cKills, cGoal, cComp) = GetMonsterSlayerProgress("Bug");
                var (dugCat, dKills, dGoal, dComp) = GetMonsterSlayerProgress("Duggy");
                var (dustCat, duKills, duGoal, duComp) = GetMonsterSlayerProgress("Dust Spirit");
                var (crabCat, crKills, crGoal, crComp) = GetMonsterSlayerProgress("Rock Crab");
                var (mumCat, mKills, mGoal, mComp) = GetMonsterSlayerProgress("Mummy");
                var (rexCat, rKills, rGoal, rComp) = GetMonsterSlayerProgress("Pepper Rex");
                var (serpCat, seKills, seGoal, seComp) = GetMonsterSlayerProgress("Serpent");
                var (magCat, maKills, maGoal, maComp) = GetMonsterSlayerProgress("Magma Sprite");

                int slayerGoalsComp = (sComp ? 1 : 0) + (bComp ? 1 : 0) + (skComp ? 1 : 0) + (vComp ? 1 : 0)
                                    + (cComp ? 1 : 0) + (dComp ? 1 : 0) + (duComp ? 1 : 0) + (crComp ? 1 : 0)
                                    + (mComp ? 1 : 0) + (rComp ? 1 : 0) + (seComp ? 1 : 0) + (maComp ? 1 : 0);
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.slayer-pct"), ModEntry.I18n.Get("lookup.perfection.slayer-format", new { completed = slayerGoalsComp }).ToString(), slayerGoalsComp == 12 ? new Color(0, 140, 0) : Game1.textColor));

                // 5. Great Friends (10%)
                int maxFriends = 0;
                int totalVillagers = 0;
                var seenNPCs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var npc in Utility.getAllCharacters())
                {
                    if (npc != null && npc.IsVillager && !npc.IsMonster && npc.CanSocialize && seenNPCs.Add(npc.Name))
                    {
                        totalVillagers++;
                        if (Game1.player.friendshipData.TryGetValue(npc.Name, out var f))
                        {
                            int reqPoints = (npc.datable.Value && !f.IsDating()) ? 2000 : 2500;
                            if (f.Points >= reqPoints) maxFriends++;
                        }
                    }
                }
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.friends-pct"), ModEntry.I18n.Get("lookup.perfection.friends-format", new { count = maxFriends, total = totalVillagers }).ToString(), maxFriends >= totalVillagers && totalVillagers > 0 ? new Color(0, 140, 0) : Game1.textColor));

                // 6. Farmer Level 25 (5%)
                int totalLevels = Game1.player.FarmingLevel + Game1.player.MiningLevel + Game1.player.ForagingLevel + Game1.player.FishingLevel + Game1.player.CombatLevel;
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.farmer-level"), ModEntry.I18n.Get("lookup.perfection.farmer-level-format", new { total = totalLevels }).ToString(), totalLevels >= 50 ? new Color(0, 140, 0) : Game1.textColor));

                // 7. Stardrops (10%)
                int stardrops = 0;
                if (Game1.player.mailReceived.Contains("CF_Spouse")) stardrops++;
                if (Game1.player.mailReceived.Contains("CF_Mines")) stardrops++;
                if (Game1.player.mailReceived.Contains("CF_Fair")) stardrops++;
                if (Game1.player.mailReceived.Contains("CF_Fish")) stardrops++;
                if (Game1.player.mailReceived.Contains("CF_Sewer")) stardrops++;
                if (Game1.player.mailReceived.Contains("CF_Statue")) stardrops++;
                if (Game1.player.mailReceived.Contains("CF_Museum")) stardrops++;
                if (stardrops == 0) stardrops = Math.Clamp((Game1.player.MaxStamina - 270) / 34, 0, 7);
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.stardrops-pct"), ModEntry.I18n.Get("lookup.perfection.stardrops-found-format", new { count = stardrops }).ToString(), stardrops == 7 ? new Color(0, 140, 0) : Game1.textColor));

                // 8. Cooking (10%)
                int cookedCount = Game1.player.recipesCooked.Pairs.Count();
                int totalCooking = CraftingRecipe.cookingRecipes.Count;
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.cooking-pct"), ModEntry.I18n.Get("lookup.perfection.cooking-format", new { cooked = cookedCount, total = totalCooking }).ToString(), cookedCount >= totalCooking ? new Color(0, 140, 0) : Game1.textColor));

                // 9. Crafting (10%)
                int craftedCount = Game1.player.craftingRecipes.Pairs.Count(kv => kv.Value > 0);
                int totalCrafting = CraftingRecipe.craftingRecipes.Count;
                if (!Game1.IsMultiplayer && CraftingRecipe.craftingRecipes.ContainsKey("Wedding Ring"))
                {
                    totalCrafting--;
                    if (Game1.player.craftingRecipes.TryGetValue("Wedding Ring", out int wr) && wr > 0)
                    {
                        craftedCount--;
                    }
                }
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.crafting-pct"), ModEntry.I18n.Get("lookup.perfection.crafting-format", new { crafted = craftedCount, total = totalCrafting }).ToString(), craftedCount >= totalCrafting ? new Color(0, 140, 0) : Game1.textColor));

                // 10. Fish (10%)
                var allFishData = DataLoader.Fish(Game1.content);
                int totalFish = 0;
                int caughtFish = 0;
                if (allFishData != null)
                {
                    foreach (var kvp in allFishData)
                    {
                        string fId = kvp.Key;
                        var fData = ItemRegistry.GetData(fId) ?? ItemRegistry.GetData($"(O){fId}");
                        if (fData != null && fData.Category == StardewValley.Object.FishCategory)
                        {
                            if (fId != "152" && fId != "153" && fId != "157" && fId != "168")
                            {
                                totalFish++;
                                if (Game1.player.fishCaught.ContainsKey(fId) || Game1.player.fishCaught.ContainsKey($"(O){fId}"))
                                {
                                    caughtFish++;
                                }
                            }
                        }
                    }
                }
                if (totalFish == 0) totalFish = 67;
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.fish-pct"), ModEntry.I18n.Get("lookup.perfection.fish-caught-format", new { caught = caughtFish, total = totalFish }).ToString(), caughtFish >= totalFish ? new Color(0, 140, 0) : Game1.textColor));

                // 11. Golden Walnuts (5%)
                int walnuts = Game1.netWorldState.Value.GoldenWalnutsFound;
                section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.walnuts-pct"), ModEntry.I18n.Get("lookup.perfection.walnuts-found-format", new { count = walnuts }).ToString(), walnuts >= 130 ? new Color(0, 140, 0) : Game1.textColor));

                float totalPerfection = (shippedPct)
                    + (obeliskCount * 1.0f)
                    + (hasGoldClock ? 10f : 0f)
                    + (slayerGoalsComp / 12f * 10f)
                    + ((float)maxFriends / Math.Max(1, totalVillagers) * 10f)
                    + (totalLevels >= 50 ? 5f : (totalLevels / 50f * 5f))
                    + (stardrops / 7f * 10f)
                    + ((float)cookedCount / Math.Max(1, totalCooking) * 10f)
                    + ((float)craftedCount / Math.Max(1, totalCrafting) * 10f)
                    + ((float)caughtFish / Math.Max(1, totalFish) * 10f)
                    + ((float)walnuts / 130f * 5f);

                section.Fields.Insert(0, new LookupField(
                    ModEntry.I18n.Get("lookup.perfection.tracker-title").ToString(),
                    ModEntry.I18n.Get("lookup.perfection.overall-format", new { percent = $"{Math.Min(100f, totalPerfection):0.0}" }).ToString(),
                    totalPerfection >= 100f ? new Color(180, 50, 180) : new Color(20, 110, 220)
                ));
            }
            catch { }

            return section;
        }

        private static LookupSection BuildMuseumProgressSection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.museum-progress"));

            try
            {
                var donatedPieces = Game1.netWorldState.Value.MuseumPieces;
                int donatedCount = donatedPieces != null ? donatedPieces.Pairs.Count() : 0;

                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.museum.total-donated-label"),
                    ModEntry.I18n.Get("lookup.museum.total-donated-format", new { count = donatedCount, remaining = Math.Max(0, 95 - donatedCount) }).ToString(),
                    donatedCount >= 95 ? new Color(0, 140, 0) : new Color(180, 100, 0)
                ));

                var donatedSet = new HashSet<string>();
                if (donatedPieces != null)
                {
                    foreach (var kvp in donatedPieces.Pairs)
                    {
                        donatedSet.Add(kvp.Value);
                        donatedSet.Add($"(O){kvp.Value}");
                    }
                }

                var missingArtifacts = new List<LookupLink>();
                var missingMinerals = new List<LookupLink>();

                var allObjects = DataLoader.Objects(Game1.content);
                if (allObjects != null)
                {
                    foreach (var kvp in allObjects)
                    {
                        string id = kvp.Key;
                        var objData = kvp.Value;
                        if (string.IsNullOrEmpty(objData.Type)) continue;

                        if (!donatedSet.Contains(id) && !donatedSet.Contains($"(O){id}"))
                        {
                            if (objData.Type == "Arch")
                            {
                                var itmData = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                                if (itmData != null)
                                {
                                    missingArtifacts.Add(new LookupLink(
                                        text: itmData.DisplayName,
                                        textColor: Game1.textColor,
                                        icon: itmData.GetTexture(),
                                        iconSourceRect: itmData.GetSourceRect(),
                                        onClick: () =>
                                        {
                                            var itm = ItemRegistry.Create(itmData.QualifiedItemId);
                                            return itm != null ? BuildItemSubject(itm) : null;
                                        }
                                    ));
                                }
                            }
                            else if (objData.Type == "Minerals")
                            {
                                var itmData = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                                if (itmData != null)
                                {
                                    missingMinerals.Add(new LookupLink(
                                        text: itmData.DisplayName,
                                        textColor: Game1.textColor,
                                        icon: itmData.GetTexture(),
                                        iconSourceRect: itmData.GetSourceRect(),
                                        onClick: () =>
                                        {
                                            var itm = ItemRegistry.Create(itmData.QualifiedItemId);
                                            return itm != null ? BuildItemSubject(itm) : null;
                                        }
                                    ));
                                }
                            }
                        }
                    }
                }

                if (missingArtifacts.Count > 0)
                {
                    section.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.museum.missing-artifacts-format", new { count = missingArtifacts.Count }).ToString(),
                        missingArtifacts.Take(12).ToList()
                    ));
                }
                else
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.artifacts"), ModEntry.I18n.Get("lookup.perfection.artifacts-all").ToString(), new Color(0, 140, 0)));
                }

                if (missingMinerals.Count > 0)
                {
                    section.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.museum.missing-minerals-format", new { count = missingMinerals.Count }).ToString(),
                        missingMinerals.Take(12).ToList()
                    ));
                }
                else
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.minerals"), ModEntry.I18n.Get("lookup.perfection.minerals-all").ToString(), new Color(0, 140, 0)));
                }

                int[] milestones = { 5, 10, 15, 20, 25, 30, 35, 40, 50, 60, 70, 80, 90, 95 };
                int nextMilestone = milestones.FirstOrDefault(m => m > donatedCount);
                if (nextMilestone > 0)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.building.milestone"), ModEntry.I18n.Get("lookup.museum.next-milestone-format", new { needed = nextMilestone - donatedCount, milestone = nextMilestone }).ToString(), new Color(20, 110, 220)));
                }
                else
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.museum-pct"), ModEntry.I18n.Get("lookup.perfection.museum-all").ToString(), new Color(180, 50, 180)));
                }
            }
            catch { }

            return section;
        }

        private static LookupSection BuildMineAndGuildProgressSection()
        {
            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.mine-guild-progress"));

            try
            {
                int deepest = Game1.player.deepestMineLevel;
                int regFloor = Math.Min(120, deepest);
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.mine.regular-depth-label"),
                    regFloor >= 120 ? ModEntry.I18n.Get("lookup.mine.regular-depth-bottom").ToString() : ModEntry.I18n.Get("lookup.mine.regular-depth-progress", new { floor = regFloor }).ToString(),
                    regFloor >= 120 ? new Color(0, 140, 0) : new Color(180, 100, 0)
                ));

                if (deepest > 120)
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.skull-record"), ModEntry.I18n.Get("lookup.mine.skull-record-format", new { level = deepest - 120 }).ToString(), new Color(180, 50, 180)));
                }
                else
                {
                    section.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.skull-record"), ModEntry.I18n.Get("lookup.farmer.skull-unexplored").ToString(), Color.DarkSlateGray));
                }

                (string cat, int k, int g, bool c)[] goals =
                {
                    GetMonsterSlayerProgress("Green Slime"),
                    GetMonsterSlayerProgress("Shadow Brute"),
                    GetMonsterSlayerProgress("Bat"),
                    GetMonsterSlayerProgress("Skeleton"),
                    GetMonsterSlayerProgress("Bug"),
                    GetMonsterSlayerProgress("Duggy"),
                    GetMonsterSlayerProgress("Dust Spirit"),
                    GetMonsterSlayerProgress("Rock Crab"),
                    GetMonsterSlayerProgress("Mummy"),
                    GetMonsterSlayerProgress("Pepper Rex"),
                    GetMonsterSlayerProgress("Serpent"),
                    GetMonsterSlayerProgress("Magma Sprite")
                };

                foreach (var (cat, k, g, c) in goals)
                {
                    string status = c ? ModEntry.I18n.Get("lookup.mine.slayer-completed-format", new { kills = k, goal = g }).ToString() : ModEntry.I18n.Get("lookup.mine.slayer-progress-format", new { kills = k, goal = g, remaining = g - k }).ToString();
                    section.Fields.Add(new LookupField(
                        $"• {GetLocalizedMonsterCategory(cat)}",
                        status,
                        c ? new Color(0, 140, 0) : Game1.textColor
                    ));
                }
            }
            catch { }

            return section;
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
            bool includeRecipes = cat == "All" || cat == "Recipes";
            bool includeLocations = cat == "All" || cat == "Locations";

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
                                subtitle: ModEntry.I18n.Get("lookup.search.sub.villager").ToString(),
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
                                string catName = !string.IsNullOrEmpty(data.ObjectType) ? data.ObjectType : ModEntry.I18n.Get("lookup.search.sub.item").ToString();
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
                                    subtitle: ModEntry.I18n.Get("lookup.search.sub.monster").ToString(),
                                    textColor: new Color(200, 60, 20),
                                    icon: null,
                                    iconSourceRect: null,
                                    onClick: () =>
                                    {
                                        var mSubject = new LookupSubject
                                        {
                                            Title = mName,
                                            Subtitle = ModEntry.I18n.Get("lookup.type.monster").ToString()
                                        };
                                        var mSection = new LookupSection(ModEntry.I18n.Get("lookup.section.combat"));
                                        string[] parts = monsterData.Split('/');
                                        if (parts.Length > 0) mSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.health"), parts[0], new Color(220, 20, 60)));
                                        if (parts.Length > 1) mSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.damage"), parts[1], new Color(200, 60, 20)));
                                        if (parts.Length > 7) mSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.weapon.defense"), parts[7], new Color(20, 110, 220)));
                                        if (parts.Length > 8) mSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.experience"), parts[8], new Color(180, 100, 0)));
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
                                    subtitle: ModEntry.I18n.Get("lookup.search.sub.building").ToString(),
                                    textColor: new Color(180, 100, 0),
                                    icon: null,
                                    iconSourceRect: null,
                                    onClick: () =>
                                    {
                                        var bSubject = new LookupSubject
                                        {
                                            Title = bName,
                                            Subtitle = ModEntry.I18n.Get("lookup.type.building").ToString()
                                        };
                                        var bSection = new LookupSection(ModEntry.I18n.Get("lookup.section.building-details"));
                                        if (!string.IsNullOrEmpty(bData.Description)) bSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.description"), bData.Description, Color.DarkSlateGray));
                                        if (bData.BuildCost > 0) bSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.building.build-cost"), $"{bData.BuildCost}g", new Color(180, 100, 0)));
                                        if (bData.BuildMaterials != null && bData.BuildMaterials.Count > 0)
                                        {
                                            var matStrs = bData.BuildMaterials.Select(m => {
                                                var mItem = ItemRegistry.GetData(m.ItemId);
                                                return $"{mItem?.DisplayName ?? m.ItemId} (x{m.Amount})";
                                            });
                                            bSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.building.materials"), string.Join(", ", matStrs), Game1.textColor));
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

            // 5. Search Crafting & Cooking Recipes
            if (includeRecipes)
            {
                try
                {
                    foreach (var kvp in CraftingRecipe.craftingRecipes)
                    {
                        string recipeKey = kvp.Key;
                        var recipe = new CraftingRecipe(recipeKey, isCookingRecipe: false);
                        string rName = recipe.DisplayName ?? recipeKey;
                        if (rName.ToLower().Contains(q) && !results.Any(r => r.Text == rName))
                        {
                            var outData = ItemRegistry.GetData(recipe.createItem()?.QualifiedItemId ?? "");
                            results.Add(new LookupLink(
                                text: rName,
                                subtitle: ModEntry.I18n.Get("lookup.search.sub.crafting-recipe").ToString(),
                                textColor: new Color(180, 100, 0),
                                icon: outData?.GetTexture(),
                                iconSourceRect: outData?.GetSourceRect(),
                                onClick: () =>
                                {
                                    var rSubject = new LookupSubject
                                    {
                                        Title = rName,
                                        Subtitle = ModEntry.I18n.Get("lookup.type.crafting-recipe").ToString()
                                    };
                                    var rSec = new LookupSection(ModEntry.I18n.Get("lookup.section.recipe-requirements"));
                                    rSec.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recipe.crafting-station"), ModEntry.I18n.Get("lookup.recipe.station-inventory").ToString(), new Color(20, 110, 220)));
                                    rSec.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recipe.unlocked"), Game1.player.craftingRecipes.ContainsKey(recipeKey) ? ModEntry.I18n.Get("lookup.recipe.known").ToString() : ModEntry.I18n.Get("lookup.recipe.not-learned").ToString(), Game1.player.craftingRecipes.ContainsKey(recipeKey) ? new Color(0, 140, 0) : Color.DarkSlateGray));
                                    rSec.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recycling.yields"), $"{recipe.numberProducedPerCraft}x {rName}", new Color(0, 140, 0)));

                                    var ingLinks = new List<LookupLink>();
                                    foreach (var ing in recipe.recipeList)
                                    {
                                        string ingId = ing.Key;
                                        int count = ing.Value;
                                        var ingData = ItemRegistry.GetData(ingId) ?? ItemRegistry.GetData($"(O){ingId}");
                                        string ingName = ingData?.DisplayName ?? ingId;
                                        ingLinks.Add(new LookupLink(
                                            text: $"{count}x {ingName}",
                                            textColor: Game1.textColor,
                                            icon: ingData?.GetTexture(),
                                            iconSourceRect: ingData?.GetSourceRect(),
                                            onClick: () =>
                                            {
                                                var itm = ItemRegistry.Create(ingData?.QualifiedItemId ?? ingId);
                                                return itm != null ? BuildItemSubject(itm) : null;
                                            }
                                        ));
                                    }
                                    rSec.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recipe.ingredients"), ingLinks));
                                    rSubject.Sections.Add(rSec);
                                    return rSubject;
                                }
                            ));
                            if (results.Count >= 50) break;
                        }
                    }

                    foreach (var kvp in CraftingRecipe.cookingRecipes)
                    {
                        string recipeKey = kvp.Key;
                        var recipe = new CraftingRecipe(recipeKey, isCookingRecipe: true);
                        string rName = recipe.DisplayName ?? recipeKey;
                        if (rName.ToLower().Contains(q) && !results.Any(r => r.Text == rName))
                        {
                            var outData = ItemRegistry.GetData(recipe.createItem()?.QualifiedItemId ?? "");
                            results.Add(new LookupLink(
                                text: rName,
                                subtitle: ModEntry.I18n.Get("lookup.search.sub.cooking-recipe").ToString(),
                                textColor: new Color(180, 50, 180),
                                icon: outData?.GetTexture(),
                                iconSourceRect: outData?.GetSourceRect(),
                                onClick: () =>
                                {
                                    var rSubject = new LookupSubject
                                    {
                                        Title = rName,
                                        Subtitle = ModEntry.I18n.Get("lookup.type.cooking-recipe").ToString()
                                    };
                                    var rSec = new LookupSection(ModEntry.I18n.Get("lookup.section.recipe-requirements"));
                                    rSec.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recipe.station"), ModEntry.I18n.Get("lookup.recipe.station-kitchen").ToString(), new Color(20, 110, 220)));
                                    rSec.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recipe.unlocked"), Game1.player.cookingRecipes.ContainsKey(recipeKey) ? ModEntry.I18n.Get("lookup.recipe.known").ToString() : ModEntry.I18n.Get("lookup.recipe.not-learned").ToString(), Game1.player.cookingRecipes.ContainsKey(recipeKey) ? new Color(0, 140, 0) : Color.DarkSlateGray));
                                    rSec.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recipe.times-cooked"), ModEntry.I18n.Get("lookup.recipe.times-cooked-format", new { count = Game1.player.recipesCooked.GetValueOrDefault(recipe.createItem()?.ItemId ?? recipeKey, 0) }).ToString(), new Color(0, 140, 0)));

                                    var ingLinks = new List<LookupLink>();
                                    foreach (var ing in recipe.recipeList)
                                    {
                                        string ingId = ing.Key;
                                        int count = ing.Value;
                                        var ingData = ItemRegistry.GetData(ingId) ?? ItemRegistry.GetData($"(O){ingId}");
                                        string ingName = ingData?.DisplayName ?? ingId;
                                        ingLinks.Add(new LookupLink(
                                            text: $"{count}x {ingName}",
                                            textColor: Game1.textColor,
                                            icon: ingData?.GetTexture(),
                                            iconSourceRect: ingData?.GetSourceRect(),
                                            onClick: () =>
                                            {
                                                var itm = ItemRegistry.Create(ingData?.QualifiedItemId ?? ingId);
                                                return itm != null ? BuildItemSubject(itm) : null;
                                            }
                                        ));
                                    }
                                    rSec.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.recipe.ingredients"), ingLinks));
                                    rSubject.Sections.Add(rSec);
                                    return rSubject;
                                }
                            ));
                            if (results.Count >= 50) break;
                        }
                    }
                }
                catch { }
            }

            // 6. Search Locations
            if (includeLocations)
            {
                try
                {
                    foreach (var loc in Game1.locations)
                    {
                        if (loc == null || string.IsNullOrEmpty(loc.Name)) continue;
                        string lName = loc.DisplayName ?? loc.Name;
                        if (lName.ToLower().Contains(q) && !results.Any(r => r.Text == lName))
                        {
                            var targetLoc = loc;
                            results.Add(new LookupLink(
                                text: lName,
                                subtitle: ModEntry.I18n.Get("lookup.search.sub.location").ToString(),
                                textColor: new Color(46, 125, 50),
                                icon: null,
                                iconSourceRect: null,
                                onClick: () => BuildWorldOverviewSubject(targetLoc, null)
                            ));
                            if (results.Count >= 50) break;
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
