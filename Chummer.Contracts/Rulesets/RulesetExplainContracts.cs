namespace Chummer.Contracts.Rulesets;

public sealed record RulesetGasBudget(
    int ProviderInstructionLimit,
    int RequestInstructionLimit,
    long MemoryBytesLimit,
    TimeSpan? WallClockLimit = null);

public sealed record RulesetExecutionOptions(
    bool Explain = false,
    RulesetGasBudget? GasBudget = null);

public sealed record RulesetGasUsage(
    int ProviderInstructionsConsumed,
    int RequestInstructionsConsumed,
    long PeakMemoryBytes,
    bool ProviderBudgetExceeded = false,
    bool RequestBudgetExceeded = false,
    bool WallClockLimitExceeded = false);

public sealed record RulesetExplainFragment(
    string Label,
    string? Value,
    string? Reason = null,
    string? PackId = null,
    string? ProviderId = null);

public sealed record RulesetProviderTrace(
    string ProviderId,
    string CapabilityId,
    string? PackId,
    bool Success,
    IReadOnlyList<RulesetExplainFragment> ExplainFragments,
    RulesetGasUsage GasUsage,
    IReadOnlyList<string> Messages);

public sealed record RulesetExplainTrace(
    string SubjectId,
    IReadOnlyList<RulesetProviderTrace> Providers,
    IReadOnlyList<string> Messages,
    RulesetGasUsage AggregateGasUsage);
