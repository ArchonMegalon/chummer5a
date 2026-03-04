using System.Xml.Linq;
using Chummer.Application.Content;
using Chummer.Application.Tools;
using Chummer.Contracts.Api;
using Chummer.Infrastructure.Files;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlToolCatalogService : IToolCatalogService
{
    private readonly IContentOverlayCatalogService _overlays;

    public XmlToolCatalogService(IContentOverlayCatalogService overlays)
    {
        _overlays = overlays;
    }

    public XmlToolCatalogService(string? baseDirectory = null)
    {
        string root = baseDirectory ?? AppContext.BaseDirectory;
        _overlays = new FileSystemContentOverlayCatalogService(root, Directory.GetCurrentDirectory(), configuredAmendsPath: null);
    }

    public MasterIndexResponse GetMasterIndex()
    {
        IReadOnlyDictionary<string, string> filesByName = EnumerateMergedFiles(_overlays.GetDataDirectories());
        if (filesByName.Count == 0)
            return new MasterIndexResponse(0, DateTimeOffset.UtcNow, Array.Empty<MasterIndexFileEntry>());

        List<MasterIndexFileEntry> files = new();
        foreach ((string fileName, string filePath) in filesByName.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            try
            {
                XDocument document = XDocument.Load(filePath, LoadOptions.None);
                files.Add(new MasterIndexFileEntry(
                    File: fileName,
                    Root: document.Root?.Name.LocalName ?? string.Empty,
                    ElementCount: document.Descendants().Count()));
            }
            catch
            {
                files.Add(new MasterIndexFileEntry(
                    File: fileName,
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
        IReadOnlyDictionary<string, string> filesByName = EnumerateMergedFiles(_overlays.GetLanguageDirectories());
        if (filesByName.Count == 0)
            return new TranslatorLanguagesResponse(0, Array.Empty<TranslatorLanguageEntry>());

        List<TranslatorLanguageEntry> languages = new();
        foreach ((string fileName, string filePath) in filesByName.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            string code = Path.GetFileNameWithoutExtension(fileName);
            string name = code;
            try
            {
                XDocument doc = XDocument.Load(filePath, LoadOptions.None);
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

    private static IReadOnlyDictionary<string, string> EnumerateMergedFiles(IReadOnlyList<string> directories)
    {
        Dictionary<string, string> filesByName = new(StringComparer.OrdinalIgnoreCase);

        foreach (string directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(directory, "*.xml", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                string fileName = Path.GetFileName(file);
                filesByName[fileName] = file;
            }
        }

        return filesByName;
    }
}
