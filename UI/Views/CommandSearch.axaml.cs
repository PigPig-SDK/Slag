using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using UI.Commands;
using UI.ViewModels;
using static System.Net.Mime.MediaTypeNames;

namespace UI;

public partial class CommandSearch : UserControl
{
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

    static IEnumerable<ICommand> CommandList(string? text)
    {
        if (text is not null)
        {
            foreach (var commandType in Enum.GetValues<CommandTypes>())
            {
                var producer = CommandLookup.CommandFactory[commandType];
                if (commandType.ToString().ToLower(CultureInfo.CurrentCulture).Contains(text.ToLower(CultureInfo.CurrentCulture), StringComparison.CurrentCulture))
                    yield return producer(); ;
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
            foreach (CommandTypes commandType in Enum.GetValues<CommandTypes>())
            {
                var producer = CommandLookup.CommandFactory[commandType];
                if (commandType.ToString().ToLower(CultureInfo.CurrentCulture).Contains(input.ToLower(CultureInfo.CurrentCulture), StringComparison.CurrentCulture))
                    CommandInvoker.Singleton.RunCommand(producer());
            }
        }
        SearchBoxInput.Text = string.Empty;
        GLControl.Instance?.Focus();
    }
}