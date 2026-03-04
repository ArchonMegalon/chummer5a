using Chummer.Contracts.Characters;
using Chummer.Contracts.Presentation;
using Chummer.Contracts.Workspaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Chummer.Presentation.Overview;

public sealed class CharacterOverviewPresenter : ICharacterOverviewPresenter
{
    private static readonly Regex DiceExpressionRegex = new(@"^\s*(\d+)d(\d+)([+-]\d+)?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly IChummerClient _client;
    private readonly IWorkspaceSessionManager _workspaceSessionManager;
    private CharacterWorkspaceId? _currentWorkspace;

    public CharacterOverviewPresenter(IChummerClient client, IWorkspaceSessionManager? workspaceSessionManager = null)
    {
        _client = client;
        _workspaceSessionManager = workspaceSessionManager ?? new WorkspaceSessionManager();
    }

    public CharacterOverviewState State { get; private set; } = CharacterOverviewState.Empty;

    public event EventHandler? StateChanged;

    public async Task InitializeAsync(CancellationToken ct)
    {
        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            Task<IReadOnlyList<AppCommandDefinition>> commandsTask = _client.GetCommandsAsync(ct);
            Task<IReadOnlyList<NavigationTabDefinition>> tabsTask = _client.GetNavigationTabsAsync(ct);
            Task<IReadOnlyList<WorkspaceListItem>> workspacesTask = _client.ListWorkspacesAsync(ct);
            await Task.WhenAll(commandsTask, tabsTask, workspacesTask);

            IReadOnlyList<OpenWorkspaceState> openWorkspaces = _workspaceSessionManager.Restore(workspacesTask.Result);

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                Commands = commandsTask.Result,
                NavigationTabs = tabsTask.Result,
                OpenWorkspaces = openWorkspaces,
                Notice = openWorkspaces.Count == 0
                    ? State.Notice
                    : $"Restored {openWorkspaces.Count} workspace(s)."
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

    public async Task ImportAsync(WorkspaceImportDocument document, CancellationToken ct)
    {
        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            WorkspaceImportResult imported = await _client.ImportAsync(document, ct);
            await LoadWorkspaceAsync(imported.Id, ct);
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

    public async Task LoadAsync(CharacterWorkspaceId id, CancellationToken ct)
    {
        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            await LoadWorkspaceAsync(id, ct);
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

    public async Task ExecuteCommandAsync(string commandId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            Publish(State with { Error = "Command id is required." });
            return;
        }

        Publish(State with
        {
            LastCommandId = commandId,
            Error = null
        });

        switch (commandId)
        {
            case "save_character":
            case "save_character_as":
                await SaveAsync(ct);
                return;
            case "file":
            case "edit":
            case "special":
            case "tools":
            case "windows":
            case "help":
                Publish(State with
                {
                    Error = null,
                    Notice = $"Menu '{commandId}' is handled by the active UI shell."
                });
                return;
            case "refresh_character":
                if (_currentWorkspace is null)
                {
                    Publish(State with { Error = "No workspace loaded." });
                    return;
                }

                await LoadAsync(_currentWorkspace.Value, ct);
                return;
            case "new_character":
                _currentWorkspace = null;
                Publish(CharacterOverviewState.Empty with
                {
                    Commands = State.Commands,
                    NavigationTabs = State.NavigationTabs,
                    LastCommandId = commandId,
                    Notice = "New character workspace initialized.",
                    Preferences = State.Preferences,
                    OpenWorkspaces = State.OpenWorkspaces
                });
                return;
            case "new_critter":
                _currentWorkspace = null;
                Publish(CharacterOverviewState.Empty with
                {
                    Commands = State.Commands,
                    NavigationTabs = State.NavigationTabs,
                    LastCommandId = commandId,
                    Notice = "New critter workspace initialized.",
                    Preferences = State.Preferences,
                    OpenWorkspaces = State.OpenWorkspaces
                });
                return;
            case "open_character":
            case "open_for_printing":
            case "open_for_export":
                Publish(State with
                {
                    Error = null,
                    Notice = "Use the file import action in this head to open a character document."
                });
                return;
            case "close_all":
            case "restart":
                CharacterWorkspaceId[] workspaceIdsToClose = State.OpenWorkspaces
                    .Select(workspace => workspace.Id)
                    .Distinct()
                    .ToArray();

                foreach (CharacterWorkspaceId workspaceId in workspaceIdsToClose)
                {
                    try
                    {
                        await _client.CloseWorkspaceAsync(workspaceId, ct);
                    }
                    catch
                    {
                        // Keep resetting local shell state even if a close request fails remotely.
                    }
                }

                _currentWorkspace = null;
                Publish(CharacterOverviewState.Empty with
                {
                    Commands = State.Commands,
                    NavigationTabs = State.NavigationTabs,
                    LastCommandId = commandId,
                    Notice = "Workspace reset complete.",
                    Preferences = State.Preferences,
                    OpenWorkspaces = []
                });
                return;
            case "close_window":
                if (_currentWorkspace is null)
                {
                    Publish(State with
                    {
                        Error = null,
                        Notice = "No open workspace to close."
                    });
                    return;
                }

                CharacterWorkspaceId closingWorkspace = _currentWorkspace.Value;
                bool closed;
                try
                {
                    closed = await _client.CloseWorkspaceAsync(closingWorkspace, ct);
                }
                catch
                {
                    closed = false;
                }

                IReadOnlyList<OpenWorkspaceState> remainingWorkspaces = _workspaceSessionManager.Close(State.OpenWorkspaces, closingWorkspace);

                if (remainingWorkspaces.Count == 0)
                {
                    _currentWorkspace = null;
                    Publish(CharacterOverviewState.Empty with
                    {
                        Commands = State.Commands,
                        NavigationTabs = State.NavigationTabs,
                        LastCommandId = commandId,
                        Notice = closed
                            ? "Closed active workspace."
                            : "Active workspace was already closed.",
                        Preferences = State.Preferences,
                        OpenWorkspaces = []
                    });
                    return;
                }

                CharacterWorkspaceId? nextWorkspaceId = _workspaceSessionManager.SelectNext(remainingWorkspaces);
                if (nextWorkspaceId is null)
                {
                    _currentWorkspace = null;
                    Publish(CharacterOverviewState.Empty with
                    {
                        Commands = State.Commands,
                        NavigationTabs = State.NavigationTabs,
                        LastCommandId = commandId,
                        Notice = "Closed active workspace.",
                        Preferences = State.Preferences,
                        OpenWorkspaces = []
                    });
                    return;
                }

                CharacterWorkspaceId selectedWorkspace = nextWorkspaceId.Value;
                await LoadWorkspaceAsync(selectedWorkspace, ct, remainingWorkspaces);
                Publish(State with
                {
                    LastCommandId = commandId,
                    Notice = closed
                        ? $"Closed active workspace. Switched to '{selectedWorkspace.Value}'."
                        : $"Active workspace was already closed. Switched to '{selectedWorkspace.Value}'."
                });
                return;
            case "new_window":
            case "wiki":
            case "discord":
            case "revision_history":
            case "dumpshock":
            case "print_setup":
            case "print_multiple":
            case "print_character":
            case "dice_roller":
            case "global_settings":
            case "character_settings":
            case "translator":
            case "xml_editor":
            case "master_index":
            case "character_roster":
            case "data_exporter":
            case "export_character":
            case "report_bug":
            case "about":
            case "hero_lab_importer":
            case "update":
                Publish(State with
                {
                    Error = null,
                    ActiveDialog = CreateCommandDialog(commandId)
                });
                return;
            case "copy":
            case "paste":
                Publish(State with
                {
                    Error = null,
                    Notice = $"Command '{commandId}' dispatched to the active section editor."
                });
                return;
            default:
                Publish(State with
                {
                    Error = $"Command '{commandId}' is not implemented in shared presenter yet."
                });
                return;
        }
    }

    public Task HandleUiControlAsync(string controlId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(controlId))
        {
            Publish(State with { Error = "UI control id is required." });
            return Task.CompletedTask;
        }

        Publish(State with
        {
            Error = null,
            ActiveDialog = CreateUiControlDialog(controlId)
        });

        return Task.CompletedTask;
    }

    public async Task ExecuteWorkspaceActionAsync(WorkspaceSurfaceActionDefinition action, CancellationToken ct)
    {
        if (action is null)
        {
            Publish(State with { Error = "Workspace action is required." });
            return;
        }

        if (action.RequiresOpenCharacter && _currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        switch (action.Kind)
        {
            case WorkspaceSurfaceActionKind.Section:
                await LoadSectionAsync(action.TargetId, action.TabId, action.Id, ct);
                return;
            case WorkspaceSurfaceActionKind.Summary:
                await RenderSummaryAction(action, ct);
                return;
            case WorkspaceSurfaceActionKind.Validate:
                await RenderValidateAction(action, ct);
                return;
            case WorkspaceSurfaceActionKind.Metadata:
                Publish(State with
                {
                    ActiveTabId = action.TabId,
                    ActiveActionId = action.Id,
                    Error = null,
                    ActiveDialog = CreateMetadataDialog()
                });
                return;
            case WorkspaceSurfaceActionKind.Command:
                await ExecuteCommandAsync(action.TargetId, ct);
                Publish(State with
                {
                    ActiveTabId = action.TabId,
                    ActiveActionId = action.Id
                });
                return;
            default:
                Publish(State with { Error = $"Unsupported workspace action kind '{action.Kind}'." });
                return;
        }
    }

    public Task UpdateDialogFieldAsync(string fieldId, string? value, CancellationToken ct)
    {
        DesktopDialogState? dialog = State.ActiveDialog;
        if (dialog is null)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(fieldId))
        {
            Publish(State with { Error = "Dialog field id is required." });
            return Task.CompletedTask;
        }

        DesktopDialogField[] updatedFields = dialog.Fields
            .Select(field => string.Equals(field.Id, fieldId, StringComparison.Ordinal)
                ? field with { Value = NormalizeDialogFieldValue(field, value) }
                : field)
            .ToArray();
        Publish(State with
        {
            ActiveDialog = dialog with { Fields = updatedFields },
            Error = null
        });
        return Task.CompletedTask;
    }

    public async Task ExecuteDialogActionAsync(string actionId, CancellationToken ct)
    {
        DesktopDialogState? dialog = State.ActiveDialog;
        if (dialog is null)
            return;

        if (string.IsNullOrWhiteSpace(actionId))
        {
            Publish(State with { Error = "Dialog action id is required." });
            return;
        }

        switch (actionId)
        {
            case "cancel":
            case "close":
                Publish(State with
                {
                    ActiveDialog = null,
                    Error = null
                });
                return;
            default:
                break;
        }

        if (string.Equals(dialog.Id, "dialog.workspace.metadata", StringComparison.Ordinal) && string.Equals(actionId, "apply_metadata", StringComparison.Ordinal))
        {
            await ApplyMetadataDialogAsync(dialog, ct);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.dice_roller", StringComparison.Ordinal) && string.Equals(actionId, "roll", StringComparison.Ordinal))
        {
            RollDice(dialog);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.global_settings", StringComparison.Ordinal) && string.Equals(actionId, "save", StringComparison.Ordinal))
        {
            ApplyGlobalSettings(dialog);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.character_settings", StringComparison.Ordinal) && string.Equals(actionId, "save", StringComparison.Ordinal))
        {
            ApplyCharacterSettings(dialog);
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.open_notes", StringComparison.Ordinal) && string.Equals(actionId, "save", StringComparison.Ordinal))
        {
            string notes = GetDialogFieldValue(dialog, "uiNotesEditor") ?? string.Empty;
            Publish(State with
            {
                ActiveDialog = null,
                Error = null,
                Preferences = State.Preferences with
                {
                    CharacterNotes = notes
                },
                Notice = "Notes saved."
            });
            return;
        }

        if (string.Equals(dialog.Id, "dialog.ui.contact_connection", StringComparison.Ordinal) && string.Equals(actionId, "apply", StringComparison.Ordinal))
        {
            string connection = GetDialogFieldValue(dialog, "uiContactConnection") ?? "0";
            string loyalty = GetDialogFieldValue(dialog, "uiContactLoyalty") ?? "0";
            Publish(State with
            {
                ActiveDialog = null,
                Error = null,
                Notice = $"Contact connection/loyalty applied ({connection}/{loyalty})."
            });
            return;
        }

        if ((string.Equals(dialog.Id, "dialog.data_exporter", StringComparison.Ordinal)
            || string.Equals(dialog.Id, "dialog.export_character", StringComparison.Ordinal))
            && string.Equals(actionId, "download", StringComparison.Ordinal))
        {
            Publish(State with
            {
                ActiveDialog = null,
                Error = null,
                Notice = "Export bundle prepared for download."
            });
            return;
        }

        Publish(State with
        {
            ActiveDialog = null,
            Error = null,
            Notice = $"{dialog.Title}: action '{actionId}' executed."
        });
    }

    private void ApplyGlobalSettings(DesktopDialogState dialog)
    {
        int uiScalePercent = ParseDialogInt(dialog, "globalUiScale", State.Preferences.UiScalePercent);
        string theme = GetDialogFieldValue(dialog, "globalTheme") ?? State.Preferences.Theme;
        string language = GetDialogFieldValue(dialog, "globalLanguage") ?? State.Preferences.Language;
        bool compactMode = ParseDialogBool(dialog, "globalCompactMode", State.Preferences.CompactMode);

        Publish(State with
        {
            ActiveDialog = null,
            Error = null,
            Preferences = State.Preferences with
            {
                UiScalePercent = uiScalePercent,
                Theme = theme,
                Language = language,
                CompactMode = compactMode
            },
            Notice = "Global settings updated."
        });
    }

    private void ApplyCharacterSettings(DesktopDialogState dialog)
    {
        string priority = GetDialogFieldValue(dialog, "characterPriority") ?? State.Preferences.CharacterPriority;
        int karmaNuyenRatio = ParseDialogInt(dialog, "characterKarmaNuyen", State.Preferences.KarmaNuyenRatio);
        bool houseRules = ParseDialogBool(dialog, "characterHouseRulesEnabled", State.Preferences.HouseRulesEnabled);
        string notes = GetDialogFieldValue(dialog, "characterNotes") ?? State.Preferences.CharacterNotes;

        Publish(State with
        {
            ActiveDialog = null,
            Error = null,
            Build = State.Build is null ? null : State.Build with { BuildMethod = priority },
            Preferences = State.Preferences with
            {
                CharacterPriority = priority,
                KarmaNuyenRatio = karmaNuyenRatio,
                HouseRulesEnabled = houseRules,
                CharacterNotes = notes
            },
            Notice = "Character settings updated."
        });
    }

    private async Task ApplyMetadataDialogAsync(DesktopDialogState dialog, CancellationToken ct)
    {
        string? name = GetDialogFieldValue(dialog, "metadataName");
        string? alias = GetDialogFieldValue(dialog, "metadataAlias");
        string? notes = GetDialogFieldValue(dialog, "metadataNotes");
        string? normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes;

        await UpdateMetadataAsync(new UpdateWorkspaceMetadata(
            Name: string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            Alias: string.IsNullOrWhiteSpace(alias) ? null : alias.Trim(),
            Notes: normalizedNotes), ct);

        if (State.Error is null)
        {
            Publish(State with
            {
                ActiveDialog = null,
                Error = null,
                Notice = "Metadata updated."
            });
        }
    }

    private void RollDice(DesktopDialogState dialog)
    {
        string expression = GetDialogFieldValue(dialog, "diceExpression") ?? "1d6";
        if (!TryRollExpression(expression, out int total, out int hits, out string error))
        {
            Publish(State with { Error = error });
            return;
        }

        string summary = $"{expression} => total {total}, hits {hits}";
        List<DesktopDialogField> fields = dialog.Fields
            .Where(field => !string.Equals(field.Id, "diceResult", StringComparison.Ordinal))
            .ToList();
        fields.Add(new DesktopDialogField(
            Id: "diceResult",
            Label: "Last Result",
            Value: summary,
            Placeholder: summary,
            IsMultiline: false,
            IsReadOnly: true));

        Publish(State with
        {
            Error = null,
            Notice = summary,
            ActiveDialog = dialog with
            {
                Message = "Expression rolled using Shadowrun-style d6 hits.",
                Fields = fields
            }
        });
    }

    private static bool TryRollExpression(string expression, out int total, out int hits, out string error)
    {
        total = 0;
        hits = 0;
        error = string.Empty;

        Match match = DiceExpressionRegex.Match(expression);
        if (!match.Success)
        {
            error = "Dice expression must match NdM with optional +K modifier (example: 12d6+2).";
            return false;
        }

        int count = int.Parse(match.Groups[1].Value);
        int sides = int.Parse(match.Groups[2].Value);
        int modifier = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        if (count < 1 || count > 100 || sides < 2 || sides > 100)
        {
            error = "Dice expression is outside supported limits.";
            return false;
        }

        for (int index = 0; index < count; index++)
        {
            int value = Random.Shared.Next(1, sides + 1);
            total += value;
            if (sides == 6 && value >= 5)
            {
                hits++;
            }
        }

        total += modifier;
        if (sides != 6)
        {
            hits = 0;
        }

        return true;
    }

    private static string NormalizeDialogFieldValue(DesktopDialogField field, string? value)
    {
        if (string.Equals(field.InputType, "checkbox", StringComparison.Ordinal))
        {
            if (bool.TryParse(value, out bool booleanValue))
            {
                return booleanValue ? "true" : "false";
            }

            if (string.Equals(value, "1", StringComparison.Ordinal)
                || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return "true";
            }

            return "false";
        }

        return value ?? string.Empty;
    }

    private static string? GetDialogFieldValue(DesktopDialogState dialog, string fieldId)
    {
        DesktopDialogField? field = dialog.Fields.FirstOrDefault(item => string.Equals(item.Id, fieldId, StringComparison.Ordinal));
        return field?.Value;
    }

    private static int ParseDialogInt(DesktopDialogState dialog, string fieldId, int fallback)
    {
        string? raw = GetDialogFieldValue(dialog, fieldId);
        return int.TryParse(raw, out int value) ? value : fallback;
    }

    private static bool ParseDialogBool(DesktopDialogState dialog, string fieldId, bool fallback)
    {
        string? raw = GetDialogFieldValue(dialog, fieldId);
        if (raw is null)
            return fallback;

        if (bool.TryParse(raw, out bool value))
            return value;

        return string.Equals(raw, "1", StringComparison.Ordinal)
            || string.Equals(raw, "on", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<SectionRowState> BuildSectionRows(JsonNode? node)
    {
        if (node is null)
            return [];

        List<SectionRowState> rows = [];
        FlattenSectionNode(node, string.Empty, rows);
        if (rows.Count > 120)
        {
            return rows.Take(120).ToArray();
        }

        return rows;
    }

    private static void FlattenSectionNode(JsonNode node, string path, List<SectionRowState> rows)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach ((string key, JsonNode? child) in obj)
                {
                    if (child is null)
                        continue;

                    string nextPath = string.IsNullOrWhiteSpace(path) ? key : $"{path}.{key}";
                    FlattenSectionNode(child, nextPath, rows);
                }

                return;
            case JsonArray array:
                if (array.Count == 0)
                {
                    rows.Add(new SectionRowState(path, "[]"));
                    return;
                }

                bool simpleArray = array.All(item => item is null or JsonValue);
                if (simpleArray)
                {
                    string value = string.Join(", ", array.Select(item => item?.ToJsonString() ?? "null"));
                    rows.Add(new SectionRowState(path, value));
                    return;
                }

                for (int index = 0; index < array.Count; index++)
                {
                    JsonNode? child = array[index];
                    if (child is null)
                        continue;

                    string nextPath = $"{path}[{index}]";
                    FlattenSectionNode(child, nextPath, rows);
                }

                return;
            case JsonValue value:
                rows.Add(new SectionRowState(path, value.ToJsonString()));
                return;
        }
    }

    public Task CloseDialogAsync(CancellationToken ct)
    {
        Publish(State with
        {
            ActiveDialog = null,
            Error = null
        });
        return Task.CompletedTask;
    }

    public async Task SelectTabAsync(string tabId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tabId))
        {
            Publish(State with { Error = "Tab id is required." });
            return;
        }

        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        NavigationTabDefinition? tab = State.NavigationTabs.FirstOrDefault(item => string.Equals(item.Id, tabId, StringComparison.Ordinal));
        if (tab is null)
        {
            Publish(State with { Error = $"Unknown tab '{tabId}'." });
            return;
        }

        await LoadSectionAsync(tab.SectionId, tab.Id, $"{tab.Id}:{tab.SectionId}", ct);
    }

    public async Task UpdateMetadataAsync(UpdateWorkspaceMetadata command, CancellationToken ct)
    {
        string? normalizedNotes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes;

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
            CommandResult<CharacterProfileSection> result = await _client.UpdateMetadataAsync(_currentWorkspace.Value, command, ct);
            if (!result.Success || result.Value is null)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error ?? "Metadata update failed."
                });
                return;
            }

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                WorkspaceId = _currentWorkspace,
                Profile = result.Value,
                Preferences = normalizedNotes is null
                    ? State.Preferences
                    : State.Preferences with { CharacterNotes = normalizedNotes }
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
            CommandResult<WorkspaceSaveReceipt> result = await _client.SaveAsync(_currentWorkspace.Value, ct);
            if (!result.Success || result.Value is null)
            {
                Publish(State with
                {
                    IsBusy = false,
                    Error = result.Error ?? "Save failed."
                });
                return;
            }

            Publish(State with
            {
                IsBusy = false,
                Error = null,
                WorkspaceId = _currentWorkspace,
                HasSavedWorkspace = true,
                Notice = "Workspace saved."
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

    private async Task LoadSectionAsync(string sectionId, string? tabId, string? actionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
        {
            Publish(State with { Error = "Section id is required." });
            return;
        }

        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            var section = await _client.GetSectionAsync(_currentWorkspace.Value, sectionId, ct);
            string sectionJson = section.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = tabId ?? State.ActiveTabId,
                ActiveActionId = actionId ?? State.ActiveActionId,
                ActiveSectionId = sectionId,
                ActiveSectionJson = sectionJson,
                ActiveSectionRows = BuildSectionRows(section)
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

    private async Task RenderSummaryAction(WorkspaceSurfaceActionDefinition action, CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            CharacterFileSummary summary = await _client.GetSummaryAsync(_currentWorkspace.Value, ct);
            JsonNode? summaryNode = JsonSerializer.SerializeToNode(summary);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = action.TabId,
                ActiveActionId = action.Id,
                ActiveSectionId = "summary",
                ActiveSectionJson = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }),
                ActiveSectionRows = BuildSectionRows(summaryNode)
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

    private async Task RenderValidateAction(WorkspaceSurfaceActionDefinition action, CancellationToken ct)
    {
        if (_currentWorkspace is null)
        {
            Publish(State with { Error = "No workspace loaded." });
            return;
        }

        Publish(State with
        {
            IsBusy = true,
            Error = null
        });

        try
        {
            CharacterValidationResult validation = await _client.ValidateAsync(_currentWorkspace.Value, ct);
            JsonNode? validationNode = JsonSerializer.SerializeToNode(validation);
            Publish(State with
            {
                IsBusy = false,
                Error = null,
                ActiveTabId = action.TabId,
                ActiveActionId = action.Id,
                ActiveSectionId = "validate",
                ActiveSectionJson = JsonSerializer.Serialize(validation, new JsonSerializerOptions { WriteIndented = true }),
                ActiveSectionRows = BuildSectionRows(validationNode)
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

    private DesktopDialogState CreateMetadataDialog()
    {
        return new DesktopDialogState(
            Id: "dialog.workspace.metadata",
            Title: "Edit Metadata",
            Message: "Apply character metadata changes to the active workspace.",
            Fields:
            [
                new DesktopDialogField("metadataName", "Name", State.Profile?.Name ?? string.Empty, "Character Name"),
                new DesktopDialogField("metadataAlias", "Alias", State.Profile?.Alias ?? string.Empty, "Street Name"),
                new DesktopDialogField("metadataNotes", "Notes", State.Preferences.CharacterNotes, "Notes", true)
            ],
            Actions:
            [
                new DesktopDialogAction("apply_metadata", "Apply", true),
                new DesktopDialogAction("cancel", "Cancel")
            ]);
    }

    private DesktopDialogState CreateCommandDialog(string commandId)
    {
        string name = State.Profile?.Name ?? "(none)";
        string alias = State.Profile?.Alias ?? string.Empty;
        string workspace = _currentWorkspace?.Value ?? "(none)";

        return commandId switch
        {
            "print_setup" => new DesktopDialogState(
                "dialog.print_setup",
                "Print Setup",
                "Printer setup is delegated to host/browser print capabilities.",
                [
                    new DesktopDialogField("printLandscape", "Landscape", "false", "false", InputType: "checkbox"),
                    new DesktopDialogField("printBackground", "Print background graphics", "true", "true", InputType: "checkbox")
                ],
                [
                    new DesktopDialogAction("ok", "OK", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "dice_roller" => new DesktopDialogState(
                "dialog.dice_roller",
                "Dice Roller",
                "Enter an expression and execute roll from this dialog.",
                [new DesktopDialogField("diceExpression", "Expression", "12d6", "12d6")],
                [
                    new DesktopDialogAction("roll", "Roll", true),
                    new DesktopDialogAction("close", "Close")
                ]),
            "global_settings" => new DesktopDialogState(
                "dialog.global_settings",
                "Global Settings",
                null,
                [
                    new DesktopDialogField("globalUiScale", "UI Scale (%)", State.Preferences.UiScalePercent.ToString(), "100", InputType: "number"),
                    new DesktopDialogField("globalTheme", "Theme", State.Preferences.Theme, "classic"),
                    new DesktopDialogField("globalLanguage", "Language", State.Preferences.Language, "en-us"),
                    new DesktopDialogField("globalCompactMode", "Compact Mode", State.Preferences.CompactMode ? "true" : "false", "false", InputType: "checkbox")
                ],
                [
                    new DesktopDialogAction("save", "Save", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "character_settings" => new DesktopDialogState(
                "dialog.character_settings",
                "Character Settings",
                null,
                [
                    new DesktopDialogField("characterPriority", "Priority System", State.Preferences.CharacterPriority, "SumToTen"),
                    new DesktopDialogField("characterKarmaNuyen", "Karma/Nuyen Ratio", State.Preferences.KarmaNuyenRatio.ToString(), "2", InputType: "number"),
                    new DesktopDialogField("characterHouseRulesEnabled", "Enable House Rules", State.Preferences.HouseRulesEnabled ? "true" : "false", "false", InputType: "checkbox"),
                    new DesktopDialogField("characterNotes", "Character Notes", State.Preferences.CharacterNotes, "notes", true)
                ],
                [
                    new DesktopDialogAction("save", "Save", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "translator" => new DesktopDialogState(
                "dialog.translator",
                "Translator",
                "Language catalog preview.",
                [
                    new DesktopDialogField("translatorSearch", "Language Search", string.Empty, "filter languages"),
                    new DesktopDialogField("lang1", "English", "en-us", "en-us", IsReadOnly: true),
                    new DesktopDialogField("lang2", "Deutsch", "de-de", "de-de", IsReadOnly: true),
                    new DesktopDialogField("lang3", "Francais", "fr-fr", "fr-fr", IsReadOnly: true)
                ],
                [new DesktopDialogAction("close", "Close", true)]),
            "xml_editor" => new DesktopDialogState(
                "dialog.xml_editor",
                "XML Editor",
                "Edit/import flow in this head is file-first; this is a debug preview.",
                [new DesktopDialogField("xmlEditorDialog", "XML", State.ActiveSectionJson ?? "<character />", "<character />", true)],
                [
                    new DesktopDialogAction("apply", "Apply", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "master_index" => new DesktopDialogState(
                "dialog.master_index",
                "Master Index",
                "Catalog data is served by the API and surfaced here in desktop parity mode.",
                [new DesktopDialogField("root", "Data Root", "/app/data", "/app/data", IsReadOnly: true)],
                [new DesktopDialogAction("close", "Close", true)]),
            "character_roster" => new DesktopDialogState(
                "dialog.character_roster",
                "Character Roster",
                "Roster persistence is managed by the shared API store.",
                [
                    new DesktopDialogField("name", "Name", name, name),
                    new DesktopDialogField("alias", "Alias", alias, alias),
                    new DesktopDialogField("workspace", "Workspace", workspace, workspace, IsReadOnly: true)
                ],
                [new DesktopDialogAction("close", "Close", true)]),
            "data_exporter" => new DesktopDialogState(
                "dialog.data_exporter",
                "Data Exporter",
                "Export pipeline is routed through API tool endpoints.",
                [new DesktopDialogField("dataExportPreview", "Export Preview", $"Workspace: {workspace}", "{}", true, true)],
                [
                    new DesktopDialogAction("download", "Download", true),
                    new DesktopDialogAction("close", "Close")
                ]),
            "export_character" => new DesktopDialogState(
                "dialog.export_character",
                "Export Character",
                "Export selected character bundle.",
                [new DesktopDialogField("dataExportPreview", "Export Preview", $"Workspace: {workspace}", "{}", true, true)],
                [
                    new DesktopDialogAction("download", "Download", true),
                    new DesktopDialogAction("close", "Close")
                ]),
            "report_bug" => new DesktopDialogState(
                "dialog.report_bug",
                "Report Bug",
                "Open the issue form in your browser: https://github.com/chummer5a/chummer5a/issues/new/choose",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "about" => new DesktopDialogState(
                "dialog.about",
                "About Chummer",
                "Dual-head preview over shared presenter/API behavior path.",
                [
                    new DesktopDialogField("runtime", "Runtime", "net10.0", "net10.0", IsReadOnly: true),
                    new DesktopDialogField("workspace", "Workspace", workspace, workspace, IsReadOnly: true)
                ],
                [new DesktopDialogAction("close", "Close", true)]),
            "hero_lab_importer" => new DesktopDialogState(
                "dialog.hero_lab_importer",
                "Hero Lab Importer",
                "Import flow placeholder for Hero Lab payload conversion.",
                [new DesktopDialogField("file", "Input File", ".por/.xml", ".por/.xml")],
                [
                    new DesktopDialogAction("import", "Import", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "new_window" => new DesktopDialogState(
                "dialog.new_window",
                "New Window",
                "Open a second shell instance from your platform runtime.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "close_window" => new DesktopDialogState(
                "dialog.close_window",
                "Close Window",
                "Close-window action is host/platform specific.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "wiki" => new DesktopDialogState(
                "dialog.wiki",
                "Wiki",
                "https://github.com/chummer5a/chummer5a/wiki",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "discord" => new DesktopDialogState(
                "dialog.discord",
                "Discord",
                "https://discord.gg/EV44Mya",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "revision_history" => new DesktopDialogState(
                "dialog.revision_history",
                "Revision History",
                "https://github.com/chummer5a/chummer5a/releases",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "dumpshock" => new DesktopDialogState(
                "dialog.dumpshock",
                "Dumpshock Thread",
                "https://forums.dumpshock.com/index.php?showtopic=37464",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "print_character" => new DesktopDialogState(
                "dialog.print_character",
                "Print Character",
                "Print preview is rendered by host/browser print facilities.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "print_multiple" => new DesktopDialogState(
                "dialog.print_multiple",
                "Print Multiple",
                "Batch print is available through roster and print endpoints.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "update" => new DesktopDialogState(
                "dialog.update",
                "Check for Updates",
                "Update channel status can be checked from the service layer.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            _ => new DesktopDialogState(
                "dialog.generic",
                commandId,
                $"Command '{commandId}' is recognized but has no dedicated dialog template yet.",
                [],
                [new DesktopDialogAction("close", "Close", true)])
        };
    }

    private DesktopDialogState CreateUiControlDialog(string controlId)
    {
        return controlId switch
        {
            "create_entry" => new DesktopDialogState(
                "dialog.ui.create_entry",
                "Add Entry",
                null,
                [new DesktopDialogField("uiCreateEntryName", "Entry Name", string.Empty, "New entry")],
                [
                    new DesktopDialogAction("add", "Add", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "edit_entry" => new DesktopDialogState(
                "dialog.ui.edit_entry",
                "Edit Entry",
                null,
                [new DesktopDialogField("uiEditEntryName", "Entry Name", "Current Entry", "Current Entry")],
                [new DesktopDialogAction("close", "Close", true)]),
            "delete_entry" => new DesktopDialogState(
                "dialog.ui.delete_entry",
                "Delete Entry",
                "Delete selected entry?",
                [],
                [
                    new DesktopDialogAction("delete", "Delete", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "open_notes" => new DesktopDialogState(
                "dialog.ui.open_notes",
                "Notes",
                null,
                [new DesktopDialogField("uiNotesEditor", "Notes", State.Preferences.CharacterNotes, "notes", true)],
                [
                    new DesktopDialogAction("save", "Save", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            "move_up" => new DesktopDialogState(
                "dialog.ui.move_up",
                "Move Up",
                "Moved selection up.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "move_down" => new DesktopDialogState(
                "dialog.ui.move_down",
                "Move Down",
                "Moved selection down.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "toggle_free_paid" => new DesktopDialogState(
                "dialog.ui.toggle_free_paid",
                "Free/Paid",
                "Toggled free/paid state for selected item.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "show_source" => new DesktopDialogState(
                "dialog.ui.show_source",
                "Source",
                "Source book and page metadata is shown here.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "gear_add" => new DesktopDialogState(
                "dialog.ui.gear_add",
                "Add Gear",
                null,
                [new DesktopDialogField("uiGearName", "Gear Name", string.Empty, "Ares Predator")],
                [new DesktopDialogAction("close", "Close", true)]),
            "gear_edit" => new DesktopDialogState(
                "dialog.ui.gear_edit",
                "Edit Gear",
                null,
                [new DesktopDialogField("uiGearEditName", "Gear Name", "Selected Gear", "Selected Gear")],
                [new DesktopDialogAction("close", "Close", true)]),
            "gear_delete" => new DesktopDialogState(
                "dialog.ui.gear_delete",
                "Delete Gear",
                "Deleted selected gear item.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "gear_mount" => new DesktopDialogState(
                "dialog.ui.gear_mount",
                "Mount Gear",
                "Mounted selected gear on compatible host.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "gear_source" => new DesktopDialogState(
                "dialog.ui.gear_source",
                "Gear Source",
                "Gear source references are displayed here.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "magic_add" => new DesktopDialogState(
                "dialog.ui.magic_add",
                "Add Spell/Power",
                null,
                [new DesktopDialogField("uiMagicName", "Name", string.Empty, "Spell or Power")],
                [new DesktopDialogAction("close", "Close", true)]),
            "magic_delete" => new DesktopDialogState(
                "dialog.ui.magic_delete",
                "Delete Spell/Power",
                "Removed selected spell/power.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "magic_bind" => new DesktopDialogState(
                "dialog.ui.magic_bind",
                "Bind/Link",
                "Bind/link workflow started for selected magical item.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "magic_source" => new DesktopDialogState(
                "dialog.ui.magic_source",
                "Magic Source",
                "Magical source references are displayed here.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "skill_add" => new DesktopDialogState(
                "dialog.ui.skill_add",
                "Add Skill",
                null,
                [new DesktopDialogField("uiSkillName", "Skill", string.Empty, "Perception")],
                [new DesktopDialogAction("close", "Close", true)]),
            "skill_specialize" => new DesktopDialogState(
                "dialog.ui.skill_specialize",
                "Specialize Skill",
                null,
                [new DesktopDialogField("uiSkillSpec", "Specialization", string.Empty, "Visual")],
                [new DesktopDialogAction("close", "Close", true)]),
            "skill_remove" => new DesktopDialogState(
                "dialog.ui.skill_remove",
                "Remove Skill",
                "Removed selected skill.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "skill_group" => new DesktopDialogState(
                "dialog.ui.skill_group",
                "Skill Group",
                "Opened skill group assignment.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "combat_add_weapon" => new DesktopDialogState(
                "dialog.ui.combat_add_weapon",
                "Add Weapon",
                null,
                [new DesktopDialogField("uiWeaponName", "Weapon", string.Empty, "Colt M23")],
                [new DesktopDialogAction("close", "Close", true)]),
            "combat_add_armor" => new DesktopDialogState(
                "dialog.ui.combat_add_armor",
                "Add Armor",
                null,
                [new DesktopDialogField("uiArmorName", "Armor", string.Empty, "Armor Jacket")],
                [new DesktopDialogAction("close", "Close", true)]),
            "combat_reload" => new DesktopDialogState(
                "dialog.ui.combat_reload",
                "Reload Weapon",
                "Reloaded selected weapon.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "combat_damage_track" => new DesktopDialogState(
                "dialog.ui.combat_damage_track",
                "Damage Track",
                "Applied one damage track step.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "contact_add" => new DesktopDialogState(
                "dialog.ui.contact_add",
                "Add Contact",
                null,
                [new DesktopDialogField("uiContactName", "Name", string.Empty, "Contact Name")],
                [new DesktopDialogAction("close", "Close", true)]),
            "contact_edit" => new DesktopDialogState(
                "dialog.ui.contact_edit",
                "Edit Contact",
                null,
                [new DesktopDialogField("uiContactEditName", "Name", "Selected Contact", "Selected Contact")],
                [new DesktopDialogAction("close", "Close", true)]),
            "contact_remove" => new DesktopDialogState(
                "dialog.ui.contact_remove",
                "Remove Contact",
                "Removed selected contact.",
                [],
                [new DesktopDialogAction("close", "Close", true)]),
            "contact_connection" => new DesktopDialogState(
                "dialog.ui.contact_connection",
                "Connection / Loyalty",
                null,
                [
                    new DesktopDialogField("uiContactConnection", "Connection", "3", "3", InputType: "number"),
                    new DesktopDialogField("uiContactLoyalty", "Loyalty", "3", "3", InputType: "number")
                ],
                [
                    new DesktopDialogAction("apply", "Apply", true),
                    new DesktopDialogAction("cancel", "Cancel")
                ]),
            _ => new DesktopDialogState(
                "dialog.ui.generic",
                "Desktop Control",
                $"Desktop control '{controlId}' triggered.",
                [],
                [new DesktopDialogAction("close", "Close", true)])
        };
    }

    private async Task LoadWorkspaceAsync(
        CharacterWorkspaceId id,
        CancellationToken ct,
        IReadOnlyList<OpenWorkspaceState>? openWorkspaceSeed = null)
    {
        Task<CharacterProfileSection> profileTask = _client.GetProfileAsync(id, ct);
        Task<CharacterProgressSection> progressTask = _client.GetProgressAsync(id, ct);
        Task<CharacterSkillsSection> skillsTask = _client.GetSkillsAsync(id, ct);
        Task<CharacterRulesSection> rulesTask = _client.GetRulesAsync(id, ct);
        Task<CharacterBuildSection> buildTask = _client.GetBuildAsync(id, ct);
        Task<CharacterMovementSection> movementTask = _client.GetMovementAsync(id, ct);
        Task<CharacterAwakeningSection> awakeningTask = _client.GetAwakeningAsync(id, ct);

        await Task.WhenAll(profileTask, progressTask, skillsTask, rulesTask, buildTask, movementTask, awakeningTask);

        _currentWorkspace = id;
        IReadOnlyList<OpenWorkspaceState> openWorkspaces = _workspaceSessionManager.Activate(
            openWorkspaceSeed ?? State.OpenWorkspaces,
            id,
            profileTask.Result);
        Publish(new CharacterOverviewState(
            IsBusy: false,
            Error: null,
            WorkspaceId: id,
            OpenWorkspaces: openWorkspaces,
            Profile: profileTask.Result,
            Progress: progressTask.Result,
            Skills: skillsTask.Result,
            Rules: rulesTask.Result,
            Build: buildTask.Result,
            Movement: movementTask.Result,
            Awakening: awakeningTask.Result,
            ActiveTabId: null,
            ActiveActionId: null,
            ActiveSectionId: null,
            ActiveSectionJson: null,
            ActiveSectionRows: [],
            LastCommandId: State.LastCommandId,
            Notice: State.Notice,
            ActiveDialog: null,
            Preferences: State.Preferences,
            Commands: State.Commands,
            NavigationTabs: State.NavigationTabs,
            HasSavedWorkspace: false));
    }

    private void Publish(CharacterOverviewState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
