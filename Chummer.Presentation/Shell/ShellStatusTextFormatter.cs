using Chummer.Presentation.Overview;

namespace Chummer.Presentation.Shell;

public static class ShellStatusTextFormatter
{
    public static string BuildComplianceState(ShellSurfaceState shellSurface, DesktopPreferenceState preferences)
    {
        ArgumentNullException.ThrowIfNull(shellSurface);
        ArgumentNullException.ThrowIfNull(preferences);

        string rulesetId = string.IsNullOrWhiteSpace(shellSurface.ActiveRulesetId)
            ? "unresolved"
            : shellSurface.ActiveRulesetId;
        int workflowDefinitionCount = shellSurface.WorkflowDefinitions?.Count ?? 0;
        int workflowSurfaceCount = shellSurface.WorkflowSurfaces?.Count ?? 0;

        return $"Ruleset: {rulesetId} | Workflows: {workflowDefinitionCount} defs / {workflowSurfaceCount} surfaces | Prefs: {preferences.UiScalePercent}%/{preferences.Theme}/{preferences.Language}";
    }
}
