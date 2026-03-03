namespace Chummer.Contracts.Characters;

public sealed record UpdateCharacterMetadataCommand(
    string Xml,
    string? Name,
    string? Alias,
    string? Notes);

public sealed record UpdateCharacterMetadataResult(
    string UpdatedXml,
    CharacterFileSummary Summary);
