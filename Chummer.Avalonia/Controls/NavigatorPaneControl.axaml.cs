using Avalonia.Controls;
using Chummer.Contracts.Presentation;

namespace Chummer.Avalonia.Controls;

public partial class NavigatorPaneControl : UserControl
{
    private bool _suppressWorkspaceSelectionEvent;
    private bool _suppressTabSelectionEvent;
    private bool _suppressSectionActionSelectionEvent;
    private bool _suppressUiControlSelectionEvent;

    public NavigatorPaneControl()
    {
        InitializeComponent();
        OpenWorkspacesList.SelectionChanged += OpenWorkspacesList_OnSelectionChanged;
        NavigationTabsList.SelectionChanged += NavigationTabsList_OnSelectionChanged;
        SectionActionsList.SelectionChanged += SectionActionsList_OnSelectionChanged;
        UiControlsList.SelectionChanged += UiControlsList_OnSelectionChanged;
    }

    public event EventHandler<string>? WorkspaceSelected;
    public event EventHandler<string>? NavigationTabSelected;
    public event EventHandler<string>? SectionActionSelected;
    public event EventHandler<string>? UiControlSelected;

    public void SetOpenWorkspaces(IEnumerable<NavigatorWorkspaceItem> workspaces, string? selectedWorkspaceId)
    {
        NavigatorWorkspaceItem[] workspaceItems = workspaces.ToArray();
        _suppressWorkspaceSelectionEvent = true;
        OpenWorkspacesList.ItemsSource = workspaceItems;
        OpenWorkspacesList.SelectedItem = workspaceItems
            .FirstOrDefault(item => string.Equals(item.Id, selectedWorkspaceId, StringComparison.Ordinal));
        _suppressWorkspaceSelectionEvent = false;
    }

    public void SetNavigationTabs(IEnumerable<NavigatorTabItem> tabs, string? activeTabId)
    {
        NavigatorTabItem[] tabItems = tabs.ToArray();
        _suppressTabSelectionEvent = true;
        NavigationTabsList.ItemsSource = tabItems;
        NavigationTabsList.SelectedItem = tabItems
            .FirstOrDefault(item => string.Equals(item.Id, activeTabId, StringComparison.Ordinal));
        _suppressTabSelectionEvent = false;
    }

    public void SetSectionActions(IEnumerable<NavigatorSectionActionItem> actions, string? activeActionId)
    {
        NavigatorSectionActionItem[] actionItems = actions.ToArray();
        _suppressSectionActionSelectionEvent = true;
        SectionActionsList.ItemsSource = actionItems;
        SectionActionsList.SelectedItem = actionItems
            .FirstOrDefault(item => string.Equals(item.Id, activeActionId, StringComparison.Ordinal));
        _suppressSectionActionSelectionEvent = false;
    }

    public void SetUiControls(IEnumerable<NavigatorUiControlItem> controls)
    {
        NavigatorUiControlItem[] controlItems = controls.ToArray();
        _suppressUiControlSelectionEvent = true;
        UiControlsList.ItemsSource = controlItems;
        UiControlsList.SelectedItem = null;
        _suppressUiControlSelectionEvent = false;
    }

    private void OpenWorkspacesList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressWorkspaceSelectionEvent)
            return;

        if (OpenWorkspacesList.SelectedItem is not NavigatorWorkspaceItem workspace || !workspace.Enabled)
            return;

        WorkspaceSelected?.Invoke(this, workspace.Id);
    }

    private void NavigationTabsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressTabSelectionEvent)
            return;

        if (NavigationTabsList.SelectedItem is not NavigatorTabItem tab || !tab.Enabled)
            return;

        NavigationTabSelected?.Invoke(this, tab.Id);
    }

    private void SectionActionsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSectionActionSelectionEvent)
            return;

        if (SectionActionsList.SelectedItem is not NavigatorSectionActionItem action)
            return;

        SectionActionSelected?.Invoke(this, action.Id);
        ClearSelection(SectionActionsList, ref _suppressSectionActionSelectionEvent);
    }

    private void UiControlsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressUiControlSelectionEvent)
            return;

        if (UiControlsList.SelectedItem is not NavigatorUiControlItem control)
            return;

        UiControlSelected?.Invoke(this, control.Id);
        ClearSelection(UiControlsList, ref _suppressUiControlSelectionEvent);
    }

    private static void ClearSelection(ListBox listBox, ref bool suppressSelectionEvent)
    {
        suppressSelectionEvent = true;
        listBox.SelectedItem = null;
        suppressSelectionEvent = false;
    }
}

public sealed record NavigatorWorkspaceItem(
    string Id,
    string Name,
    string Alias,
    bool HasSavedWorkspace,
    bool Enabled)
{
    public override string ToString()
    {
        string label = string.IsNullOrWhiteSpace(Alias) ? Name : $"{Name} ({Alias})";
        string saveTag = HasSavedWorkspace ? "saved" : "unsaved";
        return $"{label} [{Id}] [{saveTag}] {(Enabled ? "enabled" : "disabled")}";
    }
}

public sealed record NavigatorTabItem(
    string Id,
    string Label,
    string SectionId,
    string Group,
    bool Enabled)
{
    public override string ToString()
    {
        return $"{Label} ({Id}) -> {SectionId}";
    }
}

public sealed record NavigatorSectionActionItem(string Id, string Label, WorkspaceSurfaceActionKind Kind)
{
    public override string ToString()
    {
        return $"{Label} [{Kind}]";
    }
}

public sealed record NavigatorUiControlItem(string Id, string Label)
{
    public override string ToString()
    {
        return $"{Label} ({Id})";
    }
}
