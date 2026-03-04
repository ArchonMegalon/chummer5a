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
    private readonly IDesktopDialogFactory _dialogFactory;
    private CharacterWorkspaceId? _currentWorkspace;

    public CharacterOverviewPresenter(
        IChummerClient client,
        IWorkspaceSessionManager? workspaceSessionManager = null,
        IDesktopDialogFactory? dialogFactory = null)
    {
        _client = client;
        _workspaceSessionManager = workspaceSessionManager ?? new WorkspaceSessionManager();
        _dialogFactory = dialogFactory ?? new DesktopDialogFactory();
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

        if (OverviewCommandPolicy.IsMenuCommand(commandId))
        {
            Publish(State with
            {
                Error = null,
                Notice = $"Menu '{commandId}' is handled by the active UI shell."
            });
            return;
        }

        if (OverviewCommandPolicy.IsImportHintCommand(commandId))
        {
            Publish(State with
            {
                Error = null,
                Notice = "Use the file import action in this head to open a character document."
            });
            return;
        }

        if (OverviewCommandPolicy.IsDialogCommand(commandId))
        {
            Publish(State with
            {
                Error = null,
                ActiveDialog = _dialogFactory.CreateCommandDialog(
                    commandId,
                    State.Profile,
                    State.Preferences,
                    State.ActiveSectionJson,
                    _currentWorkspace)
            });
            return;
        }

        if (OverviewCommandPolicy.IsEditorRelayCommand(commandId))
        {
            Publish(State with
            {
                Error = null,
                Notice = $"Command '{commandId}' dispatched to the active section editor."
            });
            return;
        }

        switch (commandId)
        {
            case "save_character":
            case "save_character_as":
                await SaveAsync(ct);
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
            ActiveDialog = _dialogFactory.CreateUiControlDialog(controlId, State.Preferences)
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
                    ActiveDialog = _dialogFactory.CreateMetadataDialog(State.Profile, State.Preferences)
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
                ? field with { Value = DesktopDialogFieldValueParser.Normalize(field, value) }
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
            string notes = DesktopDialogFieldValueParser.GetValue(dialog, "uiNotesEditor") ?? string.Empty;
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
            string connection = DesktopDialogFieldValueParser.GetValue(dialog, "uiContactConnection") ?? "0";
            string loyalty = DesktopDialogFieldValueParser.GetValue(dialog, "uiContactLoyalty") ?? "0";
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
        int uiScalePercent = DesktopDialogFieldValueParser.ParseInt(dialog, "globalUiScale", State.Preferences.UiScalePercent);
        string theme = DesktopDialogFieldValueParser.GetValue(dialog, "globalTheme") ?? State.Preferences.Theme;
        string language = DesktopDialogFieldValueParser.GetValue(dialog, "globalLanguage") ?? State.Preferences.Language;
        bool compactMode = DesktopDialogFieldValueParser.ParseBool(dialog, "globalCompactMode", State.Preferences.CompactMode);

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
        string priority = DesktopDialogFieldValueParser.GetValue(dialog, "characterPriority") ?? State.Preferences.CharacterPriority;
        int karmaNuyenRatio = DesktopDialogFieldValueParser.ParseInt(dialog, "characterKarmaNuyen", State.Preferences.KarmaNuyenRatio);
        bool houseRules = DesktopDialogFieldValueParser.ParseBool(dialog, "characterHouseRulesEnabled", State.Preferences.HouseRulesEnabled);
        string notes = DesktopDialogFieldValueParser.GetValue(dialog, "characterNotes") ?? State.Preferences.CharacterNotes;

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
        string? name = DesktopDialogFieldValueParser.GetValue(dialog, "metadataName");
        string? alias = DesktopDialogFieldValueParser.GetValue(dialog, "metadataAlias");
        string? notes = DesktopDialogFieldValueParser.GetValue(dialog, "metadataNotes");
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
        string expression = DesktopDialogFieldValueParser.GetValue(dialog, "diceExpression") ?? "1d6";
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
                ActiveSectionRows = SectionRowProjector.BuildRows(section)
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
                ActiveSectionRows = SectionRowProjector.BuildRows(summaryNode)
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
                ActiveSectionRows = SectionRowProjector.BuildRows(validationNode)
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
