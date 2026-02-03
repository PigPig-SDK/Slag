using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Models;
using System;
using System.Runtime.Intrinsics.Arm;


namespace OpenglAvaloniaTest;

public partial class HierarchyModel : UserControl
{
    private const float _DegreesToRadians = (float)(Math.PI / 180.0);
    private const float _RadiansToDegrees = (float)(180.0 / Math.PI);

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
        UpdateRotationText();
        UpdateScaleText();
    }
    void UpdatePositionText()
    {
        XBox.Text = Model!.Position.X.ToString();
        YBox.Text = Model!.Position.Y.ToString();
        ZBox.Text = Model!.Position.Z.ToString();
    }

    private void UpdateRotationText()
    {
        XRotationBox.Text = (Model!.Rotation.X * _RadiansToDegrees).ToString();
        YRotationBox.Text = (Model!.Rotation.Y * _RadiansToDegrees).ToString();
        ZRotationBox.Text = (Model!.Rotation.Z * _RadiansToDegrees).ToString();
    }

    private void UpdateScaleText()
    {
        XScaleBox.Text = Model!.Scale.X.ToString();
        YScaleBox.Text = Model!.Scale.Y.ToString();
        ZScaleBox.Text = Model!.Scale.Z.ToString();
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
        HiddenImage.IsVisible = Model.Hidden;
        ShownImage.IsVisible = !Model.Hidden;
    }

    private void DeleteModelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Model == null) return;

        SceneHierarchy.Instance.RemoveModel(HierarchyType.Model, Model!);
    }

    private void OnPositionTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Model == null) return;

        if (float.TryParse(XBox.Text, out float x)) Model!.Position.X = x;

        if (float.TryParse(YBox.Text, out float y)) Model!.Position.Y = y;

        if (float.TryParse(ZBox.Text, out float z)) Model!.Position.Z = z;

    }

    private void OnRotationTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Model == null) return;

        if (float.TryParse(XRotationBox.Text, out float x)) Model!.Rotation.X = x * _DegreesToRadians;

        if (float.TryParse(YRotationBox.Text, out float y)) Model!.Rotation.Y = y * _DegreesToRadians;

        if (float.TryParse(ZRotationBox.Text, out float z)) Model!.Rotation.Z = z * _DegreesToRadians;
    }

    private void OnScaleTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (Model == null) return;

        if (float.TryParse(XScaleBox.Text, out float x)) Model!.Scale.X = x;

        if (float.TryParse(YScaleBox.Text, out float y)) Model!.Scale.Y = y;

        if (float.TryParse(ZScaleBox.Text, out float z)) Model!.Scale.Z = z;
    }

    private void HandleNonNumericInput(object? sender, KeyEventArgs e)
    {
        if (e == null) return;
        if (sender == null) return;

        var tb = sender as TextBox;

        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            UpdatePositionText();
            UpdateRotationText();
            UpdateScaleText();
            TopLevel.GetTopLevel(tb)?.Focus();
            e.Handled = true;
        }

        if (tb == null)
        {
            e.Handled = true;
            return;
        }
        if (tb.Text == null) return;

        bool rejectKey = true;

        // Digits allowed
        if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            rejectKey = false;
        // Period: only allow if not already in text
        else if ((e.Key == Key.OemPeriod || e.Key == Key.Decimal) && !tb.Text!.Contains("."))
            rejectKey = false;
        // Minus: only at start and not already present
        else if ((e.Key == Key.OemMinus || e.Key == Key.Subtract) && !tb.Text!.Contains("-"))
            rejectKey = false;

        e.Handled = rejectKey;
    }
}