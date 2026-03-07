namespace Chummer.Contracts.AI;

public sealed record BuildIdeaCard(
    string IdeaId,
    string RulesetId,
    string Title,
    string Summary,
    IReadOnlyList<string> RoleTags,
    IReadOnlyList<string> CompatibleProfileIds,
    string CoreLoop,
    IReadOnlyList<string> EarlyPriorities,
    IReadOnlyList<string> KarmaMilestones,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<string> TrapChoices,
    IReadOnlyList<string> LinkedContentIds,
    double CommunityScore = 0,
    string Provenance = "build-idea-card");
