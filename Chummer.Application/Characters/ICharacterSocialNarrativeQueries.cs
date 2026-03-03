using Chummer.Contracts.Characters;

namespace Chummer.Application.Characters;

public interface ICharacterSocialNarrativeQueries
{
    CharacterQualitiesSection ParseQualities(string xml);

    CharacterContactsSection ParseContacts(string xml);

    CharacterLifestylesSection ParseLifestyles(string xml);

    CharacterSourcesSection ParseSources(string xml);

    CharacterExpensesSection ParseExpenses(string xml);

    CharacterCalendarSection ParseCalendar(string xml);

    CharacterImprovementsSection ParseImprovements(string xml);

    CharacterCustomDataDirectoryNamesSection ParseCustomDataDirectoryNames(string xml);
}
