using Avalonia.Controls;

namespace Chummer.Avalonia.Controls;

public partial class SectionHostControl : UserControl
{
    public SectionHostControl()
    {
        InitializeComponent();
    }

    public string XmlInputText => XmlInputBox.Text ?? string.Empty;

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

public sealed record SectionRowDisplayItem(string Path, string Value)
{
    public override string ToString()
    {
        return $"{Path} = {Value}";
    }
}
