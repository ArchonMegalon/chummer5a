namespace Chummer.Contracts.Presentation;

public sealed record NavigationTabDefinition(
    string Id,
    string Label,
    string Group,
    bool RequiresOpenCharacter,
    bool EnabledByDefault);
