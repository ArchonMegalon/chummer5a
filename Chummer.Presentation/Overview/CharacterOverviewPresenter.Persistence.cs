using System.IO;
using System.Text;
using System.Text.Json;
using Chummer.Contracts.Api;
using Chummer.Contracts.Rulesets;
using Chummer.Contracts.Workspaces;

namespace Chummer.Presentation.Overview;

public sealed partial class CharacterOverviewPresenter
{
    public async Task UpdateMetadataAsync(UpdateWorkspaceMetadata command, CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with
            {
                Error = "No workspace loaded."
            });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null,
            PendingDownload = null
        });

        try
        {
            WorkspaceMetadataUpdateResult result = await _workspacePersistenceService.UpdateMetadataAsync(
                _client,
                _currentWorkspace.Value,
                command,
                State.Preferences,
                ct);
            if (!result.Success || result.Profile is null)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error
                });
                return;
            }

            WorkspaceSessionState session = _workspaceSessionPresenter.SetSavedStatus(_currentWorkspace.Value, hasSavedWorkspace: false);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Session = session,
                OpenWorkspaces = session.OpenWorkspaces,
                WorkspaceId = _currentWorkspace,
                Profile = result.Profile,
                Preferences = result.Preferences,
                HasSavedWorkspace = false
            });
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message
            });
        }
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with
            {
                Error = "No workspace loaded."
            });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceSaveResult result = await _workspacePersistenceService.SaveAsync(_client, _currentWorkspace.Value, ct);
            if (!result.Success)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error
                });
                return;
            }

            WorkspaceSessionState session = _workspaceSessionPresenter.SetSavedStatus(_currentWorkspace.Value, hasSavedWorkspace: true);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Session = session,
                OpenWorkspaces = session.OpenWorkspaces,
                WorkspaceId = _currentWorkspace,
                HasSavedWorkspace = true,
                Notice = "Workspace saved.",
                PendingDownload = null
            });
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message
            });
        }
    }

    public async Task DownloadAsync(CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with
            {
                Error = "No workspace loaded."
            });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null,
            PendingDownload = null
        });

        try
        {
            WorkspaceDownloadResult result = await _workspacePersistenceService.DownloadAsync(_client, _currentWorkspace.Value, ct);
            if (!result.Success || result.Receipt is null)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error,
                    PendingDownload = null
                });
                return;
            }

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Notice = $"Download prepared: {result.Receipt.FileName} ({result.Receipt.DocumentLength} bytes).",
                PendingDownload = result.Receipt,
                PendingDownloadVersion = State.PendingDownloadVersion + 1
            });
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message,
                PendingDownload = null
            });
        }
    }

    public async Task ExportAsync(CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with
            {
                Error = "No workspace loaded."
            });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null,
            PendingDownload = null
        });

        try
        {
            CharacterWorkspaceId workspaceId = _currentWorkspace.Value;
            DataExportBundle bundle = await _client.ExportAsync(workspaceId, ct);
            WorkspaceDownloadReceipt receipt = BuildExportDownloadReceipt(workspaceId, bundle);

            Publish(State with
            {
                ActiveDialog = null,
                IsBusy = false,
                Error = null,
                Notice = $"Export bundle prepared: {receipt.FileName} ({receipt.DocumentLength} bytes).",
                PendingDownload = receipt,
                PendingDownloadVersion = State.PendingDownloadVersion + 1
            });
        }
        catch (Exception ex)
        {
            Publish(State with
            {
                IsBusy = false,
                Error = ex.Message,
                PendingDownload = null
            });
        }
    }

    private WorkspaceDownloadReceipt BuildExportDownloadReceipt(CharacterWorkspaceId workspaceId, DataExportBundle bundle)
    {
        string json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        string rulesetId = ResolveWorkspaceRulesetId(workspaceId);
        string baseFileName = string.IsNullOrWhiteSpace(bundle.Summary.Name)
            ? workspaceId.Value
            : bundle.Summary.Name;
        string sanitizedFileName = SanitizeFileName(baseFileName);

        return new WorkspaceDownloadReceipt(
            Id: workspaceId,
            Format: WorkspaceDocumentFormat.Json,
            ContentBase64: Convert.ToBase64String(bytes),
            FileName: $"{sanitizedFileName}-export.json",
            DocumentLength: bytes.Length,
            RulesetId: rulesetId);
    }

    private string ResolveWorkspaceRulesetId(CharacterWorkspaceId workspaceId)
    {
        OpenWorkspaceState? workspace = State.OpenWorkspaces.FirstOrDefault(
            candidate => string.Equals(candidate.Id.Value, workspaceId.Value, StringComparison.Ordinal));
        return workspace is null
            ? RulesetDefaults.Sr5
            : RulesetDefaults.Normalize(workspace.RulesetId);
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (char character in value)
            builder.Append(invalidChars.Contains(character) ? '_' : character);

        string sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "workspace" : sanitized;
    }
}
