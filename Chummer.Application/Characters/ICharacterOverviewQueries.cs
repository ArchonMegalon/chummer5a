using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterOverviewQueries
{
    CharacterProfileSection ParseProfile(string xml);

    CharacterProgressSection ParseProgress(string xml);

    CharacterRulesSection ParseRules(string xml);

    CharacterBuildSection ParseBuild(string xml);

    CharacterMovementSection ParseMovement(string xml);

    CharacterAwakeningSection ParseAwakening(string xml);

    CharacterSkillsSection ParseSkills(string xml);
}
