using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterQOL
{
    /// <summary>
    /// Lookup builder for farm animals, coop/barn happiness, produce, and pets.
    /// </summary>
    public static partial class LookupDataManager
    {
        public static LookupSubject BuildAnimalSubject(FarmAnimal animal) => BuildFarmAnimalSubject(animal);
        public static LookupSubject BuildFarmAnimalSubject(FarmAnimal animal)
        {
            AnimalInfo farmAnimalInfo = AnimalHelper.GetFarmAnimalInfo(animal);
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = animal.Name,
                Subtitle = animal.displayType
            };
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (farmAnimalInfo != null)
            {
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.friendship"), ModEntry.I18n.Get("lookup.animal.hearts-points-format", new
                {
                    hearts = $"{farmAnimalInfo.Hearts:0.0}",
                    max = "5.0",
                    points = farmAnimalInfo.FriendshipPoints
                }).ToString(), new Color(220, 20, 60)));
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.petted-today"), farmAnimalInfo.WasPetToday ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"), farmAnimalInfo.WasPetToday ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                int value = animal.happiness.Value;
                string value2 = (value >= 200) ? ModEntry.I18n.Get("lookup.animal.mood-very-happy").ToString() : (value >= 100) ? ModEntry.I18n.Get("lookup.animal.mood-happy").ToString() : ModEntry.I18n.Get("lookup.animal.mood-unhappy").ToString();
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.happiness"), $"{value}/255 ({value2})", (value >= 100) ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                int value3 = animal.age.Value;
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.age"), ModEntry.I18n.Get("lookup.animal.days-old", new
                {
                    days = value3
                }).ToString(), Color.DarkSlateGray));
                string value4 = animal.home?.buildingType.Value ?? animal.buildingTypeILiveIn.Value;
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.home"), value4, new Color(180, 100, 0)));
                bool flag = animal.fullness.Value >= 200;
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.fed-today"), flag ? ModEntry.I18n.Get("lookup.animal.fed-yes").ToString() : ModEntry.I18n.Get("lookup.animal.fed-no").ToString(), flag ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                float num = (float)animal.friendshipTowardFarmer.Value / 1000f * ((float)(animal.happiness.Value + 100) / 355f);
                string value5 = (num >= 0.85f) ? ModEntry.I18n.Get("lookup.common.iridium-quality-highest").ToString() : (num >= 0.6f) ? ModEntry.I18n.Get("lookup.common.gold-quality").ToString() : (num >= 0.35f) ? ModEntry.I18n.Get("lookup.common.silver-quality").ToString() : ModEntry.I18n.Get("lookup.common.normal-quality").ToString();
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.quality-forecast"), value5, (num >= 0.85f) ? new Color(180, 50, 180) : (num >= 0.6f) ? new Color(180, 100, 0) : Game1.textColor));
                if (farmAnimalInfo.HasProduceReady && !string.IsNullOrEmpty(farmAnimalInfo.ProduceName))
                {
                    string readyStr = ModEntry.I18n.Get("lookup.animal.ready").ToString();
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.produce"), $"{farmAnimalInfo.ProduceName} ({readyStr})", new Color(0, 140, 0)));
                }
            }
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        public static LookupSubject BuildPetSubject(Pet pet)
        {
            AnimalInfo petInfo = AnimalHelper.GetPetInfo(pet);
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = pet.Name,
                Subtitle = pet.petType.Value ?? ModEntry.I18n.Get("hover.type.pet").ToString()
            };
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            if (petInfo != null)
            {
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.friendship"), ModEntry.I18n.Get("lookup.animal.hearts-points-format", new
                {
                    hearts = $"{petInfo.Hearts:0.0}",
                    max = "5.0",
                    points = petInfo.FriendshipPoints
                }).ToString(), new Color(220, 20, 60)));
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.petted-today"), petInfo.WasPetToday ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"), petInfo.WasPetToday ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                bool flag = false;
                try
                {
                    Farm farm = Game1.getFarm();
                    if (farm != null)
                    {
                        foreach (Building building in farm.buildings)
                        {
                            if (building is PetBowl petBowl && petBowl.watered.Value)
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                }
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.pet.water-bowl"), flag ? ModEntry.I18n.Get("lookup.petbowl.water-status-filled").ToString() : ModEntry.I18n.Get("lookup.petbowl.water-status-empty").ToString(), flag ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                if (pet.friendshipTowardFarmer.Value >= 1000)
                {
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.pet.love-milestone"), ModEntry.I18n.Get("lookup.pet.loves-you", new
                    {
                        name = pet.Name
                    }).ToString(), new Color(180, 50, 180)));
                }
            }
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }
    }
}
