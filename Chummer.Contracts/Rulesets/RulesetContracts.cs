using Chummer.Contracts.Presentation;

namespace Chummer.Contracts.Rulesets;

public static class RulesetDefaults
{
    public const string Sr5 = "sr5";

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    public static string NormalizeRequired(string value)
    {
        string? normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            throw new ArgumentException("Ruleset id is required.", nameof(value));
        }

        return normalized;
    }

    public static string NormalizeOrDefault(string? value, string defaultRulesetId)
    {
        return NormalizeOptional(value) ?? NormalizeRequired(defaultRulesetId);
    }

    public static string Normalize(string? value)
    {
        return NormalizeOrDefault(value, Sr5);
    }
}

public readonly record struct RulesetId(string Value)
{
    public static RulesetId Default => new(RulesetDefaults.Sr5);

    public string NormalizedValue => RulesetDefaults.Normalize(Value);

    public override string ToString() => NormalizedValue;
}

public sealed record WorkspacePayloadEnvelope(
    string RulesetId,
    int SchemaVersion,
    string PayloadKind,
    string Payload);

public interface IRulesetPlugin
{
    RulesetId Id { get; }

    string DisplayName { get; }

    IRulesetSerializer Serializer { get; }

    IRulesetShellDefinitionProvider ShellDefinitions { get; }

    IRulesetCatalogProvider Catalogs { get; }

    IRulesetRuleHost Rules { get; }

    IRulesetScriptHost Scripts { get; }
}

public interface IRulesetSerializer
{
    RulesetId RulesetId { get; }

    int SchemaVersion { get; }

    WorkspacePayloadEnvelope Wrap(string payloadKind, string payload);
}

public interface IRulesetShellDefinitionProvider
{
    IReadOnlyList<AppCommandDefinition> GetCommands();

    IReadOnlyList<NavigationTabDefinition> GetNavigationTabs();
}

public interface IRulesetCatalogProvider
{
    IReadOnlyList<WorkspaceSurfaceActionDefinition> GetWorkspaceActions();

    IReadOnlyList<DesktopUiControlDefinition> GetDesktopUiControls();
}

public sealed record RulesetRuleEvaluationRequest(
    string RuleId,
    IReadOnlyDictionary<string, object?> Inputs);

public sealed record RulesetRuleEvaluationResult(
    bool Success,
    IReadOnlyDictionary<string, object?> Outputs,
    IReadOnlyList<string> Messages);

public interface IRulesetRuleHost
{
    ValueTask<RulesetRuleEvaluationResult> EvaluateAsync(RulesetRuleEvaluationRequest request, CancellationToken ct);
}

public sealed record RulesetScriptExecutionRequest(
    string ScriptId,
    string ScriptSource,
    IReadOnlyDictionary<string, object?> Inputs);

public sealed record RulesetScriptExecutionResult(
    bool Success,
    string? Error,
    IReadOnlyDictionary<string, object?> Outputs);

public interface IRulesetScriptHost
{
    ValueTask<RulesetScriptExecutionResult> ExecuteAsync(RulesetScriptExecutionRequest request, CancellationToken ct);
}
