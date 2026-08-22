using System;
using System.Reflection;
using Netcode;
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

            int friendship = GetAnimalFriendship(animal);

            var info = new AnimalInfo
            {
                Name = animal.Name,
                TypeName = animal.displayType,
                WasPetToday = animal.wasPet.Value,
                FriendshipPoints = friendship,
                Hearts = Math.Min(5f, friendship / 200f),
                IsPet = false
            };

            if (animal.currentProduce.Value != null && CanAnimalBeHarvested(animal))
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

            int friendship = GetPetFriendship(pet);
            bool wasPet = WasPetToday(pet);

            return new AnimalInfo
            {
                Name = pet.Name,
                TypeName = pet.petType.Value ?? ModEntry.I18n.Get("hover.type.pet"),
                WasPetToday = wasPet,
                FriendshipPoints = friendship,
                Hearts = Math.Min(5f, friendship / 200f),
                IsPet = true
            };
        }

        private static int GetAnimalFriendship(FarmAnimal animal)
        {
            var field = typeof(FarmAnimal).GetField("friendshipData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? typeof(FarmAnimal).GetField("friendshipPoints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? typeof(FarmAnimal).GetField("friendship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field?.GetValue(animal) is NetInt netInt)
                return netInt.Value;
            if (field?.GetValue(animal) is int val)
                return val;

            var prop = typeof(FarmAnimal).GetProperty("friendshipData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(FarmAnimal).GetProperty("friendshipPoints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(FarmAnimal).GetProperty("friendship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(FarmAnimal).GetProperty("Friendship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (prop?.GetValue(animal) is NetInt netIntProp)
                return netIntProp.Value;
            if (prop?.GetValue(animal) is int propVal)
                return propVal;

            return 0;
        }

        private static bool CanAnimalBeHarvested(FarmAnimal animal)
        {
            var readyObj = typeof(FarmAnimal).GetField("readyForHarvest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(animal)
                        ?? typeof(FarmAnimal).GetProperty("readyForHarvest", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(animal);
            if (readyObj is NetBool readyNetBool)
                return readyNetBool.Value;

            var method = typeof(FarmAnimal).GetMethod("isHarvestable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                      ?? typeof(FarmAnimal).GetMethod("canBeHarvested", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null && method.Invoke(animal, null) is bool res)
                return res;

            return animal.currentProduce.Value != null;
        }

        private static int GetPetFriendship(Pet pet)
        {
            var field = typeof(Pet).GetField("friendshipPoints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? typeof(Pet).GetField("friendship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? typeof(Pet).GetField("friendshipData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field?.GetValue(pet) is NetInt netInt)
                return netInt.Value;
            if (field?.GetValue(pet) is int val)
                return val;

            var prop = typeof(Pet).GetProperty("friendshipPoints", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(Pet).GetProperty("friendship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(Pet).GetProperty("friendshipData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(Pet).GetProperty("Friendship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (prop?.GetValue(pet) is NetInt netIntProp)
                return netIntProp.Value;
            if (prop?.GetValue(pet) is int propVal)
                return propVal;

            return 0;
        }

        private static bool WasPetToday(Pet pet)
        {
            var wasPetField = typeof(Pet).GetField("wasPet", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                           ?? typeof(Pet).GetField("grantedPermit", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (wasPetField?.GetValue(pet) is NetBool wasPetNet)
                return wasPetNet.Value;

            var lastPetDayField = typeof(Pet).GetField("lastPetDay", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (lastPetDayField?.GetValue(pet) is System.Collections.IDictionary dict)
            {
                return dict.Contains(Game1.player.UniqueMultiplayerID);
            }

            return false;
        }
    }
}
