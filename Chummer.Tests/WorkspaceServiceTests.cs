using System;
using System.Collections.Generic;
using System.Linq;
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
    public void Import_does_not_create_workspace_when_summary_parse_fails()
    {
        TrackingWorkspaceStore store = new();
        WorkspaceService workspaceService = CreateWorkspaceService(
            store,
            new ThrowingCharacterFileQueries(),
            new NoopCharacterSectionQueries(),
            new NoopCharacterMetadataCommands());

        Assert.ThrowsExactly<FormatException>(() => workspaceService.Import(new WorkspaceImportDocument(
            "<character><name>Broken</name></character>",
            WorkspaceDocumentFormat.Chum5Xml)));
        Assert.AreEqual(0, store.CreateCallCount);
    }

    [TestMethod]
    public void Import_get_profile_update_and_save_roundtrip()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>15</karma><nuyen>2500</nuyen><created>True</created><gameedition>SR5</gameedition><settings>default.xml</settings><gameplayoption>Standard</gameplayoption><gameplayoptionqualitylimit>25</gameplayoptionqualitylimit><maxnuyen>10</maxnuyen><maxkarma>25</maxkarma><contactmultiplier>3</contactmultiplier><walk>2/1/0</walk><run>4/0/0</run><sprint>2/1/0</sprint><walkalt>2/1/0</walkalt><runalt>4/0/0</runalt><sprintalt>2/1/0</sprintalt><magenabled>False</magenabled><resenabled>False</resenabled><depenabled>False</depenabled><newskills><skills><skill><guid>s1</guid><suid>suid1</suid><skillcategory>Combat</skillcategory><isknowledge>False</isknowledge><base>6</base><karma>0</karma></skill></skills></newskills></character>";

        IWorkspaceStore store = new InMemoryWorkspaceStore();
        ICharacterFileQueries fileQueries = new XmlCharacterFileQueries(new CharacterFileService());
        ICharacterSectionQueries sectionQueries = new XmlCharacterSectionQueries(new CharacterSectionService());
        ICharacterMetadataCommands metadataCommands = new XmlCharacterMetadataCommands(new CharacterFileService());
        WorkspaceService workspaceService = CreateWorkspaceService(store, fileQueries, sectionQueries, metadataCommands);

        WorkspaceImportResult imported = workspaceService.Import(new WorkspaceImportDocument(xml, WorkspaceDocumentFormat.Chum5Xml, RulesetId: "SR6"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(imported.Id.Value));
        Assert.AreEqual("Neo", imported.Summary.Name);
        Assert.AreEqual("sr6", imported.RulesetId);
        IReadOnlyList<WorkspaceListItem> listed = workspaceService.List();
        Assert.IsTrue(listed.Any(item => string.Equals(item.Id.Value, imported.Id.Value, StringComparison.Ordinal)));
        Assert.AreEqual("sr6", listed.First(item => string.Equals(item.Id.Value, imported.Id.Value, StringComparison.Ordinal)).RulesetId);

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
        Assert.AreEqual(imported.Id, save.Value?.Id);
        Assert.IsGreaterThan(0, save.Value?.DocumentLength ?? 0);
        Assert.AreEqual("sr6", save.Value?.RulesetId);

        var download = workspaceService.Download(imported.Id);
        Assert.IsTrue(download.Success);
        Assert.AreEqual("sr6", download.Value?.RulesetId);

        bool closed = workspaceService.Close(imported.Id);
        Assert.IsTrue(closed);
        Assert.IsFalse(workspaceService.List().Any(item => string.Equals(item.Id.Value, imported.Id.Value, StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Import_accepts_xml_with_utf8_bom_prefix()
    {
        const string xml = "\uFEFF<character><name>BOM Runner</name><alias>BOM</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>0</karma><nuyen>0</nuyen><created>True</created></character>";

        IWorkspaceStore store = new InMemoryWorkspaceStore();
        ICharacterFileQueries fileQueries = new XmlCharacterFileQueries(new CharacterFileService());
        ICharacterSectionQueries sectionQueries = new XmlCharacterSectionQueries(new CharacterSectionService());
        ICharacterMetadataCommands metadataCommands = new XmlCharacterMetadataCommands(new CharacterFileService());
        WorkspaceService workspaceService = CreateWorkspaceService(store, fileQueries, sectionQueries, metadataCommands);

        WorkspaceImportResult imported = workspaceService.Import(new WorkspaceImportDocument(xml, WorkspaceDocumentFormat.Chum5Xml));
        Assert.IsFalse(string.IsNullOrWhiteSpace(imported.Id.Value));
        Assert.AreEqual("BOM Runner", imported.Summary.Name);
        Assert.AreEqual("BOM", imported.Summary.Alias);
    }

    [TestMethod]
    public void List_honors_maxCount_parameter()
    {
        const string xmlTemplate = "<character><name>{0}</name><alias>{0}</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>0</karma><nuyen>0</nuyen><created>True</created></character>";
        IWorkspaceStore store = new InMemoryWorkspaceStore();
        ICharacterFileQueries fileQueries = new XmlCharacterFileQueries(new CharacterFileService());
        ICharacterSectionQueries sectionQueries = new XmlCharacterSectionQueries(new CharacterSectionService());
        ICharacterMetadataCommands metadataCommands = new XmlCharacterMetadataCommands(new CharacterFileService());
        WorkspaceService workspaceService = CreateWorkspaceService(store, fileQueries, sectionQueries, metadataCommands);

        workspaceService.Import(new WorkspaceImportDocument(string.Format(xmlTemplate, "One"), WorkspaceDocumentFormat.Chum5Xml));
        workspaceService.Import(new WorkspaceImportDocument(string.Format(xmlTemplate, "Two"), WorkspaceDocumentFormat.Chum5Xml));
        workspaceService.Import(new WorkspaceImportDocument(string.Format(xmlTemplate, "Three"), WorkspaceDocumentFormat.Chum5Xml));

        IReadOnlyList<WorkspaceListItem> fullList = workspaceService.List();
        IReadOnlyList<WorkspaceListItem> cappedList = workspaceService.List(maxCount: 2);

        Assert.HasCount(3, fullList);
        Assert.HasCount(2, cappedList);
        Assert.IsTrue(cappedList.All(item => fullList.Any(full => string.Equals(full.Id.Value, item.Id.Value, StringComparison.Ordinal))));
    }

    private sealed class TrackingWorkspaceStore : IWorkspaceStore
    {
        public int CreateCallCount { get; private set; }

        public CharacterWorkspaceId Create(WorkspaceDocument document)
        {
            CreateCallCount++;
            return new CharacterWorkspaceId(Guid.NewGuid().ToString("N"));
        }

        public bool TryGet(CharacterWorkspaceId id, out WorkspaceDocument document)
        {
            document = null!;
            return false;
        }

        public IReadOnlyList<WorkspaceStoreEntry> List()
        {
            return [];
        }

        public void Save(CharacterWorkspaceId id, WorkspaceDocument document)
        {
        }

        public bool Delete(CharacterWorkspaceId id)
        {
            return false;
        }
    }

    private sealed class ThrowingCharacterFileQueries : ICharacterFileQueries
    {
        public CharacterFileSummary ParseSummary(CharacterDocument document)
        {
            throw new FormatException("Malformed summary payload.");
        }

        public CharacterValidationResult Validate(CharacterDocument document)
        {
            return new CharacterValidationResult(false, []);
        }
    }

    private sealed class NoopCharacterSectionQueries : ICharacterSectionQueries
    {
        public object ParseSection(string sectionId, CharacterDocument document)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class NoopCharacterMetadataCommands : ICharacterMetadataCommands
    {
        public UpdateCharacterMetadataResult UpdateMetadata(UpdateCharacterMetadataCommand command)
        {
            throw new NotSupportedException();
        }
    }

    private static WorkspaceService CreateWorkspaceService(
        IWorkspaceStore workspaceStore,
        ICharacterFileQueries fileQueries,
        ICharacterSectionQueries sectionQueries,
        ICharacterMetadataCommands metadataCommands)
    {
        IRulesetWorkspaceCodecResolver resolver = new RulesetWorkspaceCodecResolver(
        [
            new Sr5WorkspaceCodec(
                fileQueries,
                sectionQueries,
                metadataCommands)
        ]);
        return new WorkspaceService(workspaceStore, resolver);
    }
}
