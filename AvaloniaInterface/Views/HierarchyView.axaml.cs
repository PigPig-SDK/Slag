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
        foreach (Model model in SceneHierarchy.Instance.Models)
        {
            OnModelAdded(model);
        }
        DetachedFromVisualTree += OnDetach;
        AttachedToVisualTree += OnAttach;
    }

    private void OnAttach(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if(_attached) return;
        SceneHierarchy.Instance.OnModelAdded += OnModelAdded;
        SceneHierarchy.Instance.OnModelRemoved += OnModelRemoved;
        _attached = true;
    }

    private void OnDetach(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if(!_attached) return;
        SceneHierarchy.Instance.OnModelAdded -= OnModelAdded;
        SceneHierarchy.Instance.OnModelRemoved -= OnModelRemoved;
        _attached = false;
    }

    private void OnModelRemoved(Model model)
    {
        if (!_Mapping.ContainsKey(model)) return;

        HierarchyStack.Children.Remove(_Mapping[model]);
        _Mapping.Remove(model);
    }

    private void OnModelAdded(Model model)
    {
        HierarchyModel modelView = new HierarchyModel { Model = model };
        HierarchyStack.Children.Add(modelView);
        modelView.ReadModelData();
        _Mapping.Add(model, modelView);
    }


}