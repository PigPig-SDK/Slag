using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Models;
using System;
using System.Collections.ObjectModel;

namespace OpenglAvaloniaTest.ViewModels;

public partial class HierarchyView : UserControl, IDisposable
{

    public HierarchyView()
    {
        InitializeComponent();
        foreach(Model model in SceneHierarchy.Instance.Models)
        {
            OnModelAdded(model);
        }
        SceneHierarchy.Instance.OnModelAdded += OnModelAdded;
    }

    private void OnModelAdded(Model model)
    {
        HierarchyModel modelView = new HierarchyModel { Model = model };
        HierarchyStack.Children.Add(modelView);
        modelView.ReadModelData();
    }

    ~HierarchyView() => Dispose();

    public void Dispose()
    {
        SceneHierarchy.Instance.OnModelAdded -= OnModelAdded;
    }
}