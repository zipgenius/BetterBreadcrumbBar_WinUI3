// =============================================================================
// BetterBreadcrumbBar for WinUI 3
// Author:  Matteo Riso
// Version: 0.8.6
// Website: https://zipgenius.it
// Written with Claude AI
// License: MIT
// =============================================================================

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BetterBreadcrumbBar.Control;

/// <summary>Converts a <see cref="bool"/> to <see cref="Visibility"/>: <c>true</c> → Visible, <c>false</c> → Collapsed.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Visible;
}

/// <summary>Converts a <see cref="bool"/> to <see cref="Visibility"/>: <c>true</c> → Collapsed, <c>false</c> → Visible.</summary>
public class BoolToInverseVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Collapsed;
}
