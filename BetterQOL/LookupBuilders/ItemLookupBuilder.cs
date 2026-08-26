using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Enchantments;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Buffs;
using StardewValley.GameData.Crafting;
using StardewValley.GameData.Crops;
using StardewValley.GameData.Locations;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.TokenizableStrings;
using StardewValley.Tools;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SObject = StardewValley.Object;

namespace BetterQOL
{
    /// <summary>
    /// Lookup builder for inventory items, weapons, tools, crops, fish, forage, minerals, recipes, and crafting.
    /// </summary>
    /// <remarks>
    /// BEGINNER NOTES:
    /// - BuildItemSubject is the entry point; every "Add...Section" helper appends one
    ///   optional LookupSection to the card and silently does nothing when the item isn't
    ///   relevant - that's why they all start with an early "return".
    /// - Item categories drive dispatch: -4 fish, -5 eggs, -6 milk, -7 cooking,
    ///   -12 minerals, -75 vegetables/produce, -79 fruit, -98 skill books.
    /// - Trinket stats are RE-DERIVED from item.generationSeed: the game seeds a Random
    ///   with that value at generation time, so replaying the same rolls reproduces the
    ///   exact stats this trinket has.
    /// - Raw data tables are '/'-separated strings; parsing means Split('/') plus index
    ///   guards (e.g. fish table fields: [1] difficulty, [2] behavior, [5] times, [6] seasons).
    /// - "//IL_xxxx:" markers and "if (1 == 0)" blocks are decompiler artifacts, kept as-is.
    /// </remarks>
    public static partial class LookupDataManager
    {
	/// <summary>
	/// The main item card: overview (description, edibility, sell prices by quality,
	/// how many you own) plus every optional section the helpers below decide to add.
	/// </summary>
	public static LookupSubject BuildItemSubject(Item item)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_050f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_0812: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_088e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0880: Unknown result type (might be due to invalid IL or missing references)
		LookupSubject lookupSubject = new LookupSubject
		{
			Title = item.DisplayName
		};
		ParsedItemData data = ItemRegistry.GetData(item.QualifiedItemId);
		if (data != null)
		{
			try
			{
				lookupSubject.MainIcon = data.GetTexture();
				lookupSubject.MainIconSourceRect = data.GetSourceRect(0, (int?)null);
			}
			catch
			{
			}
		}
		string categoryName = item.getCategoryName();
		lookupSubject.Subtitle = ((!string.IsNullOrEmpty(categoryName)) ? categoryName : ((object)ModEntry.I18n.Get("lookup.type.item")).ToString());
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.overview")));
		string text = data?.Description ?? string.Empty;
		if (string.IsNullOrEmpty(text) && !(item is Tool))
		{
			// getDescription() appends machine-readable lines like "\nSell Price:" and
			// "\nNeeded for:"; cut the text back to just the human description part.
			text = item.getDescription();
			int num = text.IndexOf("\nSell Price:", StringComparison.Ordinal);
			if (num >= 0)
			{
				text = text.Substring(0, num);
			}
			num = text.IndexOf("\nNeeded for:", StringComparison.Ordinal);
			if (num >= 0)
			{
				text = text.Substring(0, num);
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.description")), text.Trim(), Color.DarkSlateGray));
		}
		Item obj2 = item;
		SObject val = (SObject)(object)((obj2 is SObject) ? obj2 : null);
		// Edibility <= -300 is the game's "not edible" convention, so only show
		// energy/health recovery for items above that threshold.
		if (val != null && val.Edibility > -300)
		{
			int num2 = ((Item)val).staminaRecoveredOnConsumption();
			int num3 = ((Item)val).healthRecoveredOnConsumption();
			string value = ((num2 >= 0) ? $"+{num2}" : $"{num2}");
			string value2 = ((num3 >= 0) ? $"+{num3}" : $"{num3}");
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.edibility")), $"{value} {ModEntry.I18n.Get("lookup.item.energy")}, {value2} {ModEntry.I18n.Get("lookup.item.health")}", (Color?)((num2 >= 0) ? new Color(0, 140, 0) : new Color(200, 60, 20))));
			List<string> foodBuffs = GetFoodBuffs(item);
			if (foodBuffs.Count > 0)
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.buffs")), string.Join(", ", foodBuffs), (Color?)new Color(180, 50, 180)));
			}
		}
		int num4 = item.sellToStorePrice(-1L);
		// Quality price tiers: silver = x1.25, gold = x1.5, iridium = x2 of the base price.
		if (num4 > 0)
		{
			int value3 = (int)((double)num4 * 1.25);
			int value4 = (int)((double)num4 * 1.5);
			int value5 = (int)((double)num4 * 2.0);
			string value6 = (ModEntry.I18n.Get("hover.quality.silver"));
			string value7 = (ModEntry.I18n.Get("hover.quality.gold"));
			string value8 = (ModEntry.I18n.Get("hover.quality.iridium"));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.sell-price")), $"{num4}g ({value6}: {value3}g, {value7}: {value4}g, {value8}: {value5}g)", (Color?)new Color(180, 100, 0)));
		}
		(int InventoryCount, int StorageCount) itemOwnedCounts = GetItemOwnedCounts(item);
		int item2 = itemOwnedCounts.InventoryCount;
		int item3 = itemOwnedCounts.StorageCount;
		int num5 = item2 + item3;
		string value9 = ((num5 > 0) ? ((object)ModEntry.I18n.Get("hover.number-owned-format", (object)new
		{
			inv = item2,
			storage = item3,
			total = num5
		})).ToString() : ((object)ModEntry.I18n.Get("hover.number-owned-none")).ToString());
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.number-owned")), value9, (Color)((num5 > 0) ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSubject.Sections.Add(lookupSection);
		if (item.Category == -4 || IsFishItem(item))
		{
			AddFishDataSection(lookupSubject, item);
		}
		AddCropDataSection(lookupSubject, item);
		AddForageDataSection(lookupSubject, item);
		AddMineralAndArtifactLocationSection(lookupSubject, item);
		AddWeaponAndCombatSection(lookupSubject, item);
		if (item is Trinket || item.QualifiedItemId.StartsWith("(TR)") || IsTrinketItem(item))
		{
			AddTrinketSection(lookupSubject, item);
		}
		if (item.Category == -98 || item.ItemId.StartsWith("Book_") || item.QualifiedItemId.Contains("Book_") || item.Name.Contains("Book"))
		{
			AddSkillBookSection(lookupSubject, item);
		}
		AddToolSection(lookupSubject, item);
		AddMachineItemSection(lookupSubject, item);
		AddFruitTreeSaplingSection(lookupSubject, item);
		AddSpecialItemLoreSection(lookupSubject, item);
		AddAnimalProductProcessingSection(lookupSubject, item);
		AddGeodeAndMysteryBoxSection(lookupSubject, item);
		AddFertilizerDetailsSection(lookupSubject, item);
		AddRecyclingSection(lookupSubject, item);
		AddArtisanProductsSection(lookupSubject, item, num4);
		AddTailoringAndDyeSection(lookupSubject, item);
		AddCollectionAndPerfectionSection(lookupSubject, item);
		if (ModEntry.Config.ShowBundleAndMuseumInfo)
		{
			LookupSection lookupSection2 = new LookupSection((ModEntry.I18n.Get("lookup.section.progress")));
			Item obj3 = item;
			SObject val2 = (SObject)(object)((obj3 is SObject) ? obj3 : null);
			if ((val2 != null && (val2.Type == "Arch" || val2.Type == "Minerals")) || item.Category == -12)
			{
				bool flag = ((IEnumerable<string>)(object)((NetDictionary<Vector2, string, NetString, SerializableDictionary<Vector2, string>, NetVector2Dictionary<string, NetString>>)(object)((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.MuseumPieces).Values).Any(delegate(string v)
				{
					int result;
					if (!(v == item.ItemId) && !(v == item.QualifiedItemId))
					{
						Item obj4 = item;
						SObject val3 = (SObject)(object)((obj4 is SObject) ? obj4 : null);
						if (val3 == null || !(v == ((Item)val3).ParentSheetIndex.ToString()))
						{
							result = ((v == "(O)" + item.ItemId) ? 1 : 0);
							goto IL_0069;
						}
					}
					result = 1;
					goto IL_0069;
					IL_0069:
					return (byte)result != 0;
				});
				lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.museum")), (flag ? ModEntry.I18n.Get("lookup.item.museum-donated") : ModEntry.I18n.Get("lookup.item.museum-needed")), (Color?)(flag ? new Color(0, 140, 0) : new Color(200, 60, 20))));
			}
			List<string> neededBundles = GetNeededBundles(item);
			if (neededBundles.Count > 0)
			{
				lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.bundles")), string.Join(", ", neededBundles), (Color?)new Color(180, 50, 180)));
			}
			if (lookupSection2.Fields.Count > 0)
			{
				lookupSubject.Sections.Add(lookupSection2);
			}
		}
		if (ModEntry.Config.ShowGiftTastes)
		{
			LookupSection lookupSection3 = new LookupSection((ModEntry.I18n.Get("lookup.section.gift-tastes")));
			var (list, list2) = GetItemGiftTastesLinks(item);
			if (list.Count > 0)
			{
				lookupSection3.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.loved-by")), list));
			}
			if (list2.Count > 0)
			{
				lookupSection3.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.liked-by")), list2));
			}
			if (lookupSection3.Fields.Count > 0)
			{
				lookupSubject.Sections.Add(lookupSection3);
			}
		}
		if (ModEntry.Config.ShowItemRecipes)
		{
			List<LookupLink> recipesUsingItemLinks = GetRecipesUsingItemLinks(item);
			if (recipesUsingItemLinks.Count > 0)
			{
				LookupSection lookupSection4 = new LookupSection((ModEntry.I18n.Get("lookup.section.recipes")));
				lookupSection4.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.item.used-in-recipes")), recipesUsingItemLinks));
				lookupSubject.Sections.Add(lookupSection4);
			}
		}
		return lookupSubject;
	}

	/// <summary>
	/// Counts how many of this item the player owns: inventory vs everything in storage
	/// (chests anywhere on any map, building interiors, and the farmhouse fridge).
	/// </summary>
	private static (int InventoryCount, int StorageCount) GetItemOwnedCounts(Item item)
	{
		int num = 0;
		int num2 = 0;
		string itemId = item.ItemId;
		string qualifiedItemId = item.QualifiedItemId;
		try
		{
			foreach (Item item2 in Game1.player.Items)
			{
				if (item2 != null && (item2.ItemId == itemId || item2.QualifiedItemId == qualifiedItemId))
				{
					num += item2.Stack;
				}
			}
			foreach (GameLocation location in Game1.locations)
			{
				if (location == null)
				{
					continue;
				}
				foreach (SObject value in location.objects.Values)
				{
					Chest val = (Chest)(object)((value is Chest) ? value : null);
					if (val == null || val.Items == null)
					{
						continue;
					}
					foreach (Item item3 in val.Items)
					{
						if (item3 != null && (item3.ItemId == itemId || item3.QualifiedItemId == qualifiedItemId))
						{
							num2 += item3.Stack;
						}
					}
				}
				// The farmhouse fridge is a special chest that isn't in location.objects,
				// so it gets its own scan.
				FarmHouse val2 = (FarmHouse)(object)((location is FarmHouse) ? location : null);
				if (val2 != null && ((NetFieldBase<Chest, NetRef<Chest>>)(object)val2.fridge).Value != null && ((NetFieldBase<Chest, NetRef<Chest>>)(object)val2.fridge).Value.Items != null)
				{
					foreach (Item item4 in ((NetFieldBase<Chest, NetRef<Chest>>)(object)val2.fridge).Value.Items)
					{
						if (item4 != null && (item4.ItemId == itemId || item4.QualifiedItemId == qualifiedItemId))
						{
							num2 += item4.Stack;
						}
					}
				}
				if (location.buildings.Count <= 0)
				{
					continue;
				}
				foreach (Building building in location.buildings)
				{
					if (((NetFieldBase<GameLocation, NetRef<GameLocation>>)(object)building.indoors).Value == null)
					{
						continue;
					}
					foreach (SObject value2 in ((NetFieldBase<GameLocation, NetRef<GameLocation>>)(object)building.indoors).Value.objects.Values)
					{
						Chest val3 = (Chest)(object)((value2 is Chest) ? value2 : null);
						if (val3 == null || val3.Items == null)
						{
							continue;
						}
						foreach (Item item5 in val3.Items)
						{
							if (item5 != null && (item5.ItemId == itemId || item5.QualifiedItemId == qualifiedItemId))
							{
								num2 += item5.Stack;
							}
						}
					}
				}
			}
		}
		catch
		{
		}
		return (InventoryCount: num, StorageCount: num2);
	}

	/// <summary>Combat card for melee weapons, slingshots, boots, and rings.</summary>
	private static void AddWeaponAndCombatSection(LookupSubject subject, Item item)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_066c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0793: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_055c: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_0407: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_07e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0870: Unknown result type (might be due to invalid IL or missing references)
		//IL_08a5: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			MeleeWeapon val = (MeleeWeapon)(object)((item is MeleeWeapon) ? item : null);
			if (val != null)
			{
				LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.weapon-combat")));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.damage")), $"{((NetFieldBase<int, NetInt>)(object)val.minDamage).Value} - {((NetFieldBase<int, NetInt>)(object)val.maxDamage).Value}", (Color?)new Color(200, 60, 20)));
				// critChance is a 0..1 fraction, so x100 turns it into a percentage.
				double value = ((NetFieldBase<float, NetFloat>)(object)val.critChance).Value * 100f;
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.crit-strike")), ((object)ModEntry.I18n.Get("lookup.weapon.crit-multiplier", (object)new
				{
					chance = $"{value:0.#}",
					mult = $"{((NetFieldBase<float, NetFloat>)(object)val.critMultiplier).Value:0.#}"
				})).ToString(), (Color?)new Color(180, 50, 180)));
				if (((NetFieldBase<int, NetInt>)(object)val.speed).Value != 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.speed")), $"{((((NetFieldBase<int, NetInt>)(object)val.speed).Value > 0) ? "+" : "")}{((NetFieldBase<int, NetInt>)(object)val.speed).Value}", (Color?)((((NetFieldBase<int, NetInt>)(object)val.speed).Value > 0) ? new Color(0, 140, 0) : new Color(200, 60, 20))));
				}
				if (((NetFieldBase<int, NetInt>)(object)val.addedDefense).Value != 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.defense")), $"{((((NetFieldBase<int, NetInt>)(object)val.addedDefense).Value > 0) ? "+" : "")}{((NetFieldBase<int, NetInt>)(object)val.addedDefense).Value}", (Color?)new Color(20, 110, 220)));
				}
				if (((NetFieldBase<int, NetInt>)(object)val.addedAreaOfEffect).Value != 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.reach")), $"+{((NetFieldBase<int, NetInt>)(object)val.addedAreaOfEffect).Value}", Color.DarkSlateGray));
				}
				if (((NetFieldBase<float, NetFloat>)(object)val.knockback).Value != 1f)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.knockback")), $"{((NetFieldBase<float, NetFloat>)(object)val.knockback).Value:0.0}", Color.DarkSlateGray));
				}
				// Forging at the volcano adds one level per dwarf gem used (max 3).
				int totalForgeLevels = ((Tool)val).GetTotalForgeLevels(false);
				if (totalForgeLevels > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.volcano-forges")), ((object)ModEntry.I18n.Get("lookup.weapon.volcano-forges-level", (object)new
					{
						level = totalForgeLevels
					})).ToString(), (Color?)new Color(180, 100, 0)));
				}
				if (((Tool)val).enchantments.Count > 0)
				{
					foreach (BaseEnchantment current in ((Tool)val).enchantments)
					{
						string name = current.GetName();
						string enchantmentDescription = GetEnchantmentDescription(name);
						string value2 = ((!string.IsNullOrEmpty(enchantmentDescription)) ? (name + " — " + enchantmentDescription) : name);
						lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.enchantment")), value2, (Color?)new Color(180, 50, 180)));
					}
				}
				subject.Sections.Add(lookupSection);
				return;
			}
			Slingshot val2 = (Slingshot)(object)((item is Slingshot) ? item : null);
			if (val2 != null)
			{
				LookupSection lookupSection2 = new LookupSection((ModEntry.I18n.Get("lookup.section.slingshot")));
				bool flag = ((Item)val2).ItemId == "33" || ((Item)val2).Name.Contains("Master");
				lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.slingshot.type")), flag ? ((object)ModEntry.I18n.Get("lookup.slingshot.type.master")).ToString() : ((object)ModEntry.I18n.Get("lookup.slingshot.type.standard")).ToString(), (Color?)new Color(180, 100, 0)));
				lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.slingshot.compatible-ammo")), ((object)ModEntry.I18n.Get("lookup.slingshot.compatible-ammo-desc")).ToString(), (Color?)new Color(0, 140, 0)));
				subject.Sections.Add(lookupSection2);
				return;
			}
			Boots val3 = (Boots)(object)((item is Boots) ? item : null);
			if (val3 != null)
			{
				LookupSection lookupSection3 = new LookupSection((ModEntry.I18n.Get("lookup.section.equipment-stats")));
				if (((NetFieldBase<int, NetInt>)(object)val3.defenseBonus).Value > 0)
				{
					lookupSection3.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.defense")), $"+{((NetFieldBase<int, NetInt>)(object)val3.defenseBonus).Value}", (Color?)new Color(20, 110, 220)));
				}
				if (((NetFieldBase<int, NetInt>)(object)val3.immunityBonus).Value > 0)
				{
					lookupSection3.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.equipment.immunity")), $"+{((NetFieldBase<int, NetInt>)(object)val3.immunityBonus).Value}", (Color?)new Color(0, 140, 0)));
				}
				subject.Sections.Add(lookupSection3);
				return;
			}
			Ring val4 = (Ring)(object)((item is Ring) ? item : null);
			if (val4 == null)
			{
				return;
			}
			LookupSection lookupSection4 = new LookupSection((ModEntry.I18n.Get("lookup.section.ring-effects")));
			string ringEffectDescription = GetRingEffectDescription(((Item)val4).ItemId, ((Item)val4).DisplayName);
			if (!string.IsNullOrEmpty(ringEffectDescription))
			{
				lookupSection4.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.ring.special-effect")), ringEffectDescription, (Color?)new Color(180, 50, 180)));
			}
			CombinedRing val5 = (CombinedRing)(object)((val4 is CombinedRing) ? val4 : null);
			if (val5 != null && val5.combinedRings.Count > 0)
			{
				List<LookupLink> list = new List<LookupLink>();
				foreach (Ring subRing in val5.combinedRings)
				{
					ParsedItemData data = ItemRegistry.GetData(((Item)subRing).QualifiedItemId);
					string ringEffectDescription2 = GetRingEffectDescription(((Item)subRing).ItemId, ((Item)subRing).DisplayName);
					string text = ((!string.IsNullOrEmpty(ringEffectDescription2)) ? (((Item)subRing).DisplayName + " (" + ringEffectDescription2 + ")") : ((Item)subRing).DisplayName);
					list.Add(new LookupLink(text, null, Game1.textColor, (data != null) ? data.GetTexture() : null, (data != null) ? new Rectangle?(data.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), () => BuildItemSubject((Item)(object)subRing)));
				}
				lookupSection4.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.ring.combined-rings")), list));
			}
			subject.Sections.Add(lookupSection4);
		}
		catch
		{
		}
	}

	/// <summary>Tool card: upgrade tier, enchantments, and fishing-rod bait/tackle state.</summary>
	private static void AddToolSection(LookupSubject subject, Item item)
	{
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Tool val = (Tool)(object)((item is Tool) ? item : null);
			if (val == null || item is MeleeWeapon)
			{
				return;
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.tool-details")));
			int upgradeLevel = val.UpgradeLevel;
			if (1 == 0)
			{
			}
			string text = upgradeLevel switch
			{
				0 => ((object)ModEntry.I18n.Get("lookup.tool.level.basic")).ToString(), 
				1 => ((object)ModEntry.I18n.Get("lookup.tool.level.copper")).ToString(), 
				2 => ((object)ModEntry.I18n.Get("lookup.tool.level.steel")).ToString(), 
				3 => ((object)ModEntry.I18n.Get("lookup.tool.level.gold")).ToString(), 
				4 => ((object)ModEntry.I18n.Get("lookup.tool.level.iridium")).ToString(), 
				_ => ((object)ModEntry.I18n.Get("lookup.tool.level.basic")).ToString(), 
			};
			if (1 == 0)
			{
			}
			string value = text;
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.tool.upgrade-level")), value, (Color?)new Color(180, 100, 0)));
			if (val.enchantments.Count > 0)
			{
				foreach (BaseEnchantment current in val.enchantments)
				{
					string name = current.GetName();
					string enchantmentDescription = GetEnchantmentDescription(name);
					string value2 = ((!string.IsNullOrEmpty(enchantmentDescription)) ? (name + " — " + enchantmentDescription) : name);
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weapon.enchantment")), value2, (Color?)new Color(180, 50, 180)));
				}
			}
			FishingRod val2 = (FishingRod)(object)((val is FishingRod) ? val : null);
			if (val2 != null)
			{
				SObject bait = val2.GetBait();
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.tool.bait-attached")), (bait != null) ? $"{((Item)bait).DisplayName} (x{((Item)bait).Stack})" : ((object)ModEntry.I18n.Get("lookup.common.none")).ToString(), (Color)((bait != null) ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				List<SObject> tackle = val2.GetTackle();
				if (tackle != null && tackle.Count > 0)
				{
				// Old-style LINQ "query syntax" (from/where/select) - same as the method
				// chaining used elsewhere, just different spelling.
				IEnumerable<string> values = from t in tackle
					where t != null
						select ((object)ModEntry.I18n.Get("lookup.tool.tackle-uses", (object)new
						{
							name = ((Item)t).DisplayName,
							uses = ((NetFieldBase<int, NetInt>)(object)t.uses).Value,
							max = FishingRod.maxTackleUses
						})).ToString();
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.tool.tackles")), string.Join(", ", values), (Color?)new Color(20, 110, 220)));
				}
			}
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Human-readable explanation for a known enchantment name, or empty string.</summary>
	private static string GetEnchantmentDescription(string enchantmentName)
	{
		string text = enchantmentName.ToLower();
		if (text.Contains("crusader"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.crusader")).ToString();
		}
		if (text.Contains("vampiric"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.vampiric")).ToString();
		}
		if (text.Contains("haymaker"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.haymaker")).ToString();
		}
		if (text.Contains("artful"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.artful")).ToString();
		}
		if (text.Contains("bug killer"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.bug-killer")).ToString();
		}
		if (text.Contains("auto-hook"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.auto-hook")).ToString();
		}
		if (text.Contains("master"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.master")).ToString();
		}
		if (text.Contains("preserving"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.preserving")).ToString();
		}
		if (text.Contains("reaching"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.reaching")).ToString();
		}
		if (text.Contains("bottomless"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.bottomless")).ToString();
		}
		if (text.Contains("efficient"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.efficient")).ToString();
		}
		if (text.Contains("generous"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.generous")).ToString();
		}
		if (text.Contains("archaeologist"))
		{
			return ((object)ModEntry.I18n.Get("lookup.enchantment.archaeologist")).ToString();
		}
		return string.Empty;
	}

	/// <summary>Human-readable explanation for a known ring effect, or empty string.</summary>
	private static string GetRingEffectDescription(string ringId, string ringName)
	{
		string text = ringName.ToLower();
		if (text.Contains("glow"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.glow")).ToString();
		}
		if (text.Contains("magnet"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.magnet")).ToString();
		}
		if (text.Contains("iridium band"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.iridium-band")).ToString();
		}
		if (text.Contains("burglar"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.burglar")).ToString();
		}
		if (text.Contains("slime charmer"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.slime-charmer")).ToString();
		}
		if (text.Contains("savage"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.savage")).ToString();
		}
		if (text.Contains("vampire"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.vampire")).ToString();
		}
		if (text.Contains("crabshell"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.crabshell")).ToString();
		}
		if (text.Contains("napalm"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.napalm")).ToString();
		}
		if (text.Contains("hot java"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.hot-java")).ToString();
		}
		if (text.Contains("lucky"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.lucky")).ToString();
		}
		if (text.Contains("phoenix"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.phoenix")).ToString();
		}
		if (text.Contains("ruby"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.ruby")).ToString();
		}
		if (text.Contains("aquamarine"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.aquamarine")).ToString();
		}
		if (text.Contains("emerald"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.emerald")).ToString();
		}
		if (text.Contains("jade"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.jade")).ToString();
		}
		if (text.Contains("amethyst"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.amethyst")).ToString();
		}
		if (text.Contains("topaz"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.topaz")).ToString();
		}
		if (text.Contains("warrior"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.warrior")).ToString();
		}
		if (text.Contains("yoba"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.yoba")).ToString();
		}
		if (text.Contains("thorns"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.thorns")).ToString();
		}
		if (text.Contains("immunity"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.immunity")).ToString();
		}
		if (text.Contains("sturdy"))
		{
			return ((object)ModEntry.I18n.Get("lookup.ring.effect.sturdy")).ToString();
		}
		return string.Empty;
	}

	/// <summary>True for Trinket instances, "(TR)" ids, or known trinket id keywords.</summary>
	private static bool IsTrinketItem(Item item)
	{
		if (item is Trinket || item.QualifiedItemId.StartsWith("(TR)"))
		{
			return true;
		}
		string text = item.ItemId.ToLowerInvariant();
		return text.Contains("fairybox") || text.Contains("frogegg") || text.Contains("magicquiver") || text.Contains("goldenspur") || text.Contains("iridiumspur") || text.Contains("icerod") || text.Contains("parrotegg") || text.Contains("basiliskpaw");
	}

	/// <summary>
	/// Trinket card: re-rolls each trinket's random stats from its generationSeed so the
	/// displayed numbers match the actual item, plus BetterForge/BetterTrinket ascension info.
	/// </summary>
	private static void AddTrinketSection(LookupSubject subject, Item item)
	{
		//IL_08bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_094d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0901: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b8b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bd5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c12: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c55: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cc5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d0f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.trinket-stats")));
			string text = item.ItemId.Replace("(TR)", "").Trim().ToLowerInvariant();
			if (string.IsNullOrEmpty(text))
			{
				text = item.QualifiedItemId.Replace("(TR)", "").Trim().ToLowerInvariant();
			}
			Trinket val = (Trinket)(object)((item is Trinket) ? item : null);
			string value = ((object)ModEntry.I18n.Get("lookup.type.item")).ToString();
			string value2 = "";
			// KEY IDEA: a trinket rolls its stats from Random(generationSeed) when created.
			// Replaying the same dice sequence here reproduces THIS item's exact stats,
			// so we can show real numbers without touching the game object.
			int num = val?.generationSeed.Value ?? 0;
			Random random = Utility.CreateRandom((double)num, 0.0, 0.0, 0.0, 0.0);
			string text3;
			if (text.Contains("fairy"))
			{
				value2 = ((object)ModEntry.I18n.Get("lookup.trinket.fairy-box.range")).ToString();
				if (val != null)
				{
					// Tier ladder: each NextBool roll upgrades the level with falling odds
					// (45% for 2, then 25%, 12.5%, ~7%) - higher tiers are rarer.
					int num2 = 1;
					if (RandomExtensions.NextBool(random, 0.45))
					{
						num2 = 2;
					}
					else if (RandomExtensions.NextBool(random, 0.25))
					{
						num2 = 3;
					}
					else if (RandomExtensions.NextBool(random, 0.125))
					{
						num2 = 4;
					}
					else if (RandomExtensions.NextBool(random, 0.0675))
					{
						num2 = 5;
					}
					float value3 = (float)(5000 - num2 * 300) / 1000f;
					float value4 = 0.7f + (float)num2 * 0.1f;
					value = ((object)ModEntry.I18n.Get("lookup.trinket.fairy-box.current", (object)new
					{
						level = num2,
						interval = $"{value3:0.0}",
						power = $"{value4:0.0}"
					})).ToString();
				}
				else
				{
					value = ((object)ModEntry.I18n.Get("lookup.trinket.fairy-box.desc")).ToString();
				}
			}
			else if (text.Contains("quiver"))
			{
				value2 = ((object)ModEntry.I18n.Get("lookup.trinket.quiver.range")).ToString();
				if (val != null)
				{
					string variant = ((object)ModEntry.I18n.Get("lookup.trinket.variant.normal")).ToString();
					// Variant roll: 4% "perfect", then 10% split evenly into rapid/heavy,
					// otherwise the normal stat range.
					int num3;
					int maxDmg;
					float num4;
					if (RandomExtensions.NextBool(random, 0.04))
					{
						variant = ((object)ModEntry.I18n.Get("lookup.trinket.variant.perfect")).ToString();
						num3 = 30;
						maxDmg = 35;
						num4 = 900f;
					}
					else if (RandomExtensions.NextBool(random, 0.1))
					{
						if (RandomExtensions.NextBool(random, 0.5))
						{
							variant = ((object)ModEntry.I18n.Get("lookup.trinket.variant.rapid")).ToString();
							num3 = random.Next(10, 15) - 2;
							maxDmg = num3 + 5;
							num4 = 600 + random.Next(11) * 10;
						}
						else
						{
							variant = ((object)ModEntry.I18n.Get("lookup.trinket.variant.heavy")).ToString();
							num3 = random.Next(25, 41) - 2;
							maxDmg = num3 + 5;
							num4 = 1500 + random.Next(6) * 100;
						}
					}
					else
					{
						num3 = random.Next(15, 31) - 2;
						maxDmg = num3 + 5;
						num4 = 1100 + random.Next(11) * 100;
					}
					value = ((object)ModEntry.I18n.Get("lookup.trinket.quiver.current", (object)new
					{
						variant = variant,
						cooldown = $"{num4 / 1000f:0.00}",
						minDmg = num3,
						maxDmg = maxDmg
					})).ToString();
				}
				else
				{
					value = ((object)ModEntry.I18n.Get("lookup.trinket.quiver.desc")).ToString();
				}
			}
			else if (text.Contains("ice") || text.Contains("rod"))
			{
				value2 = ((object)ModEntry.I18n.Get("lookup.trinket.ice-rod.range")).ToString();
				if (val != null)
				{
					// Random cooldown/freeze windows; a 5% roll makes it "perfect"
					// (best fixed values instead of the random range).
					float num5 = random.Next(3000, 5001);
					int num6 = random.Next(2000, 4001);
					bool flag = false;
					if (random.NextDouble() < 0.05)
					{
						flag = true;
						num5 = 3000f;
						num6 = 4000;
					}
					string perfect = (flag ? ((object)ModEntry.I18n.Get("lookup.trinket.ice-rod.perfect-tag")).ToString() : "");
					value = ((object)ModEntry.I18n.Get("lookup.trinket.ice-rod.current", (object)new
					{
						delay = $"{num5 / 1000f:0.0}",
						freeze = $"{(float)num6 / 1000f:0.0}",
						perfect = perfect
					})).ToString();
				}
				else
				{
					value = ((object)ModEntry.I18n.Get("lookup.trinket.ice-rod.desc")).ToString();
				}
			}
			else if (text.Contains("spur") || text.Contains("golden") || text.Contains("iridium"))
			{
				value2 = ((object)ModEntry.I18n.Get("lookup.trinket.spur.range")).ToString();
				if (val != null)
				{
					int num7 = random.Next(5, 11);
					string maxTag = ((num7 == 10) ? ((object)ModEntry.I18n.Get("lookup.trinket.spur.max-tag")).ToString() : "");
					value = ((object)ModEntry.I18n.Get("lookup.trinket.spur.current", (object)new
					{
						duration = num7,
						maxTag = maxTag
					})).ToString();
				}
				else
				{
					value = ((object)ModEntry.I18n.Get("lookup.trinket.spur.desc")).ToString();
				}
			}
			else if (text.Contains("parrot"))
			{
				value2 = ((object)ModEntry.I18n.Get("lookup.trinket.parrot.range")).ToString();
				if (val != null)
				{
					int num8 = 1;
					if (RandomExtensions.NextBool(random, 0.4))
					{
						num8 = 2;
					}
					else if (RandomExtensions.NextBool(random, 0.2))
					{
						num8 = 3;
					}
					else if (RandomExtensions.NextBool(random, 0.1))
					{
						num8 = 4;
					}
					value = ((object)ModEntry.I18n.Get("lookup.trinket.parrot.current", (object)new
					{
						level = num8,
						chance = num8 * 10
					})).ToString();
				}
				else
				{
					value = ((object)ModEntry.I18n.Get("lookup.trinket.parrot.desc")).ToString();
				}
			}
			else if (text.Contains("frog"))
			{
				value2 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.range")).ToString();
				string text2 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.green")).ToString();
				if (val != null)
				{
					int num9 = random.Next(0, 8);
					if (1 == 0)
					{
					}
					switch (num9)
					{
					case 0:
						text3 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.green")).ToString();
						break;
					case 1:
						text3 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.yellow")).ToString();
						break;
					case 2:
						text3 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.red")).ToString();
						break;
					case 3:
						text3 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.blue")).ToString();
						break;
					case 4:
						text3 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.void")).ToString();
						break;
					case 5:
						text3 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.poison")).ToString();
						break;
					// Two cases share one body: prismatic frogs are twice as likely (2/8).
					case 6:
					case 7:
						text3 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.prismatic")).ToString();
						break;
					default:
						text3 = ((object)ModEntry.I18n.Get("lookup.trinket.frog.green")).ToString();
						break;
					}
					if (1 == 0)
					{
					}
					string value5 = text3;
					text2 = $"{value5}{ModEntry.I18n.Get("lookup.trinket.frog.swallows")}";
				}
				value = text2;
			}
			else if (text.Contains("basilisk") || text.Contains("paw"))
			{
				value2 = ((object)ModEntry.I18n.Get("lookup.trinket.basilisk.range")).ToString();
				value = ((object)ModEntry.I18n.Get("lookup.trinket.basilisk.desc")).ToString();
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.trinket.active-stats")), value, (Color?)new Color(0, 140, 0)));
			if (!string.IsNullOrEmpty(value2))
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.trinket.possible-ranges")), value2, Color.DarkSlateGray));
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.trinket.anvil-reforging")), ((object)ModEntry.I18n.Get("lookup.trinket.reforge-desc")).ToString(), (Color?)new Color(180, 100, 0)));
			// Ascension is a feature of this mod's companion mods (BetterForge /
			// BetterTrinket): they tag the item in modData, which we just read back.
			bool flag2 = val != null && (((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Item)val).modData).ContainsKey("feiluvnana.BetterForge/IsAscended") || ((NetDictionary<string, string, NetString, SerializableDictionary<string, string>, NetStringDictionary<string, NetString>>)(object)((Item)val).modData).ContainsKey("feiluvnana.BetterTrinket/IsAscended"));
			bool flag3 = ModEntry.ModHelper.ModRegistry.IsLoaded("feiluvnana.BetterForge");
			string text4 = text;
			if (1 == 0)
			{
			}
			if (text4.Contains("frog"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.ascension.desc.frog")).ToString();
			}
			else
			{
				string text5 = text4;
				if (text5.Contains("fairy"))
				{
					text3 = ((object)ModEntry.I18n.Get("lookup.ascension.desc.fairy")).ToString();
				}
				else
				{
					string text6 = text4;
					if (text6.Contains("parrot"))
					{
						text3 = ((object)ModEntry.I18n.Get("lookup.ascension.desc.parrot")).ToString();
					}
					else
					{
						string text7 = text4;
						if (text7.Contains("spur") || text7.Contains("golden") || text7.Contains("iridium"))
						{
							text3 = ((object)ModEntry.I18n.Get("lookup.ascension.desc.spur")).ToString();
						}
						else
						{
							string text8 = text4;
							if (text8.Contains("quiver"))
							{
								text3 = ((object)ModEntry.I18n.Get("lookup.ascension.desc.quiver")).ToString();
							}
							else
							{
								string text9 = text4;
								if (text9.Contains("ice") || text9.Contains("rod"))
								{
									text3 = ((object)ModEntry.I18n.Get("lookup.ascension.desc.ice")).ToString();
								}
								else
								{
									string text10 = text4;
									text3 = ((!text10.Contains("basilisk") && !text10.Contains("paw")) ? ((object)ModEntry.I18n.Get("lookup.ascension.desc.default")).ToString() : ((object)ModEntry.I18n.Get("lookup.ascension.desc.basilisk")).ToString());
								}
							}
						}
					}
				}
			}
			if (1 == 0)
			{
			}
			string text11 = text3;
			if (flag2)
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.ascension.status-label")), ((object)ModEntry.I18n.Get("lookup.ascension.active-desc")).ToString(), (Color?)new Color(180, 50, 180)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.ascension.luck-label")), ((object)ModEntry.I18n.Get("lookup.ascension.luck-desc")).ToString(), (Color?)new Color(0, 140, 0)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.ascension.enhanced-power")), text11, (Color?)new Color(180, 50, 180)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.ascension.source-mod")), ((object)ModEntry.I18n.Get("lookup.ascension.source-desc")).ToString(), Color.DarkSlateGray));
			}
			else
			{
				string value6 = (flag3 ? ((object)ModEntry.I18n.Get("lookup.ascension.notice-forge")).ToString() : ((object)ModEntry.I18n.Get("lookup.ascension.notice-info")).ToString());
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.ascension.section-label")), value6, (Color?)new Color(180, 100, 0)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.ascension.power-label")), ((object)ModEntry.I18n.Get("lookup.ascension.power-format", (object)new
				{
					desc = text11
				})).ToString(), Color.DarkSlateGray));
			}
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Skill-book card: what power the book grants and whether you've read it.</summary>
	private static void AddSkillBookSection(LookupSubject subject, Item item)
	{
		//IL_0611: Unknown result type (might be due to invalid IL or missing references)
		//IL_0603: Unknown result type (might be due to invalid IL or missing references)
		//IL_064f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0692: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = item.ItemId.ToLowerInvariant();
			string text2 = item.QualifiedItemId.ToLowerInvariant();
			string text3 = item.Name.ToLowerInvariant();
			string text4 = "";
			string text5 = "";
			string text6 = "";
			string text7 = "";
			if (text.Contains("book_stars") || text3.Contains("book of stars"))
			{
				text4 = "Book_Stars";
				text6 = "Grants +250 Experience Points to all 5 skills (Farming, Mining, Foraging, Fishing, Combat).";
				text7 = "+250 XP to all 5 skills on every reading.";
			}
			else if (text.Contains("book_defense") || text3.Contains("safety manual"))
			{
				text4 = "Book_Defense";
				text5 = "DwarvishSafetyManual";
				text6 = "Dwarvish Safety Manual: Take 25% less damage from bomb blasts.";
				text7 = "+100 Combat XP on repeat readings.";
			}
			else if (text.Contains("book_woodcutting") || text3.Contains("woodcutter"))
			{
				text4 = "Book_Woodcutting";
				text6 = "Woodcutter's Weekly: Grants a 5% chance to gain extra wood from chopping trees.";
				text7 = "+100 Foraging XP on repeat readings.";
			}
			else if (text.Contains("book_mining") || text3.Contains("mining monthly"))
			{
				text4 = "Book_Mining";
				text6 = "Mining Monthly: Permanently increases Mining experience gains.";
				text7 = "+100 Mining XP on repeat readings.";
			}
			else if (text.Contains("book_friendship") || text3.Contains("friendship 101"))
			{
				text4 = "Book_Friendship";
				text6 = "Friendship 101: Friendship points with villagers decay significantly slower.";
				text7 = "+100 Friendship XP on repeat readings.";
			}
			else if (text.Contains("book_speed2") || text3.Contains("way of the wind pt 2"))
			{
				text4 = "Book_Speed2";
				text6 = "Way of the Wind (Part 2): Permanently increases base walking speed by +0.25.";
				text7 = "+100 Foraging XP on repeat readings.";
			}
			else if (text.Contains("book_speed") || text3.Contains("way of the wind"))
			{
				text4 = "Book_Speed";
				text6 = "Way of the Wind (Part 1): Permanently increases base walking speed by +0.25.";
				text7 = "+100 Foraging XP on repeat readings.";
			}
			else if (text.Contains("book_wildseeds") || text3.Contains("jack be nimble"))
			{
				text4 = "Book_WildSeeds";
				text6 = "Jack Be Nimble, Jack Be Thick: Permanently increases Defense by +1.";
				text7 = "+100 Foraging XP on repeat readings.";
			}
			else if (text.Contains("book_pricecatalogue") || text3.Contains("price catalogue"))
			{
				text4 = "Book_PriceCatalogue";
				text6 = "Price Catalogue: Permanently displays item sell prices in item tooltips.";
				text7 = "+100 Experience on repeat readings.";
			}
			else if (text.Contains("book_mapping") || text3.Contains("monster compendium"))
			{
				text4 = "Book_Mapping";
				text6 = "Monster Compendium: Monsters have a 3% chance to drop double monster loot.";
				text7 = "+100 Combat XP on repeat readings.";
			}
			else if (text.Contains("book_horse") || text3.Contains("horse the book"))
			{
				text4 = "Book_Horse";
				text6 = "Horse The Book: Permanently increases riding speed by +0.5.";
				text7 = "+100 Foraging XP on repeat readings.";
			}
			else if (text.Contains("book_artifact") || text3.Contains("treasure appraisal"))
			{
				text4 = "Book_Artifact";
				text6 = "Treasure Appraisal Guide: Artifacts and dinosaur bones sell for 3x their normal price.";
				text7 = "+100 Mining XP on repeat readings.";
			}
			else if (text.Contains("book_trash") || text3.Contains("alleyway buffoon"))
			{
				text4 = "Book_Trash";
				text6 = "Alleyway Buffoon: 20% greater chance to successfully find loot when searching garbage cans.";
				text7 = "+100 Foraging XP on repeat readings.";
			}
			else if (text.Contains("book_grass") || text3.Contains("slitherlegs"))
			{
				text4 = "Book_Grass";
				text6 = "Ol' Slitherlegs: Move at full speed through tall grass without being slowed down.";
				text7 = "+100 Foraging XP on repeat readings.";
			}
			else if (text.Contains("book_bait") || text3.Contains("bait and bobber"))
			{
				text4 = "Book_Bait";
				text6 = "Bait And Bobber: Grants +1 Fishing XP every time a fish is successfully caught.";
				text7 = "+100 Fishing XP on repeat readings.";
			}
			else if (text.Contains("book_crab") || text3.Contains("art of crabbing"))
			{
				text4 = "Book_Crab";
				text6 = "Art of Crabbing: 25% chance for Crab Pots to produce double items.";
				text7 = "+100 Fishing XP on repeat readings.";
			}
			else if (text.Contains("book_roe") || text3.Contains("jewels of the sea"))
			{
				text4 = "Book_Roe";
				text6 = "Jewels of the Sea: Fishing treasure chests have a 25% chance to contain wild Roe.";
				text7 = "+100 Fishing XP on repeat readings.";
			}
			else if (text.Contains("book_diamonds") || text3.Contains("diamond hunter"))
			{
				text4 = "Book_Diamonds";
				text6 = "Diamond Hunter: Manual quarry rocks and stones have a chance to drop Diamonds.";
				text7 = "+100 Mining XP on repeat readings.";
			}
			else if (text.Contains("book_mystery") || text3.Contains("book of mysteries"))
			{
				text4 = "Book_Mystery";
				text6 = "Book of Mysteries: Significantly increases the chance to find Mystery Boxes.";
				text7 = "+100 Experience on repeat readings.";
			}
			else if (text.Contains("book_queenofsauce") || text3.Contains("queen of sauce cookbook"))
			{
				text4 = "Book_QueenOfSauce";
				text6 = "Queen of Sauce Cookbook: Instantly learns all cooking recipes from Queen of Sauce television broadcasts.";
				text7 = "Instantly unlocks all missed cooking recipes.";
			}
			else
			{
				if (!text.Contains("book_animal") && !text3.Contains("animal catalogue"))
				{
					return;
				}
				text4 = "Book_Animal";
				text6 = "Animal Catalogue: Allows shopping at Marnie's Ranch even when Marnie is away from the counter.";
				text7 = "+100 Farming XP on repeat readings.";
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.skill-books")));
			bool flag = false;
			if (!string.IsNullOrEmpty(text4) && Game1.player.stats.Get(text4) != 0)
			{
				flag = true;
			}
			if (!string.IsNullOrEmpty(text5) && (((NetHashSet<string>)(object)Game1.player.mailReceived).Contains(text5) || Game1.player.hasOrWillReceiveMail(text5)))
			{
				flag = true;
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.book.reading-status")), flag ? ((object)ModEntry.I18n.Get("lookup.book.read-done")).ToString() : ((object)ModEntry.I18n.Get("lookup.book.read-needed")).ToString(), (Color?)(flag ? new Color(0, 140, 0) : new Color(200, 60, 20))));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.book.power-granted")), text6, (Color?)new Color(180, 50, 180)));
			if (!string.IsNullOrEmpty(text7))
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.book.secondary-readings")), text7, Color.DarkSlateGray));
			}
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Collections/perfection rows: shipped, fish caught (with size), cooked, crafted.</summary>
	private static void AddCollectionAndPerfectionSection(LookupSubject subject, Item item)
	{
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			List<LookupField> list = new List<LookupField>();
			if (item.Category != -4 && item.Category != -7)
			{
				SObject val = (SObject)(object)((item is SObject) ? item : null);
				if (val != null && !((NetFieldBase<bool, NetBool>)(object)val.bigCraftable).Value)
				{
					int num = default(int);
					bool flag = ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.basicShipped).TryGetValue(item.ItemId, out num) && num > 0;
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.section.items-shipped")), flag ? ((object)ModEntry.I18n.Get("lookup.collection.shipped-done", (object)new
					{
						count = num
					})).ToString() : ((object)ModEntry.I18n.Get("lookup.collection.shipped-needed")).ToString(), (Color?)(flag ? new Color(0, 140, 0) : new Color(200, 60, 20))));
				}
			}
			if (item.Category == -4 || IsFishItem(item))
			{
				int[] array = default(int[]);
				bool flag2 = ((NetDictionary<string, int[], NetArray<int, NetInt>, SerializableDictionary<string, int[]>, NetStringIntArrayDictionary>)(object)Game1.player.fishCaught).TryGetValue(item.ItemId, out array) || ((NetDictionary<string, int[], NetArray<int, NetInt>, SerializableDictionary<string, int[]>, NetStringIntArrayDictionary>)(object)Game1.player.fishCaught).TryGetValue("(O)" + item.ItemId, out array);
				int num2 = ((flag2 && array != null && array.Length != 0) ? array[0] : 0);
				int size = ((flag2 && array != null && array.Length > 1) ? array[1] : 0);
				list.Add(new LookupField((ModEntry.I18n.Get("lookup.section.fish-caught")), (flag2 && num2 > 0) ? ((object)ModEntry.I18n.Get("lookup.collection.fish-caught-done", (object)new
				{
					count = num2,
					size = size
				})).ToString() : ((object)ModEntry.I18n.Get("lookup.collection.fish-caught-needed")).ToString(), (Color?)((flag2 && num2 > 0) ? new Color(0, 140, 0) : new Color(200, 60, 20))));
			}
			if (item.Category == -7)
			{
				int num3 = default(int);
				bool flag3 = ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.recipesCooked).TryGetValue(item.ItemId, out num3) && num3 > 0;
				list.Add(new LookupField((ModEntry.I18n.Get("lookup.section.recipes-cooked")), flag3 ? ((object)ModEntry.I18n.Get("lookup.collection.cooked-done", (object)new
				{
					count = num3
				})).ToString() : ((object)ModEntry.I18n.Get("lookup.collection.cooked-needed")).ToString(), (Color?)(flag3 ? new Color(0, 140, 0) : new Color(200, 60, 20))));
			}
			if (CraftingRecipe.craftingRecipes.ContainsKey(item.DisplayName) || CraftingRecipe.craftingRecipes.ContainsKey(item.Name))
			{
				string text = (CraftingRecipe.craftingRecipes.ContainsKey(item.DisplayName) ? item.DisplayName : item.Name);
				int num4 = default(int);
				bool flag4 = ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.craftingRecipes).TryGetValue(text, out num4) && num4 > 0;
				list.Add(new LookupField((ModEntry.I18n.Get("lookup.section.items-crafted")), flag4 ? ((object)ModEntry.I18n.Get("lookup.collection.crafted-done", (object)new
				{
					count = num4
				})).ToString() : ((object)ModEntry.I18n.Get("lookup.collection.crafted-needed")).ToString(), (Color?)(flag4 ? new Color(0, 140, 0) : new Color(200, 60, 20))));
			}
			if (list.Count > 0)
			{
				LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.collections-perfection")));
				lookupSection.Fields.AddRange(list);
				subject.Sections.Add(lookupSection);
			}
		}
		catch
		{
		}
	}

	/// <summary>True when the item's id exists in the game's Fish data table.</summary>
	private static bool IsFishItem(Item item)
	{
		try
		{
			Dictionary<string, string> dictionary = DataLoader.Fish(Game1.content);
			return dictionary != null && (dictionary.ContainsKey(item.ItemId) || dictionary.ContainsKey(item.QualifiedItemId));
		}
		catch
		{
			return false;
		}
	}

	/// <summary>Fishing card parsed from the Fish table: difficulty, behavior, seasons,
	/// times, weather, min skill, spawn locations, and fish-pond produce.</summary>
	private static void AddFishDataSection(LookupSubject subject, Item item)
	{
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0454: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0708: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Dictionary<string, string> dictionary = DataLoader.Fish(Game1.content);
			if (dictionary == null || !dictionary.TryGetValue(item.ItemId, out var value))
			{
				return;
			}
			string[] array = value.Split('/');
			if (array.Length < 7)
			{
				return;
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.fishing-details")));
			// Fish table fields by index: [1] difficulty, [2] behavior pattern,
			// [5] time windows, [6] seasons, [7] weather, [9] min fishing level.
			string diff = array[1];
			string text = ((array.Length > 2 && !string.IsNullOrEmpty(array[2])) ? array[2] : "mixed");
			string text2 = text.ToLowerInvariant();
			string behavior = ((object)ModEntry.I18n.Get("lookup.fish.behavior." + text2)).ToString();
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish.difficulty")), ((object)ModEntry.I18n.Get("lookup.fish.difficulty-format", (object)new { diff, behavior })).ToString(), (Color?)new Color(200, 60, 20)));
			if (array.Length > 6 && !string.IsNullOrWhiteSpace(array[6]))
			{
				string[] source = array[6].Split(' ', StringSplitOptions.RemoveEmptyEntries);
				IEnumerable<string> values = source.Select(delegate(string s)
				{
					string text20 = "season." + s.ToLower();
					Translation val = ModEntry.I18n.Get(text20);
					return val.HasValue() ? ((object)val).ToString() : (char.ToUpper(s[0]) + s.Substring(1));
				});
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.forage.seasons")), string.Join(", ", values), (Color?)new Color(46, 125, 50)));
			}
			if (array.Length > 5 && !string.IsNullOrWhiteSpace(array[5]))
			{
				string[] array2 = array[5].Split(' ', StringSplitOptions.RemoveEmptyEntries);
				List<string> list = new List<string>();
				// Times come as flat "start end start end" pairs, so step 2 at a time.
				for (int num = 0; num < array2.Length; num += 2)
				{
					if (num + 1 < array2.Length)
					{
						string text3 = FormatGameTime(array2[num]);
						string text4 = FormatGameTime(array2[num + 1]);
						list.Add(text3 + " – " + text4);
					}
				}
				if (list.Count > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish.time-of-day")), string.Join(", ", list), (Color?)new Color(180, 100, 0)));
				}
			}
			string text6;
			if (array.Length > 7)
			{
				string text5 = array[7].ToLower();
				if (1 == 0)
				{
				}
				text6 = ((text5 == "sunny") ? ((object)ModEntry.I18n.Get("lookup.weather.sunny")).ToString() : ((!(text5 == "rainy")) ? ((object)ModEntry.I18n.Get("lookup.common.all-weather", (object)"Any Weather")).ToString() : ((object)ModEntry.I18n.Get("lookup.weather.rainy")).ToString()));
				if (1 == 0)
				{
				}
				string value2 = text6;
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish.weather")), value2, (Color?)new Color(20, 110, 220)));
			}
			if (array.Length > 9 && int.TryParse(array[9], out var result) && result > 0)
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish.min-skill")), ((object)ModEntry.I18n.Get("lookup.fish.min-skill-level", (object)new
				{
					level = result
				})).ToString(), Color.DarkSlateGray));
			}
			List<string> fishSpawnLocations = GetFishSpawnLocations(item.ItemId);
			if (fishSpawnLocations.Count > 0)
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish.locations")), string.Join(", ", fishSpawnLocations), (Color?)new Color(20, 110, 220)));
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish.bait-maker")), ((object)ModEntry.I18n.Get("lookup.fish.bait-maker-yield", (object)new
			{
				name = item.DisplayName
			})).ToString(), (Color?)new Color(0, 140, 0)));
			// Special pond produce per species - a long nested else-if chain (the
			// decompiler's rendering of what was probably a switch on the fish name).
			string text7 = item.Name.ToLowerInvariant();
			string text8 = text7;
			if (1 == 0)
			{
			}
			if (text8.Contains("sturgeon"))
			{
				text6 = ((object)ModEntry.I18n.Get("lookup.pond.sturgeon")).ToString();
			}
			else
			{
				string text9 = text8;
				if (text9.Contains("lava eel"))
				{
					text6 = ((object)ModEntry.I18n.Get("lookup.pond.lava-eel")).ToString();
				}
				else
				{
					string text10 = text8;
					if (text10.Contains("blobfish"))
					{
						text6 = ((object)ModEntry.I18n.Get("lookup.pond.blobfish")).ToString();
					}
					else
					{
						string text11 = text8;
						if (text11.Contains("rainbow trout"))
						{
							text6 = ((object)ModEntry.I18n.Get("lookup.pond.rainbow-trout")).ToString();
						}
						else
						{
							string text12 = text8;
							if (text12.Contains("super cucumber"))
							{
								text6 = ((object)ModEntry.I18n.Get("lookup.pond.super-cucumber")).ToString();
							}
							else
							{
								string text13 = text8;
								if (text13.Contains("midnight squid") || text13.Contains("squid"))
								{
									text6 = ((object)ModEntry.I18n.Get("lookup.pond.squid")).ToString();
								}
								else
								{
									string text14 = text8;
									if (text14.Contains("woodskip"))
									{
										text6 = ((object)ModEntry.I18n.Get("lookup.pond.woodskip")).ToString();
									}
									else
									{
										string text15 = text8;
										if (text15.Contains("slimejack"))
										{
											text6 = ((object)ModEntry.I18n.Get("lookup.pond.slimejack")).ToString();
										}
										else
										{
											string text16 = text8;
											if (text16.Contains("spook fish"))
											{
												text6 = ((object)ModEntry.I18n.Get("lookup.pond.stonefish")).ToString();
											}
											else
											{
												string text17 = text8;
												if (text17.Contains("stingray"))
												{
													text6 = ((object)ModEntry.I18n.Get("lookup.pond.stingray")).ToString();
												}
												else
												{
													string text18 = text8;
													if (text18.Contains("lionfish"))
													{
														text6 = ((object)ModEntry.I18n.Get("lookup.pond.lionfish")).ToString();
													}
													else
													{
														string text19 = text8;
														text6 = ((!text19.Contains("eel")) ? ((object)ModEntry.I18n.Get("lookup.pond.regular-roe", (object)new
														{
															fish = item.DisplayName
														})).ToString() : ((object)ModEntry.I18n.Get("lookup.pond.dorado")).ToString());
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			if (1 == 0)
			{
			}
			string value3 = text6;
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish.pond-produce")), value3, (Color?)new Color(180, 50, 180)));
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Scans every location's Fish list for this fish and returns friendly names.</summary>
	private static List<string> GetFishSpawnLocations(string fishId)
	{
		List<string> list = new List<string>();
		try
		{
			Dictionary<string, LocationData> dictionary = DataLoader.Locations(Game1.content);
			if (dictionary == null)
			{
				return list;
			}
			foreach (KeyValuePair<string, LocationData> item in dictionary)
			{
				string key = item.Key;
				if (key.Equals("fishingGame", StringComparison.OrdinalIgnoreCase) || key.Equals("Temp", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				LocationData value = item.Value;
				if (value.Fish == null)
				{
					continue;
				}
				foreach (SpawnFishData item2 in value.Fish)
				{
					if (((GenericSpawnItemData)item2).ItemId == fishId || ((GenericSpawnItemData)item2).ItemId == "(O)" + fishId || ((GenericSpawnItemData)item2).Id == fishId || ((GenericSpawnItemData)item2).Id == "(O)" + fishId)
					{
						string friendlyLocationName = GetFriendlyLocationName(key, value, item2);
						if (!list.Contains(friendlyLocationName))
						{
							list.Add(friendlyLocationName);
						}
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	/// <summary>Maps internal location keys ("Town", "Forest"...) onto player-friendly names,
	/// falling back to the data's DisplayName (with token/localization unwrapping).</summary>
	private static string GetFriendlyLocationName(string locKey, LocationData locData, SpawnFishData fishEntry)
	{
		if (locKey.Equals("Forest", StringComparison.OrdinalIgnoreCase))
		{
			if (fishEntry.FishAreaId == "Pond")
			{
				return ((object)ModEntry.I18n.Get("lookup.location.forest-pond")).ToString();
			}
			if (fishEntry.FishAreaId == "River")
			{
				return ((object)ModEntry.I18n.Get("lookup.location.forest-river")).ToString();
			}
			return ((object)ModEntry.I18n.Get("lookup.location.cindersap-forest")).ToString();
		}
		if (locKey.Equals("Town", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.pelican-town-river")).ToString();
		}
		if (locKey.Equals("Mountain", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.mountain-lake")).ToString();
		}
		if (locKey.Equals("Beach", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.ocean-beach")).ToString();
		}
		if (locKey.Equals("Woods", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.secret-woods")).ToString();
		}
		if (locKey.Equals("Desert", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.calico-desert")).ToString();
		}
		if (locKey.Equals("UndergroundMine", StringComparison.OrdinalIgnoreCase))
		{
			if (!string.IsNullOrEmpty(fishEntry.FishAreaId))
			{
				return ((object)ModEntry.I18n.Get("lookup.location.mines-floor", (object)new
				{
					floor = fishEntry.FishAreaId
				})).ToString();
			}
			return ((object)ModEntry.I18n.Get("lookup.location.the-mines")).ToString();
		}
		if (locKey.Equals("Sewer", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.the-sewers")).ToString();
		}
		if (locKey.Equals("BugLand", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.mutant-bug-lair")).ToString();
		}
		if (locKey.Equals("WitchSwamp", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.witchs-swamp")).ToString();
		}
		if (locKey.Equals("Submarine", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.night-market-submarine")).ToString();
		}
		if (locKey.Equals("IslandSouth", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.ginger-island-south")).ToString();
		}
		if (locKey.Equals("IslandWest", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.ginger-island-west")).ToString();
		}
		if (locKey.Equals("IslandNorth", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.ginger-island-river")).ToString();
		}
		if (locKey.Equals("IslandSouthEastCave", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.pirate-cove")).ToString();
		}
		if (locKey.Equals("Caldera", StringComparison.OrdinalIgnoreCase))
		{
			return ((object)ModEntry.I18n.Get("lookup.location.volcano-caldera")).ToString();
		}
		if (!string.IsNullOrEmpty(locData.DisplayName))
		{
			string displayName = locData.DisplayName;
			if (displayName.StartsWith("[LocalizedText ") && displayName.EndsWith("]"))
			{
				try
				{
					string text = displayName.Substring("[LocalizedText ".Length);
					text = text.Substring(0, text.Length - 1).Trim();
					string text2 = Game1.content.LoadString(text);
					if (!string.IsNullOrEmpty(text2))
					{
						return text2;
					}
				}
				catch
				{
				}
			}
			string text3 = TokenParser.ParseText(displayName, (Random)null, (TokenParserDelegate)null, (Farmer)null);
			if (!string.IsNullOrEmpty(text3) && !text3.StartsWith("["))
			{
				return text3;
			}
		}
		return locKey;
	}

	/// <summary>Converts a raw 24h-style game time (like "1900") into "7:00 PM" text.</summary>
	private static string FormatGameTime(string rawTime)
	{
		if (int.TryParse(rawTime, out var result))
		{
			// Hundreds digit = hour, last two = minutes; then convert 24h to 12h AM/PM.
			int num = result / 100;
			int num2 = result % 100;
			string value = ((num >= 12 && num < 24) ? "PM" : "AM");
			if (num > 12)
			{
				num -= 12;
			}
			if (num == 0)
			{
				num = 12;
			}
			return $"{num}:{((num2 == 0) ? "00" : num2.ToString())} {value}";
		}
		return rawTime;
	}

	/// <summary>Same idea as GetFriendlyLocationName but for the forage tables (switch-based).</summary>
	private static string GetFriendlyForageLocationName(string locKey)
	{
		if (1 == 0)
		{
		}
		string result = locKey switch
		{
			"Town" => ((object)ModEntry.I18n.Get("lookup.location.pelican-town-river")).ToString(), 
			"Forest" => ((object)ModEntry.I18n.Get("lookup.location.cindersap-forest")).ToString(), 
			"Mountain" => ((object)ModEntry.I18n.Get("lookup.location.the-mountain")).ToString(), 
			"BusStop" => ((object)ModEntry.I18n.Get("lookup.location.bus-stop")).ToString(), 
			"Railroad" => ((object)ModEntry.I18n.Get("lookup.location.railroad")).ToString(), 
			"Beach" => ((object)ModEntry.I18n.Get("lookup.location.the-beach")).ToString(), 
			"Woods" => ((object)ModEntry.I18n.Get("lookup.location.secret-woods")).ToString(), 
			"Desert" => ((object)ModEntry.I18n.Get("lookup.location.calico-desert")).ToString(), 
			"IslandWest" => ((object)ModEntry.I18n.Get("lookup.spawn.ginger-island-volcano")).ToString(), 
			"IslandSouth" => ((object)ModEntry.I18n.Get("lookup.location.ginger-island-south")).ToString(), 
			"IslandNorth" => ((object)ModEntry.I18n.Get("lookup.location.ginger-island-river")).ToString(), 
			"IslandSouthEast" => ((object)ModEntry.I18n.Get("lookup.location.pirate-cove")).ToString(), 
			"UndergroundMine" => ((object)ModEntry.I18n.Get("lookup.location.the-mines")).ToString(), 
			"Backwoods" => ((object)ModEntry.I18n.Get("lookup.location.backwoods")).ToString(), 
			_ => locKey, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	/// <summary>Where this forage item spawns: live data scan first, then hardcoded fallbacks.</summary>
	private static void AddForageDataSection(LookupSubject subject, Item item)
	{
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aea: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3f: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Dictionary<string, LocationData> dictionary = DataLoader.Locations(Game1.content);
			if (dictionary == null)
			{
				return;
			}
			string itemId = item.ItemId;
			string qualifiedItemId = item.QualifiedItemId;
			HashSet<string> hashSet = new HashSet<string>();
			HashSet<string> hashSet2 = new HashSet<string>();
			foreach (KeyValuePair<string, LocationData> item21 in dictionary)
			{
				string key = item21.Key;
				LocationData value = item21.Value;
				if (value.Forage == null)
				{
					continue;
				}
				foreach (SpawnForageData item22 in value.Forage)
				{
					if (((GenericSpawnItemData)item22).ItemId == itemId || ((GenericSpawnItemData)item22).ItemId == qualifiedItemId || ((GenericSpawnItemData)item22).Id == itemId || ((GenericSpawnItemData)item22).Id == qualifiedItemId)
					{
						string friendlyForageLocationName = GetFriendlyForageLocationName(key);
						if (!string.IsNullOrEmpty(friendlyForageLocationName))
						{
							hashSet.Add(friendlyForageLocationName);
						}
						if (item22.Season.HasValue)
						{
							string text = ((object)item22.Season.Value/*cast due to constrained. prefix*/).ToString();
							hashSet2.Add(char.ToUpper(text[0]) + text.Substring(1));
						}
					}
				}
			}
			string item2 = ((object)ModEntry.I18n.Get("season.spring")).ToString();
			string item3 = ((object)ModEntry.I18n.Get("season.summer")).ToString();
			string item4 = ((object)ModEntry.I18n.Get("season.fall")).ToString();
			string item5 = ((object)ModEntry.I18n.Get("season.winter")).ToString();
			string item6 = ((object)ModEntry.I18n.Get("lookup.location.pelican-town-river")).ToString();
			string item7 = ((object)ModEntry.I18n.Get("lookup.location.cindersap-forest")).ToString();
			string item8 = ((object)ModEntry.I18n.Get("lookup.location.the-mountain")).ToString();
			string item9 = ((object)ModEntry.I18n.Get("lookup.location.bus-stop")).ToString();
			string item10 = ((object)ModEntry.I18n.Get("lookup.location.cindersap-island")).ToString();
			string item11 = ((object)ModEntry.I18n.Get("lookup.location.secret-woods")).ToString();
			string item12 = ((object)ModEntry.I18n.Get("lookup.location.farm-cave-mushroom")).ToString();
			string item13 = ((object)ModEntry.I18n.Get("lookup.location.prehistoric-skull-cavern")).ToString();
			string item14 = ((object)ModEntry.I18n.Get("lookup.location.the-mines")).ToString();
			string item15 = ((object)ModEntry.I18n.Get("lookup.location.the-mines-81")).ToString();
			string item16 = ((object)ModEntry.I18n.Get("lookup.location.skull-cavern")).ToString();
			string item17 = ((object)ModEntry.I18n.Get("lookup.location.the-mines-boxes")).ToString();
			string item18 = ((object)ModEntry.I18n.Get("lookup.location.the-beach")).ToString();
			string item19 = ((object)ModEntry.I18n.Get("lookup.location.calico-desert")).ToString();
			string item20 = ((object)ModEntry.I18n.Get("lookup.location.ginger-island-south")).ToString();
			// Fallback table: some forageables (or modded data) aren't in the location
			// scan, so a few well-known ids get hardcoded season/location hints.
			switch (item.ItemId)
			{
			case "16":
			case "18":
			case "20":
			case "22":
				hashSet2.Add(item2);
				hashSet.Add(item6);
				hashSet.Add(item7);
				hashSet.Add(item8);
				hashSet.Add(item9);
				break;
			case "399":
				hashSet2.Add(item2);
				hashSet.Add(item10);
				break;
			case "257":
				hashSet2.Add(item2);
				hashSet.Add(item11);
				hashSet.Add(item12);
				break;
			case "396":
			case "398":
			case "394":
				hashSet2.Add(item3);
				hashSet.Add(item6);
				hashSet.Add(item7);
				hashSet.Add(item8);
				hashSet.Add(item9);
				break;
			case "259":
				hashSet2.Add(item3);
				hashSet.Add(item11);
				hashSet.Add(item13);
				break;
			case "404":
			case "406":
			case "408":
			case "410":
				hashSet2.Add(item4);
				hashSet.Add(item6);
				hashSet.Add(item7);
				hashSet.Add(item8);
				hashSet.Add(item9);
				break;
			case "281":
				hashSet2.Add(item4);
				hashSet.Add(item11);
				hashSet.Add(item12);
				break;
			case "420":
				hashSet2.Add(item3);
				hashSet2.Add(item4);
				hashSet.Add(item11);
				hashSet.Add(item14);
				break;
			case "422":
				hashSet.Add(item15);
				hashSet.Add(item16);
				break;
			case "78":
				hashSet.Add(item17);
				hashSet.Add(item16);
				break;
			case "372":
			case "393":
			case "397":
			case "152":
				hashSet.Add(item18);
				break;
			case "88":
			case "90":
				hashSet.Add(item19);
				break;
			case "829":
			case "830":
			case "832":
			case "834":
				hashSet.Add(item20);
				break;
			case "412":
			case "414":
			case "416":
			case "418":
			case "283":
				hashSet2.Add(item5);
				hashSet.Add(item6);
				hashSet.Add(item7);
				hashSet.Add(item8);
				hashSet.Add(item9);
				break;
			}
			if (hashSet.Count > 0 || hashSet2.Count > 0)
			{
				LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.wild-forage")));
				if (hashSet2.Count > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.forage.seasons")), string.Join(", ", hashSet2), (Color?)new Color(0, 140, 0)));
				}
				if (hashSet.Count > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.forage.spawn-locations")), string.Join(", ", hashSet), (Color?)new Color(20, 110, 220)));
				}
				subject.Sections.Add(lookupSection);
			}
		}
		catch
		{
		}
	}

	/// <summary>Where minerals/artifacts come from (artifact spots, geodes, nodes, drops).</summary>
	private static void AddMineralAndArtifactLocationSection(LookupSubject subject, Item item)
	{
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			SObject val = (SObject)(object)((item is SObject) ? item : null);
			if (val == null || (!(val.Type == "Arch") && !(val.Type == "Minerals") && item.Category != -12))
			{
				return;
			}
			List<string> list = new List<string>();
			if (val.Type == "Arch")
			{
				list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.artifact-spots")).ToString());
				list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.fishing-chests")).ToString());
				list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.artifact-troves")).ToString());
				if (item.ItemId == "107")
				{
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.pepper-rex")).ToString());
				}
			}
			else if (val.Type == "Minerals" || item.Category == -12)
			{
				if (item.ItemId == "74")
				{
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.iridium-nodes")).ToString());
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.omni-geodes")).ToString());
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.monster-drops")).ToString());
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.rainbow-trout-pond")).ToString());
				}
				else if (item.ItemId == "72")
				{
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.diamond-nodes")).ToString());
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.fishing-chests")).ToString());
				}
				else if (item.ItemId == "60" || item.ItemId == "62" || item.ItemId == "64" || item.ItemId == "66" || item.ItemId == "68" || item.ItemId == "70")
				{
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.gem-nodes")).ToString());
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.fishing-chests")).ToString());
				}
				else
				{
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.mining-mines")).ToString());
					list.Add(((object)ModEntry.I18n.Get("lookup.mineral.source.cracking-geodes")).ToString());
				}
			}
			if (list.Count > 0)
			{
				LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.gathering-sources")));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mineral.sources")), string.Join(" | ", list), (Color?)new Color(20, 110, 220)));
				subject.Sections.Add(lookupSection);
			}
		}
		catch
		{
		}
	}

	/// <summary>Crop card: growth time, regrowth, seasons, trellis, extra-harvest chance.</summary>
	private static void AddCropDataSection(LookupSubject subject, Item item)
	{
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Dictionary<string, CropData> dictionary = DataLoader.Crops(Game1.content);
			if (dictionary == null)
			{
				return;
			}
			foreach (KeyValuePair<string, CropData> item2 in dictionary)
			{
				string key = item2.Key;
				CropData value = item2.Value;
				if (!(key == item.ItemId) && !(key == item.QualifiedItemId) && !(value.HarvestItemId == item.ItemId) && !(value.HarvestItemId == item.QualifiedItemId))
				{
					continue;
				}
				LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.crop-info")));
				int days = ((value.DaysInPhase != null) ? value.DaysInPhase.Sum() : 0);
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.crop.growth-time")), ((object)ModEntry.I18n.Get("lookup.crop.growth-days", (object)new { days })).ToString(), (Color?)new Color(0, 140, 0)));
				if (value.RegrowDays > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.crop.regrowth")), ((object)ModEntry.I18n.Get("lookup.crop.regrowth-days", (object)new
					{
						days = value.RegrowDays
					})).ToString(), (Color?)new Color(180, 100, 0)));
				}
				if (value.Seasons != null && value.Seasons.Count > 0)
				{
					string value2 = string.Join(", ", value.Seasons.Select(s => {
						string text = "season." + s.ToString().ToLower();
						Translation val = ModEntry.I18n.Get(text);
						return val.HasValue() ? ((object)val).ToString() : s.ToString();
					}));
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.crop.harvest-seasons")), value2, (Color?)new Color(46, 125, 50)));
				}
				if (value.IsRaised)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.crop.trellis")), ((object)ModEntry.I18n.Get("lookup.crop.trellis-yes")).ToString(), (Color?)new Color(200, 60, 20)));
				}
				if (value.ExtraHarvestChance > 0.0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.crop.extra-harvest")), $"{value.ExtraHarvestChance * 100.0:0.#}%", Color.DarkSlateGray));
				}
				subject.Sections.Add(lookupSection);
				break;
			}
		}
		catch
		{
		}
	}

	/// <summary>What machines can process this item (wine, jelly, juice, pickles, smoking...).</summary>
	private static void AddArtisanProductsSection(LookupSubject subject, Item item, int basePrice)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_051a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0538: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_0360: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_066d: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_079c: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c4b: Unknown result type (might be due to invalid IL or missing references)
		//IL_085b: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_071e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0982: Unknown result type (might be due to invalid IL or missing references)
		//IL_0890: Unknown result type (might be due to invalid IL or missing references)
		//IL_08cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d57: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a5c: Unknown result type (might be due to invalid IL or missing references)
		//IL_09b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0904: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b07: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a91: Unknown result type (might be due to invalid IL or missing references)
		//IL_0bb2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0be7: Unknown result type (might be due to invalid IL or missing references)
		if (basePrice <= 0)
		{
			return;
		}
		List<LookupLink> list = new List<LookupLink>();
		string text = item.ItemId.ToLowerInvariant();
		string text2 = item.Name.ToLowerInvariant();
		// Fruit (-79): keg wine = x3 price, preserves-jar jelly = 2x+50,
		// dehydrator dried fruit = 7.5x+25.
		if (item.Category == -79)
		{
			int price = basePrice * 3;
			ParsedItemData data = ItemRegistry.GetData("(O)348");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.wine", (object)new
			{
				name = item.DisplayName,
				price = price
			})).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.keg-time-wine")).ToString(), (Color?)new Color(180, 50, 180), (data != null) ? data.GetTexture() : null, (data != null) ? new Rectangle?(data.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
			int price2 = basePrice * 2 + 50;
			ParsedItemData data2 = ItemRegistry.GetData("(O)444");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.jelly", (object)new
			{
				name = item.DisplayName,
				price = price2
			})).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.jar-time-jelly")).ToString(), (Color?)new Color(200, 60, 20), (data2 != null) ? data2.GetTexture() : null, (data2 != null) ? new Rectangle?(data2.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
			int price3 = (int)((double)basePrice * 7.5) + 25;
			ParsedItemData data3 = ItemRegistry.GetData("(O)DriedFruit");
			if (data3 != null)
			{
				list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.dried", (object)new
				{
					name = item.DisplayName,
					price = price3
				})).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.dehydrator-time")).ToString(), (Color?)new Color(180, 100, 0), data3.GetTexture(), (Rectangle?)data3.GetSourceRect(0, (int?)null), (Func<LookupSubject?>?)null));
			}
		}
		else
		{
			if (item.Category != -75)
			{
				if (!text2.Contains("mushroom"))
				{
					switch (text)
					{
					case "404":
					case "420":
					case "422":
					case "281":
						goto IL_03d4;
					}
					if (!(text == "257"))
					{
						if (item.Category == -4 || IsFishItem(item))
						{
							// Smoked fish = x2 price (x1.4 again with the Artisan profession).
							int num = basePrice * 2;
							ParsedItemData data4 = ItemRegistry.GetData("(O)SmokedFish");
							if (data4 != null)
							{
								list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.smoked", (object)new
								{
									name = item.DisplayName,
									price = num,
									artisanPrice = (int)((double)num * 1.4)
								})).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.smoker-time")).ToString(), (Color?)new Color(200, 60, 20), data4.GetTexture(), (Rectangle?)data4.GetSourceRect(0, (int?)null), (Func<LookupSubject?>?)null));
							}
						}
						goto IL_0550;
					}
				}
				goto IL_03d4;
			}
			// Vegetables (-75): keg juice = x2.25, preserves-jar pickles = 2x+50.
			int price4 = (int)((double)basePrice * 2.25);
			ParsedItemData data5 = ItemRegistry.GetData("(O)350");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.juice", (object)new
			{
				name = item.DisplayName,
				price = price4
			})).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.keg-time-juice")).ToString(), (Color?)new Color(0, 140, 0), (data5 != null) ? data5.GetTexture() : null, (data5 != null) ? new Rectangle?(data5.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
			int price5 = basePrice * 2 + 50;
			ParsedItemData data6 = ItemRegistry.GetData("(O)342");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.pickled", (object)new
			{
				name = item.DisplayName,
				price = price5
			})).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.jar-time-jelly")).ToString(), (Color?)new Color(180, 100, 0), (data6 != null) ? data6.GetTexture() : null, (data6 != null) ? new Rectangle?(data6.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
		}
		goto IL_0550;
		IL_0550:
		if (text == "433" || text2 == "coffee bean")
		{
			ParsedItemData data7 = ItemRegistry.GetData("(O)395");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.coffee")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.keg-time-coffee")).ToString(), (Color?)new Color(110, 40, 10), (data7 != null) ? data7.GetTexture() : null, (data7 != null) ? new Rectangle?(data7.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
		}
		else if (text == "815" || text2 == "tea leaves")
		{
			ParsedItemData data8 = ItemRegistry.GetData("(O)614");
			ParsedItemData data9 = ItemRegistry.GetData("(O)342");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.green-tea")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.keg-time-tea")).ToString(), (Color?)new Color(46, 125, 50), (data8 != null) ? data8.GetTexture() : null, (data8 != null) ? new Rectangle?(data8.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.pickled-tea")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.jar-time-jelly")).ToString(), (Color?)new Color(180, 100, 0), (data9 != null) ? data9.GetTexture() : null, (data9 != null) ? new Rectangle?(data9.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
		}
		else if (text == "304" || text2 == "hops")
		{
			ParsedItemData data10 = ItemRegistry.GetData("(O)303");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.pale-ale")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.keg-cask-ale")).ToString(), (Color?)new Color(180, 100, 0), (data10 != null) ? data10.GetTexture() : null, (data10 != null) ? new Rectangle?(data10.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
		}
		else if (text == "262" || text2 == "wheat")
		{
			ParsedItemData data11 = ItemRegistry.GetData("(O)346");
			ParsedItemData data12 = ItemRegistry.GetData("(O)246");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.beer")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.keg-cask-ale")).ToString(), (Color?)new Color(180, 100, 0), (data11 != null) ? data11.GetTexture() : null, (data11 != null) ? new Rectangle?(data11.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.wheat-flour")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.mill-overnight")).ToString(), Game1.textColor, (data12 != null) ? data12.GetTexture() : null, (data12 != null) ? new Rectangle?(data12.GetSourceRect(0, (int?)null)) : ((Rectangle?)null)));
		}
		else if (text == "340" || text2 == "honey")
		{
			ParsedItemData data13 = ItemRegistry.GetData("(O)459");
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.mead")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.keg-cask-mead")).ToString(), (Color?)new Color(180, 100, 0), (data13 != null) ? data13.GetTexture() : null, (data13 != null) ? new Rectangle?(data13.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
		}
		else
		{
			switch (text)
			{
			default:
				if (!text2.Contains("sunflower") && !(text2 == "corn"))
				{
					if (text == "271" || text2 == "unmilled rice")
					{
						ParsedItemData data14 = ItemRegistry.GetData("(O)423");
						list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.milled-rice")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.mill-overnight")).ToString(), Game1.textColor, (data14 != null) ? data14.GetTexture() : null, (data14 != null) ? new Rectangle?(data14.GetSourceRect(0, (int?)null)) : ((Rectangle?)null)));
					}
					else if (text == "284" || text2 == "beet")
					{
						ParsedItemData data15 = ItemRegistry.GetData("(O)245");
						list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.sugar-yield")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.mill-overnight")).ToString(), Game1.textColor, (data15 != null) ? data15.GetTexture() : null, (data15 != null) ? new Rectangle?(data15.GetSourceRect(0, (int?)null)) : ((Rectangle?)null)));
					}
					break;
				}
				goto case "270";
			case "270":
			case "421":
			case "431":
			{
				ParsedItemData data16 = ItemRegistry.GetData("(O)247");
				list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.cooking-oil")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.oil-maker-source")).ToString(), (Color?)new Color(180, 100, 0), (data16 != null) ? data16.GetTexture() : null, (data16 != null) ? new Rectangle?(data16.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
				break;
			}
			}
		}
		if (item.Category == -79 || item.Category == -75)
		{
			// Seed Maker works for any crop: yields 1-3 seeds (rarely ancient mixed in).
			list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.seeds-yield")).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.seed-maker-time")).ToString(), (Color?)new Color(46, 125, 50), (Texture2D?)null, (Rectangle?)null, (Func<LookupSubject?>?)null));
		}
		if (text2.Contains("wine") || text2.Contains("cheese") || text2.Contains("pale ale") || text2.Contains("beer") || text2.Contains("mead"))
		{
			// Cask aging reaches iridium quality after 56 days (wine), 14 (cheese),
			// or 34 (ale/mead); the ternary chain picks the right number.
			int value = basePrice * 2;
			int days = (text2.Contains("wine") ? 56 : (text2.Contains("cheese") ? 14 : 34));
			list.Add(new LookupLink($"{ModEntry.I18n.Get("hover.quality.iridium")} ({value}g)", ((object)ModEntry.I18n.Get("lookup.building.cask-aging", (object)new { days })).ToString(), (Color?)new Color(180, 50, 180), (Texture2D?)null, (Rectangle?)null, (Func<LookupSubject?>?)null));
		}
		if (list.Count > 0)
		{
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.artisan-products")));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.artisan.products")), list));
			subject.Sections.Add(lookupSection);
		}
		return;
		// "IL_xxxx" labels + goto jumps are decompiler leftovers from optimized
		// branches; flow is still simple. This block handles dried mushrooms (7.5x+25).
		IL_03d4:
		int price6 = (int)((double)basePrice * 7.5) + 25;
		ParsedItemData val = ItemRegistry.GetData("(O)DriedMushrooms") ?? ItemRegistry.GetData("(O)DriedFruit");
		list.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.artisan.dried", (object)new
		{
			name = item.DisplayName,
			price = price6
		})).ToString(), ((object)ModEntry.I18n.Get("lookup.artisan.dehydrator-time")).ToString(), (Color?)new Color(180, 100, 0), (val != null) ? val.GetTexture() : null, (val != null) ? new Rectangle?(val.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null));
		goto IL_0550;
	}

	/// <summary>Explains what a placeable machine does when the item IS a machine.</summary>
	private static void AddMachineItemSection(LookupSubject subject, Item item)
	{
		//IL_0653: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = item.ItemId.ToLowerInvariant();
			string text2 = item.Name.ToLowerInvariant();
			string text3 = "";
			// Long if/else-if ladder matching the item by name OR id - each arm
			// loads a localized "what this machine does" blurb.
			if (text2 == "furnace" || text == "13")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.furnace")).ToString();
			}
			else if (text2.Contains("heavy furnace") || text == "heavyfurnace" || text == "278")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.heavy-furnace")).ToString();
			}
			else if (text2.Contains("charcoal kiln") || text == "114")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.charcoal-kiln")).ToString();
			}
			else if (text2 == "crystalarium" || text == "21")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.crystalarium")).ToString();
			}
			else if (text2 == "seed maker" || text == "25")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.seed-maker")).ToString();
			}
			else if (text2 == "cheese press" || text == "16")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.cheese-press")).ToString();
			}
			else if (text2 == "mayonnaise machine" || text == "24")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.mayo-machine")).ToString();
			}
			else if (text2 == "oil maker" || text == "19")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.oil-maker")).ToString();
			}
			else if (text2 == "loom" || text == "17")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.loom")).ToString();
			}
			else if (text2 == "keg" || text == "12")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.keg")).ToString();
			}
			else if (text2 == "preserves jar" || text == "15")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.preserves-jar")).ToString();
			}
			else if (text2 == "cask" || text == "163")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.cask")).ToString();
			}
			else if (text2.Contains("dehydrator"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.dehydrator")).ToString();
			}
			else if (text2.Contains("fish smoker") || text2.Contains("smoker"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.fish-smoker")).ToString();
			}
			else if (text2.Contains("bait maker"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.bait-maker")).ToString();
			}
			else if (text2.Contains("deluxe worm bin"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.deluxe-worm-bin")).ToString();
			}
			else if (text2.Contains("worm bin") || text == "154")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.worm-bin")).ToString();
			}
			else if (text2 == "bone mill" || text == "90")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.bone-mill")).ToString();
			}
			else if (text2 == "geode crusher" || text == "182")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.geode-crusher")).ToString();
			}
			else if (text2 == "solar panel" || text == "231")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.solar-panel")).ToString();
			}
			else if (text2 == "mini-forge" || text == "230")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.mini-forge")).ToString();
			}
			else if (text2 == "anvil")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.anvil")).ToString();
			}
			else if (text2 == "auto-grabber" || text == "165")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.auto-grabber")).ToString();
			}
			else if (text2 == "auto-petter" || text == "272")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.auto-petter")).ToString();
			}
			else if (text2.Contains("statue of perfection"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.statue-perfection")).ToString();
			}
			else if (text2.Contains("statue of true perfection"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.statue-true-perfection")).ToString();
			}
			else if (text2.Contains("statue of blessings"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.statue-blessings")).ToString();
			}
			else
			{
				if (!text2.Contains("statue of the dwarf king"))
				{
					return;
				}
				text3 = ((object)ModEntry.I18n.Get("lookup.machine.statue-dwarf-king")).ToString();
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.machine-info")));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.machine.processing")), text3, (Color?)new Color(0, 140, 0)));
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Fruit-tree sapling card: harvest season, maturation, spacing, aging.</summary>
	private static void AddFruitTreeSaplingSection(LookupSubject subject, Item item)
	{
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0252: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = item.Name.ToLowerInvariant();
			if (!text.Contains("sapling") && !text.Contains("tree"))
			{
				return;
			}
			string text2 = "";
			if (text.Contains("cherry"))
			{
				text2 = ((object)ModEntry.I18n.Get("season.spring")).ToString();
			}
			else if (text.Contains("apricot"))
			{
				text2 = ((object)ModEntry.I18n.Get("season.spring")).ToString();
			}
			else if (text.Contains("orange"))
			{
				text2 = ((object)ModEntry.I18n.Get("season.summer")).ToString();
			}
			else if (text.Contains("peach"))
			{
				text2 = ((object)ModEntry.I18n.Get("season.summer")).ToString();
			}
			else if (text.Contains("banana"))
			{
				text2 = ((object)ModEntry.I18n.Get("lookup.fruit-tree.summer-greenhouse")).ToString();
			}
			else if (text.Contains("mango"))
			{
				text2 = ((object)ModEntry.I18n.Get("lookup.fruit-tree.summer-greenhouse")).ToString();
			}
			else if (text.Contains("apple"))
			{
				text2 = ((object)ModEntry.I18n.Get("season.fall")).ToString();
			}
			else if (text.Contains("pomegranate"))
			{
				text2 = ((object)ModEntry.I18n.Get("season.fall")).ToString();
			}
			else
			{
				if (!text.Contains("mystic"))
				{
					return;
				}
				text2 = ((object)ModEntry.I18n.Get("lookup.fruit-tree.all-seasons-mystic")).ToString();
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.sapling-info")));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.sapling.harvest-season")), text2, (Color?)new Color(46, 125, 50)));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.sapling.maturation-time")), ((object)ModEntry.I18n.Get("lookup.sapling.maturation-desc")).ToString(), (Color?)new Color(180, 100, 0)));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.sapling.spacing")), ((object)ModEntry.I18n.Get("lookup.sapling.spacing-desc")).ToString(), (Color?)new Color(200, 60, 20)));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.sapling.quality-aging")), ((object)ModEntry.I18n.Get("lookup.sapling.quality-desc")).ToString(), (Color?)new Color(180, 50, 180)));
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Fun/functional lore blurbs for a handful of special items.</summary>
	private static void AddSpecialItemLoreSection(LookupSubject subject, Item item)
	{
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = item.ItemId.ToLowerInvariant();
			string text2 = item.Name.ToLowerInvariant();
			string text3 = "";
			if (text2 == "stardrop tea" || text == "stardroptea")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.stardrop-tea")).ToString();
			}
			else if (text2 == "prize ticket" || text == "prizeticket")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.prize-ticket")).ToString();
			}
			else if (text2 == "calico egg" || text == "calicoegg")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.calico-egg")).ToString();
			}
			else if (text2 == "golden walnut" || text == "73")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.golden-walnut")).ToString();
			}
			else if (text2 == "qi gem" || text == "858")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.qi-gem")).ToString();
			}
			else if (text2 == "cinder shard" || text == "848")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.cinder-shard")).ToString();
			}
			else if (text2 == "magic rock candy" || text == "279")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.magic-rock-candy")).ToString();
			}
			else if (text2 == "tent kit" || text == "tentkit")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.tent-kit")).ToString();
			}
			else if (text2 == "sonar bobber" || text == "sonarbobber")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.sonar-bobber")).ToString();
			}
			else if (text2 == "challenge bait" || text == "challengebait")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.challenge-bait")).ToString();
			}
			else if (text2 == "deluxe bait" || text == "deluxebait")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.deluxe-bait")).ToString();
			}
			else if (text2.Contains("faraway") || text == "farawaystone")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.far-away-stone")).ToString();
			}
			else
			{
				if (!text2.Contains("crab pot") && !(text == "710") && !(text == "(o)710"))
				{
					return;
				}
				text3 = ((object)ModEntry.I18n.Get("lookup.lore.crab-pot")).ToString();
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.special-item")));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.special-item.function-lore")), text3, (Color?)new Color(180, 50, 180)));
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Shows sewing products and dye colors this item can be used for.</summary>
	private static void AddTailoringAndDyeSection(LookupSubject subject, Item item)
	{
		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			List<LookupField> list = new List<LookupField>();
			List<TailorItemRecipe> list2 = DataLoader.TailoringRecipes(Game1.content);
			if (list2 != null)
			{
				foreach (TailorItemRecipe item3 in list2)
				{
					// SecondItemTags are context tags like "color_red" or "item_wool";
					// HasContextTag checks whether THIS item satisfies the recipe slot.
					if (item3.SecondItemTags != null && item3.SecondItemTags.Any((string tag) => item.HasContextTag(tag) || tag == item.ItemId || tag == item.QualifiedItemId || tag == "(O)" + item.ItemId) && item3.CraftedItemIds != null && item3.CraftedItemIds.Count > 0)
					{
						string text = item3.CraftedItemIds[0];
						ParsedItemData data = ItemRegistry.GetData(text);
						if (data != null)
						{
							LookupLink item2 = new LookupLink(data.DisplayName, ((object)ModEntry.I18n.Get("lookup.tailoring.sewing-product")).ToString(), (Color?)new Color(180, 50, 180), data.GetTexture(), (Rectangle?)data.GetSourceRect(0, (int?)null), (Func<LookupSubject?>?)null);
							list.Add(new LookupField((ModEntry.I18n.Get("lookup.tailoring.sewing-product")), new List<LookupLink> { item2 }));
							break;
						}
					}
				}
			}
			HashSet<string> contextTags = item.GetContextTags();
			// Items carry "color_xxx" context tags; the ladder picks a dye color name
			// (blue wins over cyan/ocean-blue because of the || ordering).
			if (contextTags != null)
			{
				string text2 = null;
				if (contextTags.Contains("color_red"))
				{
					text2 = "Red";
				}
				else if (contextTags.Contains("color_orange"))
				{
					text2 = "Orange";
				}
				else if (contextTags.Contains("color_yellow"))
				{
					text2 = "Yellow";
				}
				else if (contextTags.Contains("color_green"))
				{
					text2 = "Green";
				}
				else if (contextTags.Contains("color_blue") || contextTags.Contains("color_cyan") || contextTags.Contains("color_ocean_blue"))
				{
					text2 = "Blue";
				}
				else if (contextTags.Contains("color_purple"))
				{
					text2 = "Purple";
				}
				else if (contextTags.Contains("color_pink"))
				{
					text2 = "Pink";
				}
				else if (contextTags.Contains("color_gray"))
				{
					text2 = "Gray";
				}
				else if (contextTags.Contains("color_brown"))
				{
					text2 = "Brown";
				}
				else if (contextTags.Contains("color_black"))
				{
					text2 = "Black";
				}
				if (text2 != null)
				{
					Color value = (Color)(text2.StartsWith("Red") ? new Color(220, 20, 60) : (text2.StartsWith("Orange") ? new Color(220, 100, 20) : (text2.StartsWith("Yellow") ? new Color(200, 160, 0) : (text2.StartsWith("Green") ? new Color(46, 125, 50) : (text2.StartsWith("Blue") ? new Color(20, 110, 220) : (text2.StartsWith("Purple") ? new Color(180, 50, 180) : Color.DarkSlateGray))))));
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.tailoring.dye-color")), text2, value));
				}
			}
			if (list.Count > 0)
			{
				LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.tailoring")));
				lookupSection.Fields.AddRange(list);
				subject.Sections.Add(lookupSection);
			}
		}
		catch
		{
		}
	}

	/// <summary>Animal-product card: incubation notes, mayo/cheese/cloth/oil outputs.</summary>
	private static void AddAnimalProductProcessingSection(LookupSubject subject, Item item)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0420: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_052e: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0914: Unknown result type (might be due to invalid IL or missing references)
		//IL_0839: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_072c: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_063b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0345: Unknown result type (might be due to invalid IL or missing references)
		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
		//IL_0563: Unknown result type (might be due to invalid IL or missing references)
		//IL_0acf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a2e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0949: Unknown result type (might be due to invalid IL or missing references)
		//IL_086e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0761: Unknown result type (might be due to invalid IL or missing references)
		//IL_0670: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b04: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = item.ItemId.ToLowerInvariant();
			string text2 = item.Name.ToLowerInvariant();
			List<LookupField> list = new List<LookupField>();
			if (item.Category == -5 || text2.Contains("egg"))
			{
				if (text2.Contains("dinosaur"))
				{
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.incubation")), ((object)ModEntry.I18n.Get("lookup.animal-processing.dino-egg")).ToString(), (Color?)new Color(46, 125, 50)));
					ParsedItemData data = ItemRegistry.GetData("(O)807");
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.mayo")), new List<LookupLink>
					{
						new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.dino-mayo")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time")).ToString(), (Color?)new Color(46, 125, 50), (data != null) ? data.GetTexture() : null, (data != null) ? new Rectangle?(data.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
					}));
				}
				else if (text2.Contains("ostrich"))
				{
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.incubation")), ((object)ModEntry.I18n.Get("lookup.animal-processing.ostrich-egg")).ToString(), (Color?)new Color(46, 125, 50)));
					ParsedItemData data2 = ItemRegistry.GetData("(O)306");
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.mayo")), new List<LookupLink>
					{
						new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.ostrich-mayo")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time")).ToString(), (Color?)new Color(180, 100, 0), (data2 != null) ? data2.GetTexture() : null, (data2 != null) ? new Rectangle?(data2.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
					}));
				}
				else if (text2.Contains("void"))
				{
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.incubation")), ((object)ModEntry.I18n.Get("lookup.animal-processing.void-egg")).ToString(), (Color?)new Color(180, 50, 180)));
					ParsedItemData data3 = ItemRegistry.GetData("(O)308");
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.mayo")), new List<LookupLink>
					{
						new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.void-mayo")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time")).ToString(), (Color?)new Color(180, 50, 180), (data3 != null) ? data3.GetTexture() : null, (data3 != null) ? new Rectangle?(data3.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
					}));
				}
				else if (text2.Contains("duck"))
				{
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.incubation")), ((object)ModEntry.I18n.Get("lookup.animal-processing.duck-egg")).ToString(), (Color?)new Color(20, 110, 220)));
					ParsedItemData data4 = ItemRegistry.GetData("(O)307");
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.mayo")), new List<LookupLink>
					{
						new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.duck-mayo")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time")).ToString(), (Color?)new Color(20, 110, 220), (data4 != null) ? data4.GetTexture() : null, (data4 != null) ? new Rectangle?(data4.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
					}));
				}
				else if (text2.Contains("golden"))
				{
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.incubation")), ((object)ModEntry.I18n.Get("lookup.animal-processing.golden-egg")).ToString(), (Color?)new Color(180, 100, 0)));
					ParsedItemData data5 = ItemRegistry.GetData("(O)306");
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.mayo")), new List<LookupLink>
					{
						new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.gold-mayo-3x")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time")).ToString(), (Color?)new Color(180, 100, 0), (data5 != null) ? data5.GetTexture() : null, (data5 != null) ? new Rectangle?(data5.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
					}));
				}
				else if (text2.Contains("large"))
				{
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.incubation")), ((object)ModEntry.I18n.Get("lookup.animal-processing.chicken-egg")).ToString(), (Color?)new Color(0, 140, 0)));
					ParsedItemData data6 = ItemRegistry.GetData("(O)306");
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.mayo")), new List<LookupLink>
					{
						new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.gold-mayo")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time")).ToString(), (Color?)new Color(180, 100, 0), (data6 != null) ? data6.GetTexture() : null, (data6 != null) ? new Rectangle?(data6.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
					}));
				}
				else
				{
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.incubation")), ((object)ModEntry.I18n.Get("lookup.animal-processing.chicken-egg")).ToString(), (Color?)new Color(0, 140, 0)));
					ParsedItemData data7 = ItemRegistry.GetData("(O)306");
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.mayo")), new List<LookupLink>
					{
						new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.normal-mayo")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.mayo-machine-time")).ToString(), Game1.textColor, (data7 != null) ? data7.GetTexture() : null, (data7 != null) ? new Rectangle?(data7.GetSourceRect(0, (int?)null)) : ((Rectangle?)null))
					}));
				}
			}
			if (item.Category == -6 || text2.Contains("milk"))
			{
				if (text2.Contains("goat"))
				{
					ParsedItemData data8 = ItemRegistry.GetData("(O)426");
					string text3 = (text2.Contains("large") ? ((object)ModEntry.I18n.Get("lookup.animal-prod.gold-goat-cheese")).ToString() : ((object)ModEntry.I18n.Get("lookup.animal-prod.regular-goat-cheese")).ToString());
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.cheese")), new List<LookupLink>
					{
						new LookupLink(text3, ((object)ModEntry.I18n.Get("lookup.animal-prod.cheese-press-time")).ToString(), (Color?)new Color(180, 100, 0), (data8 != null) ? data8.GetTexture() : null, (data8 != null) ? new Rectangle?(data8.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
					}));
				}
				else
				{
					ParsedItemData data9 = ItemRegistry.GetData("(O)424");
					string text4 = (text2.Contains("large") ? ((object)ModEntry.I18n.Get("lookup.animal-prod.gold-cheese")).ToString() : ((object)ModEntry.I18n.Get("lookup.animal-prod.regular-cheese")).ToString());
					list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.cheese")), new List<LookupLink>
					{
						new LookupLink(text4, ((object)ModEntry.I18n.Get("lookup.animal-prod.cheese-press-time")).ToString(), (Color?)new Color(180, 100, 0), (data9 != null) ? data9.GetTexture() : null, (data9 != null) ? new Rectangle?(data9.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
					}));
				}
			}
			if (text2.Contains("wool") || text == "440" || text == "(o)440")
			{
				ParsedItemData data10 = ItemRegistry.GetData("(O)428");
				list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.loom")), new List<LookupLink>
				{
					new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.cloth")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.loom-time")).ToString(), (Color?)new Color(180, 50, 180), (data10 != null) ? data10.GetTexture() : null, (data10 != null) ? new Rectangle?(data10.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
				}));
			}
			if (text2.Contains("truffle") && !text2.Contains("oil"))
			{
				ParsedItemData data11 = ItemRegistry.GetData("(O)432");
				list.Add(new LookupField((ModEntry.I18n.Get("lookup.animal-processing.oil")), new List<LookupLink>
				{
					new LookupLink(((object)ModEntry.I18n.Get("lookup.animal-prod.truffle-oil")).ToString(), ((object)ModEntry.I18n.Get("lookup.animal-prod.oil-maker-time")).ToString(), (Color?)new Color(180, 100, 0), (data11 != null) ? data11.GetTexture() : null, (data11 != null) ? new Rectangle?(data11.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), (Func<LookupSubject?>?)null)
				}));
			}
			if (list.Count > 0)
			{
				LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.animal-processing")));
				lookupSection.Fields.AddRange(list);
				subject.Sections.Add(lookupSection);
			}
		}
		catch
		{
		}
	}

	/// <summary>What the Recycling Machine yields for this piece of trash.</summary>
	private static void AddRecyclingSection(LookupSubject subject, Item item)
	{
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = item.ItemId.ToLowerInvariant();
			string text2 = item.Name.ToLowerInvariant();
			string text3 = "";
			if (text == "168" || text2 == "trash")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.recycling.trash")).ToString();
			}
			else if (text == "169" || text2 == "driftwood")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.recycling.driftwood")).ToString();
			}
			else if (text == "170" || text == "broken glasses" || text2.Contains("broken glasses"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.recycling.refined-quartz")).ToString();
			}
			else if (text == "171" || text == "broken cd" || text2.Contains("broken cd"))
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.recycling.refined-quartz")).ToString();
			}
			else if (text == "172" || text2 == "soggy newspaper")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.recycling.newspaper")).ToString();
			}
			else
			{
				if (!(text == "rotten plant") && !text2.Contains("rotten plant"))
				{
					return;
				}
				text3 = ((object)ModEntry.I18n.Get("lookup.recycling.rotten-plant")).ToString();
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.recycling")));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.recycling.yields")), text3, (Color?)new Color(0, 140, 0)));
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Geode/mystery-box card: where to crack it open and what can drop.</summary>
	private static void AddGeodeAndMysteryBoxSection(LookupSubject subject, Item item)
	{
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = item.ItemId.ToLowerInvariant();
			string text2 = item.Name.ToLowerInvariant();
			string text3 = "";
			string text4 = "";
			if (text == "535" || text2 == "geode")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.geode.crack.clint-or-crusher")).ToString();
				text4 = ((object)ModEntry.I18n.Get("lookup.geode.drops.regular")).ToString();
			}
			else if (text == "536" || text2 == "frozen geode")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.geode.crack.clint-or-crusher")).ToString();
				text4 = ((object)ModEntry.I18n.Get("lookup.geode.drops.frozen")).ToString();
			}
			else if (text == "537" || text2 == "magma geode")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.geode.crack.clint-or-crusher")).ToString();
				text4 = ((object)ModEntry.I18n.Get("lookup.geode.drops.magma")).ToString();
			}
			else if (text == "749" || text2 == "omni geode")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.geode.crack.omni")).ToString();
				text4 = ((object)ModEntry.I18n.Get("lookup.geode.drops.omni")).ToString();
			}
			else if (text == "275" || text2 == "artifact trove")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.geode.crack.trove")).ToString();
				text4 = ((object)ModEntry.I18n.Get("lookup.geode.drops.trove")).ToString();
			}
			else
			{
				if (!text.Contains("mysterybox") && !text2.Contains("mystery box"))
				{
					return;
				}
				text3 = ((object)ModEntry.I18n.Get("lookup.geode.crack.mystery-box")).ToString();
				text4 = ((object)ModEntry.I18n.Get("lookup.geode.drops.mystery-box")).ToString();
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.geode-info")));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.geode.cracking-method")), text3, (Color?)new Color(180, 100, 0)));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.geode.potential-drops")), text4, (Color?)new Color(0, 140, 0)));
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Fertilizer/retaining-soil card: what the soil effect does.</summary>
	private static void AddFertilizerDetailsSection(LookupSubject subject, Item item)
	{
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string text = item.ItemId.ToLowerInvariant();
			string text2 = item.Name.ToLowerInvariant();
			string text3 = "";
			if (text == "368" || text2 == "basic fertilizer")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.basic")).ToString();
			}
			else if (text == "369" || text2 == "quality fertilizer")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.quality")).ToString();
			}
			else if (text == "919" || text2 == "deluxe fertilizer")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.deluxe")).ToString();
			}
			else if (text == "465" || text2 == "speed-gro")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.speed-gro")).ToString();
			}
			else if (text == "466" || text2 == "deluxe speed-gro")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.deluxe-speed-gro")).ToString();
			}
			else if (text == "918" || text2 == "hyper speed-gro")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.hyper-speed-gro")).ToString();
			}
			else if (text == "370" || text2 == "basic retaining soil")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.basic-retaining")).ToString();
			}
			else if (text == "371" || text2 == "quality retaining soil")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.quality-retaining")).ToString();
			}
			else if (text == "920" || text2 == "deluxe retaining soil")
			{
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.deluxe-retaining")).ToString();
			}
			else
			{
				if (!(text == "805") && !(text2 == "tree fertilizer"))
				{
					return;
				}
				text3 = ((object)ModEntry.I18n.Get("lookup.fertilizer.tree")).ToString();
			}
			LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.fertilizer-info")));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fertilizer.soil-effect")), text3, (Color?)new Color(46, 125, 50)));
			subject.Sections.Add(lookupSection);
		}
		catch
		{
		}
	}

	/// <summary>Reads the item's Buffs list from object data and formats positive stat effects.</summary>
	private static List<string> GetFoodBuffs(Item item)
	{
		List<string> list = new List<string>();
		try
		{
			if (Game1.objectData.TryGetValue(item.ItemId, out var value) && value.Buffs != null)
			{
				foreach (ObjectBuffData buff in value.Buffs)
				{
					BuffAttributesData customAttributes = buff.CustomAttributes;
					if (customAttributes != null)
					{
						if (customAttributes.FarmingLevel > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.farming", (object)new
							{
								level = customAttributes.FarmingLevel
							})).ToString());
						}
						if (customAttributes.MiningLevel > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.mining", (object)new
							{
								level = customAttributes.MiningLevel
							})).ToString());
						}
						if (customAttributes.FishingLevel > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.fishing", (object)new
							{
								level = customAttributes.FishingLevel
							})).ToString());
						}
						if (customAttributes.ForagingLevel > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.foraging", (object)new
							{
								level = customAttributes.ForagingLevel
							})).ToString());
						}
						if (customAttributes.CombatLevel > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.combat", (object)new
							{
								level = customAttributes.CombatLevel
							})).ToString());
						}
						if (customAttributes.LuckLevel > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.luck", (object)new
							{
								level = customAttributes.LuckLevel
							})).ToString());
						}
						if (customAttributes.Speed > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.speed", (object)new
							{
								level = customAttributes.Speed
							})).ToString());
						}
						if (customAttributes.Defense > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.defense", (object)new
							{
								level = customAttributes.Defense
							})).ToString());
						}
						if (customAttributes.Attack > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.attack", (object)new
							{
								level = customAttributes.Attack
							})).ToString());
						}
						if (customAttributes.MaxStamina > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.max-energy", (object)new
							{
								level = customAttributes.MaxStamina
							})).ToString());
						}
						if (customAttributes.MagneticRadius > 0f)
						{
							list.Add(((object)ModEntry.I18n.Get("lookup.buff.magnetism", (object)new
							{
								level = customAttributes.MagneticRadius
							})).ToString());
						}
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	/// <summary>Finds every incomplete Community Center bundle that still needs this item.</summary>
	private static List<string> GetNeededBundles(Item item)
	{
		List<string> list = new List<string>();
		try
		{
			if (Game1.player.hasCompletedCommunityCenter() || ((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("JojaMember"))
			{
				return list;
			}
			Dictionary<string, string> dictionary = DataLoader.Bundles(Game1.content);
			if (dictionary == null || ((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.Bundles == null)
			{
				return list;
			}
			Dictionary<string, string>? bundleNamesDict = null;
			try
			{
				bundleNamesDict = Game1.content.Load<Dictionary<string, string>>("Strings\\BundleNames");
			}
			catch { }

			bool[] array4 = default(bool[]);
			foreach (KeyValuePair<string, string> item3 in dictionary)
			{
				string key = item3.Key;
				string[] array = key.Split('/');
				if (array.Length < 2 || !int.TryParse(array[1], out var result))
				{
					continue;
				}
				string value = item3.Value;
				string[] array2 = value.Split('/');
				if (array2.Length < 3)
				{
					continue;
				}
				string item2 = array2[0];
				if (array2.Length >= 7 && !string.IsNullOrWhiteSpace(array2[6]))
				{
					item2 = array2[6].Trim();
				}
				else if (array2.Length >= 6 && !string.IsNullOrWhiteSpace(array2[5]))
				{
					item2 = array2[5].Trim();
				}
				else if (bundleNamesDict != null && bundleNamesDict.TryGetValue(array2[0], out string? locName) && !string.IsNullOrWhiteSpace(locName))
				{
					item2 = locName.Trim();
				}
				// Field [2] = flat "id stack quality id stack quality ..." triplet list.
				string[] array3 = array2[2].Split(' ');
				if (!((NetDictionary<int, bool[], NetArray<bool, NetBool>, SerializableDictionary<int, bool[]>, NetBundles>)(object)((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.Bundles).TryGetValue(result, out array4))
				{
					continue;
				}
				int num = ((array2.Length > 4 && int.TryParse(array2[4], out var result2)) ? result2 : array4.Length);
				int num2 = array4.Count((bool b) => b);
				if (num2 >= num)
				{
					continue;
				}
				for (int num3 = 0; num3 < array4.Length; num3++)
				{
					if (!array4[num3])
					{
						int num4 = num3 * 3;
						if (num4 + 2 >= array3.Length)
						{
							break;
						}
						string text = array3[num4];
						int num5 = (int.TryParse(array3[num4 + 2], out var result3) ? result3 : 0);
						int num6;
						if (!(text == item.ItemId) && !(text == item.QualifiedItemId))
						{
							SObject val = (SObject)(object)((item is SObject) ? item : null);
							num6 = ((val != null && (text == ((Item)val).ParentSheetIndex.ToString() || text == ((Item)val).ItemId)) ? 1 : 0);
						}
						else
						{
							num6 = 1;
						}
						bool flag = (byte)num6 != 0;
						// Negative ingredient ids mean "any item of that Category"
						// (e.g. -77 for "any vegetable"), so compare categories too.
						bool flag2 = int.TryParse(text, out var result4) && result4 < 0 && item.Category == result4;
						// The bundle may demand a minimum quality (field [num4 + 2]).
						bool flag3 = item.Quality >= num5;
						if (((flag | flag2) & flag3) && !list.Contains(item2))
						{
							list.Add(item2);
						}
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	/// <summary>Villagers who love or like this item, as clickable NPC cards.</summary>
	private static (List<LookupLink> Lovers, List<LookupLink> Likers) GetItemGiftTastesLinks(Item item)
	{
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		List<LookupLink> list = new List<LookupLink>();
		List<LookupLink> list2 = new List<LookupLink>();
		foreach (NPC npc in Utility.getAllCharacters())
		{
			if (npc == null || !((Character)npc).IsVillager || ((Character)npc).IsMonster || string.IsNullOrEmpty(((Character)npc).Name))
			{
				continue;
			}
			int giftTasteForThisItem = npc.getGiftTasteForThisItem(item);
			// Gift-taste constants: 0 = love, 2 = like (1/3/4... are neutral/dislike/hate).
			if (giftTasteForThisItem == 0 && !list.Any((LookupLink l) => l.Text == (((Character)npc).displayName ?? ((Character)npc).Name)))
			{
				NPC targetNPC = npc;
				list.Add(new LookupLink(((Character)targetNPC).displayName ?? ((Character)targetNPC).Name, (string?)null, (Color?)new Color(180, 50, 180), targetNPC.Portrait, (Rectangle?)new Rectangle(0, 0, 64, 64), (Func<LookupSubject?>?)(() => BuildNPCSubject(targetNPC))));
			}
			else if (giftTasteForThisItem == 2 && !list2.Any((LookupLink l) => l.Text == (((Character)npc).displayName ?? ((Character)npc).Name)))
			{
				NPC targetNPC2 = npc;
				list2.Add(new LookupLink(((Character)targetNPC2).displayName ?? ((Character)targetNPC2).Name, (string?)null, (Color?)new Color(0, 140, 0), targetNPC2.Portrait, (Rectangle?)new Rectangle(0, 0, 64, 64), (Func<LookupSubject?>?)(() => BuildNPCSubject(targetNPC2))));
			}
		}
		return (Lovers: list.Take(12).ToList(), Likers: list2.Take(12).ToList());
	}

	/// <summary>Crafting/cooking recipes that consume this item, as clickable output cards.</summary>
	private static List<LookupLink> GetRecipesUsingItemLinks(Item item)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Expected O, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
		List<LookupLink> list = new List<LookupLink>();
		// Crafting recipes live in one static dictionary, cooking recipes in another;
		// both store "recipe string" values parsed by RecipeContainsItem below.
		foreach (KeyValuePair<string, string> craftingRecipe in CraftingRecipe.craftingRecipes)
		{
			string recipeName = craftingRecipe.Key;
			string value = craftingRecipe.Value;
			if (RecipeContainsItem(value, item) && !list.Any((LookupLink r) => r.Text == recipeName))
			{
				CraftingRecipe val = new CraftingRecipe(recipeName, false);
				Item outputItem = val.createItem();
				ParsedItemData val2 = ((outputItem != null) ? ItemRegistry.GetData(outputItem.QualifiedItemId) : null);
				list.Add(new LookupLink(val.DisplayName, null, Game1.textColor, (val2 != null) ? val2.GetTexture() : null, (val2 != null) ? new Rectangle?(val2.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), () => (outputItem != null) ? BuildItemSubject(outputItem) : null));
			}
		}
		foreach (KeyValuePair<string, string> cookingRecipe in CraftingRecipe.cookingRecipes)
		{
			string recipeName2 = cookingRecipe.Key;
			string value2 = cookingRecipe.Value;
			if (RecipeContainsItem(value2, item) && !list.Any((LookupLink r) => r.Text == recipeName2))
			{
				CraftingRecipe val3 = new CraftingRecipe(recipeName2, true);
				Item outputItem2 = val3.createItem();
				ParsedItemData val4 = ((outputItem2 != null) ? ItemRegistry.GetData(outputItem2.QualifiedItemId) : null);
				list.Add(new LookupLink(val3.DisplayName, null, Game1.textColor, (val4 != null) ? val4.GetTexture() : null, (val4 != null) ? new Rectangle?(val4.GetSourceRect(0, (int?)null)) : ((Rectangle?)null), () => (outputItem2 != null) ? BuildItemSubject(outputItem2) : null));
			}
		}
		return list.Take(12).ToList();
	}

	/// <summary>Checks a recipe's ingredient string ("id count id count ...") for this item;
	/// a negative id means "any item of that category".</summary>
	private static bool RecipeContainsItem(string recipeStr, Item item)
	{
		string[] array = recipeStr.Split('/');
		if (array.Length < 1)
		{
			return false;
		}
		string[] array2 = array[0].Split(' ');
		// Ingredient pairs are "id count" repeated, so step the loop 2 at a time.
		for (int i = 0; i < array2.Length; i += 2)
		{
			string text = array2[i];
			if (text == item.ItemId || text == item.QualifiedItemId || text == item.Category.ToString())
			{
				return true;
			}
		}
		return false;
	}

	
    }
}




