using Chummer.Application.Characters;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterStatsQueries : ICharacterStatsQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterStatsQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public CharacterAttributesSection ParseAttributes(CharacterXmlDocument document) => _characterSectionService.ParseAttributes(document.Xml);

    public CharacterAttributeDetailsSection ParseAttributeDetails(CharacterXmlDocument document) => _characterSectionService.ParseAttributeDetails(document.Xml);

    public CharacterLimitModifiersSection ParseLimitModifiers(CharacterXmlDocument document) => _characterSectionService.ParseLimitModifiers(document.Xml);
}
