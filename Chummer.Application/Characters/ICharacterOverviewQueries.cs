using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterOverviewQueries
{
    CharacterProfileSection ParseProfile(CharacterXmlDocument document);

    CharacterProgressSection ParseProgress(CharacterXmlDocument document);

    CharacterRulesSection ParseRules(CharacterXmlDocument document);

    CharacterBuildSection ParseBuild(CharacterXmlDocument document);

    CharacterMovementSection ParseMovement(CharacterXmlDocument document);

    CharacterAwakeningSection ParseAwakening(CharacterXmlDocument document);

    CharacterSkillsSection ParseSkills(CharacterXmlDocument document);
}
