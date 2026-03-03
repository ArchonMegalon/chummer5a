using Chummer.Application.Characters;
using Chummer.Contracts.Characters;
using Chummer.Core.Characters;
using Chummer.Infrastructure.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}
