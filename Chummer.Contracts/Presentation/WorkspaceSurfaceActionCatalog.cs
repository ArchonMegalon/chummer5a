using Chummer.Contracts.Rulesets;

namespace Chummer.Contracts.Presentation;

public static class WorkspaceSurfaceActionCatalog
{
    public static readonly IReadOnlyList<WorkspaceSurfaceActionDefinition> All =
    [
        new("tab-info.summary", "Summary", "tab-info", WorkspaceSurfaceActionKind.Summary, "summary", true, true),
        new("tab-info.validate", "Validate", "tab-info", WorkspaceSurfaceActionKind.Validate, "validate", true, true),
        new("tab-info.metadata", "Apply Metadata", "tab-info", WorkspaceSurfaceActionKind.Metadata, "metadata", true, true),
        new("tab-info.profile", "Profile", "tab-info", WorkspaceSurfaceActionKind.Section, "profile", true, true),
        new("tab-info.progress", "Progress", "tab-info", WorkspaceSurfaceActionKind.Section, "progress", true, true),
        new("tab-info.rules", "Rules", "tab-info", WorkspaceSurfaceActionKind.Section, "rules", true, true),
        new("tab-info.build", "Build", "tab-info", WorkspaceSurfaceActionKind.Section, "build", true, true),
        new("tab-info.movement", "Movement", "tab-info", WorkspaceSurfaceActionKind.Section, "movement", true, true),
        new("tab-info.awakening", "Awakening", "tab-info", WorkspaceSurfaceActionKind.Section, "awakening", true, true),
        new("tab-info.attributes", "Attributes", "tab-info", WorkspaceSurfaceActionKind.Section, "attributes", true, true),
        new("tab-info.attributedetails", "Attribute Details", "tab-info", WorkspaceSurfaceActionKind.Section, "attributedetails", true, true),
        new("tab-info.skills", "Skills", "tab-info", WorkspaceSurfaceActionKind.Section, "skills", true, true),
        new("tab-info.qualities", "Qualities", "tab-info", WorkspaceSurfaceActionKind.Section, "qualities", true, true),
        new("tab-info.contacts", "Contacts", "tab-info", WorkspaceSurfaceActionKind.Section, "contacts", true, true),
        new("tab-info.spells", "Spells", "tab-info", WorkspaceSurfaceActionKind.Section, "spells", true, true),
        new("tab-info.powers", "Powers", "tab-info", WorkspaceSurfaceActionKind.Section, "powers", true, true),
        new("tab-info.complexforms", "Complex Forms", "tab-info", WorkspaceSurfaceActionKind.Section, "complexforms", true, true),
        new("tab-info.martialarts", "Martial Arts", "tab-info", WorkspaceSurfaceActionKind.Section, "martialarts", true, true),

        new("tab-gear.inventory", "Inventory", "tab-gear", WorkspaceSurfaceActionKind.Section, "inventory", true, true),
        new("tab-gear.gear", "Gear", "tab-gear", WorkspaceSurfaceActionKind.Section, "gear", true, true),
        new("tab-gear.gearlocations", "Gear Locations", "tab-gear", WorkspaceSurfaceActionKind.Section, "gearlocations", true, true),
        new("tab-gear.weapons", "Weapons", "tab-gear", WorkspaceSurfaceActionKind.Section, "weapons", true, true),
        new("tab-gear.weaponaccessories", "Weapon Accessories", "tab-gear", WorkspaceSurfaceActionKind.Section, "weaponaccessories", true, true),
        new("tab-gear.weaponlocations", "Weapon Locations", "tab-gear", WorkspaceSurfaceActionKind.Section, "weaponlocations", true, true),
        new("tab-gear.armors", "Armors", "tab-gear", WorkspaceSurfaceActionKind.Section, "armors", true, true),
        new("tab-gear.armormods", "Armor Mods", "tab-gear", WorkspaceSurfaceActionKind.Section, "armormods", true, true),
        new("tab-gear.armorlocations", "Armor Locations", "tab-gear", WorkspaceSurfaceActionKind.Section, "armorlocations", true, true),
        new("tab-gear.cyberwares", "Cyberwares", "tab-gear", WorkspaceSurfaceActionKind.Section, "cyberwares", true, true),
        new("tab-gear.drugs", "Drugs", "tab-gear", WorkspaceSurfaceActionKind.Section, "drugs", true, true),
        new("tab-gear.lifestyles", "Lifestyles", "tab-gear", WorkspaceSurfaceActionKind.Section, "lifestyles", true, true),
        new("tab-gear.vehicles", "Vehicles", "tab-gear", WorkspaceSurfaceActionKind.Section, "vehicles", true, true),
        new("tab-gear.vehiclemods", "Vehicle Mods", "tab-gear", WorkspaceSurfaceActionKind.Section, "vehiclemods", true, true),
        new("tab-gear.vehiclelocations", "Vehicle Locations", "tab-gear", WorkspaceSurfaceActionKind.Section, "vehiclelocations", true, true),
        new("tab-gear.sources", "Sources", "tab-gear", WorkspaceSurfaceActionKind.Section, "sources", true, true),
        new("tab-gear.customdatadirectorynames", "Custom Data Dirs", "tab-gear", WorkspaceSurfaceActionKind.Section, "customdatadirectorynames", true, true),

        new("tab-magician.spirits", "Spirits", "tab-magician", WorkspaceSurfaceActionKind.Section, "spirits", true, true),
        new("tab-magician.foci", "Foci", "tab-magician", WorkspaceSurfaceActionKind.Section, "foci", true, true),
        new("tab-magician.aiprograms", "AI Programs", "tab-magician", WorkspaceSurfaceActionKind.Section, "aiprograms", true, true),
        new("tab-magician.limitmodifiers", "Limit Modifiers", "tab-magician", WorkspaceSurfaceActionKind.Section, "limitmodifiers", true, true),
        new("tab-magician.metamagics", "Metamagics", "tab-magician", WorkspaceSurfaceActionKind.Section, "metamagics", true, true),
        new("tab-magician.arts", "Arts", "tab-magician", WorkspaceSurfaceActionKind.Section, "arts", true, true),
        new("tab-magician.initiationgrades", "Initiation Grades", "tab-magician", WorkspaceSurfaceActionKind.Section, "initiationgrades", true, true),
        new("tab-magician.critterpowers", "Critter Powers", "tab-magician", WorkspaceSurfaceActionKind.Section, "critterpowers", true, true),
        new("tab-magician.mentorspirits", "Mentor Spirits", "tab-magician", WorkspaceSurfaceActionKind.Section, "mentorspirits", true, true),
        new("tab-magician.expenses", "Expenses", "tab-magician", WorkspaceSurfaceActionKind.Section, "expenses", true, true),
        new("tab-magician.calendar", "Calendar", "tab-magician", WorkspaceSurfaceActionKind.Section, "calendar", true, true),
        new("tab-magician.improvements", "Improvements", "tab-magician", WorkspaceSurfaceActionKind.Section, "improvements", true, true),

        new("tab-attributes.attributes", "Attributes Summary", "tab-attributes", WorkspaceSurfaceActionKind.Section, "attributes", true, true),
        new("tab-attributes.attributedetails", "Attribute Details", "tab-attributes", WorkspaceSurfaceActionKind.Section, "attributedetails", true, true),
        new("tab-attributes.limitmodifiers", "Limit Modifiers", "tab-attributes", WorkspaceSurfaceActionKind.Section, "limitmodifiers", true, true),

        new("tab-skills.skills", "Skills", "tab-skills", WorkspaceSurfaceActionKind.Section, "skills", true, true),
        new("tab-skills.martialarts", "Martial Arts", "tab-skills", WorkspaceSurfaceActionKind.Section, "martialarts", true, true),

        new("tab-qualities.qualities", "Qualities", "tab-qualities", WorkspaceSurfaceActionKind.Section, "qualities", true, true),
        new("tab-qualities.improvements", "Improvements", "tab-qualities", WorkspaceSurfaceActionKind.Section, "improvements", true, true),

        new("tab-adept.powers", "Adept Powers", "tab-adept", WorkspaceSurfaceActionKind.Section, "powers", true, true),
        new("tab-adept.metamagics", "Metamagics", "tab-adept", WorkspaceSurfaceActionKind.Section, "metamagics", true, true),
        new("tab-adept.initiationgrades", "Initiation/Submersion", "tab-adept", WorkspaceSurfaceActionKind.Section, "initiationgrades", true, true),

        new("tab-technomancer.complexforms", "Complex Forms", "tab-technomancer", WorkspaceSurfaceActionKind.Section, "complexforms", true, true),
        new("tab-technomancer.aiprograms", "Advanced Programs", "tab-technomancer", WorkspaceSurfaceActionKind.Section, "aiprograms", true, true),

        new("tab-combat.weapons", "Weapons", "tab-combat", WorkspaceSurfaceActionKind.Section, "weapons", true, true),
        new("tab-combat.armors", "Armor", "tab-combat", WorkspaceSurfaceActionKind.Section, "armors", true, true),
        new("tab-combat.drugs", "Drugs", "tab-combat", WorkspaceSurfaceActionKind.Section, "drugs", true, true),
        new("tab-combat.movement", "Movement", "tab-combat", WorkspaceSurfaceActionKind.Section, "movement", true, true),

        new("tab-armor.armors", "Armor Items", "tab-armor", WorkspaceSurfaceActionKind.Section, "armors", true, true),
        new("tab-armor.armormods", "Armor Mods", "tab-armor", WorkspaceSurfaceActionKind.Section, "armormods", true, true),
        new("tab-armor.armorlocations", "Armor Locations", "tab-armor", WorkspaceSurfaceActionKind.Section, "armorlocations", true, true),

        new("tab-cyberware.cyberwares", "Cyberware/Bioware", "tab-cyberware", WorkspaceSurfaceActionKind.Section, "cyberwares", true, true),
        new("tab-cyberware.foci", "Foci", "tab-cyberware", WorkspaceSurfaceActionKind.Section, "foci", true, true),

        new("tab-vehicles.vehicles", "Vehicles", "tab-vehicles", WorkspaceSurfaceActionKind.Section, "vehicles", true, true),
        new("tab-vehicles.vehiclemods", "Vehicle Mods", "tab-vehicles", WorkspaceSurfaceActionKind.Section, "vehiclemods", true, true),
        new("tab-vehicles.vehiclelocations", "Vehicle Locations", "tab-vehicles", WorkspaceSurfaceActionKind.Section, "vehiclelocations", true, true),

        new("tab-lifestyle.lifestyles", "Lifestyles", "tab-lifestyle", WorkspaceSurfaceActionKind.Section, "lifestyles", true, true),
        new("tab-lifestyle.expenses", "Expenses", "tab-lifestyle", WorkspaceSurfaceActionKind.Section, "expenses", true, true),
        new("tab-lifestyle.sources", "Sources", "tab-lifestyle", WorkspaceSurfaceActionKind.Section, "sources", true, true),

        new("tab-contacts.contacts", "Contacts", "tab-contacts", WorkspaceSurfaceActionKind.Section, "contacts", true, true),
        new("tab-contacts.mentorspirits", "Mentors/Spirits", "tab-contacts", WorkspaceSurfaceActionKind.Section, "mentorspirits", true, true),

        new("tab-notes.metadata", "Save Notes", "tab-notes", WorkspaceSurfaceActionKind.Metadata, "metadata", true, true),
        new("tab-notes.data_exporter", "Export Notes Snapshot", "tab-notes", WorkspaceSurfaceActionKind.Command, "data_exporter", true, true),

        new("tab-calendar.calendar", "Calendar Entries", "tab-calendar", WorkspaceSurfaceActionKind.Section, "calendar", true, true),
        new("tab-calendar.expenses", "Expense Timeline", "tab-calendar", WorkspaceSurfaceActionKind.Section, "expenses", true, true),

        new("tab-improvements.improvements", "Improvements", "tab-improvements", WorkspaceSurfaceActionKind.Section, "improvements", true, true),
        new("tab-improvements.build", "Build Snapshot", "tab-improvements", WorkspaceSurfaceActionKind.Section, "build", true, true),
        new("tab-improvements.progress", "Career Progress", "tab-improvements", WorkspaceSurfaceActionKind.Section, "progress", true, true)
    ];

    public static IReadOnlyList<WorkspaceSurfaceActionDefinition> ForRuleset(string? rulesetId)
    {
        string effectiveRulesetId = RulesetDefaults.Normalize(rulesetId);
        return All
            .Where(action => string.Equals(action.RulesetId, effectiveRulesetId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    public static IReadOnlyList<WorkspaceSurfaceActionDefinition> ForTab(string? tabId)
        => ForTab(tabId, rulesetId: null);

    public static IReadOnlyList<WorkspaceSurfaceActionDefinition> ForTab(string? tabId, string? rulesetId)
    {
        string effectiveTabId = string.IsNullOrWhiteSpace(tabId) ? "tab-info" : tabId;
        WorkspaceSurfaceActionDefinition[] rulesetScopedActions = ForRuleset(rulesetId).ToArray();

        WorkspaceSurfaceActionDefinition[] actions = rulesetScopedActions
            .Where(action => string.Equals(action.TabId, effectiveTabId, StringComparison.Ordinal))
            .ToArray();
        if (actions.Length > 0)
            return actions;

        return rulesetScopedActions
            .Where(action => string.Equals(action.TabId, "tab-info", StringComparison.Ordinal))
            .ToArray();
    }
}
