using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterFileQueries
{
    CharacterFileSummary ParseSummary(CharacterXmlDocument document);

    CharacterValidationResult Validate(CharacterXmlDocument document);
}
