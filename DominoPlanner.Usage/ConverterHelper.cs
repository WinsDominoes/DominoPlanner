﻿using DominoPlanner.Core;
using Emgu.CV.CvEnum;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DominoPlanner.Usage
{
    class ConverterHelper
    {
    }
    public class AmountToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int anzahl = 0, gesamt = 0;
            if (int.TryParse(values[0].ToString(), out anzahl) && int.TryParse(values[1].ToString(), out gesamt))
            {
                if (anzahl > gesamt)
                {
                    return System.Windows.Media.Brushes.Red;
                }
            }
            return System.Windows.Media.Brushes.Black;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorToHTMLConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color)
            {
                Color c = (Color)value;
                return c.ToString();
            }
            System.Drawing.Color c2 = (System.Drawing.Color)value;
            return System.Drawing.ColorTranslator.ToHtml(c2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color)
            {
                return new SolidColorBrush((Color)value);
            }
            else
            {
                System.Drawing.Color c = (System.Drawing.Color)value;
                return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Image() { Source = new BitmapImage(new Uri(ImageHelper.GetImageOfFile(value.ToString()), UriKind.RelativeOrAbsolute)) };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InterToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Inter)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public sealed class DiffusionModeToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (targetType.IsEnum)
            {
                var val =(int)(double)value;
                switch (val)
                {
                    case 0: return Inter.Nearest;
                    case 1: return Inter.Linear;
                    case 2: return Inter.LinearExact;
                    case 3: return Inter.Area;
                    case 4: return Inter.Cubic;
                    case 5: return Inter.Lanczos4;
                    default: return Inter.Nearest;
                }
            }

            if (value.GetType().IsEnum)
            {
                var val = (Inter)value;
                switch (val)
                {
                    case Inter.Nearest: return 0;
                    case Inter.Linear: return 1;
                    case Inter.LinearExact: return 2;
                    case Inter.Area: return 3;
                    case Inter.Cubic: return 4;
                    case Inter.Lanczos4: return 5;
                    default: return 0;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // perform the same conversion in both directions
            return Convert(value, targetType, parameter, culture);
        }
    }
    public struct DictHelper
    {
        public int index;
        public Type type;
        public string name;
    }
    public class DitheringToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch(value)
            {
                case FloydSteinbergDithering d:
                    return "Floyd/Steinberg Dithering";
                case JarvisJudiceNinkeDithering d:
                    return "Jarvis/Judice/Ninke Dithering";
                case StuckiDithering d:
                    return "Stucki Dithering";
                default:
                    return "No Dithering";
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class DitheringToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case FloydSteinbergDithering d:
                    return 1;
                case JarvisJudiceNinkeDithering d:
                    return 2;
                case StuckiDithering d:
                    return 3;
                default:
                    return 0;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((int)(double)value)
            {
                case 1:
                    return new FloydSteinbergDithering();
                case 2:
                    return new JarvisJudiceNinkeDithering() ;
                case 3:
                    return new StuckiDithering() ;
                default:
                    return new Dithering();
            }
        }
    }
    public class ColorModeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case Cie1976Comparison d:
                    return "CIE-76 (ISO 12647)";
                case CmcComparison d:
                    return "CMC (l:c)";
                case Cie94Comparison d:
                    return "CIE-94 (DIN 99)";
                default:
                    return "CIE-Delta E 2000";
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ColorModeToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case Cie1976Comparison d:
                    return 0;
                case CmcComparison d:
                    return 1;
                case Cie94Comparison d:
                    return 2;
                default:
                    return 3;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((int)(double)value)
            {
                case 1:
                    return new CmcComparison();
                case 2:
                    return new Cie94Comparison();
                case 3:
                    return new CieDe2000Comparison();
                default:
                    return new Cie1976Comparison();
            }
        }
    }
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((Enum)value).HasFlag((Enum)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }
    public class IterationInformationToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IterativeColorRestriction;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return new IterativeColorRestriction(2, 0.1);
            }
            else
            {
                return new NoColorRestriction();
            }
        }
    }
    public class BoolToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return "/Icons/ok.ico";
            else
                return "/Icons/closewindow.ico";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class LockedToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return "/Icons/lock.ico";
            else
                return "/Icons/unlock.ico";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BoolInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
    public class FilenameToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string uri = value as string;

            if (uri != null)
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                var ur = new Uri(uri, UriKind.RelativeOrAbsolute);
                if (ur.IsAbsoluteUri == false)
                {
                    if (uri[0] == '/')
                        uri = uri.Substring(1);
                    ur = new Uri("pack://application:,,,/" + uri);
                }
                image.UriSource = ur;
                image.DecodePixelWidth = 40;
                image.EndInit();
                return image;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    public class EnumToButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == parameter;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
                return parameter;
            else
                return Enum.GetValues(targetType).GetValue(0);
        }
    }
}
