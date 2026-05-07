using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using UI.ViewModels;

namespace UI;

public partial class SunEditView : UserControl
{
    public SunEditView()
    {
        InitializeComponent();
        UpdateIcon();
    }

    private void ToggleSun(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SunControls.IsEnabled = !SunControls.IsEnabled;
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        sunActiveIcon.IsVisible = SunControls.IsEnabled;
    }
}