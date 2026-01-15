using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace OpenglAvaloniaTest;

public partial class HierarchyModel : UserControl
{
    public HierarchyModel()
    {
        InitializeComponent();
    }

    private void OnNameKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            TopLevel.GetTopLevel((Control)sender!)?.Focus();
            e.Handled = true;
        }
    }
}