using Chummer.Contracts.Api;

namespace Chummer.Application.Tools;

public interface IRosterStore
{
    IReadOnlyList<RosterEntry> Load();

    IReadOnlyList<RosterEntry> Upsert(RosterEntry entry);
}
