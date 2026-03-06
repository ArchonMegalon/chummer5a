using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;

namespace Chummer.Rulesets.Hosting.Presentation;

public static class DesktopUiControlCatalog
{
    public static readonly IReadOnlyList<DesktopUiControlDefinition> All = CreateSr5Catalog();

    public static IReadOnlyList<DesktopUiControlDefinition> ForRuleset(string? rulesetId)
    {
        string effectiveRulesetId = ResolveCompatibilityRulesetId(rulesetId);
        return All
            .Where(control => string.Equals(control.RulesetId, effectiveRulesetId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static IReadOnlyList<DesktopUiControlDefinition> ForTab(string? tabId)
        => ForTab(tabId, rulesetId: null);

    public static IReadOnlyList<DesktopUiControlDefinition> ForTab(string? tabId, string? rulesetId)
    {
        string effectiveTabId = string.IsNullOrWhiteSpace(tabId) ? "tab-info" : tabId;
        DesktopUiControlDefinition[] rulesetScopedControls = ForRuleset(rulesetId).ToArray();

        DesktopUiControlDefinition[] controls = rulesetScopedControls
            .Where(control => string.Equals(control.TabId, effectiveTabId, StringComparison.Ordinal))
            .ToArray();
        if (controls.Length > 0)
            return controls;

        return rulesetScopedControls
            .Where(control => string.Equals(control.TabId, "tab-info", StringComparison.Ordinal))
            .ToArray();
    }

    private static string ResolveCompatibilityRulesetId(string? rulesetId)
    {
        return RulesetDefaults.NormalizeOptional(rulesetId) ?? RulesetDefaults.Sr5;
    }

    private static IReadOnlyList<DesktopUiControlDefinition> CreateSr5Catalog()
    {
        return
        [
            Sr5("create_entry", "Add", "tab-info", true, true),
            Sr5("edit_entry", "Edit", "tab-info", true, true),
            Sr5("delete_entry", "Delete", "tab-info", true, true),
            Sr5("move_up", "Up", "tab-info", true, true),
            Sr5("move_down", "Down", "tab-info", true, true),
            Sr5("toggle_free_paid", "Free/Paid", "tab-info", true, true),
            Sr5("show_source", "Source", "tab-info", true, true),
            Sr5("open_notes", "Notes", "tab-info", true, true),

            Sr5("gear_add", "Add Gear", "tab-gear", true, true),
            Sr5("gear_edit", "Edit Gear", "tab-gear", true, true),
            Sr5("gear_delete", "Delete Gear", "tab-gear", true, true),
            Sr5("gear_mount", "Mount", "tab-gear", true, true),
            Sr5("gear_source", "Source", "tab-gear", true, true),

            Sr5("magic_add", "Add Spell/Power", "tab-magician", true, true),
            Sr5("magic_delete", "Delete", "tab-magician", true, true),
            Sr5("magic_bind", "Bind/Link", "tab-magician", true, true),
            Sr5("magic_source", "Source", "tab-magician", true, true),

            Sr5("magic_add", "Add Spell/Power", "tab-adept", true, true),
            Sr5("magic_delete", "Delete", "tab-adept", true, true),
            Sr5("magic_bind", "Bind/Link", "tab-adept", true, true),
            Sr5("magic_source", "Source", "tab-adept", true, true),

            Sr5("magic_add", "Add Spell/Power", "tab-technomancer", true, true),
            Sr5("magic_delete", "Delete", "tab-technomancer", true, true),
            Sr5("magic_bind", "Bind/Link", "tab-technomancer", true, true),
            Sr5("magic_source", "Source", "tab-technomancer", true, true),

            Sr5("skill_add", "Add Skill", "tab-skills", true, true),
            Sr5("skill_specialize", "Specialize", "tab-skills", true, true),
            Sr5("skill_remove", "Remove", "tab-skills", true, true),
            Sr5("skill_group", "Group", "tab-skills", true, true),

            Sr5("combat_add_weapon", "Add Weapon", "tab-combat", true, true),
            Sr5("combat_add_armor", "Add Armor", "tab-combat", true, true),
            Sr5("combat_reload", "Reload", "tab-combat", true, true),
            Sr5("combat_damage_track", "Damage Track", "tab-combat", true, true),

            Sr5("contact_add", "Add Contact", "tab-contacts", true, true),
            Sr5("contact_edit", "Edit Contact", "tab-contacts", true, true),
            Sr5("contact_remove", "Remove Contact", "tab-contacts", true, true),
            Sr5("contact_connection", "Connection/Loyalty", "tab-contacts", true, true)
        ];
    }

    private static DesktopUiControlDefinition Sr5(
        string id,
        string label,
        string tabId,
        bool requiresOpenCharacter,
        bool enabledByDefault)
        => new(id, label, tabId, requiresOpenCharacter, enabledByDefault, RulesetDefaults.Sr5);
}
