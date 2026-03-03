namespace Chummer.Contracts.Characters;

public sealed record CharacterFileSummary(
    string Name,
    string Alias,
    string Metatype,
    string BuildMethod,
    string CreatedVersion,
    string AppVersion,
    decimal Karma,
    decimal Nuyen,
    bool Created);

public sealed record CharacterValidationIssue(
    string Severity,
    string Code,
    string Message,
    string Path);

public sealed record CharacterValidationResult(
    bool IsValid,
    IReadOnlyList<CharacterValidationIssue> Issues);

public sealed record CharacterMetadataUpdate(
    string? Name,
    string? Alias,
    string? Notes);

public sealed record CharacterXmlDocument(
    string Xml);
