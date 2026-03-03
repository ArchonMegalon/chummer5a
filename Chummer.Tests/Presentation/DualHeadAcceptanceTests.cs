using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Chummer.Avalonia;
using Chummer.Blazor;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class DualHeadAcceptanceTests
{
    private static readonly Uri BaseUri = ResolveBaseUri();

    [TestMethod]
    public async Task Avalonia_and_Blazor_overview_flows_show_equivalent_state_after_import()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>15</karma><nuyen>2500</nuyen><created>True</created><gameedition>SR5</gameedition><settings>default.xml</settings><gameplayoption>Standard</gameplayoption><gameplayoptionqualitylimit>25</gameplayoptionqualitylimit><maxnuyen>10</maxnuyen><maxkarma>25</maxkarma><contactmultiplier>3</contactmultiplier><walk>2/1/0</walk><run>4/0/0</run><sprint>2/1/0</sprint><walkalt>2/1/0</walkalt><runalt>4/0/0</runalt><sprintalt>2/1/0</sprintalt><magenabled>False</magenabled><resenabled>False</resenabled><depenabled>False</depenabled><newskills><skills><skill><guid>s1</guid><suid>suid1</suid><skillcategory>Combat</skillcategory><isknowledge>False</isknowledge><base>6</base><karma>0</karma><specs><spec><name>Semi-Automatics</name></spec></specs></skill></skills></newskills></character>";

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.ImportAsync(xml, CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.ImportAsync(xml, CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.IsNotNull(avaloniaState.WorkspaceId);
        Assert.IsNotNull(blazorState.WorkspaceId);
        Assert.AreEqual(avaloniaState.Profile?.Name, blazorState.Profile?.Name);
        Assert.AreEqual(avaloniaState.Profile?.Alias, blazorState.Profile?.Alias);
        Assert.AreEqual(avaloniaState.Progress?.Karma, blazorState.Progress?.Karma);
        Assert.AreEqual(avaloniaState.Skills?.Count, blazorState.Skills?.Count);
        Assert.AreEqual(avaloniaState.Rules?.GameEdition, blazorState.Rules?.GameEdition);
    }

    [TestMethod]
    public async Task Avalonia_and_Blazor_metadata_save_roundtrip_match()
    {
        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>15</karma><nuyen>2500</nuyen><created>True</created><gameedition>SR5</gameedition><settings>default.xml</settings><gameplayoption>Standard</gameplayoption><gameplayoptionqualitylimit>25</gameplayoptionqualitylimit><maxnuyen>10</maxnuyen><maxkarma>25</maxkarma><contactmultiplier>3</contactmultiplier><walk>2/1/0</walk><run>4/0/0</run><sprint>2/1/0</sprint><walkalt>2/1/0</walkalt><runalt>4/0/0</runalt><sprintalt>2/1/0</sprintalt><magenabled>False</magenabled><resenabled>False</resenabled><depenabled>False</depenabled><newskills><skills><skill><guid>s1</guid><suid>suid1</suid><skillcategory>Combat</skillcategory><isknowledge>False</isknowledge><base>6</base><karma>0</karma><specs><spec><name>Semi-Automatics</name></spec></specs></skill></skills></newskills></character>";
        UpdateWorkspaceMetadata update = new("Updated Name", "Updated Alias", "Updated Notes");

        CharacterOverviewState avaloniaState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            using var adapter = new CharacterOverviewViewModelAdapter(presenter);
            await adapter.ImportAsync(xml, CancellationToken.None);
            await presenter.UpdateMetadataAsync(update, CancellationToken.None);
            await presenter.SaveAsync(CancellationToken.None);
            avaloniaState = adapter.State;
        }

        CharacterOverviewState blazorState;
        using (HttpClient http = CreateClient())
        {
            var presenter = new CharacterOverviewPresenter(new HttpChummerClient(http));
            CharacterOverviewState callbackState = CharacterOverviewState.Empty;
            using var bridge = new CharacterOverviewStateBridge(presenter, state => callbackState = state);
            await bridge.ImportAsync(xml, CancellationToken.None);
            await presenter.UpdateMetadataAsync(update, CancellationToken.None);
            await presenter.SaveAsync(CancellationToken.None);
            blazorState = callbackState.WorkspaceId is null ? bridge.Current : callbackState;
        }

        Assert.AreEqual("Updated Name", avaloniaState.Profile?.Name);
        Assert.AreEqual("Updated Alias", avaloniaState.Profile?.Alias);
        Assert.AreEqual("Updated Name", blazorState.Profile?.Name);
        Assert.AreEqual("Updated Alias", blazorState.Profile?.Alias);
        Assert.IsTrue(avaloniaState.HasSavedWorkspace);
        Assert.IsTrue(blazorState.HasSavedWorkspace);
    }

    private static HttpClient CreateClient()
    {
        return new HttpClient
        {
            BaseAddress = BaseUri,
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private static Uri ResolveBaseUri()
    {
        string? raw = Environment.GetEnvironmentVariable("CHUMMER_WEB_BASE_URL");
        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://chummer-web:8080";

        if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri? uri))
            throw new InvalidOperationException($"Invalid CHUMMER_WEB_BASE_URL: '{raw}'");

        return uri;
    }
}
