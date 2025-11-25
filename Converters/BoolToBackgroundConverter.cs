using System.Globalization;

namespace UAUIngleza_plc.Converters
{
    public class BoolToBackgroundConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected 
                    ? Color.FromRgba(40, 167, 69, 0.9)  // Verde
                    : Color.FromRgba(220, 53, 69, 0.9); // Vermelho
            }
            return Color.FromRgba(128, 128, 128, 0.9); // Cinza
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
