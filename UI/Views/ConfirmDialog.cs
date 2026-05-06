using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;


namespace UI;

public class ConfirmDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmDialog(string title, string message, int width = 300, int height = 160)
    {
        Title = title;
        Width = width;
        Height = height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Icon = null;
        SystemDecorations = SystemDecorations.BorderOnly;
        Background = Brushes.Transparent;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        ExtendClientAreaToDecorationsHint = true;

        var yes = new Button { Content = "Yes", Width = 80 };
        var no = new Button { Content = "No", Width = 80 };

        yes.Click += (_, _) => { Confirmed = true; Close(); };
        no.Click += (_, _) => { Confirmed = false; Close(); };

        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(240, 30, 30, 30)),
            CornerRadius = new CornerRadius(8),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                Blur = 16,
                OffsetX = 0,
                OffsetY = 4,
                Color = Color.FromArgb(120, 0, 0, 0)
            }),
            Child = new StackPanel
            {
                Children =
        {
            // Title bar
            new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 45, 45, 45)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8),
                Child = new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                }
            },
            // Content
            new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { yes, no }
                    }
                }
            }
        }
            }
        };
    }
}