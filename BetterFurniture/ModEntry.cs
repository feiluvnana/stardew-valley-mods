using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Shops;

namespace BetterFurniture
{
    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static IModHelper ModHelper { get; private set; } = null!;
        public static ITranslationHelper I18n { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            ModHelper = helper;
            I18n = helper.Translation;

            var harmony = new Harmony(ModManifest.UniqueID);
            BedPatches.Apply(harmony, Monitor);
            FurniturePatches.Apply(harmony, Monitor);

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.LocaleChanged += (s, e) =>
            {
                helper.GameContent.InvalidateCache("Data/Furniture");
                helper.GameContent.InvalidateCache("Data/Shops");
            };
            helper.Events.GameLoop.SaveLoaded += (s, e) => FurniturePatches.FixAllLocationAndInventoryFurniture();
            helper.Events.GameLoop.DayStarted += (s, e) => FurniturePatches.FixAllLocationAndInventoryFurniture();

            Monitor.Log("BetterFurniture initialized with 4x4 bed enhancements, floor restorations, wall decor patches, and Pierre shop integration.", LogLevel.Debug);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Custom texture loads
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

            foreach (var kvp in textures)
            {
                if (e.NameWithoutLocale.IsEquivalentTo(kvp.Key))
                {
                    e.LoadFromModFile<Texture2D>(kvp.Value, AssetLoadPriority.Medium);
                    return;
                }
            }

            // Map image edits
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/farmhouse_tiles"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsImage();
                    var overlay = Helper.ModContent.Load<Texture2D>("assets/white_kitchen_tiles.png");
                    editor.PatchImage(overlay, patchMode: PatchMode.Overlay);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/walls_and_floors"))
            {
                e.Edit(asset =>
                {
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
            // Data/Furniture edits
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Furniture"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;

                    string bedName = I18n.Get("furniture.princess_double_bed.name");
                    string windowName = I18n.Get("furniture.princess_pastel_window.name");
                    string sconceName = I18n.Get("furniture.princess_wall_sconce.name");
                    string nightstandName = I18n.Get("furniture.princess_nightstand.name");
                    string rugName = I18n.Get("furniture.princess_grand_rug.name");
                    string mirrorName = I18n.Get("furniture.princess_rococo_mirror.name");
                    string canopyName = I18n.Get("furniture.princess_bed_canopy.name");

                    data["feiluvnana.BetterFurniture.PrincessDoubleBed"] = $"Princess Double Bed/bed double/4 4/4 4/1/10000/-1/{bedName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessDoubleBed/false";
                    data["feiluvnana.BetterFurniture.PrincessPastelWindow"] = $"Princess Pastel Window/window/4 2/4 2/1/2000/-1/{windowName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessPastelWindow/false";
                    data["feiluvnana.BetterFurniture.PrincessWallSconce"] = $"Princess Wall Sconce/painting/1 2/1 2/1/1000/-1/{sconceName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessWallSconce/false";
                    data["feiluvnana.BetterFurniture.PrincessNightstand"] = $"Princess Nightstand/lamp/1 2/1 1/1/2000/-1/{nightstandName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessNightstand/false";
                    data["feiluvnana.BetterFurniture.PrincessGrandRug"] = $"Princess Grand Rug/rug/4 3/4 3/1/3000/-1/{rugName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessGrandRug/false";
                    data["feiluvnana.BetterFurniture.PrincessRococoMirror"] = $"Princess Rococo Mirror/painting/2 2/2 2/1/2500/-1/{mirrorName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessRococoMirror/false";
                    data["feiluvnana.BetterFurniture.PrincessBedCanopy"] = $"Princess Bed Canopy/painting/4 3/4 3/1/3500/-1/{canopyName}/0/Mods\\feiluvnana.BetterFurniture\\PrincessBedCanopy/false";
                });
            }
            // Data/Shops edits (SeedShop / Pierre's General Store)
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ShopData>().Data;
                    if (data.TryGetValue("SeedShop", out var seedShop))
                    {
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
                            seedShop.Items.RemoveAll(i => i.Id == itemId);
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
