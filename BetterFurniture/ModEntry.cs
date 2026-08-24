// ============================================================================
// BetterFurniture — mod entry point
// ----------------------------------------------------------------------------
// A "namespace" groups related classes under one name; every file in this mod
// uses "namespace BetterFurniture" so they can see each other easily.
//
// The lines below are "using directives". They import types from other
// libraries so we can write short names like "IMonitor" instead of full paths
// like "StardewModdingAPI.IMonitor". What each one brings in:
//   HarmonyLib                       -> Harmony, the library that modifies
//                                       ("patches") game methods while the
//                                       game is running.
//   Microsoft.Xna.Framework          -> MonoGame math types (Vector2, Rectangle).
//   Microsoft.Xna.Framework.Graphics -> GPU image types such as Texture2D.
//   StardewModdingAPI                -> SMAPI itself: logging, events, helpers.
//   StardewModdingAPI.Events         -> Event argument types like
//                                       AssetRequestedEventArgs.
//   StardewValley.GameData.Shops     -> game data models describing shops.
// ============================================================================
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Shops;

namespace BetterFurniture
{
    /// <summary>
    /// The mod's entry point. SMAPI finds any class inheriting from its Mod base
    /// class, creates it, and calls its Entry() method once when the game boots.
    ///
    /// C# concept — inheritance: "class ModEntry : Mod" means ModEntry EXTENDS
    /// SMAPI's Mod class. We inherit useful members (Monitor, Helper,
    /// ModManifest) and use "override" below to supply our own version of the
    /// virtual (deliberately replaceable) Entry method.
    /// </summary>
    public class ModEntry : Mod
    {
        /// <summary>
        /// SMAPI's logger, shared statically so the patch classes (BedPatches,
        /// FurniturePatches) can also write messages to the SMAPI console.
        ///
        /// C# concepts:
        ///   "static"              -> belongs to the class itself, not to one object.
        ///   { get; private set; } -> auto-property anyone may READ, but only this
        ///                            class may ASSIGN it.
        ///   "= null!"             -> starts as null; the "!" tells the compiler
        ///                            "trust me, Entry() fills it in before use".
        /// </summary>
        public static IMonitor ModMonitor { get; private set; } = null!;
        /// <summary>SMAPI's helper toolkit (events, asset editing, content loading),
        /// shared statically for the same reason as <see cref="ModMonitor"/>.</summary>
        public static IModHelper ModHelper { get; private set; } = null!;
        /// <summary>Gives access to translation files in the i18n folder so item
        /// names show up in the player's selected language.</summary>
        public static ITranslationHelper I18n { get; private set; } = null!;

        /// <summary>
        /// Runs once when the game starts — a mod's setup routine.
        /// "override" supplies our implementation of the virtual Entry method
        /// inherited from SMAPI's Mod base class.
        /// </summary>
        /// <param name="helper">SMAPI's toolbox: events, asset editing, translations.</param>
        public override void Entry(IModHelper helper)
        {
            // Stash SMAPI-provided objects into our static properties so every
            // class in the mod can reach them. (Monitor comes from the base Mod
            // class; helper is passed in by SMAPI.)
            ModMonitor = Monitor;
            ModHelper = helper;
            I18n = helper.Translation;

            // Create THIS mod's Harmony instance. Harmony rewrites game methods in
            // memory at runtime; our manifest's unique ID tags patches as ours.
            var harmony = new Harmony(ModManifest.UniqueID);
            // Register all bed-related and furniture-related method patches.
            BedPatches.Apply(harmony, Monitor);
            FurniturePatches.Apply(harmony, Monitor);

            // C# concept — events: "+=" SUBSCRIBES our method to SMAPI's event.
            // From now on, whenever the game asks SMAPI for an asset (image, data
            // file...), SMAPI raises AssetRequested and our OnAssetRequested runs,
            // letting us load or edit the asset before the game ever sees it.
            helper.Events.Content.AssetRequested += OnAssetRequested;
            // "(s, e) => ..." is a LAMBDA: a tiny unnamed method written inline
            // right where it's used. This one runs when the player switches the
            // game language, discarding cached furniture/shop data so it reloads
            // with freshly translated text.
            helper.Events.Content.LocaleChanged += (s, e) =>
            {
                helper.GameContent.InvalidateCache("Data/Furniture");
                helper.GameContent.InvalidateCache("Data/Shops");
            };
            // After a save loads and again each morning, re-apply our furniture
            // type fixes to items in inventories and placed around the world.
            helper.Events.GameLoop.SaveLoaded += (s, e) => FurniturePatches.FixAllLocationAndInventoryFurniture();
            helper.Events.GameLoop.DayStarted += (s, e) => FurniturePatches.FixAllLocationAndInventoryFurniture();

            // Write a startup message to the SMAPI console (Debug level only shows
            // when verbose logging is enabled).
            Monitor.Log("BetterFurniture initialized with 4x4 bed enhancements, floor restorations, wall decor patches, and Pierre shop integration.", LogLevel.Debug);
        }

        /// <summary>
        /// SMAPI fires this event the FIRST time the game needs each asset, giving
        /// mods one chance to LOAD an asset entirely themselves or EDIT the game's
        /// built-in version. Everything this mod adds or reskins lives here.
        /// </summary>
        /// <param name="sender">Who raised the event (SMAPI); unused here — "?" means it may be null.</param>
        /// <param name="e">Which asset is being requested, plus LoadFrom/Edit helpers.</param>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Custom texture loads -------------------------------------------------
            // Map each custom asset NAME (what the game content system requests) to
            // a PNG file path inside this mod folder. A Dictionary is a key/value
            // lookup table; the comparer makes lookups case-insensitive so
            // capitalization never causes a mismatch.
            var textures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mods/feiluvnana.BetterFurniture/PrincessDoubleBed"] = "assets/princess_double_bed.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessPastelWindow"] = "assets/princess_pastel_window.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessWallSconce"] = "assets/princess_wall_sconce.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessNightstand"] = "assets/princess_nightstand.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessGrandRug"] = "assets/princess_grand_rug.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessRococoMirror"] = "assets/princess_rococo_mirror.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessBedCanopy"] = "assets/princess_bed_canopy.png"
            };

            // Walk through every key/value pair ("kvp") in the dictionary above.
            foreach (var kvp in textures)
            {
                // IsEquivalentTo compares asset IDs while ignoring the language
                // suffix (e.g. ".fr-FR"), so one check covers every locale.
                if (e.NameWithoutLocale.IsEquivalentTo(kvp.Key))
                {
                    // "<Texture2D>" is a GENERIC type argument: we tell SMAPI which
                    // kind of asset to build (a GPU texture/image). Medium priority
                    // lets other content packs still override ours if they wish.
                    e.LoadFromModFile<Texture2D>(kvp.Value, AssetLoadPriority.Medium);
                    return; // Done — no need to test the remaining keys.
                }
            }

            // Map image edits ------------------------------------------------------
            // Maps are painted using TILESETS ("tilesheets"): large images made of
            // 16x16 pixel tiles that map files reuse everywhere. Editing the sheet
            // instantly reskins every tile referencing it — no map edits needed.
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/farmhouse_tiles"))
            {
                e.Edit(asset =>
                {
                    // Treat the requested asset as an editable image.
                    var editor = asset.AsImage();
                    // Read our replacement kitchen-tile artwork from the mod folder.
                    var overlay = Helper.ModContent.Load<Texture2D>("assets/white_kitchen_tiles.png");
                    // Stamp it over the original sheet; Overlay mode keeps any
                    // pixels our PNG leaves transparent untouched.
                    editor.PatchImage(overlay, patchMode: PatchMode.Overlay);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/walls_and_floors"))
            {
                e.Edit(asset =>
                {
                    // walls_and_floors holds every wallpaper & floor pattern in the
                    // game. Each PatchImage call copies pixels FROM a source
                    // rectangle TO a target rectangle, both measured in pixels on
                    // their sheets (one tile = 16x16 px). These four calls swap
                    // specific wallpaper/floor slots for prettier custom versions.
                    var editor = asset.AsImage();
                    var princessWall = Helper.ModContent.Load<Texture2D>("assets/princess_wallpaper.png");
                    editor.PatchImage(princessWall, targetArea: new Rectangle(176, 0, 16, 48), sourceArea: new Rectangle(0, 0, 16, 48));

                    var creamWall = Helper.ModContent.Load<Texture2D>("assets/warm_cream_wallpaper.png");
                    editor.PatchImage(creamWall, targetArea: new Rectangle(176, 192, 16, 48), sourceArea: new Rectangle(0, 0, 16, 48));

                    var pastelKitchenFloor = Helper.ModContent.Load<Texture2D>("assets/pastel_kitchen_floor.png");
                    editor.PatchImage(pastelKitchenFloor, targetArea: new Rectangle(192, 400, 32, 32), sourceArea: new Rectangle(0, 0, 32, 32));

                    var kidFloor = Helper.ModContent.Load<Texture2D>("assets/kid_floor.png");
                    editor.PatchImage(kidFloor, targetArea: new Rectangle(0, 336, 32, 32), sourceArea: new Rectangle(0, 0, 32, 32));
                });
            }
            // Data/Furniture edits ---------------------------------------------------
            // Data/Furniture is a game dictionary defining every furniture item:
            // its name, category, tile size, price, texture, and so on. Each value
            // is one long string whose fields are separated by "/" characters
            // (full field spec: wiki page "Modding:Furniture data").
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture"))
            {
                e.Edit(asset =>
                {
                    // Grab the raw dictionary so we can read AND overwrite entries.
                    var data = asset.AsDictionary<string, string>().Data;

                    // Look up localized display names from our i18n translation
                    // files so items are titled correctly in every language.
                    string bedName = I18n.Get("furniture.princess_double_bed.name");
                    string windowName = I18n.Get("furniture.princess_pastel_window.name");
                    string sconceName = I18n.Get("furniture.princess_wall_sconce.name");
                    string nightstandName = I18n.Get("furniture.princess_nightstand.name");
                    string rugName = I18n.Get("furniture.princess_grand_rug.name");
                    string mirrorName = I18n.Get("furniture.princess_rococo_mirror.name");
                    string canopyName = I18n.Get("furniture.princess_bed_canopy.name");

                    // "$" before a quote marks STRING INTERPOLATION: each {bedName}
                    // style placeholder is replaced at runtime with that value.
                    // Key fields in these rows: internal Name / Type ("bed double",
                    // "lamp", "rug"...) / texture size "W H" / footprint "W H"
                    // (that's what makes the bed 4x4 tiles!) / price / edibility
                    // (-1 = can't be eaten) / display name / icon index / texture
                    // path under the mod folder / table flag.
                    data["feiluvnana.BetterFurniture.PrincessDoubleBed"] = $"Princess Double Bed/bed double/4 4/4 4/1/10000/-1/{bedName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessDoubleBed/false";
                    data["feiluvnana.BetterFurniture.PrincessPastelWindow"] = $"Princess Pastel Window/window/4 2/4 2/1/2000/-1/{windowName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessPastelWindow/false";
                    data["feiluvnana.BetterFurniture.PrincessWallSconce"] = $"Princess Wall Sconce/painting/1 2/1 2/1/1000/-1/{sconceName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessWallSconce/false";
                    data["feiluvnana.BetterFurniture.PrincessNightstand"] = $"Princess Nightstand/lamp/1 2/1 1/1/2000/-1/{nightstandName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessNightstand/false";
                    data["feiluvnana.BetterFurniture.PrincessGrandRug"] = $"Princess Grand Rug/rug/4 3/4 3/1/3000/-1/{rugName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessGrandRug/false";
                    data["feiluvnana.BetterFurniture.PrincessRococoMirror"] = $"Princess Rococo Mirror/painting/2 2/2 2/1/2500/-1/{mirrorName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessRococoMirror/false";
                    data["feiluvnana.BetterFurniture.PrincessBedCanopy"] = $"Princess Bed Canopy/painting/4 3/4 3/1/3500/-1/{canopyName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessBedCanopy/false";
                });
            }
            // Data/Shops edits (SeedShop / Pierre's General Store) -------------------
            // Data/Shops defines every store in the game: which items it stocks
            // and at what price. Here we inject our furniture into Pierre's shop.
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(asset =>
                {
                    // This time each value is a parsed ShopData OBJECT rather than
                    // a raw string, so we can edit its item list directly.
                    var data = asset.AsDictionary<string, ShopData>().Data;
                    // "TryGetValue(..., out var seedShop)" = a safe lookup: instead
                    // of crashing on a missing key it returns false, and "out"
                    // outputs the found value into the seedShop variable.
                    if (data.TryGetValue("SeedShop", out var seedShop))
                    {
                        // List<string> = an ordered, growable collection; the brace
                        // syntax below is a "collection initializer" that fills it.
                        var itemsToAdd = new List<string>
                        {
                            "feiluvnana.BetterFurniture.PrincessDoubleBed",
                            "feiluvnana.BetterFurniture.PrincessPastelWindow",
                            "feiluvnana.BetterFurniture.PrincessWallSconce",
                            "feiluvnana.BetterFurniture.PrincessNightstand",
                            "feiluvnana.BetterFurniture.PrincessGrandRug",
                            "feiluvnana.BetterFurniture.PrincessRococoMirror",
                            "feiluvnana.BetterFurniture.PrincessBedCanopy"
                        };

                        foreach (var itemId in itemsToAdd)
                        {
                            // Remove any existing entry first so this edit can run
                            // twice without creating duplicates. ("i => i.Id == itemId"
                            // is a lambda used as a predicate: true = remove it.)
                            seedShop.Items.RemoveAll(i => i.Id == itemId);
                            // Add the item as purchasable stock, using "object
                            // initializer" syntax (the { } block sets properties
                            // right after constructing the object):
                            //   Id             -> unique ID for this shop listing.
                            //   ItemId         -> which game item; "(F)" is the
                            //                     furniture item qualifier code.
                            //   Price          -> 0 gold — free!
                            //   AvailableStock -> -1 means infinitely in stock.
                            seedShop.Items.Add(new ShopItemData
                            {
                                Id = itemId,
                                ItemId = $"(F){itemId}",
                                Price = 0,
                                AvailableStock = -1
                            });
                        }
                    }
                });
            }
        }
    }
}
