using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Core;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UI.Commands;

namespace UI.ViewModels;

public partial class HierarchyView : UserControl
{

    private Dictionary<Model, HierarchyModel> _Mapping = [];

    private bool _attached = false;

    public HierarchyView()
    {
        InitializeComponent();
        foreach (Model model in SceneHierarchy.Instance.GetModels(HierarchyType.Model))
        {
            OnModelAdded(HierarchyType.Model,model);
        }
        DetachedFromVisualTree += OnDetach;
        AttachedToVisualTree += OnAttach;
    }

    private void OnAttach(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (_attached) return;
        SceneHierarchy.Instance.OnModelAdded += OnModelAdded;
        SceneHierarchy.Instance.OnModelRemoved += OnModelRemoved;
        _attached = true;
    }

    private void OnDetach(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (!_attached) return;
        SceneHierarchy.Instance.OnModelAdded -= OnModelAdded;
        SceneHierarchy.Instance.OnModelRemoved -= OnModelRemoved;
        _attached = false;
    }

    private void OnModelRemoved(HierarchyType hierarchyType, Model model)
    {
        if (!_Mapping.ContainsKey(model)) return;

        HierarchyStack.Children.Remove(_Mapping[model]);
        _Mapping.Remove(model);
    }

    private void OnModelAdded(HierarchyType hierarchyType,Model model)
    {
        HierarchyModel modelView = new HierarchyModel { Model = model };
        HierarchyStack.Children.Add(modelView);
        modelView.ReadModelData();
        _Mapping.Add(model, modelView);
    }

    private void OnAddObjectButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.Open(button);
        }
    }

    private void OnAddCubePressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SceneHierarchy.Instance.AddModel(HierarchyType.Model,ModelPrefabs.Cube());
    }

    public async void SummonObjectWithSteps(string title, string description,Func<int, Model> modelProducer, int width, int height)
    {
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow == null) return;

        Model? model = modelProducer(10);
        SceneHierarchy.Instance.AddModel(HierarchyType.Model, model);
        var stepInput = new NumericUpDown
        {
            Value = 10,
            Minimum = 2,
            Maximum = 250,
            Increment = 1,
            FormatString = "0",
            Width = 120
        };

        stepInput.ValueChanged += (_, e) =>
        {
            int newStepCount = (int)Math.Clamp(e.NewValue ?? 2, 2, 250);
            SceneHierarchy.Instance.RemoveModel(HierarchyType.Model, model);
            model = modelProducer(newStepCount);
            SceneHierarchy.Instance.AddModel(HierarchyType.Model, model);
        };

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Stretch,
            Children = { MenuPropertyCreator("Step", stepInput) }
        };

        var dialog = new ConfirmDialog(title, description, width, height, panel);
        await dialog.ShowDialog(parentWindow).ConfigureAwait(true);

        if (!dialog.Confirmed)
        {
            SceneHierarchy.Instance.RemoveModel(HierarchyType.Model, model);
            model = null;
        }
        else
        {
            SelectionManager.Instance.SelectModel(model);
        }
    }

    private void OnAddSpherePressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SummonObjectWithSteps("Add Sphere", "Select a resolution", (int x) => { return ModelPrefabs.Sphere((uint)x, 1); }, 300, 175);
    }

    private void OnAddCylinderPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SummonObjectWithSteps("Add Cylinder", "Select a resolution", (int x) => { return ModelPrefabs.Cylinder(1, 1, x); }, 300, 175);
    }

    private void OnAddConePressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SummonObjectWithSteps("Add Cone", "Select a resolution", (int x) => { return ModelPrefabs.Cone(x, 1, 1); }, 300, 175);
    }

    private async void OnAddTorusPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow == null) return;

        int stepSize = 10;
        float torusRadius = 2;
        float circleRadius = 1;
        Model? model = ModelPrefabs.Torus(stepSize, stepSize, torusRadius, circleRadius);
        SceneHierarchy.Instance.AddModel(HierarchyType.Model, model);

        var stepInput = new NumericUpDown
        {
            Value = stepSize,
            Minimum = 3,
            Maximum = 250,
            Increment = 1,
            FormatString = "0",
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        stepInput.ValueChanged += (_, e) => { stepSize = (int)Math.Clamp(e.NewValue ?? 2, 2, 250); };

        var radiusInput = new NumericUpDown
        {
            Value = (decimal)torusRadius,
            Minimum = 0,
            Maximum = 50000,
            Increment = 0.1m,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        radiusInput.ValueChanged += (_, e) => { torusRadius = (float)Math.Clamp(e.NewValue ?? 0, 0, 50000); };

        var circleRadiusInput = new NumericUpDown
        {
            Value = (decimal)circleRadius,
            Minimum = 0,
            Maximum = 50000,
            Increment = 0.1m,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        circleRadiusInput.ValueChanged += (_, e) => { circleRadius = (float)Math.Clamp(e.NewValue ?? 0, 0, 50000); };

        //Handle the redraw...
        EventHandler<NumericUpDownValueChangedEventArgs> ? onUpdateHook = (_, e) =>
        {
            SceneHierarchy.Instance.RemoveModel(HierarchyType.Model, model);
            model = ModelPrefabs.Torus(stepSize, stepSize, torusRadius, circleRadius);
            SceneHierarchy.Instance.AddModel(HierarchyType.Model, model);
        };

        stepInput.ValueChanged += onUpdateHook;
        radiusInput.ValueChanged += onUpdateHook;
        circleRadiusInput.ValueChanged += onUpdateHook;

        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Stretch,
            Children =  { MenuPropertyCreator("Step", stepInput) , MenuPropertyCreator("Torus Radius", radiusInput), MenuPropertyCreator("Circle Radius", circleRadiusInput) }
        };

        var dialog = new ConfirmDialog("Add Torus", "Select values", 300, 275, panel);
        await dialog.ShowDialog(parentWindow).ConfigureAwait(true);

        if (!dialog.Confirmed)
        {
            SceneHierarchy.Instance.RemoveModel(HierarchyType.Model, model);
            model = null;
        }
        else
        {
            SelectionManager.Instance.SelectModel(model);
        }
    }

    private static Grid MenuPropertyCreator(string title, NumericUpDown selector)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto"),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var label = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(label, 0);
        Grid.SetColumn(selector, 1);

        grid.Children.Add(label);
        grid.Children.Add(selector);

        return grid;
    }
}