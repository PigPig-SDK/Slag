using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Models;
using OpenglAvaloniaTest.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace OpenglAvaloniaTest.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
                Title = "Open Text File",
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
    }
}