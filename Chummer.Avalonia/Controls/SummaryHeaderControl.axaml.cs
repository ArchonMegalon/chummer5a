using Avalonia.Controls;

namespace Chummer.Avalonia.Controls;

public partial class SummaryHeaderControl : UserControl
{
    public SummaryHeaderControl()
    {
        InitializeComponent();
    }

    public void SetState(SummaryHeaderState state)
    {
        SetValues(state.Name, state.Alias, state.Karma, state.Skills, state.RuntimeSummary);
    }

    public void SetValues(string? name, string? alias, string? karma, string? skills, string? runtimeSummary)
    {
        NameValueText.Text = string.IsNullOrWhiteSpace(name) ? "-" : name;
        AliasValueText.Text = string.IsNullOrWhiteSpace(alias) ? "-" : alias;
        KarmaValueText.Text = string.IsNullOrWhiteSpace(karma) ? "-" : karma;
        SkillsValueText.Text = string.IsNullOrWhiteSpace(skills) ? "-" : skills;
        RuntimeValueText.Text = string.IsNullOrWhiteSpace(runtimeSummary) ? "-" : runtimeSummary;
    }
}

public sealed record SummaryHeaderState(
    string? Name,
    string? Alias,
    string? Karma,
    string? Skills,
    string? RuntimeSummary);
