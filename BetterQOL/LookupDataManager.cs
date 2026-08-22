using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace BetterQOL
{
    public class LookupField
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public Color ValueColor { get; set; } = Game1.textColor;
        public Texture2D? Icon { get; set; }
        public Rectangle? IconSourceRect { get; set; }

        public LookupField(string label, string value, Color? valueColor = null)
        {
            Label = label;
            Value = value;
            ValueColor = valueColor ?? Game1.textColor;
        }

        public LookupField(string label, string value, Color valueColor, Texture2D? icon, Rectangle? iconSourceRect)
        {
            Label = label;
            Value = value;
            ValueColor = valueColor;
            Icon = icon;
            IconSourceRect = iconSourceRect;
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
                var (lovedItems, likedItems) = GetNPCGiftPreferences(npc);

                if (lovedItems.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.npc.loved-gifts"),
                        string.Join(", ", lovedItems.Take(12)),
                        new Color(180, 50, 180)
                    ));
                }

                if (likedItems.Count > 0)
                {
                    giftSection.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.npc.liked-gifts"),
                        string.Join(", ", likedItems.Take(12)),
                        new Color(0, 120, 0)
                    ));
                }

                subject.Sections.Add(giftSection);
            }

            return subject;
        }

        private static (List<string> Loved, List<string> Liked) GetNPCGiftPreferences(NPC npc)
        {
            var loved = new List<string>();
            var liked = new List<string>();

            try
            {
                if (Game1.NPCGiftTastes != null && Game1.NPCGiftTastes.TryGetValue(npc.Name, out string? giftStr))
                {
                    string[] parts = giftStr.Split('/');
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        foreach (string id in parts[1].Split(' '))
                        {
                            var data = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                            if (data != null && !loved.Contains(data.DisplayName))
                            {
                                loved.Add(data.DisplayName);
                            }
                        }
                    }

                    if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3]))
                    {
                        foreach (string id in parts[3].Split(' '))
                        {
                            var data = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                            if (data != null && !liked.Contains(data.DisplayName))
                            {
                                liked.Add(data.DisplayName);
                            }
                        }
                    }
                }
            }
            catch { }

            loved.Sort();
            liked.Sort();
            return (loved, liked);
        }

        #endregion

        #region 2. Item Lookup

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

            // Section 1: Overview & Value
            var overviewSection = new LookupSection(ModEntry.I18n.Get("lookup.section.overview"));
            string desc = item.getDescription();
            if (!string.IsNullOrEmpty(desc))
            {
                overviewSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.description"), desc, Color.DarkSlateGray));
            }

            int sellPrice = item.sellToStorePrice();
            if (sellPrice > 0)
            {
                overviewSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.item.sell-price"), $"{sellPrice}g", new Color(180, 100, 0)));
            }

            if (item is StardewValley.Object sObj && sObj.Edibility > -300)
            {
                int energy = sObj.staminaRecoveredOnConsumption();
                int health = sObj.healthRecoveredOnConsumption();
                overviewSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.item.edibility"),
                    $"+{energy} {ModEntry.I18n.Get("lookup.item.energy")}, +{health} {ModEntry.I18n.Get("lookup.item.health")}",
                    new Color(0, 140, 0)
                ));
            }
            subject.Sections.Add(overviewSection);

            // Section 2: Museum & Bundles
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

            // Section 3: Recipes Using This Item
            if (ModEntry.Config.ShowItemRecipes)
            {
                var recipes = GetRecipesUsingItem(item);
                if (recipes.Count > 0)
                {
                    var recipeSection = new LookupSection(ModEntry.I18n.Get("lookup.section.recipes"));
                    recipeSection.Fields.Add(new LookupField(
                        ModEntry.I18n.Get("lookup.item.used-in-recipes"),
                        string.Join(", ", recipes.Take(15)),
                        Game1.textColor
                    ));
                    subject.Sections.Add(recipeSection);
                }
            }

            return subject;
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

        private static List<string> GetRecipesUsingItem(Item item)
        {
            var recipes = new List<string>();

            foreach (var kvp in CraftingRecipe.craftingRecipes)
            {
                string recipeName = kvp.Key;
                string recipeStr = kvp.Value;
                if (RecipeContainsItem(recipeStr, item) && !recipes.Contains(recipeName))
                {
                    recipes.Add(recipeName);
                }
            }

            foreach (var kvp in CraftingRecipe.cookingRecipes)
            {
                string recipeName = kvp.Key;
                string recipeStr = kvp.Value;
                if (RecipeContainsItem(recipeStr, item) && !recipes.Contains(recipeName))
                {
                    recipes.Add(recipeName);
                }
            }

            recipes.Sort();
            return recipes;
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
            var dropNames = new List<string>();
            foreach (var dropId in monster.objectsToDrop)
            {
                var dropData = ItemRegistry.GetData(dropId) ?? ItemRegistry.GetData($"(O){dropId}");
                if (dropData != null && !dropNames.Contains(dropData.DisplayName))
                {
                    dropNames.Add(dropData.DisplayName);
                }
            }

            if (dropNames.Count > 0)
            {
                dropsSection.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.monster.possible-drops"),
                    string.Join(", ", dropNames),
                    Game1.textColor
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
                section.Fields.Add(new LookupField(
                    ModEntry.I18n.Get("lookup.building.quest-item"),
                    $"{pond.neededItemCount.Value}x {pond.neededItem.Value.DisplayName}",
                    new Color(200, 60, 20)
                ));
            }

            subject.Sections.Add(section);
            return subject;
        }

        public static LookupSubject BuildTileSubject(GameLocation location, Vector2 tilePos)
        {
            var subject = new LookupSubject
            {
                Title = $"{location.DisplayName ?? location.Name} ({tilePos.X}, {tilePos.Y})",
                Subtitle = "Tile Location"
            };

            var section = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            section.Fields.Add(new LookupField("Location", location.DisplayName ?? location.Name, new Color(20, 110, 220)));
            section.Fields.Add(new LookupField("Tile Position", $"X: {tilePos.X}, Y: {tilePos.Y}", Game1.textColor));
            section.Fields.Add(new LookupField("Weather", location.IsRainingHere() ? "Raining" : "Sunny", location.IsRainingHere() ? new Color(20, 110, 220) : new Color(180, 100, 0)));

            subject.Sections.Add(section);
            return subject;
        }

        #endregion
    }
}
