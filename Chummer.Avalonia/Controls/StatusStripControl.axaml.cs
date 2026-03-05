using Avalonia.Controls;

namespace Chummer.Avalonia.Controls;

public partial class StatusStripControl : UserControl
{
    public StatusStripControl()
    {
        InitializeComponent();
    }

    public void SetValues(
        string characterState,
        string serviceState,
        string timeState,
        string complianceState)
    {
        CharacterStateText.Text = characterState;
        ServiceStateText.Text = serviceState;
        TimeStateText.Text = timeState;
        ComplianceStateText.Text = complianceState;
    }

    public void SetServiceAndTime(string serviceState, string timeState)
    {
        ServiceStateText.Text = serviceState;
        TimeStateText.Text = timeState;
    }
}
