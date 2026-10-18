using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace ConversorFicheiros;

internal sealed class FileNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string path ? Path.GetFileName(path) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException("File names are display-only values.");
}
