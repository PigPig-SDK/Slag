using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

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

        UpdatePositionText();
    }
    void UpdatePositionText()
    {
        XBox.Text = Model!.Position.X.ToString();
        YBox.Text = Model!.Position.Y.ToString();
        ZBox.Text = Model!.Position.Z.ToString();

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
        if (Model == null) return;

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

    private void DeleteModelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Model == null) return;

        SceneHierarchy.Instance.RemoveModel(Model!);
    }

    private void OnPositionTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Model == null) return;

        if (float.TryParse(XBox.Text, out float x)) Model!.Position.X = x;

        if (float.TryParse(YBox.Text, out float y)) Model!.Position.Y = y;

        if (float.TryParse(ZBox.Text, out float z)) Model!.Position.Z = z;

    }

    private void HandleNonNumericInput(object? sender, KeyEventArgs e)
    {
        var tb = sender as TextBox;

        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            UpdatePositionText();
            TopLevel.GetTopLevel(tb)?.Focus();
            e.Handled = true;
        }

        if (tb == null)
        {
            e.Handled = true;
            return;
        }

        bool rejectKey = true;

        // Digits allowed
        if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            rejectKey = false;
        // Period: only allow if not already in text
        else if ((e.Key == Key.OemPeriod || e.Key == Key.Decimal) && !tb.Text.Contains("."))
            rejectKey = false;
        // Minus: only at start and not already present
        else if ((e.Key == Key.OemMinus || e.Key == Key.Subtract) && !tb.Text.Contains("-"))
            rejectKey = false;

        e.Handled = rejectKey;
    }
}