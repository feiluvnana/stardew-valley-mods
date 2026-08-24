using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterQOL
{
    /// <summary>
    /// Lookup builder for farm animals, coop/barn happiness, produce, and pets.
    /// </summary>
    /// <remarks>
    /// HOW THIS FILE WORKS (beginner notes):
    /// - "partial class": LookupDataManager is declared across several .cs files (this one plus
    ///   ItemLookupBuilder.cs, NpcLookupBuilder.cs, ...). The compiler glues all parts together
    ///   into one class, keeping a large feature split into readable files.
    /// - The data model nests three levels: one LookupSubject (the whole popup card) holds many
    ///   LookupSection objects (titled groups), which hold many LookupField rows
    ///   (label + value + optional colour highlight).
    /// - ModEntry.I18n.Get("some.key") returns translated text from the mod's language folder,
    ///   so wording can change per language without editing code here.
    /// </remarks>
    public static partial class LookupDataManager
    {
        /// <summary>Thin entry point: forwards to BuildFarmAnimalSubject (kept for call-site clarity).</summary>
        public static LookupSubject BuildAnimalSubject(FarmAnimal animal) => BuildFarmAnimalSubject(animal);
        /// <summary>
        /// Assembles the full farm-animal card: friendship hearts, mood, age, home building,
        /// feeding state, tomorrow's product-quality forecast, and whether produce is ready now.
        /// </summary>
        public static LookupSubject BuildFarmAnimalSubject(FarmAnimal animal)
        {
            AnimalInfo farmAnimalInfo = AnimalHelper.GetFarmAnimalInfo(animal);
            // OBJECT INITIALIZER SYNTAX: "new LookupSubject { Title = ..., Subtitle = ... }" creates
            // the object and assigns properties in one statement (no constructor parameters needed).
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = animal.Name,
                Subtitle = animal.displayType
            };
            // Create the "Status" section; every Fields.Add call below appends one row to it.
            LookupSection lookupSection = new LookupSection(ModEntry.I18n.Get("lookup.section.status"));
            // Null-check the helper result: if data collection failed we still return a valid
            // (empty) card instead of throwing a NullReferenceException (defensive programming).
            if (farmAnimalInfo != null)
            {
                // FRIENDSHIP ROW. String interpolation $"..." embeds values directly in text; the
                // ":0.0" format specifier forces exactly one decimal place (e.g. "3.5"). Animals
                // max out at 5 hearts (1000 friendship points gained from petting/feeding/etc).
                // The crimson Color makes this key statistic pop.
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.friendship"), ModEntry.I18n.Get("lookup.animal.hearts-points-format", new
                {
                    hearts = $"{farmAnimalInfo.Hearts:0.0}",
                    max = "5.0",
                    points = farmAnimalInfo.FriendshipPoints
                }).ToString(), new Color(220, 20, 60)));
                // PETTED-TODAY ROW. The "? :" ternary operator is an inline if/else that PRODUCES a
                // value: condition ? valueIfTrue : valueIfFalse. Here both the displayed word
                // (Yes/No) and the colour (green/red) are chosen by the same boolean test.
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.petted-today"), farmAnimalInfo.WasPetToday ? ModEntry.I18n.Get("lookup.common.yes") : ModEntry.I18n.Get("lookup.common.no"), farmAnimalInfo.WasPetToday ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                // Happiness lives in a NetInt wrapper; ".Value" extracts the plain int (0-255 scale
                // that slowly decays each day unless the animal is kept happy).
                int value = animal.happiness.Value;
                // CHAINED TERNARIES behave like if / else if / else: >=200 Very Happy,
                // else >=100 Happy, otherwise Unhappy. Parentheses group each comparison clearly.
                string value2 = (value >= 200) ? ModEntry.I18n.Get("lookup.animal.mood-very-happy").ToString() : (value >= 100) ? ModEntry.I18n.Get("lookup.animal.mood-happy").ToString() : ModEntry.I18n.Get("lookup.animal.mood-unhappy").ToString();
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.happiness"), $"{value}/255 ({value2})", (value >= 100) ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                // AGE: stored as days since the animal was bought/hatched; young animals must grow
                // up (about a week) before they start producing eggs or milk.
                int value3 = animal.age.Value;
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.age"), ModEntry.I18n.Get("lookup.animal.days-old", new
                {
                    days = value3
                }).ToString(), Color.DarkSlateGray));
                // HOME BUILDING: "animal.home?.buildingType.Value" uses the NULL-CONDITIONAL
                // operator "?" - if 'home' is null the whole expression becomes null instead of
                // crashing. The NULL-COALESCING operator "??" then substitutes the fallback
                // building-type string the animal remembers living in.
                string value4 = animal.home?.buildingType.Value ?? animal.buildingTypeILiveIn.Value;
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.home"), value4, new Color(180, 100, 0)));
                // FED-TODAY ROW. ".Value" again unwraps the networked int; the game treats a
                // fullness of 200+ as "ate today". Same green/red ternary colour trick as above.
                bool flag = animal.fullness.Value >= 200;
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.fed-today"), flag ? ModEntry.I18n.Get("lookup.animal.fed-yes").ToString() : ModEntry.I18n.Get("lookup.animal.fed-no").ToString(), flag ? new Color(0, 140, 0) : new Color(200, 60, 20)));
                // QUALITY FORECAST MATH. The float casts matter! "(float)x / 1000f" keeps DECIMALS,
                // whereas plain integer division would truncate to zero. The formula blends how
                // much the animal loves you (friendship/1000) with its current mood
                // ((happiness+100)/355); chained ternaries then map that score onto Normal /
                // Silver / Gold / Iridium - the same quality tiers items use (iridium = purple best).
                float num = (float)animal.friendshipTowardFarmer.Value / 1000f * ((float)(animal.happiness.Value + 100) / 355f);
                string value5 = (num >= 0.85f) ? ModEntry.I18n.Get("lookup.common.iridium-quality-highest").ToString() : (num >= 0.6f) ? ModEntry.I18n.Get("lookup.common.gold-quality").ToString() : (num >= 0.35f) ? ModEntry.I18n.Get("lookup.common.silver-quality").ToString() : ModEntry.I18n.Get("lookup.common.normal-quality").ToString();
                lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.quality-forecast"), value5, (num >= 0.85f) ? new Color(180, 50, 180) : (num >= 0.6f) ? new Color(180, 100, 0) : Game1.textColor));
                // PRODUCE ROW. "&&" requires BOTH conditions: something IS ready AND its name is
                // not blank. string.IsNullOrEmpty is the standard guard against missing text.
                if (farmAnimalInfo.HasProduceReady && !string.IsNullOrEmpty(farmAnimalInfo.ProduceName))
                {
                    string readyStr = ModEntry.I18n.Get("lookup.animal.ready").ToString();
                    lookupSection.Fields.Add(new LookupField(ModEntry.I18n.Get("lookup.animal.produce"), $"{farmAnimalInfo.ProduceName} ({readyStr})", new Color(0, 140, 0)));
                }
            }
            // Attach the finished section to the card and hand the card back to the caller, which
            // renders it. Note: even when the null-check failed we still return the (empty-ish)
            // card, so the UI degrades gracefully instead of throwing on odd save files.
            lookupSubject.Sections.Add(lookupSection);
            return lookupSubject;
        }

        /// <summary>
        /// Builds the pet card (cat/dog): friendship, petted-today, water-bowl status, and the
        /// max-friendship "loves you" milestone. Mirrors the farm-animal builder above.
        /// </summary>
        public static LookupSubject BuildPetSubject(Pet pet)
        {
            AnimalInfo petInfo = AnimalHelper.GetPetInfo(pet);
            LookupSubject lookupSubject = new LookupSubject
            {
                Title = pet.Name,
                // "??" fallback: some pets have no stored type string, so substitute generic "Pet".
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
                // WATER-BOWL CHECK. 'flag' starts false ("no filled bowl found yet"). The try/catch
                // is purely defensive: if the farm or its building list is momentarily unavailable
                // (e.g. mid-save-load) we survive instead of crashing the lookup popup.
                bool flag = false;
                try
                {
                    Farm farm = Game1.getFarm();
                    if (farm != null)
                    {
                        // Simple linear search: walk every farm building until we spot the pet's bowl.
                        foreach (Building building in farm.buildings)
                        {
                            // C# PATTERN MATCHING - "x is Type name" tests the runtime type AND hands
                            // us a properly-typed variable in one expression. "break" stops the loop
                            // at the first bowl found (no need to scan the rest of the farm).
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
                // MILESTONE ROW. Pets also cap at 1000 friendship points (= 5 hearts). Reaching the
                // cap unlocks a special "loves you" message - a small reward for daily petting.
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
