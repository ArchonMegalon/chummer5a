namespace Chummer.Contracts.AI;

public static class AiExplainApiOperations
{
    public const string ExplainValue = "explain-value";
}

public static class AiExplainEntryKinds
{
    public const string DerivedValue = "derived-value";
    public const string QuickActionAvailability = "quick-action-availability";
    public const string CapabilityDescriptor = "capability-descriptor";
}

public static class AiExplainFragmentKinds
{
    public const string Input = "input";
    public const string Constant = "constant";
    public const string ProviderStep = "provider-step";
    public const string Output = "output";
    public const string Warning = "warning";
    public const string Note = "note";
}

public sealed record AiExplainValueQuery(
    string? RuntimeFingerprint = null,
    string? CharacterId = null,
    string? CapabilityId = null,
    string? ExplainEntryId = null,
    string? RulesetId = null);

public sealed record AiExplainFragmentProjection(
    string Kind,
    string Label,
    string Value);

public sealed record AiExplainValueProjection(
    string ExplainEntryId,
    string Kind,
    string Title,
    string Summary,
    string RuntimeFingerprint,
    string RulesetId,
    string? CharacterId = null,
    string? CapabilityId = null,
    string? InvocationKind = null,
    string? ProviderId = null,
    string? PackId = null,
    bool Explainable = false,
    bool SessionSafe = false,
    int? ProviderGasBudget = null,
    int? RequestGasBudget = null,
    IReadOnlyList<AiExplainFragmentProjection>? Fragments = null,
    IReadOnlyList<string>? Diagnostics = null);
