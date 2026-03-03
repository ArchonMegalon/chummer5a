using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterFileQueries
{
    CharacterFileSummary ParseSummary(string xml);

    CharacterValidationResult Validate(string xml);
}
