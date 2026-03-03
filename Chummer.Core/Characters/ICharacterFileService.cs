namespace Chummer.Core.Characters;

public interface ICharacterFileService
{
    CharacterFileSummary ParseSummaryFromXml(string xml);

    CharacterValidationResult ValidateXml(string xml);

    string ApplyMetadataUpdate(string xml, CharacterMetadataUpdate update);
}
