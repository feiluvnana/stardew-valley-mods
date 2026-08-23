using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace BetterQOL
{
    /// <summary>
    /// Lookup builder for monsters, drop tables, spawn locations, and Monster Slayer goals.
    /// </summary>
    public static partial class LookupDataManager
    {
        #region 3. Monster Lookup

        public static LookupSubject BuildMonsterSubject(Monster monster)
        {
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = (monster.displayName ?? monster.Name),
                Subtitle = ModEntry.I18n.Get("hover.type.monster").ToString()
            };
            if (monster.Sprite?.Texture != null)
            {
                lookupSubject.MainIcon = monster.Sprite.Texture;
                lookupSubject.MainIconSourceRect = monster.Sprite.SourceRect;
            }
            LookupSection statsSection = new LookupSection(ModEntry.I18n.Get("lookup.section.stats"));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.hp"), $"{monster.Health} / {monster.MaxHealth}", new Color(220, 20, 60)));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.damage"), $"{monster.DamageToFarmer}", new Color(220, 100, 20)));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.resilience"), $"{monster.resilience.Value}", new Color(20, 110, 220)));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.speed"), $"{monster.speed}", Game1.textColor));
            statsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.exp"), $"{monster.ExperienceGained}", new Color(180, 50, 180)));
            lookupSubject.Sections.Add(statsSection);

            var (category, currentKills, requiredGoal, isCompleted) = GetMonsterSlayerProgress(monster.Name);
            if (!string.IsNullOrEmpty(category) && requiredGoal > 0)
            {
                LookupSection slayerSection = new LookupSection(ModEntry.I18n.Get("lookup.section.slayer"));
                slayerSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.slayer-category"), GetLocalizedMonsterCategory(category), Game1.textColor));
                string progress = isCompleted ? ModEntry.I18n.Get("lookup.monster.slayer-done", new { count = currentKills, goal = requiredGoal }).ToString() : ModEntry.I18n.Get("lookup.monster.slayer-progress", new { count = currentKills, goal = requiredGoal }).ToString();
                slayerSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.slayer-goal"), progress, isCompleted ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                lookupSubject.Sections.Add(slayerSection);
            }

            string spawnLocations = GetMonsterSpawnLocations(monster.Name);
            if (!string.IsNullOrEmpty(spawnLocations))
            {
                LookupSection locSection = new LookupSection(ModEntry.I18n.Get("lookup.section.location"));
                locSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.spawn-location"), spawnLocations, new Color(20, 110, 220)));
                lookupSubject.Sections.Add(locSection);
            }

            List<LookupLink> dropLinks = GetMonsterDropLinks(monster);
            if (dropLinks.Count > 0)
            {
                LookupSection dropsSection = new LookupSection(ModEntry.I18n.Get("lookup.section.drops"));
                dropsSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.monster.drops"), dropLinks));
                lookupSubject.Sections.Add(dropsSection);
            }

            return lookupSubject;
        }

        private static string GetLocalizedMonsterCategory(string category) => category switch
        {
            "Slimes" => ModEntry.I18n.Get("lookup.slayer.slimes").ToString(),
            "Void Spirits" => ModEntry.I18n.Get("lookup.slayer.void-spirits").ToString(),
            "Bats" => ModEntry.I18n.Get("lookup.slayer.bats").ToString(),
            "Skeletons" => ModEntry.I18n.Get("lookup.slayer.skeletons").ToString(),
            "Cave Insects" => ModEntry.I18n.Get("lookup.slayer.cave-insects").ToString(),
            "Duggies" => ModEntry.I18n.Get("lookup.slayer.duggies").ToString(),
            "Dust Sprites" => ModEntry.I18n.Get("lookup.slayer.dust-sprites").ToString(),
            "Rock Crabs" => ModEntry.I18n.Get("lookup.slayer.rock-crabs").ToString(),
            "Mummies" => ModEntry.I18n.Get("lookup.slayer.mummies").ToString(),
            "Pepper Rex" => ModEntry.I18n.Get("lookup.slayer.pepper-rex").ToString(),
            "Serpents" => ModEntry.I18n.Get("lookup.slayer.serpents").ToString(),
            "Magma Sprites" => ModEntry.I18n.Get("lookup.slayer.magma-sprites").ToString(),
            _ => category
        };

        private static (string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) GetMonsterSlayerProgress(string monsterName)
        {
            try
            {
                string text = monsterName.ToLower();
                if (text.Contains("magma sprite") || text.Contains("magma sparker"))
                {
                    int kills = Game1.stats.getMonstersKilled("Magma Sprite") + Game1.stats.getMonstersKilled("Magma Sparker");
                    return (Category: "Magma Sprites", CurrentKills: kills, RequiredGoal: 150, IsCompleted: kills >= 150);
                }
                if (text.Contains("slime") || text.Contains("sludge"))
                {
                    int kills = Game1.stats.getMonstersKilled("Green Slime") + Game1.stats.getMonstersKilled("Frost Jelly") + Game1.stats.getMonstersKilled("Sludge") + Game1.stats.getMonstersKilled("Tiger Slime");
                    return (Category: "Slimes", CurrentKills: kills, RequiredGoal: 1000, IsCompleted: kills >= 1000);
                }
                if (text.Contains("shadow") || text.Contains("void"))
                {
                    int kills = Game1.stats.getMonstersKilled("Shadow Brute") + Game1.stats.getMonstersKilled("Shadow Shaman") + Game1.stats.getMonstersKilled("Shadow Sniper");
                    return (Category: "Void Spirits", CurrentKills: kills, RequiredGoal: 150, IsCompleted: kills >= 150);
                }
                if (text.Contains("bat"))
                {
                    int kills = Game1.stats.getMonstersKilled("Bat") + Game1.stats.getMonstersKilled("Frost Bat") + Game1.stats.getMonstersKilled("Lava Bat") + Game1.stats.getMonstersKilled("Iridium Bat");
                    return (Category: "Bats", CurrentKills: kills, RequiredGoal: 200, IsCompleted: kills >= 200);
                }
                if (text.Contains("dust"))
                {
                    int kills = Game1.stats.getMonstersKilled("Dust Spirit");
                    return (Category: "Dust Sprites", CurrentKills: kills, RequiredGoal: 500, IsCompleted: kills >= 500);
                }
                if (text.Contains("skeleton"))
                {
                    int kills = Game1.stats.getMonstersKilled("Skeleton") + Game1.stats.getMonstersKilled("Skeleton Mage");
                    return (Category: "Skeletons", CurrentKills: kills, RequiredGoal: 50, IsCompleted: kills >= 50);
                }
                if (text.Contains("bug") || text.Contains("fly") || text.Contains("grub"))
                {
                    int kills = Game1.stats.getMonstersKilled("Cave Fly") + Game1.stats.getMonstersKilled("Grub") + Game1.stats.getMonstersKilled("Bug") + Game1.stats.getMonstersKilled("Mutant Fly") + Game1.stats.getMonstersKilled("Mutant Grub");
                    return (Category: "Cave Insects", CurrentKills: kills, RequiredGoal: 125, IsCompleted: kills >= 125);
                }
                if (text.Contains("duggy"))
                {
                    int kills = Game1.stats.getMonstersKilled("Duggy") + Game1.stats.getMonstersKilled("Magma Duggy");
                    return (Category: "Duggies", CurrentKills: kills, RequiredGoal: 30, IsCompleted: kills >= 30);
                }
                if (text.Contains("crab"))
                {
                    int kills = Game1.stats.getMonstersKilled("Rock Crab") + Game1.stats.getMonstersKilled("Lava Crab") + Game1.stats.getMonstersKilled("Iridium Crab");
                    return (Category: "Rock Crabs", CurrentKills: kills, RequiredGoal: 60, IsCompleted: kills >= 60);
                }
                if (text.Contains("mummy"))
                {
                    int kills = Game1.stats.getMonstersKilled("Mummy");
                    return (Category: "Mummies", CurrentKills: kills, RequiredGoal: 100, IsCompleted: kills >= 100);
                }
                if (text.Contains("pepper") || text.Contains("rex") || text.Contains("dinosaur"))
                {
                    int kills = Game1.stats.getMonstersKilled("Pepper Rex");
                    return (Category: "Pepper Rex", CurrentKills: kills, RequiredGoal: 50, IsCompleted: kills >= 50);
                }
                if (text.Contains("serpent"))
                {
                    int kills = Game1.stats.getMonstersKilled("Serpent") + Game1.stats.getMonstersKilled("Royal Serpent");
                    return (Category: "Serpents", CurrentKills: kills, RequiredGoal: 250, IsCompleted: kills >= 250);
                }
            }
            catch
            {
            }
            int defaultKills = Game1.stats.getMonstersKilled(monsterName);
            return (Category: monsterName, CurrentKills: defaultKills, RequiredGoal: 0, IsCompleted: false);
        }

        private static string GetMonsterSpawnLocations(string monsterName)
        {
            string text = monsterName.ToLower();
            if (text.Contains("magma sprite") || text.Contains("sparker"))
            {
                return ModEntry.I18n.Get("lookup.spawn.volcano-dungeon").ToString();
            }
            if (text.Contains("green slime"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-1-39-secret").ToString();
            }
            if (text.Contains("frost jelly"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-41-79").ToString();
            }
            if (text.Contains("sludge"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-81-119-skull").ToString();
            }
            if (text.Contains("tiger slime"))
            {
                return ModEntry.I18n.Get("lookup.spawn.ginger-island-volcano").ToString();
            }
            if (text.Contains("slime"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-all-skull-island").ToString();
            }
            if (text.Contains("bat") && text.Contains("frost"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-41-79").ToString();
            }
            if (text.Contains("bat") && text.Contains("lava"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-81-119").ToString();
            }
            if (text.Contains("bat") && text.Contains("iridium"))
            {
                return ModEntry.I18n.Get("lookup.spawn.skull-deep").ToString();
            }
            if (text.Contains("bat"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-31-119-skull").ToString();
            }
            if (text.Contains("dust"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-41-79-ice").ToString();
            }
            if (text.Contains("skeleton"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-71-79").ToString();
            }
            if (text.Contains("shadow"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-81-119").ToString();
            }
            if (text.Contains("ghost") && text.Contains("carbon"))
            {
                return ModEntry.I18n.Get("lookup.spawn.skull-carbon").ToString();
            }
            if (text.Contains("ghost"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-51-79").ToString();
            }
            if (text.Contains("rock crab"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-1-29").ToString();
            }
            if (text.Contains("lava crab"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-81-119").ToString();
            }
            if (text.Contains("iridium crab"))
            {
                return ModEntry.I18n.Get("lookup.spawn.skull-carbon").ToString();
            }
            if (text.Contains("cave fly") || text.Contains("grub") || text.Contains("bug"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-1-29-bug").ToString();
            }
            if (text.Contains("duggy") && text.Contains("magma"))
            {
                return ModEntry.I18n.Get("lookup.spawn.volcano-dungeon").ToString();
            }
            if (text.Contains("duggy"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-1-29-dirt").ToString();
            }
            if (text.Contains("squid"))
            {
                return ModEntry.I18n.Get("lookup.spawn.mines-81-119").ToString();
            }
            if (text.Contains("serpent"))
            {
                return ModEntry.I18n.Get("lookup.spawn.skull-all").ToString();
            }
            if (text.Contains("mummy"))
            {
                return ModEntry.I18n.Get("lookup.spawn.skull-mummy").ToString();
            }
            if (text.Contains("pepper") || text.Contains("rex"))
            {
                return ModEntry.I18n.Get("lookup.spawn.skull-prehistoric").ToString();
            }
            if (text.Contains("lava lurk") || text.Contains("dwarvish sentry"))
            {
                return ModEntry.I18n.Get("lookup.spawn.volcano-lava-pools").ToString();
            }
            return string.Empty;
        }

        private static List<LookupLink> GetMonsterDropLinks(Monster monster)
        {
            List<LookupLink> list = new List<LookupLink>();
            Dictionary<string, double> dictionary = new Dictionary<string, double>();
            try
            {
                Dictionary<string, string> monstersData = DataLoader.Monsters(Game1.content);
                if (monstersData != null && monstersData.TryGetValue(monster.Name, out var value) && !string.IsNullOrEmpty(value))
                {
                    string[] array = value.Split('/');
                    if (array.Length > 6 && !string.IsNullOrEmpty(array[6]))
                    {
                        string[] array2 = array[6].Split(' ');
                        for (int i = 0; i + 1 < array2.Length; i += 2)
                        {
                            string key = array2[i];
                            if (double.TryParse(array2[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                            {
                                dictionary[key] = result;
                            }
                        }
                    }
                    if (array.Length > 14 && !string.IsNullOrEmpty(array[14]))
                    {
                        string[] array3 = array[14].Split(' ');
                        for (int j = 0; j + 1 < array3.Length; j += 2)
                        {
                            string key2 = array3[j];
                            if (double.TryParse(array3[j + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out var result2))
                            {
                                dictionary[key2] = result2;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            HashSet<string> hashSet = new HashSet<string>();
            foreach (string text in monster.objectsToDrop)
            {
                if (!hashSet.Add(text))
                {
                    continue;
                }
                ParsedItemData dropData = ItemRegistry.GetData(text) ?? ItemRegistry.GetData("(O)" + text);
                if (dropData != null)
                {
                    string text2 = "";
                    if (dictionary.TryGetValue(text, out var value2))
                    {
                        text2 = (value2 >= 1.0) ? " (100%)" : ((value2 >= 0.01) ? $" ({value2 * 100.0:0.#}%)" : $" ({value2 * 100.0:0.00}%)");
                    }
                    list.Add(new LookupLink(dropData.DisplayName + text2, null, Game1.textColor, dropData.GetTexture(), dropData.GetSourceRect(0, null), () =>
                    {
                        Item val = ItemRegistry.Create(dropData.QualifiedItemId, 1, 0, false);
                        return (val != null) ? BuildItemSubject(val) : null;
                    }));
                }
            }
            foreach (KeyValuePair<string, double> item in dictionary)
            {
                string key3 = item.Key;
                if (!hashSet.Add(key3))
                {
                    continue;
                }
                ParsedItemData dropData2 = ItemRegistry.GetData(key3) ?? ItemRegistry.GetData("(O)" + key3);
                if (dropData2 != null)
                {
                    double value3 = item.Value;
                    string text3 = (value3 >= 1.0) ? " (100%)" : ((value3 >= 0.01) ? $" ({value3 * 100.0:0.#}%)" : $" ({value3 * 100.0:0.00}%)");
                    list.Add(new LookupLink(dropData2.DisplayName + text3, null, Game1.textColor, dropData2.GetTexture(), dropData2.GetSourceRect(0, null), () =>
                    {
                        Item val = ItemRegistry.Create(dropData2.QualifiedItemId, 1, 0, false);
                        return (val != null) ? BuildItemSubject(val) : null;
                    }));
                }
            }
            return list;
        }

        #endregion
    }
}


