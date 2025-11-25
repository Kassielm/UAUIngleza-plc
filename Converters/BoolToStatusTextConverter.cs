using System.Globalization;

namespace UAUIngleza_plc.Converters
{
    public class BoolToStatusTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected ? "ONLINE" : "OFFLINE";
            }
            return "---";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
