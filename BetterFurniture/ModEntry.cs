using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BetterFurniture
{
    public class ModEntry : Mod
    {
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static IModHelper ModHelper { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            ModHelper = helper;

            var harmony = new Harmony(ModManifest.UniqueID);
            BedPatches.Apply(harmony, Monitor);
            FurniturePatches.Apply(harmony, Monitor);

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.SaveLoaded += (s, e) => FurniturePatches.FixAllLocationAndInventoryFurniture();
            helper.Events.GameLoop.DayStarted += (s, e) => FurniturePatches.FixAllLocationAndInventoryFurniture();

            Monitor.Log("BetterFurniture initialized with 4x4 bed enhancements, floor restorations, and wall decor patches.", LogLevel.Debug);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Custom texture loads
            var textures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mods/feiluvnana.BetterFurniture/PrincessDoubleBed"] = "assets/princess_double_bed.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessPastelWindow"] = "assets/princess_pastel_window.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessWallMolding"] = "assets/princess_wall_molding.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessWallSconce"] = "assets/princess_wall_sconce.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessNightstand"] = "assets/princess_nightstand.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessGrandRug"] = "assets/princess_grand_rug.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessRococoMirror"] = "assets/princess_rococo_mirror.png",
                ["Mods/feiluvnana.BetterFurniture/PrincessBedCanopy"] = "assets/princess_bed_canopy.png",
                ["Mods/feiluvnana.BetterFurniture/TeakDiningTable"] = "assets/teak_dining_table.png",
                ["Mods/feiluvnana.BetterFurniture/TeakDiningBench"] = "assets/teak_dining_bench.png"
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

                    data["feiluvnana.BetterFurniture.PrincessDoubleBed"] = "Princess Double Bed/bed double/4 4/4 4/1/10000/-1/Princess Double Bed/0/Mods\\feiluvnana.BetterFurniture\\PrincessDoubleBed/true";
                    data["feiluvnana.BetterFurniture.PrincessPastelWindow"] = "Princess Pastel Window/window/2 2/2 2/1/2000/-1/Princess Pastel Window/0/Mods\\feiluvnana.BetterFurniture\\PrincessPastelWindow/true";
                    data["feiluvnana.BetterFurniture.PrincessWallMolding"] = "Princess Wall Molding/painting/2 2/2 2/1/1500/-1/Princess Wall Molding/0/Mods\\feiluvnana.BetterFurniture\\PrincessWallMolding/true";
                    data["feiluvnana.BetterFurniture.PrincessWallSconce"] = "Princess Wall Sconce/painting/1 2/1 2/1/1000/-1/Princess Wall Sconce/0/Mods\\feiluvnana.BetterFurniture\\PrincessWallSconce/true";
                    data["feiluvnana.BetterFurniture.PrincessNightstand"] = "Princess Nightstand/lamp/1 2/1 1/1/2000/-1/Princess Nightstand/0/Mods\\feiluvnana.BetterFurniture\\PrincessNightstand/true";
                    data["feiluvnana.BetterFurniture.PrincessGrandRug"] = "Princess Grand Rug/rug/4 3/4 3/2/3000/-1/Princess Grand Rug/0/Mods\\feiluvnana.BetterFurniture\\PrincessGrandRug/true";
                    data["feiluvnana.BetterFurniture.PrincessRococoMirror"] = "Princess Rococo Mirror/painting/2 2/2 2/1/2500/-1/Princess Rococo Mirror/0/Mods\\feiluvnana.BetterFurniture\\PrincessRococoMirror/true";
                    data["feiluvnana.BetterFurniture.PrincessBedCanopy"] = "Princess Bed Canopy/painting/4 3/4 3/1/3500/-1/Princess Bed Canopy/0/Mods\\feiluvnana.BetterFurniture\\PrincessBedCanopy/true";
                    data["feiluvnana.BetterFurniture.TeakDiningTable"] = "Teak Dining Table/table/3 2/3 2/1/3500/-1/Teak Dining Table/0/Mods\\feiluvnana.BetterFurniture\\TeakDiningTable/true";
                    data["feiluvnana.BetterFurniture.TeakDiningBench"] = "Teak Dining Bench/bench/3 2/3 1/1/2000/-1/Teak Dining Bench/0/Mods\\feiluvnana.BetterFurniture\\TeakDiningBench/true";
                });
            }
        }
    }
}
