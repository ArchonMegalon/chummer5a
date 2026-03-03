using Chummer.Contracts.Characters;

namespace Chummer.Presentation.Overview;

public sealed record CharacterOverviewState(
    bool IsBusy,
    string? Error,
    CharacterProfileSection? Profile,
    CharacterProgressSection? Progress,
    CharacterSkillsSection? Skills)
{
    public static CharacterOverviewState Empty { get; } = new(
        IsBusy: false,
        Error: null,
        Profile: null,
        Progress: null,
        Skills: null);
}
