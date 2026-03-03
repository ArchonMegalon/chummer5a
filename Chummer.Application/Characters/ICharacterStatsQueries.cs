using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterStatsQueries
{
    CharacterAttributesSection ParseAttributes(string xml);

    CharacterAttributeDetailsSection ParseAttributeDetails(string xml);

    CharacterLimitModifiersSection ParseLimitModifiers(string xml);
}
