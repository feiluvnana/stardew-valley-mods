using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace BetterQOL
{
    /// <summary>
    /// Harmony patch for Stardew Valley's in-game clock and HUD widget (DayTimeMoneyBox).
    /// Displays rich hover tooltips when hovering over the weather icon or the season icon
    /// in the upper right corner of the screen.
    /// </summary>
    public static class DayTimeMoneyBoxPatch
    {
        /// <summary>
        /// Registers Harmony postfix on DayTimeMoneyBox.draw.
        /// </summary>
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            try
            {
                var drawMethod = AccessTools.Method(typeof(DayTimeMoneyBox), nameof(DayTimeMoneyBox.draw), new[] { typeof(SpriteBatch) });
                if (drawMethod != null)
                {
                    var drawPostfix = new HarmonyMethod(typeof(DayTimeMoneyBoxPatch), nameof(DrawPostfix));
                    harmony.Patch(drawMethod, postfix: drawPostfix);
                    monitor.Log("Successfully applied DayTimeMoneyBox draw patch for weather and season hover tooltips.", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed to patch DayTimeMoneyBox.draw: {ex.Message}", LogLevel.Warn);
            }
        }

        /// <summary>
        /// Postfix on DayTimeMoneyBox.draw to render weather and season hover tooltips.
        /// </summary>
        public static void DrawPostfix(DayTimeMoneyBox __instance, SpriteBatch b)
        {
            if (!Context.IsWorldReady || !ModEntry.Config.EnableWeatherAndSeasonHover)
                return;

            // Only during active gameplay without blocking menus or cutscenes
            if (Game1.activeClickableMenu != null || Game1.eventUp || Game1.farmEvent != null)
                return;

            // Check optional hover activation hotkey
            if (ModEntry.Config.HoverHotkey != SButton.None && !ModEntry.ModHelper.Input.IsDown(ModEntry.Config.HoverHotkey))
                return;

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            Vector2 pos = __instance.position;

            // Weather icon is drawn at pos + (116, 68) with 12x8 source rect scaled x4 (48x32 px)
            var weatherRect = new Rectangle((int)pos.X + 112, (int)pos.Y + 64, 56, 40);

            // Season icon is drawn at pos + (212, 68) with 12x8 source rect scaled x4 (48x32 px)
            var seasonRect = new Rectangle((int)pos.X + 208, (int)pos.Y + 64, 56, 40);

            if (weatherRect.Contains(mouseX, mouseY))
            {
                var (title, body) = GetWeatherTooltip();
                IClickableMenu.drawToolTip(b, body, title, null);
            }
            else if (seasonRect.Contains(mouseX, mouseY))
            {
                var (title, body) = GetSeasonTooltip();
                IClickableMenu.drawToolTip(b, body, title, null);
            }
        }

        /// <summary>
        /// Gets the weather title and description tooltip for today and tomorrow.
        /// </summary>
        public static (string title, string body) GetWeatherTooltip()
        {
            string title;
            string desc;

            if (Game1.IsGreenRainingHere() || Game1.isGreenRain || Game1.weatherIcon == 999)
            {
                title = ModEntry.I18n.Get("lookup.weather.green-rain-text").ToString();
                desc = ModEntry.I18n.Get("hover.weather.green-rain-desc").ToString();
            }
            else if (Game1.weddingToday || Game1.weatherIcon == 6)
            {
                title = ModEntry.I18n.Get("lookup.weather.wedding").ToString();
                desc = ModEntry.I18n.Get("hover.weather.wedding-desc").ToString();
            }
            else if (Utility.isFestivalDay() || Game1.weatherIcon == 4)
            {
                string? festivalName = null;
                try
                {
                    var festivalData = Game1.temporaryContent.Load<Dictionary<string, string>>("Data\\Festivals\\" + Game1.currentSeason + Game1.dayOfMonth);
                    if (festivalData != null && festivalData.TryGetValue("name", out var festName))
                    {
                        festivalName = festName;
                    }
                }
                catch { }

                title = !string.IsNullOrEmpty(festivalName) ? festivalName : ModEntry.I18n.Get("lookup.weather.festival").ToString();
                if (Game1.whereIsTodaysFest != null)
                {
                    desc = ModEntry.I18n.Get("hover.weather.festival-at", new
                    {
                        name = title,
                        location = Game1.whereIsTodaysFest
                    }).ToString();
                }
                else
                {
                    desc = ModEntry.I18n.Get("hover.weather.festival-desc").ToString();
                }
            }
            else if (Game1.IsLightningHere() || Game1.isLightning || Game1.weatherIcon == 3)
            {
                title = ModEntry.I18n.Get("lookup.weather.lightning-storm").ToString();
                desc = ModEntry.I18n.Get("hover.weather.storm-desc").ToString();
            }
            else if (Game1.IsSnowingHere() || Game1.isSnowing || Game1.weatherIcon == 5 || Game1.weatherIcon == 7)
            {
                title = ModEntry.I18n.Get("lookup.weather.snowing").ToString();
                desc = ModEntry.I18n.Get("hover.weather.snow-desc").ToString();
            }
            else if (Game1.IsRainingHere() || Game1.isRaining || Game1.weatherIcon == 1)
            {
                title = ModEntry.I18n.Get("lookup.weather.rainy-text").ToString();
                desc = ModEntry.I18n.Get("hover.weather.rain-desc").ToString();
            }
            else if (Game1.IsDebrisWeatherHere() || Game1.isDebrisWeather || Game1.weatherIcon == 2)
            {
                title = ModEntry.I18n.Get("lookup.weather.windy-debris").ToString();
                desc = ModEntry.I18n.Get("hover.weather.debris-desc").ToString();
            }
            else
            {
                title = ModEntry.I18n.Get("lookup.weather.clear").ToString();
                desc = ModEntry.I18n.Get("hover.weather.sunny-desc").ToString();
            }

            string tomorrowWeather = GetLocalizedTomorrowWeather();
            string tomorrowLine = ModEntry.I18n.Get("hover.weather.tomorrow-forecast", new { weather = tomorrowWeather }).ToString();
            string body = $"{desc}\n\n{tomorrowLine}";

            return (title, body);
        }

        /// <summary>
        /// Gets the localized weather forecast for tomorrow.
        /// </summary>
        public static string GetLocalizedTomorrowWeather()
        {
            return Game1.weatherForTomorrow switch
            {
                "Rain" => ModEntry.I18n.Get("lookup.weather.rainy-text").ToString(),
                "Storm" or "Lightning" => ModEntry.I18n.Get("lookup.weather.lightning-storm").ToString(),
                "Snow" => ModEntry.I18n.Get("lookup.weather.snowing").ToString(),
                "GreenRain" => ModEntry.I18n.Get("lookup.weather.green-rain-text").ToString(),
                "Wind" or "Debris" => ModEntry.I18n.Get("lookup.weather.windy-debris").ToString(),
                "Festival" => ModEntry.I18n.Get("lookup.weather.festival").ToString(),
                "Wedding" => ModEntry.I18n.Get("lookup.weather.wedding").ToString(),
                _ => ModEntry.I18n.Get("lookup.weather.sunny").ToString()
            };
        }

        /// <summary>
        /// Gets the season title and progress details tooltip.
        /// </summary>
        public static (string title, string body) GetSeasonTooltip()
        {
            string seasonName = GetLocalizedSeasonName(Game1.seasonIndex);
            string nextSeasonName = GetNextLocalizedSeasonName(Game1.seasonIndex);
            int day = Game1.dayOfMonth;
            int daysLeft = Math.Max(0, 28 - day);

            string title = ModEntry.I18n.Get("hover.season.title", new
            {
                season = seasonName,
                year = Game1.year
            }).ToString();

            string body = ModEntry.I18n.Get("hover.season.body", new
            {
                day = day,
                total = 28,
                daysLeft = daysLeft,
                nextSeason = nextSeasonName
            }).ToString();

            return (title, body);
        }

        /// <summary>
        /// Gets the localized display name for a season index (0: Spring, 1: Summer, 2: Fall, 3: Winter).
        /// </summary>
        public static string GetLocalizedSeasonName(int seasonIndex)
        {
            return seasonIndex switch
            {
                0 => ModEntry.I18n.Get("season.spring").ToString(),
                1 => ModEntry.I18n.Get("season.summer").ToString(),
                2 => ModEntry.I18n.Get("season.fall").ToString(),
                3 => ModEntry.I18n.Get("season.winter").ToString(),
                _ => Game1.CurrentSeasonDisplayName
            };
        }

        /// <summary>
        /// Gets the localized display name for the next season.
        /// </summary>
        public static string GetNextLocalizedSeasonName(int seasonIndex)
        {
            int nextSeason = (seasonIndex + 1) % 4;
            return GetLocalizedSeasonName(nextSeason);
        }
    }
}