namespace Chummer.Contracts.Presentation;

public static class NavigationTabCatalog
{
    public static readonly IReadOnlyList<NavigationTabDefinition> All =
    [
        new("tab-info", "Info", "character", true, true),
        new("tab-attributes", "Attributes", "character", true, true),
        new("tab-skills", "Skills", "character", true, true),
        new("tab-qualities", "Qualities", "character", true, true),
        new("tab-magician", "Magician", "character", true, true),
        new("tab-adept", "Adept", "character", true, true),
        new("tab-technomancer", "Technomancer", "character", true, true),
        new("tab-combat", "Combat", "character", true, true),
        new("tab-gear", "Gear", "character", true, true),
        new("tab-armor", "Armor", "character", true, true),
        new("tab-cyberware", "Cyberware/Bioware", "character", true, true),
        new("tab-vehicles", "Vehicles", "character", true, true),
        new("tab-lifestyle", "Lifestyle", "character", true, true),
        new("tab-contacts", "Contacts", "character", true, true),
        new("tab-notes", "Notes", "character", true, true),
        new("tab-calendar", "Calendar", "character", true, true),
        new("tab-improvements", "Improvements", "character", true, true)
    ];
}
