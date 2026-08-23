using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Buffs;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Crafting;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FishPonds;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Objects;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Pathfinding;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SObject = StardewValley.Object;

namespace BetterQOL
{
    /// <summary>
    /// Global live search engine and subject query dispatcher for Lookup Anything.
    /// </summary>
        public static partial class LookupDataManager
    {
        public static List<LookupLink> SearchAll(string query, string category = "All")
        {
            List<LookupLink> list = new List<LookupLink>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return list;
            }
		string value = query.Trim().ToLower();
		string text = category.Trim();
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
		if (flag)
		{
			foreach (NPC allCharacter in Utility.getAllCharacters())
			{
				if (allCharacter == null || !((Character)allCharacter).IsVillager || ((Character)allCharacter).IsMonster || string.IsNullOrEmpty(((Character)allCharacter).Name))
				{
					continue;
				}
				string name = ((Character)allCharacter).displayName ?? ((Character)allCharacter).Name;
				if (name.ToLower().Contains(value) && !list.Any((LookupLink r) => r.Text == name))
				{
					NPC target = allCharacter;
					list.Add(new LookupLink(name, ((object)ModEntry.I18n.Get("lookup.search.sub.villager")).ToString(), (Color?)new Color(180, 50, 180), target.Portrait, (Rectangle?)new Rectangle(0, 0, 64, 64), (() => BuildNPCSubject(target))));
				}
			}
		}
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
					if (itemData == null || string.IsNullOrEmpty(itemData.DisplayName) || !itemData.DisplayName.ToLower().Contains(value) || list.Any((LookupLink r) => r.Text == itemData.DisplayName))
					{
						continue;
					}
					if ((!(text == "Fish") || itemData.Category == -4) && (!(text == "Crops") || itemData.Category == -75 || itemData.Category == -79 || itemData.Category == -74))
					{
						ParsedItemData data = itemData;
						string subtitle = ((!string.IsNullOrEmpty(data.ObjectType)) ? data.ObjectType : ((object)ModEntry.I18n.Get("lookup.search.sub.item")).ToString());
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
						if (!mName.ToLower().Contains(value) || list.Any((LookupLink r) => r.Text == mName))
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
			}
		}
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
						string bName = bData.Name ?? item2.Key;
						if (!bName.ToLower().Contains(value) || list.Any((LookupLink r) => r.Text == bName))
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
							if (!string.IsNullOrEmpty(bData.Description))
							{
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.description")), bData.Description, Color.DarkSlateGray));
							}
							if (bData.BuildCost > 0)
							{
								lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.build-cost")), $"{bData.BuildCost}g", (Color?)new Color(180, 100, 0)));
							}
							if (bData.BuildMaterials != null && bData.BuildMaterials.Count > 0)
							{
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
		if (flag5)
		{
			try
			{
				foreach (KeyValuePair<string, string> craftingRecipe in CraftingRecipe.craftingRecipes)
				{
					string recipeKey = craftingRecipe.Key;
					CraftingRecipe recipe = new CraftingRecipe(recipeKey, false);
					string rName = recipe.DisplayName ?? recipeKey;
					if (!rName.ToLower().Contains(value) || list.Any((LookupLink r) => r.Text == rName))
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
							Fields = 
							{
								new LookupField((ModEntry.I18n.Get("lookup.recipe.crafting-station")), ((object)ModEntry.I18n.Get("lookup.recipe.station-inventory")).ToString(), (Color?)new Color(20, 110, 220)),
								new LookupField((ModEntry.I18n.Get("lookup.recipe.unlocked")), ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.craftingRecipes).ContainsKey(recipeKey) ? ((object)ModEntry.I18n.Get("lookup.recipe.known")).ToString() : ((object)ModEntry.I18n.Get("lookup.recipe.not-learned")).ToString(), (Color)(((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.craftingRecipes).ContainsKey(recipeKey) ? new Color(0, 140, 0) : Color.DarkSlateGray)),
								new LookupField((ModEntry.I18n.Get("lookup.recycling.yields")), $"{recipe.numberProducedPerCraft}x {rName}", (Color?)new Color(0, 140, 0))
							}
						};
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
					if (!rName2.ToLower().Contains(value) || list.Any((LookupLink r) => r.Text == rName2))
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
					if (lName.ToLower().Contains(value) && !list.Any((LookupLink r) => r.Text == lName))
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


