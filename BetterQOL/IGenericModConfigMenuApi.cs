using System;
using StardewModdingAPI;

// This file declares a C# INTERFACE describing the API surface of the separate
// "Generic Mod Config Menu" (GMCM) mod. An interface is a pure contract: it lists
// method signatures but contains NO implementation bodies. BetterQOL references only
// this contract, so it never needs a compile-time dependency on GMCM itself - at game
// launch SMAPI hands us GMCM's live object cast to this interface (see
// ModEntry.OnGameLaunched), or null if GMCM isn't installed.
namespace BetterQOL
{
    /// <summary>
    /// Mirror of GMCM's public API. Because every member below has no body, whatever
    /// class implements this interface must supply them all. Two delegate types recur:
    ///   Func&lt;T&gt;  - a function RETURNING T (used to GET the current setting),
    ///   Action&lt;T&gt; - a procedure TAKING T (used to SET a new value).
    /// Parameters with "= null" defaults are optional and may be omitted by callers.
    /// </summary>
    public interface IGenericModConfigMenuApi
    {
        /// <summary>
        /// Registers this mod with GMCM, creating its settings page.
        /// Must be called once before adding any options/sections.
        /// </summary>
        /// <param name="mod">This mod's manifest (identifies who owns the page).</param>
        /// <param name="reset">Runs when the player clicks "Reset to Defaults".</param>
        /// <param name="save">Runs when the player clicks "Save" (persist config.json).</param>
        /// <param name="titleScreenOnly">True restricts editing to the title-screen menu.</param>
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        /// <summary>Adds a bold heading line grouping the options beneath it.</summary>
        /// <param name="mod">Owner manifest.</param>
        /// <param name="text">Heading text (Func so it re-evaluates, e.g. on language change).</param>
        /// <param name="tooltip">Optional hover explainer for the heading.</param>
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        /// <summary>Adds a static paragraph of informational text (not editable).</summary>
        void AddParagraph(IManifest mod, Func<string> text);
        /// <summary>Adds a checkbox bound to a bool setting.</summary>
        /// <param name="getValue">Reads the live value (e.g. () => Config.InstantCracking).</param>
        /// <param name="setValue">Writes the player's new choice back to the config object.</param>
        /// <param name="name">Displayed label.</param>
        /// <param name="tooltip">Optional longer explanation.</param>
        /// <param name="fieldId">Optional stable id for GMCM's save/downstream APIs.</param>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        /// <summary>Adds a numeric slider/spinbox bound to an INT setting (int overload).</summary>
        /// <param name="min">Slider floor (null = GMCM default).</param>
        /// <param name="max">Slider ceiling (null = GMCM default).</param>
        /// <param name="interval">Step size between values.</param>
        /// <param name="formatValue">Optional formatter turning the number into display text.</param>
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
        /// <summary>Same as above but bound to a FLOAT (decimal) setting (float overload).</summary>
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);
        /// <summary>Adds a free-text field, optionally limited to a dropdown of allowedValues.</summary>
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
        /// <summary>Adds a "press a key/button" control bound to an SButton setting.</summary>
        void AddKeybind(IManifest mod, Func<SButton> getValue, Action<SButton> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }
}
