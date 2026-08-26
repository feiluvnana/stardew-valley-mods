using System.Globalization;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Network;
using StardewValley.TokenizableStrings;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace BetterQOL
{
    /// <summary>
    /// Global live search engine and subject query dispatcher for Lookup Anything.
    /// </summary>
    /// <remarks>
    /// BEGINNER NOTES:
    /// - SearchAll scans several game data sources (villagers, all items, monsters, buildings,
    ///   recipes, map locations) and collects matching LookupLink results for the live-search box.
    /// - Every result carries a LAZY callback: the detailed card is only constructed if/when
    ///   the player clicks, which keeps big searches fast.
    /// - Mixed tabs/spaces and "//IL_xxxx:" markers below are decompiler leftovers, kept
    ///   untouched deliberately.
    /// </remarks>
    public static partial class LookupDataManager
    {
		/// <summary>
		/// Strips diacritics (accents) and decomposes combined characters (including Vietnamese đ/Đ)
		/// for culture-insensitive and accent-insensitive text comparison.
		/// </summary>
		public static string RemoveDiacritics(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			string normalized = text.Normalize(NormalizationForm.FormD);
			StringBuilder sb = new StringBuilder(normalized.Length);
			for (int i = 0; i < normalized.Length; i++)
			{
				char c = normalized[i];
				UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
				if (category != UnicodeCategory.NonSpacingMark)
				{
					if (c == 'đ' || c == '₫')
					{
						sb.Append('d');
					}
					else if (c == 'Đ')
					{
						sb.Append('D');
					}
					else
					{
						sb.Append(c);
					}
				}
			}
			return sb.ToString().Normalize(NormalizationForm.FormC);
		}

		/// <summary>
		/// Checks whether target contains cleanQuery, ignoring case and diacritics/accents.
		/// </summary>
		public static bool MatchesSearch(string? target, string cleanQuery)
		{
			if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(cleanQuery))
			{
				return false;
			}
			string cleanTarget = RemoveDiacritics(target).Trim().ToLowerInvariant();
			return cleanTarget.Contains(cleanQuery);
		}

        /// <summary>
        /// Runs a live search across every enabled category and returns up to 50 clickable
        /// result links. "category" narrows the scan ("Villagers", "Fish", "Monsters", ...);
        /// "All" enables every source at once.
        /// </summary>
        public static List<LookupLink> SearchAll(string query, string category = "All")
        {
            List<LookupLink> list = new List<LookupLink>();
            // GUARD CLAUSE: an empty search box would match literally everything, so exit early
            // and skip all of the expensive scans below.
            if (string.IsNullOrWhiteSpace(query))
            {
                return list;
            }
		// Normalize query: remove diacritics and convert to lowercase for case/accent-insensitive search.
		string cleanQuery = RemoveDiacritics(query).Trim().ToLowerInvariant();
		string text = category.Trim();
		// Each bool below answers "should we scan this source?" - true when the user picked
		// that specific category OR asked for All. The clunky switch assigning 'num' and the
		// "(byte)num != 0" test afterwards are decompiled output of a simpler
		// "category is Items/Fish/Crops?" check.
		bool flag = text == "All" || text == "Villagers" || text == "NPCs";
		int num;
		switch (text)
		{
		default:
			num = ((text == "Crops") ? 1 : 0);
			break;
		case "All":
		case "Items":
		case "Fish":
			num = 1;
			break;
		}
		bool flag2 = (byte)num != 0;
		bool flag3 = text == "All" || text == "Monsters";
		bool flag4 = text == "All" || text == "Buildings";
		bool flag5 = text == "All" || text == "Recipes";
		bool flag6 = text == "All" || text == "Locations";
		// VILLAGER SCAN: getAllCharacters returns EVERYONE (monsters, pets, ...), so filters run
		// first; "continue" skips non-villagers. ".Any(...)" stops duplicate entries when the
		// same name appears twice in the world's character list.
		if (flag)
		{
			foreach (NPC allCharacter in Utility.getAllCharacters())
			{
				if (allCharacter == null || !((Character)allCharacter).IsVillager || ((Character)allCharacter).IsMonster || string.IsNullOrEmpty(((Character)allCharacter).Name))
				{
					continue;
				}
				string name = ((Character)allCharacter).displayName ?? ((Character)allCharacter).Name;
				if ((MatchesSearch(name, cleanQuery) || MatchesSearch(((Character)allCharacter).Name, cleanQuery)) && !list.Any((LookupLink r) => r.Text == name))
				{
					// Copying to 'target' matters! The click-lambda captures this VARIABLE, not the
					// loop slot - without the copy every link would open the LAST NPC found.
					NPC target = allCharacter;
					list.Add(new LookupLink(name, ((object)ModEntry.I18n.Get("lookup.search.sub.villager")).ToString(), (Color?)new Color(180, 50, 180), target.Portrait, (Rectangle?)new Rectangle(0, 0, 64, 64), (() => BuildNPCSubject(target))));
					if (list.Count >= 50)
					{
						break;
					}
				}
			}
		}
		// ITEM SCAN: walk every registered item TYPE (objects, weapons, furniture, ...) and each
		// id inside it, matching display names against the query.
		if (flag2)
		{
			foreach (IItemDataDefinition itemType in ItemRegistry.ItemTypes)
			{
				if (itemType == null)
				{
					continue;
				}
				foreach (string allId in itemType.GetAllIds())
				{
					ParsedItemData itemData = itemType.GetData(allId);
					if (itemData == null || string.IsNullOrEmpty(itemData.DisplayName) || (!MatchesSearch(itemData.DisplayName, cleanQuery) && !MatchesSearch(itemData.InternalName, cleanQuery)) || list.Any((LookupLink r) => r.Text == itemData.DisplayName))
					{
						continue;
					}
					// Category numbers are the game's internal groups: -4 = fish; -79 fruits,
					// -75 vegetables, -74 seeds. These checks only apply when the user actually
					// picked the Fish or Crops tab.
					if ((!(text == "Fish") || itemData.Category == -4) && (!(text == "Crops") || itemData.Category == -75 || itemData.Category == -79 || itemData.Category == -74))
					{
						ParsedItemData data = itemData;
						Item sampleItem = ItemRegistry.Create(data.QualifiedItemId, 1, 0, false);
						string catName = sampleItem?.getCategoryName();
						string subtitle = !string.IsNullOrWhiteSpace(catName)
							? catName
							: ((object)ModEntry.I18n.Get("lookup.search.sub.item")).ToString();

						list.Add(new LookupLink(data.DisplayName, subtitle, Game1.textColor, data.GetTexture(), data.GetSourceRect(0, null), () => { Item val = ItemRegistry.Create(data.QualifiedItemId, 1, 0, false); return (val != null) ? BuildItemSubject(val) : null; }));
						if (list.Count >= 50)
						{
							break;
						}
					}
				}
				if (list.Count < 50)
				{
					continue;
				}
				break;
			}
		}
		// MONSTER SCAN: read the game's raw Monsters data table (id -> stat text) instead of
		// spawning real monsters just to inspect them.
		if (flag3)
		{
			try
			{
				Dictionary<string, string> dictionary = DataLoader.Monsters(Game1.content);
				if (dictionary != null)
				{
					foreach (KeyValuePair<string, string> item in dictionary)
					{
						string mName = item.Key;
						if (!MatchesSearch(mName, cleanQuery) || list.Any((LookupLink r) => r.Text == mName))
						{
							continue;
						}
						string monsterData = item.Value;
						list.Add(new LookupLink(mName, ((object)ModEntry.I18n.Get("lookup.search.sub.monster")).ToString(), (Color?)new Color(200, 60, 20), null, null, delegate
						{
							//IL_008c: Unknown result type (might be due to invalid IL or missing references)
							//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
							//IL_011a: Unknown result type (might be due to invalid IL or missing references)
							//IL_0160: Unknown result type (might be due to invalid IL or missing references)
							LookupSubject lookupSubject = new LookupSubject
							{
								Title = mName,
								Subtitle = ((object)ModEntry.I18n.Get("lookup.type.monster")).ToString()
							};
							LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.combat")));
							// The monster data string is '/'-separated; indexes used here: 0 = HP,
							// 1 = damage, 7 = defense, 8 = experience. Length guards protect against
							// mod-added entries with fewer fields.
							string[] array = monsterData.Split('/');
							if (array.Length != 0)
							{
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.health")), array[0], (Color?)new Color(220, 20, 60)));
							}
							if (array.Length > 1)
							{
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.damage")), array[1], (Color?)new Color(200, 60, 20)));
							}
							if (array.Length > 7)
							{
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.defense")), array[7], (Color?)new Color(20, 110, 220)));
							}
							if (array.Length > 8)
							{
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.monster.experience")), array[8], (Color?)new Color(180, 100, 0)));
							}
							lookupSubject.Sections.Add(lookupSection);
							return lookupSubject;
						}));
						if (list.Count >= 50)
						{
							break;
						}
					}
				}
			}
			catch
			{
				// Data table unavailable? Skip monsters silently.
			}
		}
		// BUILDING SCAN: same pattern - read the Buildings data table rather than walking maps.
		if (flag4)
		{
			try
			{
				Dictionary<string, BuildingData> dictionary2 = DataLoader.Buildings(Game1.content);
				if (dictionary2 != null)
				{
					foreach (KeyValuePair<string, BuildingData> item2 in dictionary2)
					{
						BuildingData bData = item2.Value;
						string bName = !string.IsNullOrEmpty(bData.Name) ? TokenParser.ParseText(bData.Name) : item2.Key;
						if ((!MatchesSearch(bName, cleanQuery) && !MatchesSearch(bData.Name, cleanQuery) && !MatchesSearch(item2.Key, cleanQuery)) || list.Any((LookupLink r) => r.Text == bName))
						{
							continue;
						}
						list.Add(new LookupLink(bName, ((object)ModEntry.I18n.Get("lookup.search.sub.building")).ToString(), (Color?)new Color(180, 100, 0), null, null, delegate
						{
							//IL_0085: Unknown result type (might be due to invalid IL or missing references)
							//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
							//IL_0194: Unknown result type (might be due to invalid IL or missing references)
							LookupSubject lookupSubject = new LookupSubject
							{
								Title = bName,
								Subtitle = ((object)ModEntry.I18n.Get("lookup.type.building")).ToString()
							};
							LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.building-details")));
							string desc = !string.IsNullOrEmpty(bData.Description) ? TokenParser.ParseText(bData.Description) : string.Empty;
							if (!string.IsNullOrEmpty(desc))
							{
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.description")), desc, Color.DarkSlateGray));
							}
							if (bData.BuildCost > 0)
							{
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.build-cost")), $"{bData.BuildCost}g", (Color?)new Color(180, 100, 0)));
							}
							if (bData.BuildMaterials != null && bData.BuildMaterials.Count > 0)
							{
								// LINQ .Select PROJECTS each material record into display text via a
								// multi-line lambda; string.Join then glues them with commas. Note
								// LINQ is LAZY - nothing executes until Join enumerates it.
								IEnumerable<string> values = bData.BuildMaterials.Select(m =>
								{
									ParsedItemData data4 = ItemRegistry.GetData(m.ItemId);
									return $"{data4?.DisplayName ?? m.ItemId} (x{m.Amount})";
								});
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.materials")), string.Join(", ", values), Game1.textColor));
							}
							lookupSubject.Sections.Add(lookupSection);
							return lookupSubject;
						}));
						if (list.Count >= 50)
						{
							break;
						}
					}
				}
			}
			catch
			{
			}
		}
		// RECIPE SCANS: craftingRecipes / cookingRecipes are static id->data dictionaries the
		// game builds once at startup. The constructor's bool picks cooking (true) or crafting.
		if (flag5)
		{
			try
			{
				foreach (KeyValuePair<string, string> craftingRecipe in CraftingRecipe.craftingRecipes)
				{
					string recipeKey = craftingRecipe.Key;
					CraftingRecipe recipe = new CraftingRecipe(recipeKey, false);
					string rName = recipe.DisplayName ?? recipeKey;
					if ((!MatchesSearch(rName, cleanQuery) && !MatchesSearch(recipeKey, cleanQuery)) || list.Any((LookupLink r) => r.Text == rName))
					{
						continue;
					}
					Item obj3 = recipe.createItem();
					ParsedItemData data2 = ItemRegistry.GetData(((obj3 != null) ? obj3.QualifiedItemId : null) ?? "");
					list.Add(new LookupLink(rName, ((object)ModEntry.I18n.Get("lookup.search.sub.crafting-recipe")).ToString(), (Color?)new Color(180, 100, 0), data2?.GetTexture(), data2?.GetSourceRect(0, null), () =>
					{
						//IL_0080: Unknown result type (might be due to invalid IL or missing references)
						//IL_0115: Unknown result type (might be due to invalid IL or missing references)
						//IL_0107: Unknown result type (might be due to invalid IL or missing references)
						//IL_0189: Unknown result type (might be due to invalid IL or missing references)
						//IL_0264: Unknown result type (might be due to invalid IL or missing references)
						//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
						LookupSubject lookupSubject = new LookupSubject
						{
							Title = rName,
							Subtitle = ((object)ModEntry.I18n.Get("lookup.type.crafting-recipe")).ToString()
						};
						LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.recipe-requirements")))
						{
							// COLLECTION-INITIALIZER inside an object initializer: "Fields = { ... }"
							// adds these rows while the section object itself is being created.
							Fields = 
							{
								new LookupField((ModEntry.I18n.Get("lookup.recipe.crafting-station")), ((object)ModEntry.I18n.Get("lookup.recipe.station-inventory")).ToString(), (Color?)new Color(20, 110, 220)),
								// ContainsKey on the player's learned-recipe dictionary answers "do I know this recipe?".
								new LookupField((ModEntry.I18n.Get("lookup.recipe.unlocked")), ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.craftingRecipes).ContainsKey(recipeKey) ? ((object)ModEntry.I18n.Get("lookup.recipe.known")).ToString() : ((object)ModEntry.I18n.Get("lookup.recipe.not-learned")).ToString(), (Color)(((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.craftingRecipes).ContainsKey(recipeKey) ? new Color(0, 140, 0) : Color.DarkSlateGray)),
								new LookupField((ModEntry.I18n.Get("lookup.recycling.yields")), $"{recipe.numberProducedPerCraft}x {rName}", (Color?)new Color(0, 140, 0))
							}
						};
						// recipeList maps ingredient id -> quantity; convert each entry into a
						// clickable link showing "3x Wood" style text.
						List<LookupLink> list2 = new List<LookupLink>();
						foreach (KeyValuePair<string, int> recipe3 in recipe.recipeList)
						{
							string ingId = recipe3.Key;
							int value2 = recipe3.Value;
							ParsedItemData ingData = ItemRegistry.GetData(ingId) ?? ItemRegistry.GetData("(O)" + ingId);
							string value3 = ingData?.DisplayName ?? ingId;
							string text2 = $"{value2}x {value3}";
							Color? textColor = Game1.textColor;
							ParsedItemData obj7 = ingData;
							Texture2D icon = (obj7?.GetTexture());
							ParsedItemData obj8 = ingData;
							list2.Add(new LookupLink(text2, null, textColor, icon, obj8?.GetSourceRect(0, null), () => { Item val = ItemRegistry.Create(ingData?.QualifiedItemId ?? ingId, 1, 0, false); return (val != null) ? BuildItemSubject(val) : null; }));
						}
						lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.recipe.ingredients")), list2));
						lookupSubject.Sections.Add(lookupSection);
						return lookupSubject;
					}));
					if (list.Count >= 50)
					{
						break;
					}
				}
				foreach (KeyValuePair<string, string> cookingRecipe in CraftingRecipe.cookingRecipes)
				{
					string recipeKey2 = cookingRecipe.Key;
					CraftingRecipe recipe2 = new CraftingRecipe(recipeKey2, true);
					string rName2 = recipe2.DisplayName ?? recipeKey2;
					if ((!MatchesSearch(rName2, cleanQuery) && !MatchesSearch(recipeKey2, cleanQuery)) || list.Any((LookupLink r) => r.Text == rName2))
					{
						continue;
					}
					Item obj4 = recipe2.createItem();
					ParsedItemData data3 = ItemRegistry.GetData(((obj4 != null) ? obj4.QualifiedItemId : null) ?? "");
					list.Add(new LookupLink(rName2, ((object)ModEntry.I18n.Get("lookup.search.sub.cooking-recipe")).ToString(), (Color?)new Color(180, 50, 180), data3?.GetTexture(), data3?.GetSourceRect(0, null), () =>
					{
						//IL_0080: Unknown result type (might be due to invalid IL or missing references)
						//IL_0115: Unknown result type (might be due to invalid IL or missing references)
						//IL_0107: Unknown result type (might be due to invalid IL or missing references)
						//IL_0195: Unknown result type (might be due to invalid IL or missing references)
						//IL_026f: Unknown result type (might be due to invalid IL or missing references)
						//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
						LookupSubject lookupSubject = new LookupSubject
						{
							Title = rName2,
							Subtitle = ((object)ModEntry.I18n.Get("lookup.type.cooking-recipe")).ToString()
						};
						LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.recipe-requirements")))
						{
							Fields = 
							{
								new LookupField((ModEntry.I18n.Get("lookup.recipe.station")), ((object)ModEntry.I18n.Get("lookup.recipe.station-kitchen")).ToString(), (Color?)new Color(20, 110, 220)),
								new LookupField((ModEntry.I18n.Get("lookup.recipe.unlocked")), ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.cookingRecipes).ContainsKey(recipeKey2) ? ((object)ModEntry.I18n.Get("lookup.recipe.known")).ToString() : ((object)ModEntry.I18n.Get("lookup.recipe.not-learned")).ToString(), (Color)(((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.cookingRecipes).ContainsKey(recipeKey2) ? new Color(0, 140, 0) : Color.DarkSlateGray))
							}
						};
						// GetValueOrDefault looks up "how many times cooked" and returns 0 for dishes
						// never made, avoiding a KeyNotFoundException.
						List<LookupField> fields = lookupSection.Fields;
						string label = (ModEntry.I18n.Get("lookup.recipe.times-cooked"));
						ITranslationHelper i18n = ModEntry.I18n;
						NetStringDictionary<int, NetInt> recipesCooked = Game1.player.recipesCooked;
						Item obj7 = recipe2.createItem();
						fields.Add(new LookupField(label, ((object)i18n.Get("lookup.recipe.times-cooked-format", (object)new
						{
							count = ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)recipesCooked).GetValueOrDefault(((obj7 != null) ? obj7.ItemId : null) ?? recipeKey2, 0)
						})).ToString(), (Color?)new Color(0, 140, 0)));
						List<LookupLink> list2 = new List<LookupLink>();
						foreach (KeyValuePair<string, int> recipe4 in recipe2.recipeList)
						{
							string ingId = recipe4.Key;
							int value2 = recipe4.Value;
							ParsedItemData ingData = ItemRegistry.GetData(ingId) ?? ItemRegistry.GetData("(O)" + ingId);
							string value3 = ingData?.DisplayName ?? ingId;
							string text2 = $"{value2}x {value3}";
							Color? textColor = Game1.textColor;
							ParsedItemData obj8 = ingData;
							Texture2D icon = (obj8?.GetTexture());
							ParsedItemData obj9 = ingData;
							list2.Add(new LookupLink(text2, null, textColor, icon, obj9?.GetSourceRect(0, null), () => { Item val = ItemRegistry.Create(ingData?.QualifiedItemId ?? ingId, 1, 0, false); return (val != null) ? BuildItemSubject(val) : null; }));
						}
						lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.recipe.ingredients")), list2));
						lookupSubject.Sections.Add(lookupSection);
						return lookupSubject;
						}));
					if (list.Count >= 50)
					{
						break;
					}
				}
			}
			catch
			{
			}
		}
		// LOCATION SCAN: simple display-name match across every loaded map; clicking a result
		// opens that location's world-overview card.
		if (flag6)
		{
			try
			{
				foreach (GameLocation location in Game1.locations)
				{
					if (location == null || string.IsNullOrEmpty(location.Name))
					{
						continue;
					}
					string lName = location.DisplayName ?? location.Name;
					if ((MatchesSearch(lName, cleanQuery) || MatchesSearch(location.Name, cleanQuery)) && !list.Any((LookupLink r) => r.Text == lName))
					{
						GameLocation targetLoc = location;
						list.Add(new LookupLink(lName, ModEntry.I18n.Get("lookup.search.sub.location").ToString(), new Color(46, 125, 50), null, null, () => BuildWorldOverviewSubject(targetLoc)));
						if (list.Count >= 50)
						{
							break;
						}
					}
				}
			}
			catch
			{
			}
		}
		return list;
	}
    }
}


