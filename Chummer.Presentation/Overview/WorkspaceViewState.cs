namespace Chummer.Presentation.Overview;

public sealed record WorkspaceViewState(
    string? ActiveTabId,
    string? ActiveActionId,
    string? ActiveSectionId,
    string? ActiveSectionJson,
    IReadOnlyList<SectionRowState> ActiveSectionRows,
    bool HasSavedWorkspace);
