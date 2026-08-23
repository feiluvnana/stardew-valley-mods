using System;
using StardewModdingAPI;

// This file declares the SHAPE of the settings-menu API exposed by the popular
// "Generic Mod Config Menu" (GMCM) mod. It contains NO code at all — it's an
// INTERFACE: a contract listing method signatures that some other class promises
// to implement. BetterForge only needs the signatures to call GMCM safely.
//
// C# interfaces 101:
// - An interface says WHAT can be called, never HOW it works.
// - If GMCM is installed, SMAPI hands us a live object implementing this
//   interface and every call goes straight to the real mod.
// - If GMCM is NOT installed, we simply never get an API object and skip the
//   menu code — no crash, because we only ever talk through this contract.
namespace BetterForge
{
    /// <summary>
    /// Mirror of Generic Mod Config Menu's public API. Declaring it locally lets
    /// BetterForge build and run even when GMCM isn't installed.
    /// </summary>
    public interface IGenericModConfigMenuApi
    {
        /// <summary>
        /// Registers your mod with GMCM and creates its entry in the title-screen menu.
        /// </summary>
        /// <param name="mod">This mod's manifest (name/version shown in the UI).</param>
        /// <param name="reset">Action run when the player clicks "Reset to defaults".</param>
        /// <param name="save">Action run when the player clicks "Save" (writes config.json).</param>
        /// <param name="titleScreenOnly">If true, options can only change from the title screen.</param>
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        /// <summary>
        /// Adds a bold section heading used to group related options visually.
        /// </summary>
        /// <param name="mod">The mod that owns the option.</param>
        /// <param name="text">Heading text (a Func&lt;string&gt; so it can be translated later).</param>
        /// <param name="tooltip">Optional hover text explaining the section.</param>
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

        /// <summary>
        /// Adds a plain read-only paragraph of text between options.
        /// </summary>
        void AddParagraph(IManifest mod, Func<string> text);

        /// <summary>
        /// Adds a true/false checkbox bound to one bool config property.
        /// </summary>
        /// <param name="getValue">Getter GMCM calls to display the current value.</param>
        /// <param name="setValue">Setter GMCM calls when the player toggles it.</param>
        /// <param name="name">Label text.</param>
        /// <param name="tooltip">Optional hover text.</param>
        /// <param name="fieldId">Optional ID used by GMCM's keybinding/API features.</param>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

        /// <summary>
        /// Adds a numeric slider/field for an int setting (min/max/interval clamp input).
        /// </summary>
        /// <param name="formatValue">Optional callback to customize how the number is displayed.</param>
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);

        /// <summary>
        /// Same as the int overload, but for float (decimal) settings.
        /// </summary>
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);

        /// <summary>
        /// Adds a text field, optionally restricted to a dropdown of allowed values.
        /// </summary>
        /// <param name="allowedValues">If set, shows a dropdown instead of free text.</param>
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
    }
}
