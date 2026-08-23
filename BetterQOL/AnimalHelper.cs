using System;
using StardewValley;
using StardewValley.Characters;

// AnimalHelper translates the game's raw animal data into tooltip-friendly facts:
// who the creature is, its heart level, whether it was petted today, and whether
// produce is waiting to be collected. It only READS game state - nothing here
// modifies animals or pets.
namespace BetterQOL
{
    /// <summary>
    /// Simple data-transfer object holding everything the hover tooltip shows about one
    /// farm animal or pet. Instances are built fresh by AnimalHelper on each lookup.
    /// </summary>
    public class AnimalInfo
    {
        /// <summary>The animal's player-chosen name (what floats above it in-game).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Species label such as "Cow", "Chicken", or a localized generic "Pet".</summary>
        public string TypeName { get; set; } = string.Empty;
        /// <summary>True if already petted today (petting grants friendship once per day).</summary>
        public bool WasPetToday { get; set; }
        /// <summary>Friendship expressed as hearts: 200 points each, capped at the 5-heart UI maximum.</summary>
        public float Hearts { get; set; }
        /// <summary>Raw friendship points (0-1000). Happier animals give higher-quality produce.</summary>
        public int FriendshipPoints { get; set; }
        /// <summary>True when an item is sitting on the animal ready to collect.</summary>
        public bool HasProduceReady { get; set; }
        /// <summary>Localized produce name (e.g. "Large Milk"), or null when nothing is ready.</summary>
        public string? ProduceName { get; set; }
        /// <summary>True when this record describes a pet (cat/dog) rather than livestock.</summary>
        public bool IsPet { get; set; }
    }

    /// <summary>
    /// Static utility class - "static" means it can't be instantiated; you call its
    /// methods directly on the class name, e.g. AnimalHelper.GetFarmAnimalInfo(...).
    /// Converts FarmAnimal/Pet game objects into AnimalInfo records for tooltips.
    /// </summary>
    public static class AnimalHelper
    {
        /// <summary>
        /// Collects display facts about a coop/barn animal (cow, chicken, duck, etc.).
        /// </summary>
        /// <param name="animal">The animal being hovered over.</param>
        /// <returns>A populated AnimalInfo, or null when the input was null.</returns>
        public static AnimalInfo? GetFarmAnimalInfo(FarmAnimal animal)
        {
            if (animal == null)
                return null;

            // Stardew stores synchronized multiplayer data in "NetFields"; you always
            // read or write the actual value through their ".Value" wrapper.
            int friendship = animal.friendshipTowardFarmer.Value;

            // Object initializer syntax: creates the object and sets the listed
            // properties in one statement.
            var info = new AnimalInfo
            {
                // Ternary operator (condition ? a : b): prefer the nickname, falling
                // back to the internal Name when the display name is blank.
                Name = !string.IsNullOrEmpty(animal.displayName) ? animal.displayName : animal.Name,
                TypeName = animal.displayType,
                WasPetToday = animal.wasPet.Value,
                // Guard against negative values (shouldn't occur, but cheap insurance).
                FriendshipPoints = Math.Max(0, friendship),
                // 200 friendship points equal 1 heart; Clamp keeps the result inside
                // 0-5 to match the game's five-heart animal UI.
                Hearts = Math.Clamp(friendship / 200f, 0f, 5f),
                IsPet = false
            };

            // currentProduce holds the item id of whatever is ready; legacy "0" means none.
            if (!string.IsNullOrEmpty(animal.currentProduce.Value) && animal.currentProduce.Value != "0")
            {
                info.HasProduceReady = true;
                // Look up the item's data for its display name. Item ids normally carry a
                // type prefix like "(O)" for objects; "??" retries the bare id because
                // some mods store ids without the prefix.
                var produceData = ItemRegistry.GetData(animal.currentProduce.Value) ?? ItemRegistry.GetData($"(O){animal.currentProduce.Value}");
                // "?." yields null instead of crashing if the lookup failed.
                info.ProduceName = produceData?.DisplayName;
            }

            return info;
        }

        /// <summary>
        /// Collects display facts about a pet (cat or dog). Pets use the same
        /// 200-points-per-heart scale as livestock.
        /// </summary>
        /// <param name="pet">The pet being hovered over.</param>
        /// <returns>A populated AnimalInfo flagged IsPet, or null when the input was null.</returns>
        public static AnimalInfo? GetPetInfo(Pet pet)
        {
            if (pet == null)
                return null;

            int friendship = pet.friendshipTowardFarmer.Value;
            bool wasPet = WasPetToday(pet);

            return new AnimalInfo
            {
                Name = !string.IsNullOrEmpty(pet.displayName) ? pet.displayName : pet.Name,
                // "??" supplies a fallback value when the left side is null: some pets
                // have no type string, so we show a translated "Pet" label instead.
                // ModEntry.I18n.Get reads translations from the mod's i18n folder.
                TypeName = pet.petType.Value ?? ModEntry.I18n.Get("hover.type.pet"),
                WasPetToday = wasPet,
                FriendshipPoints = Math.Max(0, friendship),
                Hearts = Math.Clamp(friendship / 200f, 0f, 5f),
                IsPet = true
            };
        }

        /// <summary>
        /// Checks whether THIS player petted the animal today; each farmer's petting is
        /// tracked separately in multiplayer.
        /// </summary>
        /// <param name="pet">The pet/animal to inspect.</param>
        /// <returns>True if the current player already petted it today.</returns>
        private static bool WasPetToday(Pet pet)
        {
            if (pet == null)
                return false;

            // lastPetDay is a Dictionary mapping each player's UniqueMultiplayerID to the
            // day number they last petted. TryGetValue returns true and hands the value
            // back through "out int lastDay" only when that key exists.
            if (pet.lastPetDay != null && pet.lastPetDay.TryGetValue(Game1.player.UniqueMultiplayerID, out int lastDay))
            {
                // Game1.Date.TotalDays counts in-game days since the save began, so an
                // exact match means "petted today".
                return lastDay == Game1.Date.TotalDays;
            }

            return false;
        }
    }
}
