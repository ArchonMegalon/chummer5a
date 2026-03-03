using Chummer.Application.Characters;
using Chummer.Contracts.Characters;
using Chummer.Infrastructure.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Chummer.Tests;

[TestClass]
public class CharacterApplicationPortsTests
{
    [TestMethod]
    public void File_queries_parse_summary_from_xml()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>15</karma><nuyen>2500</nuyen><created>True</created></character>";

        ICharacterFileQueries queries = new XmlCharacterFileQueries(new CharacterFileService());
        CharacterFileSummary summary = queries.ParseSummary(xml);

        Assert.AreEqual("Neo", summary.Name);
        Assert.AreEqual("The One", summary.Alias);
        Assert.AreEqual(15m, summary.Karma);
    }

    [TestMethod]
    public void Metadata_commands_update_xml_and_return_summary()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>15</karma><nuyen>2500</nuyen><created>True</created></character>";

        ICharacterMetadataCommands commands = new XmlCharacterMetadataCommands(new CharacterFileService());
        UpdateCharacterMetadataResult result = commands.UpdateMetadata(new UpdateCharacterMetadataCommand(
            Xml: xml,
            Name: "Updated",
            Alias: "Alias",
            Notes: "Hello"));

        Assert.AreEqual("Updated", result.Summary.Name);
        Assert.AreEqual("Alias", result.Summary.Alias);
        StringAssert.Contains(result.UpdatedXml, "<notes>Hello</notes>");
    }

    [TestMethod]
    public void Section_queries_route_to_expected_section_parser()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><created>True</created><adept>False</adept><magician>False</magician><technomancer>False</technomancer><ai>False</ai></character>";

        ICharacterSectionQueries queries = new XmlCharacterSectionQueries(new CharacterSectionService());
        object section = queries.ParseSection("profile", xml);

        Assert.IsInstanceOfType<CharacterProfileSection>(section);
        CharacterProfileSection profile = (CharacterProfileSection)section;
        Assert.AreEqual("Neo", profile.Name);
    }

    [TestMethod]
    public void Feature_slice_queries_delegate_to_character_section_service()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><created>True</created><adept>False</adept><magician>False</magician><technomancer>False</technomancer><ai>False</ai></character>";

        ICharacterSectionService sectionService = new CharacterSectionService();
        ICharacterOverviewQueries overview = new XmlCharacterOverviewQueries(sectionService);
        ICharacterStatsQueries stats = new XmlCharacterStatsQueries(sectionService);
        ICharacterInventoryQueries inventory = new XmlCharacterInventoryQueries(sectionService);
        ICharacterMagicResonanceQueries magic = new XmlCharacterMagicResonanceQueries(sectionService);
        ICharacterSocialNarrativeQueries social = new XmlCharacterSocialNarrativeQueries(sectionService);

        Assert.IsNotNull(overview.ParseProfile(xml));
        Assert.IsNotNull(stats.ParseAttributes(xml));
        Assert.IsNotNull(inventory.ParseInventory(xml));
        Assert.IsNotNull(magic.ParseSpells(xml));
        Assert.IsNotNull(social.ParseQualities(xml));
    }

    [TestMethod]
    public void Section_queries_parse_profile_for_blue_sample_character()
    {
        string xml = File.ReadAllText(FindTestFilePath("BLUE.chum5"));
        ICharacterSectionService sectionService = new CharacterSectionService();

        ICharacterSectionQueries queries = new XmlCharacterSectionQueries(
            new XmlCharacterOverviewQueries(sectionService),
            new XmlCharacterStatsQueries(sectionService),
            new XmlCharacterInventoryQueries(sectionService),
            new XmlCharacterMagicResonanceQueries(sectionService),
            new XmlCharacterSocialNarrativeQueries(sectionService));

        object section = queries.ParseSection("profile", xml);
        Assert.IsInstanceOfType<CharacterProfileSection>(section);
    }

    private static string FindTestFilePath(string fileName)
    {
        string? root = Environment.GetEnvironmentVariable("CHUMMER_REPO_ROOT");
        string[] candidates =
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Chummer.Tests", "TestFiles", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", fileName),
            Path.Combine(AppContext.BaseDirectory, "TestFiles", fileName),
            Path.Combine("/src", "Chummer.Tests", "TestFiles", fileName),
            string.IsNullOrWhiteSpace(root) ? string.Empty : Path.Combine(root, "Chummer.Tests", "TestFiles", fileName)
        };

        string? match = candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
        if (match is null)
            throw new FileNotFoundException("Could not locate test file.", fileName);

        return match;
    }
}
