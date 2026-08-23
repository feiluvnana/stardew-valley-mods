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
using xTile.Dimensions;

namespace BetterQOL
{
    public class LookupLink
    {
        public string Text { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public Texture2D? Icon { get; set; }
        public Microsoft.Xna.Framework.Rectangle? IconSourceRect { get; set; }
        public Color TextColor { get; set; } = Game1.textColor;
        public Func<LookupSubject?>? OnClick { get; set; }
        public Microsoft.Xna.Framework.Rectangle Bounds { get; set; }

        public LookupLink(string text, string? subtitle = null, Color? textColor = null, Texture2D? icon = null, Microsoft.Xna.Framework.Rectangle? iconSourceRect = null, Func<LookupSubject?>? onClick = null)
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
        public Microsoft.Xna.Framework.Rectangle? MainIconSourceRect { get; set; }
        public Texture2D? Portrait { get; set; }
        public Microsoft.Xna.Framework.Rectangle? PortraitSourceRect { get; set; }
        public List<LookupSection> Sections { get; set; } = new();
    }

    /// <summary>
    /// Central coordinator and data access manager for Lookup Anything (F1).
    /// Modular builders for individual domains (NPC, Item, Monster, Animal, Tree, Building, Progress, Search)
    /// are organized by domain under the LookupBuilders/ directory.
    /// </summary>
    public static partial class LookupDataManager
    {
    }
}