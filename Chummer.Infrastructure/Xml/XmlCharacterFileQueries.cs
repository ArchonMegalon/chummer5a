using Chummer.Application.Characters;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterFileQueries : ICharacterFileQueries
{
    private readonly ICharacterFileService _characterFileService;

    public XmlCharacterFileQueries(ICharacterFileService characterFileService)
    {
        _characterFileService = characterFileService;
    }

    public CharacterFileSummary ParseSummary(CharacterXmlDocument document)
    {
        return _characterFileService.ParseSummaryFromXml(document.Xml);
    }

    public CharacterValidationResult Validate(CharacterXmlDocument document)
    {
        return _characterFileService.ValidateXml(document.Xml);
    }
}
