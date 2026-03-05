using Avalonia.Controls;

namespace Chummer.Avalonia.Controls;

public partial class SectionHostControl : UserControl
{
    public SectionHostControl()
    {
        InitializeComponent();
    }

    public string XmlInputText => XmlInputBox.Text ?? string.Empty;

    public void SetState(SectionHostState state)
    {
        SetNotice(state.Notice);
        SetSectionPreview(state.PreviewJson, state.Rows);
    }

    public void SetNotice(string notice)
    {
        NoticeText.Text = notice;
    }

    public void SetSectionPreview(string previewJson, IEnumerable<SectionRowDisplayItem> rows)
    {
        SectionPreviewBox.Text = previewJson;
        SectionRowsList.ItemsSource = rows.ToArray();
    }
}

public sealed record SectionHostState(
    string Notice,
    string PreviewJson,
    SectionRowDisplayItem[] Rows);

public sealed record SectionRowDisplayItem(string Path, string Value)
{
    public override string ToString()
    {
        return $"{Path} = {Value}";
    }
}
