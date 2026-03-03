using Chummer.Application.Characters;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;
using Chummer.Infrastructure.Xml;
using Chummer.Infrastructure.Workspaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class WorkspaceServiceTests
{
    [TestMethod]
    public void Import_get_profile_update_and_save_roundtrip()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>15</karma><nuyen>2500</nuyen><created>True</created><gameedition>SR5</gameedition><settings>default.xml</settings><gameplayoption>Standard</gameplayoption><gameplayoptionqualitylimit>25</gameplayoptionqualitylimit><maxnuyen>10</maxnuyen><maxkarma>25</maxkarma><contactmultiplier>3</contactmultiplier><walk>2/1/0</walk><run>4/0/0</run><sprint>2/1/0</sprint><walkalt>2/1/0</walkalt><runalt>4/0/0</runalt><sprintalt>2/1/0</sprintalt><magenabled>False</magenabled><resenabled>False</resenabled><depenabled>False</depenabled><newskills><skills><skill><guid>s1</guid><suid>suid1</suid><skillcategory>Combat</skillcategory><isknowledge>False</isknowledge><base>6</base><karma>0</karma></skill></skills></newskills></character>";

        IWorkspaceStore store = new InMemoryWorkspaceStore();
        ICharacterFileQueries fileQueries = new XmlCharacterFileQueries(new CharacterFileService());
        ICharacterSectionQueries sectionQueries = new XmlCharacterSectionQueries(new CharacterSectionService());
        ICharacterMetadataCommands metadataCommands = new XmlCharacterMetadataCommands(new CharacterFileService());
        IWorkspaceService workspaceService = new WorkspaceService(store, fileQueries, sectionQueries, metadataCommands);

        WorkspaceImportResult imported = workspaceService.Import(xml);
        Assert.IsFalse(string.IsNullOrWhiteSpace(imported.Id.Value));
        Assert.AreEqual("Neo", imported.Summary.Name);

        var profile = workspaceService.GetProfile(imported.Id);
        Assert.IsNotNull(profile);
        Assert.AreEqual("Neo", profile.Name);

        var rules = workspaceService.GetRules(imported.Id);
        Assert.IsNotNull(rules);
        Assert.AreEqual("SR5", rules.GameEdition);

        var movement = workspaceService.GetMovement(imported.Id);
        Assert.IsNotNull(movement);
        Assert.AreEqual("2/1/0", movement.Walk);

        var build = workspaceService.GetBuild(imported.Id);
        Assert.IsNotNull(build);
        Assert.AreEqual("Priority", build.BuildMethod);

        var awakening = workspaceService.GetAwakening(imported.Id);
        Assert.IsNotNull(awakening);
        Assert.IsFalse(awakening.MagEnabled);

        var section = workspaceService.GetSection(imported.Id, "skills") as CharacterSkillsSection;
        Assert.IsNotNull(section);
        Assert.AreEqual(1, section.Count);

        var update = workspaceService.UpdateMetadata(imported.Id, new UpdateWorkspaceMetadata("Updated", "Alias", "Notes"));
        Assert.IsTrue(update.Success);
        Assert.AreEqual("Updated", update.Value?.Name);

        var save = workspaceService.Save(imported.Id);
        Assert.IsTrue(save.Success);
        StringAssert.Contains(save.Value ?? string.Empty, "Updated");
    }
}
