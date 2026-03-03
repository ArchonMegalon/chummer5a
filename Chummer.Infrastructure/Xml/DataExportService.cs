using System.Xml.Linq;
using Chummer.Application.Characters;
using Chummer.Application.Tools;
using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class DataExportService : IDataExportService
{
    private readonly ICharacterFileQueries _characterFileQueries;
    private readonly ICharacterSectionQueries _sectionQueries;

    public DataExportService(
        ICharacterFileQueries characterFileQueries,
        ICharacterSectionQueries sectionQueries)
    {
        _characterFileQueries = characterFileQueries;
        _sectionQueries = sectionQueries;
    }

    public DataExportBundle BuildBundle(string xml)
    {
        CharacterFileSummary? summary = SafeParse(() => _characterFileQueries.ParseSummary(xml));
        summary ??= BuildFallbackSummary(xml);

        CharacterProfileSection? profile = SafeParse(() => ParseSection<CharacterProfileSection>("profile", xml));
        CharacterProgressSection? progress = SafeParse(() => ParseSection<CharacterProgressSection>("progress", xml));
        CharacterAttributesSection? attributes = SafeParse(() => ParseSection<CharacterAttributesSection>("attributes", xml));
        CharacterSkillsSection? skills = SafeParse(() => ParseSection<CharacterSkillsSection>("skills", xml));
        CharacterInventorySection? inventory = SafeParse(() => ParseSection<CharacterInventorySection>("inventory", xml));
        CharacterQualitiesSection? qualities = SafeParse(() => ParseSection<CharacterQualitiesSection>("qualities", xml));
        CharacterContactsSection? contacts = SafeParse(() => ParseSection<CharacterContactsSection>("contacts", xml));

        return new DataExportBundle(
            Summary: summary,
            Profile: profile,
            Progress: progress,
            Attributes: attributes,
            Skills: skills,
            Inventory: inventory,
            Qualities: qualities,
            Contacts: contacts);
    }

    private TSection ParseSection<TSection>(string sectionId, string xml)
    {
        return (TSection)_sectionQueries.ParseSection(sectionId, xml);
    }

    private static T? SafeParse<T>(Func<T> parser) where T : class
    {
        try
        {
            return parser();
        }
        catch
        {
            return null;
        }
    }

    private static CharacterFileSummary BuildFallbackSummary(string xml)
    {
        try
        {
            XDocument doc = XDocument.Parse(xml, LoadOptions.None);
            XElement? root = doc.Root;
            string name = root?.Element("name")?.Value ?? string.Empty;
            string alias = root?.Element("alias")?.Value ?? string.Empty;
            string metatype = root?.Element("metatype")?.Value ?? string.Empty;
            string buildMethod = root?.Element("buildmethod")?.Value ?? string.Empty;
            decimal karma = decimal.TryParse(root?.Element("karma")?.Value, out decimal parsedKarma) ? parsedKarma : 0m;
            decimal nuyen = decimal.TryParse(root?.Element("nuyen")?.Value, out decimal parsedNuyen) ? parsedNuyen : 0m;
            bool created = bool.TryParse(root?.Element("created")?.Value, out bool parsedCreated) && parsedCreated;

            return new CharacterFileSummary(
                Name: name,
                Alias: alias,
                Metatype: metatype,
                BuildMethod: buildMethod,
                CreatedVersion: string.Empty,
                AppVersion: string.Empty,
                Karma: karma,
                Nuyen: nuyen,
                Created: created);
        }
        catch
        {
            return new CharacterFileSummary(
                Name: string.Empty,
                Alias: string.Empty,
                Metatype: string.Empty,
                BuildMethod: string.Empty,
                CreatedVersion: string.Empty,
                AppVersion: string.Empty,
                Karma: 0m,
                Nuyen: 0m,
                Created: false);
        }
    }
}
