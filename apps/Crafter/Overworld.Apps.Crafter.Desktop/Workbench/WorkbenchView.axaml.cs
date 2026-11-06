using Avalonia.Controls;

namespace Overworld.Apps.Crafter.Desktop.Workbench;

public sealed partial class WorkbenchView : UserControl
{
    public WorkbenchView()
    {
        InitializeComponent();
    }

    public void FocusSearch()
    {
        Search.Focus();
        Search.SelectAll();
    }
}
