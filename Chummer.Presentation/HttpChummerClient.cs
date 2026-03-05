using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Linq;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation;

public sealed class HttpChummerClient : IChummerClient
{
    private static readonly TimeSpan ShellBootstrapRequestTimeout = TimeSpan.FromSeconds(10);
    private readonly HttpClient _httpClient;

    public HttpChummerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ShellUserPreferences> GetShellPreferencesAsync(CancellationToken ct)
    {
        ShellUserPreferences? response = await _httpClient.GetFromJsonAsync<ShellUserPreferences>(
            "/api/shell/preferences",
            ct);
        if (response is null)
            throw new InvalidOperationException("Shell preferences response was empty.");

        return new ShellUserPreferences(RulesetDefaults.Normalize(response.PreferredRulesetId));
    }

    public async Task SaveShellPreferencesAsync(ShellUserPreferences preferences, CancellationToken ct)
    {
        ShellUserPreferences payload = new(RulesetDefaults.Normalize(preferences.PreferredRulesetId));
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "/api/shell/preferences",
            payload,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Saving shell preferences failed with HTTP {(int)response.StatusCode}.");
        }
    }

    public async Task<WorkspaceImportResult> ImportAsync(WorkspaceImportDocument document, CancellationToken ct)
    {
        string contentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(document.Content));
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "/api/workspaces/import",
            new WorkspaceImportRequest(
                ContentBase64: contentBase64,
                Format: document.Format.ToString(),
                Xml: null,
                RulesetId: document.RulesetId),
            ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Workspace import failed with HTTP {(int)response.StatusCode}.");

        WorkspaceImportResponse? payload = await response.Content.ReadFromJsonAsync<WorkspaceImportResponse>(ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Id))
            throw new InvalidOperationException("Import response did not include a workspace id.");

        return new WorkspaceImportResult(new CharacterWorkspaceId(payload.Id), payload.Summary, RulesetDefaults.Normalize(payload.RulesetId));
    }

    public async Task<IReadOnlyList<WorkspaceListItem>> ListWorkspacesAsync(CancellationToken ct)
    {
        WorkspaceListResponse response = await GetRequiredAsync<WorkspaceListResponse>("/api/workspaces", ct);

        return response.Workspaces
            .Select(workspace => new WorkspaceListItem(
                Id: new CharacterWorkspaceId(workspace.Id),
                Summary: workspace.Summary,
                LastUpdatedUtc: workspace.LastUpdatedUtc,
                RulesetId: RulesetDefaults.Normalize(workspace.RulesetId)))
            .ToArray();
    }

    public async Task<bool> CloseWorkspaceAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"/api/workspaces/{id.Value}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return false;

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Workspace close failed with HTTP {(int)response.StatusCode}.");

        return true;
    }

    public async Task<IReadOnlyList<AppCommandDefinition>> GetCommandsAsync(string? rulesetId, CancellationToken ct)
    {
        string normalizedRuleset = RulesetDefaults.Normalize(rulesetId);
        AppCommandCatalogResponse? response = await _httpClient.GetFromJsonAsync<AppCommandCatalogResponse>(
            $"/api/commands?ruleset={Uri.EscapeDataString(normalizedRuleset)}",
            ct);
        if (response is null)
            throw new InvalidOperationException("Command catalog response was empty.");

        return response.Commands;
    }

    public async Task<IReadOnlyList<NavigationTabDefinition>> GetNavigationTabsAsync(string? rulesetId, CancellationToken ct)
    {
        string normalizedRuleset = RulesetDefaults.Normalize(rulesetId);
        NavigationTabCatalogResponse? response = await _httpClient.GetFromJsonAsync<NavigationTabCatalogResponse>(
            $"/api/navigation-tabs?ruleset={Uri.EscapeDataString(normalizedRuleset)}",
            ct);
        if (response is null)
            throw new InvalidOperationException("Navigation tab catalog response was empty.");

        return response.Tabs;
    }

    public async Task<ShellBootstrapSnapshot> GetShellBootstrapAsync(string? rulesetId, CancellationToken ct)
    {
        string path = "/api/shell/bootstrap";
        if (!string.IsNullOrWhiteSpace(rulesetId))
        {
            string normalizedRuleset = RulesetDefaults.Normalize(rulesetId);
            path += $"?ruleset={Uri.EscapeDataString(normalizedRuleset)}";
        }

        using var bootstrapTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        bootstrapTimeoutCts.CancelAfter(ShellBootstrapRequestTimeout);
        ShellBootstrapResponse? response;
        try
        {
            response = await _httpClient.GetFromJsonAsync<ShellBootstrapResponse>(path, bootstrapTimeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"Shell bootstrap request timed out after {ShellBootstrapRequestTimeout.TotalSeconds:0} seconds.",
                ex);
        }

        if (response is null)
        {
            throw new InvalidOperationException("Shell bootstrap response was empty.");
        }

        IReadOnlyList<WorkspaceListItem> workspaces = response.Workspaces
            .Select(workspace => new WorkspaceListItem(
                Id: new CharacterWorkspaceId(workspace.Id),
                Summary: workspace.Summary,
                LastUpdatedUtc: workspace.LastUpdatedUtc,
                RulesetId: RulesetDefaults.Normalize(workspace.RulesetId)))
            .ToArray();

        return new ShellBootstrapSnapshot(
            RulesetId: RulesetDefaults.Normalize(response.RulesetId),
            Commands: response.Commands,
            NavigationTabs: response.NavigationTabs,
            Workspaces: workspaces,
            PreferredRulesetId: RulesetDefaults.Normalize(response.PreferredRulesetId),
            ActiveRulesetId: RulesetDefaults.Normalize(response.ActiveRulesetId));
    }

    public async Task<JsonNode> GetSectionAsync(CharacterWorkspaceId id, string sectionId, CancellationToken ct)
    {
        JsonNode? response = await _httpClient.GetFromJsonAsync<JsonNode>($"/api/workspaces/{id.Value}/sections/{sectionId}", ct);
        if (response is null)
            throw new InvalidOperationException($"Section '{sectionId}' response was empty.");

        return response;
    }

    public async Task<CharacterFileSummary> GetSummaryAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterFileSummary>($"/api/workspaces/{id.Value}/summary", ct);
    }

    public async Task<CharacterValidationResult> ValidateAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterValidationResult>($"/api/workspaces/{id.Value}/validate", ct);
    }

    public async Task<CharacterProfileSection> GetProfileAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterProfileSection>($"/api/workspaces/{id.Value}/profile", ct);
    }

    public async Task<CharacterProgressSection> GetProgressAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterProgressSection>($"/api/workspaces/{id.Value}/progress", ct);
    }

    public async Task<CharacterSkillsSection> GetSkillsAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterSkillsSection>($"/api/workspaces/{id.Value}/skills", ct);
    }

    public async Task<CharacterRulesSection> GetRulesAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterRulesSection>($"/api/workspaces/{id.Value}/rules", ct);
    }

    public async Task<CharacterBuildSection> GetBuildAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterBuildSection>($"/api/workspaces/{id.Value}/build", ct);
    }

    public async Task<CharacterMovementSection> GetMovementAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterMovementSection>($"/api/workspaces/{id.Value}/movement", ct);
    }

    public async Task<CharacterAwakeningSection> GetAwakeningAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        return await GetRequiredAsync<CharacterAwakeningSection>($"/api/workspaces/{id.Value}/awakening", ct);
    }

    public async Task<CommandResult<CharacterProfileSection>> UpdateMetadataAsync(
        CharacterWorkspaceId id,
        UpdateWorkspaceMetadata command,
        CancellationToken ct)
    {
        using HttpRequestMessage request = new(HttpMethod.Patch, $"/api/workspaces/{id.Value}/metadata")
        {
            Content = JsonContent.Create(command)
        };

        using HttpResponseMessage response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return new CommandResult<CharacterProfileSection>(
                Success: false,
                Value: null,
                Error: $"HTTP {(int)response.StatusCode}");
        }

        WorkspaceMetadataResponse? payload = await response.Content.ReadFromJsonAsync<WorkspaceMetadataResponse>(ct);
        if (payload?.Profile is null)
        {
            return new CommandResult<CharacterProfileSection>(
                Success: false,
                Value: null,
                Error: "Metadata response did not include a profile payload.");
        }

        return new CommandResult<CharacterProfileSection>(
            Success: true,
            Value: payload.Profile,
            Error: null);
    }

    public async Task<CommandResult<WorkspaceSaveReceipt>> SaveAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"/api/workspaces/{id.Value}/save",
            new { },
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return new CommandResult<WorkspaceSaveReceipt>(
                Success: false,
                Value: null,
                Error: $"HTTP {(int)response.StatusCode}");
        }

        WorkspaceSaveResponse? payload = await response.Content.ReadFromJsonAsync<WorkspaceSaveResponse>(ct);

        if (payload is null || string.IsNullOrWhiteSpace(payload.Id))
        {
            return new CommandResult<WorkspaceSaveReceipt>(
                Success: false,
                Value: null,
                Error: "Save response did not include workspace id.");
        }

        return new CommandResult<WorkspaceSaveReceipt>(
            Success: true,
            Value: new WorkspaceSaveReceipt(
                Id: new CharacterWorkspaceId(payload.Id),
                DocumentLength: payload.DocumentLength,
                RulesetId: RulesetDefaults.Normalize(payload.RulesetId)),
            Error: null);
    }

    public async Task<CommandResult<WorkspaceDownloadReceipt>> DownloadAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"/api/workspaces/{id.Value}/download",
            new { },
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return new CommandResult<WorkspaceDownloadReceipt>(
                Success: false,
                Value: null,
                Error: $"HTTP {(int)response.StatusCode}");
        }

        WorkspaceDownloadResponse? payload = await response.Content.ReadFromJsonAsync<WorkspaceDownloadResponse>(ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Id))
        {
            return new CommandResult<WorkspaceDownloadReceipt>(
                Success: false,
                Value: null,
                Error: "Download response did not include workspace id.");
        }

        WorkspaceDocumentFormat format = WorkspaceDocumentFormat.Chum5Xml;
        if (!string.IsNullOrWhiteSpace(payload.Format)
            && Enum.TryParse(payload.Format, ignoreCase: true, out WorkspaceDocumentFormat parsedFormat))
        {
            format = parsedFormat;
        }

        return new CommandResult<WorkspaceDownloadReceipt>(
            Success: true,
            Value: new WorkspaceDownloadReceipt(
                Id: new CharacterWorkspaceId(payload.Id),
                Format: format,
                ContentBase64: payload.ContentBase64 ?? string.Empty,
                FileName: payload.FileName ?? $"{payload.Id}.chum5",
                DocumentLength: payload.DocumentLength,
                RulesetId: RulesetDefaults.Normalize(payload.RulesetId)),
            Error: null);
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken ct)
    {
        T? data = await _httpClient.GetFromJsonAsync<T>(path, ct);
        if (data is null)
            throw new InvalidOperationException($"API returned an empty payload for '{path}'.");

        return data;
    }
}
