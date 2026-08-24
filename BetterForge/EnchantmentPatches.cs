using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Extensions;

// EnchantmentPatches hooks the game's "pick a random enchantment" code so that
// every enchantment available for a tool/weapon has an EQUAL chance (1 in N) of
// being chosen at the Forge/Anvil, instead of the vanilla weighted odds.
// It can also force a deterministic result (same save + same enchant count = same roll)
// when the config asks for reproducible seeds.
namespace BetterForge
{
    /// <summary>
    /// Harmony patches that override how <see cref="BaseEnchantment"/> picks a random
    /// enchantment when enchanting a tool or weapon, giving all candidates a fair,
    /// uniform 1-in-N chance.
    /// </summary>
    public static class EnchantmentPatches
    {
        // The mod's user settings (loaded from config.json). "null!" is a C# trick:
        // it tells the compiler "trust me, this won't be null when used" because
        // Initialize() is guaranteed to run before any patch fires.
        public static ModConfig Config = null!;
        private static IMonitor Monitor = null!;

        /// <summary>
        /// Wires up shared references (config + SMAPI logger) once at startup.
        /// </summary>
        /// <param name="config">The loaded mod settings.</param>
        /// <param name="monitor">SMAPI's logging object used to write to the console/log.</param>
        public static void Initialize(ModConfig config, IMonitor monitor)
        {
            Config = config;
            Monitor = monitor;
        }

        /// <summary>
        /// Registers the Harmony prefix on BaseEnchantment.GetEnchantmentFromItem.
        /// Called once from ModEntry so all patching lives in one place.
        /// </summary>
        /// <param name="harmony">The Harmony instance created by SMAPI for this mod.</param>
        public static void Apply(Harmony harmony)
        {
            // try/catch so a game update that renames the target method only logs an
            // error instead of crashing the whole mod at startup.
            try
            {
                // AccessTools finds a method by reflection at runtime (by declaring
                // type + name + parameter types), so we don't need a hard reference.
                var method = AccessTools.Method(
                    typeof(BaseEnchantment),
                    nameof(BaseEnchantment.GetEnchantmentFromItem),
                    new[] { typeof(Item), typeof(Item) }
                );

                if (method != null)
                {
                    // Patch with a "prefix": our code runs BEFORE the original method.
                    // Returning false from a prefix skips the original entirely.
                    harmony.Patch(
                        original: method,
                        prefix: new HarmonyMethod(typeof(EnchantmentPatches), nameof(BaseEnchantment_GetEnchantmentFromItem_Prefix))
                    );
                    Monitor.Log("Hooked BaseEnchantment.GetEnchantmentFromItem for Uniform Enchantments successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply EnchantmentPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Harmony prefix replacing vanilla's weighted enchantment roll with a fair,
        /// uniform pick among every enchantment the item doesn't already have.
        /// </summary>
        /// <param name="base_item">The tool/weapon being enchanted.</param>
        /// <param name="item">The "catalyst" item consumed by the forge (Prismatic Shard here).</param>
        /// <param name="__result">Harmony magic: lets us set the original method's return value.</param>
        /// <returns>False to skip the original method, true to let it run normally.</returns>
        public static bool BaseEnchantment_GetEnchantmentFromItem_Prefix(Item base_item, Item item, ref BaseEnchantment? __result)
        {
            // Feature disabled in config? Fall through to the vanilla code.
            if (!Config.UniformEnchantmentChances)
                return true;

            // Only react to Prismatic Shards: "(O)74" is its qualified ID ("O" = object
            // category). The ?. null-conditional operator avoids crashes if item is null.
            if (item?.QualifiedItemId != "(O)74" && item?.ItemId != "74")
                return true;

            // `is not Tool` pattern: bail out unless the base item is a tool/weapon.
            if (base_item is not Tool tool)
                return true;

            // Ask the game which enchantments are possible for this specific tool.
            List<BaseEnchantment> available = BaseEnchantment.GetAvailableEnchantmentsForItem(tool);
            if (available == null || available.Count == 0)
                return true;

            // Filter out enchantments already applied on this item to prevent redundant rolls
            List<BaseEnchantment> candidates = new();
            foreach (var candidate in available)
            {
                bool alreadyHas = false;
                foreach (var activeEnch in tool.enchantments)
                {
                    if (activeEnch.GetType() == candidate.GetType())
                    {
                        alreadyHas = true;
                        break;
                    }
                }

                if (!alreadyHas)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                candidates = available;
            }

            // Pick with equal, uniform probability
            // Choose the random source: either the game's shared RNG (truly random each
            // time) or a seeded RNG built from the save's unique ID + how many times the
            // player has ever enchanted — so the result is reproducible for a given save.
            // The `? :` is a ternary: "condition ? valueIfTrue : valueIfFalse".
            Random rng = Config.RandomizeEnchantmentSeed
                ? Game1.random
                : Utility.CreateRandom(Game1.uniqueIDForThisGame, (double)Game1.stats.Get("timesEnchanted") * 777.0);

            // ChooseFrom picks one element with equal probability (true 1-in-N odds).
            __result = rng.ChooseFrom(candidates);
            return false;
        }
    }
}
