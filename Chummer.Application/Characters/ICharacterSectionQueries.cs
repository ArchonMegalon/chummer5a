namespace Chummer.Application.Characters;

public interface ICharacterSectionQueries
{
    object ParseSection(string sectionId, string xml);
}
