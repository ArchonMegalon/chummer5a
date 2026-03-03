using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Chummer.Core.Characters;
using Chummer.Core;
using Chummer.Core.LifeModules;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ICharacterFileService, CharacterFileService>();
builder.Services.AddSingleton<ICharacterSectionService, CharacterSectionService>();
builder.Services.AddSingleton<ILifeModulesService>(_ =>
{
    string path = LifeModulesPathResolver.Resolve(AppContext.BaseDirectory, Directory.GetCurrentDirectory());
    return new LifeModulesService(path);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/info", () => Results.Ok(new
{
    service = "Chummer",
    status = "running",
    runtime = "net10.0",
    platform = "linux-native"
}));

app.MapGet("/api/health", () => Results.Ok(new { ok = true, utc = DateTimeOffset.UtcNow }));

app.MapPost("/api/xml/is-empty", (string xml) =>
{
    var doc = new XmlDocument();
    doc.LoadXml(xml);
    bool isEmpty = doc.DocumentElement.IsNullOrInnerTextIsEmpty();
    return Results.Ok(new { isEmpty });
});

app.MapPost("/api/characters/summary", (ICharacterFileService characterFileService, CharacterXmlRequest request) =>
{
    CharacterFileSummary summary = characterFileService.ParseSummaryFromXml(request.Xml);
    return Results.Ok(summary);
});

app.MapPost("/api/characters/validate", (ICharacterFileService characterFileService, CharacterXmlRequest request) =>
{
    CharacterValidationResult validation = characterFileService.ValidateXml(request.Xml);
    return Results.Ok(validation);
});

app.MapPost("/api/characters/metadata", (ICharacterFileService characterFileService, CharacterMetadataRequest request) =>
{
    CharacterMetadataUpdate update = new(
        Name: request.Name,
        Alias: request.Alias,
        Notes: request.Notes);
    string updatedXml = characterFileService.ApplyMetadataUpdate(request.Xml, update);
    CharacterFileSummary summary = characterFileService.ParseSummaryFromXml(updatedXml);
    return Results.Ok(new { updatedXml, summary });
});

app.MapPost("/api/characters/sections/attributes", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterAttributesSection section = characterSectionService.ParseAttributes(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/attributedetails", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterAttributeDetailsSection section = characterSectionService.ParseAttributeDetails(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/inventory", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterInventorySection section = characterSectionService.ParseInventory(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/profile", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterProfileSection section = characterSectionService.ParseProfile(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/progress", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterProgressSection section = characterSectionService.ParseProgress(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/rules", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterRulesSection section = characterSectionService.ParseRules(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/build", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterBuildSection section = characterSectionService.ParseBuild(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/movement", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterMovementSection section = characterSectionService.ParseMovement(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/awakening", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterAwakeningSection section = characterSectionService.ParseAwakening(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/gear", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterGearSection section = characterSectionService.ParseGear(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/weapons", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterWeaponsSection section = characterSectionService.ParseWeapons(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/weaponaccessories", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterWeaponAccessoriesSection section = characterSectionService.ParseWeaponAccessories(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/armors", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterArmorsSection section = characterSectionService.ParseArmors(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/armormods", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterArmorModsSection section = characterSectionService.ParseArmorMods(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/cyberwares", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterCyberwaresSection section = characterSectionService.ParseCyberwares(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/vehicles", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterVehiclesSection section = characterSectionService.ParseVehicles(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/vehiclemods", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterVehicleModsSection section = characterSectionService.ParseVehicleMods(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/skills", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterSkillsSection section = characterSectionService.ParseSkills(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/qualities", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterQualitiesSection section = characterSectionService.ParseQualities(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/contacts", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterContactsSection section = characterSectionService.ParseContacts(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/spells", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterSpellsSection section = characterSectionService.ParseSpells(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/powers", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterPowersSection section = characterSectionService.ParsePowers(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/complexforms", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterComplexFormsSection section = characterSectionService.ParseComplexForms(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/spirits", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterSpiritsSection section = characterSectionService.ParseSpirits(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/foci", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterFociSection section = characterSectionService.ParseFoci(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/aiprograms", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterAiProgramsSection section = characterSectionService.ParseAiPrograms(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/martialarts", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterMartialArtsSection section = characterSectionService.ParseMartialArts(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/limitmodifiers", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterLimitModifiersSection section = characterSectionService.ParseLimitModifiers(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/lifestyles", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterLifestylesSection section = characterSectionService.ParseLifestyles(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/metamagics", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterMetamagicsSection section = characterSectionService.ParseMetamagics(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/arts", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterArtsSection section = characterSectionService.ParseArts(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/initiationgrades", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterInitiationGradesSection section = characterSectionService.ParseInitiationGrades(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/critterpowers", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterCritterPowersSection section = characterSectionService.ParseCritterPowers(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/mentorspirits", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterMentorSpiritsSection section = characterSectionService.ParseMentorSpirits(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/expenses", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterExpensesSection section = characterSectionService.ParseExpenses(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/sources", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterSourcesSection section = characterSectionService.ParseSources(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/gearlocations", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterLocationsSection section = characterSectionService.ParseGearLocations(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/armorlocations", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterLocationsSection section = characterSectionService.ParseArmorLocations(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/weaponlocations", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterLocationsSection section = characterSectionService.ParseWeaponLocations(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/vehiclelocations", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterLocationsSection section = characterSectionService.ParseVehicleLocations(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/calendar", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterCalendarSection section = characterSectionService.ParseCalendar(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/improvements", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterImprovementsSection section = characterSectionService.ParseImprovements(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/customdatadirectorynames", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterCustomDataDirectoryNamesSection section = characterSectionService.ParseCustomDataDirectoryNames(request.Xml);
    return Results.Ok(section);
});

app.MapPost("/api/characters/sections/drugs", (ICharacterSectionService characterSectionService, CharacterXmlRequest request) =>
{
    CharacterDrugsSection section = characterSectionService.ParseDrugs(request.Xml);
    return Results.Ok(section);
});

app.MapGet("/api/lifemodules/stages", (ILifeModulesService lifeModulesService) =>
{
    var stages = lifeModulesService.GetStages();
    return Results.Ok(stages);
});

app.MapGet("/api/lifemodules/modules", (ILifeModulesService lifeModulesService, string? stage) =>
{
    var modules = lifeModulesService.GetModules(stage);
    return Results.Ok(new { count = modules.Count, modules });
});

app.MapPost("/api/tools/dice/roll", (DiceRollRequest request) =>
{
    string expression = (request.Expression ?? string.Empty).Trim().ToLowerInvariant();
    Match match = Regex.Match(expression, @"^(?<count>\d{1,3})d(?<sides>\d{1,4})(?<mod>[+-]\d+)?$");
    if (!match.Success)
        return Results.BadRequest(new { error = "Expression must match NdM(+/-X), e.g. 12d6+2." });

    int count = int.Parse(match.Groups["count"].Value);
    int sides = int.Parse(match.Groups["sides"].Value);
    int modifier = match.Groups["mod"].Success ? int.Parse(match.Groups["mod"].Value) : 0;
    if (count < 1 || count > 200 || sides < 2 || sides > 1000)
        return Results.BadRequest(new { error = "Dice count or sides are out of supported range." });

    List<int> rolls = new(count);
    for (int i = 0; i < count; i++)
        rolls.Add(Random.Shared.Next(1, sides + 1));

    int hits = rolls.Count(value => value >= 5);
    int ones = rolls.Count(value => value == 1);
    int rawTotal = rolls.Sum();
    int total = rawTotal + modifier;

    return Results.Ok(new
    {
        expression = $"{count}d{sides}{(modifier > 0 ? "+" : string.Empty)}{(modifier == 0 ? string.Empty : modifier)}",
        rolls,
        rawTotal,
        modifier,
        total,
        hits,
        ones,
        glitch = ones > count / 2,
        criticalGlitch = hits == 0 && ones > count / 2
    });
});

app.MapPost("/api/tools/data-export", (ICharacterFileService characterFileService, ICharacterSectionService sectionService, CharacterXmlRequest request) =>
{
    static T? SafeParse<T>(Func<T> parser) where T : class
    {
        try
        {
            return parser();
        }
        catch
        {
            return null;
        }
    }

    static CharacterFileSummary BuildFallbackSummary(string xml)
    {
        try
        {
            XDocument doc = XDocument.Parse(xml, LoadOptions.None);
            XElement? root = doc.Root;
            string name = root?.Element("name")?.Value ?? string.Empty;
            string alias = root?.Element("alias")?.Value ?? string.Empty;
            string metatype = root?.Element("metatype")?.Value ?? string.Empty;
            string buildMethod = root?.Element("buildmethod")?.Value ?? string.Empty;
            decimal karma = decimal.TryParse(root?.Element("karma")?.Value, out decimal parsedKarma) ? parsedKarma : 0m;
            decimal nuyen = decimal.TryParse(root?.Element("nuyen")?.Value, out decimal parsedNuyen) ? parsedNuyen : 0m;
            bool created = bool.TryParse(root?.Element("created")?.Value, out bool parsedCreated) && parsedCreated;

            return new CharacterFileSummary(
                Name: name,
                Alias: alias,
                Metatype: metatype,
                BuildMethod: buildMethod,
                CreatedVersion: string.Empty,
                AppVersion: string.Empty,
                Karma: karma,
                Nuyen: nuyen,
                Created: created);
        }
        catch
        {
            return new CharacterFileSummary(
                Name: string.Empty,
                Alias: string.Empty,
                Metatype: string.Empty,
                BuildMethod: string.Empty,
                CreatedVersion: string.Empty,
                AppVersion: string.Empty,
                Karma: 0m,
                Nuyen: 0m,
                Created: false);
        }
    }

    CharacterFileSummary? summary = SafeParse(() => characterFileService.ParseSummaryFromXml(request.Xml));
    summary ??= BuildFallbackSummary(request.Xml);
    CharacterProfileSection? profile = SafeParse(() => sectionService.ParseProfile(request.Xml));
    CharacterProgressSection? progress = SafeParse(() => sectionService.ParseProgress(request.Xml));
    CharacterAttributesSection? attributes = SafeParse(() => sectionService.ParseAttributes(request.Xml));
    CharacterSkillsSection? skills = SafeParse(() => sectionService.ParseSkills(request.Xml));
    CharacterInventorySection? inventory = SafeParse(() => sectionService.ParseInventory(request.Xml));
    CharacterQualitiesSection? qualities = SafeParse(() => sectionService.ParseQualities(request.Xml));
    CharacterContactsSection? contacts = SafeParse(() => sectionService.ParseContacts(request.Xml));
    return Results.Ok(new
    {
        summary,
        profile,
        progress,
        attributes,
        skills,
        inventory,
        qualities,
        contacts
    });
});

app.MapGet("/api/tools/master-index", () =>
{
    string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
    if (!Directory.Exists(dataDir))
        return Results.Ok(new { count = 0, files = Array.Empty<object>() });

    List<object> files = new();
    foreach (string file in Directory.EnumerateFiles(dataDir, "*.xml").OrderBy(Path.GetFileName))
    {
        try
        {
            XDocument document = XDocument.Load(file, LoadOptions.None);
            files.Add(new
            {
                file = Path.GetFileName(file),
                root = document.Root?.Name.LocalName ?? string.Empty,
                elementCount = document.Descendants().Count()
            });
        }
        catch
        {
            files.Add(new
            {
                file = Path.GetFileName(file),
                root = string.Empty,
                elementCount = 0
            });
        }
    }

    return Results.Ok(new
    {
        count = files.Count,
        generatedUtc = DateTimeOffset.UtcNow,
        files
    });
});

app.MapGet("/api/tools/translator/languages", () =>
{
    string langDir = Path.Combine(AppContext.BaseDirectory, "lang");
    if (!Directory.Exists(langDir))
        return Results.Ok(new { count = 0, languages = Array.Empty<object>() });

    List<object> languages = new();
    foreach (string file in Directory.EnumerateFiles(langDir, "*.xml").OrderBy(Path.GetFileName))
    {
        string code = Path.GetFileNameWithoutExtension(file);
        string name = code;
        try
        {
            XDocument doc = XDocument.Load(file, LoadOptions.None);
            name = doc.Root?.Element("name")?.Value?.Trim() ?? code;
        }
        catch
        {
            name = code;
        }

        languages.Add(new { code, name });
    }

    return Results.Ok(new { count = languages.Count, languages });
});

app.MapGet("/api/tools/settings/{scope}", (string scope) =>
{
    string normalizedScope = (scope ?? string.Empty).Trim().ToLowerInvariant();
    if (normalizedScope is not ("global" or "character"))
        return Results.BadRequest(new { error = "scope must be 'global' or 'character'." });

    string stateDir = Path.Combine(Path.GetTempPath(), "chummer-state");
    Directory.CreateDirectory(stateDir);
    string path = Path.Combine(stateDir, $"{normalizedScope}-settings.json");
    if (!File.Exists(path))
        return Results.Ok(new { scope = normalizedScope, settings = new { } });

    string text = File.ReadAllText(path);
    object settings;
    try
    {
        settings = string.IsNullOrWhiteSpace(text)
            ? new { }
            : (System.Text.Json.JsonSerializer.Deserialize<object>(text) ?? new { });
    }
    catch
    {
        settings = new { };
    }
    return Results.Ok(new { scope = normalizedScope, settings });
});

app.MapPost("/api/tools/settings/{scope}", (string scope, Dictionary<string, object>? settings) =>
{
    string normalizedScope = (scope ?? string.Empty).Trim().ToLowerInvariant();
    if (normalizedScope is not ("global" or "character"))
        return Results.BadRequest(new { error = "scope must be 'global' or 'character'." });

    string stateDir = Path.Combine(Path.GetTempPath(), "chummer-state");
    Directory.CreateDirectory(stateDir);
    string path = Path.Combine(stateDir, $"{normalizedScope}-settings.json");
    string json = System.Text.Json.JsonSerializer.Serialize(settings ?? new Dictionary<string, object>());
    File.WriteAllText(path, json);
    return Results.Ok(new { scope = normalizedScope, saved = true });
});

app.MapGet("/api/tools/roster", () =>
{
    string stateDir = Path.Combine(Path.GetTempPath(), "chummer-state");
    Directory.CreateDirectory(stateDir);
    string path = Path.Combine(stateDir, "roster.json");
    if (!File.Exists(path))
        return Results.Ok(new { count = 0, entries = Array.Empty<RosterEntry>() });

    List<RosterEntry> entries = System.Text.Json.JsonSerializer.Deserialize<List<RosterEntry>>(File.ReadAllText(path)) ?? [];
    return Results.Ok(new { count = entries.Count, entries });
});

app.MapPost("/api/tools/roster", (RosterEntry entry) =>
{
    string stateDir = Path.Combine(Path.GetTempPath(), "chummer-state");
    Directory.CreateDirectory(stateDir);
    string path = Path.Combine(stateDir, "roster.json");
    List<RosterEntry> entries = File.Exists(path)
        ? System.Text.Json.JsonSerializer.Deserialize<List<RosterEntry>>(File.ReadAllText(path)) ?? []
        : [];

    List<RosterEntry> merged = [entry];
    foreach (RosterEntry existing in entries)
    {
        if (existing.Name == entry.Name && existing.Alias == entry.Alias)
            continue;
        merged.Add(existing);
    }

    if (merged.Count > 50)
        merged = merged.Take(50).ToList();

    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(merged));
    return Results.Ok(new { count = merged.Count, entries = merged });
});

app.Run();

public sealed record CharacterXmlRequest(string Xml);
public sealed record DiceRollRequest(string? Expression);
public sealed record RosterEntry(string Name, string Alias, string Metatype, string LastOpenedUtc);

public sealed record CharacterMetadataRequest(
    string Xml,
    string? Name,
    string? Alias,
    string? Notes);
