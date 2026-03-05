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
        SetValues(state.Name, state.Alias, state.Karma, state.Skills);
    }

    public void SetValues(string? name, string? alias, string? karma, string? skills)
    {
        NameValueText.Text = string.IsNullOrWhiteSpace(name) ? "-" : name;
        AliasValueText.Text = string.IsNullOrWhiteSpace(alias) ? "-" : alias;
        KarmaValueText.Text = string.IsNullOrWhiteSpace(karma) ? "-" : karma;
        SkillsValueText.Text = string.IsNullOrWhiteSpace(skills) ? "-" : skills;
    }
}

public sealed record SummaryHeaderState(
    string? Name,
    string? Alias,
    string? Karma,
    string? Skills);
