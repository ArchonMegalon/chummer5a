using Chummer.Application.Characters;
using Chummer.Contracts.Characters;

namespace Chummer.Infrastructure.Xml;

public sealed class XmlCharacterSocialNarrativeQueries : ICharacterSocialNarrativeQueries
{
    private readonly ICharacterSectionService _characterSectionService;

    public XmlCharacterSocialNarrativeQueries(ICharacterSectionService characterSectionService)
    {
        _characterSectionService = characterSectionService;
    }

    public CharacterQualitiesSection ParseQualities(CharacterXmlDocument document) => _characterSectionService.ParseQualities(document.Xml);

    public CharacterContactsSection ParseContacts(CharacterXmlDocument document) => _characterSectionService.ParseContacts(document.Xml);

    public CharacterLifestylesSection ParseLifestyles(CharacterXmlDocument document) => _characterSectionService.ParseLifestyles(document.Xml);

    public CharacterSourcesSection ParseSources(CharacterXmlDocument document) => _characterSectionService.ParseSources(document.Xml);

    public CharacterExpensesSection ParseExpenses(CharacterXmlDocument document) => _characterSectionService.ParseExpenses(document.Xml);

    public CharacterCalendarSection ParseCalendar(CharacterXmlDocument document) => _characterSectionService.ParseCalendar(document.Xml);

    public CharacterImprovementsSection ParseImprovements(CharacterXmlDocument document) => _characterSectionService.ParseImprovements(document.Xml);

    public CharacterCustomDataDirectoryNamesSection ParseCustomDataDirectoryNames(CharacterXmlDocument document) =>
        _characterSectionService.ParseCustomDataDirectoryNames(document.Xml);
}
