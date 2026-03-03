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

    public CharacterProfileSection ParseProfile(CharacterXmlDocument document) => _characterSectionService.ParseProfile(document.Xml);

    public CharacterProgressSection ParseProgress(CharacterXmlDocument document) => _characterSectionService.ParseProgress(document.Xml);

    public CharacterRulesSection ParseRules(CharacterXmlDocument document) => _characterSectionService.ParseRules(document.Xml);

    public CharacterBuildSection ParseBuild(CharacterXmlDocument document) => _characterSectionService.ParseBuild(document.Xml);

    public CharacterMovementSection ParseMovement(CharacterXmlDocument document) => _characterSectionService.ParseMovement(document.Xml);

    public CharacterAwakeningSection ParseAwakening(CharacterXmlDocument document) => _characterSectionService.ParseAwakening(document.Xml);

    public CharacterSkillsSection ParseSkills(CharacterXmlDocument document) => _characterSectionService.ParseSkills(document.Xml);
}
