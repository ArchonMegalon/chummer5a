namespace Chummer.Contracts.Presentation;

public sealed record DesktopUiControlDefinition(
    string Id,
    string Label,
    string TabId,
    bool RequiresOpenCharacter,
    bool EnabledByDefault);
