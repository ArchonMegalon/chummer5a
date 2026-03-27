namespace Chummer.Contracts.Api;

public sealed record MasterIndexFileEntry(
    string File,
    string Root,
    int ElementCount);

public sealed record MasterIndexResponse(
    int Count,
    DateTimeOffset GeneratedUtc,
    IReadOnlyList<MasterIndexFileEntry> Files);

public sealed record TranslatorLanguageEntry(
    string Code,
    string Name,
    bool IsSource,
    bool IsShippingTarget,
    bool HasUiChromeDomain,
    bool HasDataNamesDomain);

public sealed record TranslatorLanguagesResponse(
    int Count,
    string SourceCode,
    string FallbackCode,
    bool RequiresRestartOnChange,
    IReadOnlyList<TranslatorLanguageEntry> Languages);
