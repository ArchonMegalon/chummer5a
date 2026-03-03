using Chummer.Application.Characters;
using Chummer.Contracts.Characters;
using Chummer.Core.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterStatsQueries : ICharacterStatsQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterStatsQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public CharacterAttributesSection ParseAttributes(string xml) => _characterSectionService.ParseAttributes(xml);

    public CharacterAttributeDetailsSection ParseAttributeDetails(string xml) => _characterSectionService.ParseAttributeDetails(xml);

    public CharacterLimitModifiersSection ParseLimitModifiers(string xml) => _characterSectionService.ParseLimitModifiers(xml);
}
