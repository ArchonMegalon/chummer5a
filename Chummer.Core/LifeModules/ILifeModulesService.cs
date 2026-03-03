namespace Chummer.Core.LifeModules;

public interface ILifeModulesService
{
    IReadOnlyList<LifeModuleStage> GetStages();

    IReadOnlyList<LifeModuleSummary> GetModules(string? stage = null);
}
