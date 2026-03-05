using Avalonia.Controls;

namespace Chummer.Avalonia.Controls;

public partial class WorkspaceStripControl : UserControl
{
    public WorkspaceStripControl()
    {
        InitializeComponent();
    }

    public void SetWorkspaceText(string text)
    {
        WorkspaceText.Text = text;
    }
}
