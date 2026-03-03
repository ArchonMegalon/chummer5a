using System.Xml.Linq;

namespace Chummer.Core.LifeModules;

public sealed class LifeModulesService : ILifeModulesService
{
    private readonly Lazy<XDocument> _document;

    public LifeModulesService(string lifeModulesPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lifeModulesPath);
        _document = new Lazy<XDocument>(() => XDocument.Load(lifeModulesPath));
    }

    public IReadOnlyList<LifeModuleStage> GetStages()
    {
        return _document.Value.Root!
            .Element("stages")!
            .Elements("stage")
            .Select(stage => new LifeModuleStage(
                int.TryParse(stage.Attribute("order")?.Value, out int order) ? order : -1,
                (stage.Value ?? string.Empty).Trim()))
            .OrderBy(stage => stage.Order)
            .ToArray();
    }

    public IReadOnlyList<LifeModuleSummary> GetModules(string? stage = null)
    {
        IEnumerable<XElement> modules = _document.Value.Root!
            .Element("modules")!
            .Elements("module");

        if (!string.IsNullOrWhiteSpace(stage))
        {
            string normalizedStage = stage.Trim();
            modules = modules.Where(module =>
                string.Equals((module.Element("stage")?.Value ?? string.Empty).Trim(), normalizedStage, StringComparison.Ordinal));
        }

        return modules.Select(module => new LifeModuleSummary(
            Id: (module.Element("id")?.Value ?? string.Empty).Trim(),
            Stage: (module.Element("stage")?.Value ?? string.Empty).Trim(),
            Name: (module.Element("name")?.Value ?? string.Empty).Trim(),
            Karma: (module.Element("karma")?.Value ?? string.Empty).Trim(),
            Source: (module.Element("source")?.Value ?? string.Empty).Trim(),
            Page: (module.Element("page")?.Value ?? string.Empty).Trim(),
            Story: (module.Element("story")?.Value ?? string.Empty).Trim()))
            .ToArray();
    }
}
