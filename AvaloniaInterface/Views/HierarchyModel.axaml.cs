using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Models;
using System;

namespace OpenglAvaloniaTest;

public partial class HierarchyModel : UserControl
{

    private static readonly string HiddenImageDirectory = "avares://AvaloniaInterface/Assets/hiddenIcon.png";
    private static readonly string VisibleImageDirectory = "avares://AvaloniaInterface/Assets/visibleIcon.png";

    public Model? Model;

    public HierarchyModel()
    {
        InitializeComponent();
        ReadModelData();
    }

    public void ReadModelData()
    {
        if (Model == null) return;
        NameTextbox.Text = Model.ObjectName;
    }

    private void OnNameKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            TopLevel.GetTopLevel((Control)sender!)?.Focus();
            e.Handled = true;
        }
    }

    private void OnHideButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if(Model == null) return;

        Model.Hidden = !Model.Hidden;

        //if (Model.Hidden)
        //{
        //    HiddenImage.Source = new Bitmap(HiddenImageDirectory);
        //}
        //else
        //{
        //    HiddenImage.Source = new Bitmap(VisibleImageDirectory);
        //}
    }
}