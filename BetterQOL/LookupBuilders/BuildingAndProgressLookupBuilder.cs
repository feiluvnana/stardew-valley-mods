using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Buildings;
using StardewValley.GameData;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FishPonds;
using StardewValley.GameData.Objects;
using StardewValley.Inventories;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Monsters;
using StardewValley.Network;
using StardewValley.Objects;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using SObject = StardewValley.Object;

namespace BetterQOL
{
    /// <summary>
    /// Lookup builder for farm buildings, fish ponds, farm summaries, perfection, Community Center, and almanac.
    /// </summary>
    /// <remarks>
    /// BEGINNER NOTES:
    /// - This is the biggest builder file: it powers building/chest/farmer cards AND the big
    ///   "world overview" almanac card assembled from many smaller section-builders below.
    /// - Game progress is tracked via MAIL FLAGS (strings like "ccBoilerRoom" stored in
    ///   mailReceived sets - the game's universal "has this happened?" mechanism) and STATS
    ///   (Game1.player.stats numeric counters).
    /// - Raw data files (DataLoader.Bundles/Objects/Fish...) are '/'-separated text tables;
    ///   parsing them means Split('/') plus int.TryParse with index guards everywhere.
    /// - The "//IL_xxxx:" markers, "if (1 == 0)" blocks and redundant casts are decompiler
    ///   artifacts kept untouched on purpose.
    /// </remarks>
    public static partial class LookupDataManager
    {
	/// <summary>
	/// Builds any farm-building card by dispatching on its type: Junimo hut, barn/coop
	/// (with incubators), mill, shipping bin, silo, slime hutch, stable, pet bowl.
	/// </summary>
	public static LookupSubject BuildBuildingSubject(Building building)
	{
		//IL_0ed7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ec9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b00: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d9c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d8c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0807: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ff1: Unknown result type (might be due to invalid IL or missing references)
		//IL_098e: Unknown result type (might be due to invalid IL or missing references)
		//IL_083c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c39: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_066d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c6e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		string value = ((NetFieldBase<string, NetString>)(object)building.buildingType).Value;
		LookupSubject lookupSubject = new LookupSubject
		{
			Title = ((!string.IsNullOrEmpty(value)) ? value : ((object)ModEntry.I18n.Get("lookup.building.default-farm-building")).ToString()),
			Subtitle = ((object)ModEntry.I18n.Get("lookup.type.building")).ToString()
		};
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.building-status")));
		// Dispatch by concrete type: "X is Y ? X : null" is the decompiler's rendering of the
		// C# "building as JunimoHut" / pattern-match cast - null when the type doesn't match.
		JunimoHut val = (JunimoHut)(object)((building is JunimoHut) ? building : null);
		if (val != null)
		{
			lookupSubject.Title = ((object)ModEntry.I18n.Get("lookup.building.junimo-hut")).ToString();
			// Double negative: the field is "noHarvest", so NOT noHarvest = harvesting enabled.
			bool flag = !((NetFieldBase<bool, NetBool>)(object)val.noHarvest).Value;
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.harvesting-state")), flag ? ((object)ModEntry.I18n.Get("lookup.building.harvesting-active")).ToString() : ((object)ModEntry.I18n.Get("lookup.building.harvesting-paused")).ToString(), (Color?)(flag ? new Color(0, 140, 0) : new Color(200, 60, 20))));
			int value2 = ((NetFieldBase<int, NetInt>)(object)val.raisinDays).Value;
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.raisins-boost")), (value2 > 0) ? ((object)ModEntry.I18n.Get("lookup.building.raisins-active", (object)new
			{
				days = value2
			})).ToString() : ((object)ModEntry.I18n.Get("lookup.building.raisins-none")).ToString(), (Color)((value2 > 0) ? new Color(180, 50, 180) : Color.DarkSlateGray)));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.junimo.harvest-range")), ((object)ModEntry.I18n.Get("lookup.junimo.radius-desc")).ToString(), (Color?)new Color(20, 110, 220)));
			Chest outputChest = val.GetOutputChest();
			if (outputChest != null && ((IEnumerable<Item>)outputChest.Items).Any((Item i) => i != null))
			{
				List<LookupLink> list = new List<LookupLink>();
				foreach (Item sItem in ((IEnumerable<Item>)outputChest.Items).Where((Item i) => i != null))
				{
					ParsedItemData data = ItemRegistry.GetData(sItem.QualifiedItemId);
					list.Add(new LookupLink($"{sItem.DisplayName} (x{sItem.Stack})", null, Game1.textColor, (data != null) ? data.GetTexture() : null, (data != null) ? new Rectangle?(data.GetSourceRect(0, null)) : (null), () => BuildItemSubject(sItem)));
				}
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.junimo.stored-output")), list));
			}
		}
		else
		{
			GameLocation value3 = ((NetFieldBase<GameLocation, NetRef<GameLocation>>)(object)building.indoors).Value;
			AnimalHouse val2 = (AnimalHouse)(object)((value3 is AnimalHouse) ? value3 : null);
			if (val2 != null)
			{
				int count = ((NetList<long, NetLong>)(object)val2.animalsThatLiveHere).Count;
				int value4 = ((NetFieldBase<int, NetInt>)(object)val2.animalLimit).Value;
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.animal-capacity")), ((object)ModEntry.I18n.Get("lookup.building.animal-capacity-format", (object)new
				{
					count = count,
					max = value4
				})).ToString(), (Color?)((count >= value4) ? new Color(0, 140, 0) : new Color(20, 110, 220))));
				foreach (SObject value9 in ((GameLocation)val2).objects.Values)
				{
					if (((Item)value9).Name != null && ((Item)value9).Name.Contains("Incubator") && ((NetFieldBase<SObject, NetRef<SObject>>)(object)value9.heldObject).Value != null)
					{
						SObject value5 = ((NetFieldBase<SObject, NetRef<SObject>>)(object)value9.heldObject).Value;
						// Incubators count down in game-minutes; ~1000 minutes ≈ one in-game day,
						// so integer division gives the remaining whole days.
						int days = value9.MinutesUntilReady / 1000;
						lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.incubator")), ((object)ModEntry.I18n.Get("lookup.incubator.hatching-format", (object)new
						{
							egg = ((Item)value5).DisplayName,
							days = days
						})).ToString(), (Color?)new Color(180, 100, 0)));
					}
				}
			}
			else if (value.Contains("Mill"))
			{
				lookupSubject.Title = ((object)ModEntry.I18n.Get("lookup.building.mill")).ToString();
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.processing-rules")), ((object)ModEntry.I18n.Get("lookup.mill.rules")).ToString(), (Color?)new Color(180, 100, 0)));
				Chest buildingChest = building.GetBuildingChest("Input");
				if (buildingChest != null && ((IEnumerable<Item>)buildingChest.Items).Any((Item i) => i != null))
				{
					List<LookupLink> list3 = new List<LookupLink>();
					foreach (Item item in ((IEnumerable<Item>)buildingChest.Items).Where((Item i) => i != null))
					{
						ParsedItemData data2 = ItemRegistry.GetData(item.QualifiedItemId);
						list3.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.building.mill-processing", (object)new
						{
							name = item.DisplayName,
							stack = item.Stack
						})).ToString(), null, Game1.textColor, (data2 != null) ? data2.GetTexture() : null, (data2 != null) ? new Rectangle?(data2.GetSourceRect(0, null)) : (null), () => BuildItemSubject(item)));
					}
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mill.input-label")), list3));
				}
				Chest buildingChest2 = building.GetBuildingChest("Output");
				if (buildingChest2 != null && ((IEnumerable<Item>)buildingChest2.Items).Any((Item i) => i != null))
				{
					List<LookupLink> list4 = new List<LookupLink>();
					foreach (Item item2 in ((IEnumerable<Item>)buildingChest2.Items).Where((Item i) => i != null))
					{
						ParsedItemData data3 = ItemRegistry.GetData(item2.QualifiedItemId);
						list4.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.building.mill-ready", (object)new
						{
							name = item2.DisplayName,
							stack = item2.Stack
						})).ToString(), (string?)null, (Color?)new Color(0, 140, 0), (data3 != null) ? data3.GetTexture() : null, (data3 != null) ? new Rectangle?(data3.GetSourceRect(0, null)) : (null), (Func<LookupSubject?>?)(() => BuildItemSubject(item2))));
					}
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mill.output-label")), list4));
				}
			}
			else if (value.Contains("Shipping"))
			{
				lookupSubject.Title = ((object)ModEntry.I18n.Get("lookup.building.shipping-bin")).ToString();
				Farm farm = Game1.getFarm();
				if (farm != null)
				{
					IInventory shippingBin = farm.getShippingBin(Game1.player);
					int count2 = ((ICollection<Item>)shippingBin)?.Count ?? 0;
					int value6 = ((IEnumerable<Item>)shippingBin)?.Sum((Item i) => i.sellToStorePrice(-1L) * i.Stack) ?? 0;
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.shipping.pending-items")), ((object)ModEntry.I18n.Get("lookup.shipping.pending-format", (object)new
					{
						count = count2
					})).ToString(), (Color?)new Color(20, 110, 220)));
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.shipping.revenue")), ((object)ModEntry.I18n.Get("lookup.shipping.revenue-format", (object)new
					{
						revenue = $"{value6:N0}"
					})).ToString(), (Color?)new Color(0, 140, 0)));
					if (shippingBin != null && ((ICollection<Item>)shippingBin).Count > 0)
					{
						List<LookupLink> list5 = new List<LookupLink>();
						// Show at most 36 items to keep the card from growing unbounded.
						foreach (Item item3 in ((IEnumerable<Item>)shippingBin).Take(36))
						{
							ParsedItemData data4 = ItemRegistry.GetData(item3.QualifiedItemId);
							list5.Add(new LookupLink($"{item3.DisplayName} (x{item3.Stack})", null, Game1.textColor, (data4 != null) ? data4.GetTexture() : null, (data4 != null) ? new Rectangle?(data4.GetSourceRect(0, null)) : (null), () => BuildItemSubject(item3)));
						}
						lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.shipping.contents")), list5));
					}
				}
			}
			else if (value.Contains("Silo"))
			{
				Farm farm2 = Game1.getFarm();
				int num2 = farm2?.piecesOfHay.Value ?? 0;
				// Each silo adds 240 hay capacity; red when under a quarter full.
				int num3 = (farm2?.buildings.Count(b => b.buildingType.Value.Contains("Silo")) ?? 1) * 240;
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.silo.capacity")), ((object)ModEntry.I18n.Get("lookup.silo.hay-format", (object)new
				{
					current = num2,
					max = num3
				})).ToString(), (Color?)((num2 < num3 / 4) ? new Color(200, 60, 20) : new Color(0, 140, 0))));
			}
			else
			{
				GameLocation value7 = ((NetFieldBase<GameLocation, NetRef<GameLocation>>)(object)building.indoors).Value;
				SlimeHutch val3 = (SlimeHutch)(object)((value7 is SlimeHutch) ? value7 : null);
				if (val3 != null)
				{
					int current3 = ((IEnumerable<NPC>)((GameLocation)val3).characters).Count((NPC c) => c is GreenSlime);
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.slime-hutch.population")), ((object)ModEntry.I18n.Get("lookup.slime-hutch.population-format", (object)new
					{
						current = current3,
						max = 20
					})).ToString(), (Color?)new Color(0, 140, 0)));
					int num4 = ((IEnumerable<bool>)val3.waterSpots).Count((bool w) => w);
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.slime-hutch.water-troughs")), ((object)ModEntry.I18n.Get("lookup.slime-hutch.troughs-format", (object)new
					{
						watered = num4,
						total = 4
					})).ToString(), (Color?)((num4 == 4) ? new Color(0, 140, 0) : new Color(200, 60, 20))));
				}
				else
				{
					Stable val4 = (Stable)(object)((building is Stable) ? building : null);
					if (val4 != null)
					{
						string text = ((NetFieldBase<string, NetString>)(object)Game1.player.horseName).Value ?? ((object)ModEntry.I18n.Get("hover.stable.horse")).ToString();
						lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.stable.horse")), text ?? "", (Color?)new Color(180, 100, 0)));
					}
					else
					{
						PetBowl val5 = (PetBowl)(object)((building is PetBowl) ? building : null);
						if (val5 != null)
						{
							bool value8 = ((NetFieldBase<bool, NetBool>)(object)val5.watered).Value;
							lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.petbowl.water-status")), value8 ? ((object)ModEntry.I18n.Get("lookup.petbowl.water-status-filled")).ToString() : ((object)ModEntry.I18n.Get("lookup.petbowl.water-status-empty")).ToString(), (Color?)(value8 ? new Color(0, 140, 0) : new Color(200, 60, 20))));
						}
					}
				}
			}
		}
		lookupSubject.Sections.Add(lookupSection);
		return lookupSubject;
	}

	/// <summary>Builds a chest/fridge card: capacity usage, total stacked items, and clickable contents.</summary>
	public static LookupSubject BuildChestSubject(Chest chest)
	{
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		string title = ((Item)chest).DisplayName ?? ((object)ModEntry.I18n.Get("lookup.chest.default-name")).ToString();
		int num = ((IEnumerable<Item>)chest.Items).Count((Item i) => i != null);
		int actualCapacity = chest.GetActualCapacity();
		int value = ((IEnumerable<Item>)chest.Items).Where((Item i) => i != null).Sum((Item i) => i.Stack);
		LookupSubject lookupSubject = new LookupSubject
		{
			Title = title,
			Subtitle = ((object)ModEntry.I18n.Get("lookup.type.storage-container")).ToString()
		};
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.storage-overview")));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.storage-capacity")), ((object)ModEntry.I18n.Get("lookup.building.storage-capacity-format", (object)new
		{
			used = num,
			total = actualCapacity,
			free = actualCapacity - num
		})).ToString(), (Color?)((num >= actualCapacity) ? new Color(200, 60, 20) : new Color(0, 140, 0))));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chest.total-items")), ((object)ModEntry.I18n.Get("lookup.chest.total-items-format", (object)new
		{
			count = $"{value:N0}"
		})).ToString(), (Color?)new Color(20, 110, 220)));
		if (num > 0)
		{
			List<LookupLink> list = new List<LookupLink>();
			foreach (Item item in ((IEnumerable<Item>)chest.Items).Where((Item i) => i != null).Take(36))
			{
				ParsedItemData data = ItemRegistry.GetData(item.QualifiedItemId);
				Item target = item;
				list.Add(new LookupLink($"{target.DisplayName} (x{target.Stack})", null, Game1.textColor, (data != null) ? data.GetTexture() : null, (data != null) ? new Rectangle?(data.GetSourceRect(0, null)) : (null), () => BuildItemSubject(target)));
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chest.stored-items")), list));
		}
		lookupSubject.Sections.Add(lookupSection);
		return lookupSubject;
	}

	/// <summary>Farmer card: skills/professions, gear links, wallet, stardrops, buffs, misc stats.</summary>
	public static LookupSubject BuildFarmerSubject(Farmer farmer)
	{
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0766: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b9f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0702: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b3e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c55: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c47: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ce1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cd3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d6d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0d5f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0df9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0deb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e85: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e77: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f11: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f03: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fcb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0fbd: Unknown result type (might be due to invalid IL or missing references)
		//IL_1085: Unknown result type (might be due to invalid IL or missing references)
		//IL_1077: Unknown result type (might be due to invalid IL or missing references)
		//IL_1111: Unknown result type (might be due to invalid IL or missing references)
		//IL_1103: Unknown result type (might be due to invalid IL or missing references)
		//IL_119d: Unknown result type (might be due to invalid IL or missing references)
		//IL_118f: Unknown result type (might be due to invalid IL or missing references)
		//IL_122e: Unknown result type (might be due to invalid IL or missing references)
		//IL_1295: Unknown result type (might be due to invalid IL or missing references)
		//IL_12ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_134f: Unknown result type (might be due to invalid IL or missing references)
		//IL_13ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_13bc: Unknown result type (might be due to invalid IL or missing references)
		LookupSubject lookupSubject = new LookupSubject
		{
			Title = ((Character)farmer).Name,
			Subtitle = ((object)ModEntry.I18n.Get("lookup.farmer.farm-subtitle", (object)new
			{
				farm = ((NetFieldBase<string, NetString>)(object)farmer.farmName).Value,
				title = farmer.getTitle()
			})).ToString()
		};
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.status")));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.health")), ((object)ModEntry.I18n.Get("lookup.farmer.hp-format", (object)new
		{
			current = farmer.health,
			max = farmer.maxHealth
		})).ToString(), (Color?)new Color(220, 20, 60)));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.energy")), ((object)ModEntry.I18n.Get("lookup.farmer.energy-format", (object)new
		{
			current = (int)farmer.Stamina,
			max = farmer.MaxStamina
		})).ToString(), (Color?)new Color(0, 140, 0)));
		// Stardrop count derived backwards from max energy: base game starts at 270 and
		// each of the 7 stardrops adds exactly +34, so divide and clamp into 0..7.
		int num = Math.Clamp((farmer.MaxStamina - 270) / 34, 0, 7);
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.stardrops")), ((object)ModEntry.I18n.Get("lookup.perfection.stardrops-found-format", (object)new
		{
			count = num
		})).ToString(), (Color?)((num == 7) ? new Color(0, 140, 0) : new Color(180, 50, 180))));
		lookupSubject.Sections.Add(lookupSection);
		LookupSection lookupSection2 = new LookupSection((ModEntry.I18n.Get("lookup.section.active-buffs")));
		if (farmer.buffs != null && farmer.buffs.AppliedBuffs.Count > 0)
		{
			foreach (KeyValuePair<string, Buff> appliedBuff in farmer.buffs.AppliedBuffs)
			{
				Buff value = appliedBuff.Value;
				string label = ((!string.IsNullOrEmpty(value.displayName)) ? value.displayName : appliedBuff.Key);
				// Buff timers are stored in milliseconds: 60000 ms = 1 minute, and the
				// remainder % 60000 / 1000 converts to leftover seconds. Huge values mean permanent.
				string text = ((value.millisecondsDuration > 0 && value.millisecondsDuration < 9999999) ? ((object)ModEntry.I18n.Get("lookup.buff.duration-left", (object)new
				{
					m = value.millisecondsDuration / 60000,
					s = value.millisecondsDuration % 60000 / 1000
				})).ToString() : ((object)ModEntry.I18n.Get("lookup.buff.permanent")).ToString());
				List<string> list = new List<string>();
				BuffEffects effects = value.effects;
				if (effects != null)
				{
					if (((NetFieldBase<float, NetFloat>)(object)effects.Speed).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.speed", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.Speed).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.Attack).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.attack", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.Attack).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.Defense).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.defense", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.Defense).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.LuckLevel).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.luck", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.LuckLevel).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.FarmingLevel).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.farming", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.FarmingLevel).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.MiningLevel).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.mining", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.MiningLevel).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.FishingLevel).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.fishing", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.FishingLevel).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.ForagingLevel).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.foraging", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.ForagingLevel).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.MaxStamina).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.max-energy", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.MaxStamina).Value:0.#}"
						})).ToString());
					}
					if (((NetFieldBase<float, NetFloat>)(object)effects.MagneticRadius).Value != 0f)
					{
						list.Add(((object)ModEntry.I18n.Get("lookup.buff.magnetism", (object)new
						{
							level = $"{((NetFieldBase<float, NetFloat>)(object)effects.MagneticRadius).Value:0.#}"
						})).ToString());
					}
				}
				string text2 = ((list.Count > 0) ? (" (" + string.Join(", ", list) + ")") : "");
				lookupSection2.Fields.Add(new LookupField(label, text + text2, (Color?)new Color(180, 50, 180)));
			}
		}
		else
		{
			lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.buff.active-label")), ((object)ModEntry.I18n.Get("lookup.buff.none-active")).ToString(), Color.DarkSlateGray));
		}
		lookupSubject.Sections.Add(lookupSection2);
		LookupSection lookupSection3 = new LookupSection((ModEntry.I18n.Get("lookup.farmer.gear")));
		List<LookupLink> gearLinks = new List<LookupLink>();
		AddGearLink(((object)ModEntry.I18n.Get("lookup.slot.tool")).ToString(), (Item?)(object)farmer.CurrentTool);
		AddGearLink(((object)ModEntry.I18n.Get("lookup.slot.hat")).ToString(), (Item?)(object)((NetFieldBase<Hat, NetRef<Hat>>)(object)farmer.hat).Value);
		AddGearLink(((object)ModEntry.I18n.Get("lookup.slot.shirt")).ToString(), (Item?)(object)((NetFieldBase<Clothing, NetRef<Clothing>>)(object)farmer.shirtItem).Value);
		AddGearLink(((object)ModEntry.I18n.Get("lookup.slot.pants")).ToString(), (Item?)(object)((NetFieldBase<Clothing, NetRef<Clothing>>)(object)farmer.pantsItem).Value);
		AddGearLink(((object)ModEntry.I18n.Get("lookup.slot.boots")).ToString(), (Item?)(object)((NetFieldBase<Boots, NetRef<Boots>>)(object)farmer.boots).Value);
		AddGearLink(((object)ModEntry.I18n.Get("lookup.slot.left-ring")).ToString(), (Item?)(object)((NetFieldBase<Ring, NetRef<Ring>>)(object)farmer.leftRing).Value);
		AddGearLink(((object)ModEntry.I18n.Get("lookup.slot.right-ring")).ToString(), (Item?)(object)((NetFieldBase<Ring, NetRef<Ring>>)(object)farmer.rightRing).Value);
		if (farmer.trinketItems.Count > 0 && farmer.trinketItems[0] != null)
		{
			AddGearLink(((object)ModEntry.I18n.Get("lookup.slot.trinket")).ToString(), (Item?)(object)farmer.trinketItems[0]);
		}
		if (gearLinks.Count > 0)
		{
			lookupSection3.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.gear.equipped-items")), gearLinks));
		}
		int num2 = 0;
		int num3 = 0;
		Boots value2 = ((NetFieldBase<Boots, NetRef<Boots>>)(object)farmer.boots).Value;
		if (value2 != null)
		{
			num2 += ((NetFieldBase<int, NetInt>)(object)value2.defenseBonus).Value;
			num3 += ((NetFieldBase<int, NetInt>)(object)value2.immunityBonus).Value;
		}
		// A few ring ids contribute defense/immunity that isn't exposed as a plain stat,
		// so their bonuses are added by hand for both ring slots.
		if (((NetFieldBase<Ring, NetRef<Ring>>)(object)farmer.leftRing).Value != null)
		{
			string itemId = ((Item)((NetFieldBase<Ring, NetRef<Ring>>)(object)farmer.leftRing).Value).ItemId;
			if (itemId == "524")
			{
				num2 += 5;
			}
			if (itemId == "517")
			{
				num2++;
			}
			if (itemId == "525")
			{
				num3 += 4;
			}
		}
		if (((NetFieldBase<Ring, NetRef<Ring>>)(object)farmer.rightRing).Value != null)
		{
			string itemId2 = ((Item)((NetFieldBase<Ring, NetRef<Ring>>)(object)farmer.rightRing).Value).ItemId;
			if (itemId2 == "524")
			{
				num2 += 5;
			}
			if (itemId2 == "517")
			{
				num2++;
			}
			if (itemId2 == "525")
			{
				num3 += 4;
			}
		}
		lookupSection3.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.gear.total-def-imm")), ((object)ModEntry.I18n.Get("lookup.gear.total-def-imm-format", (object)new
		{
			def = num2,
			imm = num3
		})).ToString(), (Color?)new Color(20, 110, 220)));
		lookupSubject.Sections.Add(lookupSection3);
		LookupSection lookupSection4 = new LookupSection((ModEntry.I18n.Get("lookup.farmer.professions")));
		if (((NetHashSet<int>)(object)farmer.professions).Count > 0)
		{
			foreach (int item in (NetHashSet<int>)(object)farmer.professions)
			{
				string professionName = GetProfessionName(item);
				lookupSection4.Fields.Add(new LookupField("•", professionName, (Color?)new Color(0, 140, 0)));
			}
		}
		else
		{
			lookupSection4.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.professions-label")), ((object)ModEntry.I18n.Get("lookup.farmer.no-professions")).ToString(), Color.DarkSlateGray));
		}
		lookupSubject.Sections.Add(lookupSection4);
		LookupSection lookupSection5 = new LookupSection((ModEntry.I18n.Get("lookup.section.wallet-powers")));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.special-charm")), farmer.hasSpecialCharm ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.special-charm-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)(farmer.hasSpecialCharm ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.skull-key")), farmer.hasSkullKey ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.skull-key-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)(farmer.hasSkullKey ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.club-card")), farmer.hasClubCard ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.club-card-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)(farmer.hasClubCard ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.magnifying-glass")), farmer.hasMagnifyingGlass ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.magnifying-glass-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)(farmer.hasMagnifyingGlass ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.dark-talisman")), farmer.hasDarkTalisman ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.dark-talisman-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)(farmer.hasDarkTalisman ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.magic-ink")), farmer.hasMagicInk ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.magic-ink-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)(farmer.hasMagicInk ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.bears-knowledge")), (((NetHashSet<string>)(object)farmer.eventsSeen).Contains("2120303") || farmer.hasOrWillReceiveMail("BearKnowledge")) ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.bears-knowledge-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)((((NetHashSet<string>)(object)farmer.eventsSeen).Contains("2120303") || farmer.hasOrWillReceiveMail("BearKnowledge")) ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.spring-onion-mastery")), (((NetHashSet<string>)(object)farmer.eventsSeen).Contains("3910979") || farmer.hasOrWillReceiveMail("SpringOnionMastery")) ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.spring-onion-mastery-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)((((NetHashSet<string>)(object)farmer.eventsSeen).Contains("3910979") || farmer.hasOrWillReceiveMail("SpringOnionMastery")) ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.dwarvish-translation")), farmer.canUnderstandDwarves ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.dwarvish-translation-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)(farmer.canUnderstandDwarves ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSection5.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.key-to-town")), farmer.HasTownKey ? ((object)ModEntry.I18n.Get("lookup.wallet.unlocked", (object)new
		{
			desc = ModEntry.I18n.Get("lookup.wallet.key-to-town-desc")
		})).ToString() : ((object)ModEntry.I18n.Get("lookup.wallet.locked")).ToString(), (Color)(farmer.HasTownKey ? new Color(0, 140, 0) : Color.DarkSlateGray)));
		lookupSubject.Sections.Add(lookupSection5);
		LookupSection lookupSection6 = new LookupSection((ModEntry.I18n.Get("lookup.section.farmer-statistics")));
		lookupSection6.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.current-gold")), $"{farmer.Money:N0}g", (Color?)new Color(180, 100, 0)));
		lookupSection6.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.total-earnings")), $"{farmer.totalMoneyEarned:N0}g", (Color?)new Color(0, 140, 0)));
		lookupSection6.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.qi-gems")), $"{farmer.QiGems}", (Color?)new Color(180, 50, 180)));
		lookupSection6.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.island.walnuts")), ((object)ModEntry.I18n.Get("lookup.island.walnuts-format", (object)new
		{
			count = ((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.GoldenWalnutsFound
		})).ToString(), (Color?)new Color(180, 100, 0)));
		lookupSection6.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.daily-luck")), $"{farmer.DailyLuck:F3}", (Color?)((farmer.DailyLuck >= 0.0) ? new Color(0, 140, 0) : new Color(200, 60, 20))));
		lookupSubject.Sections.Add(lookupSection6);
		lookupSubject.Sections.Add(BuildSkillsAndMasterySection());
		return lookupSubject;
		// A "local function": a method declared inside another method. It can use the
		// gearLinks list above directly (a "closure" over the surrounding variables).
		void AddGearLink(string slotName, Item? gItem)
		{
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			if (gItem != null)
			{
				ParsedItemData data = ItemRegistry.GetData(gItem.QualifiedItemId);
				gearLinks.Add(new LookupLink(slotName + ": " + gItem.DisplayName, null, Game1.textColor, (data != null) ? data.GetTexture() : null, (data != null) ? new Rectangle?(data.GetSourceRect(0, null)) : (null), () => BuildItemSubject(gItem)));
			}
		}
	}

	/// <summary>Maps a profession id (0-29+) onto its localized name via one giant switch.</summary>
	private static string GetProfessionName(int id)
	{
		// "if (1 == 0)" is always false - a harmless leftover from the decompiler.
		if (1 == 0)
		{
		}
		string result = id switch
		{
			0 => ((object)ModEntry.I18n.Get("lookup.profession.0")).ToString(), 
			1 => ((object)ModEntry.I18n.Get("lookup.profession.1")).ToString(), 
			2 => ((object)ModEntry.I18n.Get("lookup.profession.2")).ToString(), 
			3 => ((object)ModEntry.I18n.Get("lookup.profession.3")).ToString(), 
			4 => ((object)ModEntry.I18n.Get("lookup.profession.4")).ToString(), 
			5 => ((object)ModEntry.I18n.Get("lookup.profession.5")).ToString(), 
			6 => ((object)ModEntry.I18n.Get("lookup.profession.6")).ToString(), 
			7 => ((object)ModEntry.I18n.Get("lookup.profession.7")).ToString(), 
			8 => ((object)ModEntry.I18n.Get("lookup.profession.8")).ToString(), 
			9 => ((object)ModEntry.I18n.Get("lookup.profession.9")).ToString(), 
			10 => ((object)ModEntry.I18n.Get("lookup.profession.10")).ToString(), 
			11 => ((object)ModEntry.I18n.Get("lookup.profession.11")).ToString(), 
			12 => ((object)ModEntry.I18n.Get("lookup.profession.12")).ToString(), 
			13 => ((object)ModEntry.I18n.Get("lookup.profession.13")).ToString(), 
			14 => ((object)ModEntry.I18n.Get("lookup.profession.14")).ToString(), 
			15 => ((object)ModEntry.I18n.Get("lookup.profession.15")).ToString(), 
			16 => ((object)ModEntry.I18n.Get("lookup.profession.16")).ToString(), 
			17 => ((object)ModEntry.I18n.Get("lookup.profession.17")).ToString(), 
			18 => ((object)ModEntry.I18n.Get("lookup.profession.18")).ToString(), 
			19 => ((object)ModEntry.I18n.Get("lookup.profession.19")).ToString(), 
			20 => ((object)ModEntry.I18n.Get("lookup.profession.20")).ToString(), 
			21 => ((object)ModEntry.I18n.Get("lookup.profession.21")).ToString(), 
			22 => ((object)ModEntry.I18n.Get("lookup.profession.22")).ToString(), 
			23 => ((object)ModEntry.I18n.Get("lookup.profession.23")).ToString(), 
			24 => ((object)ModEntry.I18n.Get("lookup.profession.24")).ToString(), 
			25 => ((object)ModEntry.I18n.Get("lookup.profession.25")).ToString(), 
			26 => ((object)ModEntry.I18n.Get("lookup.profession.26")).ToString(), 
			27 => ((object)ModEntry.I18n.Get("lookup.profession.27")).ToString(), 
			28 => ((object)ModEntry.I18n.Get("lookup.profession.28")).ToString(), 
			29 => ((object)ModEntry.I18n.Get("lookup.profession.29")).ToString(), 
			_ => ((object)ModEntry.I18n.Get("lookup.profession.unknown", (object)new { id })).ToString(), 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	/// <summary>
	/// Fish-pond card: population vs capacity, roe quality/price forecast (including aged
	/// roe and the Sturgeon's caviar special case), drop chances, and quests.
	/// </summary>
	public static LookupSubject BuildFishPondSubject(FishPond pond)
	{
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_054a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0784: Unknown result type (might be due to invalid IL or missing references)
		//IL_0776: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ac: Unknown result type (might be due to invalid IL or missing references)
		string value = ((NetFieldBase<string, NetString>)(object)pond.fishType).Value;
		ParsedItemData val = ItemRegistry.GetData(value) ?? ItemRegistry.GetData("(O)" + value);
		string text = val?.DisplayName ?? ((object)ModEntry.I18n.Get("lookup.building.fish")).ToString();
		LookupSubject lookupSubject = new LookupSubject
		{
			Title = (ModEntry.I18n.Get("lookup.building.fish-pond-title", (object)new
			{
				fish = text
			})),
			Subtitle = ((object)ModEntry.I18n.Get("lookup.type.building")).ToString()
		};
		if (val != null)
		{
			try
			{
				lookupSubject.MainIcon = val.GetTexture();
				lookupSubject.MainIconSourceRect = val.GetSourceRect(0, null);
			}
			catch
			{
			}
		}
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.status")));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.population")), $"{pond.FishCount} / {((NetFieldBase<int, NetInt>)(object)((Building)pond).maxOccupants).Value}", (Color?)new Color(20, 110, 220)));
		int value2 = ((NetFieldBase<int, NetInt>)(object)((Building)pond).maxOccupants).Value;
		if (pond.FishCount < value2)
		{
			// How often a new fish spawns: game data defines it per species ("SpawnTime");
			// fall back to 3 days when the data lookup fails.
			int num = 3;
			try
			{
				FishPondData fishPondData = pond.GetFishPondData();
				if (fishPondData != null && fishPondData.SpawnTime > 0)
				{
					num = fishPondData.SpawnTime;
				}
			}
			catch
			{
			}
			// Countdown = spawn interval minus days already waited; 0 means "arrives tomorrow".
			int num2 = Math.Max(0, num - ((NetFieldBase<int, NetInt>)(object)pond.daysSinceSpawn).Value);
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish-pond.spawn-countdown")), (num2 == 0) ? ((object)ModEntry.I18n.Get("hover.fishpond.spawning-tomorrow")).ToString() : ((object)ModEntry.I18n.Get("lookup.fish-pond.spawn-days-format", (object)new
			{
				days = num2
			})).ToString(), (Color?)((num2 == 0) ? new Color(0, 140, 0) : new Color(180, 100, 0))));
		}
		else
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish-pond.spawn-countdown")), ((object)ModEntry.I18n.Get("lookup.fish-pond.max-capacity")).ToString(), Color.DarkSlateGray));
		}
		if (((NetFieldBase<bool, NetBool>)(object)pond.hasSpawnedFish).Value)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.next-spawn")), (ModEntry.I18n.Get("lookup.common.ready")), (Color?)new Color(0, 140, 0)));
		}
		if (((NetFieldBase<Item, NetRef<Item>>)(object)pond.neededItem).Value != null)
		{
			ParsedItemData data = ItemRegistry.GetData(((NetFieldBase<Item, NetRef<Item>>)(object)pond.neededItem).Value.QualifiedItemId);
			LookupLink item = new LookupLink($"{((NetFieldBase<int, NetIntDelta>)(object)pond.neededItemCount).Value}x {((NetFieldBase<Item, NetRef<Item>>)(object)pond.neededItem).Value.DisplayName}", (string?)null, (Color?)new Color(200, 60, 20), (data != null) ? data.GetTexture() : null, (data != null) ? new Rectangle?(data.GetSourceRect(0, null)) : (null), (Func<LookupSubject?>?)(() => BuildItemSubject(((NetFieldBase<Item, NetRef<Item>>)(object)pond.neededItem).Value)));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.building.quest-item")), new List<LookupLink> { item }));
		}
		lookupSubject.Sections.Add(lookupSection);
		LookupSection lookupSection2 = new LookupSection((ModEntry.I18n.Get("lookup.section.fish-pond-drops")));
		// Base sell price of the fish: prefer the raw data-table price, otherwise create a
		// temporary Item and ask the game itself (sellToStorePrice), defaulting to 50g.
		int num3 = 0;
		if (val != null && int.TryParse(val.RawData?.ToString(), out var result))
		{
			num3 = result;
		}
		else
		{
			Item val2 = ItemRegistry.Create(val?.QualifiedItemId ?? value, 1, 0, false);
			num3 = ((val2 != null) ? val2.sellToStorePrice(-1L) : 50);
		}
		// Roe price formula: 30g + half the fish's price; "aged" roe (Preserves Jar) doubles it.
		int num4 = 30 + num3 / 2;
		int aged = num4 * 2;
		// Special case: Sturgeon roe processed in a Preserves Jar becomes caviar instead.
		if (text.Contains("Sturgeon") || value == "698" || value == "(O)698")
		{
			lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish-pond.caviar-label")), ((object)ModEntry.I18n.Get("lookup.fish-pond.caviar-desc")).ToString(), (Color?)new Color(180, 50, 180)));
		}
		else
		{
			lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish-pond.roe-value")), ((object)ModEntry.I18n.Get("lookup.fish-pond.roe-format", (object)new
			{
				roe = num4,
				aged = aged
			})).ToString(), (Color?)new Color(180, 100, 0)));
		}
		try
		{
			FishPondData fishPondData2 = pond.GetFishPondData();
			if (fishPondData2 != null && fishPondData2.ProducedItems != null && fishPondData2.ProducedItems.Count > 0)
			{
				List<LookupLink> list = new List<LookupLink>();
				// Each possible drop has: drop chance, min-max stack range, and a population
				// gate (grey until the pond holds that many fish).
				foreach (FishPondReward producedItem in fishPondData2.ProducedItems)
				{
					ParsedItemData rData = ItemRegistry.GetData(((GenericSpawnItemData)producedItem).ItemId);
					if (rData != null)
					{
						string chance = ((producedItem.Chance >= 1f) ? "100%" : $"{producedItem.Chance * 100f:0.#}%");
						string count = ((((GenericSpawnItemData)producedItem).MinStack != ((GenericSpawnItemData)producedItem).MaxStack) ? $"{((GenericSpawnItemData)producedItem).MinStack}-{((GenericSpawnItemData)producedItem).MaxStack}x " : ((((GenericSpawnItemData)producedItem).MinStack > 1) ? $"{((GenericSpawnItemData)producedItem).MinStack}x " : ""));
						string text2 = ((object)ModEntry.I18n.Get("lookup.fish-pond.produce-label", (object)new
						{
							count = count,
							item = rData.DisplayName,
							chance = chance,
							pop = producedItem.RequiredPopulation
						})).ToString();
						list.Add(new LookupLink(text2, null, (Color)((producedItem.RequiredPopulation <= pond.FishCount) ? new Color(0, 140, 0) : Color.DarkSlateGray), rData.GetTexture(), rData.GetSourceRect(0, null), delegate
						{
							Item val3 = ItemRegistry.Create(rData.QualifiedItemId, 1, 0, false);
							return (val3 != null) ? BuildItemSubject(val3) : null;
						}));
					}
				}
				if (list.Count > 0)
				{
					lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fish-pond.possible-produce")), list));
				}
			}
		}
		catch
		{
		}
		lookupSubject.Sections.Add(lookupSection2);
		return lookupSubject;
	}

	/// <summary>Thin wrapper that turns a map tile into a card via BuildWorldOverviewSubject.</summary>
	public static LookupSubject BuildTileSubject(GameLocation location, Vector2 tilePos)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		return BuildWorldOverviewSubject(location, tilePos);
	}

	/// <summary>
	/// Assembles the BIG almanac card from many section builders: daily highlights, calendar,
	/// luck, weather, events, farm chores, CC progress, friendship, perfection, museum,
	/// mines, seasonal crops/forage, skills, island, and optional tile details. Sections only
	/// appear when they have content AND their config toggle allows them.
	/// </summary>
	public static LookupSubject BuildWorldOverviewSubject(GameLocation? location = null, Vector2? tilePos = null)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Unknown result type (might be due to invalid IL or missing references)
		string text = ((object)ModEntry.I18n.Get("lookup.world.farm")).ToString();
		string text2 = ((location != null) ? (location.DisplayName ?? location.Name) : (((NetFieldBase<string, NetString>)(object)Game1.player.farmName).Value + " " + text));
		string text3 = ((object)ModEntry.I18n.Get("lookup.world.daily-almanac")).ToString();
		string title = (tilePos.HasValue ? $"{text2} ({tilePos.Value.X}, {tilePos.Value.Y})" : (text2 + " - " + text3));
		string timeOfDayString = Game1.getTimeOfDayString(Game1.timeOfDay);
		int days = 28 - Game1.dayOfMonth;
		string value = ((object)ModEntry.I18n.Get("lookup.world.days-left", (object)new { days })).ToString();
		LookupSubject lookupSubject = new LookupSubject
		{
			Title = title,
			Subtitle = $"{GetFullDateString()} — {timeOfDayString} ({value})"
		};
		lookupSubject.Sections.Add(BuildDailyOverviewSummarySection());
		lookupSubject.Sections.Add(BuildCalendarSection());
		lookupSubject.Sections.Add(BuildDailyLuckSection());
		lookupSubject.Sections.Add(BuildWeatherSection(location));
		LookupSection lookupSection = BuildSpecialEventsSection();
		if (lookupSection.Fields.Count > 0)
		{
			lookupSubject.Sections.Add(lookupSection);
		}
		lookupSubject.Sections.Add(BuildFarmSummarySection());
		if (ModEntry.Config.ShowCommunityCenterProgress)
		{
			LookupSection lookupSection2 = BuildCommunityCenterSection();
			if (lookupSection2.Fields.Count > 0)
			{
				lookupSubject.Sections.Add(lookupSection2);
			}
		}
		if (ModEntry.Config.ShowFriendshipOverview)
		{
			LookupSection lookupSection3 = BuildFriendshipOverviewSection();
			if (lookupSection3.Fields.Count > 0)
			{
				lookupSubject.Sections.Add(lookupSection3);
			}
		}
		if (ModEntry.Config.ShowProgressAndPerfection)
		{
			LookupSection lookupSection4 = BuildProgressAndPerfectionSection();
			if (lookupSection4.Fields.Count > 0)
			{
				lookupSubject.Sections.Add(lookupSection4);
			}
		}
		if (ModEntry.Config.ShowMuseumProgress)
		{
			LookupSection lookupSection5 = BuildMuseumProgressSection();
			if (lookupSection5.Fields.Count > 0)
			{
				lookupSubject.Sections.Add(lookupSection5);
			}
		}
		if (ModEntry.Config.ShowMineAndGuildProgress)
		{
			LookupSection lookupSection6 = BuildMineAndGuildProgressSection();
			if (lookupSection6.Fields.Count > 0)
			{
				lookupSubject.Sections.Add(lookupSection6);
			}
		}
		lookupSubject.Sections.Add(BuildSeasonalCropsSection());
		lookupSubject.Sections.Add(BuildSeasonalForageSection());
		lookupSubject.Sections.Add(BuildSkillsAndMasterySection());
		LookupSection lookupSection7 = BuildIslandProgressSection();
		if (lookupSection7 != null && lookupSection7.Fields.Count > 0)
		{
			lookupSubject.Sections.Add(lookupSection7);
		}
		if (location != null && tilePos.HasValue)
		{
			lookupSubject.Sections.Add(BuildTileDetailsSection(location, tilePos.Value));
		}
		return lookupSubject;
	}

	/// <summary>Daily headline rows: date, luck/weather keywords, and quick chore tallies.</summary>
	private static LookupSection BuildDailyOverviewSummarySection()
	{
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_064d: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ca: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.almanac-highlights")));
		try
		{
			string text = ((!Game1.isRaining) ? (Game1.isGreenRain ? "green-rain" : (Game1.isDebrisWeather ? "debris" : "sunny")) : (Game1.isLightning ? "stormy" : (Game1.isSnowing ? "snowy" : "rainy")));
			string weather = ((object)ModEntry.I18n.Get("lookup.weather." + text)).ToString();
			double dailyLuck = Game1.player.DailyLuck;
			if (1 == 0)
			{
			}
			string text2 = ((dailyLuck > 0.07) ? "very-lucky" : ((dailyLuck > 0.02) ? "good-luck" : ((dailyLuck >= -0.02) ? "neutral" : ((!(dailyLuck >= -0.07)) ? "very-bad-luck" : "bad-luck"))));
			if (1 == 0)
			{
			}
			string text3 = text2;
			string luck = ((object)ModEntry.I18n.Get("lookup.luck." + text3)).ToString();
			string value = ((object)ModEntry.I18n.Get("lookup.world.outlook-format", (object)new { weather, luck })).ToString();
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.world.outlook")), value, (Color)((dailyLuck >= 0.02) ? new Color(0, 140, 0) : ((dailyLuck <= -0.02) ? new Color(200, 60, 20) : Color.DarkSlateGray))));
			Farm farm = Game1.getFarm();
			if (farm != null)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				if (farm.terrainFeatures != null)
				{
					foreach (KeyValuePair<Vector2, TerrainFeature> pair in farm.terrainFeatures.Pairs)
					{
						TerrainFeature value2 = pair.Value;
						HoeDirt val = value2 as HoeDirt;
						if (val != null && val.crop != null && !val.crop.dead.Value)
						{
							if (val.readyForHarvest())
							{
								num2++;
							}
							else if (val.needsWatering() && val.state.Value != 1)
							{
								num++;
							}
						}
					}
				}
				foreach (FarmAnimal allFarmAnimal in ((GameLocation)farm).getAllFarmAnimals())
				{
					if (!((NetFieldBase<bool, NetBool>)(object)allFarmAnimal.wasPet).Value)
					{
						num3++;
					}
					if (((NetFieldBase<string, NetString>)(object)allFarmAnimal.currentProduce).Value != null)
					{
						num4++;
					}
				}
				foreach (SObject value4 in ((GameLocation)farm).objects.Values)
				{
					if (((NetFieldBase<SObject, NetRef<SObject>>)(object)value4.heldObject).Value != null && (value4.MinutesUntilReady <= 0 || ((NetFieldBase<bool, NetBool>)(object)value4.readyForHarvest).Value))
					{
						num5++;
					}
				}
				string value3 = ((num2 > 0 || num > 0 || num3 > 0 || num4 > 0 || num5 > 0) ? ((object)ModEntry.I18n.Get("lookup.world.chores-summary", (object)new
				{
					readyCrops = num2,
					unwatered = num,
					unpet = num3,
					readyProduce = num4,
					readyMach = num5
				})).ToString() : ((object)ModEntry.I18n.Get("lookup.world.chores-all-done")).ToString());
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.section.farm-chores")), value3, (Color?)((num2 > 0 || num > 0 || num3 > 0 || num5 > 0) ? new Color(180, 100, 0) : new Color(0, 140, 0))));
			}
			List<NPC> list = (from c in Utility.getAllCharacters()
				where c != null && ((Character)c).IsVillager && string.Equals(c.Birthday_Season, Game1.currentSeason, StringComparison.OrdinalIgnoreCase) && c.Birthday_Day == Game1.dayOfMonth
				select c).ToList();
			string item = ((list.Count > 0) ? string.Join(", ", list.Select((NPC n) => ((object)ModEntry.I18n.Get("lookup.calendar.npc-birthday", (object)new
			{
				name = (((Character)n).displayName ?? ((Character)n).Name)
			})).ToString())) : ((object)ModEntry.I18n.Get("lookup.calendar.no-birthdays")).ToString());
			bool flag = Game1.getLocationFromName("Town")?.characters?.Any(c => c.Name.Equals("Bookseller", StringComparison.OrdinalIgnoreCase)) ?? false;
			// Weekday index: seasons are 28 days = exactly 4 weeks, so (day-1) % 7 gives
			// 0=Monday..6=Sunday. Cart visits Friday (4), Sunday (6), and winter fair days.
			int dayOfMonth = Game1.dayOfMonth;
			int num6 = (dayOfMonth - 1) % 7;
			bool flag2 = num6 == 4 || num6 == 6 || (Game1.currentSeason == "winter" && dayOfMonth >= 15 && dayOfMonth <= 17);
			List<string> list2 = new List<string>();
			if (list.Count > 0)
			{
				list2.Add(item);
			}
			if (flag)
			{
				list2.Add(((object)ModEntry.I18n.Get("lookup.events.bookseller-in-town")).ToString());
			}
			if (flag2)
			{
				list2.Add(((object)ModEntry.I18n.Get("lookup.events.cart-in-forest")).ToString());
			}
			string festivalName = GetFestivalName(Game1.currentSeason, Game1.dayOfMonth);
			if (!string.IsNullOrEmpty(festivalName))
			{
				list2.Add(festivalName);
			}
			if (list2.Count > 0)
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.world.events-today")), string.Join(" | ", list2), (Color?)new Color(180, 50, 180)));
			}
			if (((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("JojaMember"))
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.world.progress-label")), ((object)ModEntry.I18n.Get("lookup.world.joja-active")).ToString(), (Color?)new Color(20, 110, 220)));
			}
			else if (Game1.player.hasCompletedCommunityCenter())
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.world.progress-label")), ((object)ModEntry.I18n.Get("lookup.world.cc-restored")).ToString(), (Color?)new Color(0, 140, 0)));
			}
			else
			{
				// Each completed CC room sets one "cc..." mail flag on the host player,
				// so counting flags = counting finished rooms.
				int num7 = 0;
				if (((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccBoilerRoom"))
				{
					num7++;
				}
				if (((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccCraftsRoom"))
				{
					num7++;
				}
				if (((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccPantry"))
				{
					num7++;
				}
				if (((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccVault"))
				{
					num7++;
				}
				if (((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccFishTank"))
				{
					num7++;
				}
				if (((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccBulletin"))
				{
					num7++;
				}
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.world.progress-label")), ((object)ModEntry.I18n.Get("lookup.world.cc-rooms-done", (object)new
				{
					count = num7
				})).ToString(), (Color?)new Color(180, 100, 0)));
			}
		}
		catch
		{
		}
		return lookupSection;
	}

	/// <summary>Formats today's date with weekday, season, day and year in one localized string.</summary>
	private static string GetFullDateString()
	{
		// (day - 1) % 7 turns the 1-based day number into a weekday index for the switch below.
		int dayOfMonth = Game1.dayOfMonth;
		int num = (dayOfMonth - 1) % 7;
		if (1 == 0)
		{
		}
		string text = num switch
		{
			0 => "monday", 
			1 => "tuesday", 
			2 => "wednesday", 
			3 => "thursday", 
			4 => "friday", 
			5 => "saturday", 
			6 => "sunday", 
			_ => "monday", 
		};
		if (1 == 0)
		{
		}
		string text2 = text;
		string dayOfWeek = ((object)ModEntry.I18n.Get("day." + text2)).ToString();
		string text3 = "season." + Game1.currentSeason.ToLower();
		Translation val = ModEntry.I18n.Get(text3);
		string season = (val.HasValue() ? ((object)val).ToString() : (char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1)));
		return ((object)ModEntry.I18n.Get("lookup.world.full-date", (object)new
		{
			dayOfWeek = dayOfWeek,
			season = season,
			day = dayOfMonth,
			year = Game1.year
		})).ToString();
	}

	/// <summary>28-day calendar: birthdays, festivals, Queen of Sauce episodes, and cart days.</summary>
	private static LookupSection BuildCalendarSection()
	{
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_033f: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.calendar-events")));
		List<NPC> list = new List<NPC>();
		// (NPC, int) tuples pair each villager with days-until-birthday for easy sorting.
		List<(NPC, int)> list2 = new List<(NPC, int)>();
		foreach (NPC allCharacter in Utility.getAllCharacters())
		{
			if (allCharacter != null && ((Character)allCharacter).IsVillager && !((Character)allCharacter).IsMonster && !string.IsNullOrEmpty(((Character)allCharacter).Name) && string.Equals(allCharacter.Birthday_Season, Game1.currentSeason, StringComparison.OrdinalIgnoreCase))
			{
				if (allCharacter.Birthday_Day == Game1.dayOfMonth)
				{
					list.Add(allCharacter);
				}
				else if (allCharacter.Birthday_Day > Game1.dayOfMonth && allCharacter.Birthday_Day <= Game1.dayOfMonth + 7)
				{
					list2.Add((allCharacter, allCharacter.Birthday_Day - Game1.dayOfMonth));
				}
			}
		}
		if (list.Count > 0)
		{
			List<LookupLink> list3 = new List<LookupLink>();
			foreach (NPC item3 in list)
			{
				NPC target = item3;
				list3.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.calendar.birthday-today-format", (object)new
				{
					name = (((Character)target).displayName ?? ((Character)target).Name)
				})).ToString(), (string?)null, (Color?)new Color(180, 50, 180), target.Portrait, (Rectangle?)new Rectangle(0, 0, 64, 64), (Func<LookupSubject?>?)(() => BuildNPCSubject(target))));
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.calendar.todays-birthday")), list3));
		}
		else
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.calendar.todays-birthday")), ((object)ModEntry.I18n.Get("lookup.common.none")).ToString(), Color.DarkSlateGray));
		}
		if (list2.Count > 0)
		{
			List<LookupLink> list4 = new List<LookupLink>();
			// OrderBy sorts the tuples by their DaysUntil field so soonest birthdays come first.
			foreach (var item4 in list2.OrderBy<(NPC, int), int>(((NPC Npc, int DaysUntil) u) => u.DaysUntil))
			{
				NPC item = item4.Item1;
				int item2 = item4.Item2;
				NPC target2 = item;
				string text = ((item2 == 1) ? ((object)ModEntry.I18n.Get("lookup.calendar.tomorrow")).ToString() : ((object)ModEntry.I18n.Get("lookup.calendar.in-days-format", (object)new
				{
					days = item2
				})).ToString());
				list4.Add(new LookupLink((((Character)target2).displayName ?? ((Character)target2).Name) + " (" + text + ")", (string?)null, (Color?)Game1.textColor, target2.Portrait, (Rectangle?)new Rectangle(0, 0, 64, 64), (Func<LookupSubject?>?)(() => BuildNPCSubject(target2))));
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.calendar.upcoming-birthdays")), list4));
		}
		string festivalName = GetFestivalName(Game1.currentSeason, Game1.dayOfMonth);
		if (!string.IsNullOrEmpty(festivalName))
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.calendar.festival-today")), festivalName, (Color?)new Color(200, 60, 20)));
		}
		for (int num = Game1.dayOfMonth + 1; num <= Math.Min(28, Game1.dayOfMonth + 7); num++)
		{
			string festivalName2 = GetFestivalName(Game1.currentSeason, num);
			if (!string.IsNullOrEmpty(festivalName2))
			{
				int days = num - Game1.dayOfMonth;
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.calendar.upcoming-festival")), ((object)ModEntry.I18n.Get("lookup.calendar.in-days", (object)new
				{
					festival = festivalName2,
					days = days
				})).ToString(), (Color?)new Color(180, 100, 0)));
				break;
			}
		}
		return lookupSection;
	}

	/// <summary>Returns the festival name for a season/day pair, or null if none.</summary>
	private static string? GetFestivalName(string season, int day)
	{
		switch (season.ToLower())
		{
		case "spring":
			if (day == 13)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-egg")).ToString();
			}
			if (day == 24)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-flower")).ToString();
			}
			if (day >= 15 && day <= 17)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-desert")).ToString();
			}
			break;
		case "summer":
			if (day == 11)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-luau")).ToString();
			}
			if (day == 28)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-jellies")).ToString();
			}
			if (day == 20 || day == 21)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-trout")).ToString();
			}
			break;
		case "fall":
			switch (day)
			{
			case 16:
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-fair")).ToString();
			case 27:
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-spirits-eve")).ToString();
			}
			break;
		case "winter":
			if (day == 8)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-ice")).ToString();
			}
			if (day >= 15 && day <= 17)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-night-market")).ToString();
			}
			if (day == 25)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-winter-star")).ToString();
			}
			if (day == 12 || day == 13)
			{
				return ((object)ModEntry.I18n.Get("lookup.calendar.festival-squid")).ToString();
			}
			break;
		}
		return null;
	}

	/// <summary>Shows DailyLuck with a keyword badge (best/good/normal/bad/worst).</summary>
	private static LookupSection BuildDailyLuckSection()
	{
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.daily-luck")));
		double dailyLuck = Game1.player.DailyLuck;
		string value;
		Color darkSlateGray;
		if (dailyLuck > 0.07)
		{
			value = ModEntry.I18n.Get("lookup.fortune.very-lucky-text").ToString();
			darkSlateGray = new Color(0, 140, 0);
		}
		else if (dailyLuck > 0.02)
		{
			value = ModEntry.I18n.Get("lookup.fortune.lucky-text").ToString();
			darkSlateGray = new Color(46, 125, 50);
		}
		else if (dailyLuck >= -0.02)
		{
			value = ModEntry.I18n.Get("lookup.fortune.neutral-text").ToString();
			darkSlateGray = Color.DarkSlateGray;
		}
		else if (dailyLuck >= -0.07)
		{
			value = ModEntry.I18n.Get("lookup.fortune.unlucky-text").ToString();
			darkSlateGray = new Color(200, 100, 20);
		}
		else
		{
			value = ModEntry.I18n.Get("lookup.fortune.very-unlucky-text").ToString();
			darkSlateGray = new Color(220, 20, 60);
		}
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fortune.spirits-forecast")), value, darkSlateGray));
		string value2 = ((dailyLuck >= 0.0) ? $"+{dailyLuck:F3}" : $"{dailyLuck:F3}");
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.fortune.modifier")), value2, (Color?)((dailyLuck >= 0.0) ? new Color(0, 140, 0) : new Color(200, 60, 20))));
		if (Game1.player.hasSpecialCharm)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.wallet.special-charm")), ((object)ModEntry.I18n.Get("lookup.wallet.special-charm-active")).ToString(), (Color?)new Color(180, 50, 180)));
		}
		return lookupSection;
	}

	/// <summary>Detects tomorrow's forecast from Game1.weatherForTomorrow plus special rain types.</summary>
	private static LookupSection BuildWeatherSection(GameLocation? location)
	{
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.weather-forecast")));
		string value = (Game1.isGreenRain ? ((object)ModEntry.I18n.Get("lookup.weather.green-rain-text")).ToString() : (Game1.isLightning ? ((object)ModEntry.I18n.Get("lookup.weather.lightning-storm")).ToString() : (Game1.isSnowing ? ((object)ModEntry.I18n.Get("lookup.weather.snowing")).ToString() : (Game1.isRaining ? ((object)ModEntry.I18n.Get("lookup.weather.rainy-text")).ToString() : (Game1.isDebrisWeather ? ((object)ModEntry.I18n.Get("lookup.weather.windy-debris")).ToString() : ((object)ModEntry.I18n.Get("lookup.weather.clear")).ToString())))));
		Color value2 = ((Game1.isRaining || Game1.isLightning || Game1.isGreenRain) ? new Color(20, 110, 220) : new Color(180, 100, 0));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weather.today-label")), value, value2));
		string weatherForTomorrow = Game1.weatherForTomorrow;
		if (1 == 0)
		{
		}
		string text = weatherForTomorrow switch
		{
			"Rain" => ((object)ModEntry.I18n.Get("lookup.weather.rainy-text")).ToString(), 
			"Storm" => ((object)ModEntry.I18n.Get("lookup.weather.lightning-storm")).ToString(), 
			"Snow" => ((object)ModEntry.I18n.Get("lookup.weather.snowing")).ToString(), 
			"GreenRain" => ((object)ModEntry.I18n.Get("lookup.weather.green-rain-text")).ToString(), 
			"Wind" => ((object)ModEntry.I18n.Get("lookup.weather.windy-debris")).ToString(), 
			_ => ((object)ModEntry.I18n.Get("lookup.weather.sunny")).ToString(), 
		};
		if (1 == 0)
		{
		}
		string value3 = text;
		Color val;
		switch (weatherForTomorrow)
		{
		default:
			val = new Color(180, 100, 0);
			break;
		case "Rain":
		case "Storm":
		case "GreenRain":
			val = new Color(20, 110, 220);
			break;
		}
		Color value4 = val;
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weather.tomorrow-label")), value3, value4));
		if (((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.IslandVisitors.Count > 0 || Game1.player.hasOrWillReceiveMail("Visited_Island"))
		{
			GameLocation locationFromName = Game1.getLocationFromName("IslandSouth");
			if (locationFromName != null)
			{
				string value5 = (locationFromName.IsRainingHere() ? ((object)ModEntry.I18n.Get("lookup.weather.rainy-text")).ToString() : ((object)ModEntry.I18n.Get("lookup.weather.sunny")).ToString());
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.weather.island-weather")), value5, (Color?)(locationFromName.IsRainingHere() ? new Color(20, 110, 220) : new Color(180, 100, 0))));
			}
		}
		return lookupSection;
	}

	/// <summary>One-off daily highlights: TV recipes, traveling-cart stock, and other events.</summary>
	private static LookupSection BuildSpecialEventsSection()
	{
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_042a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.events-tv-quests")));
		bool flag = Game1.getLocationFromName("Town")?.characters?.Any(c => c.Name.Equals("Bookseller", StringComparison.OrdinalIgnoreCase)) ?? false;
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.events.bookseller")), flag ? ((object)ModEntry.I18n.Get("lookup.events.bookseller-visiting")).ToString() : ((object)ModEntry.I18n.Get("lookup.events.bookseller-not-today")).ToString(), (Color)(flag ? new Color(180, 50, 180) : Color.DarkSlateGray)));
		if (((NetFieldBase<int, NetInt>)(object)Game1.player.daysLeftForToolUpgrade).Value > 0)
		{
			int value = ((NetFieldBase<int, NetInt>)(object)Game1.player.daysLeftForToolUpgrade).Value;
			string ready = ((value == 1) ? ((object)ModEntry.I18n.Get("lookup.events.tool-ready-tomorrow")).ToString() : ((object)ModEntry.I18n.Get("lookup.events.tool-ready-in-days", (object)new
			{
				days = value
			})).ToString());
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.events.tool-upgrade")), ((object)ModEntry.I18n.Get("lookup.events.tool-upgrading", (object)new { ready })).ToString(), (Color?)new Color(180, 100, 0)));
		}
		int count = ((NetList<Quest, NetRef<Quest>>)(object)Game1.player.questLog).Count;
		int count2 = Game1.player.team.specialOrders.Count;
		if (count > 0 || count2 > 0)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.events.active-quests")), ((object)ModEntry.I18n.Get("lookup.events.quests-active-format", (object)new
			{
				billboard = count,
				special = count2
			})).ToString(), (Color?)new Color(20, 110, 220)));
		}
		int dayOfMonth = Game1.dayOfMonth;
		// Weekday again: 6=Sunday airs a NEW recipe, 2=Wednesday reruns an old one.
		int num = (dayOfMonth - 1) % 7;
		switch (num)
		{
		case 6:
		{
			(string, bool)? queenOfSauceRecipe2 = GetQueenOfSauceRecipe(isSunday: true);
			if (queenOfSauceRecipe2.HasValue)
			{
				string text2 = (queenOfSauceRecipe2.Value.Item2 ? ((object)ModEntry.I18n.Get("lookup.tv.recipe-known")).ToString() : ((object)ModEntry.I18n.Get("lookup.tv.recipe-new")).ToString());
				Color value3 = (Color)(queenOfSauceRecipe2.Value.Item2 ? Color.DarkSlateGray : new Color(0, 140, 0));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.tv.qos-sunday")), queenOfSauceRecipe2.Value.Item1 + " - " + text2, value3));
			}
			break;
		}
		case 2:
		{
			(string, bool)? queenOfSauceRecipe = GetQueenOfSauceRecipe(isSunday: false);
			if (queenOfSauceRecipe.HasValue)
			{
				string text = (queenOfSauceRecipe.Value.Item2 ? ((object)ModEntry.I18n.Get("lookup.tv.recipe-known")).ToString() : ((object)ModEntry.I18n.Get("lookup.tv.recipe-new")).ToString());
				Color value2 = (Color)(queenOfSauceRecipe.Value.Item2 ? Color.DarkSlateGray : new Color(0, 140, 0));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.tv.qos-rerun")), queenOfSauceRecipe.Value.Item1 + " - " + text, value2));
			}
			break;
		}
		}
		if (num == 4 || num == 6 || (Game1.currentSeason == "winter" && dayOfMonth >= 15 && dayOfMonth <= 17))
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.events.cart")), ((object)ModEntry.I18n.Get("lookup.events.cart-schedule")).ToString(), (Color?)new Color(180, 50, 180)));
		}
		else
		{
			// Days until Friday: (target - today + 7) % 7 wraps around the week; the
			// result of 0 is bumped to 7 so "today" never claims a visit.
			int num2 = (4 - num + 7) % 7;
			if (num2 == 0)
			{
				num2 = 7;
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.events.cart")), ((object)ModEntry.I18n.Get("lookup.events.cart-next", (object)new
			{
				days = num2,
				dayOfWeek = ModEntry.I18n.Get("day.friday")
			})).ToString(), Color.DarkSlateGray));
		}
		return lookupSection;
	}

	/// <summary>
	/// The Queen of Sauce TV cooking-show lookup: returns the recipe airing on a given
	/// Sunday (new episode) or Wednesday (rerun), or null when nothing airs / data fails.
	/// </summary>
	/// <remarks>
	/// BEGINNER NOTES:
	/// - The game stores every recipe's air date as an absolute day number since year 1;
	///   dividing by 7 groups days into "weeks" so any Sunday/Wednesday can find its show.
	/// - Reruns skip recipes the player already knows, picking the first unknown one instead.
	/// </remarks>
	private static (string RecipeName, bool Known)? GetQueenOfSauceRecipe(bool isSunday)
	{
		try
		{
			Dictionary<string, string> dictionary = DataLoader.Tv_CookingChannel(Game1.content);
			if (dictionary == null)
			{
				return null;
			}
			if (isSunday)
			{
				string currentSeason = Game1.currentSeason;
				if (1 == 0)
				{
				}
				int num = currentSeason switch
				{
					"summer" => 28, 
					"fall" => 56, 
					"winter" => 84, 
					_ => 0, 
				};
				if (1 == 0)
				{
				}
				// Absolute day since year 1: (year-1)*112 days + season offset (0/28/56/84)
				// + day of month; dividing by 7 gives the "week number" used as the TV key.
				int num2 = num;
				if (dictionary.TryGetValue((((Game1.year - 1) * 112 + Game1.dayOfMonth + num2) / 7).ToString(), out var value))
				{
					string[] array = value.Split('/');
					if (array.Length != 0)
					{
						string text = array[0];
						bool item = ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.cookingRecipes).ContainsKey(text);
						return (text, item);
					}
				}
			}
			else
			{
				// Rerun: walk the schedule in order and air the first recipe the
				// player has NOT learned yet (dictionary order = chronological).
				foreach (KeyValuePair<string, string> item2 in dictionary)
				{
					string[] array2 = item2.Value.Split('/');
					if (array2.Length != 0 && !((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.cookingRecipes).ContainsKey(array2[0]))
					{
						return (array2[0], false);
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	/// <summary>Farm-wide chore summary: machines ready, watered crops, petted animals, hay, and more.</summary>
	private static LookupSection BuildFarmSummarySection()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0683: Unknown result type (might be due to invalid IL or missing references)
		//IL_0688: Unknown result type (might be due to invalid IL or missing references)
		//IL_068c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0691: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_056f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0528: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_07a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0790: Unknown result type (might be due to invalid IL or missing references)
		//IL_0907: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0639: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.farm-chores")));
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		try
		{
			Farm farm = Game1.getFarm();
			if (farm != null)
			{
				if (farm.terrainFeatures != null)
				{
					foreach (KeyValuePair<Vector2, TerrainFeature> pair in farm.terrainFeatures.Pairs)
					{
						TerrainFeature value = pair.Value;
						HoeDirt val = value as HoeDirt;
						if (val != null && val.crop != null)
						{
							if (val.crop.dead.Value)
							{
								num3++;
							}
							else if (val.readyForHarvest())
							{
								num2++;
							}
							else if (val.needsWatering() && val.state.Value != 1)
							{
								num++;
							}
						}
					}
				}
				foreach (FarmAnimal allFarmAnimal in ((GameLocation)farm).getAllFarmAnimals())
				{
					if (!((NetFieldBase<bool, NetBool>)(object)allFarmAnimal.wasPet).Value)
					{
						num4++;
					}
					if (((NetFieldBase<string, NetString>)(object)allFarmAnimal.currentProduce).Value != null)
					{
						num5++;
					}
				}
				foreach (SObject value5 in ((GameLocation)farm).objects.Values)
				{
					if (((NetFieldBase<bool, NetBool>)(object)value5.readyForHarvest).Value)
					{
						num6++;
					}
				}
				// Machines ready for harvest exist in TWO places - loose on the farm
				// (scanned above) and inside building interiors like barns/coops,
				// so every building's "indoors" gets its own pass here.
				foreach (Building building in ((GameLocation)farm).buildings)
				{
					if (((NetFieldBase<GameLocation, NetRef<GameLocation>>)(object)building.indoors).Value == null)
					{
						continue;
					}
					foreach (SObject value6 in ((NetFieldBase<GameLocation, NetRef<GameLocation>>)(object)building.indoors).Value.objects.Values)
					{
						if (((NetFieldBase<bool, NetBool>)(object)value6.readyForHarvest).Value)
						{
							num6++;
						}
					}
				}
			}
		}
		catch
		{
		}
		if (num == 0 && num2 == 0 && num3 == 0)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.crops")), ((object)ModEntry.I18n.Get("lookup.chores.crops-done")).ToString(), (Color?)new Color(0, 140, 0)));
		}
		else
		{
			List<string> list = new List<string>();
			if (num > 0)
			{
				list.Add(((object)ModEntry.I18n.Get("lookup.chores.crop-unwatered-part", (object)new
				{
					count = num
				})).ToString());
			}
			if (num2 > 0)
			{
				list.Add(((object)ModEntry.I18n.Get("lookup.chores.crop-ready-part", (object)new
				{
					count = num2
				})).ToString());
			}
			if (num3 > 0)
			{
				list.Add(((object)ModEntry.I18n.Get("lookup.chores.crop-dead-part", (object)new
				{
					count = num3
				})).ToString());
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.crops")), string.Join(", ", list), (Color?)((num > 0) ? new Color(200, 60, 20) : new Color(0, 140, 0))));
		}
		if (num4 == 0 && num5 == 0)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.animals")), ((object)ModEntry.I18n.Get("lookup.chores.animals-done")).ToString(), (Color?)new Color(0, 140, 0)));
		}
		else
		{
			List<string> list2 = new List<string>();
			if (num4 > 0)
			{
				list2.Add(((object)ModEntry.I18n.Get("lookup.chores.animal-unpetted-part", (object)new
				{
					count = num4
				})).ToString());
			}
			if (num5 > 0)
			{
				list2.Add(((object)ModEntry.I18n.Get("lookup.chores.animal-produce-part", (object)new
				{
					count = num5
				})).ToString());
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.animals")), string.Join(", ", list2), (Color?)((num4 > 0) ? new Color(200, 60, 20) : new Color(0, 140, 0))));
		}
		if (num6 > 0)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.machines")), ((object)ModEntry.I18n.Get("lookup.chores.machines-ready-format", (object)new
			{
				count = num6
			})).ToString(), (Color?)new Color(0, 140, 0)));
		}
		else
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.machines")), ((object)ModEntry.I18n.Get("lookup.chores.no-machines")).ToString(), Color.DarkSlateGray));
		}
		try
		{
			Farm farm2 = Game1.getFarm();
			if (farm2 != null)
			{
				int value2 = ((NetFieldBase<int, NetInt>)(object)((GameLocation)farm2).piecesOfHay).Value;
				// Hay stored across all silos: 240 capacity each.
				int num7 = ((IEnumerable<Building>)((GameLocation)farm2).buildings).Count((Building b) => ((NetFieldBase<string, NetString>)(object)b.buildingType).Value.Contains("Silo")) * 240;
				if (num7 > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.silo-hay")), ((object)ModEntry.I18n.Get("lookup.chores.silo-hay-format", (object)new
					{
						current = value2,
						max = num7
					})).ToString(), (Color)((value2 < num7 / 4) ? new Color(200, 60, 20) : Game1.textColor)));
				}
			}
		}
		catch
		{
		}
		try
		{
			// Greenhouse and island farm are separate GameLocations, so the same
			// crop scan runs again there (each wrapped in its own try for safety).
			GameLocation locationFromName = Game1.getLocationFromName("Greenhouse");
			if (locationFromName != null)
			{
				int num8 = 0;
				int num9 = 0;
				if (locationFromName.terrainFeatures != null)
				{
					foreach (KeyValuePair<Vector2, TerrainFeature> pair in locationFromName.terrainFeatures.Pairs)
					{
						TerrainFeature value3 = pair.Value;
						HoeDirt val2 = value3 as HoeDirt;
						if (val2 != null && val2.crop != null && !val2.crop.dead.Value)
						{
							if (val2.readyForHarvest())
							{
								num9++;
							}
							else if (val2.needsWatering() && val2.state.Value != 1)
							{
								num8++;
							}
						}
					}
				}
				if (num8 > 0 || num9 > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.greenhouse")), ((object)ModEntry.I18n.Get("lookup.chores.greenhouse-format", (object)new
					{
						unwatered = num8,
						ready = num9
					})).ToString(), (Color?)((num8 > 0) ? new Color(200, 60, 20) : new Color(0, 140, 0))));
				}
			}
		}
		catch
		{
		}
		try
		{
			GameLocation locationFromName2 = Game1.getLocationFromName("IslandWest");
			if (locationFromName2 != null)
			{
				int num10 = 0;
				int num11 = 0;
				if (locationFromName2.terrainFeatures != null)
				{
					foreach (KeyValuePair<Vector2, TerrainFeature> pair in locationFromName2.terrainFeatures.Pairs)
					{
						TerrainFeature value4 = pair.Value;
						HoeDirt val3 = value4 as HoeDirt;
						if (val3 != null && val3.crop != null && !val3.crop.dead.Value)
						{
							if (val3.readyForHarvest())
							{
								num11++;
							}
							else if (val3.needsWatering() && val3.state.Value != 1)
							{
								num10++;
							}
						}
					}
				}
				if (num10 > 0 || num11 > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.island-farm")), ((object)ModEntry.I18n.Get("lookup.chores.island-farm-format", (object)new
					{
						unwatered = num10,
						ready = num11
					})).ToString(), (Color?)((num10 > 0) ? new Color(200, 60, 20) : new Color(0, 140, 0))));
				}
			}
		}
		catch
		{
		}
		return lookupSection;
	}

	/// <summary>Extra rows for a clicked map tile: building on it, crop, terrain feature, machines.</summary>
	private static LookupSection BuildTileDetailsSection(GameLocation location, Vector2 tilePos)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.tile-details")));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.tile.location")), location.DisplayName ?? location.Name, (Color?)new Color(20, 110, 220)));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.tile.position")), $"X: {tilePos.X}, Y: {tilePos.Y}", Game1.textColor));
		bool flag = location.isWaterTile((int)tilePos.X, (int)tilePos.Y);
		bool flag2 = location.isTilePassable(new xTile.Dimensions.Location((int)tilePos.X, (int)tilePos.Y), Game1.viewport);
		string value = (flag ? ((object)ModEntry.I18n.Get("lookup.tile.water")).ToString() : (flag2 ? ((object)ModEntry.I18n.Get("lookup.tile.walkable")).ToString() : ((object)ModEntry.I18n.Get("lookup.tile.obstacle")).ToString()));
		lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.tile.type")), value, (Color?)(flag ? new Color(20, 110, 220) : (flag2 ? new Color(0, 140, 0) : new Color(200, 60, 20)))));
		return lookupSection;
	}

	/// <summary>Crops planted this season: growth progress, watered state, regrow behavior.</summary>
	private static LookupSection BuildSeasonalCropsSection()
	{
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		string text = "season." + Game1.currentSeason.ToLower();
		Translation val = ModEntry.I18n.Get(text);
		string season = (val.HasValue() ? ((object)val).ToString() : (char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1)));
		LookupSection lookupSection = new LookupSection(((object)ModEntry.I18n.Get("lookup.seasonal.crops-title", (object)new { season })).ToString());
		List<LookupLink> list = new List<LookupLink>();
		try
		{
			Dictionary<string, CropData> dictionary = DataLoader.Crops(Game1.content);
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, CropData> item2 in dictionary)
				{
					CropData value = item2.Value;
					if (value.Seasons == null || !value.Seasons.Any((Season s) => s.ToString().Equals(Game1.currentSeason, StringComparison.OrdinalIgnoreCase)))
					{
						continue;
					}
					ParsedItemData harvestItem = ItemRegistry.GetData(value.HarvestItemId);
					if (harvestItem != null && !list.Any((LookupLink l) => l.Text.StartsWith(harvestItem.DisplayName)))
					{
						// Growth time = the sum of every phase length in DaysInPhase;
						// RegrowDays > 0 means the crop keeps producing after harvest.
						int days = value.DaysInPhase?.Sum() ?? 0;
						string text2 = ((value.RegrowDays > 0) ? ((object)ModEntry.I18n.Get("lookup.seasonal.crop-info-regrow", (object)new
						{
							name = harvestItem.DisplayName,
							days = days,
							regrow = value.RegrowDays
						})).ToString() : ((object)ModEntry.I18n.Get("lookup.seasonal.crop-info-single", (object)new
						{
							name = harvestItem.DisplayName,
							days = days
						})).ToString());
						Item item = ItemRegistry.Create(harvestItem.QualifiedItemId, 1, 0, false);
						list.Add(new LookupLink(text2, null, (Color)((value.RegrowDays > 0) ? new Color(0, 140, 0) : Game1.textColor), harvestItem.GetTexture(), harvestItem.GetSourceRect(0, null), () => (item != null) ? BuildItemSubject(item) : null));
					}
				}
			}
		}
		catch
		{
		}
		if (list.Count > 0)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.crop.plantable")), list));
		}
		else
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.crop.plantable")), ((object)ModEntry.I18n.Get("lookup.crop.winter-none")).ToString(), Color.DarkSlateGray));
		}
		return lookupSection;
	}

	/// <summary>Forageable items per season with clickable links (hardcoded id lists per season).</summary>
	private static LookupSection BuildSeasonalForageSection()
	{
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		string text = "season." + Game1.currentSeason.ToLower();
		Translation val = ModEntry.I18n.Get(text);
		string season = (val.HasValue() ? ((object)val).ToString() : (char.ToUpper(Game1.currentSeason[0]) + Game1.currentSeason.Substring(1)));
		LookupSection lookupSection = new LookupSection(((object)ModEntry.I18n.Get("lookup.seasonal.forage-title", (object)new { season })).ToString());
		List<LookupLink> list = new List<LookupLink>();
		string text2 = Game1.currentSeason.ToLower();
		if (1 == 0)
		{
		}
		// Forageables aren't tagged in data, so each season gets a hardcoded list of
		// qualified item ids (the "(O)" prefix marks "Object" category).
		string[] array = text2 switch
		{
			"spring" => new string[7] { "(O)16", "(O)18", "(O)20", "(O)22", "(O)399", "(O)257", "(O)296" }, 
			"summer" => new string[6] { "(O)396", "(O)398", "(O)394", "(O)259", "(O)402", "(O)393" }, 
			"fall" => new string[6] { "(O)404", "(O)406", "(O)408", "(O)410", "(O)281", "(O)420" }, 
			"winter" => new string[5] { "(O)412", "(O)414", "(O)416", "(O)418", "(O)283" }, 
			_ => Array.Empty<string>(), 
		};
		if (1 == 0)
		{
		}
		string[] array2 = array;
		string[] array3 = array2;
		foreach (string text3 in array3)
		{
			ParsedItemData data = ItemRegistry.GetData(text3);
			if (data != null && !list.Any((LookupLink l) => l.Text == data.DisplayName))
			{
				Item item = ItemRegistry.Create(data.QualifiedItemId, 1, 0, false);
				list.Add(new LookupLink(data.DisplayName, null, Game1.textColor, data.GetTexture(), data.GetSourceRect(0, null), () => (item != null) ? BuildItemSubject(item) : null));
			}
		}
		// Beach forage (clams, cockles, mussels, coral...) shows in every season.
		List<LookupLink> list2 = new List<LookupLink>();
		string[] array4 = new string[4] { "(O)372", "(O)393", "(O)397", "(O)152" };
		foreach (string text4 in array4)
		{
			ParsedItemData data2 = ItemRegistry.GetData(text4);
			if (data2 != null)
			{
				Item item2 = ItemRegistry.Create(data2.QualifiedItemId, 1, 0, false);
				list2.Add(new LookupLink(data2.DisplayName, (string?)null, (Color?)new Color(20, 110, 220), data2.GetTexture(), (Rectangle?)data2.GetSourceRect(0, null), (Func<LookupSubject?>?)(() => (item2 != null) ? BuildItemSubject(item2) : null)));
			}
		}
		if (list.Count > 0)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.forage.valley")), list));
		}
		if (list2.Count > 0)
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.forage.beach")), list2));
		}
		return lookupSection;
	}

	/// <summary>Five skill levels, mastery progress (sum of levels >= 50 unlocks it), and stat rows.</summary>
	private static LookupSection BuildSkillsAndMasterySection()
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.skills-mastery")));
		int farmingLevel = Game1.player.FarmingLevel;
		int miningLevel = Game1.player.MiningLevel;
		int foragingLevel = Game1.player.ForagingLevel;
		int fishingLevel = Game1.player.FishingLevel;
		int combatLevel = Game1.player.CombatLevel;

		int[] expPerLevel = SkillsPagePatch.ExpPointsPerLevel;

		AddSkillProgressField(lookupSection, Farmer.farmingSkill, farmingLevel, ModEntry.I18n.Get("lookup.skills.farming").ToString(), expPerLevel);
		AddSkillProgressField(lookupSection, Farmer.miningSkill, miningLevel, ModEntry.I18n.Get("lookup.skills.mining").ToString(), expPerLevel);
		AddSkillProgressField(lookupSection, Farmer.foragingSkill, foragingLevel, ModEntry.I18n.Get("lookup.skills.foraging").ToString(), expPerLevel);
		AddSkillProgressField(lookupSection, Farmer.fishingSkill, fishingLevel, ModEntry.I18n.Get("lookup.skills.fishing").ToString(), expPerLevel);
		AddSkillProgressField(lookupSection, Farmer.combatSkill, combatLevel, ModEntry.I18n.Get("lookup.skills.combat").ToString(), expPerLevel);

		try
		{
			// Mastery unlocks only when the five skills total 50 levels (10 each);
			// before that, the whole mastery panel stays hidden.
			int num = farmingLevel + miningLevel + foragingLevel + fishingLevel + combatLevel;
			if (num >= 50)
			{
				int value = (int)Game1.stats.Get("MasteryExp");
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mastery.progress")), ((object)ModEntry.I18n.Get("lookup.mastery.exp-format", (object)new
				{
					exp = $"{value:N0}"
				})).ToString(), (Color?)new Color(180, 50, 180)));
				// Each mastery perk claims a "Mastery_0..4" stat flag (0 = not claimed).
				bool flag = Game1.player.stats.Get("Mastery_0") != 0;
				bool flag2 = Game1.player.stats.Get("Mastery_1") != 0;
				bool flag3 = Game1.player.stats.Get("Mastery_2") != 0;
				bool flag4 = Game1.player.stats.Get("Mastery_3") != 0;
				bool flag5 = Game1.player.stats.Get("Mastery_4") != 0;
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mastery.combat")), flag ? ((object)ModEntry.I18n.Get("lookup.mastery.claimed-combat")).ToString() : ((object)ModEntry.I18n.Get("lookup.mastery.locked")).ToString(), (Color)(flag ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mastery.foraging")), flag2 ? ((object)ModEntry.I18n.Get("lookup.mastery.claimed-foraging")).ToString() : ((object)ModEntry.I18n.Get("lookup.mastery.locked")).ToString(), (Color)(flag2 ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mastery.farming")), flag3 ? ((object)ModEntry.I18n.Get("lookup.mastery.claimed-farming")).ToString() : ((object)ModEntry.I18n.Get("lookup.mastery.locked")).ToString(), (Color)(flag3 ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mastery.fishing")), flag4 ? ((object)ModEntry.I18n.Get("lookup.mastery.claimed-fishing")).ToString() : ((object)ModEntry.I18n.Get("lookup.mastery.locked")).ToString(), (Color)(flag4 ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.mastery.mining")), flag5 ? ((object)ModEntry.I18n.Get("lookup.mastery.claimed-mining")).ToString() : ((object)ModEntry.I18n.Get("lookup.mastery.locked")).ToString(), (Color)(flag5 ? new Color(0, 140, 0) : Color.DarkSlateGray)));
			}
		}
		catch
		{
		}
		return lookupSection;
	}

	/// <summary>Adds a detailed skill progress line with exact current XP and next level target.</summary>
	private static void AddSkillProgressField(LookupSection section, int skillIndex, int effectiveLevel, string skillLabel, int[] expPerLevel)
	{
		int curXp = Game1.player.experiencePoints.Length > skillIndex ? Game1.player.experiencePoints[skillIndex] : 0;
		int baseLevel = Game1.player.GetUnmodifiedSkillLevel(skillIndex);

		if (baseLevel < 10)
		{
			int nextLevel = baseLevel + 1;
			int nextXp = expPerLevel[Math.Clamp(baseLevel, 0, expPerLevel.Length - 1)];
			int prevXp = baseLevel > 0 ? expPerLevel[baseLevel - 1] : 0;
			int needed = Math.Max(0, nextXp - curXp);
			float pct = Math.Clamp((float)(curXp - prevXp) / Math.Max(1, nextXp - prevXp) * 100f, 0f, 100f);

			string buffStr = effectiveLevel > baseLevel ? $" (+{effectiveLevel - baseLevel})" : "";
			string text = $"{baseLevel}{buffStr} ({curXp:N0} / {nextXp:N0} XP, {needed:N0} to Lvl {nextLevel} [{pct:0.0}%])";
			section.Fields.Add(new LookupField("• " + skillLabel, text, (Color?)new Color(0, 140, 0)));
		}
		else
		{
			string buffStr = effectiveLevel > 10 ? $" (+{effectiveLevel - 10})" : "";
			string text = $"10{buffStr} ({curXp:N0} XP — Max Level ✓)";
			section.Fields.Add(new LookupField("• " + skillLabel, text, (Color?)new Color(180, 50, 180)));
		}
	}

	/// <summary>Ginger Island progress: golden walnuts found (goal 130) and island unlocks.</summary>
	private static LookupSection? BuildIslandProgressSection()
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			// The island section only exists once the player has actually been there
			// (mail flag "Visited_Island" or at least one walnut found).
			if (((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.GoldenWalnutsFound > 0 || Game1.player.hasOrWillReceiveMail("Visited_Island"))
			{
				LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.ginger-island")));
				int goldenWalnutsFound = ((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.GoldenWalnutsFound;
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.island.walnuts")), ((object)ModEntry.I18n.Get("lookup.island.walnuts-format", (object)new
				{
					count = goldenWalnutsFound
				})).ToString(), (Color?)((goldenWalnutsFound >= 130) ? new Color(0, 140, 0) : new Color(180, 100, 0))));
				if (Game1.player.hasOrWillReceiveMail("QiChallengeComplete") || Game1.player.QiGems > 0)
				{
					lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.farmer.qi-gems")), ((object)ModEntry.I18n.Get("lookup.farmer.qi-gems-format", (object)new
					{
						count = Game1.player.QiGems
					})).ToString(), (Color?)new Color(180, 50, 180)));
				}
				return lookupSection;
			}
		}
		catch
		{
		}
		return null;
	}

	/// <summary>Community Center bundles: rooms, per-bundle completion bits, or Joja route costs.</summary>
	private static LookupSection BuildCommunityCenterSection()
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0487: Unknown result type (might be due to invalid IL or missing references)
		//IL_059e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0526: Unknown result type (might be due to invalid IL or missing references)
		//IL_0518: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a29: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ab6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0384: Unknown result type (might be due to invalid IL or missing references)
		//IL_0376: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_095a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0982: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.community-center")));
		try
		{
			// Joja membership replaces the CC route entirely - show membership costs instead.
			if (((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("JojaMember"))
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.section.status")), ((object)ModEntry.I18n.Get("lookup.joja.active-desc")).ToString(), (Color?)new Color(20, 110, 220)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.joja.minecarts")), ((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaBoilerRoom") ? ((object)ModEntry.I18n.Get("lookup.joja.completed")).ToString() : ((object)ModEntry.I18n.Get("lookup.joja.cost-format", (object)new
				{
					cost = "5,000"
				})).ToString(), (Color)(((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaBoilerRoom") ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.joja.bridge-repair")), ((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaCraftsRoom") ? ((object)ModEntry.I18n.Get("lookup.joja.completed")).ToString() : ((object)ModEntry.I18n.Get("lookup.joja.cost-format", (object)new
				{
					cost = "25,000"
				})).ToString(), (Color)(((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaCraftsRoom") ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.chores.greenhouse")), ((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaPantry") ? ((object)ModEntry.I18n.Get("lookup.joja.completed")).ToString() : ((object)ModEntry.I18n.Get("lookup.joja.cost-format", (object)new
				{
					cost = "35,000"
				})).ToString(), (Color)(((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaPantry") ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.joja.bus-repair")), ((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaVault") ? ((object)ModEntry.I18n.Get("lookup.joja.completed")).ToString() : ((object)ModEntry.I18n.Get("lookup.joja.cost-format", (object)new
				{
					cost = "40,000"
				})).ToString(), (Color)(((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaVault") ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.joja.panning")), ((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaFishTank") ? ((object)ModEntry.I18n.Get("lookup.joja.completed")).ToString() : ((object)ModEntry.I18n.Get("lookup.joja.cost-format", (object)new
				{
					cost = "20,000"
				})).ToString(), (Color)(((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("jojaFishTank") ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.joja.movie-theater")), ((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccMovieTheater") ? ((object)ModEntry.I18n.Get("lookup.joja.completed")).ToString() : ((object)ModEntry.I18n.Get("lookup.joja.cost-format", (object)new
				{
					cost = "500,000"
				})).ToString(), (Color)(((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccMovieTheater") ? new Color(0, 140, 0) : Color.DarkSlateGray)));
				return lookupSection;
			}
			if (Game1.player.hasCompletedCommunityCenter())
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.world.cc-status")), ((object)ModEntry.I18n.Get("lookup.cc.restored-all")).ToString(), (Color?)new Color(0, 140, 0)));
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.cc.abandoned-jojamart")), ((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccMovieTheater") ? ((object)ModEntry.I18n.Get("lookup.cc.theater-built")).ToString() : ((object)ModEntry.I18n.Get("lookup.cc.theater-missing")).ToString(), (Color?)(((NetHashSet<string>)(object)Game1.MasterPlayer.mailReceived).Contains("ccMovieTheater") ? new Color(0, 140, 0) : new Color(180, 50, 180))));
				return lookupSection;
			}
		// Bundles data table (definitions) + the live NetBundles (per-slot donation bits).
		Dictionary<string, string> dictionary = DataLoader.Bundles(Game1.content);
		NetBundles bundles = ((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.Bundles;
			if (dictionary == null)
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.section.status")), ((object)ModEntry.I18n.Get("lookup.world.cc-in-progress")).ToString(), (Color?)new Color(0, 140, 0)));
				return lookupSection;
			}
			Dictionary<string, string>? bundleNamesDict = null;
			try
			{
				bundleNamesDict = Game1.content.Load<Dictionary<string, string>>("Strings\\BundleNames");
			}
			catch { }

			Dictionary<string, string> roomNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "Pantry", ModEntry.I18n.Get("lookup.cc.room.pantry").ToString() },
				{ "Crafts Room", ModEntry.I18n.Get("lookup.cc.room.crafts").ToString() },
				{ "CraftsRoom", ModEntry.I18n.Get("lookup.cc.room.crafts").ToString() },
				{ "Fish Tank", ModEntry.I18n.Get("lookup.cc.room.fish-tank").ToString() },
				{ "FishTank", ModEntry.I18n.Get("lookup.cc.room.fish-tank").ToString() },
				{ "Boiler Room", ModEntry.I18n.Get("lookup.cc.room.boiler").ToString() },
				{ "BoilerRoom", ModEntry.I18n.Get("lookup.cc.room.boiler").ToString() },
				{ "Vault", ModEntry.I18n.Get("lookup.cc.room.vault").ToString() },
				{ "Bulletin Board", ModEntry.I18n.Get("lookup.cc.room.bulletin").ToString() },
				{ "BulletinBoard", ModEntry.I18n.Get("lookup.cc.room.bulletin").ToString() },
				{ "Abandoned Joja Mart", ModEntry.I18n.Get("lookup.cc.room.abandoned-joja").ToString() },
				{ "AbandonedJojaMart", ModEntry.I18n.Get("lookup.cc.room.abandoned-joja").ToString() }
			};

			Dictionary<string, List<(string, string)>> dictionary3 = new Dictionary<string, List<(string, string)>>();
			// Bundle keys look like "Room/BundleId" or "Room/BundleId/BundleName" - group all bundles by room.
			foreach (KeyValuePair<string, string> item3 in dictionary)
			{
				string key = item3.Key.Split('/')[0];
				if (!dictionary3.ContainsKey(key))
				{
					dictionary3[key] = new List<(string, string)>();
				}
				dictionary3[key].Add((item3.Key, item3.Value));
			}
			bool[] array3 = default(bool[]);
			foreach (KeyValuePair<string, List<(string, string)>> item4 in dictionary3)
			{
				string key2 = item4.Key;
				string valueOrDefault = roomNameMap.TryGetValue(key2, out var rName) ? rName : (roomNameMap.TryGetValue(key2.Replace(" ", ""), out var rName2) ? rName2 : key2);
				List<(string, string)> value = item4.Value;
				int num = 0;
				List<LookupLink> list = new List<LookupLink>();
				foreach (var item5 in value)
				{
					// Bundle VALUE fields: [0]=English name, [1]=reward, [2]="id stack quality ..." triplets,
					// [4]=items required, [5]=vanilla localized name, [6]=xnb localized name.
					string item = item5.Item1;
					string item2 = item5.Item2;
					string[] array = item2.Split('/');
					if (array.Length < 3)
					{
						continue;
					}
					string text = array[0];
					if (array.Length >= 7 && !string.IsNullOrWhiteSpace(array[6]))
					{
						text = array[6].Trim();
					}
					else if (array.Length >= 6 && !string.IsNullOrWhiteSpace(array[5]))
					{
						text = array[5].Trim();
					}
					else if (bundleNamesDict != null && bundleNamesDict.TryGetValue(array[0], out string? locBundle) && !string.IsNullOrWhiteSpace(locBundle))
					{
						text = locBundle.Trim();
					}

					string text2 = array[2];
					int num2 = ((array.Length > 4 && int.TryParse(array[4], out var result)) ? result : (-1));
					// Bundle id comes from the KEY ("Room/Id"); parse field [1].
					int num3 = 0;
					string[] array2 = item.Split('/');
					if (array2.Length > 1 && int.TryParse(array2[1], out var result2))
					{
						num3 = result2;
					}
					// Completion check: the live bundle stores one bool per item slot;
					// count the true bits and compare against the required amount.
					bool flag = false;
					if (((NetDictionary<int, bool[], NetArray<bool, NetBool>, SerializableDictionary<int, bool[]>, NetBundles>)(object)bundles).TryGetValue(num3, out array3) && array3 != null)
					{
						int num4 = array3.Count((bool s) => s);
						string[] array4 = text2.Split(' ');
						int num5 = array4.Length / 3;
						int num6 = ((num2 > 0) ? num2 : num5);
						if (num4 >= num6)
						{
							flag = true;
						}
					}
					if (flag)
					{
						num++;
						continue;
					}
					// List missing items: walk the triplets in strides of 3 (id/stack/quality)
					// and skip slots already donated; cap the link list at 8 per bundle.
					string[] array5 = text2.Split(' ');
					for (int num7 = 0; num7 + 2 < array5.Length; num7 += 3)
					{
						string text3 = array5[num7];
						int num8 = num7 / 3;
						if (array3 != null && num8 < array3.Length && array3[num8])
						{
							continue;
						}
						ParsedItemData itemData = ItemRegistry.GetData(text3) ?? ItemRegistry.GetData("(O)" + text3);
						string dispName = itemData?.DisplayName;
						if (string.IsNullOrEmpty(dispName) && int.TryParse(text3, out int catId) && catId < 0)
						{
							if (catId == -1)
							{
								itemData = ItemRegistry.GetData("(O)348");
								dispName = itemData?.DisplayName ?? "Wine";
							}
							else
							{
								dispName = catId switch
								{
									-4 => ModEntry.I18n.Get("lookup.category.any-fish").ToString(),
									-5 => ModEntry.I18n.Get("lookup.category.any-egg").ToString(),
									-6 => ModEntry.I18n.Get("lookup.category.any-milk").ToString(),
									-2 => ModEntry.I18n.Get("lookup.category.any-gem").ToString(),
									_ => ModEntry.I18n.Get("lookup.category.any-item").ToString()
								};
							}
						}

						if (!string.IsNullOrEmpty(dispName) && !list.Any((LookupLink l) => l.Text.StartsWith(dispName)))
						{
							list.Add(new LookupLink(dispName + " (" + text + ")", null, Game1.textColor, itemData?.GetTexture(), (itemData != null) ? (Rectangle?)itemData.GetSourceRect(0, null) : null, () => { if (itemData != null) { Item val = ItemRegistry.Create(itemData.QualifiedItemId, 1, 0, false); return (val != null) ? BuildItemSubject(val) : null; } return null; }));
							if (list.Count >= 8)
							{
								break;
							}
						}
					}
				}
				if (num == value.Count)
				{
					lookupSection.Fields.Add(new LookupField(valueOrDefault, ((object)ModEntry.I18n.Get("lookup.world.room-completed")).ToString(), (Color?)new Color(0, 140, 0)));
					continue;
				}
				string text4 = ((object)ModEntry.I18n.Get("lookup.cc.bundles-progress-format", (object)new
				{
					completed = num,
					total = value.Count
				})).ToString();
				if (list.Count > 0)
				{
					lookupSection.Fields.Add(new LookupField(valueOrDefault + " (" + text4 + ")", list));
				}
				else
				{
					lookupSection.Fields.Add(new LookupField(valueOrDefault, text4, (Color?)new Color(180, 100, 0)));
				}
			}
		}
		catch
		{
		}
		return lookupSection;
	}

	/// <summary>All villagers ranked by friendship hearts, with dating/married status and gift info.</summary>
	private static LookupSection BuildFriendshipOverviewSection()
	{
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.friendship-overview")));
		try
		{
			List<LookupLink> list = new List<LookupLink>();
			List<LookupLink> list2 = new List<LookupLink>();
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			Friendship val = default(Friendship);
			foreach (NPC allCharacter in Utility.getAllCharacters())
			{
				if (allCharacter == null || !((Character)allCharacter).IsVillager || ((Character)allCharacter).IsMonster || string.IsNullOrEmpty(((Character)allCharacter).Name) || !((NetDictionary<string, Friendship, NetRef<Friendship>, SerializableDictionary<string, Friendship>, NetStringDictionary<Friendship, NetRef<Friendship>>>)(object)Game1.player.friendshipData).TryGetValue(((Character)allCharacter).Name, out val))
				{
					continue;
				}
				num3++;
				// 250 friendship points = one heart (same rate as everywhere else in the game).
				int num4 = val.Points / 250;
				num2 += num4;
				// Datable villagers cap at 8 hearts until you start dating them (then 10);
				// everyone else can reach 10 right away.
				int num5 = ((((NetFieldBase<bool, NetBool>)(object)allCharacter.datable).Value && !val.IsDating()) ? 8 : 10);
				if (num4 >= num5)
				{
					num++;
				}
				NPC target = allCharacter;
				if (!Game1.player.hasPlayerTalkedToNPC(((Character)allCharacter).Name))
				{
					list.Add(new LookupLink($"{((Character)target).displayName ?? ((Character)target).Name} ({num4}♥)", (string?)null, (Color?)Game1.textColor, target.Portrait, (Rectangle?)new Rectangle(0, 0, 64, 64), (Func<LookupSubject?>?)(() => BuildNPCSubject(target))));
				}
				if (val.GiftsThisWeek < 2 && val.GiftsToday == 0)
				{
					list2.Add(new LookupLink(((object)ModEntry.I18n.Get("lookup.friendship.gifts-left-format", (object)new
					{
						target = (((Character)target).displayName ?? ((Character)target).Name),
						count = 2 - val.GiftsThisWeek
					})).ToString(), (string?)null, (Color?)new Color(0, 140, 0), target.Portrait, (Rectangle?)new Rectangle(0, 0, 64, 64), (Func<LookupSubject?>?)(() => BuildNPCSubject(target))));
				}
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.friendship.summary-label")), ((object)ModEntry.I18n.Get("lookup.friendship.summary-format", (object)new
			{
				maxFriends = num,
				totalVillagers = num3,
				totalHearts = num2
			})).ToString(), (Color?)new Color(180, 50, 180)));
			if (list.Count > 0)
			{
				lookupSection.Fields.Add(new LookupField(((object)ModEntry.I18n.Get("lookup.friendship.unspoken-format", (object)new
				{
					count = list.Count
				})).ToString(), list.Take(12).ToList()));
			}
			else
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.talked-today")), ((object)ModEntry.I18n.Get("lookup.npc.talked-all")).ToString(), (Color?)new Color(0, 140, 0)));
			}
			if (list2.Count > 0)
			{
				lookupSection.Fields.Add(new LookupField(((object)ModEntry.I18n.Get("lookup.friendship.gifts-available-format", (object)new
				{
					count = list2.Count
				})).ToString(), list2.Take(12).ToList()));
			}
		}
		catch
		{
		}
		return lookupSection;
	}

	/// <summary>
	/// Perfection tracker: 12 monster-slayer goals, friend hearts, skill total, stardrops,
	/// cooked/crafted recipes, fish caught, walnuts, and a weighted overall percentage.
	/// </summary>
	private static LookupSection BuildProgressAndPerfectionSection()
	{
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_076f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0761: Unknown result type (might be due to invalid IL or missing references)
		//IL_08af: Unknown result type (might be due to invalid IL or missing references)
		//IL_08bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0953: Unknown result type (might be due to invalid IL or missing references)
		//IL_0945: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ade: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b40: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b32: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b5f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c3b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0c2d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dcd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dbf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e3f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e31: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f92: Unknown result type (might be due to invalid IL or missing references)
		//IL_0f7f: Unknown result type (might be due to invalid IL or missing references)
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.progress-perfection")));
		try
		{
			int num = 0;
			int num2 = 0;
			Dictionary<string, ObjectData> dictionary = DataLoader.Objects(Game1.content);
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, ObjectData> item49 in dictionary)
				{
					string key = item49.Key;
					ObjectData value = item49.Value;
					ParsedItemData val = ItemRegistry.GetData(key) ?? ItemRegistry.GetData("(O)" + key);
					if (val != null && !val.ObjectType.Equals("Arch", StringComparison.OrdinalIgnoreCase) && !val.ObjectType.Equals("Minerals", StringComparison.OrdinalIgnoreCase) && !val.ObjectType.Equals("Fish", StringComparison.OrdinalIgnoreCase) && val.Category != -75 && val.Category != -79 && val.Category != -80 && val.Category != -81 && val.Category != -999 && val.Category != 0 && (value.Type == "Basic" || val.Category == -75 || val.Category == -79 || val.Category == -80 || val.Category == -5 || val.Category == -6 || val.Category == -26 || val.Category == -14 || val.Category == -27 || val.Category == -81))
					{
						num++;
						if (((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.basicShipped).ContainsKey(key) || ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.basicShipped).ContainsKey("(O)" + key))
						{
							num2++;
						}
					}
				}
			}
			if (num == 0)
			{
				num = 145;
			}
			float num3 = Math.Min(15f, (float)num2 / (float)Math.Max(1, num) * 15f);
			float val2 = (float)num2 / (float)Math.Max(1, num) * 100f;
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.shipped-summary")), ((object)ModEntry.I18n.Get("lookup.perfection.shipped-format", (object)new
			{
				shipped = num2,
				total = num,
				percent = $"{Math.Min(100f, val2):0.0}"
			})).ToString(), (Color)((num3 >= 15f) ? new Color(0, 140, 0) : Game1.textColor)));
			List<Building> list = new List<Building>();
			if (Game1.getFarm() != null)
			{
				list.AddRange((IEnumerable<Building>)((GameLocation)Game1.getFarm()).buildings);
			}
			GameLocation locationFromName = Game1.getLocationFromName("IslandWest");
			if (locationFromName != null)
			{
				list.AddRange((IEnumerable<Building>)locationFromName.buildings);
			}
			int num4 = 0;
			if (list.Any((Building b) => ((NetFieldBase<string, NetString>)(object)b.buildingType).Value.Contains("Earth Obelisk")))
			{
				num4++;
			}
			if (list.Any((Building b) => ((NetFieldBase<string, NetString>)(object)b.buildingType).Value.Contains("Water Obelisk")))
			{
				num4++;
			}
			if (list.Any((Building b) => ((NetFieldBase<string, NetString>)(object)b.buildingType).Value.Contains("Desert Obelisk")))
			{
				num4++;
			}
			if (list.Any((Building b) => ((NetFieldBase<string, NetString>)(object)b.buildingType).Value.Contains("Island Obelisk")))
			{
				num4++;
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.obelisks")), ((object)ModEntry.I18n.Get("lookup.perfection.obelisks-built-format", (object)new
			{
				count = num4
			})).ToString(), (Color)((num4 == 4) ? new Color(0, 140, 0) : Color.DarkSlateGray)));
			bool flag = list.Any((Building b) => ((NetFieldBase<string, NetString>)(object)b.buildingType).Value.Contains("Gold Clock"));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.gold-clock")), flag ? ((object)ModEntry.I18n.Get("lookup.perfection.gold-clock-built")).ToString() : ((object)ModEntry.I18n.Get("lookup.perfection.gold-clock-not-built")).ToString(), (Color)(flag ? new Color(0, 140, 0) : Color.DarkSlateGray)));
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress = GetMonsterSlayerProgress("Green Slime");
			string item = monsterSlayerProgress.Category;
			int item2 = monsterSlayerProgress.CurrentKills;
			int item3 = monsterSlayerProgress.RequiredGoal;
			bool item4 = monsterSlayerProgress.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress2 = GetMonsterSlayerProgress("Bat");
			string item5 = monsterSlayerProgress2.Category;
			int item6 = monsterSlayerProgress2.CurrentKills;
			int item7 = monsterSlayerProgress2.RequiredGoal;
			bool item8 = monsterSlayerProgress2.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress3 = GetMonsterSlayerProgress("Skeleton");
			string item9 = monsterSlayerProgress3.Category;
			int item10 = monsterSlayerProgress3.CurrentKills;
			int item11 = monsterSlayerProgress3.RequiredGoal;
			bool item12 = monsterSlayerProgress3.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress4 = GetMonsterSlayerProgress("Shadow Brute");
			string item13 = monsterSlayerProgress4.Category;
			int item14 = monsterSlayerProgress4.CurrentKills;
			int item15 = monsterSlayerProgress4.RequiredGoal;
			bool item16 = monsterSlayerProgress4.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress5 = GetMonsterSlayerProgress("Bug");
			string item17 = monsterSlayerProgress5.Category;
			int item18 = monsterSlayerProgress5.CurrentKills;
			int item19 = monsterSlayerProgress5.RequiredGoal;
			bool item20 = monsterSlayerProgress5.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress6 = GetMonsterSlayerProgress("Duggy");
			string item21 = monsterSlayerProgress6.Category;
			int item22 = monsterSlayerProgress6.CurrentKills;
			int item23 = monsterSlayerProgress6.RequiredGoal;
			bool item24 = monsterSlayerProgress6.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress7 = GetMonsterSlayerProgress("Dust Spirit");
			string item25 = monsterSlayerProgress7.Category;
			int item26 = monsterSlayerProgress7.CurrentKills;
			int item27 = monsterSlayerProgress7.RequiredGoal;
			bool item28 = monsterSlayerProgress7.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress8 = GetMonsterSlayerProgress("Rock Crab");
			string item29 = monsterSlayerProgress8.Category;
			int item30 = monsterSlayerProgress8.CurrentKills;
			int item31 = monsterSlayerProgress8.RequiredGoal;
			bool item32 = monsterSlayerProgress8.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress9 = GetMonsterSlayerProgress("Mummy");
			string item33 = monsterSlayerProgress9.Category;
			int item34 = monsterSlayerProgress9.CurrentKills;
			int item35 = monsterSlayerProgress9.RequiredGoal;
			bool item36 = monsterSlayerProgress9.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress10 = GetMonsterSlayerProgress("Pepper Rex");
			string item37 = monsterSlayerProgress10.Category;
			int item38 = monsterSlayerProgress10.CurrentKills;
			int item39 = monsterSlayerProgress10.RequiredGoal;
			bool item40 = monsterSlayerProgress10.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress11 = GetMonsterSlayerProgress("Serpent");
			string item41 = monsterSlayerProgress11.Category;
			int item42 = monsterSlayerProgress11.CurrentKills;
			int item43 = monsterSlayerProgress11.RequiredGoal;
			bool item44 = monsterSlayerProgress11.IsCompleted;
			(string Category, int CurrentKills, int RequiredGoal, bool IsCompleted) monsterSlayerProgress12 = GetMonsterSlayerProgress("Magma Sprite");
			string item45 = monsterSlayerProgress12.Category;
			int item46 = monsterSlayerProgress12.CurrentKills;
			int item47 = monsterSlayerProgress12.RequiredGoal;
			bool item48 = monsterSlayerProgress12.IsCompleted;
			// 12 slayer goals: count how many monster categories are fully completed.
			int num5 = (item4 ? 1 : 0) + (item8 ? 1 : 0) + (item12 ? 1 : 0) + (item16 ? 1 : 0) + (item20 ? 1 : 0) + (item24 ? 1 : 0) + (item28 ? 1 : 0) + (item32 ? 1 : 0) + (item36 ? 1 : 0) + (item40 ? 1 : 0) + (item44 ? 1 : 0) + (item48 ? 1 : 0);
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.slayer-pct")), ((object)ModEntry.I18n.Get("lookup.perfection.slayer-format", (object)new
			{
				completed = num5
			})).ToString(), (Color)((num5 == 12) ? new Color(0, 140, 0) : Game1.textColor)));
			int num6 = 0;
			int num7 = 0;
			// HashSet.Add returns false for duplicates, so it doubles as a de-dup filter
			// while counting every socializable villager exactly once.
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Friendship val3 = default(Friendship);
			foreach (NPC allCharacter in Utility.getAllCharacters())
			{
				if (allCharacter == null || !((Character)allCharacter).IsVillager || ((Character)allCharacter).IsMonster || !allCharacter.CanSocialize || !hashSet.Add(((Character)allCharacter).Name))
				{
					continue;
				}
				num7++;
				if (((NetDictionary<string, Friendship, NetRef<Friendship>, SerializableDictionary<string, Friendship>, NetStringDictionary<Friendship, NetRef<Friendship>>>)(object)Game1.player.friendshipData).TryGetValue(((Character)allCharacter).Name, out val3))
				{
					int num8 = ((((NetFieldBase<bool, NetBool>)(object)allCharacter.datable).Value && !val3.IsDating()) ? 2000 : 2500);
					if (val3.Points >= num8)
					{
						num6++;
					}
				}
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.friends-pct")), ((object)ModEntry.I18n.Get("lookup.perfection.friends-format", (object)new
			{
				count = num6,
				total = num7
			})).ToString(), (Color)((num6 >= num7 && num7 > 0) ? new Color(0, 140, 0) : Game1.textColor)));
			int num9 = Game1.player.FarmingLevel + Game1.player.MiningLevel + Game1.player.ForagingLevel + Game1.player.FishingLevel + Game1.player.CombatLevel;
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.farmer-level")), ((object)ModEntry.I18n.Get("lookup.perfection.farmer-level-format", (object)new
			{
				total = num9
			})).ToString(), (Color)((num9 >= 50) ? new Color(0, 140, 0) : Game1.textColor)));
			int num10 = 0;
			if (((NetHashSet<string>)(object)Game1.player.mailReceived).Contains("CF_Spouse"))
			{
				num10++;
			}
			if (((NetHashSet<string>)(object)Game1.player.mailReceived).Contains("CF_Mines"))
			{
				num10++;
			}
			if (((NetHashSet<string>)(object)Game1.player.mailReceived).Contains("CF_Fair"))
			{
				num10++;
			}
			if (((NetHashSet<string>)(object)Game1.player.mailReceived).Contains("CF_Fish"))
			{
				num10++;
			}
			if (((NetHashSet<string>)(object)Game1.player.mailReceived).Contains("CF_Sewer"))
			{
				num10++;
			}
			if (((NetHashSet<string>)(object)Game1.player.mailReceived).Contains("CF_Statue"))
			{
				num10++;
			}
			if (((NetHashSet<string>)(object)Game1.player.mailReceived).Contains("CF_Museum"))
			{
				num10++;
			}
			if (num10 == 0)
			{
				num10 = Math.Clamp((Game1.player.MaxStamina - 270) / 34, 0, 7);
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.stardrops-pct")), ((object)ModEntry.I18n.Get("lookup.perfection.stardrops-found-format", (object)new
			{
				count = num10
			})).ToString(), (Color)((num10 == 7) ? new Color(0, 140, 0) : Game1.textColor)));
			int num11 = ((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.recipesCooked).Pairs.Count();
			int count = CraftingRecipe.cookingRecipes.Count;
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.cooking-pct")), ((object)ModEntry.I18n.Get("lookup.perfection.cooking-format", (object)new
			{
				cooked = num11,
				total = count
			})).ToString(), (Color)((num11 >= count) ? new Color(0, 140, 0) : Game1.textColor)));
			int num12 = ((IEnumerable<KeyValuePair<string, int>>)(object)((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.craftingRecipes).Pairs).Count((KeyValuePair<string, int> kv) => kv.Value > 0);
			int num13 = CraftingRecipe.craftingRecipes.Count;
			if (!Game1.IsMultiplayer && CraftingRecipe.craftingRecipes.ContainsKey("Wedding Ring"))
			{
				num13--;
				int num14 = default(int);
				if (((NetDictionary<string, int, NetInt, SerializableDictionary<string, int>, NetStringDictionary<int, NetInt>>)(object)Game1.player.craftingRecipes).TryGetValue("Wedding Ring", out num14) && num14 > 0)
				{
					num12--;
				}
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.crafting-pct")), ((object)ModEntry.I18n.Get("lookup.perfection.crafting-format", (object)new
			{
				crafted = num12,
				total = num13
			})).ToString(), (Color)((num12 >= num13) ? new Color(0, 140, 0) : Game1.textColor)));
			Dictionary<string, string> dictionary2 = DataLoader.Fish(Game1.content);
			int num15 = 0;
			int num16 = 0;
			if (dictionary2 != null)
			{
				foreach (KeyValuePair<string, string> item50 in dictionary2)
				{
					string key2 = item50.Key;
					ParsedItemData val4 = ItemRegistry.GetData(key2) ?? ItemRegistry.GetData("(O)" + key2);
					if (val4 != null && val4.Category == -4 && key2 != "152" && key2 != "153" && key2 != "157" && key2 != "168")
					{
						num15++;
						if (((NetDictionary<string, int[], NetArray<int, NetInt>, SerializableDictionary<string, int[]>, NetStringIntArrayDictionary>)(object)Game1.player.fishCaught).ContainsKey(key2) || ((NetDictionary<string, int[], NetArray<int, NetInt>, SerializableDictionary<string, int[]>, NetStringIntArrayDictionary>)(object)Game1.player.fishCaught).ContainsKey("(O)" + key2))
						{
							num16++;
						}
					}
				}
			}
			// Fallback total (67 vanilla fish) when the data table can't be read,
			// so the percentage never divides by zero or shows 0/0.
			if (num15 == 0)
			{
				num15 = 67;
			}
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.fish-pct")), ((object)ModEntry.I18n.Get("lookup.perfection.fish-caught-format", (object)new
			{
				caught = num16,
				total = num15
			})).ToString(), (Color)((num16 >= num15) ? new Color(0, 140, 0) : Game1.textColor)));
			int goldenWalnutsFound = ((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.GoldenWalnutsFound;
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.perfection.walnuts-pct")), ((object)ModEntry.I18n.Get("lookup.perfection.walnuts-found-format", (object)new
			{
				count = goldenWalnutsFound
			})).ToString(), (Color)((goldenWalnutsFound >= 130) ? new Color(0, 140, 0) : Game1.textColor)));
			// Weighted overall score: each category contributes up to its weight
			// (e.g. slayer 10, friends 10, skills 5, fish 10, walnuts 5...), summed
			// into a percentage that caps at 100.
			float num17 = num3 + (float)num4 * 1f + (flag ? 10f : 0f) + (float)num5 / 12f * 10f + (float)num6 / (float)Math.Max(1, num7) * 10f + ((num9 >= 50) ? 5f : ((float)num9 / 50f * 5f)) + (float)num10 / 7f * 10f + (float)num11 / (float)Math.Max(1, count) * 10f + (float)num12 / (float)Math.Max(1, num13) * 10f + (float)num16 / (float)Math.Max(1, num15) * 10f + (float)goldenWalnutsFound / 130f * 5f;
			// Insert(0, ...) puts the headline percentage FIRST, above the detail rows.
			lookupSection.Fields.Insert(0, new LookupField(((object)ModEntry.I18n.Get("lookup.perfection.tracker-title")).ToString(), ((object)ModEntry.I18n.Get("lookup.perfection.overall-format", (object)new
			{
				percent = $"{Math.Min(100f, num17):0.0}"
			})).ToString(), (Color?)((num17 >= 100f) ? new Color(180, 50, 180) : new Color(20, 110, 220))));
		}
		catch
		{
		}
		return lookupSection;
	}

		/// <summary>Museum card: donations vs 95 total, missing artifacts/minerals links, milestones.</summary>
		private static LookupSection BuildMuseumProgressSection()
	{
		LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.museum-progress"));
		try
		{
			var museumPieces = Game1.netWorldState.Value.MuseumPieces;
			int donatedCount = museumPieces?.Pairs.Count() ?? 0;
			lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.museum.total-donated-label"), ModEntry.I18n.Get("lookup.museum.total-donated-format", new
			{
				count = donatedCount,
				remaining = Math.Max(0, 95 - donatedCount)
			}).ToString(), (donatedCount >= 95) ? new Color(0, 140, 0) : new Color(180, 100, 0)));
			// MuseumPieces stores ids WITHOUT the "(O)" prefix; collect both forms so
			// later lookups match either way.
			HashSet<string> hashSet = new HashSet<string>();
			if (museumPieces != null)
			{
				foreach (KeyValuePair<Vector2, string> current in museumPieces.Pairs)
				{
					hashSet.Add(current.Value);
					hashSet.Add("(O)" + current.Value);
				}
			}
			List<LookupLink> list = new List<LookupLink>();
			List<LookupLink> list2 = new List<LookupLink>();
			Dictionary<string, ObjectData> dictionary = DataLoader.Objects(Game1.content);
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, ObjectData> item in dictionary)
				{
					string key = item.Key;
					ObjectData value = item.Value;
					if (string.IsNullOrEmpty(value.Type) || hashSet.Contains(key) || hashSet.Contains("(O)" + key))
					{
						continue;
					}
					if (value.Type == "Arch")
					{
						ParsedItemData itmData = ItemRegistry.GetData(key) ?? ItemRegistry.GetData("(O)" + key);
						if (itmData != null)
						{
							list.Add(new LookupLink(itmData.DisplayName, null, Game1.textColor, itmData.GetTexture(), itmData.GetSourceRect(0, null), () =>
							{
								Item val = ItemRegistry.Create(itmData.QualifiedItemId, 1, 0, false);
								return (val != null) ? BuildItemSubject(val) : null;
							}));
						}
					}
					else if (value.Type == "Minerals")
					{
						ParsedItemData itmData2 = ItemRegistry.GetData(key) ?? ItemRegistry.GetData("(O)" + key);
						if (itmData2 != null)
						{
							list2.Add(new LookupLink(itmData2.DisplayName, null, Game1.textColor, itmData2.GetTexture(), itmData2.GetSourceRect(0, null), () =>
							{
								Item val = ItemRegistry.Create(itmData2.QualifiedItemId, 1, 0, false);
								return (val != null) ? BuildItemSubject(val) : null;
							}));
						}
					}
				}
			}
			if (list.Count > 0)
			{
				lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.museum.missing-artifacts-format", new
				{
					count = list.Count
				}).ToString(), list.Take(12).ToList()));
			}
			else
			{
				lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.artifacts"), ModEntry.I18n.Get("lookup.perfection.artifacts-all").ToString(), new Color(0, 140, 0)));
			}
			if (list2.Count > 0)
			{
				lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.museum.missing-minerals-format", new
				{
					count = list2.Count
				}).ToString(), list2.Take(12).ToList()));
			}
			else
			{
				lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.minerals"), ModEntry.I18n.Get("lookup.perfection.minerals-all").ToString(), new Color(0, 140, 0)));
			}
			int[] source = new int[]
			{
				5, 10, 15, 20, 25, 30, 35, 40, 50, 60,
				70, 80, 90, 95
			};
			// Donation milestones (rewards at 5, 10, ... 95 items); FirstOrDefault picks the
		// first threshold still above our count = the NEXT milestone. 0 means all reached.
		int num = source.FirstOrDefault((int m) => m > donatedCount);
			if (num > 0)
			{
				lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.building.milestone"), ModEntry.I18n.Get("lookup.museum.next-milestone-format", new
				{
					needed = num - donatedCount,
					milestone = num
				}).ToString(), new Color(20, 110, 220)));
			}
			else
			{
				lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.perfection.museum-pct"), ModEntry.I18n.Get("lookup.perfection.museum-all").ToString(), new Color(180, 50, 180)));
			}
		}
		catch
		{
		}
		return lookupSection;
	}

	/// <summary>Mines card: deepest regular floor, Skull Cavern record, and 12 slayer goals.</summary>
	private static LookupSection BuildMineAndGuildProgressSection()
	{
		LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.mine-guild-progress"));
		try
		{
			// Floors 1-120 are the regular mine; anything deeper is the Skull Cavern record.
			int deepestMineLevel = Game1.player.deepestMineLevel;
			int num = Math.Min(120, deepestMineLevel);
			lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.mine.regular-depth-label"), (num >= 120) ? ModEntry.I18n.Get("lookup.mine.regular-depth-bottom").ToString() : ModEntry.I18n.Get("lookup.mine.regular-depth-progress", new
			{
				floor = num
			}).ToString(), (num >= 120) ? new Color(0, 140, 0) : new Color(180, 100, 0)));
			if (deepestMineLevel > 120)
			{
				lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.skull-record"), ModEntry.I18n.Get("lookup.mine.skull-record-format", new
				{
					level = deepestMineLevel - 120
				}).ToString(), new Color(180, 50, 180)));
			}
			else
			{
				lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.farmer.skull-record"), ModEntry.I18n.Get("lookup.farmer.skull-unexplored").ToString(), Color.DarkSlateGray));
			}
			// All 12 slayer goals fetched up front into a fixed-size tuple array,
			// then rendered one row per category below.
			(string, int, int, bool)[] array = new(string, int, int, bool)[12]
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
			for (int i = 0; i < array.Length; i++)
			{
				(string, int, int, bool) tuple = array[i];
				string item = tuple.Item1;
				int item2 = tuple.Item2;
				int item3 = tuple.Item3;
				bool item4 = tuple.Item4;
				string value = (item4 ? ModEntry.I18n.Get("lookup.mine.slayer-completed-format", new
				{
					kills = item2,
					goal = item3
				}).ToString() : ModEntry.I18n.Get("lookup.mine.slayer-progress-format", new
				{
					kills = item2,
					goal = item3,
					remaining = item3 - item2
				}).ToString());
				lookupSection.Fields.Add(new LookupField("• " + GetLocalizedMonsterCategory(item), value, (Color)(item4 ? new Color(0, 140, 0) : Game1.textColor)));
			}
		}
		catch
		{
		}
		return lookupSection;
	}
    }
}




