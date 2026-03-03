using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterMagicResonanceQueries
{
    CharacterSpellsSection ParseSpells(CharacterXmlDocument document);

    CharacterPowersSection ParsePowers(CharacterXmlDocument document);

    CharacterComplexFormsSection ParseComplexForms(CharacterXmlDocument document);

    CharacterSpiritsSection ParseSpirits(CharacterXmlDocument document);

    CharacterFociSection ParseFoci(CharacterXmlDocument document);

    CharacterAiProgramsSection ParseAiPrograms(CharacterXmlDocument document);

    CharacterMartialArtsSection ParseMartialArts(CharacterXmlDocument document);

    CharacterMetamagicsSection ParseMetamagics(CharacterXmlDocument document);

    CharacterArtsSection ParseArts(CharacterXmlDocument document);

    CharacterInitiationGradesSection ParseInitiationGrades(CharacterXmlDocument document);

    CharacterCritterPowersSection ParseCritterPowers(CharacterXmlDocument document);

    CharacterMentorSpiritsSection ParseMentorSpirits(CharacterXmlDocument document);
}
