using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Models;
using System.Collections.ObjectModel;

namespace OpenglAvaloniaTest.ViewModels;

public partial class HierarchyView : UserControl
{
    public HierarchyView()
    {
        InitializeComponent();
        HierarchyStack.Children.Add(new Button{Content = "Code given"});
    }
}