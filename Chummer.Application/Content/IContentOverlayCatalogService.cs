namespace Chummer.Application.Content;

public interface IContentOverlayCatalogService
{
    ContentOverlayCatalog GetCatalog();

    IReadOnlyList<string> GetDataDirectories();

    IReadOnlyList<string> GetLanguageDirectories();

    string ResolveDataFile(string fileName);
}

public sealed record ContentOverlayCatalog(
    string BaseDataPath,
    string BaseLanguagePath,
    IReadOnlyList<ContentOverlayPack> Overlays);

public sealed record ContentOverlayPack(
    string Id,
    string Name,
    string RootPath,
    string DataPath,
    string LanguagePath,
    int Priority,
    bool Enabled,
    string Description);
