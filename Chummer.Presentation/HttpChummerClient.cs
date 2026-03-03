using System.Net.Http.Json;
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

        MetadataResponse? payload = await response.Content.ReadFromJsonAsync<MetadataResponse>(ct);
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

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken ct)
    {
        T? data = await _httpClient.GetFromJsonAsync<T>(path, ct);
        if (data is null)
            throw new InvalidOperationException($"API returned an empty payload for '{path}'.");

        return data;
    }

    private sealed record MetadataResponse(CharacterProfileSection Profile);
}
