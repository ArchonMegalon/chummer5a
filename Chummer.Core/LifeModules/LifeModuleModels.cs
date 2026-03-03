namespace Chummer.Core.LifeModules;

public sealed record LifeModuleStage(int Order, string Name);

public sealed record LifeModuleSummary(
    string Id,
    string Stage,
    string Name,
    string Karma,
    string Source,
    string Page,
    string Story);
