using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Models;
using System;

namespace OpenglAvaloniaTest;

public partial class HierarchyModel : UserControl
{

    public Model? Model;

    public HierarchyModel()
    {
        InitializeComponent();
        ReadModelData();
    }
    public void ReadModelData()
    {
        
        if (Model == null) return;
        Console.WriteLine("Fuck");
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
}