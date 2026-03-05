using Avalonia.Controls;

namespace Chummer.Avalonia.Controls;

public partial class StatusStripControl : UserControl
{
    public StatusStripControl()
    {
        InitializeComponent();
    }

    public void SetState(StatusStripState state)
    {
        SetValues(
            characterState: state.CharacterState,
            serviceState: state.ServiceState,
            timeState: state.TimeState,
            complianceState: state.ComplianceState);
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

public sealed record StatusStripState(
    string CharacterState,
    string ServiceState,
    string TimeState,
    string ComplianceState);
