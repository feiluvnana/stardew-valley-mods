using StardewModdingAPI;
using StardewModdingAPI.Utilities;

// ModConfig holds every user-facing setting for BetterQOL.
// SMAPI serializes this plain class to/from the mod's config.json automatically,
// and the Generic Mod Config Menu mod displays each property as an in-game row.
namespace BetterQOL
{
    /// <summary>
    /// Container of all configurable options. Each property is an "auto-property":
    /// the compiler secretly creates a hidden backing field, with "{ get; set; }"
    /// acting as the getter/setter pair. The "= value" after each property is the
    /// default used the first time the mod runs (before config.json exists).
    /// </summary>
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

        /// <summary>Whether to show exact XP, remaining XP, and progress percentage when hovering over skills in the Skills page.</summary>
        public bool ShowExactExperienceInSkillsPage { get; set; } = true;

        // SButton is SMAPI's cross-device enum: one type covers keyboard keys, mouse
        // buttons, and controller buttons (so a single setting can serve all inputs).
        /// <summary>Optional key to hold to show hover tooltips (default None: always shows on hover).</summary>
        public SButton HoverHotkey { get; set; } = SButton.None;

        // ---------------- Lookup Anything (F1 by default) ----------------
        /// <summary>Whether the Lookup Anything feature is enabled.</summary>
        public bool EnableLookupAnything { get; set; } = true;

        /// <summary>Keyboard key to press to open the detailed lookup window (default: F1).</summary>
        public SButton LookupKey { get; set; } = SButton.F1;

        /// <summary>Controller button to press to open the detailed lookup window (default: RightStick / R3).</summary>
        public SButton ControllerLookupKey { get; set; } = SButton.RightStick;

        /// <summary>Whether to display loved/liked gift tastes in lookup cards.</summary>
        public bool ShowGiftTastes { get; set; } = true;

        /// <summary>Whether to display crafting and cooking recipes using the item in lookup cards.</summary>
        public bool ShowItemRecipes { get; set; } = true;

        /// <summary>Whether to display Community Center bundle and Museum donation needs in lookup cards.</summary>
        public bool ShowBundleAndMuseumInfo { get; set; } = true;

        /// <summary>Whether to display the Community Center bundle progress section in the World Overview.</summary>
        public bool ShowCommunityCenterProgress { get; set; } = true;

        /// <summary>Whether to display the Friendship Overview section in the World Overview.</summary>
        public bool ShowFriendshipOverview { get; set; } = true;

        /// <summary>Whether to display the Collections & Perfection Tracker section in the World Overview.</summary>
        public bool ShowProgressAndPerfection { get; set; } = true;

        /// <summary>Whether to display Mine levels and Monster Slayer Goals in the World Overview.</summary>
        public bool ShowMineAndGuildProgress { get; set; } = true;

        /// <summary>Whether to display Museum donation progress in the World Overview.</summary>
        public bool ShowMuseumProgress { get; set; } = true;

        // ---------------- Object & Environment Transparency ----------------
        /// <summary>Master toggle to enable or disable all dynamic object transparency features.</summary>
        public bool EnableTransparency { get; set; } = true;

        // Buildings
        /// <summary>Whether to apply custom transparency to player-constructed farm buildings.</summary>
        public bool EnableBuildingTransparency { get; set; } = true;
        /// <summary>Whether building transparency only activates when the player is behind (above) the building.</summary>
        public bool BuildingBelowPlayerOnly { get; set; } = true;
        /// <summary>Tile distance around the player for building transparency activation.</summary>
        public int BuildingTileDistance { get; set; } = 3;
        /// <summary>Minimum opacity for transparent buildings (0.0 to 1.0).</summary>
        public float BuildingMinimumOpacity { get; set; } = 0.4f;

        // Bushes
        /// <summary>Whether to apply custom transparency to bushes.</summary>
        public bool EnableBushTransparency { get; set; } = true;
        /// <summary>Whether bush transparency only activates when the player is behind (above) the bush.</summary>
        public bool BushBelowPlayerOnly { get; set; } = true;
        /// <summary>Tile distance around the player for bush transparency activation.</summary>
        public int BushTileDistance { get; set; } = 5;
        /// <summary>Minimum opacity for transparent bushes (0.0 to 1.0).</summary>
        public float BushMinimumOpacity { get; set; } = 0.4f;

        // Trees & Fruit Trees
        /// <summary>Whether to apply custom transparency to full-grown wild trees and fruit trees.</summary>
        public bool EnableTreeTransparency { get; set; } = true;
        /// <summary>Whether tree transparency only activates when the player is behind (above) the tree.</summary>
        public bool TreeBelowPlayerOnly { get; set; } = true;
        /// <summary>Tile distance around the player for tree canopy transparency activation.</summary>
        public int TreeTileDistance { get; set; } = 5;
        /// <summary>Minimum opacity for transparent tree canopies (0.0 to 1.0).</summary>
        public float TreeMinimumOpacity { get; set; } = 0.1f;

        // Grass
        /// <summary>Whether to apply custom transparency to tall grass.</summary>
        public bool EnableGrassTransparency { get; set; } = true;
        /// <summary>Whether grass transparency only activates when the player is behind (above) the grass.</summary>
        public bool GrassBelowPlayerOnly { get; set; } = false;
        /// <summary>Tile distance around the player for grass transparency activation.</summary>
        public int GrassTileDistance { get; set; } = 3;
        /// <summary>Minimum opacity for transparent grass (0.0 to 1.0).</summary>
        public float GrassMinimumOpacity { get; set; } = 0.3f;

        // Crops
        /// <summary>Whether to apply custom transparency to crops in dirt and garden pots.</summary>
        public bool EnableCropTransparency { get; set; } = false;
        /// <summary>Whether crop transparency only activates when the player is behind (above) the crop.</summary>
        public bool CropBelowPlayerOnly { get; set; } = false;
        /// <summary>Tile distance around the player for crop transparency activation.</summary>
        public int CropTileDistance { get; set; } = 3;
        /// <summary>Minimum opacity for transparent crops (0.0 to 1.0).</summary>
        public float CropMinimumOpacity { get; set; } = 0.4f;

        // Objects (forage, stones, weeds, twigs)
        /// <summary>Whether to apply custom transparency to small forage items, stones, weeds, and debris.</summary>
        public bool EnableObjectTransparency { get; set; } = false;
        /// <summary>Whether object transparency only activates when the player is behind (above) the object.</summary>
        public bool ObjectBelowPlayerOnly { get; set; } = true;
        /// <summary>Tile distance around the player for object transparency activation.</summary>
        public int ObjectTileDistance { get; set; } = 3;
        /// <summary>Minimum opacity for transparent objects (0.0 to 1.0).</summary>
        public float ObjectMinimumOpacity { get; set; } = 0.4f;

        // Big Craftables (machines, scarecrows, chests)
        /// <summary>Whether to apply custom transparency to craftable machines, scarecrows, and placables.</summary>
        public bool EnableCraftableTransparency { get; set; } = false;
        /// <summary>Whether craftable transparency only activates when the player is behind (above) the craftable.</summary>
        public bool CraftableBelowPlayerOnly { get; set; } = true;
        /// <summary>Tile distance around the player for craftable transparency activation.</summary>
        public int CraftableTileDistance { get; set; } = 3;
        /// <summary>Minimum opacity for transparent craftables (0.0 to 1.0).</summary>
        public float CraftableMinimumOpacity { get; set; } = 0.4f;

        // Transparency Keybinds
        /// <summary>Keybind to temporarily disable custom transparency and revert to vanilla rendering.</summary>
        public KeybindList DisableTransparencyKey { get; set; } = new();

        /// <summary>Keybind to force maximum transparency on all affected objects.</summary>
        public KeybindList FullTransparencyKey { get; set; } = new();
    }
}
