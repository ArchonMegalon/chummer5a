using System.Threading;
using System.Threading.Tasks;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;
using Chummer.Presentation;
using Chummer.Presentation.Overview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public class CharacterOverviewPresenterTests
{
    [TestMethod]
    public async Task LoadAsync_populates_profile_progress_and_skills()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);

        Assert.IsFalse(presenter.State.IsBusy);
        Assert.IsNull(presenter.State.Error);
        Assert.IsNotNull(presenter.State.Profile);
        Assert.IsNotNull(presenter.State.Progress);
        Assert.IsNotNull(presenter.State.Skills);
        Assert.AreEqual("BLUE", presenter.State.Profile.Alias);
    }

    [TestMethod]
    public async Task UpdateMetadataAsync_requires_loaded_workspace()
    {
        var presenter = new CharacterOverviewPresenter(new FakeChummerClient());

        await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Name", "Alias", "Notes"), CancellationToken.None);

        Assert.AreEqual("No workspace loaded.", presenter.State.Error);
    }

    [TestMethod]
    public async Task UpdateMetadataAsync_updates_profile_when_client_succeeds()
    {
        var client = new FakeChummerClient();
        var presenter = new CharacterOverviewPresenter(client);

        await presenter.LoadAsync(new CharacterWorkspaceId("ws-1"), CancellationToken.None);
        await presenter.UpdateMetadataAsync(new UpdateWorkspaceMetadata("Updated", "Alias", "Notes"), CancellationToken.None);

        Assert.IsNull(presenter.State.Error);
        Assert.AreEqual("Updated", presenter.State.Profile?.Name);
    }

    private sealed class FakeChummerClient : IChummerClient
    {
        public Task<CharacterProfileSection> GetProfileAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterProfileSection profile = new(
                Name: "Troy Simmons",
                Alias: "BLUE",
                PlayerName: string.Empty,
                Metatype: "Ork",
                Metavariant: string.Empty,
                Sex: string.Empty,
                Age: string.Empty,
                Height: string.Empty,
                Weight: string.Empty,
                Hair: string.Empty,
                Eyes: string.Empty,
                Skin: string.Empty,
                Concept: string.Empty,
                Description: string.Empty,
                Background: string.Empty,
                CreatedVersion: string.Empty,
                AppVersion: string.Empty,
                BuildMethod: "SumtoTen",
                GameplayOption: string.Empty,
                Created: true,
                Adept: false,
                Magician: false,
                Technomancer: false,
                AI: false,
                MainMugshotIndex: 0,
                MugshotCount: 0);

            return Task.FromResult(profile);
        }

        public Task<CharacterProgressSection> GetProgressAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterProgressSection progress = new(
                Karma: 12m,
                Nuyen: 5000m,
                StartingNuyen: 0m,
                StreetCred: 1,
                Notoriety: 0,
                PublicAwareness: 0,
                BurntStreetCred: 0,
                BuildKarma: 0,
                TotalAttributes: 0,
                TotalSpecial: 0,
                PhysicalCmFilled: 0,
                StunCmFilled: 0,
                TotalEssence: 6m,
                InitiateGrade: 0,
                SubmersionGrade: 0,
                MagEnabled: false,
                ResEnabled: false,
                DepEnabled: false);

            return Task.FromResult(progress);
        }

        public Task<CharacterSkillsSection> GetSkillsAsync(CharacterWorkspaceId id, CancellationToken ct)
        {
            CharacterSkillsSection skills = new(
                Count: 1,
                KnowledgeCount: 0,
                Skills:
                [
                    new CharacterSkillSummary(
                        Guid: "1",
                        Suid: string.Empty,
                        Category: "Combat",
                        IsKnowledge: false,
                        BaseValue: 6,
                        KarmaValue: 0,
                        Specializations: ["Semi-Automatics"])
                ]);

            return Task.FromResult(skills);
        }

        public Task<CommandResult<CharacterProfileSection>> UpdateMetadataAsync(CharacterWorkspaceId id, UpdateWorkspaceMetadata command, CancellationToken ct)
        {
            CharacterProfileSection updated = new(
                Name: command.Name ?? "Troy Simmons",
                Alias: command.Alias ?? "BLUE",
                PlayerName: string.Empty,
                Metatype: "Ork",
                Metavariant: string.Empty,
                Sex: string.Empty,
                Age: string.Empty,
                Height: string.Empty,
                Weight: string.Empty,
                Hair: string.Empty,
                Eyes: string.Empty,
                Skin: string.Empty,
                Concept: string.Empty,
                Description: string.Empty,
                Background: string.Empty,
                CreatedVersion: string.Empty,
                AppVersion: string.Empty,
                BuildMethod: "SumtoTen",
                GameplayOption: string.Empty,
                Created: true,
                Adept: false,
                Magician: false,
                Technomancer: false,
                AI: false,
                MainMugshotIndex: 0,
                MugshotCount: 0);

            return Task.FromResult(new CommandResult<CharacterProfileSection>(
                Success: true,
                Value: updated,
                Error: null));
        }
    }
}
