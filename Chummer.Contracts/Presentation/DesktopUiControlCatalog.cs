namespace Chummer.Contracts.Presentation;

public static class DesktopUiControlCatalog
{
    public static readonly IReadOnlyList<DesktopUiControlDefinition> All =
    [
        new("create_entry", "Add", "tab-info", true, true),
        new("edit_entry", "Edit", "tab-info", true, true),
        new("delete_entry", "Delete", "tab-info", true, true),
        new("move_up", "Up", "tab-info", true, true),
        new("move_down", "Down", "tab-info", true, true),
        new("toggle_free_paid", "Free/Paid", "tab-info", true, true),
        new("show_source", "Source", "tab-info", true, true),
        new("open_notes", "Notes", "tab-info", true, true),

        new("gear_add", "Add Gear", "tab-gear", true, true),
        new("gear_edit", "Edit Gear", "tab-gear", true, true),
        new("gear_delete", "Delete Gear", "tab-gear", true, true),
        new("gear_mount", "Mount", "tab-gear", true, true),
        new("gear_source", "Source", "tab-gear", true, true),

        new("magic_add", "Add Spell/Power", "tab-magician", true, true),
        new("magic_delete", "Delete", "tab-magician", true, true),
        new("magic_bind", "Bind/Link", "tab-magician", true, true),
        new("magic_source", "Source", "tab-magician", true, true),

        new("magic_add", "Add Spell/Power", "tab-adept", true, true),
        new("magic_delete", "Delete", "tab-adept", true, true),
        new("magic_bind", "Bind/Link", "tab-adept", true, true),
        new("magic_source", "Source", "tab-adept", true, true),

        new("magic_add", "Add Spell/Power", "tab-technomancer", true, true),
        new("magic_delete", "Delete", "tab-technomancer", true, true),
        new("magic_bind", "Bind/Link", "tab-technomancer", true, true),
        new("magic_source", "Source", "tab-technomancer", true, true),

        new("skill_add", "Add Skill", "tab-skills", true, true),
        new("skill_specialize", "Specialize", "tab-skills", true, true),
        new("skill_remove", "Remove", "tab-skills", true, true),
        new("skill_group", "Group", "tab-skills", true, true),

        new("combat_add_weapon", "Add Weapon", "tab-combat", true, true),
        new("combat_add_armor", "Add Armor", "tab-combat", true, true),
        new("combat_reload", "Reload", "tab-combat", true, true),
        new("combat_damage_track", "Damage Track", "tab-combat", true, true),

        new("contact_add", "Add Contact", "tab-contacts", true, true),
        new("contact_edit", "Edit Contact", "tab-contacts", true, true),
        new("contact_remove", "Remove Contact", "tab-contacts", true, true),
        new("contact_connection", "Connection/Loyalty", "tab-contacts", true, true)
    ];

    public static IReadOnlyList<DesktopUiControlDefinition> ForTab(string? tabId)
    {
        string effectiveTabId = string.IsNullOrWhiteSpace(tabId) ? "tab-info" : tabId;
        DesktopUiControlDefinition[] controls = All
            .Where(control => string.Equals(control.TabId, effectiveTabId, StringComparison.Ordinal))
            .ToArray();
        if (controls.Length > 0)
            return controls;

        return All
            .Where(control => string.Equals(control.TabId, "tab-info", StringComparison.Ordinal))
            .ToArray();
    }
}
