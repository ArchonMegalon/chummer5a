using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterSocialNarrativeQueries
{
    CharacterQualitiesSection ParseQualities(CharacterXmlDocument document);

    CharacterContactsSection ParseContacts(CharacterXmlDocument document);

    CharacterLifestylesSection ParseLifestyles(CharacterXmlDocument document);

    CharacterSourcesSection ParseSources(CharacterXmlDocument document);

    CharacterExpensesSection ParseExpenses(CharacterXmlDocument document);

    CharacterCalendarSection ParseCalendar(CharacterXmlDocument document);

    CharacterImprovementsSection ParseImprovements(CharacterXmlDocument document);

    CharacterCustomDataDirectoryNamesSection ParseCustomDataDirectoryNames(CharacterXmlDocument document);
}
