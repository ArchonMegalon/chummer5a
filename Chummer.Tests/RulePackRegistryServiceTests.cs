#nullable enable annotations

using System.Collections.Generic;
using System.IO;
using Chummer.Application.Content;
using Chummer.Contracts.Content;
using Chummer.Contracts.Owners;
using Chummer.Contracts.Rulesets;
using Chummer.Rulesets.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class RulePackRegistryServiceTests
{
    [TestMethod]
    public void Overlay_registry_service_projects_overlays_into_rulepack_registry_entries()
    {
        ContentOverlayCatalog catalog = new(
            BaseDataPath: "/app/data",
            BaseLanguagePath: "/app/lang",
            Overlays:
            [
                new ContentOverlayPack(
                    Id: "house-rules",
                    Name: "House Rules",
                    RootPath: "/packs/house-rules",
                    DataPath: "/packs/house-rules/data",
                    LanguagePath: "/packs/house-rules/lang",
                    Priority: 50,
                    Enabled: true,
                    Mode: ContentOverlayModes.MergeCatalog,
                    Description: "Campaign overlay.")
            ]);
        OverlayRulePackRegistryService service = new(
            new ContentOverlayCatalogServiceStub(catalog),
            new RulesetSelectionPolicyStub());

        IReadOnlyList<RulePackRegistryEntry> entries = service.List(OwnerScope.LocalSingleUser, RulesetDefaults.Sr5);

        Assert.HasCount(1, entries);
        Assert.AreEqual("house-rules", entries[0].Manifest.PackId);
        Assert.AreEqual(RulesetDefaults.Sr5, entries[0].Manifest.Targets[0]);
        Assert.AreEqual(RulePackPublicationStatuses.Published, entries[0].Publication.PublicationStatus);
        Assert.AreEqual(RulePackReviewStates.NotRequired, entries[0].Publication.Review.State);
    }

    [TestMethod]
    public void Overlay_registry_service_returns_null_for_unknown_pack()
    {
        OverlayRulePackRegistryService service = new(
            new ContentOverlayCatalogServiceStub(new ContentOverlayCatalog("/app/data", "/app/lang", [])),
            new RulesetSelectionPolicyStub());

        RulePackRegistryEntry? entry = service.Get(OwnerScope.LocalSingleUser, "missing-pack", RulesetDefaults.Sr5);

        Assert.IsNull(entry);
    }

    private sealed class ContentOverlayCatalogServiceStub : IContentOverlayCatalogService
    {
        private readonly ContentOverlayCatalog _catalog;

        public ContentOverlayCatalogServiceStub(ContentOverlayCatalog catalog)
        {
            _catalog = catalog;
        }

        public ContentOverlayCatalog GetCatalog() => _catalog;

        public IReadOnlyList<string> GetDataDirectories() => [_catalog.BaseDataPath];

        public IReadOnlyList<string> GetLanguageDirectories() => [_catalog.BaseLanguagePath];

        public string ResolveDataFile(string fileName) => Path.Combine(_catalog.BaseDataPath, fileName);
    }

    private sealed class RulesetSelectionPolicyStub : IRulesetSelectionPolicy
    {
        public string GetDefaultRulesetId() => RulesetDefaults.Sr5;
    }
}
