using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using UI.Commands;
using UI.ViewModels;

namespace UI;

public partial class CommandSearch : UserControl
{
    HashSet<Type> _commands = new ()
    {
        typeof(MoveCommand),
        typeof(RotateCommand),
        typeof(ScaleCommand),
        typeof(ExtrudeCommand),
        typeof(MergeCommand),
        typeof(FlipCommand),
        typeof(DebugCommand),
    };
    public CommandSearch()
    {
        InitializeComponent();

        //Focus on CTRL+F
        this.AttachedToVisualTree += (s, e) =>
        {
            var window = TopLevel.GetTopLevel(this);
            if(window is null) return;

            window.KeyDown += (s, e) =>
            {
                if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
                {
                    SearchBoxInput.Focus();
                    e.Handled = true;
                }
            };

            SearchBoxInput.AsyncPopulator = async (text, token) =>
            {
                return CommandList(text).ToList();
            };
        };

        SearchBoxInput.GotFocus += (s, e) =>
        {
            SearchBoxInput.IsDropDownOpen = true;
        };

        SearchBoxInput.ClearSelectionOnLostFocus = true;
    }

    IEnumerable<ICommand> CommandList(string? text)
    {
        if (text is not null)
        {
            foreach (var commandType in _commands)
            {
                if (Activator.CreateInstance(commandType) is not ICommand command) continue;

                if (command.Name.ToLower(CultureInfo.CurrentCulture).Contains(text.ToLower(CultureInfo.CurrentCulture), StringComparison.CurrentCulture))
                    yield return command;
            }
        }
    }

    private void SearchKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            Submit();
            e.Handled = true;
        }
    }
    private void Submit()
    {
        SearchBoxInput.IsDropDownOpen = false;

        string? input = SearchBoxInput.Text;
        if (!string.IsNullOrWhiteSpace(input))
        {
            foreach (Type commandType in _commands)
            {
                ICommand? command = Activator.CreateInstance(commandType) as ICommand;

                if (command is null) goto End;

                if(command.Name.ToLower(CultureInfo.CurrentCulture)
                    .Equals(input.ToLower(CultureInfo.CurrentCulture), StringComparison.Ordinal)) 
                    CommandInvoker.Singleton.RunCommand((ICommand)Activator.CreateInstance(commandType)!, new (null, null, CommandInfo.Initialization));
            }
        }

    End:
        SearchBoxInput.Text = string.Empty;
        GLControl.Instance?.Focus();
    }
}