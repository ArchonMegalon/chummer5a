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

    public CharacterFileSummary ParseSummary(string xml)
    {
        return _characterFileService.ParseSummaryFromXml(xml);
    }

    public CharacterValidationResult Validate(string xml)
    {
        return _characterFileService.ValidateXml(xml);
    }
}
