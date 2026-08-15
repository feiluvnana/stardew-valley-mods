using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using xTile;

namespace BetterMap
{
    public class ModEntry : Mod
    {
        public static ModEntry Instance { get; private set; } = null!;
        public ModConfig Config { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = Helper.ReadConfig<ModConfig>();

            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Monitor.Log("BetterMap loaded successfully.", LogLevel.Info);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/Island_S"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchIslandSouth(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/Island_W"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchIslandWest(editor.Data, Config, Monitor);
                });
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            RegisterGenericModConfigMenu();
        }

        private void RegisterGenericModConfigMenu()
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    Config = new ModConfig();
                    Helper.WriteConfig(Config);
                    ReloadMaps();
                },
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    ReloadMaps();
                }
            );

            // Section: Ginger Island
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => Helper.Translation.Get("gmcm.section.ginger_island.title"),
                tooltip: () => Helper.Translation.Get("gmcm.section.ginger_island.description")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => Config.RemoveBeachFarmWreck,
                setValue: value => Config.RemoveBeachFarmWreck = value,
                name: () => Helper.Translation.Get("gmcm.remove_beach_farm_wreck.name"),
                tooltip: () => Helper.Translation.Get("gmcm.remove_beach_farm_wreck.tooltip")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => Config.RemoveIslandWestShipwreck,
                setValue: value => Config.RemoveIslandWestShipwreck = value,
                name: () => Helper.Translation.Get("gmcm.remove_island_west_shipwreck.name"),
                tooltip: () => Helper.Translation.Get("gmcm.remove_island_west_shipwreck.tooltip")
            );
        }

        private void ReloadMaps()
        {
            Helper.GameContent.InvalidateCache("Maps/Island_S");
            Helper.GameContent.InvalidateCache("Maps/Island_W");
            Monitor.Log("BetterMap: Invalided map cache and reloaded maps.", LogLevel.Debug);
        }
    }
}
