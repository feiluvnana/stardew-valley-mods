using System;
using StardewModdingAPI;

namespace Common
{
    /// <summary>Mirror of Generic Mod Config Menu's public API interface.</summary>
    public interface IGenericModConfigMenuApi
    {
        /// <summary>Registers this mod with GMCM, creating its settings page.</summary>
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        /// <summary>Adds a bold heading line grouping the options beneath it.</summary>
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

        /// <summary>Adds a static paragraph of informational text.</summary>
        void AddParagraph(IManifest mod, Func<string> text);

        /// <summary>Adds a checkbox bound to a bool setting.</summary>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

        /// <summary>Adds a numeric slider/spinbox bound to an int setting.</summary>
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);

        /// <summary>Adds a numeric slider/spinbox bound to a float setting.</summary>
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);

        /// <summary>Adds a free-text field, optionally limited to a dropdown of allowed values.</summary>
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);

        /// <summary>Adds a keybind picker bound to an SButton setting.</summary>
        void AddKeybind(IManifest mod, Func<SButton> getValue, Action<SButton> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }
}
