using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterMagicResonanceQueries
{
    CharacterSpellsSection ParseSpells(string xml);

    CharacterPowersSection ParsePowers(string xml);

    CharacterComplexFormsSection ParseComplexForms(string xml);

    CharacterSpiritsSection ParseSpirits(string xml);

    CharacterFociSection ParseFoci(string xml);

    CharacterAiProgramsSection ParseAiPrograms(string xml);

    CharacterMartialArtsSection ParseMartialArts(string xml);

    CharacterMetamagicsSection ParseMetamagics(string xml);

    CharacterArtsSection ParseArts(string xml);

    CharacterInitiationGradesSection ParseInitiationGrades(string xml);

    CharacterCritterPowersSection ParseCritterPowers(string xml);

    CharacterMentorSpiritsSection ParseMentorSpirits(string xml);
}
