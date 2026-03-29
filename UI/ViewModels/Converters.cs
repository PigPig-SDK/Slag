using Avalonia.Data.Converters;

namespace UI.ViewModels;

public static class Converters
{
    public static readonly FuncValueConverter<double, bool> WiderThan60 =
        new FuncValueConverter<double, bool>(w => w > 60);
}
