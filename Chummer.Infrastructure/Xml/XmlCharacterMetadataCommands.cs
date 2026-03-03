using Chummer.Application.Characters;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterMetadataCommands : ICharacterMetadataCommands
{
    private readonly ICharacterFileService _characterFileService;

    public XmlCharacterMetadataCommands(ICharacterFileService characterFileService)
    {
        _characterFileService = characterFileService;
    }

    public UpdateCharacterMetadataResult UpdateMetadata(UpdateCharacterMetadataCommand command)
    {
        CharacterMetadataUpdate update = new(
            Name: command.Name,
            Alias: command.Alias,
            Notes: command.Notes);

        string updatedXml = _characterFileService.ApplyMetadataUpdate(command.Xml, update);
        CharacterFileSummary summary = _characterFileService.ParseSummaryFromXml(updatedXml);
        return new UpdateCharacterMetadataResult(
            UpdatedXml: updatedXml,
            Summary: summary);
    }
}
