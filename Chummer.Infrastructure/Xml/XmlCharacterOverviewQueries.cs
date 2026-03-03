using Chummer.Application.Characters;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterOverviewQueries : ICharacterOverviewQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterOverviewQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public CharacterProfileSection ParseProfile(string xml) => _characterSectionService.ParseProfile(xml);

    public CharacterProgressSection ParseProgress(string xml) => _characterSectionService.ParseProgress(xml);

    public CharacterRulesSection ParseRules(string xml) => _characterSectionService.ParseRules(xml);

    public CharacterBuildSection ParseBuild(string xml) => _characterSectionService.ParseBuild(xml);

    public CharacterMovementSection ParseMovement(string xml) => _characterSectionService.ParseMovement(xml);

    public CharacterAwakeningSection ParseAwakening(string xml) => _characterSectionService.ParseAwakening(xml);

    public CharacterSkillsSection ParseSkills(string xml) => _characterSectionService.ParseSkills(xml);
}
