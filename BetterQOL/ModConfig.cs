using StardewModdingAPI;

namespace BetterQOL
{
    public class ModConfig
    {
        // ---------------- Blacksmith Geode Cracking ----------------
        /// <summary>Whether to skip the cracking animation for instantaneous results in single clicks.</summary>
        public bool InstantCracking { get; set; } = false;

        /// <summary>Whether to show the dedicated 'Crack All' button in Clint's geode menu.</summary>
        public bool ShowCrackAllButton { get; set; } = true;

        /// <summary>Maximum batch size when clicking 'Crack All' or Shift+Clicking on the anvil.</summary>
        public int BulkBatchSize { get; set; } = 999;

        /// <summary>Whether to display a HUD summary toast after bulk cracking.</summary>
        public bool ShowSummaryToast { get; set; } = true;

        // ---------------- Farm Machine Options (Geode Crusher) ----------------
        /// <summary>Whether Geode Crusher machines process instantly (0 minutes).</summary>
        public bool InstantGeodeCrusher { get; set; } = false;

        /// <summary>Whether Geode Crusher machines require 1 Coal to operate.</summary>
        public bool GeodeCrusherRequiresCoal { get; set; } = true;

        // ---------------- Item Stacking Options ----------------
        /// <summary>Maximum stack size limit for normally unstackable items.</summary>
        public int MaxStackSize { get; set; } = 999;

        /// <summary>Allow fishing tackle/bobbers with matching durability to stack.</summary>
        public bool EnableTackleStacking { get; set; } = true;

        /// <summary>Allow identical 1.6 trinkets to stack.</summary>
        public bool EnableTrinketStacking { get; set; } = true;

        /// <summary>Allow furniture and decorations to stack.</summary>
        public bool EnableFurnitureStacking { get; set; } = true;

        /// <summary>Allow identical rings and combined rings to stack.</summary>
        public bool EnableRingStacking { get; set; } = true;

        /// <summary>Allow identical clothing and hats to stack.</summary>
        public bool EnableClothingAndHatStacking { get; set; } = true;

        /// <summary>Allow identical boots to stack.</summary>
        public bool EnableBootsStacking { get; set; } = true;

        // ---------------- Hover Information & Timers (UI Info Suite 2 Style) ----------------
        /// <summary>Whether to show crop growth time, harvest readiness, and soil info when hovering.</summary>
        public bool EnableCropHover { get; set; } = true;

        /// <summary>Whether to show machine processing time remaining, finish times, and outputs when hovering.</summary>
        public bool EnableMachineHover { get; set; } = true;

        /// <summary>Whether to show fruit tree maturation/yield and wild tree stages when hovering.</summary>
        public bool EnableTreeHover { get; set; } = true;

        /// <summary>Whether to show animal/pet friendship, daily petting, and produce info when hovering.</summary>
        public bool EnableAnimalHover { get; set; } = true;

        /// <summary>Whether to display water and fertilizer status in crop tooltips.</summary>
        public bool ShowWaterAndFertilizer { get; set; } = true;

        /// <summary>Whether to render produce/item icons in hover tooltips.</summary>
        public bool ShowItemIconInTooltip { get; set; } = true;

        /// <summary>Whether to display the exact clock time when machines will finish.</summary>
        public bool ShowExactFinishTime { get; set; } = true;

        /// <summary>Whether to show item sell price in inventory/menu tooltips.</summary>
        public bool ShowItemSellPriceOnHover { get; set; } = true;

        /// <summary>Whether to show Community Center bundle need in inventory/menu tooltips.</summary>
        public bool ShowBundleNeedOnHover { get; set; } = true;

        /// <summary>Whether to show Museum donation status in inventory/menu tooltips.</summary>
        public bool ShowMuseumNeedOnHover { get; set; } = true;

        /// <summary>Optional key to hold to show hover tooltips (default None: always shows on hover).</summary>
        public SButton HoverHotkey { get; set; } = SButton.None;

        // ---------------- Lookup Anything (F1 by default) ----------------
        /// <summary>Whether the Lookup Anything feature is enabled.</summary>
        public bool EnableLookupAnything { get; set; } = true;

        /// <summary>Key to press to open the detailed lookup window for whatever is hovered.</summary>
        public SButton LookupKey { get; set; } = SButton.F1;

        /// <summary>Whether to display loved/liked gift tastes in lookup cards.</summary>
        public bool ShowGiftTastes { get; set; } = true;

        /// <summary>Whether to display crafting and cooking recipes using the item in lookup cards.</summary>
        public bool ShowItemRecipes { get; set; } = true;

        /// <summary>Whether to display Community Center bundle and Museum donation needs in lookup cards.</summary>
        public bool ShowBundleAndMuseumInfo { get; set; } = true;
    }
}
