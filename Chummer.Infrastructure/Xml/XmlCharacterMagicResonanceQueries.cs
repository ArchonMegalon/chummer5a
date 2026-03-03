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

    public CharacterSpellsSection ParseSpells(string xml) => _characterSectionService.ParseSpells(xml);

    public CharacterPowersSection ParsePowers(string xml) => _characterSectionService.ParsePowers(xml);

    public CharacterComplexFormsSection ParseComplexForms(string xml) => _characterSectionService.ParseComplexForms(xml);

    public CharacterSpiritsSection ParseSpirits(string xml) => _characterSectionService.ParseSpirits(xml);

    public CharacterFociSection ParseFoci(string xml) => _characterSectionService.ParseFoci(xml);

    public CharacterAiProgramsSection ParseAiPrograms(string xml) => _characterSectionService.ParseAiPrograms(xml);

    public CharacterMartialArtsSection ParseMartialArts(string xml) => _characterSectionService.ParseMartialArts(xml);

    public CharacterMetamagicsSection ParseMetamagics(string xml) => _characterSectionService.ParseMetamagics(xml);

    public CharacterArtsSection ParseArts(string xml) => _characterSectionService.ParseArts(xml);

    public CharacterInitiationGradesSection ParseInitiationGrades(string xml) => _characterSectionService.ParseInitiationGrades(xml);

    public CharacterCritterPowersSection ParseCritterPowers(string xml) => _characterSectionService.ParseCritterPowers(xml);

    public CharacterMentorSpiritsSection ParseMentorSpirits(string xml) => _characterSectionService.ParseMentorSpirits(xml);
}
