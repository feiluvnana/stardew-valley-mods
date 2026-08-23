using System;
using StardewValley;
using StardewValley.Characters;

namespace BetterQOL
{
    public class AnimalInfo
    {
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public bool WasPetToday { get; set; }
        public float Hearts { get; set; }
        public int FriendshipPoints { get; set; }
        public bool HasProduceReady { get; set; }
        public string? ProduceName { get; set; }
        public bool IsPet { get; set; }
    }

    public static class AnimalHelper
    {
        public static AnimalInfo? GetFarmAnimalInfo(FarmAnimal animal)
        {
            if (animal == null)
                return null;

            int friendship = animal.friendshipTowardFarmer.Value;

            var info = new AnimalInfo
            {
                Name = !string.IsNullOrEmpty(animal.displayName) ? animal.displayName : animal.Name,
                TypeName = animal.displayType,
                WasPetToday = animal.wasPet.Value,
                FriendshipPoints = Math.Max(0, friendship),
                Hearts = Math.Clamp(friendship / 200f, 0f, 5f),
                IsPet = false
            };

            if (!string.IsNullOrEmpty(animal.currentProduce.Value) && animal.currentProduce.Value != "0")
            {
                info.HasProduceReady = true;
                var produceData = ItemRegistry.GetData(animal.currentProduce.Value) ?? ItemRegistry.GetData($"(O){animal.currentProduce.Value}");
                info.ProduceName = produceData?.DisplayName;
            }

            return info;
        }

        public static AnimalInfo? GetPetInfo(Pet pet)
        {
            if (pet == null)
                return null;

            int friendship = pet.friendshipTowardFarmer.Value;
            bool wasPet = WasPetToday(pet);

            return new AnimalInfo
            {
                Name = !string.IsNullOrEmpty(pet.displayName) ? pet.displayName : pet.Name,
                TypeName = pet.petType.Value ?? ModEntry.I18n.Get("hover.type.pet"),
                WasPetToday = wasPet,
                FriendshipPoints = Math.Max(0, friendship),
                Hearts = Math.Clamp(friendship / 200f, 0f, 5f),
                IsPet = true
            };
        }

        private static bool WasPetToday(Pet pet)
        {
            if (pet == null)
                return false;

            if (pet.lastPetDay != null && pet.lastPetDay.TryGetValue(Game1.player.UniqueMultiplayerID, out int lastDay))
            {
                return lastDay == Game1.Date.TotalDays;
            }

            return false;
        }
    }
}
