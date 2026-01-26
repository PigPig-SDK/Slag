using Avalonia.Controls;
using Avalonia.Input;
using OpenglAvaloniaTest.ViewModels;
using System;
using System.Diagnostics;

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
    }
}