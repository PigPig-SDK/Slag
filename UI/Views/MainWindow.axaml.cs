using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Core;
using UI.Commands;
using UI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace UI.Views;

public partial class MainWindow : Window
{
    public static MainWindow? Instance;
    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        CommandInvoker.Singleton.CommandExecuted += Singleton_CommandExecuted;
    }

    private void Singleton_CommandExecuted(ICommand? obj)
    {
        if (obj == null || !obj!.DisplayToolText)
        {
            CommandTitle.Content = string.Empty;
            CommandTitle.IsVisible = false;

            CommandDescription.Content = string.Empty;
            CommandDescription.IsVisible = false;

            CommandSeparator.IsVisible = false;
        }
        else
        {
            CommandTitle.Content = obj.Name;
            CommandTitle.IsVisible = true;

            CommandDescription.Content = obj.Description;
            CommandDescription.IsVisible = true;

            CommandSeparator.IsVisible = true;
        }
    }

    private void OnRendermodeChanged(object? sender, SelectionChangedEventArgs e)
    {
        var cb = (ComboBox)sender!;
        if (cb == null) return;

        var item = (ComboBoxItem)cb.SelectedItem!;
        if (item == null) return;

        var mode = item.Content?.ToString();
        if (mode == null) return;

        switch (mode)
        {
            case "All":
                GLControl.RenderMode = RenderMode.Solid;
                break;
            case "Solid":
                GLControl.RenderMode = RenderMode.Triangles;
                break;
            case "Wireframe":
                GLControl.RenderMode = RenderMode.Wireframe;
                break;
            case "Points":
                GLControl.RenderMode = RenderMode.Verts;
                break;
        }
    }

    private void OnVertexPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => SelectionModeChange(ViewModels.SelectionMode.Vertex);

    private void OnEdgePressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => SelectionModeChange(ViewModels.SelectionMode.Edge);

    private void OnFacePressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => SelectionModeChange(ViewModels.SelectionMode.Face);

    private void OnShapePressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => SelectionModeChange(ViewModels.SelectionMode.Object);

    private void SelectionModeChange(ViewModels.SelectionMode mode)
    {
        SelectionManager.Instance.CurrentSelectionMode = mode;

        faceButton.Background = (mode == ViewModels.SelectionMode.Face) ? Avalonia.Media.Brushes.Orange : Avalonia.Media.Brushes.Transparent;
        vertexButton.Background = (mode == ViewModels.SelectionMode.Vertex) ? Avalonia.Media.Brushes.Orange : Avalonia.Media.Brushes.Transparent;
        objectButton.Background = (mode == ViewModels.SelectionMode.Object) ? Avalonia.Media.Brushes.Orange : Avalonia.Media.Brushes.Transparent;
        edgeButton.Background = (mode == ViewModels.SelectionMode.Edge) ? Avalonia.Media.Brushes.Orange : Avalonia.Media.Brushes.Transparent;
    }

    private async void OnFileOpen(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open OBJ File",
            FileTypeFilter = new[] {
                new FilePickerFileType("OBJ Files") {
                    Patterns = new[] { "*.obj" },
                } },
            AllowMultiple = true
        });

        for (int i = 0; i < files.Count; i++)
        {
            Stream stream = await files[i].OpenReadAsync();

            StreamReader streamReader = new(stream);
            try
            {
                List<Model> models = OBJFile.LoadOBJ(streamReader);
                foreach (Model model in models)
                {
                    SceneHierarchy.Instance.AddModel(HierarchyType.Model, model);
                }
            }
            catch (InvalidDataException exception)
            {
                Console.WriteLine(exception.Message);
            }
            finally
            {
                streamReader.Close();
            }
        }
    }

    private async void OnFileSave(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Scene As...",
            SuggestedFileName = "ObjectSet",
            DefaultExtension = "obj",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("OBJ Files")
                {
                    Patterns = new[] { "*.obj" },
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            },
            ShowOverwritePrompt = true
        });

        if (file == null) return;//User didnt make a selection

        Stream stream = await file.OpenWriteAsync();
        try
        {
            using StreamWriter streamWriter = new StreamWriter(stream);
            OBJFile.SaveOBJ(streamWriter);
        }
        catch (InvalidDataException exception)
        {
            Console.WriteLine(exception.Message);
        }
    }

    private void OnUndoPressed(object? sender, RoutedEventArgs e)
    {
        CommandInvoker.Singleton?.ExecuteUndo();
    }

    private void OnRedoPressed(object? sender, RoutedEventArgs e)
    {
        CommandInvoker.Singleton?.ExecuteRedo();
    }
}