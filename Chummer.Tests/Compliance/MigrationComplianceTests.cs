using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Compliance;

[TestClass]
public class MigrationComplianceTests
{
    private static readonly Regex SectionMethodRegex = new(@"\bCharacter[A-Za-z0-9_]+\s+Parse([A-Za-z0-9_]+)\(string xml\)", RegexOptions.Compiled);
    private static readonly Regex SectionEndpointRegex = new(@"/api/characters/sections/([a-z0-9]+)", RegexOptions.Compiled);
    private static readonly Regex UiActionRegex = new(@"data-action=""([a-z0-9]+)""", RegexOptions.Compiled);
    private static readonly Regex CommandRegex = new(@"data-command=""([a-z_]+)""", RegexOptions.Compiled);

    private static readonly HashSet<string> RequiredDesktopCommands = new(StringComparer.Ordinal)
    {
        "file",
        "edit",
        "special",
        "tools",
        "windows",
        "help",
        "new_character",
        "open_character",
        "save_character",
        "save_character_as",
        "print_character",
        "export_character",
        "copy",
        "paste",
        "dice_roller",
        "global_settings",
        "character_settings",
        "translator",
        "xml_editor",
        "master_index",
        "character_roster",
        "data_exporter",
        "update",
        "restart"
    };

    [TestMethod]
    public void Section_parsers_are_exposed_as_api_endpoints_and_ui_actions()
    {
        string interfacePath = FindPath("Chummer.Core", "Characters", "ICharacterSectionService.cs");
        string programPath = FindPath("Chummer.Web", "Program.cs");
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");

        string interfaceText = File.ReadAllText(interfacePath);
        string programText = File.ReadAllText(programPath);
        string indexText = File.ReadAllText(indexPath);

        HashSet<string> expectedSections = SectionMethodRegex.Matches(interfaceText)
            .Select(match => ToSectionName(match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> endpointSections = SectionEndpointRegex.Matches(programText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> uiActions = UiActionRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(expectedSections.OrderBy(x => x).ToList(), endpointSections.OrderBy(x => x).ToList(),
            "API endpoint set must match ICharacterSectionService parser set.");

        List<string> missingInUi = expectedSections.Where(section => !uiActions.Contains(section)).OrderBy(x => x).ToList();
        Assert.AreEqual(0, missingInUi.Count, "Missing UI actions for sections: " + string.Join(", ", missingInUi));
    }

    [TestMethod]
    public void Desktop_shell_commands_exist_and_have_handlers()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        HashSet<string> commandIds = CommandRegex.Matches(indexText)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        List<string> missingCommands = RequiredDesktopCommands.Where(command => !commandIds.Contains(command)).OrderBy(x => x).ToList();
        Assert.AreEqual(0, missingCommands.Count, "Missing desktop command buttons: " + string.Join(", ", missingCommands));

        foreach (string command in RequiredDesktopCommands)
        {
            StringAssert.Contains(indexText, $"{command}:", $"Missing command handler for '{command}'.");
        }
    }

    [TestMethod]
    public void Ui_exposes_summary_validate_and_metadata_actions()
    {
        string indexPath = FindPath("Chummer.Web", "wwwroot", "index.html");
        string indexText = File.ReadAllText(indexPath);

        StringAssert.Contains(indexText, "data-action=\"summary\"");
        StringAssert.Contains(indexText, "data-action=\"validate\"");
        StringAssert.Contains(indexText, "data-action=\"metadata\"");

        StringAssert.Contains(indexText, "action === \"summary\"");
        StringAssert.Contains(indexText, "action === \"validate\"");
        StringAssert.Contains(indexText, "action === \"metadata\"");
    }

    private static string ToSectionName(string pascalName)
    {
        return pascalName.ToLowerInvariant();
    }

    private static string FindPath(params string[] parts)
    {
        foreach (string? root in CandidateRoots())
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;

            DirectoryInfo current = new(root);
            while (true)
            {
                string candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
                if (File.Exists(candidate))
                    return candidate;

                if (current.Parent == null)
                    break;

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate file.", Path.Combine(parts));
    }

    private static IEnumerable<string?> CandidateRoots()
    {
        yield return Environment.GetEnvironmentVariable("CHUMMER_REPO_ROOT");
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
        yield return "/src";
    }
}
