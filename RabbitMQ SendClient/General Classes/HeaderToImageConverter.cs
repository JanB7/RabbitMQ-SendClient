namespace RabbitMQ_SendClient
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    [ValueConversion(typeof(string), typeof(BitmapImage))]
    public class HeaderToImageConverter : IValueConverter
    {
        #region Variables & Structures

        public static HeaderToImageConverter Instance = new HeaderToImageConverter();

        #endregion Variables & Structures

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Get the full path
            var path = (string)value;

            // If the path is null, ignore
            if (path == null)
                return null;

            var image = "Icons/organization-9.png";
            var pathList = path.Substring(0,
                path.IndexOf(":", path.IndexOf(":") + 1) + path.IndexOf("/", path.IndexOf("//") + 1));
            if ((pathList != string.Empty) && pathList.Contains("/"))
                image = "Icons/organization.png";
            else if (pathList != string.Empty)
                image = "Icons/organization-6.png";
            return new BitmapImage(new Uri($"pack://application:,,,/{image}"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}