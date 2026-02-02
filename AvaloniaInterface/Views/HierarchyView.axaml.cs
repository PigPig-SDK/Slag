using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenglAvaloniaTest.ViewModels;

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

    private void OnAddSpherePressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SceneHierarchy.Instance.AddModel(HierarchyType.Model, ModelPrefabs.Sphere(10, 1));
    }

    private void OnAddCylinderPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SceneHierarchy.Instance.AddModel(HierarchyType.Model, ModelPrefabs.Cylinder(1,1,10));
    }

    private void OnAddConePressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        SceneHierarchy.Instance.AddModel(HierarchyType.Model, ModelPrefabs.Cone(10, 1, 1));
    }

    private void OnAddTorusPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //SceneHierarchy.Instance.AddModel(HierarchyType.Model, ModelPrefabs.Torus(20, 20, 2, 1));
        //Stress test torus
        SceneHierarchy.Instance.AddModel(HierarchyType.Model, ModelPrefabs.Torus(1000, 1000, 40, 20));
    }
}