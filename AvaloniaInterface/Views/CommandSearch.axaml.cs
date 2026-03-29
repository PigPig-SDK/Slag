using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using UI.Commands;
using UI.ViewModels;
using System;
using System.Collections.Generic;

namespace UI;

public partial class CommandSearch : UserControl
{
    private Dictionary<string, Type> _commands = new() 
    { 
        { "move", typeof(MoveCommand) },
        { "rotate", typeof(RotateCommand) },
        { "scale", typeof(ScaleCommand) },
        { "extrude", typeof(ExtrudeCommand) },
    };
    public CommandSearch()
    {
        InitializeComponent();

        //Focus on CTRL+F
        this.AttachedToVisualTree += (s, e) =>
        {
            var window = TopLevel.GetTopLevel(this);
            window.KeyDown += (s, e) =>
            {
                if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
                {
                    SearchBoxInput.Focus();
                    e.Handled = true;
                }
            };
        };

        SearchBoxInput.GotFocus += (s, e) =>
        {
            SearchBoxInput.IsDropDownOpen = true;
        };
        SearchBoxInput.ItemsSource = _commands.Keys;
    }

    private void KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            SearchBoxInput.IsDropDownOpen = false;

            string? input = SearchBoxInput.Text;
            if (!string.IsNullOrWhiteSpace(input))
            {
                if (_commands.TryGetValue(input.ToLower(), out Type? commandType))
                {
                    CommandInvoker.Singleton.RunCommand((ICommand)Activator.CreateInstance(commandType)!,(null, null, CommandInfo.Initialization)
                    );
                }

                SearchBoxInput.Text = string.Empty;
                GLControl.Instance?.Focus();
            }

            e.Handled = true;
        }
    }
}