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
namespace BetterMap
{
    /// <summary>
    /// The slice of GMCM's API used by BetterMap: registering a settings page and adding
    /// headings, paragraphs, and boolean checkboxes for its two toggle options.
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
        /// <summary>
        /// Adds a bold section heading used to visually group the options below it.
        /// </summary>
        /// <param name="text">Zero-argument delegate (Func) returning the heading string; polled on demand so translations refresh live.</param>
        /// <param name="tooltip">Optional delegate supplying hover text; the trailing "?" means the parameter may be null.</param>
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        /// <summary>
        /// Adds a passive informational paragraph (no control, just explanatory text).
        /// </summary>
        void AddParagraph(IManifest mod, Func<string> text);
        /// <summary>
        /// Adds a checkbox bound to a bool setting (e.g. BetterMap's RemoveFarmDriftwoodBarrier).
        /// </summary>
        /// <param name="getValue">Getter delegate GMCM polls while drawing the checkbox.</param>
        /// <param name="setValue">Delegate GMCM calls with the newly-ticked bool whenever the player toggles it.</param>
        /// <param name="name">Label shown beside the checkbox.</param>
        /// <param name="tooltip">Optional hover description.</param>
        /// <param name="fieldId">Optional stable identifier another mod can use to programmatically change this option.</param>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }
}
