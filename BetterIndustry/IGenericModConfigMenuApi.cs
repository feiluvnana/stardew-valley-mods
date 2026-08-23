using System;
using StardewModdingAPI;

// This file is a local COPY of the public API of "Generic Mod Config Menu" (GMCM), the
// widely-used companion mod that gives every mod a graphical settings panel inside the
// game's options screen.
//
// What is an interface? A C# interface is a pure CONTRACT: it declares method signatures
// (name, parameter types, return type) but contains no implementation whatsoever. This
// mod never executes the code below - instead ModEntry asks SMAPI to find the installed
// GMCM mod and return an object that truly implements these methods (via SMAPI's
// Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>()). Keeping our own identical
// declaration means this project compiles even when GMCM isn't installed, making GMCM an
// "optional dependency": the settings menu simply appears only if the player has it.
//
// NOTE: member signatures must match GMCM's real interface exactly or runtime binding fails.
// (Each mod here ships its own copy so the projects stay independently buildable.)
namespace BetterIndustry
{
    /// <summary>
    /// The slice of GMCM's API used by BetterIndustry: registering a settings page plus
    /// headings, paragraphs, checkboxes, sliders, and text/dropdown options.
    /// </summary>
    public interface IGenericModConfigMenuApi
    {
        /// <summary>
        /// Registers this mod with GMCM and supplies the callbacks its menu buttons invoke.
        /// </summary>
        /// <param name="mod">This mod's manifest, shown as the page title in the options UI.</param>
        /// <param name="reset">Parameterless Action (a delegate) run when the player clicks "Reset to Defaults".</param>
        /// <param name="save">Parameterless Action run on "Save"; copies menu values back into the config object.</param>
        /// <param name="titleScreenOnly">Optional parameter (default false): when true, options are editable only from the title screen.</param>
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        /// <summary>Adds a bold heading row used to visually group related options.</summary>
        /// <param name="text">Zero-argument delegate returning the heading text; polled on demand so translations refresh live.</param>
        /// <param name="tooltip">Optional delegate supplying hover text; the trailing "?" means it may be null.</param>
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        /// <summary>Adds a passive informational paragraph (no interactive control).</summary>
        void AddParagraph(IManifest mod, Func<string> text);
        /// <summary>Adds a checkbox bound to a bool setting.</summary>
        /// <param name="getValue">Getter delegate GMCM polls while drawing the checkbox.</param>
        /// <param name="setValue">Delegate GMCM calls with the newly-ticked bool whenever the player toggles it.</param>
        /// <param name="name">Label shown beside the checkbox.</param>
        /// <param name="tooltip">Optional hover description.</param>
        /// <param name="fieldId">Optional stable identifier other mods can use to programmatically change this option.</param>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        /// <summary>Adds a slider bound to an int (whole-number) setting, with optional min/max/step bounds.</summary>
        /// <param name="min">Nullable int ("int?" - a value type that can also be null): omit to leave the slider unbounded below.</param>
        /// <param name="interval">Step size per click (e.g. 5 jumps 0, 5, 10...).</param>
        /// <param name="formatValue">Optional delegate converting the raw number into display text for the slider label.</param>
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
        /// <summary>Adds a slider bound to a float (decimal) setting. It shares the method NAME with the int version
        /// above - C# allows such "overloads", and the compiler picks one by argument types.</summary>
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);
        /// <summary>Adds a free-text field; providing allowedValues turns it into a dropdown instead.</summary>
        /// <param name="allowedValues">Optional whitelist of permitted strings; null allows arbitrary typing.</param>
        /// <param name="formatAllowedValue">Optional delegate mapping each allowed value to prettier display text in the dropdown.</param>
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
    }
}
