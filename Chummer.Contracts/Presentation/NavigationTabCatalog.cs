using Chummer.Contracts.Rulesets;

namespace Chummer.Contracts.Presentation;

public static class NavigationTabCatalog
{
    public static readonly IReadOnlyList<NavigationTabDefinition> All =
    [
        new("tab-info", "Info", "profile", "character", true, true),
        new("tab-attributes", "Attributes", "attributes", "character", true, true),
        new("tab-skills", "Skills", "skills", "character", true, true),
        new("tab-qualities", "Qualities", "qualities", "character", true, true),
        new("tab-magician", "Magician", "spells", "character", true, true),
        new("tab-adept", "Adept", "powers", "character", true, true),
        new("tab-technomancer", "Technomancer", "complexforms", "character", true, true),
        new("tab-combat", "Combat", "weapons", "character", true, true),
        new("tab-gear", "Gear", "gear", "character", true, true),
        new("tab-armor", "Armor", "armors", "character", true, true),
        new("tab-cyberware", "Cyberware/Bioware", "cyberwares", "character", true, true),
        new("tab-vehicles", "Vehicles", "vehicles", "character", true, true),
        new("tab-lifestyle", "Lifestyle", "lifestyles", "character", true, true),
        new("tab-contacts", "Contacts", "contacts", "character", true, true),
        new("tab-rules", "Rules", "rules", "character", true, true),
        new("tab-notes", "Notes", "profile", "character", true, true),
        new("tab-calendar", "Calendar", "calendar", "character", true, true),
        new("tab-improvements", "Improvements", "improvements", "character", true, true)
    ];

    public static IReadOnlyList<NavigationTabDefinition> ForRuleset(string? rulesetId)
    {
        string effectiveRulesetId = RulesetDefaults.Normalize(rulesetId);
        return All
            .Where(tab => string.Equals(tab.RulesetId, effectiveRulesetId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
