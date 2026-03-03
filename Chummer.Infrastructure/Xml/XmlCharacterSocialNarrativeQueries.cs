using Chummer.Application.Characters;
using Chummer.Contracts.Characters;
using Chummer.Core.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterSocialNarrativeQueries : ICharacterSocialNarrativeQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterSocialNarrativeQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public CharacterQualitiesSection ParseQualities(string xml) => _characterSectionService.ParseQualities(xml);

    public CharacterContactsSection ParseContacts(string xml) => _characterSectionService.ParseContacts(xml);

    public CharacterLifestylesSection ParseLifestyles(string xml) => _characterSectionService.ParseLifestyles(xml);

    public CharacterSourcesSection ParseSources(string xml) => _characterSectionService.ParseSources(xml);

    public CharacterExpensesSection ParseExpenses(string xml) => _characterSectionService.ParseExpenses(xml);

    public CharacterCalendarSection ParseCalendar(string xml) => _characterSectionService.ParseCalendar(xml);

    public CharacterImprovementsSection ParseImprovements(string xml) => _characterSectionService.ParseImprovements(xml);

    public CharacterCustomDataDirectoryNamesSection ParseCustomDataDirectoryNames(string xml) =>
        _characterSectionService.ParseCustomDataDirectoryNames(xml);
}
