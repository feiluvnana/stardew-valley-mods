using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;
using StardewValley.Tools;

// MenuTooltipPatch appends BetterQOL's extra info (sell price, needed Community Center
// bundles, Museum donation status) to the description text the game already builds for
// items shown inside menus (inventory, chests, shops...). It uses the HARMONY library
// to attach one shared "postfix" callback to getDescription() across every concrete
// item class at startup, instead of editing each call site.
namespace BetterQOL
{
    /// <summary>
    /// Installs and hosts the Harmony postfix that enriches vanilla item descriptions
    /// with QOL extras, formatted exactly like native tooltip text.
    /// </summary>
    public static class MenuTooltipPatch
    {
        /// <summary>
        /// Applies the description postfix to every concrete item type we care about.
        /// </summary>
        /// <param name="harmony">Shared Harmony instance created by ModEntry.</param>
        /// <param name="monitor">SMAPI logger used for diagnostics.</param>
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            // HarmonyMethod packages our static callback for Harmony; 'postfix:' means
            // it runs AFTER the original getDescription rather than replacing it.
            var postfix = new HarmonyMethod(typeof(MenuTooltipPatch), nameof(DescriptionPostfix));

            // typeof(SomeClass) grabs that class's runtime type object; Harmony needs these
            // to locate the methods to patch.
            // Only patch concrete item classes (never abstract classes like Item or Tool)
            Type[] concreteTypes = new[]
            {
                typeof(StardewValley.Object),
                typeof(Ring),
                typeof(Clothing),
                typeof(Hat),
                typeof(Boots),
                typeof(Furniture),
                typeof(Trinket),
                typeof(MeleeWeapon),
                typeof(Slingshot),
                typeof(Pickaxe),
                typeof(Axe),
                typeof(Hoe),
                typeof(WateringCan),
                typeof(FishingRod),
                typeof(Pan),
                typeof(Shears),
                typeof(MilkPail)
            };

            int patchedCount = 0;
            foreach (var type in concreteTypes)
            {
                try
                {
                    // AccessTools reflects into the GAME's assembly: find the class's OWN
                    // getDescription() taking zero arguments (Type.EmptyTypes). Null if the
                    // class only inherits the base implementation without redeclaring it.
                    MethodInfo? method = AccessTools.DeclaredMethod(type, "getDescription", Type.EmptyTypes);
                    // Skip abstract declarations - Harmony cannot hook a method with no body.
                    if (method != null && !method.IsAbstract)
                    {
                        // Attach our postfix to this exact method implementation.
                        harmony.Patch(method, postfix: postfix);
                        patchedCount++;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Could not patch getDescription on {type.Name}: {ex.Message}", LogLevel.Trace);
                }
            }

            monitor.Log($"Successfully applied Harmony description patches to {patchedCount} item classes for native in-menu tooltips.", LogLevel.Debug);
        }

        /// <summary>
        /// Runs after each patched getDescription() call and appends our extra lines to its result.
        /// </summary>
        /// <param name="__instance">Harmony-injected: the Item the method was called on ("?" = may be null).</param>
        /// <param name="__result">Harmony-injected return value; 'ref' lets us MODIFY what callers receive.</param>
        public static void DescriptionPostfix(Item? __instance, ref string __result)
        {
            // Nothing to decorate when there's no item, or when the world is not loaded.
            if (__instance == null || !Context.IsWorldReady)
                return;

            string extra = BuildItemExtraText(__instance);
            if (!string.IsNullOrEmpty(extra))
            {
                if (string.IsNullOrEmpty(__result))
                {
                    __result = extra;
                }
                // Contains() guard avoids duplicating our block if the game asks twice.
                else if (!__result.Contains(extra))
                {
                    // "\n\n" leaves a blank line between vanilla text and our additions.
                    __result = __result + "\n\n" + extra;
                }
            }
        }

        /// <summary>
        /// Assembles the extra tooltip lines for one item according to current settings.
        /// </summary>
        /// <param name="item">The item being displayed in a menu.</param>
        /// <returns>Lines joined by "\n"; empty string when no feature applies.</returns>
        public static string BuildItemExtraText(Item item)
        {
            var lines = new List<string>();
            // ModEntry.Config is a static shortcut to the live settings object.
            var config = ModEntry.Config;

            // 1. Community Center Bundles
            if (config.ShowBundleNeedOnHover)
            {
                var bundles = GetNeededBundles(item);
                if (bundles.Count == 1)
                {
                    string singleText = ModEntry.I18n.Get("hover.item.bundle-needed-single", new { bundle = bundles[0] }).ToString();
                    lines.Add(Game1.parseText(singleText, Game1.smallFont, 200));
                }
                else if (bundles.Count > 1)
                {
                    lines.Add(ModEntry.I18n.Get("hover.item.bundle-needed-header").ToString());
                    foreach (var bundle in bundles)
                    {
                        string entryText = ModEntry.I18n.Get("hover.item.bundle-needed-entry", new { bundle }).ToString();
                        lines.Add(Game1.parseText(entryText, Game1.smallFont, 200));
                    }
                }
            }

            // 2. Museum Donation
            if (config.ShowMuseumNeedOnHover)
            {
                // Museum pieces are artifacts ("Arch" type) and minerals; mineralsCategory is a
                // legacy negative-number category constant still used for matching.
                bool isMuseumItem = (item is StardewValley.Object obj && (obj.Type == "Arch" || obj.Type == "Minerals"))
                                 || item.Category == StardewValley.Object.mineralsCategory;
                if (isMuseumItem)
                {
                    // LINQ .Any() = "does at least one donated piece match?" (stops at first hit).
                    // Four id formats are compared because older saves recorded donations differently;
                    // $"(O){item.ItemId}" is string interpolation building the qualified "(O)123" form.
                    bool isDonated = Game1.netWorldState.Value.MuseumPieces.Values.Any(v =>
                        v == item.ItemId ||
                        v == item.QualifiedItemId ||
                        (item is StardewValley.Object sObj && v == sObj.ParentSheetIndex.ToString()) ||
                        v == $"(O){item.ItemId}"
                    );

                    if (!isDonated)
                    {
                        string neededText = ModEntry.I18n.Get("hover.item.museum-needed").ToString();
                        lines.Add(Game1.parseText(neededText, Game1.smallFont, 200));
                    }
                    else
                    {
                        string donatedText = ModEntry.I18n.Get("hover.item.museum-donated").ToString();
                        lines.Add(Game1.parseText(donatedText, Game1.smallFont, 200));
                    }
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Scans raw Data/Bundles entries to find unfinished bundles that would accept this item.
        /// </summary>
        /// <param name="item">Candidate ingredient.</param>
        /// <returns>Friendly names of matching unfinished bundles (empty when none / CC finished).</returns>
        public static List<string> GetNeededBundles(Item item)
        {
            var results = new List<string>();
            try
            {
                if (item == null)
                    return results;

                // Bundle requirements are only standard Objects (not BigCraftables, Tools, Weapons, Hats, Furniture, etc.)
                if (item is not StardewValley.Object sObj || sObj.bigCraftable.Value)
                    return results;

                bool isCCComplete = Game1.player.hasCompletedCommunityCenter();
                bool isJojaMember = Game1.MasterPlayer.mailReceived.Contains("JojaMember");
                bool isMovieTheaterBuilt = Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheater") ||
                                           Utility.doesMasterPlayerHaveMailReceivedButNotMailForTomorrow("ccMovieTheaterJoja");

                // If Joja route was chosen, standard CC bundles are closed, and Joja uses gold purchase for Movie Theater (no bundles).
                if (isJojaMember)
                    return results;

                // If Movie Theater is already built, even The Missing Bundle is complete.
                if (isMovieTheaterBuilt)
                    return results;

                var bundleData = DataLoader.Bundles(Game1.content);
                if (bundleData == null || Game1.netWorldState.Value.Bundles == null)
                    return results;

                Dictionary<string, string>? bundleNamesDict = null;
                try
                {
                    bundleNamesDict = Game1.content.Load<Dictionary<string, string>>("Strings\\BundleNames");
                }
                catch { }

                foreach (var kvp in bundleData)
                {
                    string bundleKey = kvp.Key; // e.g. "Pantry/0" or "Abandoned Joja Mart/36"
                    string[] keyParts = bundleKey.Split('/');
                    if (keyParts.Length < 2 || !int.TryParse(keyParts[1], out int bundleId))
                        continue;

                    string areaName = keyParts[0];

                    // Skip Vault bundles because they are pure gold donations directly in the CC interface
                    if (areaName.Equals("Vault", StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool isMissingBundle = areaName.Equals("Abandoned Joja Mart", StringComparison.OrdinalIgnoreCase) || bundleId == 36;

                    // If Community Center is complete, ONLY The Missing Bundle (Abandoned JojaMart) can be active
                    if (isCCComplete && !isMissingBundle)
                        continue;

                    // If Community Center is NOT complete, The Missing Bundle is not active yet
                    if (!isCCComplete && isMissingBundle)
                        continue;

                    string bundleValue = kvp.Value;
                    string[] parts = bundleValue.Split('/');
                    if (parts.Length < 3)
                        continue;

                    // Resolve localized bundle name: index 6 (Vietnamese/xnb), index 5 (vanilla display name), or Strings/BundleNames lookup
                    string bundleName = parts[0];
                    if (parts.Length >= 7 && !string.IsNullOrWhiteSpace(parts[6]))
                    {
                        bundleName = parts[6].Trim();
                    }
                    else if (parts.Length >= 6 && !string.IsNullOrWhiteSpace(parts[5]))
                    {
                        bundleName = parts[5].Trim();
                    }
                    else if (bundleNamesDict != null && bundleNamesDict.TryGetValue(parts[0], out string? locName) && !string.IsNullOrWhiteSpace(locName))
                    {
                        bundleName = locName.Trim();
                    }

                    string[] reqParts = parts[2].Split(' ');

                    if (Game1.netWorldState.Value.Bundles.TryGetValue(bundleId, out bool[] ingredientSlots))
                    {
                        int itemsRequired = parts.Length > 4 && int.TryParse(parts[4], out int req) ? req : ingredientSlots.Length;
                        int filledCount = ingredientSlots.Count(b => b);
                        if (filledCount >= itemsRequired)
                            continue; // Bundle already finished

                        for (int k = 0; k < ingredientSlots.Length; k++)
                        {
                            if (!ingredientSlots[k]) // Slot not filled yet
                            {
                                int reqIndex = k * 3;
                                if (reqIndex + 2 >= reqParts.Length)
                                    break;

                                string reqId = reqParts[reqIndex];
                                int reqMinQuality = int.TryParse(reqParts[reqIndex + 2], out int q) ? q : 0;

                                if (IsBundleIngredientMatch(item, reqId, reqMinQuality))
                                {
                                    if (!results.Contains(bundleName))
                                    {
                                        results.Add(bundleName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return results;
        }

        /// <summary>
        /// Accurately checks whether a candidate item satisfies a bundle ingredient requirement.
        /// Strict matching against item quality, qualified item ID, object category, and SDV 1.6 exceptions.
        /// </summary>
        public static bool IsBundleIngredientMatch(Item item, string reqId, int reqMinQuality)
        {
            if (item == null || string.IsNullOrWhiteSpace(reqId))
                return false;

            // Only standard Stardew Valley Objects can be bundle ingredients
            if (item is not StardewValley.Object obj || obj.bigCraftable.Value)
                return false;

            // 1. Minimum quality requirement check
            if (item.Quality < reqMinQuality)
                return false;

            // 2. Category matching (negative integer ID, e.g. -4 = Fish, -5 = Egg, -6 = Milk, etc.)
            if (int.TryParse(reqId, out int catId) && catId < 0)
            {
                // Vanilla special exception: Dinosaur Egg ((O)107, category -2) counts as Egg (category -5)
                if (catId == -5 && (item.QualifiedItemId == "(O)107" || item.ItemId == "107"))
                    return true;

                // River Jelly, Sea Jelly, Cave Jelly share FishCategory (-4) but cannot be deposited in fish bundles
                if (catId == StardewValley.Object.FishCategory)
                {
                    if (item.ItemId is "RiverJelly" or "SeaJelly" or "CaveJelly" ||
                        item.QualifiedItemId is "(O)RiverJelly" or "(O)SeaJelly" or "(O)CaveJelly")
                        return false;
                }

                if (item.Category == catId)
                    return true;

                return false;
            }

            // 3. Exact ItemId or QualifiedItemId match (or ItemRegistry lookup)
            if (ItemRegistry.HasItemId(item, reqId))
                return true;

            if (reqId == item.ItemId || reqId == item.QualifiedItemId || "(O)" + reqId == item.QualifiedItemId)
                return true;

            // 4. Object ParentSheetIndex match if positive integer
            if (obj.ParentSheetIndex > 0 && reqId == obj.ParentSheetIndex.ToString())
                return true;

            return false;
        }
    }
}
