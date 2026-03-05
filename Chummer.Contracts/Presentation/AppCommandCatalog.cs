using Chummer.Contracts.Rulesets;

namespace Chummer.Contracts.Presentation;

public static class AppCommandCatalog
{
    public static readonly IReadOnlyList<AppCommandDefinition> All =
    [
        new("file", "command.file", "menu", false, true),
        new("edit", "command.edit", "menu", false, true),
        new("special", "command.special", "menu", false, true),
        new("tools", "command.tools", "menu", false, true),
        new("windows", "command.windows", "menu", false, true),
        new("help", "command.help", "menu", false, true),
        new("new_character", "command.new_character", "file", false, true),
        new("new_critter", "command.new_critter", "file", false, true),
        new("open_character", "command.open_character", "file", false, true),
        new("open_for_printing", "command.open_for_printing", "file", false, true),
        new("open_for_export", "command.open_for_export", "file", false, true),
        new("save_character", "command.save_character", "file", true, true),
        new("save_character_as", "command.save_character_as", "file", true, true),
        new("print_character", "command.print_character", "file", true, true),
        new("print_multiple", "command.print_multiple", "file", false, true),
        new("print_setup", "command.print_setup", "file", false, true),
        new("export_character", "command.export_character", "file", true, true),
        new("copy", "command.copy", "edit", true, true),
        new("paste", "command.paste", "edit", true, true),
        new("dice_roller", "command.dice_roller", "tools", false, true),
        new("global_settings", "command.global_settings", "tools", false, true),
        new("switch_ruleset", "command.switch_ruleset", "tools", false, true),
        new("character_settings", "command.character_settings", "tools", true, true),
        new("translator", "command.translator", "tools", false, true),
        new("hero_lab_importer", "command.hero_lab_importer", "tools", false, true),
        new("xml_editor", "command.xml_editor", "tools", true, true),
        new("master_index", "command.master_index", "tools", false, true),
        new("character_roster", "command.character_roster", "tools", false, true),
        new("data_exporter", "command.data_exporter", "tools", true, true),
        new("report_bug", "command.report_bug", "help", false, true),
        new("new_window", "command.new_window", "windows", false, true),
        new("close_window", "command.close_window", "windows", false, true),
        new("close_all", "command.close_all", "windows", false, true),
        new("wiki", "command.wiki", "help", false, true),
        new("discord", "command.discord", "help", false, true),
        new("revision_history", "command.revision_history", "help", false, true),
        new("dumpshock", "command.dumpshock", "help", false, true),
        new("about", "command.about", "help", false, true),
        new("update", "command.update", "help", false, true),
        new("restart", "command.restart", "help", false, true)
    ];

    public static IReadOnlyList<AppCommandDefinition> ForRuleset(string? rulesetId)
    {
        string effectiveRulesetId = RulesetDefaults.Normalize(rulesetId);
        return All
            .Where(command => string.Equals(command.RulesetId, effectiveRulesetId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
