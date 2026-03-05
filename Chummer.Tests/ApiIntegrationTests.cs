using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class ApiIntegrationTests
{
    private static readonly Uri BaseUri = ResolveBaseUri();
    private static readonly string? ApiKey = ResolveApiKey();
    private static readonly string? ExpectedAmendId = ResolveExpectedAmendId();
    private static readonly TimeSpan HttpTimeout = ResolveHttpTimeout();
    private static readonly string[] AllSectionIds =
    {
        "attributes",
        "attributedetails",
        "inventory",
        "profile",
        "progress",
        "rules",
        "build",
        "movement",
        "awakening",
        "gear",
        "weapons",
        "weaponaccessories",
        "armors",
        "armormods",
        "cyberwares",
        "vehicles",
        "vehiclemods",
        "skills",
        "qualities",
        "contacts",
        "spells",
        "powers",
        "complexforms",
        "spirits",
        "foci",
        "aiprograms",
        "martialarts",
        "limitmodifiers",
        "lifestyles",
        "metamagics",
        "arts",
        "initiationgrades",
        "critterpowers",
        "mentorspirits",
        "expenses",
        "sources",
        "gearlocations",
        "armorlocations",
        "weaponlocations",
        "vehiclelocations",
        "calendar",
        "improvements",
        "customdatadirectorynames",
        "drugs"
    };

    [TestMethod]
    public async Task Info_endpoint_reports_chummer_service()
    {
        using var client = CreateClient();

        JsonObject info = await GetRequiredJsonObject(client, "/api/info");

        Assert.AreEqual("Chummer", info["service"]?.GetValue<string>());
        Assert.AreEqual("running", info["status"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Info_endpoint_reports_content_overlay_metadata()
    {
        using var client = CreateClient();

        JsonObject info = await GetRequiredJsonObject(client, "/api/info");

        Assert.IsInstanceOfType<JsonObject>(info["content"]);
        JsonObject content = (JsonObject)info["content"]!;
        Assert.IsTrue(content["baseDataPath"] is not null);
        Assert.IsTrue(content["baseLanguagePath"] is not null);
        Assert.IsInstanceOfType<JsonArray>(content["overlays"]);
    }

    [TestMethod]
    public async Task Content_overlays_endpoint_reports_catalog_and_expected_overlay_when_configured()
    {
        using var client = CreateClient();

        JsonObject overlays = await GetRequiredJsonObject(client, "/api/content/overlays");
        Assert.IsTrue(overlays["baseDataPath"] is not null);
        Assert.IsTrue(overlays["baseLanguagePath"] is not null);
        Assert.IsInstanceOfType<JsonArray>(overlays["overlays"]);

        if (!string.IsNullOrWhiteSpace(ExpectedAmendId))
        {
            JsonArray items = (JsonArray)overlays["overlays"]!;
            bool found = items.OfType<JsonObject>()
                .Any(item => string.Equals(item["id"]?.GetValue<string>(), ExpectedAmendId, StringComparison.Ordinal));
            Assert.IsTrue(found, $"Expected overlay id '{ExpectedAmendId}' was not found.");
        }
    }

    [TestMethod]
    public async Task Health_endpoint_reports_ok()
    {
        using var client = CreateClient();

        JsonObject health = await GetRequiredJsonObject(client, "/api/health");

        Assert.IsTrue(health["ok"]?.GetValue<bool>() ?? false);
    }

    [TestMethod]
    public async Task Root_endpoint_reports_api_service_document()
    {
        using var client = CreateClient();

        JsonObject payload = await GetRequiredJsonObject(client, "/");

        Assert.AreEqual("Chummer.Api", payload["service"]?.GetValue<string>());
        Assert.AreEqual("running", payload["status"]?.GetValue<string>());
        Assert.IsTrue(payload["docs"] is JsonArray);
    }

    [TestMethod]
    public async Task Public_endpoints_remain_accessible_without_api_key_header_when_auth_is_enabled()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return;

        using var client = CreateClient(includeApiKey: false);

        JsonObject health = await GetRequiredJsonObject(client, "/api/health");
        Assert.IsTrue(health["ok"]?.GetValue<bool>() ?? false);

        JsonObject info = await GetRequiredJsonObject(client, "/api/info");
        Assert.AreEqual("Chummer", info["service"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Protected_endpoint_requires_valid_api_key_when_auth_is_enabled()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return;

        using var client = CreateClient(includeApiKey: false);

        using HttpResponseMessage response = await client.GetAsync("/api/tools/master-index");
        string content = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(401, (int)response.StatusCode, content);
        JsonNode? parsed = JsonNode.Parse(content);
        Assert.AreEqual("missing_or_invalid_api_key", parsed?["error"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Contacts_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><contacts><contact><name>A</name><role>B</role><location>C</location><connection>3</connection><loyalty>2</loyalty></contact></contacts></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/contacts", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Attribute_details_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><attributes><attribute><name>BOD</name><metatypemin>1</metatypemin><metatypemax>6</metatypemax><metatypeaugmax>9</metatypeaugmax><base>3</base><karma>1</karma><totalvalue>4</totalvalue><metatypecategory>Standard</metatypecategory></attribute></attributes></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/attributedetails", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Vehicles_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><vehicles><vehicle><guid>v1</guid><name>Roadmaster</name><category>Truck</category><handling>3</handling><speed>4</speed><body>18</body><armor>16</armor><sensor>3</sensor><seats>6</seats><cost>120000</cost><mods><mod><name>GridLink Override</name></mod></mods><weapons><weapon><name>LMG</name></weapon></weapons></vehicle></vehicles></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/vehicles", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Profile_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><name>Neo</name><alias>The One</alias><playername>T</playername><metatype>Human</metatype><sex>Male</sex><age>29</age><buildmethod>Priority</buildmethod><created>True</created><adept>False</adept><magician>True</magician><technomancer>False</technomancer><ai>False</ai><mainmugshotindex>0</mainmugshotindex><mugshots><mugshot>a</mugshot></mugshots></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/profile", body);
        Assert.AreEqual("Neo", response["name"]?.GetValue<string>());
        Assert.AreEqual("Human", response["metatype"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Progress_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><karma>15</karma><nuyen>2500</nuyen><startingnuyen>6000</startingnuyen><streetcred>2</streetcred><notoriety>1</notoriety><publicawareness>0</publicawareness><burntstreetcred>0</burntstreetcred><buildkarma>25</buildkarma><totalattributes>18</totalattributes><totalspecial>2</totalspecial><physicalcmfilled>1</physicalcmfilled><stuncmfilled>3</stuncmfilled><totaless>5.25</totaless><initiategrade>0</initiategrade><submersiongrade>0</submersiongrade><magenabled>True</magenabled><resenabled>False</resenabled><depenabled>False</depenabled></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/progress", body);
        Assert.AreEqual(15m, response["karma"]?.GetValue<decimal>());
        Assert.AreEqual(2500m, response["nuyen"]?.GetValue<decimal>());
    }

    [TestMethod]
    public async Task Rules_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><gameedition>SR5</gameedition><settings>default.xml</settings><gameplayoption>Standard</gameplayoption><gameplayoptionqualitylimit>25</gameplayoptionqualitylimit><maxnuyen>10</maxnuyen><maxkarma>25</maxkarma><contactmultiplier>3</contactmultiplier><bannedwaregrades><grade>Betaware</grade><grade>Deltaware</grade></bannedwaregrades></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/rules", body);
        Assert.AreEqual("SR5", response["gameEdition"]?.GetValue<string>());
        Assert.AreEqual(2, response["bannedWareGrades"]?.AsArray().Count);
    }

    [TestMethod]
    public async Task Build_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><buildmethod>SumtoTen</buildmethod><prioritymetatype>C,2</prioritymetatype><priorityattributes>E,0</priorityattributes><priorityspecial>A,4</priorityspecial><priorityskills>B,3</priorityskills><priorityresources>D,1</priorityresources><prioritytalent>Mundane</prioritytalent><sumtoten>10</sumtoten><special>1</special><totalspecial>4</totalspecial><totalattributes>20</totalattributes><contactpoints>15</contactpoints><contactpointsused>8</contactpointsused></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/build", body);
        Assert.AreEqual("SumtoTen", response["buildMethod"]?.GetValue<string>());
        Assert.AreEqual(10, response["sumToTen"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Movement_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><walk>2/1/0</walk><run>4/0/0</run><sprint>2/1/0</sprint><walkalt>2/1/0</walkalt><runalt>4/0/0</runalt><sprintalt>2/1/0</sprintalt><physicalcmfilled>1</physicalcmfilled><stuncmfilled>3</stuncmfilled></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/movement", body);
        Assert.AreEqual("2/1/0", response["walk"]?.GetValue<string>());
        Assert.AreEqual(3, response["stunCmFilled"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Weapon_accessories_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><weapons><weapon><guid>w1</guid><name>Ares Predator</name><accessories><accessory><guid>a1</guid><name>Smartgun System</name><mount>Internal</mount><extramount>None</extramount><rating>0</rating><cost>500</cost><equipped>True</equipped></accessory></accessories></weapon></weapons></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/weaponaccessories", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Armor_mods_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><armors><armor><guid>ar1</guid><name>Armor Jacket</name><armormods><armormod><guid>m1</guid><name>Nonconductivity</name><category>General</category><rating>6</rating><cost>6000</cost><equipped>True</equipped></armormod></armormods></armor></armors></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/armormods", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Vehicle_mods_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><vehicles><vehicle><guid>v1</guid><name>Roadmaster</name><mods><mod><guid>vm1</guid><name>GridLink Override</name><category>Electromagnetic</category><slots>1</slots><rating>0</rating><cost>1000</cost><equipped>True</equipped></mod></mods></vehicle></vehicles></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/vehiclemods", body);
        Assert.AreEqual(1, response["count"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Awakening_section_endpoint_parses_payload()
    {
        using var client = CreateClient();

        const string xml = "<character><magenabled>True</magenabled><resenabled>False</resenabled><depenabled>False</depenabled><adept>False</adept><magician>True</magician><technomancer>False</technomancer><ai>False</ai><initiategrade>2</initiategrade><submersiongrade>0</submersiongrade><tradition>Hermetic</tradition><traditionname>Hermetic</traditionname><traditiondrain>LOG + WIL</traditiondrain><spiritcombat>Fire</spiritcombat><spiritdetection>Air</spiritdetection><spirithealth>Water</spirithealth><spiritillusion>Earth</spiritillusion><spiritmanipulation>Man</spiritmanipulation><stream></stream><streamdrain></streamdrain><currentcounterspellingdice>3</currentcounterspellingdice><spelllimit>12</spelllimit><cfplimit>0</cfplimit><ainormalprogramlimit>0</ainormalprogramlimit><aiadvancedprogramlimit>0</aiadvancedprogramlimit></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/characters/sections/awakening", body);
        Assert.IsTrue(response["magEnabled"]?.GetValue<bool>() ?? false);
        Assert.AreEqual(2, response["initiateGrade"]?.GetValue<int>());
    }

    [TestMethod]
    public async Task Dice_roll_endpoint_returns_rolls()
    {
        using var client = CreateClient();

        JsonObject body = new()
        {
            ["expression"] = "8d6+2"
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/tools/dice/roll", body);
        Assert.AreEqual(8, response["rolls"]?.AsArray().Count);
        Assert.IsTrue(response["total"]?.GetValue<int>() >= 10);
    }

    [TestMethod]
    public async Task Data_export_endpoint_returns_bundle()
    {
        using var client = CreateClient();

        const string xml = "<character><name>Neo</name><alias>The One</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><karma>15</karma><nuyen>2500</nuyen><attributes><attribute><name>BOD</name><base>3</base><karma>1</karma><metatypecategory>Standard</metatypecategory><totalvalue>4</totalvalue></attribute></attributes><skills><skill><name>Pistols</name></skill></skills><contacts><contact><name>Fixer</name></contact></contacts></character>";
        JsonObject body = new()
        {
            ["xml"] = xml
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/tools/data-export", body);
        Assert.IsNotNull(response["summary"]);
        Assert.IsNotNull(response["profile"]);
        Assert.IsNotNull(response["attributes"]);
    }

    [TestMethod]
    public async Task Master_index_endpoint_returns_data()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/tools/master-index");
        Assert.IsTrue((response["count"]?.GetValue<int>() ?? 0) > 0);
        Assert.IsTrue(response["files"] is JsonArray);
    }

    [TestMethod]
    public async Task Translator_languages_endpoint_returns_data()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/tools/translator/languages");
        Assert.IsTrue((response["count"]?.GetValue<int>() ?? 0) > 0);
        Assert.IsTrue(response["languages"] is JsonArray);
    }

    [TestMethod]
    public async Task Settings_endpoints_roundtrip()
    {
        using var client = CreateClient();

        JsonObject saveBody = new()
        {
            ["uiScale"] = 110,
            ["theme"] = "classic"
        };

        JsonObject saveResponse = await PostRequiredJsonObject(client, "/api/tools/settings/global", saveBody);
        Assert.IsTrue(saveResponse["saved"]?.GetValue<bool>() ?? false);

        JsonObject getResponse = await GetRequiredJsonObject(client, "/api/tools/settings/global");
        Assert.IsNotNull(getResponse["settings"]);
    }

    [TestMethod]
    public async Task Roster_endpoints_accept_entry()
    {
        using var client = CreateClient();

        JsonObject body = new()
        {
            ["name"] = "BLUE",
            ["alias"] = "Troy",
            ["metatype"] = "Ork",
            ["lastOpenedUtc"] = DateTimeOffset.UtcNow.ToString("O")
        };

        JsonObject response = await PostRequiredJsonObject(client, "/api/tools/roster", body);
        Assert.IsTrue((response["count"]?.GetValue<int>() ?? 0) > 0);
        Assert.IsTrue(response["entries"] is JsonArray);
    }

    [TestMethod]
    public async Task Life_modules_stages_endpoint_returns_data()
    {
        using var client = CreateClient();

        JsonNode stages = await client.GetFromJsonAsync<JsonNode>("/api/lifemodules/stages");
        Assert.IsNotNull(stages);
        Assert.IsTrue(stages is JsonArray array && array.Count > 0);
    }

    [TestMethod]
    public async Task Commands_endpoint_returns_catalog()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/commands?ruleset=sr5");
        JsonObject defaultResponse = await GetRequiredJsonObject(client, "/api/commands");

        Assert.IsTrue((response["count"]?.GetValue<int>() ?? 0) > 0);
        Assert.IsTrue(response["commands"] is JsonArray);
        Assert.AreEqual(response.ToJsonString(), defaultResponse.ToJsonString());
    }

    [TestMethod]
    public async Task Commands_endpoint_returns_empty_catalog_for_unknown_ruleset()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/commands?ruleset=sr6");

        Assert.AreEqual(0, response["count"]?.GetValue<int>() ?? -1);
        Assert.IsTrue(response["commands"] is JsonArray commands && commands.Count == 0);
    }

    [TestMethod]
    public async Task Navigation_tabs_endpoint_returns_catalog()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/navigation-tabs?ruleset=sr5");
        JsonObject defaultResponse = await GetRequiredJsonObject(client, "/api/navigation-tabs");

        Assert.IsTrue((response["count"]?.GetValue<int>() ?? 0) >= 16);
        Assert.IsTrue(response["tabs"] is JsonArray);
        Assert.IsTrue((response["tabs"] as JsonArray)?.Any(node => string.Equals(node?["id"]?.GetValue<string>(), "tab-info", StringComparison.Ordinal)) ?? false);
        Assert.IsTrue((response["tabs"] as JsonArray)?.All(node => !string.IsNullOrWhiteSpace(node?["sectionId"]?.GetValue<string>())) ?? false);
        Assert.AreEqual(response.ToJsonString(), defaultResponse.ToJsonString());
    }

    [TestMethod]
    public async Task Navigation_tabs_endpoint_returns_empty_catalog_for_unknown_ruleset()
    {
        using var client = CreateClient();

        JsonObject response = await GetRequiredJsonObject(client, "/api/navigation-tabs?ruleset=sr6");

        Assert.AreEqual(0, response["count"]?.GetValue<int>() ?? -1);
        Assert.IsTrue(response["tabs"] is JsonArray tabs && tabs.Count == 0);
    }

    [TestMethod]
    public async Task Shell_bootstrap_endpoint_returns_ruleset_catalog_and_workspace_snapshot()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);

        JsonObject response = await GetRequiredJsonObject(client, "/api/shell/bootstrap?ruleset=sr5");

        Assert.AreEqual("sr5", (response["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.IsTrue(response["commands"] is JsonArray commands && commands.Count > 0);
        Assert.IsTrue(response["navigationTabs"] is JsonArray tabs && tabs.Count > 0);
        Assert.IsTrue(response["workspaces"] is JsonArray);
    }

    [TestMethod]
    public async Task Shell_bootstrap_endpoint_defaults_to_active_workspace_ruleset_when_unspecified()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromSeconds(180);

        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        JsonObject importBody = new()
        {
            ["xml"] = xml,
            ["rulesetId"] = "SR6"
        };

        await PostRequiredJsonObject(client, "/api/workspaces/import", importBody);
        JsonObject response = await GetRequiredJsonObject(client, "/api/shell/bootstrap");

        Assert.AreEqual("sr6", (response["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
    }

    [TestMethod]
    public async Task Workspace_endpoints_import_read_update_and_save_character()
    {
        using var client = CreateClient();

        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        JsonObject importBody = new()
        {
            ["xml"] = xml
        };

        JsonObject importResponse = await PostRequiredJsonObject(client, "/api/workspaces/import", importBody);
        string workspaceId = importResponse["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));
        Assert.AreEqual("sr5", (importResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject summary = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/summary");
        Assert.AreEqual("Cerri", summary["name"]?.GetValue<string>());
        Assert.AreEqual("Apex", summary["alias"]?.GetValue<string>());

        JsonObject validation = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/validate");
        Assert.AreEqual(true, validation["isValid"]?.GetValue<bool>());
        Assert.IsTrue(validation["issues"] is JsonArray);

        JsonObject profile = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/profile");
        Assert.AreEqual("Cerri", profile["name"]?.GetValue<string>());
        Assert.AreEqual("Apex", profile["alias"]?.GetValue<string>());

        JsonObject skills = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/skills");
        Assert.IsTrue((skills["count"]?.GetValue<int>() ?? 0) > 0);

        JsonObject rules = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/rules");
        Assert.IsFalse(string.IsNullOrWhiteSpace(rules["gameEdition"]?.GetValue<string>()));

        JsonObject build = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/build");
        Assert.AreEqual("SumtoTen", build["buildMethod"]?.GetValue<string>());

        JsonObject movement = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/movement");
        Assert.IsFalse(string.IsNullOrWhiteSpace(movement["walk"]?.GetValue<string>()));

        JsonObject awakening = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/awakening");
        Assert.IsNotNull(awakening["magEnabled"]);

        JsonObject patchBody = new()
        {
            ["name"] = "Updated Name",
            ["alias"] = "Updated Alias",
            ["notes"] = "Updated notes"
        };

        JsonObject patchResponse = await PatchRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/metadata", patchBody);
        Assert.AreEqual("Updated Name", patchResponse["profile"]?["name"]?.GetValue<string>());

        JsonObject saveResponse = await PostRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/save", new JsonObject());
        Assert.AreEqual(workspaceId, saveResponse["id"]?.GetValue<string>());
        Assert.IsTrue((saveResponse["documentLength"]?.GetValue<int>() ?? 0) > 0);
        Assert.AreEqual("sr5", (saveResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject downloadResponse = await PostRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/download", new JsonObject());
        Assert.AreEqual(workspaceId, downloadResponse["id"]?.GetValue<string>());
        Assert.AreEqual("Chum5Xml", downloadResponse["format"]?.GetValue<string>());
        Assert.AreEqual("sr5", (downloadResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
        Assert.IsTrue((downloadResponse["fileName"]?.GetValue<string>() ?? string.Empty).EndsWith(".chum5", StringComparison.Ordinal));
        string contentBase64 = downloadResponse["contentBase64"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(contentBase64));
        Assert.IsTrue(Convert.FromBase64String(contentBase64).Length > 0);
    }

    [TestMethod]
    public async Task Workspace_import_accepts_content_base64_payload_with_utf8_bom()
    {
        using var client = CreateClient();

        byte[] xmlBytes = File.ReadAllBytes(FindTestFilePath("BLUE.chum5"));
        JsonObject importBody = new()
        {
            ["contentBase64"] = Convert.ToBase64String(xmlBytes),
            ["format"] = "Chum5Xml"
        };

        JsonObject importResponse = await PostRequiredJsonObject(client, "/api/workspaces/import", importBody);
        string workspaceId = importResponse["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));
        Assert.AreEqual("sr5", (importResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject summary = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/summary");
        Assert.AreEqual("Troy Simmons", summary["name"]?.GetValue<string>());
        Assert.AreEqual("BLUE", summary["alias"]?.GetValue<string>());
    }

    [TestMethod]
    public async Task Workspace_endpoints_preserve_ruleset_id_from_import_request()
    {
        using var client = CreateClient();

        string xml = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        JsonObject importBody = new()
        {
            ["xml"] = xml,
            ["rulesetId"] = "SR6"
        };

        JsonObject importResponse = await PostRequiredJsonObject(client, "/api/workspaces/import", importBody);
        string workspaceId = importResponse["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));
        Assert.AreEqual("sr6", (importResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject listed = await GetRequiredJsonObject(client, "/api/workspaces");
        JsonArray listedWorkspaces = listed["workspaces"]?.AsArray() ?? [];
        JsonObject listedItem = listedWorkspaces
            .Select(node => node as JsonObject)
            .FirstOrDefault(node => string.Equals(node?["id"]?.GetValue<string>(), workspaceId, StringComparison.Ordinal))
            ?? new JsonObject();
        Assert.IsTrue(listedItem.Count > 0, "Expected workspace list entry for imported workspace.");
        Assert.AreEqual("sr6", (listedItem["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject saveResponse = await PostRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/save", new JsonObject());
        Assert.AreEqual("sr6", (saveResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());

        JsonObject downloadResponse = await PostRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/download", new JsonObject());
        Assert.AreEqual("sr6", (downloadResponse["rulesetId"]?.GetValue<string>() ?? string.Empty).ToLowerInvariant());
    }

    [TestMethod]
    public async Task Workspace_list_and_close_endpoints_manage_open_workspace_collection()
    {
        using var client = CreateClient();

        string xmlA = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        string xmlB = File.ReadAllText(FindTestFilePath("BLUE.chum5"));
        JsonObject importBodyA = new() { ["xml"] = xmlA };
        JsonObject importBodyB = new() { ["xml"] = xmlB };

        JsonObject importA = await PostRequiredJsonObject(client, "/api/workspaces/import", importBodyA);
        JsonObject importB = await PostRequiredJsonObject(client, "/api/workspaces/import", importBodyB);
        string workspaceA = importA["id"]?.GetValue<string>() ?? string.Empty;
        string workspaceB = importB["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceA));
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceB));

        JsonObject listed = await GetRequiredJsonObject(client, "/api/workspaces");
        Assert.IsTrue((listed["count"]?.GetValue<int>() ?? 0) >= 2);
        JsonArray listedWorkspaces = listed["workspaces"]?.AsArray() ?? [];
        CollectionAssert.IsSubsetOf(
            new[] { workspaceA, workspaceB },
            listedWorkspaces
                .Select(node => node?["id"]?.GetValue<string>() ?? string.Empty)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray());

        using HttpResponseMessage closeResponse = await client.DeleteAsync($"/api/workspaces/{workspaceA}");
        Assert.AreEqual(204, (int)closeResponse.StatusCode);

        JsonObject listedAfterClose = await GetRequiredJsonObject(client, "/api/workspaces");
        JsonArray listedAfterCloseItems = listedAfterClose["workspaces"]?.AsArray() ?? [];
        Assert.IsFalse(listedAfterCloseItems.Any(node => string.Equals(node?["id"]?.GetValue<string>(), workspaceA, StringComparison.Ordinal)));
        Assert.IsTrue(listedAfterCloseItems.Any(node => string.Equals(node?["id"]?.GetValue<string>(), workspaceB, StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Workspace_list_endpoint_honors_maxCount_query_parameter()
    {
        using var client = CreateClient();

        string xmlA = File.ReadAllText(FindTestFilePath("Apex Predator.chum5"));
        string xmlB = File.ReadAllText(FindTestFilePath("BLUE.chum5"));
        await PostRequiredJsonObject(client, "/api/workspaces/import", new JsonObject { ["xml"] = xmlA });
        await PostRequiredJsonObject(client, "/api/workspaces/import", new JsonObject { ["xml"] = xmlB });

        JsonObject listed = await GetRequiredJsonObject(client, "/api/workspaces?maxCount=1");
        Assert.AreEqual(1, listed["count"]?.GetValue<int>());
        JsonArray listedWorkspaces = listed["workspaces"]?.AsArray() ?? [];
        Assert.AreEqual(1, listedWorkspaces.Count);
    }

    [TestMethod]
    public async Task Workspace_import_returns_bad_request_for_invalid_summary_payload()
    {
        using var client = CreateClient();

        JsonObject payload = new()
        {
            ["xml"] = "<character><name>Broken</name><alias>X</alias><metatype>Human</metatype><buildmethod>Priority</buildmethod><createdversion>1.0</createdversion><appversion>1.0</appversion><karma>not-a-number</karma><nuyen>2500</nuyen><created>True</created></character>"
        };

        using StringContent request = new(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync("/api/workspaces/import", request);
        string body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(400, (int)response.StatusCode, body);
        StringAssert.Contains(body, "error");
    }

    [TestMethod]
    public async Task Workspace_section_endpoint_matches_legacy_section_payload_for_all_sections()
    {
        using var client = CreateClient();

        string xml = File.ReadAllText(FindTestFilePath("BLUE.chum5"));
        JsonObject payload = new()
        {
            ["xml"] = xml
        };

        JsonObject importResponse = await PostRequiredJsonObject(client, "/api/workspaces/import", payload);
        string workspaceId = importResponse["id"]?.GetValue<string>() ?? string.Empty;
        Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));

        foreach (string sectionId in AllSectionIds)
        {
            JsonObject legacySection = await PostRequiredJsonObject(client, $"/api/characters/sections/{sectionId}", payload);
            JsonObject workspaceSection = await GetRequiredJsonObject(client, $"/api/workspaces/{workspaceId}/sections/{sectionId}");

            Assert.AreEqual(legacySection.ToJsonString(), workspaceSection.ToJsonString(), $"Section mismatch for '{sectionId}'.");
        }
    }

    private static string FindTestFilePath(string fileName)
    {
        string? root = Environment.GetEnvironmentVariable("CHUMMER_REPO_ROOT");
        string[] candidates =
        {
            Path.Combine(Directory.GetCurrentDirectory(), "Chummer.Tests", "TestFiles", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", fileName),
            Path.Combine(AppContext.BaseDirectory, "TestFiles", fileName),
            Path.Combine("/src", "Chummer.Tests", "TestFiles", fileName),
            string.IsNullOrWhiteSpace(root) ? string.Empty : Path.Combine(root, "Chummer.Tests", "TestFiles", fileName)
        };

        string? match = candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
        if (match is null)
            throw new FileNotFoundException("Could not locate test file.", fileName);

        return match;
    }

    private static HttpClient CreateClient(bool includeApiKey = true)
    {
        var client = new HttpClient
        {
            BaseAddress = BaseUri,
            Timeout = HttpTimeout
        };

        if (includeApiKey && !string.IsNullOrWhiteSpace(ApiKey))
        {
            client.DefaultRequestHeaders.Remove("X-Api-Key");
            client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }

        return client;
    }

    private static async Task<JsonObject> GetRequiredJsonObject(HttpClient client, string relativePath)
    {
        using HttpResponseMessage response = await client.GetAsync(relativePath);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"GET {relativePath} failed with {(int)response.StatusCode}: {content}");

        JsonNode parsed = JsonNode.Parse(content);
        Assert.IsInstanceOfType<JsonObject>(parsed);
        return (JsonObject)parsed!;
    }

    private static async Task<JsonObject> PostRequiredJsonObject(HttpClient client, string relativePath, JsonObject payload)
    {
        using var request = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(relativePath, request);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST {relativePath} failed with {(int)response.StatusCode}: {content}");

        JsonNode parsed = JsonNode.Parse(content);
        Assert.IsInstanceOfType<JsonObject>(parsed);
        return (JsonObject)parsed!;
    }

    private static async Task<JsonObject> PatchRequiredJsonObject(HttpClient client, string relativePath, JsonObject payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, relativePath)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json")
        };
        using HttpResponseMessage response = await client.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"PATCH {relativePath} failed with {(int)response.StatusCode}: {content}");

        JsonNode parsed = JsonNode.Parse(content);
        Assert.IsInstanceOfType<JsonObject>(parsed);
        return (JsonObject)parsed!;
    }

    private static Uri ResolveBaseUri()
    {
        string? raw = Environment.GetEnvironmentVariable("CHUMMER_API_BASE_URL");
        if (string.IsNullOrWhiteSpace(raw))
            raw = Environment.GetEnvironmentVariable("CHUMMER_WEB_BASE_URL");
        if (string.IsNullOrWhiteSpace(raw))
            raw = "http://chummer-api:8080";

        if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri? uri))
            throw new InvalidOperationException($"Invalid CHUMMER_API_BASE_URL/CHUMMER_WEB_BASE_URL: '{raw}'");

        return uri;
    }

    private static string? ResolveApiKey()
    {
        return Environment.GetEnvironmentVariable("CHUMMER_API_KEY");
    }

    private static string? ResolveExpectedAmendId()
    {
        string? configured = Environment.GetEnvironmentVariable("CHUMMER_AMENDS_EXPECTED_ID");
        if (string.IsNullOrWhiteSpace(configured))
            return null;

        return configured;
    }

    private static TimeSpan ResolveHttpTimeout()
    {
        string? raw = Environment.GetEnvironmentVariable("CHUMMER_API_TEST_TIMEOUT_SECONDS");
        if (int.TryParse(raw, out int seconds) && seconds > 0)
            return TimeSpan.FromSeconds(seconds);

        return TimeSpan.FromSeconds(45);
    }
}
