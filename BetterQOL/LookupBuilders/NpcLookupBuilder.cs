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
    /// Lookup builder for NPCs, villagers, friendship levels, gift tastes, and schedules.
    /// </summary>
    public static partial class LookupDataManager
    {
	public static LookupSubject BuildNPCSubject(NPC npc)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_046c: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_066d: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0929: Unknown result type (might be due to invalid IL or missing references)
		//IL_0935: Unknown result type (might be due to invalid IL or missing references)
		//IL_094f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_0890: Unknown result type (might be due to invalid IL or missing references)
		//IL_0882: Unknown result type (might be due to invalid IL or missing references)
		LookupSubject lookupSubject = new LookupSubject
		{
			Title = (((Character)npc).displayName ?? ((Character)npc).Name),
			Portrait = npc.Portrait,
			PortraitSourceRect = new Rectangle(0, 0, 64, 64)
		};
		string season = ((object)ModEntry.I18n.Get("season." + (npc.Birthday_Season?.ToLower() ?? "spring"))).ToString();
		lookupSubject.Subtitle = (ModEntry.I18n.Get("lookup.npc.subtitle", (object)new
		{
			season = season,
			day = npc.Birthday_Day
		}));
		LookupSection lookupSection = new LookupSection((ModEntry.I18n.Get("lookup.section.relationship")));
		Friendship val = default(Friendship);
		if (((NetDictionary<string, Friendship, NetRef<Friendship>, SerializableDictionary<string, Friendship>, NetStringDictionary<Friendship, NetRef<Friendship>>>)(object)Game1.player.friendshipData).TryGetValue(((Character)npc).Name, out val))
		{
			int points = val.Points;
			int hearts = points / 250;
			int ptsInHeart = points % 250;
			int maxHearts = ((val.IsMarried() || val.IsRoommate()) ? 14 : (val.IsDating() ? 10 : 8));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.friendship")), ((object)ModEntry.I18n.Get("lookup.npc.friendship-format", (object)new { hearts, maxHearts, points, ptsInHeart })).ToString(), (Color?)new Color(220, 20, 60)));
			if (((Character)npc).currentLocation != null)
			{
				string location = ((Character)npc).currentLocation.DisplayName ?? ((Character)npc).currentLocation.Name;
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.current-location")), ((object)ModEntry.I18n.Get("lookup.npc.current-location-format", (object)new
				{
					location = location,
					x = (int)((Character)npc).Tile.X,
					y = (int)((Character)npc).Tile.Y
				})).ToString(), (Color?)new Color(20, 110, 220)));
			}
			bool flag = Game1.player.hasPlayerTalkedToNPC(((Character)npc).Name);
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.talked-today")), (flag ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no")), (Color?)(flag ? new Color(0, 140, 0) : new Color(200, 60, 20))));
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.gifts-this-week")), ((object)ModEntry.I18n.Get("lookup.npc.gifts-this-week-format", (object)new
			{
				count = val.GiftsThisWeek,
				today = ((val.GiftsToday > 0) ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"))
			})).ToString(), (Color)((val.GiftsThisWeek >= 2) ? new Color(0, 140, 0) : Game1.textColor)));
			if (val.IsMarried())
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.status")), (ModEntry.I18n.Get("lookup.npc.status-married")), (Color?)new Color(180, 50, 180)));
			}
			else if (val.IsRoommate())
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.status")), (ModEntry.I18n.Get("lookup.npc.status-roommate")), (Color?)new Color(180, 50, 180)));
			}
			else if (val.IsDating())
			{
				lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.status")), (ModEntry.I18n.Get("lookup.npc.status-dating")), (Color?)new Color(220, 20, 60)));
			}
		}
		else
		{
			lookupSection.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.friendship")), (ModEntry.I18n.Get("lookup.npc.unmet")), Color.DarkSlateGray));
		}
		lookupSubject.Sections.Add(lookupSection);
		if (ModEntry.Config.ShowGiftTastes)
		{
			LookupSection lookupSection2 = new LookupSection((ModEntry.I18n.Get("lookup.section.gifts")));
			var (list, list2, list3, list4) = GetNPCAllGiftPreferenceLinks(npc);
			if (list.Count > 0)
			{
				lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.loved-gifts")), list));
			}
			if (list2.Count > 0)
			{
				lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.liked-gifts")), list2));
			}
			if (list3.Count > 0)
			{
				lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.neutral-gifts")), list3));
			}
			if (list4.Count > 0)
			{
				lookupSection2.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.disliked-gifts")), list4));
			}
			lookupSubject.Sections.Add(lookupSection2);
		}
		try
		{
			LookupSection lookupSection3 = new LookupSection((ModEntry.I18n.Get("lookup.section.schedule", (object)"Daily Schedule")));
			if (((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.IslandVisitors != null && ((NetFieldBase<NetWorldState, NetRef<NetWorldState>>)(object)Game1.netWorldState).Value.IslandVisitors.Contains(((Character)npc).Name))
			{
				lookupSection3.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.npc.today-schedule")), ((object)ModEntry.I18n.Get("lookup.npc.schedule-island")).ToString(), (Color?)new Color(20, 110, 220)));
			}
			else if (npc.Schedule != null && npc.Schedule.Count > 0)
			{
				List<KeyValuePair<int, SchedulePathDescription>> list5 = npc.Schedule.OrderBy((KeyValuePair<int, SchedulePathDescription> kv) => kv.Key).ToList();
				for (int num = 0; num < list5.Count; num++)
				{
					KeyValuePair<int, SchedulePathDescription> keyValuePair = list5[num];
					int key = keyValuePair.Key;
					SchedulePathDescription value = keyValuePair.Value;
					string text = FormatGameTime(key.ToString());
					object obj = value.targetLocationName;
					if (obj == null)
					{
						GameLocation currentLocation = ((Character)npc).currentLocation;
						obj = ((currentLocation != null) ? currentLocation.DisplayName : null) ?? ((object)ModEntry.I18n.Get("lookup.schedule.unknown-location")).ToString();
					}
					string text2 = (string)obj;
					GameLocation locationFromName = Game1.getLocationFromName(text2);
					string value2 = ((locationFromName != null) ? locationFromName.DisplayName : null) ?? text2;
					bool flag2 = false;
					int num2 = ((num + 1 < list5.Count) ? list5[num + 1].Key : 2600);
					if (Game1.timeOfDay >= key && Game1.timeOfDay < num2)
					{
						flag2 = true;
					}
					string text3 = $"{value2} (Tile: {value.targetTile.X}, {value.targetTile.Y})";
					if (!string.IsNullOrEmpty(value.endOfRouteBehavior))
					{
						text3 = text3 + " — " + value.endOfRouteBehavior;
					}
					string label = text + (flag2 ? ((object)ModEntry.I18n.Get("lookup.schedule.current-tag")).ToString() : "");
					lookupSection3.Fields.Add(new LookupField(label, text3, (Color)(flag2 ? new Color(0, 140, 0) : Game1.textColor)));
				}
			}
			else
			{
				string location2 = ((((Character)npc).currentLocation != null) ? (((Character)npc).currentLocation.DisplayName ?? ((Character)npc).currentLocation.Name) : ((object)ModEntry.I18n.Get("lookup.schedule.unknown-location")).ToString());
				lookupSection3.Fields.Add(new LookupField((ModEntry.I18n.Get("lookup.section.schedule")), ((object)ModEntry.I18n.Get("lookup.schedule.no-departures", (object)new
				{
					location = location2,
					x = (int)((Character)npc).Tile.X,
					y = (int)((Character)npc).Tile.Y
				})).ToString(), Color.DarkSlateGray));
			}
			lookupSubject.Sections.Add(lookupSection3);
		}
		catch
		{
		}
		return lookupSubject;
	}

	private static (List<LookupLink> Loved, List<LookupLink> Liked, List<LookupLink> Neutral, List<LookupLink> Disliked) GetNPCAllGiftPreferenceLinks(NPC npc)
	{
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_032c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
		//IL_042d: Unknown result type (might be due to invalid IL or missing references)
		List<LookupLink> list = new List<LookupLink>();
		List<LookupLink> list2 = new List<LookupLink>();
		List<LookupLink> list3 = new List<LookupLink>();
		List<LookupLink> list4 = new List<LookupLink>();
		try
		{
			if (Game1.NPCGiftTastes != null && Game1.NPCGiftTastes.TryGetValue(((Character)npc).Name, out var value))
			{
				string[] array = value.Split('/');
				if (array.Length > 1 && !string.IsNullOrEmpty(array[1]))
				{
					string[] array2 = array[1].Split(' ');
					foreach (string text in array2)
					{
						ParsedItemData data = ItemRegistry.GetData(text) ?? ItemRegistry.GetData("(O)" + text);
						if (data != null && !list.Any((LookupLink l) => l.Text == data.DisplayName))
						{
							list.Add(new LookupLink(data.DisplayName, (string?)null, (Color?)new Color(180, 50, 180), data.GetTexture(), (Rectangle?)data.GetSourceRect(0, (int?)null), (Func<LookupSubject?>?)delegate
							{
								Item val = ItemRegistry.Create(data.QualifiedItemId, 1, 0, false);
								return (val != null) ? BuildItemSubject(val) : null;
							}));
						}
					}
				}
				if (array.Length > 3 && !string.IsNullOrEmpty(array[3]))
				{
					string[] array3 = array[3].Split(' ');
					foreach (string text2 in array3)
					{
						ParsedItemData data2 = ItemRegistry.GetData(text2) ?? ItemRegistry.GetData("(O)" + text2);
						if (data2 != null && !list2.Any((LookupLink l) => l.Text == data2.DisplayName))
						{
							list2.Add(new LookupLink(data2.DisplayName, (string?)null, (Color?)new Color(0, 140, 0), data2.GetTexture(), (Rectangle?)data2.GetSourceRect(0, (int?)null), (Func<LookupSubject?>?)delegate
							{
								Item val = ItemRegistry.Create(data2.QualifiedItemId, 1, 0, false);
								return (val != null) ? BuildItemSubject(val) : null;
							}));
						}
					}
				}
				if (array.Length > 5 && !string.IsNullOrEmpty(array[5]))
				{
					string[] array4 = array[5].Split(' ');
					foreach (string text3 in array4)
					{
						ParsedItemData data3 = ItemRegistry.GetData(text3) ?? ItemRegistry.GetData("(O)" + text3);
						if (data3 != null && !list4.Any((LookupLink l) => l.Text == data3.DisplayName))
						{
							list4.Add(new LookupLink(data3.DisplayName, (string?)null, (Color?)new Color(200, 60, 20), data3.GetTexture(), (Rectangle?)data3.GetSourceRect(0, (int?)null), (Func<LookupSubject?>?)delegate
							{
								Item val = ItemRegistry.Create(data3.QualifiedItemId, 1, 0, false);
								return (val != null) ? BuildItemSubject(val) : null;
							}));
						}
					}
				}
				if (array.Length > 9 && !string.IsNullOrEmpty(array[9]))
				{
					string[] array5 = array[9].Split(' ');
					foreach (string text4 in array5)
					{
						ParsedItemData data4 = ItemRegistry.GetData(text4) ?? ItemRegistry.GetData("(O)" + text4);
						if (data4 != null && !list3.Any((LookupLink l) => l.Text == data4.DisplayName))
						{
							list3.Add(new LookupLink(data4.DisplayName, null, Color.DarkSlateGray, data4.GetTexture(), data4.GetSourceRect(0, (int?)null), delegate
							{
								Item val = ItemRegistry.Create(data4.QualifiedItemId, 1, 0, false);
								return (val != null) ? BuildItemSubject(val) : null;
							}));
						}
					}
				}
			}
		}
		catch
		{
		}
		return (Loved: list.Take(12).ToList(), Liked: list2.Take(12).ToList(), Neutral: list3.Take(8).ToList(), Disliked: list4.Take(8).ToList());
	}

    }
}