// =============================================================================
//  IGenericModConfigMenuApi.cs — a beginner-friendly look at "API mirroring".
// -----------------------------------------------------------------------------
//  WHAT IS THIS FILE?
//  "Generic Mod Config Menu" (GMCM, by spacechase0) is a SEPARATE mod that
//  draws a nice in-game settings screen. BetterMap and BetterEvent want to
//  put their options into that screen. But neither project links against
//  GMCM's compiled DLL, on purpose:
//
//    * If GMCM is not installed, a hard DLL reference would make our mod fail.
//    * Any GMCM update could silently break that hard link.
//
//  HOW DOES IT WORK WITHOUT THE DLL?
//  1. An INTERFACE in C# is a pure contract: it lists method signatures
//     (name + parameter types + return type) but contains NO code at all.
//     You cannot create an interface directly; other classes "implement" it.
//  2. Below, we re-type GMCM's public methods by hand, matching names and
//     types exactly — like cutting a duplicate key by tracing the original.
//  3. At runtime, our mod asks SMAPI for GMCM's live object:
//         Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")
//     SMAPI inspects the real GMCM object and checks that it satisfies every
//     signature in OUR interface — a loose match often called "duck typing"
//     (if it walks and quacks like the interface, it fits).
//  4. Our code can then call configMenu.AddBoolOption(...) and .NET routes
//     each call to GMCM's real implementation. Compilation never needed GMCM!
//
//  C# SYNTAX USED HERE (quick tour):
//   * `void`          — the method returns nothing.
//   * `Action`        — a DELEGATE: a variable holding "a reference to a
//                       method". Plain `Action` takes nothing, returns nothing.
//   * `Action<bool>`  — a delegate that RECEIVES a bool (GMCM hands us the new
//                       value when the player flips a checkbox).
//   * `Func<bool>`    — a delegate that RETURNS a bool (GMCM asks us for the
//                       current value while drawing the menu).
//   * `Func<string>?` — the trailing `?` means NULLABLE: the caller may pass
//                       null (here: "no tooltip wanted").
//   * `= null` / `= false` at the end of a parameter list = OPTIONAL
//                       parameters with DEFAULT VALUES; callers may omit them.
//   * `string[]?`     — a nullable ARRAY of strings.
//   * `SButton`       — an ENUM (a named-constant list) of keyboard/mouse/
//                       controller buttons defined by SMAPI.
//   * About `[method: ...]` ATTRIBUTES: attributes are metadata tags written
//                       in square brackets; the `method:` prefix tells C#
//                       WHICH member the tag applies to (needed wherever the
//                       target would otherwise be ambiguous, e.g. attaching an
//                       attribute to a property's getter/setter or to an
//                       interface implementation). Several well-known SMAPI
//                       API mirrors use such targeted attributes on their
//                       members; THIS interface consists solely of plain
//                       methods, so no attribute targeting is required here.
// =============================================================================
using StardewModdingAPI;

namespace Common
{
    /// <summary>Mirror of Generic Mod Config Menu's public API interface.</summary>
    /// <remarks>
    /// An interface declares WHAT methods exist, never HOW they work — there
    /// are no method bodies between these braces, only signatures ending in a
    /// semicolon. SMAPI binds this contract to the real GMCM object at runtime
    /// purely by matching method names and parameter types, so every signature
    /// below must stay identical to GMCM's own public API; otherwise the
    /// lookup fails and returns null (each ModEntry then simply skips creating
    /// a config menu — see its `if (configMenu is null) return;` check).
    /// </remarks>
    public interface IGenericModConfigMenuApi
    {
        /// <summary>Registers this mod with GMCM, creating its settings page.</summary>
        /// <remarks>
        /// Must be called exactly once per mod, BEFORE any Add* method.
        /// <paramref name="mod"/> identifies whose page this is (our manifest).
        /// <paramref name="reset"/> runs when the player clicks "Reset to
        /// Defaults"; <paramref name="save"/> runs when they click Save — that
        /// is where each mod persists its config.json.
        /// <paramref name="titleScreenOnly"/> is optional and defaults to
        /// false, meaning the page is also available in-game.
        /// </remarks>
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        /// <summary>Adds a bold heading line grouping the options beneath it.</summary>
        /// <remarks>
        /// <paramref name="text"/> is a <c>Func&lt;string&gt;</c>, so GMCM
        /// re-evaluates it every time the page opens — that is how translated
        /// text stays current. <paramref name="tooltip"/> is optional (nullable)
        /// hover help.
        /// </remarks>
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

        /// <summary>Adds a static paragraph of informational text.</summary>
        /// <remarks>Purely cosmetic — not bound to any setting.</remarks>
        void AddParagraph(IManifest mod, Func<string> text);

        /// <summary>Adds a checkbox bound to a bool setting.</summary>
        /// <remarks>
        /// GMCM calls <paramref name="getValue"/> to draw the current state and
        /// <paramref name="setValue"/> when the player toggles it — the classic
        /// get/set delegate pair that lets GMCM read and write OUR config
        /// object without knowing anything about our ModConfig class.
        /// <paramref name="fieldId"/> optionally tags the widget so other mods
        /// can find it through GMCM's own API.
        /// </remarks>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

        /// <summary>Adds a numeric slider/spinbox bound to an int setting.</summary>
        /// <remarks>
        /// <paramref name="min"/>/<paramref name="max"/>/<paramref name="interval"/>
        /// limit the range and step size; <paramref name="formatValue"/>
        /// converts the raw number into display text (e.g. "day 22").
        /// </remarks>
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);

        /// <summary>Adds a numeric slider/spinbox bound to a float setting.</summary>
        /// <remarks>
        /// Same idea as the int version above — this is an OVERLOAD: the same
        /// method name reused with different parameter types.
        /// </remarks>
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);

        /// <summary>Adds a free-text field, optionally limited to a dropdown of allowed values.</summary>
        /// <remarks>
        /// When <paramref name="allowedValues"/> is supplied the box becomes a
        /// dropdown; <paramref name="formatAllowedValue"/> prettifies each
        /// choice shown to the player.
        /// </remarks>
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);

        /// <summary>Adds a keybind picker bound to an SButton setting.</summary>
        /// <remarks>
        /// SButton is SMAPI's cross-platform button enum, so one binding covers
        /// keyboard, mouse, and controller inputs alike.
        /// </remarks>
        void AddKeybind(IManifest mod, Func<SButton> getValue, Action<SButton> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }
}
