using System.Net.Http.Json;
using Chummer.Contracts.Api;
using Chummer.Contracts.Characters;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation;

public sealed class HttpChummerClient : IChummerClient
{
    private readonly HttpClient _httpClient;

    public HttpChummerClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WorkspaceImportResult> ImportAsync(string xml, CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "/api/workspaces/import",
            new CharacterXmlRequest(xml),
            ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Workspace import failed with HTTP {(int)response.StatusCode}.");

        WorkspaceImportResponse? payload = await response.Content.ReadFromJsonAsync<WorkspaceImportResponse>(ct);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Id))
            throw new InvalidOperationException("Import response did not include a workspace id.");

        return new WorkspaceImportResult(new CharacterWorkspaceId(payload.Id), payload.Summary);
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

    public async Task<CommandResult<string>> SaveAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            $"/api/workspaces/{id.Value}/save",
            new { },
            ct);

        if (!response.IsSuccessStatusCode)
        {
            return new CommandResult<string>(
                Success: false,
                Value: null,
                Error: $"HTTP {(int)response.StatusCode}");
        }

        WorkspaceSaveResponse? payload = await response.Content.ReadFromJsonAsync<WorkspaceSaveResponse>(ct);

        if (payload is null || string.IsNullOrWhiteSpace(payload.Xml))
        {
            return new CommandResult<string>(
                Success: false,
                Value: null,
                Error: "Save response did not include xml.");
        }

        return new CommandResult<string>(
            Success: true,
            Value: payload.Xml,
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
