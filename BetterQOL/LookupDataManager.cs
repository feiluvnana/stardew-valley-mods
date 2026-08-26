using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace BetterQOL
{
    /// <summary>
    /// One clickable "chip" (a small labeled button) inside a lookup page, e.g. a fish
    /// name that jumps to that fish's own lookup screen. This class is pure DATA at
    /// creation time; its Bounds rectangle is filled in later while drawing, when the
    /// chip's on-screen position becomes known.
    /// </summary>
    public class LookupLink
    {
        /// <summary>The visible label text of this clickable chip.</summary>
        public string Text { get; set; } = string.Empty;
        /// <summary>Optional secondary text shown right-aligned in search rows.</summary>
        public string? Subtitle { get; set; }
        /// <summary>Optional icon texture to draw beside the label ("?" = may be absent).</summary>
        public Texture2D? Icon { get; set; }
        /// <summary>Pixel region inside Icon when it is a spritesheet packing many icons.</summary>
        public Microsoft.Xna.Framework.Rectangle? IconSourceRect { get; set; }
        /// <summary>Tint used when rendering the label (defaults to standard game text color).</summary>
        public Color TextColor { get; set; } = Game1.textColor;
        /// <summary>
        /// The click behavior as a DELEGATE: "Func&lt;LookupSubject?&gt;" reads as "a function
        /// taking nothing and returning either a LookupSubject or null". Assigning a method
        /// here lets one generic click handler invoke whatever navigation each link wants.
        /// </summary>
        public Func<LookupSubject?>? OnClick { get; set; }
        /// <summary>Screen-space hit box assigned during drawing so clicks can find this link.</summary>
        public Microsoft.Xna.Framework.Rectangle Bounds { get; set; }

        /// <summary>
        /// Builds a link. Parameters with "= null" are OPTIONAL: callers may omit them and
        /// C# fills in the defaults. "Color? textColor" being null means "not specified",
        /// and "??" picks the fallback game color.
        /// </summary>
        public LookupLink(string text, string? subtitle = null, Color? textColor = null, Texture2D? icon = null, Microsoft.Xna.Framework.Rectangle? iconSourceRect = null, Func<LookupSubject?>? onClick = null)
        {
            // Copy each argument into the matching property - a classic constructor pattern.
            Text = text;
            Subtitle = subtitle;
            TextColor = textColor ?? Game1.textColor;
            Icon = icon;
            IconSourceRect = iconSourceRect;
            OnClick = onClick;
        }
    }

    /// <summary>
    /// One "Label: Value" row inside a lookup section. A field shows EITHER a plain text
    /// value OR a row of clickable links (chips) - never both at once. Which constructor
    /// was used decides which mode applies.
    /// </summary>
    public class LookupField
    {
        /// <summary>Left-hand caption, e.g. "Best Season" or "Sell Price".</summary>
        public string Label { get; set; } = string.Empty;
        /// <summary>Right-hand plain text answer, if this is a text row.</summary>
        public string? Value { get; set; }
        /// <summary>Tint for the value text (red for bad news, green for good, etc.).</summary>
        public Color ValueColor { get; set; } = Game1.textColor;
        /// <summary>Clickable chips shown under the label, if this is a link row.</summary>
        public List<LookupLink> Links { get; set; } = new();

        /// <summary>Constructor for plain text rows: label plus a colored string answer.</summary>
        public LookupField(string label, string value, Color? valueColor = null)
        {
            Label = label;
            Value = value;
            // "??" substitutes the default color when the caller passed null.
            ValueColor = valueColor ?? Game1.textColor;
        }

        /// <summary>
        /// OVERLOADED constructor - same method name, different parameter list. C# picks
        /// the matching overload by argument types. This one builds chip-row fields.
        /// </summary>
        public LookupField(string label, List<LookupLink> links)
        {
            Label = label;
            Links = links;
        }
    }

    /// <summary>
    /// A titled block of fields within a page ("Combat Stats", "Growth", ...). The whole
    /// lookup page is just a vertical list of these sections - simple composition of
    /// small classes instead of one giant monolithic structure.
    /// </summary>
    public class LookupSection
    {
        /// <summary>Bold header text drawn above the section's fields.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>The label/value/chip rows belonging to this section.</summary>
        public List<LookupField> Fields { get; set; } = new();

        /// <summary>Creates an empty section that builders can then add fields to.</summary>
        public LookupSection(string title)
        {
            Title = title;
        }
    }

    /// <summary>
    /// The complete data model for ONE lookup page: what to show in the header (title,
    /// portrait/icon) and which sections fill the scrollable body. Builders in the
    /// LookupBuilders folder produce these; LookupMenu merely renders them.
    /// </summary>
    public class LookupSubject
    {
        /// <summary>Big bold heading, usually the item/NPC/creature name.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Optional smaller line under the title (category, quality, etc.).</summary>
        public string? Subtitle { get; set; }
        /// <summary>Square icon texture for the header (items, crops, buildings...).</summary>
        public Texture2D? MainIcon { get; set; }
        /// <summary>Pick region within MainIcon when it lives on a spritesheet.</summary>
        public Microsoft.Xna.Framework.Rectangle? MainIconSourceRect { get; set; }
        /// <summary>Larger face portrait (villagers and monsters), if available.</summary>
        public Texture2D? Portrait { get; set; }
        /// <summary>Pick region within Portrait when it lives on a spritesheet.</summary>
        public Microsoft.Xna.Framework.Rectangle? PortraitSourceRect { get; set; }
        /// <summary>All content blocks displayed below the header, top to bottom.</summary>
        public List<LookupSection> Sections { get; set; } = new();
    }

    /// <summary>
    /// Central coordinator and data access manager for Lookup Anything (F1).
    /// Modular builders for individual domains (NPC, Item, Monster, Animal, Tree, Building, Progress, Search)
    /// are organized by domain under the LookupBuilders/ directory.
    /// </summary>
    /// <remarks>
    /// "partial" tells the compiler this class is SPLIT across several .cs files that are
    /// stitched together at compile time - handy when one logical class would otherwise
    /// span many thousands of lines (the other parts live in LookupBuilders/).
    /// </remarks>
    public static partial class LookupDataManager
    {
    }
}
