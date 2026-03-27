namespace Chummer.Contracts.Api;

public sealed record TranslatorShippingLanguage(
    string Code,
    string Name);

public static class TranslatorLanguageCatalog
{
    public const string SourceCode = "en-us";
    public const string FallbackCode = SourceCode;

    public static IReadOnlyList<TranslatorShippingLanguage> ShippingLanguages { get; } =
    [
        new("en-us", "English"),
        new("de-de", "Deutsch"),
        new("fr-fr", "Francais"),
        new("ja-jp", "Japanese"),
        new("pt-br", "Portugues (Brasil)"),
        new("zh-cn", "Chinese (Simplified)")
    ];

    public static bool IsShippingTarget(string? code)
    {
        string normalized = NormalizeCode(code);
        return ShippingLanguages.Any(language => string.Equals(language.Code, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static string NormalizeOrFallback(string? code)
    {
        string normalized = NormalizeCode(code);
        return IsShippingTarget(normalized) ? normalized : FallbackCode;
    }

    public static string NormalizeCode(string? code)
    {
        return Normalize(code);
    }

    public static string ResolveName(string? code)
    {
        string normalized = NormalizeCode(code);
        TranslatorShippingLanguage? language = ShippingLanguages.FirstOrDefault(item => string.Equals(item.Code, normalized, StringComparison.OrdinalIgnoreCase));
        return language?.Name ?? normalized;
    }

    private static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        return code.Trim().Replace('_', '-').ToLowerInvariant();
    }
}
