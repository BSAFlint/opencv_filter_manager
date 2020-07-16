using FilterBuilder.Filter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SimpleFilterBuilder.Common
{
    class FilterTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FilterType type = (FilterType)value;
            Color useColor = Color.FromArgb(155, 255, 255, 255);
            switch (type)
            {
                case FilterType.Source: useColor = Color.FromArgb(155, 0, 0, 255); break;
                case FilterType.BGRBlank: useColor = Color.FromArgb(155, 255, 255, 255); break;
                case FilterType.File: useColor = Color.FromArgb(155, 0, 0, 255); break;
                case FilterType.SourceSwipe: useColor = Color.FromArgb(155, 0, 100, 255); break;
                case FilterType.Split: useColor = Color.FromArgb(155, 255, 0, 0); break;
                case FilterType.Merge: useColor = Color.FromArgb(155, 0, 255, 0); break;
                case FilterType.MergeInSrc: useColor = Color.FromArgb(205, 70, 255, 0); break;
                case FilterType.Negative: useColor = Color.FromArgb(155, 23, 23, 23); break;
                case FilterType.Bgr2Gray: useColor = Color.FromArgb(155, 55, 55, 55); break;
                case FilterType.Gray2Bgr: useColor = Color.FromArgb(155, 255, 255, 255); break;
                case FilterType.ResizeAbs: useColor = Color.FromArgb(155, 0, 255, 255); break;
                case FilterType.Resize: useColor = Color.FromArgb(155, 0, 255, 255); break;
                case FilterType.Rotation: useColor = Color.FromArgb(155, 255, 255, 0); break;
                case FilterType.Blur: useColor = Color.FromArgb(155, 255, 0, 255); break;
                case FilterType.Morphology: useColor = Color.FromArgb(155, 55, 255, 55); break;
                case FilterType.Threshold: useColor = Color.FromArgb(155, 255, 55, 255); break;
                case FilterType.Gradient: useColor = Color.FromArgb(155, 255, 255, 55); break;
                case FilterType.AddRect: useColor = Color.FromArgb(155, 255, 77, 55); break;
                case FilterType.CropRect: useColor = Color.FromArgb(175, 235, 27, 65); break;
                case FilterType.RectExport: useColor = Color.FromArgb(155, 205, 177, 55); break;
                case FilterType.PixelSum: useColor = Color.FromArgb(155, 23, 66, 55); break;
                case FilterType.Histogram: useColor = Color.FromArgb(23, 23, 23, 155); break;
                case FilterType.ContourSearch: useColor = Color.FromArgb(255, 0, 255, 155); break;
                case FilterType.ContourAreaSelect: useColor = Color.FromArgb(255, 95, 77, 43); break;
                case FilterType.ContourAraund: useColor = Color.FromArgb(255, 77, 95, 43); break;
                case FilterType.ContourShift: useColor = Color.FromArgb(255, 177, 195, 203); break;
                case FilterType.ContourMaskMerge: useColor = Color.FromArgb(255, 87, 105, 133); break;
            }
            return new SolidColorBrush(useColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
