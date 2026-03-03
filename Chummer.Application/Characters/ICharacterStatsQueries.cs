using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterStatsQueries
{
    CharacterAttributesSection ParseAttributes(CharacterXmlDocument document);

    CharacterAttributeDetailsSection ParseAttributeDetails(CharacterXmlDocument document);

    CharacterLimitModifiersSection ParseLimitModifiers(CharacterXmlDocument document);
}
