using Chummer.Application.Characters;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterMagicResonanceQueries : ICharacterMagicResonanceQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterMagicResonanceQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public CharacterSpellsSection ParseSpells(CharacterXmlDocument document) => _characterSectionService.ParseSpells(document.Xml);

    public CharacterPowersSection ParsePowers(CharacterXmlDocument document) => _characterSectionService.ParsePowers(document.Xml);

    public CharacterComplexFormsSection ParseComplexForms(CharacterXmlDocument document) => _characterSectionService.ParseComplexForms(document.Xml);

    public CharacterSpiritsSection ParseSpirits(CharacterXmlDocument document) => _characterSectionService.ParseSpirits(document.Xml);

    public CharacterFociSection ParseFoci(CharacterXmlDocument document) => _characterSectionService.ParseFoci(document.Xml);

    public CharacterAiProgramsSection ParseAiPrograms(CharacterXmlDocument document) => _characterSectionService.ParseAiPrograms(document.Xml);

    public CharacterMartialArtsSection ParseMartialArts(CharacterXmlDocument document) => _characterSectionService.ParseMartialArts(document.Xml);

    public CharacterMetamagicsSection ParseMetamagics(CharacterXmlDocument document) => _characterSectionService.ParseMetamagics(document.Xml);

    public CharacterArtsSection ParseArts(CharacterXmlDocument document) => _characterSectionService.ParseArts(document.Xml);

    public CharacterInitiationGradesSection ParseInitiationGrades(CharacterXmlDocument document) => _characterSectionService.ParseInitiationGrades(document.Xml);

    public CharacterCritterPowersSection ParseCritterPowers(CharacterXmlDocument document) => _characterSectionService.ParseCritterPowers(document.Xml);

    public CharacterMentorSpiritsSection ParseMentorSpirits(CharacterXmlDocument document) => _characterSectionService.ParseMentorSpirits(document.Xml);
}
