using Chummer.Application.Characters;
using Chummer.Application.Workspaces;
using Chummer.Contracts.Workspaces;
using Chummer.Core.Characters;
using Chummer.Infrastructure.Workspaces;
using Chummer.Infrastructure.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class WorkspaceServiceTests
{
    [TestMethod]
    public void Import_get_profile_update_and_save_roundtrip()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>15</karma><nuyen>2500</nuyen><created>True</created><newskills><skills><skill><guid>s1</guid><suid>suid1</suid><skillcategory>Combat</skillcategory><isknowledge>False</isknowledge><base>6</base><karma>0</karma></skill></skills></newskills></character>";

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

        var update = workspaceService.UpdateMetadata(imported.Id, new UpdateWorkspaceMetadata("Updated", "Alias", "Notes"));
        Assert.IsTrue(update.Success);
        Assert.AreEqual("Updated", update.Value?.Name);

        var save = workspaceService.Save(imported.Id);
        Assert.IsTrue(save.Success);
        StringAssert.Contains(save.Value ?? string.Empty, "Updated");
    }
}
