using System.Xml.Linq;
using Chummer.Application.Tools;
using Chummer.Contracts.Api;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlToolCatalogService : IToolCatalogService
{
    private readonly string _baseDirectory;

    public XmlToolCatalogService(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
    }

    public MasterIndexResponse GetMasterIndex()
    {
        string dataDir = Path.Combine(_baseDirectory, "data");
        if (!Directory.Exists(dataDir))
            return new MasterIndexResponse(0, DateTimeOffset.UtcNow, Array.Empty<MasterIndexFileEntry>());

        List<MasterIndexFileEntry> files = new();
        foreach (string file in Directory.EnumerateFiles(dataDir, "*.xml").OrderBy(Path.GetFileName))
        {
            try
            {
                XDocument document = XDocument.Load(file, LoadOptions.None);
                files.Add(new MasterIndexFileEntry(
                    File: Path.GetFileName(file),
                    Root: document.Root?.Name.LocalName ?? string.Empty,
                    ElementCount: document.Descendants().Count()));
            }
            catch
            {
                files.Add(new MasterIndexFileEntry(
                    File: Path.GetFileName(file),
                    Root: string.Empty,
                    ElementCount: 0));
            }
        }

        return new MasterIndexResponse(
            Count: files.Count,
            GeneratedUtc: DateTimeOffset.UtcNow,
            Files: files);
    }

    public TranslatorLanguagesResponse GetTranslatorLanguages()
    {
        string langDir = Path.Combine(_baseDirectory, "lang");
        if (!Directory.Exists(langDir))
            return new TranslatorLanguagesResponse(0, Array.Empty<TranslatorLanguageEntry>());

        List<TranslatorLanguageEntry> languages = new();
        foreach (string file in Directory.EnumerateFiles(langDir, "*.xml").OrderBy(Path.GetFileName))
        {
            string code = Path.GetFileNameWithoutExtension(file);
            string name = code;
            try
            {
                XDocument doc = XDocument.Load(file, LoadOptions.None);
                name = doc.Root?.Element("name")?.Value?.Trim() ?? code;
            }
            catch
            {
                name = code;
            }

            languages.Add(new TranslatorLanguageEntry(
                Code: code,
                Name: name));
        }

        return new TranslatorLanguagesResponse(
            Count: languages.Count,
            Languages: languages);
    }
}
